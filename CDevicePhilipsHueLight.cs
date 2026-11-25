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

        #region public get/set variables
        public string HueBridge { get; set; }
        public string HueUniqueID { get; set; } = null;
        public string HueType { get; set; }
        public string HueModelID { get; set; }
        
        public byte[] ScreenSectors { get; set; } = null;
        public byte PixelVert { get; set; } = 3;
        public byte PixelHori { get; set; } = 3;

        /// <summary>Member of the PC entertainment group (checkbox in the device list).</summary>
        public bool InGroup { get; set; } = false;

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
            // Entertainment streaming (DTLS) through the PC group (HuePcGroup)
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

            this.EditFrameDelay = false;       // the bridge smooths the stream by itself
            this.EditFramesPerSecond = false;  // the group device sets the frame rate
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
                this.WriteXmlElement(writer, "InGroup", this.InGroup);
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
                if (reader.IsStartElement("InGroup"))
                    this.InGroup = this.ReadXmlElementAsBool(reader, "InGroup");

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
            this.InGroup = myDevCopyFrom.InGroup;

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
        /// Single lights do not stream by themselves: they are members of the PC group
        /// (<see cref="HuePcGroup"/>), whose device streams all of them in one DTLS session.
        /// </summary>
        public override void HandleThread()
        {
        }

        public override void StartThread()
        {
            DoDebugOutput("CDevicePhilipsHueLight|" + this.GetDeviceName() + " streams through the group \"" + HuePcGroup.AreaName + "\"");
        }

        public void SendFrameDelay()
        {
        }

        public void SendDeviceName()
        {
        }

        /// <summary>Screen rectangles of the selected sectors for a frame of this size.</summary>
        public List<Rectangle> SelectedSectorRectangles(int width, int height)
        {
            var all = new Rectangle[(this.PixelHori * 2) + (this.PixelVert * 2)];
            this.CalcRectangles(0, 0, width, height, 0, all);
            var result = new List<Rectangle>();
            for (int i = 0; ScreenSectors != null && i < all.Length && i < ScreenSectors.Length; i++)
                if (ScreenSectors[i] == 1)
                    result.Add(all[i]);
            return result;
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
