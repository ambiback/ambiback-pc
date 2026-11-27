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

        /// <summary>
        /// The area of this PC (<see cref="HuePcGroup"/>): its lights are the single light devices with
        /// the checkbox set, each one reads its own screen sectors.
        /// </summary>
        public bool PcGroup { get; set; } = false;
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
            this.EditValueGainPercent = true;      // applied in AdjustColor (UseDeviceValueGainPercent is false)
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
                this.WriteXmlElement(writer, "PcGroup", this.PcGroup);
                this.WriteXmlElement(writer, "ValueGainPercent", this.ValueGainPercent);
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
                if (reader.IsStartElement("PcGroup"))
                    this.PcGroup = this.ReadXmlElementAsBool(reader, "PcGroup");
                if (reader.IsStartElement("ValueGainPercent"))
                    this.ValueGainPercent = Math.Max(100, this.ReadXmlElementAsInt(reader, "ValueGainPercent", 100));
                // An area of this PC added by hand before the PC group existed
                if (this.HueAreaName == HuePcGroup.AreaName)
                    this.PcGroup = true;
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
            this.PcGroup = other.PcGroup;
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
            this.ValueGainPercent = other.ValueGainPercent;
        }

        public override void SetDeviceID()
        {
            // The configuration id is stable; the name may be renamed in the Hue app
            // The PC group keeps its id even when the area is created again on the bridge
            szDeviceID = CGlobals.DEVICETYPES[(int)this.GetDeviceType()] + "_" + (this.PcGroup ? "PCGROUP" : (this.HueConfigID ?? ""));
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
            int pcVersion = -1;
            Stopwatch pcRetry = new Stopwatch();

            try
            {
                if (!PcGroup)
                {
                    session = new HueEntertainmentSession(this.HueConfigID, this.DoDebugOutput);
                    session.AreaLoaded += OnAreaLoaded;
                }

                while (!bAbortThread)
                {
                    try
                    {
                        long frameStart = frameClock.ElapsedMilliseconds;

                        if (!CGlobals.monitorPowerStatus || this.CaptureInterrupted)
                        {
                            // Monitor off or no capture: release the area so the lights return to normal
                            // instead of repeating the last frame (e.g. the colors of the logon screen)
                            if (session != null && session.State != HueEntertainmentSession.SessionState.Idle)
                                session.Stop();
                            SetStatus(CGlobals.monitorPowerStatus ? L.T("mon_waiting") : L.T("hue_st_monitor_off"));
                            Thread.Sleep(1000);
                            continue;
                        }

                        // PC group: set the area up for the current members (again after every change)
                        if (PcGroup && HuePcGroup.MembershipVersion != pcVersion)
                        {
                            if (pcRetry.IsRunning && pcRetry.ElapsedMilliseconds < 5000)
                            {
                                Thread.Sleep(200);
                                continue;
                            }
                            pcVersion = HuePcGroup.MembershipVersion;
                            List<CDevicePhilipsHueLight> members = HuePcGroup.Members();
                            session?.Stop(turnOff: members.Count == 0);
                            session = null;
                            pcLayout = null;
                            if (members.Count > 0)
                            {
                                try
                                {
                                    HuePcGroup.Layout layout = HuePcGroup.Configure(members, true);
                                    this.HueConfigID = layout.AreaID;
                                    OnAreaLoaded(layout.Area);
                                    pcLayout = layout;
                                    session = new HueEntertainmentSession(layout.AreaID, this.DoDebugOutput);
                                    session.AreaLoaded += OnAreaLoaded;
                                    pcRetry.Reset();
                                }
                                catch (Exception ex) when (!(ex is ThreadInterruptedException) && !(ex is ThreadAbortException))
                                {
                                    SetStatus(HueClipV2.IsAreaLimitError(ex) ? L.T("hue_area_limit") : L.F("hue_area_err", ex.Message));
                                    pcVersion = -1;
                                    pcRetry.Restart();
                                    continue;
                                }
                            }
                        }
                        if (session == null)
                        {
                            SetStatus(L.T("hue_st_no_members"));
                            Thread.Sleep(500);
                            continue;
                        }

                        if (!session.EnsureConnected())
                        {
                            SetStatus(SessionStatusText(session));
                            Thread.Sleep(200);
                            continue;
                        }

                        // New colors only when the monitor delivered a new frame; otherwise repeat the last
                        // ones (the bridge ends the session after about 10 s without data)
                        int count;
                        if (PcGroup)
                        {
                            count = ComputePcGroupColors(channelIds, rgb16);
                        }
                        else
                        {
                            // No frame from the capture yet (monitor waking, capture restarting): try again
                            if (!this.bInitDone && !InitHelpers())
                                continue;
                            count = Math.Min(brPixels.Length, HueStreamMessage.MaxChannels);
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
                        }
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
                        session?.Stop();
                        if (PcGroup)
                        {
                            session = null;
                            pcVersion = -1;     // set the area up again on Start
                        }
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
                pcLayout = null;
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

        /// <summary>
        /// Ends the thread and waits until the session is closed and the lights are off (app exit).
        /// The normal AbortThread waits only 100 ms and the process is killed right after.
        /// </summary>
        public void RequestShutdown()
        {
            this.SetSleepThread(false);
            this.SetAbortThread(true);
        }

        /// <summary>Waits for the thread after <see cref="RequestShutdown"/>; false on timeout.</summary>
        public bool WaitForShutdown(int timeoutMs)
        {
            Thread t = myThread;
            return t == null || !t.IsAlive || t.Join(Math.Max(0, timeoutMs));
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

        #region PC group
        private HuePcGroup.Layout pcLayout;
        private int[] pcRangeStart;
        private int[] pcRangeCount;
        private readonly Stopwatch pcRectsAge = new Stopwatch();

        /// <summary>
        /// Colors of the PC group: every channel averages the selected screen sectors of its light device
        /// (a light without sectors uses the region around its position). Returns the channel count.
        /// </summary>
        private int ComputePcGroupColors(byte[] channelIds, ushort[] rgb16)
        {
            HuePcGroup.Layout layout = pcLayout;
            if (layout == null)
                return 0;
            int count = Math.Min(layout.Channels.Count, HueStreamMessage.MaxChannels);

            // Rebuild the rectangles now and then: the sectors can be changed while streaming
            if (!bInitDone || pcRangeStart == null || pcRangeStart.Length != count || pcRectsAge.ElapsedMilliseconds > 500)
            {
                if (!WaitForFrame())
                    return 0;
                int width, height;
                lock (this.Frame.Sync)
                {
                    width = this.Frame.Width;
                    height = this.Frame.Height;
                }
                BuildPcGroupRectangles(width, height);
                bInitDone = true;
            }

            this.CalcColorsFromFrame(rectScanPixels.Length, brPixels, BuildPcGroupRectangles);

            for (int i = 0; i < count; i++)
            {
                CDevicePhilipsHueLight light = layout.Channels[i].Value;
                channelIds[i] = layout.Channels[i].Key;
                int r = 0, g = 0, b = 0, n = pcRangeCount[i];
                for (int k = pcRangeStart[i]; k < pcRangeStart[i] + n; k++)
                {
                    r += brPixels[k].R;
                    g += brPixels[k].G;
                    b += brPixels[k].B;
                }
                Color c = n > 0 ? Color.FromArgb(255, Math.Min(255, r / n), Math.Min(255, g / n), Math.Min(255, b / n)) : Color.Black;
                // The sliders of the light first (incl. its value gain), then those of the group
                c = ApplyLightSettings(c, light.ValueGainPercent, light.MinimumLuminosityPercent, light.ColorRedPercent,
                    light.ColorGreenPercent, light.ColorBluePercent, light.MaxBrightnessPercent);
                ToRgb16(c, rgb16, i * 3);
            }
            return count;
        }

        private void BuildPcGroupRectangles(int width, int height)
        {
            HuePcGroup.Layout layout = pcLayout;
            int count = layout == null ? 0 : Math.Min(layout.Channels.Count, HueStreamMessage.MaxChannels);
            var rects = new List<Rectangle>();
            pcRangeStart = new int[count];
            pcRangeCount = new int[count];
            for (int i = 0; i < count; i++)
            {
                List<Rectangle> sectors = layout.Channels[i].Value.SelectedSectorRectangles(width, height);
                if (sectors.Count == 0)
                {
                    EntertainmentChannel ch = layout.Area.Channels.Find(c => c.ID == layout.Channels[i].Key) ?? new EntertainmentChannel();
                    sectors.Add(ScanRectangle(ch, width, height, this.InnerSizePercent));
                }
                pcRangeStart[i] = rects.Count;
                pcRangeCount[i] = sectors.Count;
                rects.AddRange(sectors);
            }
            if (rects.Count == 0)
                rects.Add(Rectangle.Empty);

            rectScanPixels = rects.ToArray();
            brPixels = new Color[rectScanPixels.Length];
            aLeft = new int[rectScanPixels.Length];
            aRight = new int[rectScanPixels.Length];
            aTop = new int[rectScanPixels.Length];
            aBottom = new int[rectScanPixels.Length];
            for (int i = 0; i < rectScanPixels.Length; i++)
            {
                aLeft[i] = rectScanPixels[i].Left;
                aRight[i] = rectScanPixels[i].Right;
                aTop[i] = rectScanPixels[i].Top;
                aBottom[i] = rectScanPixels[i].Bottom;
            }
            this.iScanAreaWidth = width;
            this.iScanAreaHeight = height;
            lColorsFrameNumber = -1;    // calculate the new rectangles on the next call
            pcRectsAge.Restart();
        }

        private bool WaitForFrame()
        {
            int iWait = 0;
            while ((this.Frame == null || this.Frame.Pixels == null) && iWait <= 300 && !bAbortThread)
            {
                Thread.Sleep(100);
                iWait++;
            }
            return this.Frame != null && this.Frame.Pixels != null;
        }
        #endregion

        #region Pixel processing
        /// <summary>Applies the color and brightness sliders and widens 8 bit to 16 bit.</summary>
        private void ToRgb16(Color c, ushort[] target, int offset)
        {
            // Value gain and color boost were applied with the screen colors (CDevice.AdjustColor)
            Color o = ApplyLightSettings(c, 100, this.MinimumLuminosityPercent, this.ColorRedPercent,
                this.ColorGreenPercent, this.ColorBluePercent, this.MaxBrightnessPercent);
            target[offset] = (ushort)(o.R * 257);
            target[offset + 1] = (ushort)(o.G * 257);
            target[offset + 2] = (ushort)(o.B * 257);
        }

        /// <summary>
        /// Color pipeline of a Hue channel, in the order of the AmbiBack firmware (LEDHelper): value gain
        /// (raises the brightness, capped at 255), minimum brightness as lower limit per channel (percent of
        /// 255), then the red, green and blue shares and the maximum brightness.
        /// </summary>
        public static Color ApplyLightSettings(Color c, int valueGainPercent, int minimumPercent,
            int redPercent, int greenPercent, int bluePercent, int maxBrightnessPercent)
        {
            int r = c.R, g = c.G, b = c.B;
            if (valueGainPercent != 100)
            {
                r = r * valueGainPercent / 100;
                g = g * valueGainPercent / 100;
                b = b * valueGainPercent / 100;
            }
            int floor = Math.Max(0, Math.Min(100, minimumPercent)) * 255 / 100;
            r = Math.Max(floor, Math.Min(255, r));
            g = Math.Max(floor, Math.Min(255, g));
            b = Math.Max(floor, Math.Min(255, b));
            int max = Math.Max(0, Math.Min(100, maxBrightnessPercent));
            r = r * redPercent / 100 * max / 100;
            g = g * greenPercent / 100 * max / 100;
            b = b * bluePercent / 100 * max / 100;
            return Color.FromArgb(255, Math.Min(255, r), Math.Min(255, g), Math.Min(255, b));
        }

        /// <summary>Allocates the buffers once the capture delivers a frame; false when there is none after 30 s (the caller retries).</summary>
        private bool InitHelpers()
        {
            if (!WaitForFrame())
                return false;

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
            return true;
        }

        private void UpdateScanAreas(int width, int height)
        {
            List<EntertainmentChannel> snapshot;
            lock (channelsLock)
                snapshot = new List<EntertainmentChannel>(channels);
            snapshot = EffectivePositions(snapshot);
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
        /// Areas created without placing the lights (all channels at the same position, e.g. 0/0/0)
        /// would give every light the same region. Then spread the channels evenly from left to right
        /// at mid height, in channel order.
        /// </summary>
        public static List<EntertainmentChannel> EffectivePositions(List<EntertainmentChannel> list)
        {
            if (list.Count < 2)
                return list;
            bool same = true;
            foreach (EntertainmentChannel ch in list)
                if (Math.Abs(ch.X - list[0].X) > 0.01 || Math.Abs(ch.Z - list[0].Z) > 0.01)
                {
                    same = false;
                    break;
                }
            if (!same)
                return list;
            var spread = new List<EntertainmentChannel>(list.Count);
            for (int i = 0; i < list.Count; i++)
                spread.Add(new EntertainmentChannel { ID = list[i].ID, X = -1 + 2 * (i + 0.5) / list.Count, Y = list[i].Y, Z = 0 });
            return spread;
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
