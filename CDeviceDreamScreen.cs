using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Custom
using System.Runtime.InteropServices;
using System.IO; 
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace AmbiBack
{
    #region Public Enums
    public enum DsCommands
    {
        DsName = 0x0107,
        GroupName = 0x0108,
        GroupNumber = 0x0109,
        CurrentState = 0x010A,              /* Current State R 0x01 0x0A Dump of all attributes Variable
                                                Read current state’ response: The payload received varies by what the device is and the
                                                version of firmware. However, the last index of the payload always contains the productId of the
                                                device, which you should use to determine the proper context of the payload. Currently
                                                DreamScreen can send up to 140 bytes; SideKick can send up to 62.
                                                Product ID Device
                                                0x01 DreamScreen HD
                                                0x02 DreamScreen 4K
                                                0x03 SideKick

                                                DreamScreen HD and 4K Current State Payload
                                                Index Size (Bytes) Attribute
                                                0-15 16 (UTF8) Name
                                                16-31 16 (UTF8) Group Name
                                                32 1 Group Number
                                                33 1 Mode
                                                34 1 Brightness
                                                40-42 3 Ambient Color
                                                62 1 Ambient Scene
                                                73 1 HDMI Input
                                                75-90 16 (UTF8) HDMI Input Name 1
                                                91-106 16 (UTF8) HDMI Input Name 2
                                                107-122 16 (UTF8) HDMI Input Name 3
                                                129 1 HDMI Active Channels

                                                SideKick Current State Payload
                                                Index Size (Bytes) Attribute
                                                0-15 16 (UTF8) Name
                                                16-31 16 (UTF8) Group Name
                                                32 1 Group Number
                                                33 1 Mode
                                                34 1 Brightness
                                                35-37 3 Ambient Color
                                                60 1 Ambient Scene
                                            */
        HDMIInputName1 = 0x0123,
        HDMIInputName2 = 0x0124,
        HDMIInputName3 = 0x0125,
        HDMIActiveChannels = 0x012C,

        Mode = 0x0301,                      /*  0x00 - Idle Mode
			                                    0x01 - Video Mode
			                                    0x02 - Music Mode
			                                    0x03 - Ambient Mode
                                                0x04 - Identity Mode
			                                    0x05 - Direct Pixel Write
                                            */
        Brightness = 0x0302,
        AmbientColor = 0x0305,
        AmbientModeType = 0x0308,

        MusicModeColors = 0x030A,           // (0-2)(0-2)(0-2) 0-Red, 1 - Green 2 - Blue for Treble, Middle, Base
        MusicModeWeights = 0x030B,          // (0-100)(0-100)(0-100) - Treble, Middle, Bss
        AmbientScene = 0x030D,

        PixelVideoAddressing = 0x0315,      // Pixel Video Mode Addressing  0x0315 RW  (Start Pixel); (length) (RGB),(RGB),(RGB)........
        App_Music_Data = 0x0322,            // 8 Bytes (LEFT Bass, Mid, Treble, Energy; RIGHT Bass, Mid, Treble, Energy)
        PixelMusicAddressing = 0x0322,      // Music Video Mode Addressing  0x0322 RW  (Start Pixel); (length) (RGB),(RGB),(RGB)........

        Temperature = 0x0329,               // read one byte celsius

        Advanced_Color_Boost = 0x032D, 	    // 0 - Disable, 1 - Low, 2 - Medium, 3 - High
        Advanced_CEC_Power_Detect = 0x032E, // 0 - Disable, 1 - Enable

        Install_SKU_Setup = 0x0340,         /* 	0 - Classic
			                                    1 - Mega
			                                    2 - Xtreme
			                                    3 - Flex/DIY 
                                            */
        Install_Flex_Setup = 0x0341,        /*  (005 - 045)(010-080) (Left Count)(Right Count)(Left Start)(Right Start)- Vertical/Horizontal LEDs
                                                Left/Right Start: (Based on Back of TV)
			                                    0x01 - Right Cclock - bottom right up
			                                    0x02 - Top Cclock - top right left
			                                    0x03 - Left Cclock - top left down
			                                    0x04 - Bottom Cclock - bottom left right
			                                    0x05 - Right Clock - top right down
			                                    0x06 - Top Clock - top left right
			                                    0x07 - Left Clock - bottom left up
			                                    0x08 - Bottom Clock - bottom right left 
                                            */


    }
    public enum DsFlags
    {
        Receive = 0x30, /*BroadCast?!*/
        Read = 0x11,
        Write = 0x21,
    }
    public enum DsModes
    {
        Sleep = 0x0,
        Video = 0x1,
        Music = 0x2,
        Ambient = 0x3,
    }
    public enum DsHdmiInput
    {
        Input1 = 0x0,
        Input2 = 0x1,
        Input3 = 0x2,
    }
    public enum DsDeviceType
    {
        DreamScreenHD = 0x01,
        DreamScreen4K = 0x02,
        DreamScreen4KSolo = 0x07,
        DreamScreenSideKick = 0x03,
    }
    public enum DsDeviceSkuSetup
    {
        Classic = 0x00,
        Mega = 0x01,
        Xtreme = 0x02,
        FlexDIY = 0x03,
    }
    public enum DsFlexSetups
    {
        Right_Cclock_bottom_right_up = 0x01,
        Top_Cclock_top_right_left = 0x02,
        Left_Cclock_top_left_down = 0x03,
        Bottom_Cclock_bottom_left_right = 0x04,
        Right_Clock_top_right_down = 0x05,
        Top_Clock_top_left_right = 0x06,
        Left_Clock_bottom_left_up = 0x07,
        Bottom_Clock_bottom_right_left = 0x08,
    }
    #endregion

    [StructLayout(LayoutKind.Sequential)]
    public struct DreamScreenPacket
    {
        public byte bStartPacket;   // Start of Packet - is used to provide synchronization when parsing packets. Always 0xFC
        public byte bPacketLength;  // Packet Length - Packet Length from Group Address (inclusive) to CRC (inclusive)
        public byte bGroupAddress;  // Group Address - the group number which the device belongs. 0x00 indicates ‘No specified Group’, 0x01 indicates group 1, 0x02 indicates group 2, etc. If the Group Address is incorrect, DreamScreen will discard the message.
        public byte bFlags;         // Flags - provides context for handling the message
        public UInt16 u16Command;     // Command Upper/lower combined - specifies command namespace
                                      //        public byte bCommandLower;  // Command Lower - specifies individual command within namespace
        public byte[] bPayload;     // Payload - variable length, depending upon the context of the command
        public byte bChecksum;      // CRC - 8 bit CRC for error detection. If incorrect, DreamScreen will discard the message.
    };
    // Example, Set Mode 0:
    //  FC:06:01:21:03:01:00:C6.Command is 03:01, with payload 0x00, packet length 0x06


    [Serializable]
    public class CDeviceDreamScreen : CDevice
    {
        // Global Screen Bitmaps Helpers
        public Bitmap bmpPrimScreen = null;
        public Bitmap bmpLeftScreen = null;
        public Bitmap bmpRightScreen = null;

        #region public get/set variables
        public string GroupName { get; set; }
        public byte GroupNumber { get; set; }
        public byte Mode { get; set; }

        public byte Brightness { get; set; }
        public Color AmbientColor { get; set; }
        public byte AmbientScene { get; set; }

        public byte HDMIInput { get; set; }
        public string HDMIInputName1 { get; set; }
        public string HDMIInputName2 { get; set; }
        public string HDMIInputName3 { get; set; }
        public byte HDMIActiveChannels { get; set; }

        public byte InstallationType { get; set; } = (byte)DsDeviceSkuSetup.Classic;
        public byte FlexSetupPort1 { get; set; } = (byte)DsFlexSetups.Left_Clock_bottom_left_up;
        public byte FlexSetupPort2 { get; set; } = (byte)DsFlexSetups.Left_Clock_bottom_left_up;
        public byte PixelTotalPort1 { get; set; } = 48;
        public byte PixelTotalPort2 { get; set; } = 48;
        public byte PixelVert { get; set; } = 8;
        public byte PixelHori { get; set; } = 16;

        public byte MaxBytesPerPacket { get; set; } = 196;
        public bool SplitPackets { get; set; } = false;
        #endregion

        // Constructor
        public CDeviceDreamScreen()
        {
            // Set defaults 
            this.SetDeviceType(DeviceType.UnknownDevice);
            this.SetDeviceName("Unknown Device");
            this.SetDeviceID();
        }
        public CDeviceDreamScreen(DeviceType newDeviceType, string szDeviceName)
        {
            // Set device params
            this.SetDeviceType(newDeviceType);
            this.SetDeviceName(szDeviceName);
            this.SetDeviceID();
        }

        #region Virtual Public functions
        public override void InitClass()
        {
            this.SetDeviceID();

            // Set other defaults
            this.FramesPerSecond = 10;
            this.MaxFramesPerSecond = 30;

            // Enable supported 
        }
        // Process Payload data for DreamScreen devices
        public override void ProcessPayloadData(byte[] bData)
        {
            if (m_DeviceType == DeviceType.DreamScreenHD ||
                m_DeviceType == DeviceType.DreamScreen4K ||
                m_DeviceType == DeviceType.DreamScreen4KSolo)
            {
                GroupName = Encoding.UTF8.GetString(bData, 16, 16).Trim(new char[] { (char)0 });
                GroupNumber = bData[32];

                Mode = bData[33];
                Brightness = bData[34];
                // What is 35-39
                AmbientColor = Color.FromArgb(bData[40], bData[41], bData[42]);
                // What is 43-61
                AmbientScene = bData[62];
                // What is 63-72
                HDMIInput = bData[73];
                HDMIInputName1 = Encoding.UTF8.GetString(bData, 75, 16).Trim(new char[] { (char)0 }).Trim();
                HDMIInputName2 = Encoding.UTF8.GetString(bData, 91, 16).Trim(new char[] { (char)0 }).Trim();
                HDMIInputName3 = Encoding.UTF8.GetString(bData, 107, 16).Trim(new char[] { (char)0 }).Trim();
                // What is 108-128
                HDMIActiveChannels = bData[129];
            }
            else if (m_DeviceType == DeviceType.DreamScreenSideKick)
            {
                GroupName = Encoding.UTF8.GetString(bData, 16, 16).Trim(new char[] { (char)0 });
                GroupNumber = bData[32];

                Mode = bData[33];
                Brightness = bData[34];
                AmbientColor = Color.FromArgb(bData[35], bData[36], bData[37]);
                // What is 38-59
                AmbientScene = bData[60];
            }
            else
            {
                throw (new Exception("Unknown device Type: " + m_DeviceType.ToString()));
            }
        }

        // Write Child XML Elements
        public override void WriteChildXml(XmlWriter writer)
        {
            try
            {
                // Write Common DreamScreen Device properties 
                writer.WriteElementString("GroupName", this.GroupName);
                this.WriteXmlElement(writer, "GroupNumber", this.GroupNumber);
                this.WriteXmlElement(writer, "Mode", this.Mode);

                this.WriteXmlElement(writer, "Brightness", this.Brightness);
                this.WriteXmlElement(writer, "AmbientColor", this.AmbientColor.ToArgb());
                this.WriteXmlElement(writer, "AmbientScene", this.AmbientScene);

                if (m_DeviceType == DeviceType.DreamScreenHD ||
                    m_DeviceType == DeviceType.DreamScreen4K ||
                    m_DeviceType == DeviceType.DreamScreen4KSolo)
                {
                    // Write DreamScreen Device Controller properties 
                    this.WriteXmlElement(writer, "HDMIInput", this.HDMIInput);
                    writer.WriteElementString("HDMIInputName1", this.HDMIInputName1);
                    writer.WriteElementString("HDMIInputName2", this.HDMIInputName2);
                    writer.WriteElementString("HDMIInputName3", this.HDMIInputName3);
                    this.WriteXmlElement(writer, "HDMIActiveChannels", this.HDMIActiveChannels);

                    this.WriteXmlElement(writer, "InstallationType", this.InstallationType);
                    this.WriteXmlElement(writer, "FlexSetupPort1", this.FlexSetupPort1);
                    this.WriteXmlElement(writer, "FlexSetupPort2", this.FlexSetupPort2);
                    this.WriteXmlElement(writer, "PixelTotalPort1", this.PixelTotalPort1);
                    this.WriteXmlElement(writer, "PixelTotalPort2", this.PixelTotalPort2);
                    this.WriteXmlElement(writer, "PixelVert", this.PixelVert);
                    this.WriteXmlElement(writer, "PixelHori", this.PixelHori);

                    this.WriteXmlElement(writer, "MaxBytesPerPacket", this.MaxBytesPerPacket);
                    this.WriteXmlElement(writer, "SplitPackets", this.SplitPackets);
                    this.WriteXmlElement(writer, "FramesPerSecond", this.FramesPerSecond);
                    this.WriteXmlElement(writer, "InnerSizePercent", this.InnerSizePercent);
                    this.WriteXmlElement(writer, "ColorBoostPercent", this.ColorBoostPercent);
                }

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
                // Read Common DreamScreen Device properties 
                this.GroupName = reader.ReadElementContentAsString("GroupName", "");
                this.GroupNumber = this.ReadXmlElementAsByte(reader, "GroupNumber", 0);
                this.Mode = this.ReadXmlElementAsByte(reader, "Mode", 0);

                this.Brightness = this.ReadXmlElementAsByte(reader, "Brightness", 100);
                this.AmbientColor = Color.FromArgb((reader.ReadElementContentAsInt("AmbientColor", "")));
                this.AmbientScene = this.ReadXmlElementAsByte(reader, "AmbientScene", 0);

                if (m_DeviceType == DeviceType.DreamScreenHD ||
                    m_DeviceType == DeviceType.DreamScreen4K ||
                    m_DeviceType == DeviceType.DreamScreen4KSolo)
                {
                    // Read DreamScreen Device Controller properties 
                    this.HDMIInput = this.ReadXmlElementAsByte(reader, "HDMIInput", 1);
                    this.HDMIInputName1 = reader.ReadElementContentAsString("HDMIInputName1", "");
                    this.HDMIInputName2 = reader.ReadElementContentAsString("HDMIInputName2", "");
                    this.HDMIInputName3 = reader.ReadElementContentAsString("HDMIInputName3", "");
                    this.HDMIActiveChannels = this.ReadXmlElementAsByte(reader, "HDMIActiveChannels", 1);

                    this.InstallationType = this.ReadXmlElementAsByte(reader, "InstallationType", 0);
                    this.FlexSetupPort1 = this.ReadXmlElementAsByte(reader, "FlexSetupPort1", 50);
                    this.FlexSetupPort2 = this.ReadXmlElementAsByte(reader, "FlexSetupPort2", 50);
                    this.PixelTotalPort1 = this.ReadXmlElementAsByte(reader, "PixelTotalPort1", 50);
                    this.PixelTotalPort2 = this.ReadXmlElementAsByte(reader, "PixelTotalPort2", 50);
                    this.PixelVert = this.ReadXmlElementAsByte(reader, "PixelVert", 0);
                    this.PixelHori = this.ReadXmlElementAsByte(reader, "PixelHori", 0);

                    this.MaxBytesPerPacket = this.ReadXmlElementAsByte(reader, "MaxBytesPerPacket", 128);
                    this.SplitPackets = this.ReadXmlElementAsBool(reader, "SplitPackets");
                    this.FramesPerSecond = this.ReadXmlElementAsByte(reader, "FramesPerSecond", 10);
                    this.InnerSizePercent = this.ReadXmlElementAsByte(reader, "InnerSizePercent", 15);
                    this.ColorBoostPercent = this.ReadXmlElementAsByte(reader, "ColorBoostPercent", 0);
                }
            }
            catch (Exception ex)
            {
                CGlobals.HandleException("ReadXml", ex);
            }
        }
        public override void UpdateFrom(CDevice devCopyFrom)
        {
            this.CopyFrom(devCopyFrom); 
        }
        public override void CopyFrom(CDevice devCopyFrom)
        {
            CDeviceDreamScreen myDevCopyFrom = (CDeviceDreamScreen)devCopyFrom;

            // Internal Properties
            this.bIsOnline = myDevCopyFrom.bIsOnline;

            // Copy Common properties
            this.GroupName = myDevCopyFrom.GroupName;
            this.GroupNumber = myDevCopyFrom.GroupNumber; 
            this.Mode = myDevCopyFrom.Mode;

            this.Brightness = myDevCopyFrom.Brightness;
            this.AmbientColor = Color.FromArgb(myDevCopyFrom.AmbientColor.ToArgb());
            this.AmbientScene = myDevCopyFrom.AmbientScene;

            if (m_DeviceType == DeviceType.DreamScreenHD ||
                m_DeviceType == DeviceType.DreamScreen4K ||
                m_DeviceType == DeviceType.DreamScreen4KSolo)
            {
                // Copy Controller properties
                this.HDMIInput = myDevCopyFrom.HDMIInput;
                this.HDMIInputName1 = myDevCopyFrom.HDMIInputName1;
                this.HDMIInputName2 = myDevCopyFrom.HDMIInputName2;
                this.HDMIInputName3 = myDevCopyFrom.HDMIInputName3;
                this.HDMIActiveChannels = myDevCopyFrom.HDMIActiveChannels;

                this.InstallationType = myDevCopyFrom.InstallationType;
                this.FlexSetupPort1 = myDevCopyFrom.FlexSetupPort1;
                this.FlexSetupPort2 = myDevCopyFrom.FlexSetupPort2;
                this.PixelTotalPort1 = myDevCopyFrom.PixelTotalPort1;
                this.PixelTotalPort2 = myDevCopyFrom.PixelTotalPort2;
                this.PixelVert = myDevCopyFrom.PixelVert;
                this.PixelHori = myDevCopyFrom.PixelHori;

                this.MaxBytesPerPacket = myDevCopyFrom.MaxBytesPerPacket;
                this.SplitPackets = myDevCopyFrom.SplitPackets;
                this.FramesPerSecond = myDevCopyFrom.FramesPerSecond;
                this.InnerSizePercent = myDevCopyFrom.InnerSizePercent;
                this.ColorBoostPercent = myDevCopyFrom.ColorBoostPercent;
            }
        }

        public override void SetDeviceID()
        {
            /* Generate Device ID from these values */
            szDeviceID = CGlobals.DEVICETYPES[(int)this.GetDeviceType()] + "_" +
                             this.GetDeviceName() + "_" +
                            (this.GetIPAddress() != null ? this.GetIPAddress().ToString() : "");
        }
        #endregion

        #region Virtual Public Threading functions
        public override void HandleThread()
        {
            DoDebugOutput("CDeviceDreamScreen|HandleThread|Begin Thread");

            while (bAbortThread == false)
            {
                try
                {
                    // (RE)Init Helper Variables!
                    InitHelpers();
                }
                catch (Exception ex)
                {
                    CGlobals.DebugOutput(ex);
                }

                try
                {
                    int iFrameTimeMs = 1000 / FramesPerSecond;
                    int iLastSecond = DateTime.Now.Second;
                    int iLastMilliSec = DateTime.Now.Millisecond;
                    int iFramesPerSec = 0;

                    while (true)
                    {
                        // Scan Images by each Pixel for Colors
                        this.CalcColorsRectangles(this.PixelTotalPort1, this.bmpPrimScreen, brPixels);

                        // Finally send calced pixels to DS!
                        this.DreamScreenSendLEDData();

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
                    }
                }
                catch (ThreadInterruptedException)
                {
                    // Interrupted
                    DoDebugOutput("CDeviceDreamScreen|HandleThread|Interrupted");

                    while (bSleepThread)
                    {
                        // Use Sleep instead of SpinWait 
                        Thread.Sleep(1);
                        // Thread.SpinWait(10000000);
                    }

                    // Interrupted
                    DoDebugOutput("CDeviceDreamScreen|HandleThread|Restart");

                    // this.SetAbortThread(true);
                }
                catch (ThreadAbortException)
                {
                    // Aborted
                    DoDebugOutput("CDeviceDreamScreen|HandleThread|Aborted");

                    this.SetAbortThread(true);
                }
                catch (Exception ex)
                {
                    CGlobals.DebugOutput(ex);
                    DoDebugOutput("CDeviceDreamScreen|HandleThread|Exception: " + ex.ToString());

                    this.SetAbortThread(true);
                }
                finally
                {
                    // nothing todo..
                }
            }

            DoDebugOutput("CDeviceDreamScreen|HandleThread|Finished Thread");
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
                    myThread.Join(100); // Wait max 1 second until the thread is gone...
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
                    DoDebugOutput("CDeviceDreamScreen|StartThread|Unexpected Thread STATE: " + myThread.ThreadState.ToString());
                }
            }
            catch (System.InvalidOperationException)
            {
                // Bla we don't care
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
                DoDebugOutput("CDeviceDreamScreen|StartThread|Exception: " + ex.ToString());
            }
        }
        public void DreamScreenSendLEDData()
        {
            // Payload helpers 
            MemoryStream msPacket = new MemoryStream();
            BinaryWriter bwPacket = new BinaryWriter(msPacket);
            byte bPixelStart = 0x00; // StartIndex is 0x00 (where user configured his start! 

            try
            {
                // add pixels to array!
                for (byte bPixelCurrent = 0; bPixelCurrent < this.PixelTotalPort1; bPixelCurrent++)
                {
                    if (msPacket.Length <= 0)
                    {
                        // Init Payload
                        bwPacket.Write(bPixelStart); // Start Pixel
                        bwPacket.Write((byte)(this.PixelTotalPort1 - bPixelStart)); // Pixel Length
                    }

                    bwPacket.Write(((SolidBrush)brPixels[bPixelCurrent]).Color.R); // RED
                    bwPacket.Write(((SolidBrush)brPixels[bPixelCurrent]).Color.G); // GREEN
                    bwPacket.Write(((SolidBrush)brPixels[bPixelCurrent]).Color.B); // BLUE

                    if (msPacket.Length >= this.MaxBytesPerPacket || bPixelCurrent >= (this.PixelTotalPort1 - 1))
                    {
                        // Max Packet size reached, split from here!
                        msPacket.Position = 1;
                        msPacket.WriteByte((byte)(bPixelCurrent - bPixelStart));

#if USE_DREAMSCREEN
                        // Send pixels to DS as UDP Packet
                        CGlobals.deviceController.SendDreamscreenCommand(this, DsCommands.PixelVideoAddressing, 0xFF, (byte)DsFlags.Write, msPacket.ToArray());
#endif

                        if (this.SplitPackets)
                        {
                            // Create new Packet 
                            msPacket = new MemoryStream();
                            bwPacket = new BinaryWriter(msPacket);
                            bPixelStart = (byte)(bPixelCurrent); // Set new Start Pixel

// Force a Sleep when packages are split up
//Thread.Sleep(5);
                        }
                        else
                        {
                            // Skip further packets!
                            break;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                if (brPixels == null)
                    throw (new Exception("ERROR PIXELS NOT INITIALIZED"));

                CGlobals.DebugOutput(ex);
            }
        }
#endregion

#region Pixel Processing functions 
        public void CalcCorners(System.Drawing.Rectangle[] rectPixels)
        {
            aLeft = new int[this.PixelTotalPort1];
            aRight = new int[this.PixelTotalPort1];
            aTop = new int[this.PixelTotalPort1];
            aBottom = new int[this.PixelTotalPort1];

            // Loop through all Scan rectangles
            for (int n = 0; n < this.PixelTotalPort1; n++)
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
            if (this.FlexSetupPort1 == (byte)DsFlexSetups.Right_Cclock_bottom_right_up)
            {
                iOffset = CalcRightRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.BottomUp, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcTopRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.RightToLeft, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcLeftRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.TopDown, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcBottomRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.LeftToRight, rectPixels, iOffset);
            }
            // CounterClockWise Top Right to Left
            else if (this.FlexSetupPort1 == (byte) DsFlexSetups.Top_Cclock_top_right_left)
            {
                iOffset = CalcTopRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.RightToLeft, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcLeftRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.TopDown, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcBottomRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.LeftToRight, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcRightRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.BottomUp, rectPixels, iOffset);
            }
            // CounterClockWise Top Left to Down
            else if (this.FlexSetupPort1 == (byte) DsFlexSetups.Left_Cclock_top_left_down)
            {
                iOffset = CalcLeftRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.TopDown, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcBottomRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.LeftToRight, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcRightRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.BottomUp, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcTopRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.RightToLeft, rectPixels, iOffset);
            }
            // CounterClockWise Bottom Left to Right
            else if (this.FlexSetupPort1 == (byte) DsFlexSetups.Bottom_Cclock_bottom_left_right)
            {
                iOffset = CalcBottomRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.LeftToRight, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcRightRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.BottomUp, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcTopRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.RightToLeft, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcLeftRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.TopDown, rectPixels, iOffset);
            }
            // ClockWise Top Right To Down
            else if (this.FlexSetupPort1 == (byte) DsFlexSetups.Right_Clock_top_right_down)
            {
                iOffset = CalcRightRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.TopDown, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcBottomRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.RightToLeft, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcLeftRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.BottomUp, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcTopRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.LeftToRight, rectPixels, iOffset);
            }
            // ClockWise Top Left to Right
            else if (this.FlexSetupPort1 == (byte) DsFlexSetups.Top_Clock_top_left_right)
            {
                iOffset = CalcTopRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.LeftToRight, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcRightRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.TopDown, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcBottomRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.RightToLeft, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcLeftRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.BottomUp, rectPixels, iOffset);
            }
            // ClockWise Bottom Left to Up
            else if (this.FlexSetupPort1 == (byte) DsFlexSetups.Left_Clock_bottom_left_up)
            {
// TODODODODOD FIX ALL OTHER CALCS! we need to calc based obn the BACK screen!
                iOffset = CalcRightRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.BottomUp, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcTopRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.LeftToRight, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcLeftRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.TopDown, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcBottomRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.RightToLeft, rectPixels, iOffset);
            }
            // ClockWise Bottom Right to Left
            else if (this.FlexSetupPort1 == (byte) DsFlexSetups.Bottom_Clock_bottom_right_left)
            {
                iOffset = CalcBottomRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.RightToLeft, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcLeftRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.BottomUp, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcTopRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.LeftToRight, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcRightRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.TopDown, rectPixels, iOffset);
            }
        }
#endregion

#region public functions
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
                    brPixels = new Brush[this.PixelTotalPort1];

                    // Init Screen Scan Areas
                    rectScanPixels = new Rectangle[this.PixelTotalPort1];
                    this.CalcRectangles(0, 0, iScreenWidth, iScreenHeight, 0, this.rectScanPixels);
                    this.CalcCorners(this.rectScanPixels);
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

    }
}
