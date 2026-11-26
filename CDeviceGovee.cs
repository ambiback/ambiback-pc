using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using Newtonsoft.Json.Linq;

namespace AmbiBack
{
    /// <summary>
    /// Govee Wi-Fi light controlled over the Govee LAN API (UDP; "LAN Control" must be switched on for the
    /// device in the Govee Home app). Discovery: a "scan" request to the multicast group 239.255.255.250
    /// port 4001, the devices answer to port 4002. Commands go to the device on port 4003.
    ///
    /// Two modes:
    /// - Segments: the "razer" command (the protocol behind Govee's DreamView / Razer Chroma support) takes
    ///   up to 255 colors per frame, which the device spreads over its segments. Not every model supports it.
    /// - Single color: the documented "colorwc" command with the average screen color; works on every model
    ///   with LAN Control, but is meant for occasional changes, so it is sent at most 10 times per second.
    /// </summary>
    [Serializable]
    public class CDeviceGovee : CDeviceLedStripUdp
    {
        public const int ScanPort = 4001;
        public const int ResponsePort = 4002;
        public const int ControlPort = 4003;
        public static readonly IPAddress MulticastGroup = IPAddress.Parse("239.255.255.250");

        public const byte ModeSegments = 0;
        public const byte ModeSingleColor = 1;
        /// <summary>Colors per razer frame (the count is one byte).</summary>
        public const int MaxSegments = 255;

        /// <summary>The device leaves razer mode after about a minute without data; enable it again regularly.</summary>
        private const int RazerEnableIntervalMs = 20000;
        private const int SingleColorMinIntervalMs = 100;
        private const int SingleColorRefreshMs = 1000;

        private readonly Stopwatch sinceEnable = new Stopwatch();
        private readonly Stopwatch sinceColor = new Stopwatch();
        private int lastColor = -1;
        /// <summary>Mode the stream was started with; the mode can be changed while streaming.</summary>
        private byte activeMode;

        public CDeviceGovee()
        {
            this.SetDeviceType(DeviceType.GoveeLan);
            this.SetDeviceName("Govee");
            this.InitClass();
        }

        public CDeviceGovee(string szDeviceName, IPAddress ipAddress)
        {
            this.SetDeviceType(DeviceType.GoveeLan);
            this.SetDeviceName(szDeviceName);
            this.SetIPAddress(ipAddress);
            this.InitClass();
        }

        protected override int DefaultPort { get { return ControlPort; } }
        /// <summary><see cref="ModeSegments"/> or <see cref="ModeSingleColor"/>.</summary>
        public override string[] ModeTextKeys { get { return new[] { "govee_mode_segments", "govee_mode_single" }; } }
        protected override byte DefaultFramesPerSecond { get { return 30; } }
        // 16 segments around the screen; set it to the segments of the device
        protected override byte DefaultPixelHori { get { return 5; } }
        protected override byte DefaultPixelVert { get { return 3; } }

        protected override void WriteStripXml(XmlWriter writer)
        {
            this.WriteXmlElement(writer, "Mode", this.Mode);
        }

        protected override void ReadStripXml(XmlReader reader)
        {
            this.Mode = this.ReadXmlElementAsByte(reader, "Mode", ModeSegments);
            if (this.Mode > ModeSingleColor)
                this.Mode = ModeSegments;
        }

        #region Sending
        protected override void OnStreamStart()
        {
            Send(TurnCommand(true));
            StartMode(this.Mode);
        }

        protected override void OnStreamStop()
        {
            // Back to the scene or color set in the Govee app; single color keeps the last color
            if (activeMode == ModeSegments)
                Send(RazerCommand(RazerEnablePacket(false)));
        }

        private void StartMode(byte mode)
        {
            activeMode = mode;
            if (mode == ModeSegments)
            {
                Send(RazerCommand(RazerEnablePacket(true)));
                sinceEnable.Restart();
            }
            lastColor = -1;
            sinceColor.Reset();
        }

        protected override void SendFrame(byte[] colors, int bytes)
        {
            byte mode = this.Mode;
            if (mode != activeMode)
            {
                // Changed in the settings while streaming
                if (activeMode == ModeSegments)
                    Send(RazerCommand(RazerEnablePacket(false)));
                StartMode(mode);
            }
            if (mode == ModeSingleColor)
            {
                SendSingleColor(colors, bytes);
                return;
            }
            if (sinceEnable.ElapsedMilliseconds >= RazerEnableIntervalMs)
            {
                Send(RazerCommand(RazerEnablePacket(true)));
                sinceEnable.Restart();
            }
            Send(RazerCommand(RazerColorPacket(colors, bytes / 3)));
        }

        private void SendSingleColor(byte[] colors, int bytes)
        {
            int color = AverageColor(colors, bytes / 3);
            bool due = !sinceColor.IsRunning
                || (color != lastColor && sinceColor.ElapsedMilliseconds >= SingleColorMinIntervalMs)
                || sinceColor.ElapsedMilliseconds >= SingleColorRefreshMs;
            if (!due)
                return;
            Send(ColorCommand((byte)(color >> 16), (byte)(color >> 8), (byte)color));
            lastColor = color;
            sinceColor.Restart();
        }

        private void Send(string json)
        {
            SendDatagram(Encoding.UTF8.GetBytes(json));
        }
        #endregion

        #region Protocol
        /// <summary>Razer mode on or off: BB 00 01 B1 [on] [xor].</summary>
        public static byte[] RazerEnablePacket(bool on)
        {
            return WithChecksum(new byte[] { 0xBB, 0x00, 0x01, 0xB1, (byte)(on ? 1 : 0) });
        }

        /// <summary>
        /// Colors for razer mode: BB [length hi] [length lo] B0 [gradient 0] [count] RGB*count [xor], the
        /// length counting the gradient byte, the count byte and the RGB bytes (2 + 3 * count). At most
        /// <see cref="MaxSegments"/> colors; further LEDs of the layout are left out.
        /// </summary>
        public static byte[] RazerColorPacket(byte[] colors, int count)
        {
            count = Math.Max(0, Math.Min(MaxSegments, count));
            int payload = 2 + 3 * count;
            byte[] p = new byte[4 + payload + 1];
            p[0] = 0xBB;
            p[1] = (byte)(payload >> 8);
            p[2] = (byte)payload;
            p[3] = 0xB0;
            p[4] = 0x00;            // no gradient: show the colors as they are
            p[5] = (byte)count;
            Buffer.BlockCopy(colors, 0, p, 6, count * 3);
            p[p.Length - 1] = Xor(p, p.Length - 1);
            return p;
        }

        private static byte[] WithChecksum(byte[] data)
        {
            byte[] p = new byte[data.Length + 1];
            Buffer.BlockCopy(data, 0, p, 0, data.Length);
            p[data.Length] = Xor(data, data.Length);
            return p;
        }

        private static byte Xor(byte[] data, int length)
        {
            byte x = 0;
            for (int i = 0; i < length; i++)
                x ^= data[i];
            return x;
        }

        public static string RazerCommand(byte[] packet)
        {
            return "{\"msg\":{\"cmd\":\"razer\",\"data\":{\"pt\":\"" + Convert.ToBase64String(packet) + "\"}}}";
        }

        public static string TurnCommand(bool on)
        {
            return "{\"msg\":{\"cmd\":\"turn\",\"data\":{\"value\":" + (on ? 1 : 0) + "}}}";
        }

        public static string ColorCommand(byte r, byte g, byte b)
        {
            return "{\"msg\":{\"cmd\":\"colorwc\",\"data\":{\"color\":{\"r\":" + r + ",\"g\":" + g + ",\"b\":" + b
                + "},\"colorTemInKelvin\":0}}}";
        }

        public const string ScanCommand = "{\"msg\":{\"cmd\":\"scan\",\"data\":{\"account_topic\":\"reserve\"}}}";

        #endregion

        #region Discovery
        /// <summary>What a Govee device answers to the scan request.</summary>
        public class GoveeInfo
        {
            public IPAddress Address;
            /// <summary>Device id, e.g. "1F:80:C5:32:32:36:72:4E".</summary>
            public string DeviceId;
            /// <summary>Model, e.g. "H6199".</summary>
            public string Sku;
            public string WifiVersion;
            public string BleVersion;
        }

        /// <summary>Parses a scan answer; null when it is not one.</summary>
        public static GoveeInfo ParseScanResponse(string json)
        {
            JObject msg = JObject.Parse(json)["msg"] as JObject;
            if (msg == null || (string)msg["cmd"] != "scan")
                return null;
            JObject data = msg["data"] as JObject;
            IPAddress ip;
            if (data == null || !IPAddress.TryParse((string)data["ip"] ?? "", out ip))
                return null;
            return new GoveeInfo
            {
                Address = ip,
                DeviceId = (string)data["device"] ?? "",
                Sku = (string)data["sku"] ?? "",
                WifiVersion = (string)data["wifiVersionSoft"] ?? "",
                BleVersion = (string)data["bleVersionSoft"] ?? "",
            };
        }

        /// <summary>
        /// Govee devices with LAN Control that answer the scan request: sent to the multicast group on every
        /// network interface, and to <paramref name="target"/> directly when given. Answers arrive on port 4002.
        /// </summary>
        public static List<GoveeInfo> Discover(int timeoutMs = 2000, IPAddress target = null)
        {
            var result = new List<GoveeInfo>();
            byte[] scan = Encoding.UTF8.GetBytes(ScanCommand);
            using (var client = new UdpClient(AddressFamily.InterNetwork))
            {
                // Port 4002 may be used by another Govee tool on this PC as well
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.Client.Bind(new IPEndPoint(IPAddress.Any, ResponsePort));

                var groupEndPoint = new IPEndPoint(MulticastGroup, ScanPort);
                foreach (IPAddress local in AmbiBack.PhilipsHue.HueDiscovery.LocalIPv4Addresses())
                {
                    try
                    {
                        client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, local.GetAddressBytes());
                        client.Send(scan, scan.Length, groupEndPoint);
                    }
                    catch (SocketException ex)
                    {
                        CGlobals.DebugOutput("Govee scan on " + local + ": " + ex.SocketErrorCode);
                    }
                }
                if (target != null)
                    client.Send(scan, scan.Length, new IPEndPoint(target, ScanPort));

                var clock = Stopwatch.StartNew();
                while (clock.ElapsedMilliseconds < timeoutMs)
                {
                    client.Client.ReceiveTimeout = Math.Max(1, timeoutMs - (int)clock.ElapsedMilliseconds);
                    byte[] data;
                    try
                    {
                        IPEndPoint from = null;
                        data = client.Receive(ref from);
                    }
                    catch (SocketException)
                    {
                        break;      // timeout
                    }
                    GoveeInfo info;
                    try
                    {
                        info = ParseScanResponse(Encoding.UTF8.GetString(data));
                    }
                    catch (Newtonsoft.Json.JsonException)
                    {
                        continue;
                    }
                    if (info == null || result.Exists(i => i.DeviceId == info.DeviceId && i.Address.Equals(info.Address)))
                        continue;
                    result.Add(info);
                    if (target != null && info.Address.Equals(target))
                        break;
                }
            }
            return result;
        }

        /// <summary>The device at this address, null when it does not answer (no Govee, or LAN Control off).</summary>
        public static GoveeInfo QueryInfo(IPAddress ip, int timeoutMs = 2000)
        {
            return Discover(timeoutMs, ip).Find(i => i.Address.Equals(ip));
        }

        /// <summary>New device from a scan answer: model as name, device id for the id.</summary>
        public static CDeviceGovee FromInfo(GoveeInfo info)
        {
            string id = (info.DeviceId ?? "").Replace(":", "").ToLowerInvariant();
            var device = new CDeviceGovee("Govee " + info.Sku, info.Address)
            {
                ReceiverInfo = (info.Sku + (string.IsNullOrEmpty(info.WifiVersion) ? "" : ", Wi-Fi " + info.WifiVersion)).Trim(),
                InternalNetworkMAC = id.Length > 0 ? id : null,
            };
            device.SetDeviceID();
            device.SetIsOnline(true);
            return device;
        }

        /// <summary>
        /// Command line test (--govee-test host [seconds] [single]): asks the device for its scan answer,
        /// then streams a moving test picture through the normal device thread. True when frames were sent.
        /// </summary>
        public static bool RunStreamTest(string host, int seconds, bool singleColor, Action<string> output)
        {
            IPAddress ip = ResolveHost(host, output);
            if (ip == null)
                return false;

            GoveeInfo info = QueryInfo(ip);
            CDeviceGovee device;
            if (info != null)
            {
                device = FromInfo(info);
                output("Govee " + info.Sku + " (id " + info.DeviceId + ", Wi-Fi " + info.WifiVersion + ") at " + info.Address);
            }
            else
            {
                device = new CDeviceGovee(host, ip);
                output("No scan answer from " + ip + " (is LAN Control on in the Govee Home app?), sending anyway");
            }
            device.Mode = singleColor ? ModeSingleColor : ModeSegments;
            output(singleColor ? "Mode: single color" : "Mode: segments, " + device.PixelTotal + " colors (layout " + device.PixelHori + " x " + device.PixelVert + ")");

            int framesReported = StreamTestPicture(device, seconds, output);
            output(framesReported > 0 ? "Done." : "No frames were sent.");
            return framesReported > 0;
        }
        #endregion
    }
}
