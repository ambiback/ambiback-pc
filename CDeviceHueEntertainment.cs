using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Xml;
using AmbiBack.PhilipsHue;

namespace AmbiBack
{
    /// <summary>
    /// Philips Hue entertainment area, fed through the Entertainment API (DTLS streaming, 25 to 50 fps).
    /// Every channel of the area gets the average color of a screen region around its position:
    /// x (left/right) maps to the screen width and z (bottom/top) to the screen height.
    /// The per light REST device (<see cref="CDevicePhilipsHueLight"/>) remains as fallback.
    /// </summary>
    [Serializable]
    public class CDeviceHueEntertainment : CDevice
    {
        private bool bInitDone = false;
        private List<EntertainmentChannel> channels = new List<EntertainmentChannel>();
        private readonly object channelsLock = new object();
        private HueEntertainmentSession session;

        #region public get/set variables
        public string HueConfigID { get; set; }
        public string HueAreaName { get; set; }

        /// <summary>Last known channel layout ("id:x:y:z;..."), refreshed from the bridge on every connect.</summary>
        public string HueChannels
        {
            get { lock (channelsLock) return EntertainmentChannel.Format(channels); }
            set { lock (channelsLock) channels = EntertainmentChannel.Parse(value); }
        }

        public int ChannelCount { get { lock (channelsLock) return channels.Count; } }

        /// <summary>Streaming state for the UI, e.g. "Streaming", "Waiting" with the last error.</summary>
        public string StreamStatus { get; private set; } = "";

        public bool IsStreaming { get { return session != null && session.State == HueEntertainmentSession.SessionState.Streaming; } }
        #endregion

        public CDeviceHueEntertainment()
        {
            this.SetDeviceType(DeviceType.PhilipsHueEntertainment);
            this.SetDeviceName("Hue Entertainment");
            this.InitClass();
        }

        public CDeviceHueEntertainment(EntertainmentArea area)
        {
            this.SetDeviceType(DeviceType.PhilipsHueEntertainment);
            this.SetDeviceName(area.Name);
            this.HueConfigID = area.ID;
            this.HueAreaName = area.Name;
            lock (channelsLock)
                channels = new List<EntertainmentChannel>(area.Channels);
            this.InitClass();
        }

        #region Virtual Public functions
        public override void InitClass()
        {
            this.SetDeviceID();

            this.FramesPerSecond = 25;
            this.MaxFramesPerSecond = 50;
            this.StepFramesPerSecond = 5;
            this.InnerSizePercent = 25;   // size of the scan region around each light (percent of the screen)

            this.EditColorRedPercent = true;
            this.EditColorGreenPercent = true;
            this.EditColorBluePercent = true;
            this.EditColorBoostPercent = true;
            this.EditMaxBrightnessPercent = true;
            this.EditMinimumLuminosityPercent = true;
            this.EditFramesPerSecond = true;
            this.EditFrameDelay = false;
        }

        public override void ProcessPayloadData(byte[] bData)
        {
        }

        public override void WriteChildXml(XmlWriter writer)
        {
            try
            {
                writer.WriteElementString("HueConfigID", this.HueConfigID ?? "");
                writer.WriteElementString("HueAreaName", this.HueAreaName ?? "");
                writer.WriteElementString("HueChannels", this.HueChannels);
                this.WriteXmlElement(writer, "FramesPerSecond", this.FramesPerSecond);
                this.WriteXmlElement(writer, "InnerSizePercent", this.InnerSizePercent);
                this.WriteXmlElement(writer, "ColorBoostPercent", this.ColorBoostPercent);
                this.WriteXmlElement(writer, "ColorRedPercent", this.ColorRedPercent);
                this.WriteXmlElement(writer, "ColorGreenPercent", this.ColorGreenPercent);
                this.WriteXmlElement(writer, "ColorBluePercent", this.ColorBluePercent);
                this.WriteXmlElement(writer, "MaxBrightnessPercent", this.MaxBrightnessPercent);
                this.WriteXmlElement(writer, "MinimumLuminosityPercent", this.MinimumLuminosityPercent);
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
                this.HueConfigID = reader.ReadElementContentAsString("HueConfigID", "");
                this.HueAreaName = reader.ReadElementContentAsString("HueAreaName", "");
                this.HueChannels = reader.ReadElementContentAsString("HueChannels", "");
                this.FramesPerSecond = this.ReadXmlElementAsByte(reader, "FramesPerSecond", 25);
                this.InnerSizePercent = this.ReadXmlElementAsByte(reader, "InnerSizePercent", 25);
                this.ColorBoostPercent = this.ReadXmlElementAsByte(reader, "ColorBoostPercent", 0);
                this.ColorRedPercent = this.ReadXmlElementAsByte(reader, "ColorRedPercent", 100);
                this.ColorGreenPercent = this.ReadXmlElementAsByte(reader, "ColorGreenPercent", 100);
                this.ColorBluePercent = this.ReadXmlElementAsByte(reader, "ColorBluePercent", 100);
                this.MaxBrightnessPercent = this.ReadXmlElementAsByte(reader, "MaxBrightnessPercent", 100);
                this.MinimumLuminosityPercent = this.ReadXmlElementAsByte(reader, "MinimumLuminosityPercent", 0);
                if (this.FramesPerSecond < 1 || this.FramesPerSecond > this.MaxFramesPerSecond)
                    this.FramesPerSecond = 25;
            }
            catch (Exception ex)
            {
                CGlobals.HandleException("ReadXml", ex);
            }
        }

        public override void UpdateFrom(CDevice devCopyFrom)
        {
            CDeviceHueEntertainment other = (CDeviceHueEntertainment)devCopyFrom;
            this.HueConfigID = other.HueConfigID;
            this.HueAreaName = other.HueAreaName;
            this.HueChannels = other.HueChannels;
        }

        public override void CopyFrom(CDevice devCopyFrom)
        {
            CDeviceHueEntertainment other = (CDeviceHueEntertainment)devCopyFrom;
            this.bIsOnline = other.bIsOnline;
            this.UpdateFrom(other);
            this.FramesPerSecond = other.FramesPerSecond;
            this.InnerSizePercent = other.InnerSizePercent;
            this.ColorBoostPercent = other.ColorBoostPercent;
            this.ColorRedPercent = other.ColorRedPercent;
            this.ColorGreenPercent = other.ColorGreenPercent;
            this.ColorBluePercent = other.ColorBluePercent;
            this.MaxBrightnessPercent = other.MaxBrightnessPercent;
            this.MinimumLuminosityPercent = other.MinimumLuminosityPercent;
        }

        public override void SetDeviceID()
        {
            // The configuration id is stable; the name may be renamed in the Hue app
            szDeviceID = CGlobals.DEVICETYPES[(int)this.GetDeviceType()] + "_" + (this.HueConfigID ?? "");
        }
        #endregion

        #region Threading
        public override void HandleThread()
        {
            DoDebugOutput("CDeviceHueEntertainment|HandleThread|Begin Thread");
            byte[] channelIds = new byte[HueStreamMessage.MaxChannels];
            ushort[] rgb16 = new ushort[HueStreamMessage.MaxChannels * 3];
            Stopwatch frameClock = Stopwatch.StartNew();
            Stopwatch fpsClock = Stopwatch.StartNew();
            int framesSent = 0;
            string lastStatus = null;

            try
            {
                session = new HueEntertainmentSession(this.HueConfigID, this.DoDebugOutput);
                session.AreaLoaded += OnAreaLoaded;

                while (!bAbortThread)
                {
                    try
                    {
                        long frameStart = frameClock.ElapsedMilliseconds;

                        if (!CGlobals.monitorPowerStatus)
                        {
                            // Monitor off: release the area so the lights return to normal
                            if (session.State != HueEntertainmentSession.SessionState.Idle)
                                session.Stop();
                            SetStatus(L.T("hue_st_monitor_off"));
                            Thread.Sleep(1000);
                            continue;
                        }

                        if (!session.EnsureConnected())
                        {
                            SetStatus(SessionStatusText(session));
                            Thread.Sleep(200);
                            continue;
                        }

                        if (!this.bInitDone)
                            InitHelpers();

                        // New colors only when the monitor delivered a new frame; otherwise repeat the last
                        // ones (the bridge ends the session after about 10 s without data)
                        int count = Math.Min(brPixels.Length, HueStreamMessage.MaxChannels);
                        if (this.rectScanPixels != null)
                            this.CalcColorsFromFrame(count, this.brPixels, this.UpdateScanAreas);

                        lock (channelsLock)
                        {
                            count = Math.Min(count, channels.Count);
                            for (int i = 0; i < count; i++)
                                channelIds[i] = channels[i].ID;
                        }
                        for (int i = 0; i < count; i++)
                            ToRgb16(brPixels[i], rgb16, i * 3);
                        session.Send(channelIds, rgb16, count);
                        framesSent++;

                        if (fpsClock.ElapsedMilliseconds >= 1000)
                        {
                            SetStatus(L.F("hue_st_streaming", framesSent));
                            DoUpdateStatus("Framerate send: " + framesSent + " - Target: " + this.GetDeviceName());
                            framesSent = 0;
                            fpsClock.Restart();
                        }

                        int fps = Math.Max(1, Math.Min((int)this.FramesPerSecond, (int)this.MaxFramesPerSecond));
                        int sleep = 1000 / fps - (int)(frameClock.ElapsedMilliseconds - frameStart);
                        if (sleep > 0)
                            Thread.Sleep(sleep);
                    }
                    catch (ThreadInterruptedException)
                    {
                        DoDebugOutput("CDeviceHueEntertainment|HandleThread|Interrupted");
                        session.Stop();
                        while (bSleepThread && !bAbortThread)
                            Thread.Sleep(10);
                    }

                    if (StreamStatus != lastStatus)
                    {
                        lastStatus = StreamStatus;
                        DoDebugOutput("CDeviceHueEntertainment|" + StreamStatus);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                DoDebugOutput("CDeviceHueEntertainment|HandleThread|Aborted");
                this.SetAbortThread(true);
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
                DoDebugOutput("CDeviceHueEntertainment|HandleThread|Exception: " + ex.Message);
                this.SetAbortThread(true);
            }
            finally
            {
                try
                {
                    session?.Stop();
                }
                catch (Exception ex)
                {
                    CGlobals.DebugOutput(ex);
                }
                session = null;
                bInitDone = false;
                SetStatus(L.T("hue_st_stopped"));
            }
            DoDebugOutput("CDeviceHueEntertainment|HandleThread|Finished Thread");
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
                DoDebugOutput("CDeviceHueEntertainment|StartThread|Exception: " + ex.Message);
            }
        }

        /// <summary>Recalculate the scan regions on the next frame (after the region size changed).</summary>
        public void RecalculateScanAreas()
        {
            this.bInitDone = false;
        }

        public bool IsThreadRunningSafe()
        {
            Thread t = myThread;
            return t != null && t.IsAlive;
        }

        private void OnAreaLoaded(EntertainmentArea area)
        {
            bool changed;
            lock (channelsLock)
            {
                changed = EntertainmentChannel.Format(channels) != EntertainmentChannel.Format(area.Channels);
                channels = new List<EntertainmentChannel>(area.Channels);
            }
            this.HueAreaName = area.Name;
            if (changed)
                this.bInitDone = false;    // recalculate the scan regions
        }

        private static string SessionStatusText(HueEntertainmentSession s)
        {
            string text = s.State == HueEntertainmentSession.SessionState.Connecting ? L.T("hue_st_connecting")
                : s.State == HueEntertainmentSession.SessionState.Waiting ? L.T("hue_st_waiting")
                : L.T("hue_st_stopped");
            return s.LastError != null ? text + ": " + s.LastError : text;
        }

        private void SetStatus(string status)
        {
            StreamStatus = status;
        }
        #endregion

        #region Pixel processing
        /// <summary>Applies the color and brightness sliders and widens 8 bit to 16 bit.</summary>
        private void ToRgb16(Color c, ushort[] target, int offset)
        {
            int max = this.MaxBrightnessPercent;
            target[offset] = (ushort)(c.R * 257 * this.ColorRedPercent / 100 * max / 100);
            target[offset + 1] = (ushort)(c.G * 257 * this.ColorGreenPercent / 100 * max / 100);
            target[offset + 2] = (ushort)(c.B * 257 * this.ColorBluePercent / 100 * max / 100);
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
            int n = Math.Max(1, Math.Min(ChannelCount, HueStreamMessage.MaxChannels));
            brPixels = new Color[n];
            rectScanPixels = new Rectangle[n];
            lColorsFrameNumber = -1;
            UpdateScanAreas(width, height);
            bInitDone = true;
        }

        private void UpdateScanAreas(int width, int height)
        {
            List<EntertainmentChannel> snapshot;
            lock (channelsLock)
                snapshot = new List<EntertainmentChannel>(channels);
            int n = rectScanPixels.Length;
            for (int i = 0; i < n; i++)
                rectScanPixels[i] = i < snapshot.Count
                    ? ScanRectangle(snapshot[i], width, height, this.InnerSizePercent)
                    : Rectangle.Empty;

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

        /// <summary>
        /// Screen region for a channel: centered at x (-1 left .. +1 right) and z (+1 top .. -1 bottom),
        /// sizePercent of the screen in both directions, kept inside the screen.
        /// </summary>
        public static Rectangle ScanRectangle(EntertainmentChannel ch, int width, int height, int sizePercent)
        {
            double size = Math.Max(5, Math.Min(50, sizePercent)) / 100.0;
            int w = Math.Max(1, (int)(width * size));
            int h = Math.Max(1, (int)(height * size));
            double x = Math.Max(-1, Math.Min(1, ch.X));
            double z = Math.Max(-1, Math.Min(1, ch.Z));
            int cx = (int)Math.Round((x + 1) / 2 * width);
            int cy = (int)Math.Round((1 - z) / 2 * height);
            int left = Math.Max(0, Math.Min(width - w, cx - w / 2));
            int top = Math.Max(0, Math.Min(height - h, cy - h / 2));
            return new Rectangle(left, top, w, h);
        }
        #endregion
    }
}
