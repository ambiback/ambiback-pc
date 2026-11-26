using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml;

namespace AmbiBack
{
    /// <summary>
    /// Base of the network LED strips whose layout and colors are computed in AmbiBack PC (WLED/DDP,
    /// Govee): the strip runs around the screen like an AmbiBack controller (LEDs per side, start corner and
    /// direction), the device gets the final RGB values. The color sliders and the smoothing (FrameDelay)
    /// are applied here; the subclass connects (<see cref="Connect"/>) and puts the colors on the wire
    /// (<see cref="SendFrame"/>). <see cref="CDeviceLedStripUdp"/> is the base for UDP devices.
    /// </summary>
    [Serializable]
    public abstract class CDeviceLedStrip : CDevice, ILedStripLayout
    {
        /// <summary>Largest LED counts per side the monitor preview offers.</summary>
        public const int MaxPixelHori = 120;
        public const int MaxPixelVert = 60;

        protected bool bInitDone = false;
        private bool connected;
        private IPAddress connectedAddress;
        private bool streaming;
        private byte[] rgb;
        private float[] smoothed;

        #region public get/set variables
        public byte PixelHori { get; set; } = 16;
        public byte PixelVert { get; set; } = 8;
        public byte FlexSetupPort1 { get; set; } = (byte)DsFlexSetups.Left_Clock_bottom_left_up;
        public int Port { get; set; }

        /// <summary>LED count the device reported, 0 when unknown.</summary>
        public int ReceiverLedCount { get; set; } = 0;
        /// <summary>What the device reported about itself, e.g. "WLED 0.15.0"; empty when unknown.</summary>
        public string ReceiverInfo { get; set; } = "";

        /// <summary>LEDs of the layout: both sides plus top and bottom.</summary>
        public int PixelTotal { get { return 2 * (Math.Max(1, (int)PixelHori) + Math.Max(1, (int)PixelVert)); } }

        /// <summary>Index into <see cref="ModeTextKeys"/> for devices with modes; stored by the subclass.</summary>
        public byte Mode { get; set; } = 0;
        /// <summary>Localization keys of the modes shown on the Device tab; null when the device has none.</summary>
        public virtual string[] ModeTextKeys { get { return null; } }

        /// <summary>
        /// True when the monitor card shows the strip layout (LEDs per side, start corner, direction) for
        /// this device; false for one-color modes and devices with their own positions (Nanoleaf).
        /// </summary>
        public virtual bool UsesStripLayout { get { return true; } }

        /// <summary>Colors sent per frame: the LEDs of the layout, or e.g. the panels of a Nanoleaf.</summary>
        protected virtual int ColorCount { get { return this.PixelTotal; } }

        /// <summary>Screen region of every color; the strip runs around the screen.</summary>
        protected virtual void CalcScanRegions(int width, int height, Rectangle[] regions)
        {
            this.CalcStripRectangles(this.FlexSetupPort1, Math.Max((byte)1, this.PixelVert), Math.Max((byte)1, this.PixelHori),
                regions.Length, 0, 0, width, height, 0, regions);
        }
        #endregion

        protected CDeviceLedStrip()
        {
            this.Port = this.DefaultPort;
        }

        /// <summary>UDP port of the device.</summary>
        protected abstract int DefaultPort { get; }
        /// <summary>Frame rate of a new device, and after reading an invalid value.</summary>
        protected virtual byte DefaultFramesPerSecond { get { return 50; } }
        protected virtual byte DefaultPixelHori { get { return 16; } }
        protected virtual byte DefaultPixelVert { get { return 8; } }

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
            this.PixelHori = this.DefaultPixelHori;
            this.PixelVert = this.DefaultPixelVert;
            this.SetDeviceID();

            this.FramesPerSecond = this.DefaultFramesPerSecond;
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
                this.WriteStripXml(writer);
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
                this.PixelHori = Math.Max((byte)1, this.ReadXmlElementAsByte(reader, "PixelHori", this.DefaultPixelHori));
                this.PixelVert = Math.Max((byte)1, this.ReadXmlElementAsByte(reader, "PixelVert", this.DefaultPixelVert));
                this.FlexSetupPort1 = this.ReadXmlElementAsByte(reader, "FlexSetupPort1", (byte)DsFlexSetups.Left_Clock_bottom_left_up);
                this.Port = this.ReadXmlElementAsInt(reader, "Port", this.DefaultPort);
                this.FramesPerSecond = this.ReadXmlElementAsByte(reader, "FramesPerSecond", this.DefaultFramesPerSecond);
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
                this.ReadStripXml(reader);
            }
            catch (Exception ex)
            {
                CGlobals.HandleException("ReadXml", ex);
            }
            finally
            {
                if (this.Port <= 0 || this.Port > 65535)
                    this.Port = this.DefaultPort;
                if (this.FramesPerSecond < 1 || this.FramesPerSecond > this.MaxFramesPerSecond)
                    this.FramesPerSecond = this.DefaultFramesPerSecond;
                this.CheckInternalValues();
            }
        }

        /// <summary>Settings of the subclass, written after the common ones.</summary>
        protected virtual void WriteStripXml(XmlWriter writer) { }
        protected virtual void ReadStripXml(XmlReader reader) { }

        public override void UpdateFrom(CDevice devCopyFrom)
        {
            CDeviceLedStrip other = (CDeviceLedStrip)devCopyFrom;
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
            // what the device reports, keep the layout and the sliders of the stored device
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

        #region Connection and sending, implemented by the subclass
        /// <summary>Opens the connection to the device; throws SocketException or IOException on failure.</summary>
        protected abstract void Connect(IPAddress ip);
        /// <summary>Closes the connection; must not throw.</summary>
        protected abstract void Disconnect();
        /// <summary>Sends one frame: <paramref name="bytes"/> RGB bytes, sliders and smoothing applied.</summary>
        protected abstract void SendFrame(byte[] colors, int bytes);
        /// <summary>Before the first frame, and again after a pause (monitor off or stopped, new address).</summary>
        protected virtual void OnStreamStart() { }
        /// <summary>After the last frame: stop, monitor off, new address.</summary>
        protected virtual void OnStreamStop() { }
        #endregion

        #region Threading
        public override void HandleThread()
        {
            string name = this.GetType().Name;
            DoDebugOutput(name + "|HandleThread|Begin Thread");
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
                                // Monitor off: stop sending, the device returns to its own effect
                                StopStream();
                                Thread.Sleep(1000);
                                continue;
                            }

                            if (!this.bInitDone)
                                InitHelpers();

                            this.CalcColorsFromFrame(this.brPixels.Length, this.brPixels, this.UpdateScanAreas);
                            int bytes = BuildColors();
                            if (!EnsureConnected())
                            {
                                Thread.Sleep(500);
                                continue;
                            }
                            SendFrame(rgb, bytes);

                            int iFramesPerSec = pacer.FrameDone(this.FramesPerSecond);
                            if (iFramesPerSec >= 0)
                                DoUpdateStatus("Framerate send: " + iFramesPerSec + " - Target: " + this.GetDeviceName());
                        }
                    }
                    catch (ThreadInterruptedException)
                    {
                        // Stop of the monitor (SleepThread): end the stream, it starts again on resume
                        DoDebugOutput(name + "|HandleThread|Interrupted");
                        StopStream();
                        while (bSleepThread && !bAbortThread)
                            Thread.Sleep(10);
                    }
                    catch (Exception ex) when (ex is SocketException || ex is IOException)
                    {
                        // Device not reachable or connection lost: close and retry
                        DoDebugOutput(name + "|Connection: " + ex.Message);
                        CloseClient();
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                DoDebugOutput(name + "|HandleThread|Aborted");
                this.SetAbortThread(true);
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
                DoDebugOutput(name + "|HandleThread|Exception: " + ex.Message);
                this.SetAbortThread(true);
            }
            finally
            {
                StopStream();
                CloseClient();
                bInitDone = false;
            }
            DoDebugOutput(name + "|HandleThread|Finished Thread");
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
                DoDebugOutput(this.GetType().Name + "|StartThread|Exception: " + ex.Message);
            }
        }

        /// <summary>Connects to the current address and starts the stream; false while the address is unknown.</summary>
        private bool EnsureConnected()
        {
            IPAddress ip = this.GetIPAddress();
            if (ip == null)
                return false;
            // The address can change while streaming (DHCP, found again by discovery)
            if (connected && !ip.Equals(connectedAddress))
            {
                StopStream();
                CloseClient();
            }
            if (!connected)
            {
                Connect(ip);
                connected = true;
                connectedAddress = ip;
            }
            if (!streaming)
            {
                OnStreamStart();
                streaming = true;
            }
            return true;
        }

        private void StopStream()
        {
            if (!streaming)
                return;
            streaming = false;
            try
            {
                if (connected)
                    OnStreamStop();
            }
            catch (Exception ex) when (ex is SocketException || ex is IOException || ex is ObjectDisposedException)
            {
                DoDebugOutput(this.GetType().Name + "|StopStream: " + ex.Message);
            }
        }

        private void CloseClient()
        {
            try
            {
                Disconnect();
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
            connected = false;
            streaming = false;
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
            int n = Math.Max(0, this.ColorCount);
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
            CalcScanRegions(width, height, rectScanPixels);
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
        #endregion

        #region Layout and test helpers
        /// <summary>Mean of the colors as 0xRRGGBB.</summary>
        public static int AverageColor(byte[] colors, int count)
        {
            if (count <= 0)
                return 0;
            long r = 0, g = 0, b = 0;
            for (int i = 0; i < count; i++)
            {
                r += colors[i * 3];
                g += colors[i * 3 + 1];
                b += colors[i * 3 + 2];
            }
            return (int)((r + count / 2) / count) << 16 | (int)((g + count / 2) / count) << 8 | (int)((b + count / 2) / count);
        }

        /// <summary>
        /// Splits a strip of the given length around a 16:9 screen (top/bottom : sides = 16 : 9), within the
        /// choices of the monitor preview (<see cref="MaxPixelHori"/>, <see cref="MaxPixelVert"/>).
        /// Leaves the layout unchanged for an unknown count.
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

        /// <summary>IPv4 address of a host name or address text; null (with a message) when unknown.</summary>
        protected static IPAddress ResolveHost(string host, Action<string> output)
        {
            IPAddress ip;
            if (IPAddress.TryParse(host, out ip))
                return ip;
            try
            {
                ip = Array.Find(Dns.GetHostAddresses(host), a => a.AddressFamily == AddressFamily.InterNetwork);
            }
            catch (SocketException ex)
            {
                output("Cannot resolve " + host + ": " + ex.Message);
                return null;
            }
            if (ip == null)
                output("Cannot resolve " + host);
            return ip;
        }

        /// <summary>
        /// Streams a test picture with a color wheel turning once per 5 s through the normal device thread
        /// (command line tests). Returns how many frame rate reports the thread gave.
        /// </summary>
        protected static int StreamTestPicture(CDeviceLedStrip device, int seconds, Action<string> output)
        {
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
            return framesReported;
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
        #endregion
    }
}
