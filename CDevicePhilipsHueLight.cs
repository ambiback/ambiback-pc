using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Custom
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Xml;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using AmbiBack.PhilipsHue;

namespace AmbiBack
{
    [Serializable]
    public class CDevicePhilipsHueLight : CDevice
    {
        bool bInitDone = false;

        #region public get/set variables
        public string HueBridge { get; set; }
        public string HueUniqueID { get; set; } = null;
        public string HueType { get; set; }
        public string HueModelID { get; set; }
        
        public byte[] ScreenSectors { get; set; } = null;
        public byte PixelVert { get; set; } = 3;
        public byte PixelHori { get; set; } = 3;

        // Reference to Hue Object!
        public Light HueLight { get; set; } = null;

        public void SetHueUniqueID(string szID) {
            // Try to retrieve Hue object
            HueUniqueID = szID;
            HueLight = CGlobals.GetHueLightByUniqueID(szID); 
        }
        #endregion


        public CDevicePhilipsHueLight()
        {
            // Set defaults 
            this.SetDeviceType(DeviceType.PhilipsHueColorLight);
            this.SetDeviceName("Unknown Controller");
            this.InitClass();
        }

        public CDevicePhilipsHueLight(DeviceType newDeviceType, string szDeviceName)
        {
            // Set device params
            this.SetDeviceType(newDeviceType);
            this.SetDeviceName(szDeviceName);
            this.InitClass();
        }

        #region Virtual Public functions
        public override void InitClass()
        {
            this.SetDeviceID();
            this.InitScreenSectors();

            // Set other defaults
            // Entertainment streaming (DTLS) through HueLampStreamer
            this.FramesPerSecond = 25;
            this.MaxFramesPerSecond = 50;
            this.StepFramesPerSecond = 5;

            this.FrameDelay = 200;
            this.MinFrameDelay = 100;
            this.MaxFrameDelay = 1000;
            this.StepFrameDelay = 100;

            // Enable supported 
            this.EditColorRedPercent = true;
            this.EditColorGreenPercent = true;
            this.EditColorBluePercent = true;
            this.EditColorBoostPercent = true;
            this.EditMaxBrightnessPercent = true;
            this.EditMinimumLuminosityPercent = true;

            this.EditFrameDelay = false;    // the bridge smooths the stream by itself
            this.EditFramesPerSecond = true;
        }

        // Process Payload data for devices
        public override void ProcessPayloadData(byte[] bData)
        {
            // TODO 
        }

        // Write Child XML Elements
        public override void WriteChildXml(XmlWriter writer)
        {
            try
            {
                // Write Common Hue Device properties 
                writer.WriteElementString("HueBridge", Properties.Settings.Default.szHueDeviceIP /* this.HueBridge */);
                writer.WriteElementString("HueUniqueID", this.HueUniqueID);
                writer.WriteElementString("HueType", this.HueType);
                writer.WriteElementString("HueModelID", this.HueModelID);
                this.WriteXmlElement(writer, "ScreenSectors", ScreenSectors); 

                this.WriteXmlElement(writer, "FramesPerSecond", this.FramesPerSecond);
                this.WriteXmlElement(writer, "InnerSizePercent", this.InnerSizePercent);
                this.WriteXmlElement(writer, "ColorBoostPercent", this.ColorBoostPercent);

                this.WriteXmlElement(writer, "ColorRedPercent", this.ColorRedPercent);
                this.WriteXmlElement(writer, "ColorGreenPercent", this.ColorGreenPercent);
                this.WriteXmlElement(writer, "ColorBluePercent", this.ColorBluePercent);
                this.WriteXmlElement(writer, "MaxBrightnessPercent", this.MaxBrightnessPercent);
                this.WriteXmlElement(writer, "MinimumLuminosityPercent", this.MinimumLuminosityPercent);
                this.WriteXmlElement(writer, "FrameLength", this.FrameLength);
                this.WriteXmlElement(writer, "FrameDelay", this.FrameDelay);
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
                // Read Common Hue Device properties 
                this.HueBridge = reader.ReadElementContentAsString("HueBridge", "");
                this.SetHueUniqueID( reader.ReadElementContentAsString("HueUniqueID", "") );
                this.HueType = reader.ReadElementContentAsString("HueType", "");
                this.HueModelID = reader.ReadElementContentAsString("HueModelID", "");
                this.ScreenSectors = this.ReadXmlElementAsByteArray(reader, "ScreenSectors");
                this.FramesPerSecond = this.ReadXmlElementAsByte(reader, "FramesPerSecond", 30);
                this.InnerSizePercent = this.ReadXmlElementAsByte(reader, "InnerSizePercent", 15);
                this.ColorBoostPercent = this.ReadXmlElementAsByte(reader, "ColorBoostPercent", 0);

                this.ColorRedPercent = this.ReadXmlElementAsByte(reader, "ColorRedPercent", 100);
                this.ColorGreenPercent = this.ReadXmlElementAsByte(reader, "ColorGreenPercent", 100);
                this.ColorBluePercent = this.ReadXmlElementAsByte(reader, "ColorBluePercent", 100);
                this.MaxBrightnessPercent = this.ReadXmlElementAsByte(reader, "MaxBrightnessPercent", 100);
                this.MinimumLuminosityPercent = this.ReadXmlElementAsByte(reader, "MinimumLuminosityPercent", 0);
                this.FrameLength = this.ReadXmlElementAsByte(reader, "FrameLength", 10);
                this.FrameDelay = this.ReadXmlElementAsInt(reader, "FrameDelay", 60);
                this.ValueGainPercent = this.ReadXmlElementAsInt(reader, "ValueGainPercent", 100);

                // Settings from the HTTP times (5 to 10 fps) are too slow for streaming
                if (this.FramesPerSecond < 15 || this.FramesPerSecond > this.MaxFramesPerSecond)
                    this.FramesPerSecond = 25;

                // Correct internal parameters
                this.CheckInternalValues(); 
            }
            catch (Exception ex)
            {
                CGlobals.HandleException("ReadXml", ex);
            }
            finally
            {
                if (ScreenSectors == null)
                    this.InitScreenSectors(); 
            }
        }
        public override void UpdateFrom(CDevice devCopyFrom)
        {
            CDevicePhilipsHueLight myDevCopyFrom = (CDevicePhilipsHueLight)devCopyFrom;

            // Copy Common properties
            this.HueBridge = myDevCopyFrom.HueBridge;
            this.SetHueUniqueID(myDevCopyFrom.HueUniqueID);
            this.HueType = myDevCopyFrom.HueType;
            this.HueModelID = myDevCopyFrom.HueModelID;
        }
        public override void CopyFrom(CDevice devCopyFrom)
        {
            CDevicePhilipsHueLight myDevCopyFrom = (CDevicePhilipsHueLight)devCopyFrom;

            // Internal Properties
            this.bIsOnline = myDevCopyFrom.bIsOnline;

            // Copy Common properties
            this.HueBridge = myDevCopyFrom.HueBridge;
            this.SetHueUniqueID(myDevCopyFrom.HueUniqueID);
            this.HueType = myDevCopyFrom.HueType;
            this.HueModelID = myDevCopyFrom.HueModelID;
            this.ScreenSectors = myDevCopyFrom.ScreenSectors;

            this.FramesPerSecond = myDevCopyFrom.FramesPerSecond;
            this.InnerSizePercent = myDevCopyFrom.InnerSizePercent;
            this.ColorBoostPercent = myDevCopyFrom.ColorBoostPercent;

            this.ColorRedPercent = myDevCopyFrom.ColorRedPercent;
            this.ColorGreenPercent = myDevCopyFrom.ColorGreenPercent;
            this.ColorBluePercent = myDevCopyFrom.ColorBluePercent;
            this.MaxBrightnessPercent = myDevCopyFrom.MaxBrightnessPercent;
            this.MinimumLuminosityPercent = myDevCopyFrom.MinimumLuminosityPercent;
            this.FrameLength = myDevCopyFrom.FrameLength;
            this.FrameDelay = myDevCopyFrom.FrameDelay;
            this.ValueGainPercent = myDevCopyFrom.ValueGainPercent;
        }
        public override void SetDeviceID()
        {
            /* Generate Device ID from these values */
            szDeviceID = CGlobals.DEVICETYPES[(int)this.GetDeviceType()] + "_" +
                             this.GetDeviceName() + "_" +
                            (this.HueUniqueID != null ? this.HueUniqueID : "");
        }

        #endregion

        #region Virtual Public Threading functions
        /// <summary>
        /// Computes the color of the selected screen sectors and hands it to <see cref="HueLampStreamer"/>,
        /// which streams all running lights over the Entertainment API (DTLS). No HTTP color commands.
        /// </summary>
        public override void HandleThread()
        {
            DoDebugOutput("CDevicePhilipsHueLight|HandleThread|Begin Thread");
            Stopwatch frameClock = Stopwatch.StartNew();

            try
            {
                HueLampStreamer.Register(this);
                while (!bAbortThread)
                {
                    try
                    {
                        long frameStart = frameClock.ElapsedMilliseconds;

                        if (!this.bInitDone)
                            InitHelpers();

                        // Colors of all sectors from the monitor's frame buffer; skipped when there is no new frame
                        if (this.rectScanPixels != null)
                            this.CalcColorsFromFrame(this.ScreenSectors.Length, this.brPixels, this.UpdateScanAreas);

                        HueLampStreamer.SetColor(this, CurrentColor());

                        int fps = Math.Max(1, Math.Min((int)this.FramesPerSecond, (int)this.MaxFramesPerSecond));
                        int sleep = 1000 / fps - (int)(frameClock.ElapsedMilliseconds - frameStart);
                        if (sleep > 0)
                            Thread.Sleep(sleep);
                    }
                    catch (ThreadInterruptedException)
                    {
                        // Stop button: leave the stream (the streamer switches the light off), wait for Start
                        DoDebugOutput("CDevicePhilipsHueLight|HandleThread|Interrupted");
                        HueLampStreamer.Unregister(this);
                        while (bSleepThread && !bAbortThread)
                            Thread.Sleep(10);
                        if (!bAbortThread)
                        {
                            DoDebugOutput("CDevicePhilipsHueLight|HandleThread|Restart");
                            HueLampStreamer.Register(this);
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {
                DoDebugOutput("CDevicePhilipsHueLight|HandleThread|Aborted");
                this.SetAbortThread(true);
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
                DoDebugOutput("CDevicePhilipsHueLight|HandleThread|Exception: " + ex.Message);
                this.SetAbortThread(true);
            }
            finally
            {
                HueLampStreamer.Unregister(this);
                bInitDone = false;
            }

            DoDebugOutput("CDevicePhilipsHueLight|HandleThread|Finished Thread");
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
                DoDebugOutput("CDevicePhilipsHueLight|StartThread|Exception: " + ex.Message);
            }
        }

        public void SendFrameDelay()
        {
            // Streaming: the bridge smooths by itself, nothing to send
        }

        public void SendDeviceName()
        {
            // The name is managed in the Hue app
        }

        /// <summary>Average of the selected sectors with the color and brightness sliders applied.</summary>
        private Color CurrentColor()
        {
            if (brPixels == null || ScreenSectors == null)
                return Color.Black;
            int r = 0, g = 0, b = 0, n = 0;
            for (int i = 0; i < ScreenSectors.Length && i < brPixels.Length; i++)
            {
                if (ScreenSectors[i] == 1)
                {
                    r += brPixels[i].R;
                    g += brPixels[i].G;
                    b += brPixels[i].B;
                    n++;
                }
            }
            if (n == 0)
                return Color.Black;
            int max = this.MaxBrightnessPercent;
            r = r / n * this.ColorRedPercent / 100 * max / 100;
            g = g / n * this.ColorGreenPercent / 100 * max / 100;
            b = b / n * this.ColorBluePercent / 100 * max / 100;
            return Color.FromArgb(255, Math.Min(255, r), Math.Min(255, g), Math.Min(255, b));
        }
        #endregion

        #region public functions
        public void InitScreenSectors()
        {
            // Create sectors byte array if NULL
            int iSectors = (this.PixelHori * 2) + (this.PixelVert * 2);
            this.ScreenSectors = new byte[iSectors];
            // Preselect sectors
            this.ScreenSectors[0] = 1;
            this.ScreenSectors[1] = 1;
            this.ScreenSectors[2] = 1;
        }
        /// <summary>Scan rectangles for a screenshot of this size (also after a resolution change).</summary>
        private void UpdateScanAreas(int iScreenWidth, int iScreenHeight)
        {
            this.CalcRectangles(0, 0, iScreenWidth, iScreenHeight, 0, this.rectScanPixels);
            this.CalcCorners(this.rectScanPixels);
            this.iScanAreaWidth = iScreenWidth;
            this.iScanAreaHeight = iScreenHeight;
        }

        public void InitHelpers()
        {
            // Wait for first screenshot
            int iWait = 0;
            while ((this.Frame == null || this.Frame.Pixels == null) && iWait <= 300)
            {
                Thread.Sleep(100);
                iWait++;
            }

            /* Calc rectPixels from base image */
            if (this.Frame != null && this.Frame.Pixels != null)
            {
                try
                {
                    int iScreenWidth;
                    int iScreenHeight;
                    lock (this.Frame.Sync)
                    {
                        iScreenWidth = this.Frame.Width;
                        iScreenHeight = this.Frame.Height;
                    }

                    // Init Pixels Brush helper variables which will contain the color codes!
                    brPixels = new Color[this.ScreenSectors.Length];
                    lColorsFrameNumber = -1;   // new (black) colors: calculate on the next call

                    // Init Screen Scan Areas
                    rectScanPixels = new Rectangle[this.ScreenSectors.Length];
                    this.UpdateScanAreas(iScreenWidth, iScreenHeight);

                    // Init finished
                    this.bInitDone = true;
                }
                catch (Exception ex)
                {
                    CGlobals.DebugOutput(ex);
                    throw (ex);
                }
            }
            else
                throw (new Exception("ERROR COULD NOT GRAB SCREENSHOT"));
        }
        #endregion

        #region Pixel Processing functions 
        public void CalcCorners(System.Drawing.Rectangle[] rectPixels)
        {
            aLeft = new int[this.ScreenSectors.Length];
            aRight = new int[this.ScreenSectors.Length];
            aTop = new int[this.ScreenSectors.Length];
            aBottom = new int[this.ScreenSectors.Length];

            // Loop through all Scan rectangles
            for (int n = 0; n < this.ScreenSectors.Length; n++)
            {
                System.Drawing.Rectangle myRect = rectPixels[n];
                aLeft[n] = myRect.Left;
                aRight[n] = myRect.Right;
                aTop[n] = myRect.Top;
                aBottom[n] = myRect.Bottom;
            }
        }

        public void CalcRectangles(int iLeftOffset, int iTopOffset, int iScanWidth, int iScanHeight, int iIntoImagePx, System.Drawing.Rectangle[] rectPixels)
        {
            int iOffset = 0;

            // CounterClockWise Bottom Right To Up
            iOffset = CalcRightRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.BottomUp, rectPixels, iOffset);
            iOffset = CalcTopRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.RightToLeft, rectPixels, iOffset);
            iOffset = CalcLeftRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.TopDown, rectPixels, iOffset);
            iOffset = CalcBottomRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.LeftToRight, rectPixels, iOffset);
        }


        #endregion

    }
}
