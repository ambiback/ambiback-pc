using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace AmbiBack
{
    /// <summary>
    /// One RGB controller of an OpenRGB SDK server (TCP port 6742): motherboard, RAM, graphics card, LED
    /// strip, keyboard, fans … OpenRGB must run with its SDK server started. The controller is switched to
    /// direct ("custom") mode on start and gets all its LED colors with every frame.
    ///
    /// Two modes:
    /// - Strip: the controller's LEDs, in OpenRGB's order, run around the screen like an AmbiBack strip.
    /// - One color: every LED gets the average screen color (fans, RAM, keyboard, case lights).
    ///
    /// The controller is found again by name, location and serial, because its index changes when
    /// OpenRGB detects devices in another order.
    /// </summary>
    [Serializable]
    public class CDeviceOpenRgb : CDeviceLedStrip
    {
        public const int SdkPort = 6742;
        /// <summary>Highest protocol version this client parses (zones with segments, OpenRGB 0.9).</summary>
        public const uint ClientProtocolVersion = 4;

        public const byte ModeStrip = 0;
        public const byte ModeSingleColor = 1;
        /// <summary>OpenRGB device type of LED strips; they start in strip mode, all others in one color.</summary>
        public const int DeviceTypeLedStrip = 4;

        public const uint PacketRequestControllerCount = 0;
        public const uint PacketRequestControllerData = 1;
        public const uint PacketRequestProtocolVersion = 40;
        public const uint PacketSetClientName = 50;
        public const uint PacketDeviceListUpdated = 100;
        public const uint PacketUpdateLeds = 1050;
        public const uint PacketSetCustomMode = 1100;

        private const int HeaderLength = 16;
        private const int TimeoutMs = 2000;
        private static readonly byte[] Magic = { (byte)'O', (byte)'R', (byte)'G', (byte)'B' };

        private TcpClient tcp;
        private NetworkStream stream;
        private uint controllerIndex;
        private int ledCount;
        private byte[] ledPacket;

        #region Stored settings
        /// <summary>Name, location and serial as OpenRGB reports them, to find the controller again.</summary>
        public string ControllerName { get; set; } = "";
        public string ControllerLocation { get; set; } = "";
        public string ControllerSerial { get; set; } = "";
        /// <summary>Index at the last connection; used when the controller cannot be matched otherwise.</summary>
        public int ControllerIndex { get; set; } = 0;
        #endregion

        public CDeviceOpenRgb()
        {
            this.SetDeviceType(DeviceType.OpenRgb);
            this.SetDeviceName("OpenRGB");
            this.InitClass();
        }

        public CDeviceOpenRgb(string szDeviceName, IPAddress ipAddress)
        {
            this.SetDeviceType(DeviceType.OpenRgb);
            this.SetDeviceName(szDeviceName);
            this.SetIPAddress(ipAddress);
            this.InitClass();
        }

        protected override int DefaultPort { get { return SdkPort; } }
        protected override byte DefaultFramesPerSecond { get { return 30; } }
        public override string[] ModeTextKeys { get { return new[] { "openrgb_mode_strip", "openrgb_mode_single" }; } }

        protected override void WriteStripXml(XmlWriter writer)
        {
            this.WriteXmlElement(writer, "Mode", this.Mode);
            writer.WriteElementString("ControllerName", this.ControllerName ?? "");
            writer.WriteElementString("ControllerLocation", this.ControllerLocation ?? "");
            writer.WriteElementString("ControllerSerial", this.ControllerSerial ?? "");
            this.WriteXmlElement(writer, "ControllerIndex", this.ControllerIndex);
        }

        protected override void ReadStripXml(XmlReader reader)
        {
            this.Mode = this.ReadXmlElementAsByte(reader, "Mode", ModeStrip);
            if (this.Mode > ModeSingleColor)
                this.Mode = ModeStrip;
            this.ControllerName = reader.ReadElementContentAsString("ControllerName", "");
            this.ControllerLocation = reader.ReadElementContentAsString("ControllerLocation", "");
            this.ControllerSerial = reader.ReadElementContentAsString("ControllerSerial", "");
            this.ControllerIndex = this.ReadXmlElementAsInt(reader, "ControllerIndex", 0);
        }

        public override void UpdateFrom(CDevice devCopyFrom)
        {
            base.UpdateFrom(devCopyFrom);
            // The index can change between OpenRGB sessions
            this.ControllerIndex = ((CDeviceOpenRgb)devCopyFrom).ControllerIndex;
        }

        #region Connection and sending
        protected override void Connect(IPAddress ip)
        {
            Session session = Session.Open(ip, this.Port);
            try
            {
                List<ControllerInfo> controllers = session.ReadControllers();
                ControllerInfo match = FindController(controllers);
                if (match == null)
                    throw new IOException("OpenRGB has no controller \"" + this.ControllerName + "\"");
                controllerIndex = (uint)match.Index;
                this.ControllerIndex = match.Index;
                ledCount = match.LedCount;
                this.ReceiverLedCount = match.LedCount;
                tcp = session.Client;
                stream = session.Stream;
                ledPacket = null;
            }
            catch
            {
                session.Close();
                throw;
            }
        }

        /// <summary>Same name, location and serial; else the same name at the stored index; else the only one with that name.</summary>
        private ControllerInfo FindController(List<ControllerInfo> controllers)
        {
            ControllerInfo exact = controllers.Find(c => c.Name == this.ControllerName && c.Location == this.ControllerLocation && c.Serial == this.ControllerSerial);
            if (exact != null)
                return exact;
            ControllerInfo atIndex = controllers.Find(c => c.Index == this.ControllerIndex && c.Name == this.ControllerName);
            if (atIndex != null)
                return atIndex;
            List<ControllerInfo> sameName = controllers.FindAll(c => c.Name == this.ControllerName);
            return sameName.Count == 1 ? sameName[0] : null;
        }

        protected override void Disconnect()
        {
            try
            {
                stream?.Close();
                tcp?.Close();
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
            stream = null;
            tcp = null;
        }

        protected override void OnStreamStart()
        {
            // Direct mode: the controller shows the colors it gets
            Write(BuildPacket(controllerIndex, PacketSetCustomMode, new byte[0]));
        }

        protected override void SendFrame(byte[] colors, int bytes)
        {
            DrainIncoming();
            if (ledCount <= 0)
                return;
            if (ledPacket == null || ledPacket.Length != HeaderLength + 6 + 4 * ledCount)
                ledPacket = new byte[HeaderLength + 6 + 4 * ledCount];
            FillUpdateLeds(ledPacket, controllerIndex, colors, bytes / 3, ledCount, this.Mode == ModeSingleColor);
            Write(ledPacket);
        }

        /// <summary>Reads what the server sent meanwhile; a changed device list means reconnecting.</summary>
        private void DrainIncoming()
        {
            while (stream.DataAvailable)
            {
                Packet p = ReadPacket(stream);
                if (p.Id == PacketDeviceListUpdated)
                    throw new IOException("OpenRGB device list changed");
            }
        }

        private void Write(byte[] packet)
        {
            stream.Write(packet, 0, packet.Length);
        }
        #endregion

        #region Protocol
        /// <summary>Header (magic "ORGB", device index, packet id, data length, little endian) plus data.</summary>
        public static byte[] BuildPacket(uint deviceIndex, uint packetId, byte[] data)
        {
            byte[] p = new byte[HeaderLength + data.Length];
            WriteHeader(p, deviceIndex, packetId, (uint)data.Length);
            Buffer.BlockCopy(data, 0, p, HeaderLength, data.Length);
            return p;
        }

        private static void WriteHeader(byte[] p, uint deviceIndex, uint packetId, uint length)
        {
            Buffer.BlockCopy(Magic, 0, p, 0, 4);
            WriteUInt32(p, 4, deviceIndex);
            WriteUInt32(p, 8, packetId);
            WriteUInt32(p, 12, length);
        }

        /// <summary>
        /// UPDATELEDS into <paramref name="packet"/> (header + u32 data size + u16 color count + 4 bytes per
        /// LED: R, G, B, 0). Strip: LED i gets layout color i, LEDs beyond the layout stay dark. One color:
        /// every LED gets the average.
        /// </summary>
        public static void FillUpdateLeds(byte[] packet, uint deviceIndex, byte[] colors, int colorCount, int leds, bool singleColor)
        {
            int dataSize = 4 + 2 + 4 * leds;
            WriteHeader(packet, deviceIndex, PacketUpdateLeds, (uint)dataSize);
            WriteUInt32(packet, HeaderLength, (uint)dataSize);
            packet[HeaderLength + 4] = (byte)leds;
            packet[HeaderLength + 5] = (byte)(leds >> 8);
            int avg = singleColor ? AverageColor(colors, colorCount) : 0;
            int o = HeaderLength + 6;
            for (int i = 0; i < leds; i++, o += 4)
            {
                if (singleColor)
                {
                    packet[o] = (byte)(avg >> 16);
                    packet[o + 1] = (byte)(avg >> 8);
                    packet[o + 2] = (byte)avg;
                }
                else if (i < colorCount)
                {
                    packet[o] = colors[i * 3];
                    packet[o + 1] = colors[i * 3 + 1];
                    packet[o + 2] = colors[i * 3 + 2];
                }
                else
                {
                    packet[o] = packet[o + 1] = packet[o + 2] = 0;
                }
                packet[o + 3] = 0;
            }
        }

        private static void WriteUInt32(byte[] p, int offset, uint value)
        {
            p[offset] = (byte)value;
            p[offset + 1] = (byte)(value >> 8);
            p[offset + 2] = (byte)(value >> 16);
            p[offset + 3] = (byte)(value >> 24);
        }

        private struct Packet
        {
            public uint DeviceIndex;
            public uint Id;
            public byte[] Data;
        }

        private static Packet ReadPacket(NetworkStream stream)
        {
            byte[] header = ReadExact(stream, HeaderLength);
            if (header[0] != Magic[0] || header[1] != Magic[1] || header[2] != Magic[2] || header[3] != Magic[3])
                throw new IOException("Not an OpenRGB SDK answer");
            uint length = BitConverter.ToUInt32(header, 12);
            if (length > 8 * 1024 * 1024)
                throw new IOException("OpenRGB packet too large");
            return new Packet
            {
                DeviceIndex = BitConverter.ToUInt32(header, 4),
                Id = BitConverter.ToUInt32(header, 8),
                Data = ReadExact(stream, (int)length),
            };
        }

        private static byte[] ReadExact(NetworkStream stream, int length)
        {
            byte[] data = new byte[length];
            int read = 0;
            while (read < length)
            {
                int n = stream.Read(data, read, length - read);
                if (n <= 0)
                    throw new IOException("OpenRGB closed the connection");
                read += n;
            }
            return data;
        }

        /// <summary>What a controller reports about itself (the part AmbiBack needs).</summary>
        public class ControllerInfo
        {
            public int Index;
            public int Type;
            public string Name = "";
            public string Vendor = "";
            public string Description = "";
            public string Serial = "";
            public string Location = "";
            public int LedCount;
            public List<string> Zones = new List<string>();
        }

        /// <summary>
        /// Parses the controller data of REQUEST_CONTROLLER_DATA for the given protocol version: type,
        /// strings, modes (skipped), zones (name and LED count), LEDs (counted).
        /// </summary>
        public static ControllerInfo ParseControllerData(byte[] data, uint version)
        {
            var r = new Reader(data);
            var info = new ControllerInfo();
            r.UInt32();                         // data size
            info.Type = r.Int32();
            info.Name = r.String();
            if (version >= 1)
                info.Vendor = r.String();
            info.Description = r.String();
            r.String();                         // version
            info.Serial = r.String();
            info.Location = r.String();
            int modes = r.UInt16();
            r.Int32();                          // active mode
            for (int m = 0; m < modes; m++)
            {
                r.String();                     // name
                r.Skip(4 * 4);                  // value, flags, speed min, speed max
                if (version >= 3)
                    r.Skip(2 * 4);              // brightness min, max
                r.Skip(3 * 4);                  // colors min, max, speed
                if (version >= 3)
                    r.Skip(4);                  // brightness
                r.Skip(2 * 4);                  // direction, color mode
                r.Skip(4 * r.UInt16());         // colors
            }
            int zones = r.UInt16();
            for (int z = 0; z < zones; z++)
            {
                string name = r.String();
                r.Skip(4 * 3);                  // type, leds min, leds max
                int count = (int)r.UInt32();
                r.Skip(r.UInt16());             // matrix map (length in bytes)
                if (version >= 4)
                {
                    int segments = r.UInt16();
                    for (int s = 0; s < segments; s++)
                    {
                        r.String();
                        r.Skip(4 * 3);          // type, start, count
                    }
                }
                info.Zones.Add(name + " (" + count + ")");
            }
            info.LedCount = r.UInt16();
            return info;
        }

        private class Reader
        {
            private readonly byte[] data;
            private int pos;

            public Reader(byte[] data) { this.data = data; }

            private void Need(int n)
            {
                if (pos + n > data.Length)
                    throw new IOException("OpenRGB controller data is shorter than expected");
            }
            public uint UInt32() { Need(4); uint v = BitConverter.ToUInt32(data, pos); pos += 4; return v; }
            public int Int32() { Need(4); int v = BitConverter.ToInt32(data, pos); pos += 4; return v; }
            public int UInt16() { Need(2); int v = BitConverter.ToUInt16(data, pos); pos += 2; return v; }
            public void Skip(int n) { Need(n); pos += n; }
            /// <summary>u16 length (with the terminating zero) and the characters.</summary>
            public string String()
            {
                int len = UInt16();
                Need(len);
                string s = Encoding.UTF8.GetString(data, pos, len).TrimEnd('\0');
                pos += len;
                return s;
            }
        }

        /// <summary>An open SDK connection with the protocol version agreed on.</summary>
        private class Session
        {
            public TcpClient Client;
            public NetworkStream Stream;
            public uint Version;

            public static Session Open(IPAddress ip, int port)
            {
                var s = new Session();
                s.Connect(ip, port);
                try
                {
                    s.Write(BuildPacket(0, PacketRequestProtocolVersion, BitConverter.GetBytes(ClientProtocolVersion)));
                    Packet answer = s.ReadAnswer(PacketRequestProtocolVersion);
                    uint server = answer.Data.Length >= 4 ? BitConverter.ToUInt32(answer.Data, 0) : 0;
                    s.Version = Math.Min(server, ClientProtocolVersion);
                }
                catch (IOException)
                {
                    // Protocol 0 servers (OpenRGB 0.3) do not answer: connect again without asking
                    s.Close();
                    s.Connect(ip, port);
                    s.Version = 0;
                }
                s.Write(BuildPacket(0, PacketSetClientName, Encoding.UTF8.GetBytes("AmbiBack PC\0")));
                return s;
            }

            private void Connect(IPAddress ip, int port)
            {
                Client = new TcpClient(ip.AddressFamily) { NoDelay = true, ReceiveTimeout = TimeoutMs, SendTimeout = TimeoutMs };
                IAsyncResult connecting = Client.BeginConnect(ip, port, null, null);
                if (!connecting.AsyncWaitHandle.WaitOne(TimeoutMs))
                {
                    Client.Close();
                    throw new SocketException((int)SocketError.TimedOut);
                }
                Client.EndConnect(connecting);
                Stream = Client.GetStream();
            }

            public void Write(byte[] packet)
            {
                Stream.Write(packet, 0, packet.Length);
            }

            /// <summary>The next answer with this id; notifications in between are skipped.</summary>
            public Packet ReadAnswer(uint id)
            {
                while (true)
                {
                    Packet p = ReadPacket(Stream);
                    if (p.Id == id)
                        return p;
                }
            }

            public List<ControllerInfo> ReadControllers()
            {
                Write(BuildPacket(0, PacketRequestControllerCount, new byte[0]));
                Packet countAnswer = ReadAnswer(PacketRequestControllerCount);
                uint count = countAnswer.Data.Length >= 4 ? BitConverter.ToUInt32(countAnswer.Data, 0) : 0;
                var result = new List<ControllerInfo>();
                for (uint i = 0; i < count && i < 256; i++)
                {
                    byte[] payload = Version >= 1 ? BitConverter.GetBytes(Version) : new byte[0];
                    Write(BuildPacket(i, PacketRequestControllerData, payload));
                    Packet p = ReadAnswer(PacketRequestControllerData);
                    ControllerInfo info = ParseControllerData(p.Data, Version);
                    info.Index = (int)i;
                    result.Add(info);
                }
                return result;
            }

            public void Close()
            {
                try
                {
                    Stream?.Close();
                    Client?.Close();
                }
                catch (Exception ex)
                {
                    CGlobals.DebugOutput(ex);
                }
            }
        }
        #endregion

        #region Discovery
        /// <summary>The controllers of the OpenRGB SDK server at this address; empty when none answers.</summary>
        public static List<ControllerInfo> QueryControllers(IPAddress ip, int port = SdkPort)
        {
            Session session;
            try
            {
                session = Session.Open(ip, port);
            }
            catch (Exception ex) when (ex is SocketException || ex is IOException)
            {
                return new List<ControllerInfo>();
            }
            try
            {
                return session.ReadControllers();
            }
            catch (Exception ex) when (ex is SocketException || ex is IOException)
            {
                CGlobals.DebugOutput("OpenRGB at " + ip + ": " + ex.Message);
                return new List<ControllerInfo>();
            }
            finally
            {
                session.Close();
            }
        }

        /// <summary>New device for a controller: its name, and an id from name, location and serial.</summary>
        public static CDeviceOpenRgb FromInfo(ControllerInfo info, IPAddress ip)
        {
            var device = new CDeviceOpenRgb(info.Name, ip)
            {
                ControllerName = info.Name,
                ControllerLocation = info.Location,
                ControllerSerial = info.Serial,
                ControllerIndex = info.Index,
                ReceiverLedCount = info.LedCount,
                ReceiverInfo = (string.IsNullOrEmpty(info.Vendor) ? "" : info.Vendor + ": ") + string.Join(", ", info.Zones),
                InternalNetworkMAC = StableId(info),
                Mode = info.Type == DeviceTypeLedStrip ? ModeStrip : ModeSingleColor,
            };
            device.SuggestLayout(info.LedCount);
            device.SetDeviceID();
            device.SetIsOnline(true);
            return device;
        }

        /// <summary>Short hash of name, location and serial: the same controller gets the same id.</summary>
        public static string StableId(ControllerInfo info)
        {
            using (var sha = SHA1.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(info.Name + "|" + info.Location + "|" + info.Serial));
                var sb = new StringBuilder();
                for (int i = 0; i < 6; i++)
                    sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>
        /// Command line test (--openrgb-test host [controller] [seconds] [single]): lists the controllers,
        /// then streams a moving test picture to one of them (index, default 0). True when frames were sent.
        /// </summary>
        public static bool RunStreamTest(string host, int controller, int seconds, bool singleColor, Action<string> output)
        {
            IPAddress ip = ResolveHost(host, output);
            if (ip == null)
                return false;
            List<ControllerInfo> controllers = QueryControllers(ip);
            if (controllers.Count == 0)
            {
                output("No OpenRGB SDK server with controllers at " + ip + ":" + SdkPort + " (is the SDK server started?)");
                return false;
            }
            foreach (ControllerInfo c in controllers)
                output("[" + c.Index + "] " + c.Name + " (" + c.Vendor + "), type " + c.Type + ", " + c.LedCount + " LEDs: " + string.Join(", ", c.Zones));
            if (controller < 0 || controller >= controllers.Count)
            {
                output("No controller " + controller);
                return false;
            }
            CDeviceOpenRgb device = FromInfo(controllers[controller], ip);
            device.Mode = singleColor ? ModeSingleColor : ModeStrip;
            output("Streaming to [" + controller + "] " + device.ControllerName + ", " + (singleColor ? "one color" : "strip layout " + device.PixelHori + " x " + device.PixelVert));
            int framesReported = StreamTestPicture(device, seconds, output);
            output(framesReported > 0 ? "Done, the controller keeps the last colors." : "No frames were sent.");
            return framesReported > 0;
        }
        #endregion
    }
}
