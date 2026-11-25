using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Custom
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Threading;
using System.Xml;
using System.Xml.Schema;    
using System.Xml.Serialization;
using System.Net;
using System.Net.Sockets;
using System.IO.Ports;
using AmbiBack.Properties;

namespace AmbiBack
{
    [Serializable]
    public class CDeviceAmbiBack : CDevice
    {
        public static readonly int SERIAL_HEADLEN = 14;
        public static readonly int TCP_HEADLEN = 8;
        public static readonly int TCP_CHUNKSIZE = 4;

        bool bFirstTime = true;
        bool bInitDone = false;
        bool bForceReload = false;
        byte[] bData = null; 

        public int m_iPort = 1337;
        private UdpClient m_clientUdp = null;
        private TcpClient m_clientTcp = null;
        private SerialPort m_clientSerial = null;

        private byte _pixelVert = 8;
        private byte _pixelHori = 16;

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

        public byte FlexSetupPort1 { get; set; } = (byte)DsFlexSetups.Left_Clock_bottom_left_up;
        public byte FlexSetupPort2 { get; set; } = (byte)DsFlexSetups.Left_Clock_bottom_left_up;
        public byte PixelTotalPort1 { get; set; } = 48;
        public byte PixelTotalPort2 { get; set; } = 48;
        public byte PixelVert
        {
            get => _pixelVert;
            set => _pixelVert = value;
        }
        public byte PixelHori {
            get => _pixelHori;
            set => _pixelHori = value;
        }
        public void SetInnerSizePercent(byte bNew)
        {
            this.InnerSizePercent = bNew;
            // redo Pixel Init
            this.bInitDone = false;
        }

        public void SetConnectionType(ConnectionTypes newType)
        {
            // Set new Connection Type and Force reload
            this.ConnectionType = (byte)newType;
            this.bForceReload = true;
        }

        public void SetPixelHori(byte bNew)
        {
            this.PixelHori = bNew;

            /* Currently hardcoded to all 4 sides */
            this.PixelTotalPort1 = (byte)(this.PixelHori * 2 + this.PixelVert * 2);
            this.PixelTotalPort2 = (byte)(this.PixelHori * 2 + this.PixelVert * 2);
            // redo Pixel Init
            this.bInitDone = false;
        }
        public void SetPixelVert(byte bNew)
        {
            this.PixelVert = bNew;

            /* Currently hardcoded to all 4 sides */
            this.PixelTotalPort1 = (byte)(this.PixelHori * 2 + this.PixelVert * 2);
            this.PixelTotalPort2 = (byte)(this.PixelHori * 2 + this.PixelVert * 2);

            // redo Pixel Init
            this.bInitDone = false;
        }

        public void SetFlexPort1(byte bNew)
        {
            this.FlexSetupPort1 = bNew;
            // redo Pixel Init
            this.bInitDone = false;
        }

        public byte MaxBytesPerPacket { get; set; } = 196;
        public bool SplitPackets { get; set; } = false;
        #endregion

        // Constructor
        public CDeviceAmbiBack()
        {
            // Set defaults 
            this.SetDeviceType(DeviceType.AmbiBackController);
            this.SetDeviceName("Unknown Controller");
            this.InitClass();
        }
        public CDeviceAmbiBack(DeviceType newDeviceType, string szDeviceName)
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

            // Set other defaults
            this.SerialFramesPerSecond = 60;
            this.FramesPerSecond = 30;
            this.MaxFramesPerSecond = 60;

            this.FrameDelay = 60;
            this.MinFrameDelay = 10;
            this.MaxFrameDelay = 1000;
            this.StepFrameDelay = 10;

            // Enable supported 
            this.EditColorRedPercent = true;
            this.EditColorGreenPercent = true;
            this.EditColorBluePercent = true;
            this.EditColorBoostPercent = true;
            this.EditValueGainPercent = true;
            this.EditMaxBrightnessPercent = true;
            this.EditMinimumLuminosityPercent = true;

            // Now Device supported!
            this.UseDeviceValueGainPercent = true;
            this.UseDeviceColorBoostPercent = true;

            this.EditDeviceName = true;
            this.EditFrameDelay = true;
            this.EditFramesPerSecond = true;

            this.DebugSupport = true;
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
                // Write Common Device properties 
                writer.WriteElementString("GroupName", this.GroupName);
                this.WriteXmlElement(writer, "GroupNumber", this.GroupNumber);
                this.WriteXmlElement(writer, "Mode", this.Mode);

                this.WriteXmlElement(writer, "Brightness", this.Brightness);
                this.WriteXmlElement(writer, "AmbientColor", this.AmbientColor.ToArgb());
                this.WriteXmlElement(writer, "AmbientScene", this.AmbientScene);

                this.WriteXmlElement(writer, "FlexSetupPort1", this.FlexSetupPort1);
                this.WriteXmlElement(writer, "FlexSetupPort2", this.FlexSetupPort2);
                this.WriteXmlElement(writer, "PixelTotalPort1", this.PixelTotalPort1);
                this.WriteXmlElement(writer, "PixelTotalPort2", this.PixelTotalPort2);
                this.WriteXmlElement(writer, "PixelVert", this.PixelVert);
                this.WriteXmlElement(writer, "PixelHori", this.PixelHori);

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
                writer.WriteElementString("InternalDeviceID", this.InternalDeviceID);
                this.WriteXmlElement(writer, "ConnectionType", this.ConnectionType);
                writer.WriteElementString("InternalNetworkMAC", this.InternalNetworkMAC);
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
                // Read Common Device properties 
                this.GroupName = reader.ReadElementContentAsString("GroupName", "");
                this.GroupNumber = this.ReadXmlElementAsByte(reader, "GroupNumber", 0);
                this.Mode = this.ReadXmlElementAsByte(reader, "Mode", 0);

                this.Brightness = this.ReadXmlElementAsByte(reader, "Brightness", 100);
                this.AmbientColor = Color.FromArgb((reader.ReadElementContentAsInt("AmbientColor", "")));
                this.AmbientScene = this.ReadXmlElementAsByte(reader, "AmbientScene", 0);

                this.FlexSetupPort1 = this.ReadXmlElementAsByte(reader, "FlexSetupPort1", 50);
                this.FlexSetupPort2 = this.ReadXmlElementAsByte(reader, "FlexSetupPort2", 50);
                this.PixelTotalPort1 = this.ReadXmlElementAsByte(reader, "PixelTotalPort1", 50);
                this.PixelTotalPort2 = this.ReadXmlElementAsByte(reader, "PixelTotalPort2", 50);
                this.PixelVert = this.ReadXmlElementAsByte(reader, "PixelVert", 0);
                this.PixelHori = this.ReadXmlElementAsByte(reader, "PixelHori", 0);

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
                this.SetValueGainPercent(this.ReadXmlElementAsInt(reader, "ValueGainPercent", 100));
                InternalDeviceID = reader.ReadElementContentAsString("InternalDeviceID", "");
                this.SetConnectionType((ConnectionTypes)this.ReadXmlElementAsByte(reader, "ConnectionType", (byte)ConnectionTypes.UDP));
                InternalNetworkMAC = reader.ReadElementContentAsString("InternalNetworkMAC", "");
            }
            catch (Exception ex)
            {
                CGlobals.HandleException("ReadXml", ex);
            }
            finally
            {
                // Correct internal parameters
                this.CheckInternalValues();
            }
        }
        public override void UpdateFrom(CDevice devCopyFrom)
        {
            CDeviceAmbiBack myDevCopyFrom = (CDeviceAmbiBack)devCopyFrom;

            // Internal Properties
            this.bIsOnline = myDevCopyFrom.bIsOnline;
            this.SetIPAddress( myDevCopyFrom.GetIPAddress() );
            if (!this.bIsSerial)
            {   // Only Update if not already set!
                this.bIsSerial = myDevCopyFrom.bIsSerial;
                this.szSerialPort = myDevCopyFrom.szSerialPort;
            }

            // Copy Common properties
            this.Brightness = myDevCopyFrom.Brightness;

            this.ColorRedPercent = myDevCopyFrom.ColorRedPercent;
            this.ColorGreenPercent = myDevCopyFrom.ColorGreenPercent;
            this.ColorBluePercent = myDevCopyFrom.ColorBluePercent;
            this.MaxBrightnessPercent = myDevCopyFrom.MaxBrightnessPercent;
            this.MinimumLuminosityPercent = myDevCopyFrom.MinimumLuminosityPercent;
            this.FrameLength = myDevCopyFrom.FrameLength;
            this.FrameDelay = myDevCopyFrom.FrameDelay;

            this.InternalDeviceID = myDevCopyFrom.InternalDeviceID;
            this.InternalNetworkMAC = myDevCopyFrom.InternalNetworkMAC;
        }
        public override void CopyFrom(CDevice devCopyFrom)
        {
            CDeviceAmbiBack myDevCopyFrom = (CDeviceAmbiBack)devCopyFrom;

            // Internal Properties
            this.bIsOnline = myDevCopyFrom.bIsOnline;

            // Copy Common properties
            this.GroupName = myDevCopyFrom.GroupName;
            this.GroupNumber = myDevCopyFrom.GroupNumber;
            this.Mode = myDevCopyFrom.Mode;

            this.Brightness = myDevCopyFrom.Brightness;
            this.AmbientColor = Color.FromArgb(myDevCopyFrom.AmbientColor.ToArgb());
            this.AmbientScene = myDevCopyFrom.AmbientScene;

            this.FlexSetupPort1 = myDevCopyFrom.FlexSetupPort1;
            this.FlexSetupPort2 = myDevCopyFrom.FlexSetupPort2;
            this.PixelTotalPort1 = myDevCopyFrom.PixelTotalPort1;
            this.PixelTotalPort2 = myDevCopyFrom.PixelTotalPort2;
            this.PixelVert = myDevCopyFrom.PixelVert;
            this.PixelHori = myDevCopyFrom.PixelHori;

            this.MaxBytesPerPacket = myDevCopyFrom.MaxBytesPerPacket;
            this.SplitPackets = myDevCopyFrom.SplitPackets;
            this.FramesPerSecond = myDevCopyFrom.FramesPerSecond;
            this.SerialFramesPerSecond = myDevCopyFrom.SerialFramesPerSecond;
            this.InnerSizePercent = myDevCopyFrom.InnerSizePercent;
            this.ConnectionType = myDevCopyFrom.ConnectionType;
            this.ColorBoostPercent = myDevCopyFrom.ColorBoostPercent;

            this.ColorRedPercent = myDevCopyFrom.ColorRedPercent;
            this.ColorGreenPercent = myDevCopyFrom.ColorGreenPercent;
            this.ColorBluePercent = myDevCopyFrom.ColorBluePercent;
            this.MaxBrightnessPercent = myDevCopyFrom.MaxBrightnessPercent;
            this.MinimumLuminosityPercent = myDevCopyFrom.MinimumLuminosityPercent;
            this.FrameLength = myDevCopyFrom.FrameLength;
            this.FrameDelay = myDevCopyFrom.FrameDelay;
            this.ValueGainPercent = myDevCopyFrom.ValueGainPercent;
            this.InternalDeviceID = myDevCopyFrom.InternalDeviceID;
            this.InternalNetworkMAC = myDevCopyFrom.InternalNetworkMAC;
        }
        public override void SetDeviceID()
        {
            /* Generate Device ID from these values */
            /* New we also integrate the MAC ID! */
            szDeviceID = CGlobals.DEVICETYPES[(int)this.GetDeviceType()] + "_" +
                             (  InternalDeviceID != null ? 
                                this.InternalDeviceID : 
                                (this.GetIPAddress() != null ? 
                                    this.GetIPAddress().ToString() : 
                                    this.GetDeviceName()
                                )
                              ) + 
                              ( InternalNetworkMAC != null && InternalNetworkMAC.Length > 0 ? 
                                "_" + this.InternalNetworkMAC : 
                                ""
                              )
                              ;
        }
        #endregion

        #region Virtual Public Threading functions
        public override void HandleThread()
        {
            CGlobals.DebugOutput("CDeviceAmbiBack|HandleThread|Begin Thread");

            while (bAbortThread == false)
            {
                try
                {
                    // Framerate Control helpers
                    int iFrameTimeMs = 1000 / (bIsSerial ? SerialFramesPerSecond : FramesPerSecond);
                    int iLastSecond = DateTime.Now.Second;
                    int iLastMilliSec = DateTime.Now.Millisecond;
                    int iFramesPerSec = 0;

                    while (true)
                    {
                        // (RE)Init Helper Variables!
                        if (!this.bInitDone)
                        {
                            try
                            {
                                InitHelpers();

                                // Only performed ONCE per running session!
                                if (!bFirstTime)
                                {
                                    InitSettingsFromController();
                                    bFirstTime = false;
                                }
                            }
                            catch (Exception ex)
                            {
                                CGlobals.DebugOutput(ex);
                            }
                        }

                        // Colors of all LEDs from the monitor's frame buffer; skipped when there is no new frame
                        if (this.rectScanPixels != null)
                            this.CalcColorsFromFrame(this.PixelTotalPort1, this.brPixels, this.UpdateScanAreas);

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
                            iFrameTimeMs = 1000 / (bIsSerial ? SerialFramesPerSecond : FramesPerSecond);
                        }

                        // Loop to avoid running wenn Monitor is turned off
                        while (!CGlobals.monitorPowerStatus)
                        {
                            // Wait for monitor to come back online
                            CGlobals.DebugOutput("CDeviceAmbiBack:HandleThread:WaitForMonitorLoop");

                            // Sleep for a second
                            Thread.Sleep(1000);
                        }
                    }
                }
                catch (ThreadInterruptedException)
                {
                    // Interrupted
                    DoDebugOutput("CDeviceAmbiBack|HandleThread|Interrupted");

                    while (bSleepThread)
                    {
                        // Use Sleep instead of SpinWait 
                        Thread.Sleep(1);
                        // Thread.SpinWait(10000000);
                    }

                    // Interrupted
                    DoDebugOutput("CDeviceAmbiBack|HandleThread|Restart");

                    // this.SetAbortThread(true);
                }
                catch (ThreadAbortException)
                {
                    // Aborted
                    DoDebugOutput("CDeviceAmbiBack|HandleThread|Aborted");

                    this.SetAbortThread(true);
                }
                catch (Exception ex)
                {
                    CGlobals.DebugOutput(ex);
                    DoDebugOutput("CDeviceAmbiBack|HandleThread|Exception: " + ex.ToString());

                    this.SetAbortThread(true);
                }
                finally
                {
                    // nothing todo..
                }
            }

            CGlobals.DebugOutput("CDeviceAmbiBack|HandleThread|Finished Thread");
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
                    CGlobals.DebugOutput("CDeviceAmbiBack|StartThread|Unexpected Thread STATE: " + myThread.ThreadState.ToString());
                }
            }
            catch (System.InvalidOperationException)
            {
                // Bla we don't care
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
                CGlobals.DebugOutput("CDeviceAmbiBack|StartThread|Exception: " + ex.ToString());
            }
        }

        public void OpenSerial(bool bFastMode = true)
        {
            // Use Serial connection!
            if (m_clientSerial == null)
            {
                m_clientSerial = new SerialPort();
                m_clientSerial.PortName = this.szSerialPort;
                m_clientSerial.BaudRate = (int)SerialSpeed.Normal;
                m_clientSerial.Open();
                m_clientSerial.ReadTimeout = CGlobals.SerialReadTimeout;
                m_clientSerial.WriteTimeout = CGlobals.SerialWriteTimeout;

                if (bFastMode) {
                    // Make sure to switch to fast Data Mode!
                    m_clientSerial.Write("%STARTLEDDATA:");
                    m_clientSerial.Close();
                    m_clientSerial.BaudRate = this.GetMaxBaudRate();
                    m_clientSerial.Open();
                }
                // BInd serial events
                m_clientSerial.DataReceived += SerialPort_DataReceived;
                m_clientSerial.ErrorReceived += SerialPort_ErrorReceived;
            }
        }
        public void CloseSerial()
        {
            if (m_clientSerial != null)
            {
                m_clientSerial.Close();
                m_clientSerial = null;
            }
        }

        public void WriteSerialCommand(byte[] bIn)
        {
            try
            {
                if (m_clientSerial != null) {
                    m_clientSerial.Write(bIn, 0, bIn.Length);
                } else {
                    this.WriteSerialCommandDirect(bIn);
                }
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
                throw (ex);
            }
        }

        public void WriteSerialCommandDirect(byte[] bIn)
        {
            try
            {
                if ((this.ConnectionType == (byte)ConnectionTypes.USB) && this.IsSerial())
                {
                    CGlobals.DebugOutput("CDeviceAmbiBack|WriteSerialCommand:AUTO");
                    bData = bIn; 
                } else
                {
                    CGlobals.DebugOutput("CDeviceAmbiBack|WriteSerialCommand:MANUAL");
                    // Create tmp Serial connection!
                    SerialPort clientSerial = new SerialPort();
                    clientSerial.PortName = this.szSerialPort;
                    clientSerial.BaudRate = (int)SerialSpeed.Normal;
                    clientSerial.Open();
                    clientSerial.ReadTimeout = CGlobals.SerialReadTimeout;
                    clientSerial.WriteTimeout = CGlobals.SerialWriteTimeout;

                    // Now we are ready to send SERIAL Data!/

                    // Check if there is data queued!
                    if (bIn != null) {
                        clientSerial.Write(bIn, 0, bIn.Length);
                        clientSerial.Close();
                        clientSerial = null;
                    }
                }
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
                throw (ex);
            }
        }
        private void CloseClientTCP()
        {
            if (m_clientTcp != null)
            {
                m_clientTcp.Close();
                m_clientTcp = null;
            }
        }
        private void CloseClientUDP()
        {
            if (m_clientUdp != null)
            {
                m_clientUdp.Close();
                m_clientUdp = null;
            }
        }

        public void SendLEDData()
        {
            try
            {
                if ((Properties.Settings.Default.bPreferSerialConnection || this.ConnectionType == (byte)ConnectionTypes.USB) && this.IsSerial())
                {
                    // Close Network Connections in Serial Mode
                    CloseClientTCP();
                    CloseClientUDP();

                    if (bForceReload)
                    {
                        DoDebugOutput("CDeviceAmbiBack|SendLEDData|ForceSerialReload");
                        if (m_clientSerial != null)
                        {
                            try
                            {
                                m_clientSerial.Write("%ENDLEDDATA:");
                            }
                            catch (Exception ex)
                            {
                                DoDebugOutput("CDeviceAmbiBack|SendLEDData|Exception:ForceSerialReload Unexpected " + ex.ToString());
                            }

                            m_clientSerial.Close();
                            m_clientSerial = null;
                        }
                        bForceReload = false;
                    }

                    // Use function to OpenSerial Connection - error handeled in exception block here
                    this.OpenSerial();

                    // Check if there is data queued!
                    if (bData != null)
                    {
                        m_clientSerial.Write(bData, 0, bData.Length);
                        m_clientSerial.Close();
                        m_clientSerial = null;
                        bData = null;
                        // Give time to process DATA!
                        Thread.Sleep(1000);
                    }
                }
                else if (this.ConnectionType == (byte)ConnectionTypes.UDP)
                {
                    // Close Network Connection in Serial Mode FIRST!
                    CloseClientTCP();
                    if (!this.IsSerialDebug())
                        CloseSerial();

                    // Use Network connection!
                    if (m_clientUdp == null)
                    {
                        m_clientUdp = new UdpClient(this.GetIPAddress().ToString(), m_iPort);
                        m_clientUdp.DontFragment = false;
                    }
                }
                else // TCP IS DEFAULT
                {
                    // Close Network Connection in Serial Mode FIRST!
                    CloseClientUDP();
                    if (!this.IsSerialDebug())
                        CloseSerial();

                        // Use Network connection!
                        if (m_clientTcp == null) {
                        m_clientTcp = new TcpClient(this.GetIPAddress().ToString(), m_iPort);
                    } else {
                        // Recreate IP Client if not connected!
                        if (!m_clientTcp.Connected)
                            m_clientTcp = new TcpClient(this.GetIPAddress().ToString(), m_iPort);
                    }
                }
            }
            catch (ThreadInterruptedException ex)
            {
                // Handles by upper function
                throw (ex);
            }
            catch (SocketException ex)
            {
                DoDebugOutput("CDeviceAmbiBack|SendLEDData|Exception: TcpClient " + ex.ToString());
            }
            catch (System.UnauthorizedAccessException ex)
            {
                DoDebugOutput("CDeviceAmbiBack|SendLEDData|Exception: SerialClient ACCESS denied on Port " + szSerialPort + " - DISABLE SERIAL on this Device, details: " + ex.ToString());
                CloseSerial();

                // Disable serial connection until next discovery!
                this.SetIsSerial(false);
                this.szSerialPort = "";

                // trigger display Update!
                CGlobals.deviceController.HandleDiscoveredDevice(this);

                return;
            }
            catch (Exception ex)
            {
                DoDebugOutput("CDeviceAmbiBack|SendLEDData|Exception: Unexpected " + ex.ToString());
            }

            // Abort if not connected!
            if (this.IsSerial() && (m_clientSerial != null && !m_clientSerial.IsOpen))
            {
                // Recreate next time!
                DoDebugOutput("CDeviceAmbiBack|SendLEDData|SerialClient not connected, retry next loop!");
                if (!this.IsSerialDebug())
                    CloseSerial();
                return;
            }
            else if (!this.IsSerial() && (m_clientTcp != null && !m_clientTcp.Connected) )
            {
                // Recreate next time!
                DoDebugOutput("CDeviceAmbiBack|SendLEDData|Network Client not connected, retry next loop!");
                CloseClientTCP();
                return;
            }

            // Payload helpers 
            MemoryStream msPacket = new MemoryStream();
            BinaryWriter bwPacket = new BinaryWriter(msPacket);
            byte bPixelStart = 0x00; // StartIndex is 0x00 (where user configured his start! 
            try
            {
                int iCrcBytePos = TCP_HEADLEN - 1; // Default is 7!
                int iCrOoffset = TCP_HEADLEN; // Offset for checksum calc!

                if ((Properties.Settings.Default.bPreferSerialConnection || this.ConnectionType == (byte)ConnectionTypes.USB) && m_clientSerial != null) {
                    iCrcBytePos = SERIAL_HEADLEN;
                    iCrOoffset = SERIAL_HEADLEN;

                    // Write Serial Byte Detection Header
                    bwPacket.Write("%LEDDATA:");
                    bwPacket.Write((byte)0x00);
                }
                else {
                    // Write 4 Byte Network Detection Header
                    bwPacket.Write((byte)0x00);
                    bwPacket.Write((byte)0xFF);
                    bwPacket.Write((byte)0x13);
                    bwPacket.Write((byte)0x37);
                }

                // Write 4 Byte Package Header
                bwPacket.Write(this.PixelTotalPort1);   // Length
                bwPacket.Write(bPixelStart);            // StartIndex
                bwPacket.Write((byte)0x01);             // Îf set to 0x01 we use CheckSum Check
                bwPacket.Write((byte)0x00);             // Set to Checksum later!

                // add pixels to array!
                for (byte bPixelCurrent = 0; bPixelCurrent < this.PixelTotalPort1; bPixelCurrent++)
                {
                    bwPacket.Write((byte)0x00); // WHITE
                    bwPacket.Write(brPixels[bPixelCurrent].R); // RED
                    bwPacket.Write(brPixels[bPixelCurrent].G); // GREEN
                    bwPacket.Write(brPixels[bPixelCurrent].B); // BLUE
                }
                
                // Convert into byte array for Network send
                byte[] bData = msPacket.ToArray();
                bData[iCrcBytePos] = CGlobals.crc_8(bData, bData.Length, iCrOoffset);

                if ((Properties.Settings.Default.bPreferSerialConnection || this.ConnectionType == (byte)ConnectionTypes.USB) && m_clientSerial != null) {
                    // Send byte area to serial
                    if (m_clientSerial != null) 
                        m_clientSerial.Write(bData, 0, bData.Length);
                }
                else if (m_clientUdp != null)
                {
//CGlobals.DebugOutput("CDeviceAmbiBack|SendLEDData|Send UDP " + bData.Length);
                    m_clientUdp.Send(bData, bData.Length);
                }
                else if (m_clientTcp != null)
                {
                    // Get Network Stream
                    NetworkStream netStream = m_clientTcp.GetStream();

                    // Send pixels to DS as UDP Packet
                    netStream.Write(bData, 0, bData.Length);

                    // Buffer to store the response bytes.
                    bData = new Byte[64];
                    Int32 bytesRead = netStream.Read(bData, 0, bData.Length);
                    String responseData = String.Empty;
                    responseData = System.Text.Encoding.ASCII.GetString(bData, 0, bytesRead);
                    // UNCOMMENT VERBOSE
                    //                DoDebugOutput("CDeviceAmbiBack|SendLEDData: TcpClient received '" + responseData + "'");

                    // Device is ONLINE
                    this.SetIsOnline(true);
                }
                else
                {
                    // TODO: Handle USB Failure!
                    throw (new Exception("ERROR UNKNOWN CONNECTIONTYPE"));
                }
            }
            catch (ThreadAbortException ex)
            {
                // Aborted
                DoDebugOutput("CDeviceAmbiBack|SendLEDData|Aborted");
                throw (ex);
            }
            catch (IOException ex)
            {
                // IO: Exception, happens on disconnect 
                CGlobals.HandleException("SendLEDData", ex);
            }
            catch (ObjectDisposedException)
            {
                // Something is not right, close all objects
                CloseClientTCP();
                CloseClientUDP();
                CloseSerial();
            }
            catch(SocketException)
            {
                // Something is not right, close all objects
                CloseClientTCP();
                CloseClientUDP();
                CloseSerial();
            }
            catch (Exception ex)
            {
                // Something is not right, close all objects
                CloseClientTCP();
                CloseClientUDP();
                CloseSerial();

                if (brPixels == null)
                    throw (new Exception("ERROR PIXELS NOT INITIALIZED"));

                CGlobals.DebugOutput(ex);
            }
        }

        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            // TODO: Disable SerialPort!
            SerialPort sp = (SerialPort)sender;
            DoDebugOutput("CDeviceAmbiBack|SerialPort: ErrorReceived '" + e.ToString() + "'\r\n");
            this.bForceReload = true;
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            StringBuilder builder = new StringBuilder();
            bool bCmd = false;
            bool bData = false;
#pragma warning disable CS0219 // only read by the disabled reload check below
            bool bValid = false;
#pragma warning restore CS0219
            string indata = "";
            if (sp.IsOpen)
            {
                try
                {
                    // indata = (sp.IsOpen ? sp.ReadExisting().Replace("\n", "\r\n") : null);
                    indata = sp.ReadLine().Replace("\n", "\r\n");
                    string szCmd = null;
                    string szData = "";

                    if (indata == null) {
                        CGlobals.DebugOutput("CDeviceAmbiBack|SerialPort: Data received but Port is closed!\r\n", "SerialPort_DataReceived", "Error");
                        return;
                    }

                    //                    DoDebugOutput("SerialPort(" + this.szSerialPort + "): '" + indata.Trim() + "'\r\n");
                    //                    return;

                    foreach (char c in indata) {
                        if (bCmd) {
                            if (c == ':')
                            {
                                bCmd = false;
                                bData = true;
                                szCmd = builder.ToString();
                                builder.Clear();
                            }
                            else
                                builder.Append(c);
                        } else if (bData) {
                            if (c == '%')
                            {
                                bCmd = true;
                                bData = false;
                                bValid = true;
                                szData = builder.ToString();
                                builder.Clear();
                                DoDebugOutput("SerialPort(" + this.szSerialPort + "): " + szCmd.Trim() + ": '" + szData.Trim() + "'\r\n");
                            }
                            else if (c == '\0' || c == '\n')
                            {
                                bCmd = false;
                                bData = false;
                                bValid = true;
                                szData = builder.ToString();
                                builder.Clear();
                                DoDebugOutput("SerialPort(" + this.szSerialPort + "): " + szCmd.Trim() + ": '" + szData.Trim() + "'\r\n");
                            }
                            else
                                builder.Append(c);
                        }
                        else if (c == '%') {
                            // Message starts
                            builder.Clear();
                            bCmd = true;
                        }
                    }

                    // received incomplete data
                    if (bCmd || bData) {
                        bValid = true;
                        Application.DoEvents();
                        szData = builder.ToString();
                        szData += sp.ReadTo("\n");
                        DoDebugOutput("SerialPort(" + this.szSerialPort + "): " + (szCmd != null ? szCmd.Trim() : "debug") + ": '" + szData.Trim() + "'\r\n");
                    } else {
                        DoDebugOutput("SerialPort(" + this.szSerialPort + "): " + "debug: '" + (indata != null ? indata.Trim() : "") + "'\r\n");
                    }
                }
                catch (Exception)
                {   if (indata.Length == 0) {
                        indata = sp.ReadExisting();
                    }

                    DoDebugOutput("SerialPort(" + this.szSerialPort + "): " + "debug: '" + (indata != null ? indata.Trim() : "") + "'\r\n");
                    bValid = false;
//                    CGlobals.HandleException("SerialPort_DataReceived", ex);
                }

            }
/*
            if (!bValid)
            {
                try
                {
                    CGlobals.DebugOutput("CDeviceAmbiBack|SerialPort: Invalid Data received '" + indata.Trim() + "' - Force Serial Reload!\r\n");
                    this.bForceReload = true; 
                }
                catch (Exception ex)
                {
                    CGlobals.DebugOutput(ex);
                }
            }
*/
        }
        #endregion

        #region Read / Save Config variables
        public void ReadOptionsFromDevice()
        {
            // Read LED Setup
            this.ReadAllOptions();
        }
        public int ReadOption(string szUrl, int iDefault)
        {
            int iResult = iDefault;

            try
            {
                // Send prepared URL to controller
                WebRequest request = WebRequest.Create(szUrl.ToString());
                WebResponse response = request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string szResult = reader.ReadToEnd();

                JSONNode jsonResults = JSON.Parse(szResult);

                if (jsonResults.Count > 0)
                {
                    iResult = jsonResults[0].AsInt;
                }
                CGlobals.DebugOutput(szResult);

                reader.Close();
                response.Close();
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }

            // return 
            return iResult;
        }
        public byte ReadOption(string szUrl, byte bDefault)
        {
            byte bResult = bDefault;

            try
            {
                // Send prepared URL to controller
                WebRequest request = WebRequest.Create(szUrl.ToString());
                request.Timeout = 1000;
                WebResponse response = request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);

                string szResult = reader.ReadToEnd();

                JSONNode jsonResults = JSON.Parse(szResult);

                if (jsonResults.Count > 0)
                {
                    bResult = (byte)jsonResults[0].AsInt;
                }
                CGlobals.DebugOutput(szResult);

                reader.Close();
                response.Close();
            }
            catch (System.Net.WebException ex)
            {
                if (ex.Status == WebExceptionStatus.Timeout)
                    throw (ex);
                else
                    CGlobals.DebugOutput(ex);
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }

            // return 
            return bResult;
        }
        public void ReadAllOptions()
        {
            try
            {
                // Send prepared URL to controller
                WebRequest request = WebRequest.Create("http://" + this.GetIPAddress().ToString() + "/getparamatersasjson");
                request.Timeout = 1000;
                WebResponse response = request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);

                string szResult = reader.ReadToEnd();
                JSONNode jsonResults = JSON.Parse(szResult);
                if (jsonResults.Count > 0)
                {
                    // Process data from json stream!

                    // Read Default Vars
                    if (jsonResults["devicename"] != null)
                        this.SetDeviceName (jsonResults["devicename"]);

                    // Read LED Setup
                    if (jsonResults["vertical"] != null)
                        this.SetPixelVert((byte)jsonResults["vertical"].AsInt);
                    if (jsonResults["horizontal"] != null)
                        this.SetPixelHori((byte)jsonResults["horizontal"].AsInt);
                    if (jsonResults["direction"] != null)
                        this.SetFlexPort1((byte)jsonResults["direction"].AsInt);

                    // Read Color Properties from device now
                    // Convert to BYTE max 255
                    if (jsonResults["colorred"] != null)
                        this.ColorRedPercent = (byte)((jsonResults["colorred"].AsInt + 1) * 100 / 256);
                    if (jsonResults["colorgreen"] != null)
                        this.ColorGreenPercent = (byte)((jsonResults["colorgreen"].AsInt + 1) * 100 / 256);
                    if (jsonResults["colorblue"] != null)
                        this.ColorBluePercent = (byte)((jsonResults["colorblue"].AsInt + 1) * 100 / 256);
                    if (jsonResults["brightness"] != null)
                        this.MaxBrightnessPercent = (byte)((jsonResults["brightness"].AsInt + 1) * 100 / 256);
                    if (jsonResults["minimumluminosity"] != null)
                        this.MinimumLuminosityPercent = (byte)((jsonResults["minimumluminosity"].AsInt+1) * 100 / 256);
                    if (jsonResults["colorvaluegain"] != null) {
                        SetValueGainPercent(jsonResults["colorvaluegain"].AsInt);
                    }
                    if (jsonResults["colorboost"] != null)
                        this.ColorBoostPercent = (byte)((jsonResults["colorboost"].AsInt * 100) / 256);

                    // Read Other Properties from device now
                    if (jsonResults["framelength"] != null)
                        this.FrameLength = (byte)((jsonResults["framelength"].AsInt * 100) / 256);
                    if (jsonResults["framedelay"] != null)
                        this.FrameDelay = jsonResults["framedelay"].AsInt;

                }
                CGlobals.DebugOutput(szResult);

                reader.Close();
                response.Close();
            }
            catch (System.Net.WebException ex)
            {
                if (ex.Status == WebExceptionStatus.Timeout)
                    throw (ex);
                else
                    CGlobals.DebugOutput(ex);
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
        }
        public void SetValueGainPercent(int iValue)
        {
            if (iValue < 100)
                iValue = 100;
            else if (iValue > 500)
                iValue = 500;
            this.ValueGainPercent = iValue;
        }

        /*
        public void ReadLedSetup()
        {
            // Read LED Setup Values from device
            byte bValue = ReadOption("http://" + this.GetIPAddress().ToString() + "/getvertical", (byte)(this.PixelVert));
            this.SetPixelVert(bValue);
            bValue = ReadOption("http://" + this.GetIPAddress().ToString() + "/gethorizontal", (byte)(this.PixelHori));
            this.SetPixelHori(bValue);
            bValue = ReadOption("http://" + this.GetIPAddress().ToString() + "/getdirection", (byte)(this.FlexSetupPort1));
            this.SetFlexPort1(bValue);
        }
        public void ReadColorRedPercent()
        {
            // Read Value from device
            byte bValue = ReadOption("http://" + this.GetIPAddress().ToString() + "/getcolorred", (byte)((this.ColorRedPercent * 256) / 100));
            // Convert to BYTE max 255
            this.ColorRedPercent = (byte)((bValue * 100) / 256);
        }
        public void ReadColorGreenPercent()
        {
            // Read Value from device
            byte bValue = ReadOption("http://" + this.GetIPAddress().ToString() + "/getcolorgreen", (byte)((this.ColorGreenPercent * 256) / 100));
            // Convert to BYTE max 255
            this.ColorGreenPercent = (byte)((bValue * 100) / 256);
        }
        public void ReadColorBluePercent()
        {
            // Read Value from device
            byte bValue = ReadOption("http://" + this.GetIPAddress().ToString() + "/getcolorblue", (byte)((this.ColorBluePercent * 256) / 100));
            // Convert to BYTE max 255
            this.ColorBluePercent = (byte)((bValue * 100) / 256);
        }
        public void ReadMaxBrightness()
        {
            // Read Value from device
            byte bValue = ReadOption("http://" + this.GetIPAddress().ToString() + "/getbrightness", (byte)((this.MaxBrightnessPercent * 256) / 100));
            // Convert to BYTE max 255
            this.MaxBrightnessPercent = (byte)((bValue * 100) / 256);
        }
        public void ReadMinimumLuminosity()
        {
            // Read Value from device
            byte bValue = ReadOption("http://" + this.GetIPAddress().ToString() + "/getminimumluminosity", (byte)((this.MinimumLuminosityPercent * 256) / 100));
            // Convert to BYTE max 255
            this.MinimumLuminosityPercent = (byte)((bValue * 100) / 256);
        }
        public void ReadValueGain()
        {
            // Read Value from device
            int iValue = ReadOption("http://" + this.GetIPAddress().ToString() + "/getcolorvaluegain", (this.ValueGainPercent));
            if (iValue < 100)
                iValue = 100;
            else if (iValue > 300)
                iValue = 300;
            this.ValueGainPercent = iValue;
        }
        public void ReadColorBoost()
        {
            // Read Value from device
            byte bValue = ReadOption("http://" + this.GetIPAddress().ToString() + "/getcolorboost", (byte)(this.ColorBoostPercent));
            // Convert to BYTE max 255
            this.ColorBoostPercent = (byte)(bValue);
        }
        public void ReadFrameLength()
        {
            // Read Value from device
            byte bValue = ReadOption("http://" + this.GetIPAddress().ToString() + "/getframelength", this.FrameLength);
            // Convert to BYTE max 255
            // Can  be between 10 and 100
            this.FrameLength = (byte)((bValue * 100) / 255);
        }
        public void ReadFrameDelay()
        {
            // Read Value from device
            int iValue = ReadOption("http://" + this.GetIPAddress().ToString() + "/getframedelay", this.FrameDelay);
            // Can  be between 10 and 1000
            this.FrameDelay = iValue;
        }
        */

        public void WriteOption(string szUrl)
        {
            try
            {
                // Send prepared URL to controller
                WebRequest request = WebRequest.Create(szUrl.ToString());
                WebResponse response = request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string szResult = reader.ReadToEnd();
                CGlobals.DebugOutput(szResult);
                reader.Close();
                response.Close();
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
        }
        public void SendColorRedPercent()
        {
            // Convert to BYTE max 255
            byte bValue = (byte)((this.ColorRedPercent * 255) / 100);
            // Build URL and send to Controller
            WriteOption("http://" + this.GetIPAddress().ToString() + "/setcolorred?value=" + bValue.ToString());
        }
        public void SendColorGreenPercent()
        {
            // Convert to BYTE max 255
            byte bValue = (byte)((this.ColorGreenPercent * 255) / 100);
            // Build URL and send to Controller
            WriteOption("http://" + this.GetIPAddress().ToString() + "/setcolorgreen?value=" + bValue.ToString());
        }
        public void SendColorBluePercent()
        {
            // Convert to BYTE max 255
            byte bValue = (byte)((this.ColorBluePercent * 255) / 100);
            // Build URL and send to Controller
            WriteOption("http://" + this.GetIPAddress().ToString() + "/setcolorblue?value=" + bValue.ToString());
        }
        public void SendMaxBrightness()
        {
            // Convert to BYTE max 255
            byte bValue = (byte)((this.MaxBrightnessPercent * 255) / 100);
            // Build URL and send to Controller
            WriteOption("http://" + this.GetIPAddress().ToString() + "/setbrightness?value=" + bValue.ToString());
        }
        public void SendMinimumLuminosity()
        {
            // Convert to BYTE max 255
            byte bValue = (byte)((this.MinimumLuminosityPercent * 255) / 100);
            // Build URL and send to Controller
            WriteOption("http://" + this.GetIPAddress().ToString() + "/setminimumluminosity?value=" + bValue.ToString());
        }
        public void SendValueGain()
        {
            // Convert to BYTE max 255
            int iValue = this.ValueGainPercent;
            // Build URL and send to Controller
            WriteOption("http://" + this.GetIPAddress().ToString() + "/setcolorvaluegain?value=" + iValue.ToString());
        }
        public void SendColorBoost()
        {
            // Convert to BYTE max 255
            byte bValue = this.ColorBoostPercent;
            // Build URL and send to Controller
            WriteOption("http://" + this.GetIPAddress().ToString() + "/setcolorboost?value=" + bValue.ToString());
        }
        public void SendFrameLength()
        {
            // max 255
            byte bValue = this.FrameLength;
            // Build URL and send to Controller
            WriteOption("http://" + this.GetIPAddress().ToString() + "/setframelength?value=" + bValue.ToString());
        }
        public void SendFrameDelay()
        {
            // max 1000
            int iValue = this.FrameDelay;
            // Build URL and send to Controller
            WriteOption("http://" + this.GetIPAddress().ToString() + "/setframedelay?value=" + iValue.ToString());
        }
        public void SendDeviceName()
        {
            // Build URL and send to Controller
            WriteOption("http://" + this.GetIPAddress().ToString() + "/setdevicename?value=" + this.GetDeviceName());
        }
        public void SendLedVertical()
        {
            // Build URL and send to Controller
            WriteOption("http://" + this.GetIPAddress().ToString() + "/setvertical?value=" + this.PixelVert.ToString());
        }
        public void SendLedHorizontal()
        {
            // Build URL and send to Controller
            WriteOption("http://" + this.GetIPAddress().ToString() + "/sethorizontal?value=" + this.PixelHori.ToString());
        }
        public void SendLedDirection()
        {
            // Build URL and send to Controller
            WriteOption("http://" + this.GetIPAddress().ToString() + "/setdirection?value=" + this.FlexSetupPort1.ToString());
        }

        public void StartSyslogDebugging(string szServer, string szPort)
        {
            // Enable Syslog debugging on controller!
            WriteOption("http://" + this.GetIPAddress().ToString() + "/setdebugsyslogserver?value=" + szServer);
            WriteOption("http://" + this.GetIPAddress().ToString() + "/setdebugsyslogport?value=" + szPort);
            WriteOption("http://" + this.GetIPAddress().ToString() + "/setdebugsyslog?value=1");
        }
        public void StopSyslogDebugging()
        {
            // Disable Syslog debugging on controller!
            WriteOption("http://" + this.GetIPAddress().ToString() + "/setdebugsyslog?value=0");
        }
        #endregion

        #region Pixel Processing functions 
        // WE NOW CALC FROM FRONT VIEW
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
            else if (this.FlexSetupPort1 == (byte)DsFlexSetups.Top_Cclock_top_right_left)
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
            else if (this.FlexSetupPort1 == (byte)DsFlexSetups.Left_Cclock_top_left_down)
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
            else if (this.FlexSetupPort1 == (byte)DsFlexSetups.Bottom_Cclock_bottom_left_right)
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
            else if (this.FlexSetupPort1 == (byte)DsFlexSetups.Right_Clock_top_right_down)
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
            else if (this.FlexSetupPort1 == (byte)DsFlexSetups.Top_Clock_top_left_right)
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
            else if (this.FlexSetupPort1 == (byte)DsFlexSetups.Left_Clock_bottom_left_up)
            {
                iOffset = CalcLeftRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.BottomUp, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcTopRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.LeftToRight, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcRightRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.TopDown, rectPixels, iOffset);
                if (this.PixelTotalPort1 > iOffset)
                    iOffset = CalcBottomRectangles(this.PixelVert, this.PixelHori, iLeftOffset, iTopOffset, iScanWidth, iScanHeight, iIntoImagePx, FlowDirection.RightToLeft, rectPixels, iOffset);
            }
            // ClockWise Bottom Right to Left
            else if (this.FlexSetupPort1 == (byte)DsFlexSetups.Bottom_Clock_bottom_right_left)
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
        public void InitSettingsFromController()
        {
            try
            {
                // Try to refresh values from device FIRST!
                this.ReadOptionsFromDevice();
                DoDebugOutput("CDeviceAmbiBack|InitSettingsFromController SUCCESS");
            }
            catch (WebException exweb)
            {
                DoDebugOutput("CDeviceAmbiBack|InitSettingsFromController FAILED with Timeout");
                if (exweb.Status == WebExceptionStatus.Timeout)
                {
                    // Device OFFLINE
                    this.SetIsOnline(false);
                }
            }
            catch (Exception ex)
            {
                CGlobals.HandleException("HandleThread", ex);
            }
        }

        /// <summary>Scan rectangles for a screenshot of this size (also after a resolution change).</summary>
        private void UpdateScanAreas(int iScreenWidth, int iScreenHeight)
        {
            this.CalcRectangles(0, 0, iScreenWidth, iScreenHeight, 0, this.rectScanPixels);
            this.CalcCorners(this.PixelTotalPort1, this.rectScanPixels);
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
                    brPixels = new Color[this.PixelTotalPort1];
                    lColorsFrameNumber = -1;   // new (black) colors: calculate on the next call

                    // Init Screen Scan Areas
                    rectScanPixels = new Rectangle[this.PixelTotalPort1];
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

        public int GetMaxBaudRate()
        {
            if (m_DeviceType == DeviceType.AmbiBack_Cam_m5_Controller ||
                m_DeviceType == DeviceType.AmbiBack_Cam_5x_Controller ||
                m_DeviceType == DeviceType.AmbiBack_Cam_5tx_Controller ||
                m_DeviceType == DeviceType.AmbiBack_Pc_M5AL_Controller ||
                m_DeviceType == DeviceType.AmbiBack_Pc_M5AX_Controller ||
                m_DeviceType == DeviceType.AmbiBack_Led_M5A1_Controller
                ) {
                return (int)SerialSpeed.FastM5Stack;
            } else {
                return (int)SerialSpeed.FastESP32;
            }
        }

        #endregion
    }
}

