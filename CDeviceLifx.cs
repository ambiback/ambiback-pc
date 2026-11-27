using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;

namespace AmbiBack
{
    /// <summary>
    /// LIFX light over the LIFX LAN protocol (binary, UDP port 56700, little endian, colors as HSBK).
    ///
    /// Bulbs and other single-color lights get the average screen color (Light::SetColor). Multizone lights
    /// (LIFX Z strips, Beam, Neon) can also run as a strip around the screen: one color per zone with
    /// SetExtendedColorZones (82 zones per message). The state before the start (color and power) is
    /// restored on stop. LIFX asks for at most 20 messages per second per light.
    /// </summary>
    [Serializable]
    public class CDeviceLifx : CDeviceLedStripUdp
    {
        public const int LanPort = 56700;
        public const byte ModeStrip = 0;
        public const byte ModeSingleColor = 1;
        public const int ZonesPerMessage = 82;

        public const ushort MsgGetService = 2;
        public const ushort MsgStateService = 3;
        public const ushort MsgGetLabel = 23;
        public const ushort MsgStateLabel = 25;
        public const ushort MsgGetVersion = 32;
        public const ushort MsgStateVersion = 33;
        public const ushort MsgGetColor = 101;
        public const ushort MsgSetColor = 102;
        public const ushort MsgLightState = 107;
        public const ushort MsgSetLightPower = 117;
        public const ushort MsgSetExtendedColorZones = 510;
        public const ushort MsgGetExtendedColorZones = 511;
        public const ushort MsgStateExtendedColorZones = 512;

        private const int HeaderLength = 36;
        private const int ReplyTimeoutMs = 700;
        private const ushort Kelvin = 3500;

        private static readonly Random random = new Random();
        private readonly uint source = (uint)random.Next(2, int.MaxValue);
        private byte sequence;
        private byte[] target = new byte[8];
        private byte[] savedState;          // LightState payload before the start, restored on stop
        private bool restorePending;        // savedState was read before a stream and is not restored yet
        private bool stateReadAttempted;    // the state was asked for in this run, successfully or not
        private byte[] zonePayload;

        #region Stored settings
        /// <summary>Zones of a multizone light; 0 or 1 for a single-color light.</summary>
        public int ZoneCount { get; set; } = 0;
        /// <summary>LIFX product id, as reported by GetVersion.</summary>
        public int ProductId { get; set; } = 0;
        #endregion

        public CDeviceLifx()
        {
            this.SetDeviceType(DeviceType.Lifx);
            this.SetDeviceName("LIFX");
            this.InitClass();
        }

        public CDeviceLifx(string szDeviceName, IPAddress ipAddress)
        {
            this.SetDeviceType(DeviceType.Lifx);
            this.SetDeviceName(szDeviceName);
            this.SetIPAddress(ipAddress);
            this.InitClass();
        }

        public bool IsMultizone { get { return this.ZoneCount > 1; } }

        protected override int DefaultPort { get { return LanPort; } }
        protected override byte DefaultFramesPerSecond { get { return 20; } }
        /// <summary>Modes only for multizone lights; a bulb always shows one color.</summary>
        public override string[] ModeTextKeys { get { return IsMultizone ? new[] { "lifx_mode_zones", "lifx_mode_single" } : null; } }
        public override bool UsesStripLayout { get { return IsMultizone && this.Mode == ModeStrip; } }

        public override void InitClass()
        {
            base.InitClass();
            this.MaxFramesPerSecond = 20;    // LIFX: at most 20 messages per second per light
            this.FrameDelay = 100;
        }

        protected override void WriteStripXml(XmlWriter writer)
        {
            this.WriteXmlElement(writer, "Mode", this.Mode);
            this.WriteXmlElement(writer, "ZoneCount", this.ZoneCount);
            this.WriteXmlElement(writer, "ProductId", this.ProductId);
        }

        protected override void ReadStripXml(XmlReader reader)
        {
            this.Mode = this.ReadXmlElementAsByte(reader, "Mode", ModeSingleColor);
            if (this.Mode > ModeSingleColor)
                this.Mode = ModeSingleColor;
            this.ZoneCount = this.ReadXmlElementAsInt(reader, "ZoneCount", 0);
            this.ProductId = this.ReadXmlElementAsInt(reader, "ProductId", 0);
            if (!IsMultizone)
                this.Mode = ModeSingleColor;
        }

        public override void UpdateFrom(CDevice devCopyFrom)
        {
            base.UpdateFrom(devCopyFrom);
            CDeviceLifx other = (CDeviceLifx)devCopyFrom;
            if (other.ZoneCount != this.ZoneCount && other.ZoneCount > 0)
            {
                this.ZoneCount = other.ZoneCount;
                if (!IsMultizone)
                    this.Mode = ModeSingleColor;
                this.bInitDone = false;
            }
            if (other.ProductId > 0)
                this.ProductId = other.ProductId;
        }

        #region Connection and sending
        protected override void Connect(IPAddress ip)
        {
            base.Connect(ip);
            target = MacTarget(this.InternalNetworkMAC);
            // The state to restore is not read here: a monitor off/on on a connected lamp starts the next stream
            // without a new Connect, so the read lives in OnStreamStart
        }

        protected override void OnDeviceStopped()
        {
            // The device really ended: the next start reads the lamp again, it may have been changed meanwhile
            restorePending = false;
            stateReadAttempted = false;
            savedState = null;
        }

        /// <summary>Sends a message that asks for an answer and waits for that answer; null after the timeout.</summary>
        private byte[] Request(ushort type, byte[] payload, ushort answerType)
        {
            SendDatagram(BuildMessage(type, payload, target, false, true, ++sequence, source));
            var clock = Stopwatch.StartNew();
            while (clock.ElapsedMilliseconds < ReplyTimeoutMs)
            {
                byte[] reply = ReceiveDatagram(ReplyTimeoutMs - (int)clock.ElapsedMilliseconds);
                if (reply == null)
                    return null;
                if (reply.Length >= HeaderLength && MessageType(reply) == answerType)
                    return Payload(reply);
            }
            return null;
        }

        private void Send(ushort type, byte[] payload)
        {
            SendDatagram(BuildMessage(type, payload, target, false, false, ++sequence, source));
        }

        protected override void OnStreamStart()
        {
            // State to restore on stop. Runs before the first frame of every stream, also after a pause on a
            // connected lamp and after a reconnect. The lamp shows the stream color after a lost connection, also
            // when the stream was paused without a restore, so it is only read while no restore is pending. It is
            // asked for once per stream: a failed read leaves nothing pending (no restore for that stream) and is
            // not repeated on a reconnect, when the lamp may already show the stream color. The flag is reset when
            // the stream ends while connected and a restore was sent (OnStreamStop), and when the device thread
            // ends (OnDeviceStopped)
            if (!stateReadAttempted && !restorePending)
            {
                stateReadAttempted = true;
                byte[] state = Request(MsgGetColor, new byte[0], MsgLightState);
                if (state != null && state.Length >= 12)
                {
                    savedState = state;
                    restorePending = true;
                }
            }
            Send(MsgSetLightPower, PowerPayload(true, 0));
        }

        protected override void OnStreamStop()
        {
            // Back to the color and power before the start
            if (!restorePending || savedState == null || savedState.Length < 12)
                return;
            // The stream ended while connected and the restore goes out now: the next start asks the lamp again.
            // Not reset when no restore was pending (the read failed): the lamp may still show the stream color,
            // so reading it again on the next start would save that color and restore it later
            stateReadAttempted = false;
            byte[] color = new byte[13];
            Buffer.BlockCopy(savedState, 0, color, 1, 8);
            WriteUInt32(color, 9, 500);
            Send(MsgSetColor, color);
            bool wasOn = BitConverter.ToUInt16(savedState, 10) > 0;
            Send(MsgSetLightPower, PowerPayload(wasOn, 500));
            // Only after the restore was sent; if the connection is gone the pending state stays for the reconnect
            restorePending = false;
        }

        protected override void SendFrame(byte[] colors, int bytes)
        {
            int count = bytes / 3;
            if (count == 0)
                return;
            uint duration = (uint)(1000 / Math.Max(1, (int)this.FramesPerSecond));
            if (!IsMultizone || this.Mode == ModeSingleColor)
            {
                int avg = AverageColor(colors, count);
                Send(MsgSetColor, ColorPayload((byte)(avg >> 16), (byte)(avg >> 8), (byte)avg, duration));
                return;
            }
            int zones = Math.Min(this.ZoneCount, count);
            for (int start = 0; start < zones; start += ZonesPerMessage)
            {
                int n = Math.Min(ZonesPerMessage, zones - start);
                if (zonePayload == null)
                    zonePayload = new byte[8 + ZonesPerMessage * 8];
                FillExtendedZones(zonePayload, colors, start, n, duration);
                Send(MsgSetExtendedColorZones, zonePayload);
            }
        }
        #endregion

        #region Protocol
        /// <summary>
        /// Header (36 bytes, little endian): size, protocol 1024 | addressable | tagged, source, target
        /// (MAC + 2 zero bytes), 6 reserved, flags (res_required 1, ack_required 2), sequence, 8 reserved,
        /// type, 2 reserved; then the payload.
        /// </summary>
        public static byte[] BuildMessage(ushort type, byte[] payload, byte[] target8, bool tagged, bool resRequired, byte seq, uint src)
        {
            byte[] m = new byte[HeaderLength + payload.Length];
            WriteUInt16(m, 0, (ushort)m.Length);
            WriteUInt16(m, 2, (ushort)(1024 | (1 << 12) | (tagged ? 1 << 13 : 0)));
            WriteUInt32(m, 4, src);
            Buffer.BlockCopy(target8, 0, m, 8, 8);
            m[22] = (byte)(resRequired ? 1 : 0);
            m[23] = seq;
            WriteUInt16(m, 32, type);
            Buffer.BlockCopy(payload, 0, m, HeaderLength, payload.Length);
            return m;
        }

        public static ushort MessageType(byte[] m) { return BitConverter.ToUInt16(m, 32); }

        public static byte[] Payload(byte[] m)
        {
            byte[] p = new byte[m.Length - HeaderLength];
            Buffer.BlockCopy(m, HeaderLength, p, 0, p.Length);
            return p;
        }

        /// <summary>Target field from a MAC address ("d073d5…", 12 hex digits): 6 bytes plus 2 zero bytes.</summary>
        public static byte[] MacTarget(string mac)
        {
            byte[] t = new byte[8];
            if (string.IsNullOrEmpty(mac) || mac.Length < 12)
                return t;
            for (int i = 0; i < 6; i++)
                t[i] = Convert.ToByte(mac.Substring(i * 2, 2), 16);
            return t;
        }

        public static string MacText(byte[] message)
        {
            var sb = new StringBuilder();
            for (int i = 8; i < 14; i++)
                sb.Append(message[i].ToString("x2"));
            return sb.ToString();
        }

        /// <summary>RGB to HSBK (hue, saturation, brightness 0..65535; kelvin fixed): brightness is the HSV value.</summary>
        public static void RgbToHsbk(byte r, byte g, byte b, out ushort hue, out ushort sat, out ushort bri)
        {
            int max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
            int delta = max - min;
            double h = 0;
            if (delta > 0)
            {
                if (max == r) h = ((g - b) / (double)delta) % 6;
                else if (max == g) h = (b - r) / (double)delta + 2;
                else h = (r - g) / (double)delta + 4;
                h *= 60;
                if (h < 0) h += 360;
            }
            hue = (ushort)Math.Round(h / 360.0 * 65535);
            sat = (ushort)(max == 0 ? 0 : Math.Round(delta / (double)max * 65535));
            bri = (ushort)Math.Round(max / 255.0 * 65535);
        }

        private static void WriteHsbk(byte[] p, int offset, byte r, byte g, byte b)
        {
            RgbToHsbk(r, g, b, out ushort hue, out ushort sat, out ushort bri);
            WriteUInt16(p, offset, hue);
            WriteUInt16(p, offset + 2, sat);
            WriteUInt16(p, offset + 4, bri);
            WriteUInt16(p, offset + 6, Kelvin);
        }

        /// <summary>Light::SetColor: reserved byte, HSBK, duration in ms.</summary>
        public static byte[] ColorPayload(byte r, byte g, byte b, uint durationMs)
        {
            byte[] p = new byte[13];
            WriteHsbk(p, 1, r, g, b);
            WriteUInt32(p, 9, durationMs);
            return p;
        }

        /// <summary>Light::SetPower: level 0 or 65535, duration in ms.</summary>
        public static byte[] PowerPayload(bool on, uint durationMs)
        {
            byte[] p = new byte[6];
            WriteUInt16(p, 0, (ushort)(on ? 65535 : 0));
            WriteUInt32(p, 2, durationMs);
            return p;
        }

        /// <summary>
        /// SetExtendedColorZones: duration, apply (1), first zone, color count, then always 82 HSBK slots.
        /// Zone start + i gets RGB color start + i.
        /// </summary>
        public static void FillExtendedZones(byte[] p, byte[] colors, int start, int count, uint durationMs)
        {
            Array.Clear(p, 0, p.Length);
            WriteUInt32(p, 0, durationMs);
            p[4] = 1;
            WriteUInt16(p, 5, (ushort)start);
            p[7] = (byte)count;
            for (int i = 0; i < count; i++)
            {
                int c = (start + i) * 3;
                WriteHsbk(p, 8 + i * 8, colors[c], colors[c + 1], colors[c + 2]);
            }
        }

        private static void WriteUInt16(byte[] p, int o, ushort v) { p[o] = (byte)v; p[o + 1] = (byte)(v >> 8); }
        private static void WriteUInt32(byte[] p, int o, uint v) { p[o] = (byte)v; p[o + 1] = (byte)(v >> 8); p[o + 2] = (byte)(v >> 16); p[o + 3] = (byte)(v >> 24); }
        #endregion

        #region Discovery
        /// <summary>What a LIFX light reports: address, MAC, label, product and zones.</summary>
        public class LifxInfo
        {
            public IPAddress Address;
            public string Mac = "";
            public string Label = "";
            public int Product;
            public int Zones;
        }

        /// <summary>
        /// LIFX lights on the network: GetService as broadcast (or to <paramref name="target"/> only), then
        /// label, version and the zone count of every light that answered.
        /// </summary>
        public static List<LifxInfo> Discover(int timeoutMs = 1500, IPAddress target = null)
        {
            var found = new List<LifxInfo>();
            uint src = (uint)random.Next(2, int.MaxValue);
            using (var client = new UdpClient(AddressFamily.InterNetwork))
            {
                client.EnableBroadcast = true;
                client.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
                byte[] scan = BuildMessage(MsgGetService, new byte[0], new byte[8], true, true, 1, src);
                if (target != null)
                    client.Send(scan, scan.Length, new IPEndPoint(target, LanPort));
                else
                {
                    client.Send(scan, scan.Length, new IPEndPoint(IPAddress.Broadcast, LanPort));
                    foreach (IPAddress local in AmbiBack.PhilipsHue.HueDiscovery.LocalIPv4Addresses())
                    {
                        // Directed broadcast of every /24 network, for PCs with several networks
                        byte[] b = local.GetAddressBytes();
                        b[3] = 255;
                        try { client.Send(scan, scan.Length, new IPEndPoint(new IPAddress(b), LanPort)); }
                        catch (SocketException) { }
                    }
                }

                var clock = Stopwatch.StartNew();
                while (clock.ElapsedMilliseconds < timeoutMs)
                {
                    client.Client.ReceiveTimeout = Math.Max(1, timeoutMs - (int)clock.ElapsedMilliseconds);
                    IPEndPoint from = null;
                    byte[] reply;
                    try { reply = client.Receive(ref from); }
                    catch (SocketException) { break; }
                    if (reply.Length < HeaderLength + 5 || MessageType(reply) != MsgStateService || reply[HeaderLength] != 1)
                        continue;
                    string mac = MacText(reply);
                    if (found.Exists(f => f.Mac == mac))
                        continue;
                    found.Add(new LifxInfo { Address = from.Address, Mac = mac });
                }

                // Details of each light (unicast, one after the other)
                byte seq = 2;
                foreach (LifxInfo info in found)
                {
                    byte[] t = MacTarget(info.Mac);
                    byte[] label = Ask(client, info.Address, BuildMessage(MsgGetLabel, new byte[0], t, false, true, seq++, src), MsgStateLabel, info.Mac);
                    if (label != null)
                        info.Label = Encoding.UTF8.GetString(label, 0, Math.Min(32, label.Length)).TrimEnd('\0');
                    byte[] version = Ask(client, info.Address, BuildMessage(MsgGetVersion, new byte[0], t, false, true, seq++, src), MsgStateVersion, info.Mac);
                    if (version != null && version.Length >= 8)
                        info.Product = (int)BitConverter.ToUInt32(version, 4);
                    byte[] zones = Ask(client, info.Address, BuildMessage(MsgGetExtendedColorZones, new byte[0], t, false, true, seq++, src), MsgStateExtendedColorZones, info.Mac);
                    if (zones != null && zones.Length >= 2)
                        info.Zones = BitConverter.ToUInt16(zones, 0);
                }
            }
            return found;
        }

        /// <summary>Sends a request and returns the payload of the answer from that light, null after the timeout.</summary>
        private static byte[] Ask(UdpClient client, IPAddress ip, byte[] request, ushort answerType, string mac)
        {
            client.Send(request, request.Length, new IPEndPoint(ip, LanPort));
            var clock = Stopwatch.StartNew();
            while (clock.ElapsedMilliseconds < ReplyTimeoutMs)
            {
                client.Client.ReceiveTimeout = Math.Max(1, ReplyTimeoutMs - (int)clock.ElapsedMilliseconds);
                IPEndPoint from = null;
                byte[] reply;
                try { reply = client.Receive(ref from); }
                catch (SocketException) { return null; }
                if (reply.Length >= HeaderLength && MessageType(reply) == answerType && MacText(reply) == mac)
                    return Payload(reply);
            }
            return null;
        }

        /// <summary>New device from a discovered light: label as name, MAC for the id, a layout for its zones.</summary>
        public static CDeviceLifx FromInfo(LifxInfo info)
        {
            var device = new CDeviceLifx(string.IsNullOrEmpty(info.Label) ? "LIFX " + info.Mac.Substring(6) : info.Label, info.Address)
            {
                InternalNetworkMAC = info.Mac,
                ProductId = info.Product,
                ZoneCount = info.Zones,
                ReceiverLedCount = Math.Max(1, info.Zones),
                ReceiverInfo = "LIFX product " + info.Product + (info.Zones > 1 ? ", " + info.Zones + " zones" : ""),
            };
            device.Mode = device.IsMultizone ? ModeStrip : ModeSingleColor;
            if (device.IsMultizone)
                device.SuggestLayout(info.Zones);
            device.SetDeviceID();
            device.SetIsOnline(true);
            return device;
        }

        /// <summary>
        /// Command line test (--lifx-test [host] [seconds] [single]): finds the lights (all, or the one at
        /// host), then streams a moving test picture to the first multizone light, else the first light (with
        /// "single": the first light in one color). True when frames were sent.
        /// </summary>
        public static bool RunStreamTest(string host, int seconds, bool singleColor, Action<string> output)
        {
            IPAddress ip = null;
            if (!string.IsNullOrEmpty(host))
            {
                ip = ResolveHost(host, output);
                if (ip == null)
                    return false;
            }
            List<LifxInfo> lights = Discover(1500, ip);
            foreach (LifxInfo l in lights)
                output(l.Address + "  " + l.Mac + "  \"" + l.Label + "\"  product " + l.Product + (l.Zones > 1 ? ", " + l.Zones + " zones" : ", one color"));
            if (lights.Count == 0)
            {
                output("No LIFX light answered" + (ip != null ? " at " + ip : "") + ".");
                return false;
            }
            LifxInfo chosen = singleColor ? lights[0] : (lights.Find(l => l.Zones > 1) ?? lights[0]);
            CDeviceLifx device = FromInfo(chosen);
            if (singleColor)
                device.Mode = ModeSingleColor;
            output("Streaming to \"" + device.GetDeviceName() + "\", " + (device.UsesStripLayout ? "zones, layout " + device.PixelHori + " x " + device.PixelVert : "one color"));
            int framesReported = StreamTestPicture(device, seconds, output);
            output(framesReported > 0 ? "Done, the light is back to its previous color." : "No frames were sent.");
            return framesReported > 0;
        }
        #endregion
    }
}
