using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;
using Newtonsoft.Json.Linq;

namespace AmbiBack
{
    /// <summary>
    /// LED strip driven over DDP (Distributed Display Protocol, UDP port 4048): WLED, ESPHome, Hyperion
    /// receivers, ESPixelStick and other DDP receivers. WLED switches to live mode on the first packet and
    /// returns to its own effect about 2.5 s after the last one, so stopping needs no extra command.
    /// The strip runs around the screen like an AmbiBack controller (LEDs per side, start corner and
    /// direction), but the layout and all color settings stay in AmbiBack PC: the receiver gets the final
    /// RGB values. Smoothing (FrameDelay) is done here as well, DDP receivers show every frame as is.
    /// </summary>
    [Serializable]
    public class CDeviceWled : CDevice, ILedStripLayout
    {
        public const int DefaultPort = 4048;
        /// <summary>Largest LED counts per side the monitor preview offers.</summary>
        public const int MaxPixelHori = 120;
        public const int MaxPixelVert = 60;
        public const int DdpHeaderLength = 10;
        /// <summary>RGB bytes per packet: 480 LEDs, the size WLED sends and expects.</summary>
        public const int DdpMaxDataPerPacket = 1440;

        private const byte DdpFlagsVersion1 = 0x40;
        private const byte DdpFlagPush = 0x01;
        private const byte DdpTypeRgb8 = 0x0B;        // RGB, 8 bit per channel
        private const byte DdpIdDefaultOutput = 0x01;

        private bool bInitDone = false;
        private UdpClient udp;
        private IPAddress connectedAddress;
        private byte sequence = 0;
        private byte[] rgb;
        private float[] smoothed;
        private readonly List<byte[]> packets = new List<byte[]>();

        #region public get/set variables
        public byte PixelHori { get; set; } = 16;
        public byte PixelVert { get; set; } = 8;
        public byte FlexSetupPort1 { get; set; } = (byte)DsFlexSetups.Left_Clock_bottom_left_up;
        public int Port { get; set; } = DefaultPort;

        /// <summary>LED count the receiver reported (WLED /json/info), 0 when unknown.</summary>
        public int ReceiverLedCount { get; set; } = 0;
        /// <summary>Firmware the receiver reported, e.g. "WLED 0.15.0"; empty for a plain DDP receiver.</summary>
        public string ReceiverInfo { get; set; } = "";

        /// <summary>LEDs of the layout: both sides plus top and bottom.</summary>
        public int PixelTotal { get { return 2 * (Math.Max(1, (int)PixelHori) + Math.Max(1, (int)PixelVert)); } }
        #endregion

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

        #region ILedStripLayout
        public void SetPixelHori(byte bNew)
        {
            this.PixelHori = Math.Max((byte)1, bNew);
            this.bInitDone = false;
        }
        public void SetPixelVert(byte bNew)
        {
            this.PixelVert = Math.Max((byte)1, bNew);
            this.bInitDone = false;
        }
        public void SetFlexPort1(byte bNew)
        {
            this.FlexSetupPort1 = bNew;
            this.bInitDone = false;
        }
        // The layout lives in AmbiBack PC only
        public void SendLedHorizontal() { }
        public void SendLedVertical() { }
        public void SendLedDirection() { }
        #endregion

        /// <summary>Recalculate the scan regions on the next frame (after the region size changed).</summary>
        public void RecalculateScanAreas()
        {
            this.bInitDone = false;
        }

        #region Virtual Public functions
        public override void InitClass()
        {
            this.SetDeviceID();

            this.FramesPerSecond = 50;
            this.MaxFramesPerSecond = 60;
            this.StepFramesPerSecond = 5;
            this.InnerSizePercent = 15;

            // Smoothing in ms (0 = off), done here
            this.FrameDelay = 0;
            this.MinFrameDelay = 0;
            this.MaxFrameDelay = 1000;
            this.StepFrameDelay = 10;

            this.EditColorRedPercent = true;
            this.EditColorGreenPercent = true;
            this.EditColorBluePercent = true;
            this.EditColorBoostPercent = true;
            this.EditValueGainPercent = true;      // applied in AdjustColor (UseDeviceValueGainPercent is false)
            this.EditMaxBrightnessPercent = true;
            this.EditMinimumLuminosityPercent = true;
            this.EditDeviceName = true;
            this.EditFramesPerSecond = true;
            this.EditFrameDelay = true;
        }

        public override void ProcessPayloadData(byte[] bData)
        {
        }

        public override void WriteChildXml(XmlWriter writer)
        {
            try
            {
                this.WriteXmlElement(writer, "PixelHori", this.PixelHori);
                this.WriteXmlElement(writer, "PixelVert", this.PixelVert);
                this.WriteXmlElement(writer, "FlexSetupPort1", this.FlexSetupPort1);
                this.WriteXmlElement(writer, "Port", this.Port);
                this.WriteXmlElement(writer, "FramesPerSecond", this.FramesPerSecond);
                this.WriteXmlElement(writer, "InnerSizePercent", this.InnerSizePercent);
                this.WriteXmlElement(writer, "ColorBoostPercent", this.ColorBoostPercent);
                this.WriteXmlElement(writer, "ColorRedPercent", this.ColorRedPercent);
                this.WriteXmlElement(writer, "ColorGreenPercent", this.ColorGreenPercent);
                this.WriteXmlElement(writer, "ColorBluePercent", this.ColorBluePercent);
                this.WriteXmlElement(writer, "MaxBrightnessPercent", this.MaxBrightnessPercent);
                this.WriteXmlElement(writer, "MinimumLuminosityPercent", this.MinimumLuminosityPercent);
                this.WriteXmlElement(writer, "ValueGainPercent", this.ValueGainPercent);
                this.WriteXmlElement(writer, "FrameDelay", this.FrameDelay);
                this.WriteXmlElement(writer, "ReceiverLedCount", this.ReceiverLedCount);
                writer.WriteElementString("ReceiverInfo", this.ReceiverInfo ?? "");
                writer.WriteElementString("InternalNetworkMAC", this.InternalNetworkMAC ?? "");
            }
            catch (Exception ex)
            {
                CGlobals.HandleException("WriteXml", ex);
            }
        }

        public override void ReadChildXml(XmlReader reader)
        {
            try
            {
                this.PixelHori = Math.Max((byte)1, this.ReadXmlElementAsByte(reader, "PixelHori", 16));
                this.PixelVert = Math.Max((byte)1, this.ReadXmlElementAsByte(reader, "PixelVert", 8));
                this.FlexSetupPort1 = this.ReadXmlElementAsByte(reader, "FlexSetupPort1", (byte)DsFlexSetups.Left_Clock_bottom_left_up);
                this.Port = this.ReadXmlElementAsInt(reader, "Port", DefaultPort);
                this.FramesPerSecond = this.ReadXmlElementAsByte(reader, "FramesPerSecond", 50);
                this.InnerSizePercent = this.ReadXmlElementAsByte(reader, "InnerSizePercent", 15);
                this.ColorBoostPercent = this.ReadXmlElementAsByte(reader, "ColorBoostPercent", 0);
                this.ColorRedPercent = this.ReadXmlElementAsByte(reader, "ColorRedPercent", 100);
                this.ColorGreenPercent = this.ReadXmlElementAsByte(reader, "ColorGreenPercent", 100);
                this.ColorBluePercent = this.ReadXmlElementAsByte(reader, "ColorBluePercent", 100);
                this.MaxBrightnessPercent = this.ReadXmlElementAsByte(reader, "MaxBrightnessPercent", 100);
                this.MinimumLuminosityPercent = this.ReadXmlElementAsByte(reader, "MinimumLuminosityPercent", 0);
                this.ValueGainPercent = Math.Max(100, this.ReadXmlElementAsInt(reader, "ValueGainPercent", 100));
                this.FrameDelay = this.ReadXmlElementAsInt(reader, "FrameDelay", 0);
                this.ReceiverLedCount = this.ReadXmlElementAsInt(reader, "ReceiverLedCount", 0);
                this.ReceiverInfo = reader.ReadElementContentAsString("ReceiverInfo", "");
                string mac = reader.ReadElementContentAsString("InternalNetworkMAC", "");
                this.InternalNetworkMAC = mac.Length > 0 ? mac : null;
            }
            catch (Exception ex)
            {
                CGlobals.HandleException("ReadXml", ex);
            }
            finally
            {
                if (this.Port <= 0 || this.Port > 65535)
                    this.Port = DefaultPort;
                if (this.FramesPerSecond < 1 || this.FramesPerSecond > this.MaxFramesPerSecond)
                    this.FramesPerSecond = 50;
                this.CheckInternalValues();
            }
        }

        public override void UpdateFrom(CDevice devCopyFrom)
        {
            CDeviceWled other = (CDeviceWled)devCopyFrom;
            this.bIsOnline = other.bIsOnline;
            if (other.GetIPAddress() != null)
                this.SetIPAddress(other.GetIPAddress());
            if (other.ReceiverLedCount > 0)
                this.ReceiverLedCount = other.ReceiverLedCount;
            if (!string.IsNullOrEmpty(other.ReceiverInfo))
                this.ReceiverInfo = other.ReceiverInfo;
            if (!string.IsNullOrEmpty(other.InternalNetworkMAC))
                this.InternalNetworkMAC = other.InternalNetworkMAC;
        }

        public override void CopyFrom(CDevice devCopyFrom)
        {
            // Called when a device with the same id is added again (discovery, add dialog): only refresh
            // what the receiver reports, keep the layout and the sliders of the stored device
            this.UpdateFrom(devCopyFrom);
        }

        public override void SetDeviceID()
        {
            // The MAC address is stable; the address may change with DHCP
            string id = !string.IsNullOrEmpty(this.InternalNetworkMAC) ? this.InternalNetworkMAC
                : (this.GetIPAddress() != null ? this.GetIPAddress().ToString() : this.GetDeviceName());
            szDeviceID = CGlobals.DEVICETYPES[(int)this.GetDeviceType()] + "_" + id;
        }
        #endregion

        #region Threading
        public override void HandleThread()
        {
            DoDebugOutput("CDeviceWled|HandleThread|Begin Thread");
            try
            {
                while (!bAbortThread)
                {
                    try
                    {
                        FramePacer pacer = new FramePacer();
                        while (!bAbortThread)
                        {
                            if (!CGlobals.monitorPowerStatus)
                            {
                                // Monitor off: stop sending, the receiver returns to its own effect
                                Thread.Sleep(1000);
                                continue;
                            }

                            if (!this.bInitDone)
                                InitHelpers();

                            this.CalcColorsFromFrame(this.brPixels.Length, this.brPixels, this.UpdateScanAreas);
                            int bytes = BuildColors();
                            Send(bytes);

                            int iFramesPerSec = pacer.FrameDone(this.FramesPerSecond);
                            if (iFramesPerSec >= 0)
                                DoUpdateStatus("Framerate send: " + iFramesPerSec + " - Target: " + this.GetDeviceName());
                        }
                    }
                    catch (ThreadInterruptedException)
                    {
                        DoDebugOutput("CDeviceWled|HandleThread|Interrupted");
                        while (bSleepThread && !bAbortThread)
                            Thread.Sleep(10);
                    }
                    catch (SocketException ex)
                    {
                        // Address not reachable (e.g. no route): close and retry
                        DoDebugOutput("CDeviceWled|Send: " + ex.SocketErrorCode);
                        CloseClient();
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                DoDebugOutput("CDeviceWled|HandleThread|Aborted");
                this.SetAbortThread(true);
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
                DoDebugOutput("CDeviceWled|HandleThread|Exception: " + ex.Message);
                this.SetAbortThread(true);
            }
            finally
            {
                CloseClient();
                bInitDone = false;
            }
            DoDebugOutput("CDeviceWled|HandleThread|Finished Thread");
        }

        public override void StartThread()
        {
            try
            {
                this.SetAbortThread(false);
                this.SetSleepThread(false);
                if (myThread != null && myThread.ThreadState == System.Threading.ThreadState.Stopped)
                    myThread = null;
                CreateThread();
                if (myThread.ThreadState == System.Threading.ThreadState.Unstarted)
                    myThread.Start();
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
                DoDebugOutput("CDeviceWled|StartThread|Exception: " + ex.Message);
            }
        }

        private void InitHelpers()
        {
            int iWait = 0;
            while ((this.Frame == null || this.Frame.Pixels == null) && iWait <= 300 && !bAbortThread)
            {
                Thread.Sleep(100);
                iWait++;
            }
            if (this.Frame == null || this.Frame.Pixels == null)
                throw new Exception("ERROR COULD NOT GRAB SCREENSHOT");

            int width, height;
            lock (this.Frame.Sync)
            {
                width = this.Frame.Width;
                height = this.Frame.Height;
            }
            int n = this.PixelTotal;
            brPixels = new Color[n];
            rectScanPixels = new Rectangle[n];
            rgb = new byte[n * 3];
            smoothed = null;
            lColorsFrameNumber = -1;
            UpdateScanAreas(width, height);
            bInitDone = true;
        }

        private void UpdateScanAreas(int width, int height)
        {
            this.CalcStripRectangles(this.FlexSetupPort1, Math.Max((byte)1, this.PixelVert), Math.Max((byte)1, this.PixelHori),
                rectScanPixels.Length, 0, 0, width, height, 0, rectScanPixels);
            // CalcCorners takes a byte count, the strip can be longer
            int n = rectScanPixels.Length;
            aLeft = new int[n];
            aRight = new int[n];
            aTop = new int[n];
            aBottom = new int[n];
            for (int i = 0; i < n; i++)
            {
                aLeft[i] = rectScanPixels[i].Left;
                aRight[i] = rectScanPixels[i].Right;
                aTop[i] = rectScanPixels[i].Top;
                aBottom[i] = rectScanPixels[i].Bottom;
            }
            this.iScanAreaWidth = width;
            this.iScanAreaHeight = height;
        }

        /// <summary>Final RGB bytes of all LEDs (sliders and smoothing applied); returns the byte count.</summary>
        private int BuildColors()
        {
            int n = brPixels.Length;
            // Smoothing: exponential moving average with a time constant of FrameDelay ms
            float alpha = 1f;
            if (this.FrameDelay > 0)
            {
                float frameMs = 1000f / Math.Max(1, (int)this.FramesPerSecond);
                alpha = frameMs / (frameMs + this.FrameDelay);
            }
            if (smoothed == null || smoothed.Length != n * 3)
            {
                smoothed = new float[n * 3];
                alpha = 1f;
            }
            for (int i = 0; i < n; i++)
            {
                // Value gain and color boost were applied with the screen colors (CDevice.AdjustColor)
                Color c = CDeviceHueEntertainment.ApplyLightSettings(brPixels[i], 100, this.MinimumLuminosityPercent,
                    this.ColorRedPercent, this.ColorGreenPercent, this.ColorBluePercent, this.MaxBrightnessPercent);
                int o = i * 3;
                smoothed[o] += (c.R - smoothed[o]) * alpha;
                smoothed[o + 1] += (c.G - smoothed[o + 1]) * alpha;
                smoothed[o + 2] += (c.B - smoothed[o + 2]) * alpha;
                rgb[o] = (byte)Math.Max(0, Math.Min(255, (int)(smoothed[o] + 0.5f)));
                rgb[o + 1] = (byte)Math.Max(0, Math.Min(255, (int)(smoothed[o + 1] + 0.5f)));
                rgb[o + 2] = (byte)Math.Max(0, Math.Min(255, (int)(smoothed[o + 2] + 0.5f)));
            }
            return n * 3;
        }

        private void Send(int bytes)
        {
            IPAddress ip = this.GetIPAddress();
            if (ip == null)
            {
                Thread.Sleep(500);
                return;
            }
            // The address can change while streaming (DHCP, found again by discovery)
            if (udp != null && !ip.Equals(connectedAddress))
                CloseClient();
            if (udp == null)
            {
                udp = new UdpClient(ip.AddressFamily);
                udp.Connect(new IPEndPoint(ip, this.Port));
                connectedAddress = ip;
            }
            sequence = (byte)(sequence % 15 + 1);   // 1..15, 0 means "not used"
            BuildDdpPackets(rgb, bytes, sequence, packets);
            foreach (byte[] packet in packets)
                udp.Send(packet, packet.Length);
        }

        private void CloseClient()
        {
            try
            {
                udp?.Close();
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
            udp = null;
        }
        #endregion

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
        /// well), then streams a test picture with a color wheel turning once per 5 s through the normal
        /// device thread. True when frames were sent.
        /// </summary>
        public static bool RunStreamTest(string host, int seconds, Action<string> output)
        {
            IPAddress ip;
            if (!IPAddress.TryParse(host, out ip))
            {
                try
                {
                    ip = Array.Find(Dns.GetHostAddresses(host), a => a.AddressFamily == AddressFamily.InterNetwork);
                }
                catch (SocketException ex)
                {
                    output("Cannot resolve " + host + ": " + ex.Message);
                    return false;
                }
                if (ip == null)
                {
                    output("Cannot resolve " + host);
                    return false;
                }
            }

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

            int framesReported = 0;
            device.UpdateStatus += (sender, e, status) => { output(status); framesReported++; };
            device.Frame = new FrameBuffer();
            using (var bmp = new Bitmap(320, 180, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                var clock = System.Diagnostics.Stopwatch.StartNew();
                DrawTestPicture(bmp, 0);
                device.Frame.Update(bmp);
                device.StartThread();
                while (clock.ElapsedMilliseconds < seconds * 1000L)
                {
                    Thread.Sleep(33);
                    DrawTestPicture(bmp, clock.ElapsedMilliseconds / 5000.0);
                    device.Frame.Update(bmp);
                }
            }
            device.SetAbortThread(true);
            device.myThread?.Join(2000);
            output(framesReported > 0 ? "Done, the receiver returns to its own effect after about 2.5 s." : "No frames were sent.");
            return framesReported > 0;
        }

        /// <summary>Color wheel around the center, turned by the given fraction of a turn.</summary>
        private static void DrawTestPicture(Bitmap bmp, double turn)
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                int cx = bmp.Width / 2, cy = bmp.Height / 2;
                for (int i = 0; i < 36; i++)
                {
                    double hue = (i * 10 + turn * 360) % 360;
                    using (var brush = new SolidBrush(FromHue(hue)))
                        g.FillPie(brush, cx - bmp.Width, cy - bmp.Width, bmp.Width * 2, bmp.Width * 2, i * 10, 10);
                }
            }
        }

        private static Color FromHue(double hue)
        {
            double h = hue / 60, x = 1 - Math.Abs(h % 2 - 1);
            double r = h < 1 || h >= 5 ? 1 : h < 2 ? x : h >= 4 ? x : 0;
            double g = h < 1 ? x : h < 3 ? 1 : h < 4 ? x : 0;
            double b = h < 2 ? 0 : h < 3 ? x : h < 5 ? 1 : x;
            return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

        /// <summary>
        /// Splits a strip of the given length around a 16:9 screen (top/bottom : sides = 16 : 9), within the
        /// choices of the monitor preview (<see cref="MaxPixelHori"/>, <see cref="MaxPixelVert"/>).
        /// Leaves the default layout for an unknown count.
        /// </summary>
        public void SuggestLayout(int ledCount)
        {
            if (ledCount < 4)
                return;
            int half = ledCount / 2;
            int hori = Math.Max(1, Math.Min(MaxPixelHori, (int)Math.Round(half * 16.0 / 25.0)));
            int vert = Math.Max(1, Math.Min(MaxPixelVert, half - hori));
            this.PixelHori = (byte)hori;
            this.PixelVert = (byte)vert;
            this.bInitDone = false;
        }
        #endregion
    }
}
