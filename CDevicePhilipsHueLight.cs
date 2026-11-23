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
using System.Windows.Forms;
using AmbiBack.PhilipsHue;

namespace AmbiBack
{
    [Serializable]
    public class CDevicePhilipsHueLight : CDevice
    {
        bool bInitDone = false;
        bool bTurnedOn = false; 

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
            this.FramesPerSecond = 5;
            this.MaxFramesPerSecond = 10;
            this.StepFramesPerSecond = 1; 

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

            this.EditFrameDelay = true;
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
        public override void HandleThread()
        {
            DoDebugOutput("CDevicePhilipsHueLight|HandleThread|Begin Thread");

            while (bAbortThread == false)
            {
                try
                {
                    int iFrameTimeMs = 1000 / FramesPerSecond;
                    int iLastSecond = DateTime.Now.Second;
                    int iLastMilliSec = DateTime.Now.Millisecond;
                    int iFramesPerSec = 0;

                    while (true)
                    {
                        if (!this.bTurnedOn)
                            this.TurnLightOn(); 

                        // (RE)Init Helper Variables!
                        if (!this.bInitDone)
                        {
                            try
                            {
                                InitHelpers();
                            }
                            catch (Exception ex)
                            {
                                CGlobals.DebugOutput(ex);
                            }
                        }

                        if (this.bmpScreenCopy != null)
                        {
                            // Scan Images by each Pixel for Colors
                            this.CalcColorsRectangles((byte)this.ScreenSectors.Length, this.bmpScreenCopy, brPixels);
                        }

                        // Finally send calced pixels to DS!
                        this.SendLEDData();

                        // Do Frame time Processing
                        if (DateTime.Now.Second == iLastSecond)
                        {
                            // Add little sleeptime if necessary!
                            int iMillSecondsDone = DateTime.Now.Millisecond - iLastMilliSec;
                            if (iMillSecondsDone < iFrameTimeMs)
                                Thread.Sleep(iFrameTimeMs - iMillSecondsDone);

                            // Safe last millsec now
                            iLastMilliSec = DateTime.Now.Millisecond;

                            // Increment Counter
                            iFramesPerSec++;
                        }
                        else
                        {
                            DoUpdateStatus("Framerate send: " + iFramesPerSec + " - Target: " + this.GetDeviceName());

                            // Reset variables
                            iLastSecond = DateTime.Now.Second;
                            iLastMilliSec = DateTime.Now.Millisecond;
                            iFramesPerSec = 0;

                            // Update Frame Sleeptime 
                            iFrameTimeMs = 1000 / FramesPerSecond;
                        }

                        // Loop to avoid running wenn Monitor is turned off
                        while (!CGlobals.monitorPowerStatus)
                        {
                            // Turn light off
                            if (this.bTurnedOn)
                                this.TurnLightOff();

                            // Wait for monitor to come back online
                            CGlobals.DebugOutput("CDevicePhilipsHueLight:HandleThread:WaitForMonitorLoop");

                            // Sleep for a second
                            Thread.Sleep(1000);
                        }
                    }
                }
                catch (ThreadInterruptedException)
                {
                    // Interrupted
                    DoDebugOutput("CDevicePhilipsHueLight|HandleThread|Interrupted");

                    while (bSleepThread)
                    {
                        // Use Sleep instead of SpinWait 
                        Thread.Sleep(1);
                        // Thread.SpinWait(10000000);
                    }

                    // Interrupted
                    DoDebugOutput("CDevicePhilipsHueLight|HandleThread|Restart");

                    // this.SetAbortThread(true);
                }
                catch (ThreadAbortException)
                {
                    // Aborted
                    DoDebugOutput("CDevicePhilipsHueLight|HandleThread|Aborted");

                    this.SetAbortThread(true);
                }
                catch (Exception ex)
                {
                    CGlobals.DebugOutput(ex);
                    DoDebugOutput("CDevicePhilipsHueLight|HandleThread|Exception: " + ex.ToString());

                    this.SetAbortThread(true);
                }
                finally
                {
                    // Turn light off
                    if (this.bTurnedOn)
                        this.TurnLightOff();
                }
            }

            DoDebugOutput("CDevicePhilipsHueLight|HandleThread|Finished Thread");
        }
        public override void StartThread()
        {
            try
            {
                // Reset Abort Thread
                this.SetAbortThread(false);
                this.SetSleepThread(false);

                // Create new Thread each time we start!
                CreateThread();

                if (myThread.ThreadState == System.Threading.ThreadState.Unstarted)
                {
                    // Start the Thread
                    myThread.Start();
                }
                else if (myThread.ThreadState == System.Threading.ThreadState.Stopped)
                {
                    myThread.Join(500);    // Wait max 1 second until the thread is gone...
                }
                else if (myThread.ThreadState == System.Threading.ThreadState.Running)
                {
                    // Continue thread
                    this.SetSleepThread(false);
                }

                else if (myThread.ThreadState == System.Threading.ThreadState.Aborted)
                {
                    // Start the Thread
                    myThread.Start();
                }
                else
                {
                    DoDebugOutput("CDevicePhilipsHueLight|StartThread|Unexpected Thread STATE: " + myThread.ThreadState.ToString());
                }
            }
            catch (System.InvalidOperationException)
            {
                // Bla we don't care
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
                DoDebugOutput("CDevicePhilipsHueLight|StartThread|Exception: " + ex.ToString());
            }
        }
        private void TurnLightOff()
        {
            try
            {
                if (HueLight != null)
                {
                    /* Send new Color State */
                    LightStateBuilder lightStateBuilder = new LightStateBuilder();
                    lightStateBuilder.TurnOff();

                    // lightStateBuilder.ColorTemperature
                    HueLight.SetState(lightStateBuilder);
                    this.bTurnedOn = false;
                }
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
                DoDebugOutput("TurnLightOff|Exception: " + ex.ToString());
            }
        }
        private void TurnLightOn()
        {
            try
            {
                if (HueLight != null)
                {
                    /* Send new Color State */
                    LightStateBuilder lightStateBuilder = new LightStateBuilder();
                    lightStateBuilder.TurnOn();
                    lightStateBuilder.TransitionTime((ushort)(FrameDelay >= 100 ? (FrameDelay <= 1000 ? FrameDelay / 100 : 10) : 1)); //(/ 2);
                    lightStateBuilder.ColorTemperature(150);

                    // lightStateBuilder.ColorTemperature
                    HueLight.SetState(lightStateBuilder);
                    this.bTurnedOn = true;
                }
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
                DoDebugOutput("TurnLightOn|Exception: " + ex.ToString());
            }
        }
        public void SendFrameDelay()
        {
            if (HueLight != null)
            {
                // Get TransTime Value for Hue API
                ushort usTransTime = (ushort)(FrameDelay >= 100 ? (FrameDelay <= 1000 ? FrameDelay / 100 : 10) : 1);
            }
        }
        public void SendDeviceName()
        {
            // TODO 
            if (HueLight != null)
            {
                try
                {
                }
                catch (Exception ex)
                {
                    CGlobals.DebugOutput(ex);
                    DoDebugOutput("SendDeviceName|Exception: " + ex.ToString());
                }
            }
        }
        private void SendLEDData()
        {
            // Payload helpers 
            try
            {
                if (HueLight != null) {
                    int iRed = 0;
                    int iGreen = 0;
                    int iBlue = 0;
                    int iSectors = 0;

                    /* Loop through enabled sectors*/
                    for (int iNum = 0; iNum < ScreenSectors.Length; iNum++)
                    {
                        if (ScreenSectors[iNum] == 1)
                        {
                            iRed += ((SolidBrush)brPixels[iNum]).Color.R;
                            iGreen += ((SolidBrush)brPixels[iNum]).Color.G;
                            iBlue += ((SolidBrush)brPixels[iNum]).Color.B;
                            iSectors++; 
                        }
                    }

                    /* Calc avarage color */
                    iRed = (iRed > 0 ? iRed / iSectors : 0);
                    iGreen = (iGreen > 0 ? iGreen / iSectors : 0);
                    iBlue = (iBlue > 0 ? iBlue / iSectors : 0);

                    // Calc Color percent rate here!
                    if (this.ColorRedPercent < 100)
                        iRed = (iRed * this.ColorRedPercent) / 100;
                    if (this.ColorGreenPercent < 100)
                        iGreen = (iGreen * this.ColorGreenPercent) / 100;
                    if (this.ColorBluePercent < 100)
                        iBlue = (iBlue * this.ColorBluePercent) / 100;
                    
                    // Create final Brush
                    Brush myBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, iRed, iGreen, iBlue));

                    /* Send new Color State */
                    LightStateBuilder lightStateBuilder = new LightStateBuilder();
                    /*
                     * 	The duration of the transition from the light’s current state to the new state. 
                     * 	This is given as a multiple of 100ms and defaults to 4 (400ms). 
                     * 	For example, setting transitiontime:10 will make the transition last 1 second.
                     * */
                    lightStateBuilder.Color(((SolidBrush)myBrush).Color);
                    if (iRed == 0 && iGreen == 0 && iBlue == 0)
                        lightStateBuilder.Brightness(0);
                    else
                        lightStateBuilder.Brightness((byte)((this.MaxBrightnessPercent * 255) / 100));

                    HueLight.SetState(lightStateBuilder);
                }

                    // UNCOMMENT VERBOSE
                    //                DoDebugOutput("CDevicePhilipsHueLight|AmbientManagerSendLEDData: TcpClient received '" + responseData + "'");
                }
            catch (ThreadAbortException ex)
            {
                // Aborted
                DoDebugOutput("CDevicePhilipsHueLight|SendLEDData|Aborted");
                throw (ex);
            }
            catch (IOException ex)
            {
                // Happens on disconnect 
                CGlobals.DebugOutput(ex);
            }
            catch (Exception ex)
            {
                if (brPixels == null)
                    throw (new Exception("ERROR PIXELS NOT INITIALIZED"));

                CGlobals.DebugOutput(ex);
            }
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
        public void InitHelpers()
        {
            // Wait for first screenshot
            int iWait = 0;
            while (this.bmpScreenCopy == null && iWait <= 300)
            {
                Thread.Sleep(100);
                iWait++;
            }

            /* Calc rectPixels from base image */
            if (this.bmpScreenCopy != null)
            {
                try
                {
                    int iScreenWidth;
                    int iScreenHeight;
                    lock (CGlobals.bmpLock)
                    {
                        iScreenWidth = this.bmpScreenCopy.Width;
                        iScreenHeight = this.bmpScreenCopy.Height;
                    }

                    // Init Pixels Brush helper variables which will contain the color codes!
                    brPixels = new Brush[this.ScreenSectors.Length];

                    // Init Screen Scan Areas
                    rectScanPixels = new Rectangle[this.ScreenSectors.Length];
                    this.CalcRectangles(0, 0, iScreenWidth, iScreenHeight, 0, this.rectScanPixels);
                    this.CalcCorners(this.rectScanPixels);

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
