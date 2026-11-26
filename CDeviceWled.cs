using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json.Linq;

namespace AmbiBack
{
    /// <summary>
    /// LED strip driven over DDP (Distributed Display Protocol, UDP port 4048): WLED, ESPHome, Hyperion
    /// receivers, ESPixelStick and other DDP receivers. WLED switches to live mode on the first packet and
    /// returns to its own effect about 2.5 s after the last one, so stopping needs no extra command.
    /// Layout, colors and smoothing are handled by <see cref="CDeviceLedStrip"/>.
    /// </summary>
    [Serializable]
    public class CDeviceWled : CDeviceLedStripUdp
    {
        public const int DdpPort = 4048;
        public const int DdpHeaderLength = 10;
        /// <summary>RGB bytes per packet: 480 LEDs, the size WLED sends and expects.</summary>
        public const int DdpMaxDataPerPacket = 1440;

        private const byte DdpFlagsVersion1 = 0x40;
        private const byte DdpFlagPush = 0x01;
        private const byte DdpTypeRgb8 = 0x0B;        // RGB, 8 bit per channel
        private const byte DdpIdDefaultOutput = 0x01;

        private byte sequence = 0;
        private readonly List<byte[]> packets = new List<byte[]>();

        public CDeviceWled()
        {
            this.SetDeviceType(DeviceType.WledDdp);
            this.SetDeviceName("WLED");
            this.InitClass();
        }

        public CDeviceWled(string szDeviceName, IPAddress ipAddress)
        {
            this.SetDeviceType(DeviceType.WledDdp);
            this.SetDeviceName(szDeviceName);
            this.SetIPAddress(ipAddress);
            this.InitClass();
        }

        protected override int DefaultPort { get { return DdpPort; } }

        protected override void SendFrame(byte[] colors, int bytes)
        {
            sequence = (byte)(sequence % 15 + 1);   // 1..15, 0 means "not used"
            BuildDdpPackets(colors, bytes, sequence, packets);
            foreach (byte[] packet in packets)
                SendDatagram(packet);
        }

        #region DDP
        /// <summary>
        /// Splits the RGB bytes into DDP packets of at most <see cref="DdpMaxDataPerPacket"/> bytes: 10 byte
        /// header (flags version 1, sequence, data type RGB 8 bit, id 1, byte offset and length big endian),
        /// the push flag on the last packet so the receiver shows the frame once complete.
        /// </summary>
        public static void BuildDdpPackets(byte[] data, int length, byte sequenceNumber, List<byte[]> output)
        {
            output.Clear();
            int offset = 0;
            do
            {
                int len = Math.Min(DdpMaxDataPerPacket, length - offset);
                bool last = offset + len >= length;
                byte[] p = new byte[DdpHeaderLength + len];
                p[0] = (byte)(DdpFlagsVersion1 | (last ? DdpFlagPush : 0));
                p[1] = (byte)(sequenceNumber & 0x0F);
                p[2] = DdpTypeRgb8;
                p[3] = DdpIdDefaultOutput;
                p[4] = (byte)(offset >> 24);
                p[5] = (byte)(offset >> 16);
                p[6] = (byte)(offset >> 8);
                p[7] = (byte)offset;
                p[8] = (byte)(len >> 8);
                p[9] = (byte)len;
                Buffer.BlockCopy(data, offset, p, DdpHeaderLength, len);
                output.Add(p);
                offset += len;
            } while (offset < length);
        }
        #endregion

        #region WLED info and discovery
        /// <summary>What a WLED device reports under /json/info.</summary>
        public class WledInfo
        {
            public IPAddress Address;
            public string Name;
            public string Version;
            public string Mac;
            public int LedCount;
        }

        /// <summary>Reads /json/info; null when the address does not answer as WLED.</summary>
        public static WledInfo QueryInfo(IPAddress ip, int timeoutMs = 2000)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://" + ip + "/json/info");
                request.Timeout = timeoutMs;
                request.ReadWriteTimeout = timeoutMs;
                request.Proxy = null;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    return ParseInfo(ip, reader.ReadToEnd());
            }
            catch (Exception ex) when (ex is WebException || ex is IOException || ex is Newtonsoft.Json.JsonException)
            {
                return null;
            }
        }

        /// <summary>Parses the JSON of /json/info; null when it is not WLED.</summary>
        public static WledInfo ParseInfo(IPAddress ip, string json)
        {
            JObject info = JObject.Parse(json);
            string brand = (string)info["brand"];
            if (!string.Equals(brand, "WLED", StringComparison.OrdinalIgnoreCase))
                return null;
            string mac = (string)info["mac"] ?? "";
            return new WledInfo
            {
                Address = ip,
                Name = (string)info["name"] ?? "WLED",
                Version = (string)info["ver"] ?? "",
                Mac = mac.Replace(":", "").ToLowerInvariant(),
                LedCount = (int?)info["leds"]?["count"] ?? 0,
            };
        }

        /// <summary>WLED devices that answer an mDNS query for _wled._tcp.local.</summary>
        public static List<WledInfo> Discover(int timeoutMs = 2000)
        {
            var result = new List<WledInfo>();
            List<IPAddress> candidates = AmbiBack.PhilipsHue.HueDiscovery.QueryMdns(timeoutMs, "_wled");
            var checks = new List<System.Threading.Tasks.Task<WledInfo>>();
            foreach (IPAddress ip in candidates)
                checks.Add(System.Threading.Tasks.Task.Run(() => QueryInfo(ip)));
            foreach (var t in checks)
                if (t.Result != null)
                    result.Add(t.Result);
            return result;
        }

        /// <summary>New device from what WLED reported: name, MAC for the id, and a layout for its LED count.</summary>
        public static CDeviceWled FromInfo(WledInfo info)
        {
            var device = new CDeviceWled(info.Name, info.Address)
            {
                ReceiverLedCount = info.LedCount,
                ReceiverInfo = "WLED " + info.Version,
                InternalNetworkMAC = string.IsNullOrEmpty(info.Mac) ? null : info.Mac,
            };
            device.SuggestLayout(info.LedCount);
            device.SetDeviceID();
            device.SetIsOnline(true);
            return device;
        }

        /// <summary>
        /// Command line test (--wled-test host [seconds]): reads /json/info (a plain DDP receiver works as
        /// well), then streams a moving test picture through the normal device thread. True when frames were sent.
        /// </summary>
        public static bool RunStreamTest(string host, int seconds, Action<string> output)
        {
            IPAddress ip = ResolveHost(host, output);
            if (ip == null)
                return false;

            WledInfo info = QueryInfo(ip);
            CDeviceWled device;
            if (info != null)
            {
                device = FromInfo(info);
                output("WLED " + info.Version + " \"" + info.Name + "\", " + info.LedCount + " LEDs, layout " + device.PixelHori + " x " + device.PixelVert);
            }
            else
            {
                device = new CDeviceWled(host, ip);
                output("No WLED answer at http://" + ip + "/json/info, sending DDP with the default layout " + device.PixelHori + " x " + device.PixelVert);
            }

            int framesReported = StreamTestPicture(device, seconds, output);
            output(framesReported > 0 ? "Done, the receiver returns to its own effect after about 2.5 s." : "No frames were sent.");
            return framesReported > 0;
        }
        #endregion
    }
}
