using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Custom
using System.Net;
using System.Threading;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace AmbiBack
{
    [Serializable]
    public abstract class CDevice : IXmlSerializable
    {
#region Threadstuff
        public System.Threading.Thread myThread = null;	// My Thread Object
        public bool bAbortThread = false;			    // Used to finish the Thread
        public bool bSleepThread = false;               // Helper to sleep when interrupted
        public int myThreadID = -1;                     // INT Number used to identify the thread
        public bool bIsOnline = false;                  // Store Controls if device is online

        public bool bIsSerial = false;                  // Serial Control Helper
        public string szSerialPort = null;              // Serial ComPort if Serial Connected
        public bool bIsSerialDebug = false;             // Serial Debug Capture enabled?

        // Global Screen Bitmaps Helpers
        // Latest frame of the monitor this device is linked to (set by AmbiMonitor)
        public FrameBuffer Frame = null;
        // Frame number the colors in brPixels were calculated from
        protected long lColorsFrameNumber = -1;

        // Bitmap size the scan rectangles were calculated for; they are recalculated when it changes
        protected int iScanAreaWidth = 0;
        protected int iScanAreaHeight = 0;

        public bool GetAbortThread() { return bAbortThread; }
        public void SetAbortThread(bool bNew) { bAbortThread = bNew; }
        public bool GetSleepThread() { return bSleepThread; }
        public void SetSleepThread(bool bNew) { bSleepThread = bNew; }
#endregion

#region Public Events
        public event EventDebugOutput DebugOutput;
        public event EventUpdateStatus UpdateStatus;
#endregion

#region public variables
        // Helper Array to store the screen areas to scan color values for each pixel! 
        public Rectangle[] rectScanPixels;
        public int[] aLeft;
        public int[] aRight;
        public int[] aTop;
        public int[] aBottom;
        // Helper Attay to store calculated pixels
        public Color[] brPixels;   // LED colors (plain values, no GDI+ brushes)
#endregion

#region private variables
        protected string szDeviceID;
        protected string m_szDeviceName;
        protected DeviceType m_DeviceType;
        protected IPAddress m_IPAddress;
#endregion

#region get/set helpers
        public string GetDeviceName() { return m_szDeviceName; }
        public void SetDeviceName(string szNewName) { m_szDeviceName = szNewName; }
        public string GetDeviceID() { return szDeviceID; }

        public DeviceType GetDeviceType() { return m_DeviceType; }
        public void SetDeviceType(DeviceType iNewType) { m_DeviceType = iNewType; }
        public IPAddress GetIPAddress() { return m_IPAddress; }
        public void SetIPAddress(IPAddress ipNew) { m_IPAddress = ipNew; }

        public string InternalDeviceID { get; set; } = null;
        public string InternalNetworkMAC { get; set; } = null;
        public bool DebugSupport { get; set; } = false; 

        // Color Control Properties
        public byte ColorRedPercent { get; set; } = 100;
        public byte ColorGreenPercent { get; set; } = 100;
        public byte ColorBluePercent { get; set; } = 100;
        public byte ColorBoostPercent { get; set; } = 0;
        public int ValueGainPercent { get; set; } = 100;

        public byte MaxBrightnessPercent { get; set; } = 100;
        public byte MinimumLuminosityPercent { get; set; } = 0;
        public byte MaxColorBoostPercent { get; set; } = 100;
        public byte MaxValueGainPercent { get; set; } = 200;

        // Supported Properties
        public bool EditColorRedPercent { get; set; } = false;
        public bool EditColorGreenPercent { get; set; } = false;
        public bool EditColorBluePercent { get; set; } = false;
        public bool EditColorBoostPercent { get; set; } = false;
        public bool EditValueGainPercent { get; set; } = false;
        public bool EditMaxBrightnessPercent { get; set; } = false;
        public bool EditMinimumLuminosityPercent { get; set; } = false;
        // Check for device supported features
        public bool UseDeviceValueGainPercent { get; set; } = false;
        public bool UseDeviceColorBoostPercent { get; set; } = false;



        // Advanced Properties
        public byte FrameLength { get; set; } = 10;
        public int FrameDelay { get; set; } = 60;
        public int MinFrameDelay { get; set; } = 10;
        public int MaxFrameDelay { get; set; } = 1000;
        public int StepFrameDelay { get; set; } = 10;
        public byte FramesPerSecond { get; set; } = 30;
        public byte SerialFramesPerSecond { get; set; } = 60;
        public byte MaxFramesPerSecond { get; set; } = 60;
        public int StepFramesPerSecond { get; set; } = 5;
        public byte InnerSizePercent { get; set; } = 10;
        // Supported Properties
        public byte ConnectionType { get; set; } = (byte)ConnectionTypes.UDP;   // UDP works on any Wi-Fi
        
        public bool EditDeviceName { get; set; } = false;
        public bool EditFrameDelay { get; set; } = false;
        public bool EditFramesPerSecond { get; set; } = false;
#endregion

        // Constructor
        public CDevice()
        {
            // set defaults
            this.SetDeviceType(DeviceType.UnknownDevice);
            this.SetDeviceName("Unknown device");
            this.SetDeviceID();
            this.InitClass();
        }

#region Public functions
        public override string ToString()
        {
            return m_szDeviceName;
        }

        public void DoDebugOutput(string szDebug)
        {
            if (DebugOutput != null)
                DebugOutput(this, null, szDebug);
        }
        public void DoUpdateStatus(string szStatus)
        {
            if (UpdateStatus != null)
                UpdateStatus(this, null, szStatus);
        }

        public bool IsOnline()
        {
            return bIsOnline; 
        }
        public void SetIsOnline(bool bnew)
        {
            bIsOnline = bnew;
        }
        public bool IsSerial()
        {
            return bIsSerial;
        }
        public void SetIsSerial(bool bnew)
        {
            bIsSerial = bnew;
        }
        public bool IsSerialDebug()
        {
            return bIsSerialDebug;
        }
        public void SetSerialDebug(bool bnew)
        {
            bIsSerialDebug = bnew;
        }
        
        public void SetComPort(string szNew)
        {
            szSerialPort = szNew;
        }

        public bool CheckIsOnline()
        {
            bool bReturn = false;

            try
            {
                // Local vars
                int dwStart = 0; 
                int dwStop = 0;
                string szScanMyIp = this.m_IPAddress.ToString(); 

                // Create a PingHelper!
                CPingHelper myPing = new CPingHelper();
                myPing.InitSocket();
                myPing.iMyTimeout = 2000;   // Hardcoded
                myPing.iMyBytes2Send = 32;  // Hardcoded

                // --- First create entpoints hosts
                if (myPing.CreateEndPoints(szScanMyIp))
                {
                    // --- Create ICMP Packet
                    byte[] mySendBuf = null;
                    int iSendSize = myPing.CreateWholeIPPacket(null, ref mySendBuf);
                    // --- 

                    // --- Send ICMP Packet
                    dwStart = System.Environment.TickCount; // Start timing
                    if (myPing.SendIcmpPacket(ref mySendBuf))
                    {   // packet send, continue

                        // --- Receive ICMP Packet
                        byte[] myRecvBuf = null;
                        IcmpPacket myRecPacket = null;
                        int iByteRecv = 0;
                        bool bIPReplied = false;

                        //New, we do a receive loop 
                        while ((dwStop = System.Environment.TickCount - dwStart) < myPing.iMyTimeout && bIPReplied == false)
                        {
                            // receive a packet and more then 0 bytes
                            if (myPing.RecvIcmpPacket(ref myRecvBuf, ref iByteRecv) && iByteRecv > 0)
                            {
#region Handle Icmp Packet
                                myPing.ResolveIcmpPacket(ref myRecvBuf, iByteRecv, ref myRecPacket);
                                if (myRecPacket != null)
                                {
                                    // First we compare the IPAddresses
                                    if (myPing.CheckSourceOfPacket(ref myRecvBuf, iByteRecv, ref mySendBuf, iSendSize))
                                    {
                                        // Packet was for us so we check
                                        if (myRecPacket.Type == CPingHelper.Icmp_EchoReply)
                                        {
                                            // Store local helper value
                                            bReturn = true;
                                            bIsOnline = bReturn;
                                            return bReturn;
                                        }
                                    }
                                }
#endregion

                                // We sleep in 10 ms steps 
                                Thread.Sleep(1);

                            }
                        }

                        // We reached here? Timed out
                        CGlobals.DebugOutput("CDevice:IsOnline: Could not receive from '" + szScanMyIp + "' in " + myPing.iMyTimeout + " ms!");
                    }
                    else
                        CGlobals.DebugOutput("CDevice:IsOnline: Could not use '" + szScanMyIp + "'!");
                    // --- 

                }
                else
                    CGlobals.DebugOutput("CDevice:IsOnline: Could not use '" + szScanMyIp + "'!");

            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }

            // Store local helper value
            bIsOnline = bReturn; 
            return bReturn; 
        }

        public void CheckInternalValues()
        {
            // Correct internal parameters
            if (FrameDelay < MinFrameDelay)
                FrameDelay = MinFrameDelay;
            else if (FrameDelay > MaxFrameDelay)
                FrameDelay = MaxFrameDelay;
        }
#endregion

#region Pixel Processing functions 
        public void CalcCorners(byte PixelTotal, System.Drawing.Rectangle[] rectPixels)
        {
            aLeft = new int[PixelTotal];
            aRight = new int[PixelTotal];
            aTop = new int[PixelTotal];
            aBottom = new int[PixelTotal];

            // Loop through all Scan rectangles
            for (int n = 0; n < PixelTotal; n++)
            {
                System.Drawing.Rectangle myRect = rectPixels[n];
                aLeft[n] = myRect.Left;
                aRight[n] = myRect.Right;
                aTop[n] = myRect.Top;
                aBottom[n] = myRect.Bottom;
            }
        }
        public Color getColorFromPixelRregion(System.Drawing.Bitmap myScreenshot, int iLeft, int iTop, int iWidth, int iHeight)
        {
            int iRed = 0;
            int iGreen = 0;
            int iBlue = 0;
            int iPixels = 0;

            unsafe
            {
                BitmapData bmlData = null;

                try
                {
                    // Lock ImageData | workarround GetPixel() locking, using direct access on bit data!
                    bmlData = myScreenshot.LockBits(new System.Drawing.Rectangle(iLeft, iTop, iWidth, iHeight), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    byte* srcPointer = (byte*)bmlData.Scan0;

                    // Found using performance profiler
                    int screenshotWidth = myScreenshot.Width;

                    int iIndex = 0;
                    for (int i = 0; i < iHeight; i++)
                    {
                        for (int j = 0; j < iWidth; j++)
                        {
                            iBlue += srcPointer[iIndex + 0];     // Blue
                            iGreen += srcPointer[iIndex + 1];    // Green
                            iRed += srcPointer[iIndex + 2];      // Red
                                                                 // not needed dstPointer[3] = srcPointer[3]; // Alpha
                                                                 /*
                                                                 srcPointer[iIndex + 0] = 0;
                                                                 srcPointer[iIndex + 1] = 0;
                                                                 srcPointer[iIndex + 2] = 0;
                                                                 */
                            iPixels++;
                            iIndex += 4; /* Add 4 bytes to step */
                        }

                        /* Fast Forward Pixels */
                        iIndex += (screenshotWidth - iWidth) * 4;
                    }

                }
                catch (Exception ex)
                {
                    DoDebugOutput("CDevice|getColorFromPixelRregion|Exception: " + ex.ToString());
                }
                finally
                {
                    // Unlock Imagedata
                    if (bmlData != null)
                        myScreenshot.UnlockBits(bmlData);
                }
            }

            /* Calc avarage color */
            iRed = (iRed > 0 ? iRed / iPixels : 0);
            iGreen = (iGreen > 0 ? iGreen / iPixels : 0);
            iBlue = (iBlue > 0 ? iBlue / iPixels : 0);

            return AdjustColor(iRed, iGreen, iBlue);
        }

        /// <summary>Value gain and color boost (when the device does not do it), clamped to 0..255.</summary>
        private Color AdjustColor(int iRed, int iGreen, int iBlue)
        {
            /* Check for Value Gain */
            if (!this.UseDeviceValueGainPercent && this.ValueGainPercent > 100)
            {
                iRed = (iRed * this.ValueGainPercent) / 100;
                iGreen = (iGreen * this.ValueGainPercent) / 100;
                iBlue = (iBlue * this.ValueGainPercent) / 100;
            }

            /* Check for ColorBoost */
            if (!this.UseDeviceColorBoostPercent && this.ColorBoostPercent > 0)
            {
                if (iRed > iGreen && iRed > iBlue)
                {
                    iRed = (iRed * (100 + this.ColorBoostPercent) / 100);
                    iGreen = (iGreen * (100 - this.ColorBoostPercent) / 100);
                    iBlue = (iBlue * (100 - this.ColorBoostPercent) / 100);
                }
                else if (iGreen > iRed && iGreen > iBlue)
                {
                    iGreen = (iGreen * (100 + this.ColorBoostPercent) / 100);
                    iRed = (iRed * (100 - this.ColorBoostPercent) / 100);
                    iBlue = (iBlue * (100 - this.ColorBoostPercent) / 100);
                }
                else if (iBlue > iGreen && iBlue > iRed)
                {
                    iBlue = (iBlue * (100 + this.ColorBoostPercent) / 100);
                    iGreen = (iGreen * (100 - this.ColorBoostPercent) / 100);
                    iRed = (iRed * (100 - this.ColorBoostPercent) / 100);
                }
            }

            /* Correct values if they are above 255 */
            if (iRed > 255)
                iRed = 255;
            if (iGreen > 255)
                iGreen = 255;
            if (iBlue > 255)
                iBlue = 255;


            return System.Drawing.Color.FromArgb(255, iRed, iGreen, iBlue);
        }

        /// <summary>
        /// Average color of every scan rectangle, read directly from the monitor's frame buffer under its
        /// lock (no LockBits, no allocations). Returns false when there is no new frame since the last call.
        /// Recalculates the scan rectangles when the frame size changed.
        /// </summary>
        public unsafe bool CalcColorsFromFrame(int PixelTotal, Color[] colors, Action<int, int> updateScanAreas)
        {
            FrameBuffer f = this.Frame;
            if (f == null || colors == null || aLeft == null)
                return false;
            lock (f.Sync)
            {
                if (f.Pixels == null || f.FrameNumber == lColorsFrameNumber)
                    return false;
                if (f.Width != iScanAreaWidth || f.Height != iScanAreaHeight)
                    updateScanAreas(f.Width, f.Height);

                int w = f.Width, h = f.Height, stride = w * 4;
                int n = Math.Min(PixelTotal, Math.Min(colors.Length, aLeft.Length));
                fixed (byte* p = f.Pixels)
                {
                    for (int i = 0; i < n; i++)
                    {
                        // Clamp to the frame: a zero sized or outside rectangle gives black
                        int x0 = Math.Max(0, aLeft[i]), x1 = Math.Min(w, aRight[i]);
                        int y0 = Math.Max(0, aTop[i]), y1 = Math.Min(h, aBottom[i]);
                        long r = 0, g = 0, b = 0;
                        int count = (x1 > x0 && y1 > y0) ? (x1 - x0) * (y1 - y0) : 0;
                        for (int y = y0; y < y1; y++)
                        {
                            byte* px = p + y * stride + x0 * 4;
                            for (int x = x0; x < x1; x++, px += 4)
                            {
                                b += px[0];
                                g += px[1];
                                r += px[2];
                            }
                        }
                        colors[i] = count == 0 ? Color.FromArgb(255, 0, 0, 0)
                            : AdjustColor((int)(r / count), (int)(g / count), (int)(b / count));
                    }
                }
                lColorsFrameNumber = f.FrameNumber;
                return true;
            }
        }

        public void CalcColorsRectangles(byte PixelTotal, System.Drawing.Bitmap myScreenshot, Color[] brPixels)
        {
            try
            {
                // Old method of grabbing pixel data!
                for (int x = 0; x < PixelTotal; x++)
                {
                    brPixels[x] = this.getColorFromPixelRregion(myScreenshot, aLeft[x], aTop[x], aRight[x] - aLeft[x], aBottom[x] - aTop[x]);
                }
            }
            catch (ThreadInterruptedException ex)
            {
                // Handles by upper function
                throw (ex);
            }
            catch (ThreadAbortException ex)
            {
                // Handles by upper function
                throw (ex);
            }
            catch (Exception ex)
            {
                DoDebugOutput("CDevice|CalcColorsRectangles|Exception: " + ex.ToString());
            }
            return;
        }
        public int CalcRightRectangles(byte PixelVert, byte PixelHori, int iLeftOffset, int iTopOffset, int iScanWidth, int iScanHeight, int iIntoImagePx, FlowDirection myDirection, System.Drawing.Rectangle[] rectPixels, int iOffset)
        {
            int iNewOffset = iOffset;

            int iPixelWidth = iIntoImagePx == 0 ? (iScanWidth * this.InnerSizePercent) / 100 : iIntoImagePx;   /* Use % width */
            int iPixelHeight = iScanHeight / PixelVert;
            int iLeft = iLeftOffset + iScanWidth - iPixelWidth;
            int iTop = 0;

            if (myDirection == FlowDirection.TopDown)
            {
                // TopDown!
                for (int iLoop = 0; iLoop < PixelVert; iLoop++)
                {
                    iTop = iTopOffset + iLoop * iPixelHeight;
                    rectPixels[iNewOffset] = new System.Drawing.Rectangle(iLeft, iTop, iPixelWidth, iPixelHeight);
                    iNewOffset++;
                }
            }
            else
            {
                // BottomUp!
                for (int iLoop = PixelVert - 1; iLoop >= 0; iLoop--)
                {
                    iTop = iTopOffset + iLoop * iPixelHeight;
                    rectPixels[iNewOffset] = new System.Drawing.Rectangle(iLeft, iTop, iPixelWidth, iPixelHeight);
                    iNewOffset++;
                }
            }

            /* return new array offset*/
            return iNewOffset;
        }
        public int CalcTopRectangles(byte PixelVert, byte PixelHori, int iLeftOffset, int iTopOffset, int iScanWidth, int iScanHeight, int iIntoImagePx, FlowDirection myDirection, System.Drawing.Rectangle[] rectPixels, int iOffset)
        {
            int iNewOffset = iOffset;

            int iPixelWidth = iScanWidth / PixelHori;
            int iPixelHeight = iIntoImagePx == 0 ? (iScanHeight * this.InnerSizePercent) / 100 : iIntoImagePx; /* Use % width */
            int iLeft = 0;
            int iTop = iTopOffset;

            if (myDirection == FlowDirection.RightToLeft)
            {
                // RightToLeft!
                for (int iLoop = PixelHori - 1; iLoop >= 0; iLoop--)
                {
                    iLeft = iLeftOffset + iLoop * iPixelWidth;
                    rectPixels[iNewOffset] = new System.Drawing.Rectangle(iLeft, iTop, iPixelWidth, iPixelHeight);
                    iNewOffset++;
                }
            }
            else
            {
                // LeftToRight!
                for (int iLoop = 0; iLoop < PixelHori; iLoop++)
                {
                    iLeft = iLeftOffset + iLoop * iPixelWidth;
                    rectPixels[iNewOffset] = new System.Drawing.Rectangle(iLeft, iTop, iPixelWidth, iPixelHeight);
                    iNewOffset++;
                }
            }

            /* return new array offset*/
            return iNewOffset;
        }
        public int CalcLeftRectangles(byte PixelVert, byte PixelHori, int iLeftOffset, int iTopOffset, int iScanWidth, int iScanHeight, int iIntoImagePx, FlowDirection myDirection, System.Drawing.Rectangle[] rectPixels, int iOffset)
        {
            int iNewOffset = iOffset;

            int iPixelWidth = iIntoImagePx == 0 ? (iScanWidth * this.InnerSizePercent) / 100 : iIntoImagePx; /* Use % width! */
            int iPixelHeight = iScanHeight / PixelVert;
            int iLeft = iLeftOffset;
            int iTop = 0;

            if (myDirection == FlowDirection.TopDown)
            {
                // TopDown!
                for (int iLoop = 0; iLoop < PixelVert; iLoop++)
                {
                    iTop = iTopOffset + iLoop * iPixelHeight;
                    rectPixels[iNewOffset] = new System.Drawing.Rectangle(iLeft, iTop, iPixelWidth, iPixelHeight);
                    iNewOffset++;
                }
            }
            else
            {
                // BottomUp!
                for (int iLoop = PixelVert - 1; iLoop >= 0; iLoop--)
                {
                    iTop = iTopOffset + iLoop * iPixelHeight;
                    rectPixels[iNewOffset] = new System.Drawing.Rectangle(iLeft, iTop, iPixelWidth, iPixelHeight);
                    iNewOffset++;
                }
            }

            /* return new array offset*/
            return iNewOffset;
        }
        public int CalcBottomRectangles(byte PixelVert, byte PixelHori, int iLeftOffset, int iTopOffset, int iScanWidth, int iScanHeight, int iIntoImagePx, FlowDirection myDirection, System.Drawing.Rectangle[] rectPixels, int iOffset)
        {
            int iNewOffset = iOffset;

            int iPixelWidth = iScanWidth / PixelHori;
            int iPixelHeight = iIntoImagePx == 0 ? (iScanHeight * this.InnerSizePercent) / 100 : iIntoImagePx; /* Use % width */
            int iLeft = 0;
            int iTop = iTopOffset + iScanHeight - iPixelHeight;

            if (myDirection == FlowDirection.RightToLeft)
            {
                // RightToLeft!
                for (int iLoop = PixelHori - 1; iLoop >= 0; iLoop--)
                {
                    iLeft = iLeftOffset + iLoop * iPixelWidth;
                    rectPixels[iNewOffset] = new System.Drawing.Rectangle(iLeft, iTop, iPixelWidth, iPixelHeight);
                    iNewOffset++;
                }
            }
            else
            {
                // LeftToRight!
                for (int iLoop = 0; iLoop < PixelHori; iLoop++)
                {
                    iLeft = iLeftOffset + iLoop * iPixelWidth;
                    rectPixels[iNewOffset] = new System.Drawing.Rectangle(iLeft, iTop, iPixelWidth, iPixelHeight);
                    iNewOffset++;
                }
            }

            /* return new array offset*/
            return iNewOffset;
        }
#endregion

#region Thread Controlling Functions
        public void CreateThread()
        {
            if (myThread == null)
            {
                myThread = new Thread(new ThreadStart(HandleThread));
                // Lowest starved the device threads under game load
                myThread.Priority = ThreadPriority.BelowNormal;

                // Set ThreadName here
                myThread.Name = "DeviceHelper";
            }
        }
        public bool IsThreadRunning()
        {
            if (myThread.ThreadState == System.Threading.ThreadState.Running)
                return true;
            else
                return false;

        }
        public bool SleepThread()
        {
            if (myThread != null)
            {
                if (myThread.ThreadState == System.Threading.ThreadState.Running ||
                        myThread.ThreadState == System.Threading.ThreadState.WaitSleepJoin)
                {
                    this.SetSleepThread(true);
                    myThread.Interrupt(); //  Interrupt only!;
                }

                // True means Interrupt does need to be taken care of
                return true;
            }
            else
                // False means Interrupt has to be ignored
                return false;
        }
        public bool AbortThread()
        {
            if (myThread != null)
            {
                if (myThread.ThreadState == System.Threading.ThreadState.Running ||
                        myThread.ThreadState == System.Threading.ThreadState.WaitSleepJoin)
                {
                    // Abort thread
                    this.SetSleepThread(false);
                    this.SetAbortThread(true);

                    myThread.Abort(); //  Abort();
                    myThread.Join(100); // Wait max 1 second until the thread is gone...
                    myThread = null;		// Byebye thread
                }

                // True means bAbortOperation does need to be taken care of
                return true;
            }
            else
                // False means bAbortOperation has to be ignored
                return false;
        }
#endregion

#region Virtual Public Threading functions
        public abstract void HandleThread();
        public abstract void StartThread();
#endregion

#region Virtual Public functions
        // Virtual function filled by Child classes
        public abstract void ProcessPayloadData(byte[] bData);
        public abstract void WriteChildXml(XmlWriter writer);
        public abstract void ReadChildXml(XmlReader reader);
        public abstract void CopyFrom(CDevice devCopyFrom);
        public abstract void UpdateFrom(CDevice devCopyFrom);
        public abstract void SetDeviceID();
        public abstract void InitClass();
#endregion

#region XMLSerializer Functions
        // Xml Serialization Infrastructure

        public void WriteXml(XmlWriter writer)
        {
            // Write Base XML Values

            try
            {
                // Write XML Elements
                this.WriteXmlElement(writer, "DeviceType", (int)this.GetDeviceType()); 
                writer.WriteElementString("DeviceName", this.GetDeviceName());
                this.WriteXmlElement(writer, "IPAddress", (this.GetIPAddress() != null ? this.GetIPAddress().GetAddressBytes() : new byte[1] { 0 }));

                // Append Child Properties!
                this.WriteChildXml(writer); 
            }
            catch (Exception ex)
            {
                CGlobals.HandleException("WriteXml", ex);
            }
        }

        public void ReadXml(XmlReader reader)
        {
            try
            {
                // Read Base XML Values
                reader.ReadToFollowing("DeviceType");
                this.SetDeviceType((DeviceType)reader.ReadElementContentAsInt("DeviceType", "")); //   Int32.Parse( reader.ReadElementContentAsString("DeviceType", "") ) );
                this.SetDeviceName(reader.ReadElementContentAsString("DeviceName", "")); // "DeviceName", ""));
                byte[] bIP = this.ReadXmlElementAsByteArray(reader, "IPAddress"); 
                if (bIP != null && bIP.Length > 1)
                    this.SetIPAddress(new IPAddress(bIP));

                /*                byte[] bufferIPAddress = new byte[64];
                                int iBytesRead = reader.ReadElementContentAsBinHex(bufferIPAddress, 0, bufferIPAddress.Length);
                                Array.Resize(ref bufferIPAddress, iBytesRead);
                                bufferIPAddress)); //  (IPAddress)reader.ReadElementContentAsObject("IPAddress", ""));
                */

                // Read Child Properties!
                this.ReadChildXml(reader);

            }
            catch (Exception ex)
            {
                CGlobals.HandleException("ReadXml", ex);
            }
        }

        public void WriteXmlElement(XmlWriter writer, string szName, bool bValue)
        {
            // Write Element with INT Value
            writer.WriteStartElement(szName);
            writer.WriteValue(bValue);
            writer.WriteEndElement();
        }

        public void WriteXmlElement(XmlWriter writer, string szName, int iValue)
        {
            // Write Element with INT Value
            writer.WriteStartElement(szName);
            writer.WriteValue(iValue);
            writer.WriteEndElement();
        }

        public void WriteXmlElement(XmlWriter writer, string szName, byte bValue)
        {
            // Write Element with INT Value
            writer.WriteStartElement(szName);
            byte[] bTmp = new byte[1] { bValue };
            writer.WriteBinHex(bTmp, 0, bTmp.Length);
            writer.WriteEndElement();
        }

        public void WriteXmlElement(XmlWriter writer, string szName, byte[] bValue)
        {
            // Write Element with INT Value
            writer.WriteStartElement(szName);
            writer.WriteBinHex(bValue, 0, bValue.Length);
            writer.WriteEndElement();
        }

        public bool ReadXmlElementAsBool(XmlReader reader, string szName)
        {
            reader.ReadStartElement();
            bool bReturn = reader.ReadContentAsBoolean();
            reader.ReadEndElement();
            return bReturn;
        }

        public byte ReadXmlElementAsByte(XmlReader reader, string szName, byte bDefault)
        {
            try
            {
                byte[] buffer = new byte[8];
                reader.ReadStartElement();
                reader.ReadContentAsBinHex(buffer, 0, buffer.Length);
                reader.ReadEndElement();
                return buffer[0];
            }
            catch (Exception ex)
            {
                CGlobals.HandleException("ReadXml", ex);
            }

            /* Use default */
            return bDefault; 
        }

        public int ReadXmlElementAsInt(XmlReader reader, string szName, int iDefault)
        {
            int iReturn = iDefault; 
            try
            {
                reader.ReadStartElement();
                iReturn = reader.ReadContentAsInt();
                reader.ReadEndElement();
                return iReturn;    // returned an unused buffer (always 0) before
            }
            catch (Exception ex)
            {
                CGlobals.HandleException("ReadXml", ex);
            }

            /* Use default */
            return iReturn;
        }


        public byte[] ReadXmlElementAsByteArray(XmlReader reader, string szName)
        {
            try
            {
                byte[] buffer = new byte[255];
                reader.ReadStartElement();
                int iBytesRead = reader.ReadContentAsBinHex(buffer, 0, buffer.Length);
                Array.Resize(ref buffer, iBytesRead);
                reader.ReadEndElement();

                // return result
                return buffer;
            }
            catch (Exception ex)
            {
                CGlobals.HandleException("ReadXml", ex);
            }

            // return null on failure
            return null;
        }

        public XmlSchema GetSchema()
        {
            return (null);
        }
#endregion

    }
}
