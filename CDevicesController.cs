using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Custom
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Diagnostics;
using AmbiBack.PhilipsHue;
using System.Threading;
using System.IO.Ports;
using SharpDX.Direct3D11;
using System.Windows.Forms;
using System.Collections;

namespace AmbiBack
{
#region Public Event definitions
    public delegate void DeviceFinderDiscoveredDevice(object sender, System.EventArgs e, CDevice newDevice);
#if USE_DREAMSCREEN
    public delegate void DeviceFinderReceivedDSPacket(object sender, System.EventArgs e, IPAddress myIpAddress, DreamScreenPacket dsPacket );
#endif
#endregion

    /* Helper to class to communicate with Devices */
    public class CDevicesController
    {
#region Threadstuff
        public System.Threading.Thread myThread = null;	// My Thread Object
        public bool AbortThread { get; set; } = false;
        public bool SleepThread { get; set; } = false;
        public int m_nThreadID = -1;                     // INT Number used to identify the thread

        // Helper to set if we should start enum devices
        public bool DoEnumDevices { get; set; } = false;
        #endregion

        // Public variables
#if USE_DREAMSCREEN
        public int udpPortNumberDreamScreen = 8888;
#endif
        public int udpPortNumberAmbiBack = 1337;
        public string[] serialPorts = null; // Init with SerialPort Names

        #region Public Events
        public event DeviceFinderDiscoveredDevice DiscoveredDevice;
#if USE_DREAMSCREEN
        public event DeviceFinderReceivedDSPacket ReceivedDSPacket;
#endif
#endregion

        // UDP Socket
        private Socket udpSocket;
        private IPHostEntry localhostEntry;
        private IPEndPoint localEndPoint;
#if USE_DREAMSCREEN
        private UdpClient udpClientDreamScreen;
        private object udpStateDreamScreen;
#endif
        private UdpClient udpClientAmbiBack; 
        private object udpStateAmbiBack;

#region Helper variables 
        private readonly byte[] uartComm_crc8_table = new byte[]{
            0x00 , 0x07 , 0x0E , 0x09 , 0x1C , 0x1B , 0x12 , 0x15 , 0x38 , 0x3F , 0x36 , 0x31 , 0x24 ,
            0x23 , 0x2A , 0x2D , 0x70 , 0x77 , 0x7E , 0x79 , 0x6C , 0x6B , 0x62 , 0x65 , 0x48 , 0x4F , 0x46 ,
            0x41 , 0x54 , 0x53 , 0x5A , 0x5D , ( byte ) 0xE0 , ( byte ) 0xE7 , ( byte ) 0xEE , ( byte ) 0xE9 ,
            ( byte ) 0xFC , ( byte ) 0xFB , ( byte ) 0xF2 , ( byte ) 0xF5 , ( byte ) 0xD8 , ( byte ) 0xDF , ( byte )
            0xD6 , ( byte ) 0xD1 , ( byte ) 0xC4 , ( byte ) 0xC3 , ( byte ) 0xCA , ( byte ) 0xCD , ( byte ) 0x90 ,
            ( byte ) 0x97 , ( byte ) 0x9E , ( byte ) 0x99 , ( byte ) 0x8C , ( byte ) 0x8B , ( byte ) 0x82 , ( byte )
            0x85 , ( byte ) 0xA8 , ( byte ) 0xAF , ( byte ) 0xA6 , ( byte ) 0xA1 , ( byte ) 0xB4 , ( byte ) 0xB3 ,
            ( byte ) 0xBA , ( byte ) 0xBD , ( byte ) 0xC7 , ( byte ) 0xC0 , ( byte ) 0xC9 , ( byte ) 0xCE , ( byte )
            0xDB , ( byte ) 0xDC , ( byte ) 0xD5 , ( byte ) 0xD2 , ( byte ) 0xFF , ( byte ) 0xF8 , ( byte ) 0xF1 ,
            ( byte ) 0xF6 , ( byte ) 0xE3 , ( byte ) 0xE4 , ( byte ) 0xED , ( byte ) 0xEA , ( byte ) 0xB7 , ( byte )
            0xB0 , ( byte ) 0xB9 , ( byte ) 0xBE , ( byte ) 0xAB , ( byte ) 0xAC , ( byte ) 0xA5 , ( byte ) 0xA2 ,
            ( byte ) 0x8F , ( byte ) 0x88 , ( byte ) 0x81 , ( byte ) 0x86 , ( byte ) 0x93 , ( byte ) 0x94 , ( byte )
            0x9D , ( byte ) 0x9A , 0x27 , 0x20 , 0x29 , 0x2E , 0x3B , 0x3C , 0x35 , 0x32 , 0x1F , 0x18 , 0x11 ,
            0x16 , 0x03 , 0x04 , 0x0D , 0x0A , 0x57 , 0x50 , 0x59 , 0x5E , 0x4B , 0x4C , 0x45 , 0x42 , 0x6F ,
            0x68 , 0x61 , 0x66 , 0x73 , 0x74 , 0x7D , 0x7A , ( byte ) 0x89 , ( byte ) 0x8E , ( byte ) 0x87 ,
            ( byte ) 0x80 , ( byte ) 0x95 , ( byte ) 0x92 , ( byte ) 0x9B , ( byte ) 0x9C , ( byte ) 0xB1 , ( byte )
            0xB6 , ( byte ) 0xBF , ( byte ) 0xB8 , ( byte ) 0xAD , ( byte ) 0xAA , ( byte ) 0xA3 , ( byte ) 0xA4 ,
            ( byte ) 0xF9 , ( byte ) 0xFE , ( byte ) 0xF7 , ( byte ) 0xF0 , ( byte ) 0xE5 , ( byte ) 0xE2 , ( byte )
            0xEB , ( byte ) 0xEC , ( byte ) 0xC1 , ( byte ) 0xC6 , ( byte ) 0xCF , ( byte ) 0xC8 , ( byte ) 0xDD ,
            ( byte ) 0xDA , ( byte ) 0xD3 , ( byte ) 0xD4 , 0x69 , 0x6E , 0x67 , 0x60 , 0x75 , 0x72 , 0x7B ,
            0x7C , 0x51 , 0x56 , 0x5F , 0x58 , 0x4D , 0x4A , 0x43 , 0x44 , 0x19 , 0x1E , 0x17 , 0x10 , 0x05 ,
            0x02 , 0x0B , 0x0C , 0x21 , 0x26 , 0x2F , 0x28 , 0x3D , 0x3A , 0x33 , 0x34 , 0x4E , 0x49 , 0x40 ,
            0x47 , 0x52 , 0x55 , 0x5C , 0x5B , 0x76 , 0x71 , 0x78 , 0x7F , 0x6A , 0x6D , 0x64 , 0x63 , 0x3E ,
            0x39 , 0x30 , 0x37 , 0x22 , 0x25 , 0x2C , 0x2B , 0x06 , 0x01 , 0x08 , 0x0F , 0x1A , 0x1D , 0x14 ,
            0x13 , ( byte ) 0xAE , ( byte ) 0xA9 , ( byte ) 0xA0 , ( byte ) 0xA7 , ( byte ) 0xB2 , ( byte ) 0xB5 ,
            ( byte ) 0xBC , ( byte ) 0xBB , ( byte ) 0x96 , ( byte ) 0x91 , ( byte ) 0x98 , ( byte ) 0x9F , ( byte )
            0x8A , ( byte ) 0x8D , ( byte ) 0x84 , ( byte ) 0x83 , ( byte ) 0xDE , ( byte ) 0xD9 , ( byte ) 0xD0 ,
            ( byte ) 0xD7 , ( byte ) 0xC2 , ( byte ) 0xC5 , ( byte ) 0xCC , ( byte ) 0xCB , ( byte ) 0xE6 , ( byte )
            0xE1 , ( byte ) 0xE8 , ( byte ) 0xEF , ( byte ) 0xFA , ( byte ) 0xFD , ( byte ) 0xF4 , ( byte ) 0xF3
        };
#endregion

        public CDevicesController()
        {
            try
            {
                // Start our handling thread here!
                this.StartThread();
            }
            catch (Exception ex)
            {
                CGlobals.HandleException("CDevicesController", ex, true, false);
            }
        }

        /// <summary>Entertainment area device of the given configuration id, or null.</summary>
        public CDeviceHueEntertainment FindHueEntertainmentDevice(string configId)
        {
            CDeviceHueEntertainment pcGroup = HuePcGroup.FindDevice();
            if (pcGroup != null && pcGroup.HueConfigID == configId)
                return pcGroup;
            foreach (CDevice myDevice in CGlobals.mainConfig.Devices.GetDeviceList())
                if (myDevice is CDeviceHueEntertainment existing && existing.HueConfigID == configId)
                    return existing;
            return null;
        }

        /// <summary>Adds an entertainment area of the bridge as streaming device (chosen in frmConnectHue).</summary>
        public CDeviceHueEntertainment AddHueEntertainmentArea(EntertainmentArea area)
        {
            if (area.Name == HuePcGroup.AreaName)
                return HuePcGroup.EnsureDevice(area);
            CDeviceHueEntertainment device = FindHueEntertainmentDevice(area.ID);
            if (device == null)
                device = (CDeviceHueEntertainment)CGlobals.mainConfig.Devices.Add(new CDeviceHueEntertainment(area));
            device.SetIsOnline(true);
            HandleDiscoveredDevice(device);
            return device;
        }

        /// <summary>Marks the added entertainment areas online and refreshes their layout (needs the client key).</summary>
        public void EnumHueEntertainmentAreas()
        {
            List<EntertainmentArea> areas = CGlobals.HueEntertainmentAreas;
            if (areas == null)
                return;

            foreach (EntertainmentArea area in areas)
            {
                // The area of this PC always shows up in the list
                if (area.Name == HuePcGroup.AreaName)
                {
                    HuePcGroup.EnsureDevice(area);
                    continue;
                }
                CDeviceHueEntertainment device = FindHueEntertainmentDevice(area.ID);
                if (device == null)
                    continue;    // only areas the user added
                if (!device.IsThreadRunningSafe())
                {
                    // Keep the stored layout current while not streaming
                    device.HueAreaName = area.Name;
                    device.HueChannels = EntertainmentChannel.Format(area.Channels);
                }
                device.SetIsOnline(true);
                HandleDiscoveredDevice(device);
            }
        }

        public void EnumHueDevices()
        {
            // Streaming areas first, then the single lights (REST fallback)
            this.EnumHueEntertainmentAreas();

            // Skip if no Hue available
            if (CGlobals.HueLights == null)
                return; 

            // Loop Through Hue Lights
            foreach (Light hueLight in CGlobals.HueLights)
            {
                // Only lights that can show colors; plugs, switches and white only lights are left out
                if (!hueLight.IsColorLight)
                    continue;

                // Check if device was already added
                CDevice newDevice = null; 
                foreach (CDevice myDevice in CGlobals.mainConfig.Devices.GetDeviceList()) {
                    if (myDevice.GetDeviceType() == DeviceType.PhilipsHueColorLight)
                    {
                        CDevicePhilipsHueLight myHueDevice = (CDevicePhilipsHueLight)myDevice;
                        if (hueLight.UniqueID == myHueDevice.HueUniqueID)
                        {
                            newDevice = myHueDevice;
                            break; 
                        }
                    }
                }

                // Add device if not known yet!
                if (newDevice == null)
                {
                    // Reached here so create a new Device
                    newDevice = new CDevicePhilipsHueLight(DeviceType.PhilipsHueColorLight, hueLight.Name);
                    // Set Hue Specific parameters
                    CDevicePhilipsHueLight philipsDevice = (CDevicePhilipsHueLight)newDevice;
                    philipsDevice.SetHueUniqueID(hueLight.UniqueID);
                    philipsDevice.HueType = hueLight.Type;
                    philipsDevice.HueModelID = hueLight.ModelID;
                    // Create Service ID now!
                    newDevice.SetDeviceID();

                    // Ádd device to Main config
                    newDevice = CGlobals.mainConfig.Devices.Add(newDevice);
                }

                // Device is online
                newDevice.SetIsOnline(true);

                // Handle UI Updates here
                HandleDiscoveredDevice(newDevice);
            }
        }

        /// <summary>
        /// Adds the WLED devices that answer mDNS, or refreshes address and LED count of known ones
        /// (their layout and sliders are kept).
        /// </summary>
        public void EnumWledDevices()
        {
            try
            {
                foreach (CDeviceWled.WledInfo info in CDeviceWled.Discover())
                {
                    CDevice device = CGlobals.mainConfig.Devices.Add(CDeviceWled.FromInfo(info));
                    device.SetIsOnline(true);
                    HandleDiscoveredDevice(device);
                }
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
        }

        /// <summary>
        /// Adds the Govee devices with LAN Control that answer the scan, or refreshes the address of known
        /// ones (their layout, mode and sliders are kept).
        /// </summary>
        public void EnumGoveeDevices()
        {
            try
            {
                foreach (CDeviceGovee.GoveeInfo info in CDeviceGovee.Discover())
                {
                    CDevice device = CGlobals.mainConfig.Devices.Add(CDeviceGovee.FromInfo(info));
                    device.SetIsOnline(true);
                    HandleDiscoveredDevice(device);
                }
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
        }

        public void HandleDiscoveredDevice(CDevice newDevice)
        {
            if (this.DiscoveredDevice != null)
                this.DiscoveredDevice(this, null, newDevice);
        }

        public bool UpdateComPorts()
        {
            bool bChanged = false;
            string[] aPorts = SerialPort.GetPortNames();


            if (serialPorts == null || serialPorts.Length == 0)
            {
                bChanged = true;
            }
            else if (aPorts.Length != serialPorts.Length)
            {
                bChanged = true;
            }
            else
                    {
                foreach (string szExistingPort in serialPorts)
                {
                    if (!aPorts.Contains(szExistingPort))
                    {
                        bChanged = true;
                        break;
                    }
                }
            }

            if (bChanged)
            {
                // Copy new List of serial ports!
                serialPorts = aPorts;
            }

            // return result
            return bChanged; 
        }
        public void ReadInfoFromComPorts()
        {
            // Stop / Disable Serial Conenctions first!
            ArrayList aDevices = CGlobals.mainConfig.Devices.GetDeviceList();
            foreach (CDevice myDevice in aDevices)
            {
                if (myDevice.GetType().ToString() == "AmbiBack.CDeviceAmbiBack")
                {
                    // Cast object
                    CDeviceAmbiBack ambiBackDevice = (CDeviceAmbiBack)myDevice;

                    // Remove Serial Properties, will automatically stop using them!
                    if (ambiBackDevice.IsSerial()) {
                        ambiBackDevice.SetIsSerial(false);
                        ambiBackDevice.szSerialPort = "";
                    }

                    // trigger display Update!
                    HandleDiscoveredDevice(ambiBackDevice);
                }
            }
            
            // return if there are no serial ports found 
            if (serialPorts == null || serialPorts.Length == 0)
                return;

            // create new serial Port and get INFO properties!
            SerialPort serialPort = new SerialPort();
            serialPort.ReadTimeout = CGlobals.SerialReadTimeout;
            serialPort.WriteTimeout = CGlobals.SerialWriteTimeout;

            foreach (string szExistingPort in serialPorts)
            {
                JSONNode jsonResults = null;
                string szJson = null;
                try
                {
                    // Set properties and try to open Com Port
                    serialPort.PortName = szExistingPort;
                    serialPort.BaudRate = (int)SerialSpeed.Normal; // Try default rate
                    serialPort.Open();

                    // Get Info from data!
                    serialPort.Write("%INFO:");
                    // Read until end of JSON String
                    szJson = serialPort.ReadTo("}\n") + "}\n";

                    // Close Serial Port
                    serialPort.Close();
                }
                catch (System.TimeoutException)
                {
                    if (serialPort.IsOpen)
                        serialPort.Close();

                    // Try again with higher BAUDRATE
                    try
                    {
                        serialPort.BaudRate = (int)SerialSpeed.FastM5Stack; // Try default M5 rate
                        serialPort.Open();

                        // Get Info from data!
                        serialPort.Write("%INFO:");
                        // Read until end of JSON String
                        szJson = serialPort.ReadTo("}\n") + "}\n";

                        // Close Serial Port
                        serialPort.Close();
                    }
                    catch (Exception ex2)
                    {
                        CGlobals.DebugOutput(ex2);
                    }
                }
                catch (System.UnauthorizedAccessException ex)
                {
                    // ACCESS DENIED!
                    CGlobals.HandleException("ReadInfoFromComPorts", ex);
                }
                catch (Exception ex)
                {
                    CGlobals.HandleException("ReadInfoFromComPorts", ex);
                } 
                finally
                {
                    if (serialPort.IsOpen)
                        serialPort.Close();
                }

                Application.DoEvents();

                try
                {
                    // Add device if data was found!
                    if (szJson != null && szJson.Length > 0 && szJson.Contains('{'))
                    {
                        // Get correct part of string!
                        szJson = szJson.Substring(szJson.LastIndexOf('{'));

                        jsonResults = JSON.Parse(szJson);
                        if (jsonResults.Count > 0)
                        {
                            // Handle found device
                            this.CheckAndAddDevice(jsonResults, true, szExistingPort);
                        }
                    }
                }
                catch (Exception ex)
                {
                    CGlobals.HandleException("ReadInfoFromComPorts", ex);
                    if (szJson != null) 
                        CGlobals.DebugOutput(szJson);
                }


                // Process GUI stuff
                Application.DoEvents();
            }
        }

        private void EnumDevices()
        {
// TODO: USE OWN THREAD FOR DISCOVERY

            // Enum Hue Lights FIRST!
            this.EnumHueDevices();

            // WLED via mDNS and Govee via its LAN scan in the background (each waits about 2 s for answers)
            System.Threading.Tasks.Task.Run(() => this.EnumWledDevices());
            System.Threading.Tasks.Task.Run(() => this.EnumGoveeDevices());

            // UDP Helper Client
            UdpClient client = new UdpClientBroadcast();

#if USE_DREAMSCREEN
            // Start DreamScreen Device discovery
            IPEndPoint ipBroadcastDreamScreen = new IPEndPoint(IPAddress.Broadcast, this.udpPortNumberDreamScreen);
            byte[] bytes = new byte[0]; // { 0x01 }; // CreateUdpPacket(DsCommands.CurrentState, null, true);
            byte[] bBroadcast = CreateUdpPacket(DsCommands.CurrentState, new byte[0], 0xFF /* All Groups?! */, (byte)DsFlags.Receive, true);
            CGlobals.DebugOutput("EnumDevices: Sending Broadcast DreamScreen discovery"); 
            client.Send(bBroadcast, bBroadcast.Length, ipBroadcastDreamScreen);
#endif
            // Start AmbiBack Device discovery
            IPEndPoint ipBroadcastAmbiBack = new IPEndPoint(IPAddress.Broadcast, this.udpPortNumberAmbiBack);
            byte[] bBroadCastAmbiBack = CGlobals.GetByteArrayFromString(CGlobals.AMBIBACK_BROADCAST_DISCOVER);  
            CGlobals.DebugOutput("EnumDevices: Sending Broadcast AmbiBack discovery");
            client.Send(bBroadCastAmbiBack, bBroadCastAmbiBack.Length, ipBroadcastAmbiBack);
        }
#if USE_DREAMSCREEN

        public void SendDreamscreenCommand(CDeviceDreamScreen myDevice, DsCommands dsCommand, byte bGroupNumber, byte bFlags, byte[] bPayload)
        {
            UdpClient client = new UdpClientBroadcast();
            IPEndPoint ipUnicast = new IPEndPoint(myDevice.GetIPAddress(), this.udpPortNumberDreamScreen);
            byte[] bytes = new byte[0];
            if (bPayload != null)
                bytes = bPayload; 

            // Create UDP Packet 
            byte[] bUnicast = CreateUdpPacket(dsCommand, bPayload, bGroupNumber /* myDevice.GetGroupNumber() */, bFlags, false);

            // Send to target
            client.Send(bUnicast, bUnicast.Length, ipUnicast); 

            // Send UDP Packet
            CGlobals.DebugOutput("SendDreamscreenCommand: Sending Unicast Command: " + dsCommand.ToString());
        }
#endif

#region Private helpers
        private bool IsLocalBroadcast(IPEndPoint ipCheck)
        {
            bool bFound = false;

            foreach (IPAddress localIp in localhostEntry.AddressList)
            {
                /* Local ipCheck matching */
                 
                if (localIp.ToString() == ipCheck.Address.ToString() )
                    //.Address.Equals( localIp.Address) )
                    return true; 
            }

            return bFound; 
        }

#if USE_DREAMSCREEN
        private void StartUdpListeningDreamScreem()
        {
            udpClientDreamScreen.BeginReceive(UdpReceiverDreamScreen, udpStateDreamScreen = new object());
        }
        private void UdpReceiverDreamScreen(IAsyncResult ar)
        {
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, this.udpPortNumberDreamScreen);
            bool bProcess = true;
            byte[] bytes = udpClientDreamScreen.EndReceive(ar, ref ip);

            if (IsLocalBroadcast(ip))
            {
                CGlobals.DebugOutput("Skip own selfsend broadcasts!");
                bProcess = false; 
            }

            if (bProcess)
            {
                // string message = Encoding.ASCII.GetString(bytes);
                CGlobals.DebugOutput("Received message from " + ip.Address.ToString());

                // Process Udp Packet
                DreamScreenPacket dreamScreenPacket = AnalyzeUdpPacket(bytes);

                // needed for new device
                CDevice newDevice = null;
                bool bTriggerPacketEvent = false; 
                string szDeviceName = null; 

                /* Handle commands */
                switch ((DsCommands)dreamScreenPacket.u16Command)
                {
                    case DsCommands.CurrentState:
                        CGlobals.DebugOutput("Received CurrentState Packet, processing...");
                        if (dreamScreenPacket.bPayload.Length > 1)
                        {
                            if (dreamScreenPacket.bPayload[dreamScreenPacket.bPayload.Length - 2] == (byte)DsDeviceType.DreamScreenHD)
                            {
                                szDeviceName = Encoding.UTF8.GetString(dreamScreenPacket.bPayload, 0, 16).Trim(new char[] { (char)0 });
                                CGlobals.DebugOutput("DreamScreen HD detected: " + szDeviceName);
                                newDevice = new CDeviceDreamScreen(DeviceType.DreamScreenHD, szDeviceName);
                                newDevice.ProcessPayloadData(dreamScreenPacket.bPayload);
                            }
                            else if (dreamScreenPacket.bPayload[dreamScreenPacket.bPayload.Length - 2] == (byte)DsDeviceType.DreamScreen4K)
                            {
                                szDeviceName = Encoding.UTF8.GetString(dreamScreenPacket.bPayload, 0, 16).Trim(new char[] { (char)0 });
                                CGlobals.DebugOutput("DreamScreen 4K detected: " + szDeviceName);
                                newDevice = new CDeviceDreamScreen(DeviceType.DreamScreen4K, szDeviceName);
                                newDevice.ProcessPayloadData(dreamScreenPacket.bPayload);
                            }
                            else if (dreamScreenPacket.bPayload[dreamScreenPacket.bPayload.Length - 2] == (byte)DsDeviceType.DreamScreen4KSolo)
                            {
                                szDeviceName = Encoding.UTF8.GetString(dreamScreenPacket.bPayload, 0, 16).Trim(new char[] { (char)0 });
                                CGlobals.DebugOutput("DreamScreen 4K Solo detected: " + szDeviceName);
                                newDevice = new CDeviceDreamScreen(DeviceType.DreamScreen4KSolo, szDeviceName);
                                newDevice.ProcessPayloadData(dreamScreenPacket.bPayload);
                            }
                            else if (dreamScreenPacket.bPayload[dreamScreenPacket.bPayload.Length - 2] == (byte)DsDeviceType.DreamScreenSideKick)
                            {
                                szDeviceName = Encoding.UTF8.GetString(dreamScreenPacket.bPayload, 0, 16).Trim(new char[] { (char)0 });
                                CGlobals.DebugOutput("DreamScreen Sidekick detected: " + szDeviceName + " - " + BitConverter.ToString(bytes).Replace('-', ':'));
                                newDevice = new CDeviceDreamScreen(DeviceType.DreamScreenSideKick, szDeviceName);
                                newDevice.ProcessPayloadData(dreamScreenPacket.bPayload);
                            }
                            else
                            {
                                CGlobals.DebugOutput("Unknown DreamScreen device detected: " + BitConverter.ToString(dreamScreenPacket.bPayload.ToArray()).Replace('-', ':'));
                            }
                        }
                        else
                        {
                            CGlobals.DebugOutput("INVALID CurrentState Packet reveived: " + BitConverter.ToString(bytes).Replace('-', ':'));
                        }
                        break;
                    case DsCommands.Mode:
                        CGlobals.DebugOutput("received Mode from DreamScreen device.");
                        bTriggerPacketEvent = true; 
                        break;
                    case DsCommands.Install_SKU_Setup:
                        CGlobals.DebugOutput("received Install_SKU_Setup from DreamScreen device.");
                        bTriggerPacketEvent = true;
                        break;
                    case DsCommands.Install_Flex_Setup:
                        CGlobals.DebugOutput("received Install_Flex_Setup from DreamScreen device.");
                        bTriggerPacketEvent = true;
                        break;
                    case DsCommands.Temperature:
                        CGlobals.DebugOutput("received Temperature from DreamScreen device.");
                        bTriggerPacketEvent = true;
                        break;
                    default:
                        // CGlobals.DebugOutput("Command=0x" + dreamScreenPacket.u16Command.ToString("X4") + " unknown to handle!");
                        break;
                }

                // Add/update device if set
                if (newDevice != null)
                {
                    newDevice.SetIPAddress(ip.Address);
                    newDevice.SetDeviceID();
                    newDevice.SetIsOnline(true);

                    // Ádd device to Main config
                    newDevice = CGlobals.mainConfig.Devices.Add(newDevice);
                    if (newDevice != null)
                    {
                        // Trigger new found device
                        HandleDiscoveredDevice(newDevice);
                    }
                }
                else if(bTriggerPacketEvent)
                {
                    // Other DreamScreen Packet found, trigger event 
                    if (ReceivedDSPacket != null)
                        ReceivedDSPacket(this, null, ip.Address, dreamScreenPacket);
                }
            }

            // Start listening for next Packet
            StartUdpListeningDreamScreem();
        }
        private DreamScreenPacket AnalyzeUdpPacket(byte[] receivedBytes)
        {
            DreamScreenPacket dreamScreenPacket = new DreamScreenPacket();

            if (receivedBytes.Length >= 6)
            {
                dreamScreenPacket.bStartPacket = receivedBytes[0];
                dreamScreenPacket.bPacketLength = receivedBytes[1];
                dreamScreenPacket.bGroupAddress = receivedBytes[2];
                dreamScreenPacket.bFlags = receivedBytes[3];
                dreamScreenPacket.u16Command = CGlobals.ReverseBytes( BitConverter.ToUInt16 (receivedBytes, 4) );
                int iPayloadLen = receivedBytes.Length - 5 /*Head*/ - 1 /*CRC*/; 
                if (iPayloadLen > 0)
                {
                    dreamScreenPacket.bPayload = new byte[receivedBytes.Length - 5 - 1];
                    Array.Copy(receivedBytes, 6, dreamScreenPacket.bPayload, 0, iPayloadLen);
                    // CGlobals.DebugOutput("AnalyzeUdpPacket: Payload=" + Encoding.UTF8.GetString(dreamScreenPacket.bPayload) + "\r\n");
                }

                //if (receivedBytes.Length)
                //dreamScreenPacket.bPayload = receivedBytes.CopyTo(); 
                dreamScreenPacket.bChecksum = receivedBytes[receivedBytes.Length-1];
                
                /* Debug output */
//                 CGlobals.DebugOutput("AnalyzeUdpPacket: Command=0x(" + dreamScreenPacket.u16Command.ToString("X4") + " - Bytes=" + BitConverter.ToString(receivedBytes).Replace('-', ':'));
            }
            else
            {
                CGlobals.DebugOutput("AnalyzeUdpPacket: Unknown Packet Received - Bytes=" + BitConverter.ToString(receivedBytes).Replace('-', ':'));
            }

            // return parsed packet
            return dreamScreenPacket; 
        }
        private byte[] CreateUdpPacket(DsCommands myCommand, byte[] payload, byte bGroupNumber /*0x00*/, byte bFlags /*0x11*/, bool broadcastingToGroup)
        {
            MemoryStream msPacket = new MemoryStream();
            BinaryWriter bwPacket = new BinaryWriter(msPacket);

            // Hardcoded Packet header
            bwPacket.Write((byte)   0xFC); // 252
            // length of the full packet 
            bwPacket.Write((byte)(  0x05 + (payload != null && payload.Length > 0 ? payload.Length : 0)));
            // Broadcast Group if set
            bwPacket.Write((byte) bGroupNumber); 
            if (broadcastingToGroup)
                bwPacket.Write((byte) DsFlags.Receive /*0x31*/ /*0b00100001*/); // used for Broadcast - reserved
            else
                bwPacket.Write((byte) bFlags /*0x11*/ /*0b00010001*/); // used for unicast - reserved

            // Add command to packet
            bwPacket.Write(CGlobals.ReverseBytes((ushort) myCommand));

            // Add payload to packet
            if (payload != null && payload.Length > 0)
                bwPacket.Write(payload);

            /* Handle commands */
            switch (myCommand)
            {
                case DsCommands.CurrentState:
                    CGlobals.DebugOutput("CreateUdpPacket: Create CurrentState Packet");
                    break;
                case DsCommands.PixelVideoAddressing:
                    CGlobals.DebugOutput("CreateUdpPacket: Direct Video Pixel Addressing Packet");
                    break;
                default:
                    CGlobals.DebugOutput("CreateUdpPacket: Unknown DSCOMMAND '" + myCommand.ToString() + "'"); 
                    break;
            }

            // Add Checksum to message!
            bwPacket.Write(uartCommCalculateCRC8(msPacket.ToArray()));

            CGlobals.DebugOutput("CreateUdpPacket: Packet created - Bytes=" + BitConverter.ToString(msPacket.ToArray()).Replace('-',':'));

            // return Packet as Byte Array
            return msPacket.ToArray();
        }
        // FC:05:00:10:03:01:(A3)
        private byte uartCommCalculateCRC8(byte[] data)
        {
            byte size = (byte)(data[1] + 0x01);
            byte cntr = 0x00;
            byte crc = 0x00;
            while (cntr < size)
            {
                crc = uartComm_crc8_table[(byte)(crc ^ (data[cntr])) & 0xFF];
                cntr++;
            }
            return crc;
        }
#endif
        private void StartUdpListeningAmbiBack()
        {
            udpClientAmbiBack.BeginReceive(UdpReceiverAmbiBack, udpStateAmbiBack = new object());
        }

        private void UdpReceiverAmbiBack(IAsyncResult ar)
        {
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, this.udpPortNumberAmbiBack);
            bool bProcess = true;
            byte[] bytes = udpClientAmbiBack.EndReceive(ar, ref ip);

            if (IsLocalBroadcast(ip))
            {
                CGlobals.DebugOutput("Skip own selfsend broadcasts!");
                bProcess = false;
            }

            if (bProcess)
            {
                try
                {

                    // string message = Encoding.ASCII.GetString(bytes);
                    string szResponse = Encoding.ASCII.GetString(bytes, 0, bytes.Length).Trim(new char[] { (char)0 });
                    CGlobals.DebugOutput("UdpReceiverAmbiBack: Received message from " + ip.Address.ToString() + " Data: " + szResponse);

                    // Process json
//                    szResponse = szResponse.Replace("\t", "").Replace("\n", "").Replace("\"", "");
                    JSONNode jsonResults = JSON.Parse(szResponse);
                    if (jsonResults.Count > 0)
                    {
                        // Handle found device
                        this.CheckAndAddDevice(jsonResults, false, null); 
                    }
                }
                catch (Exception ex)
                {
                    CGlobals.DebugOutput(ex);
                }
            }

            // Start listening for next Packet
            StartUdpListeningAmbiBack();
        }

        private void CheckAndAddDevice(JSONNode jsonResults, bool bSerial, string szComPort)
        {
            // Debug Output
            CGlobals.DebugOutput("CheckAndAddDevice: " + jsonResults.ToString());

            // Get Data from JSON string
            string szDeviceType = (jsonResults["DeviceType"] != null ? jsonResults["DeviceType"].Value.Trim() : null);
            string szDeviceVersion = (jsonResults["DeviceVersion"] != null ? jsonResults["DeviceVersion"].Value.Trim(): null);
            string szDeviceID = (jsonResults["DeviceID"] != null ? jsonResults["DeviceID"].Value.Trim(): null);
            string szDeviceMAC = (jsonResults["DeviceMAC"] != null ? jsonResults["DeviceMAC"].Value.Trim() : null);
            string szDeviceName = (jsonResults["DeviceName"] != null ? jsonResults["DeviceName"].Value.Trim(): null);
            string szDeviceIP = (jsonResults["DeviceIP"] != null ? jsonResults["DeviceIP"].Value.Trim(): null);
            int iDeviceType = CGlobals.FindDeviceId(szDeviceType);

            CDevice newDevice = null;
            newDevice = new CDeviceAmbiBack((DeviceType)iDeviceType, szDeviceIP);
            newDevice.SetIPAddress(IPAddress.Parse(szDeviceIP));
            newDevice.InternalDeviceID = szDeviceID;
            newDevice.InternalNetworkMAC = szDeviceMAC;
            newDevice.SetDeviceName(szDeviceName);
            newDevice.SetIsOnline(true);
            newDevice.SetIsSerial(bSerial);
            newDevice.SetComPort(szComPort);

            // Create Service ID now!
            newDevice.SetDeviceID();

            // Ádd device to Main config
            if (CGlobals.mainConfig.Devices.GetDevice(newDevice.GetDeviceID()) == null)
            {
                newDevice = CGlobals.mainConfig.Devices.Add(newDevice);
                if (newDevice != null)
                {
                    // Trigger UI Update
                    HandleDiscoveredDevice(newDevice);
                }
            }
            else
            {
                CDevice existingDevice = CGlobals.mainConfig.Devices.GetDevice(newDevice.GetDeviceID());
                // Only update some of the values!
                existingDevice.UpdateFrom(newDevice);

                // Trigger UI Update
                HandleDiscoveredDevice(existingDevice);
            }
        }

#endregion

#region Thread Controlling Functions
        public void CreateThread()
        {
            if (myThread == null)
            {
                myThread = new Thread(new ThreadStart(HandleThread));
                myThread.Priority = ThreadPriority.Lowest;

                // Set ThreadName here
                myThread.Name = "DeviceController";
            }
        }
        public bool IsThreadRunning()
        {
            if (myThread.ThreadState == System.Threading.ThreadState.Running)
                return true;
            else
                return false;

        }
        public bool DoSleepThread()
        {
            if (myThread != null)
            {
                if (myThread.ThreadState == System.Threading.ThreadState.Running ||
                        myThread.ThreadState == System.Threading.ThreadState.WaitSleepJoin)
                {
                    this.SleepThread = true;
                    myThread.Interrupt(); //  Interrupt only!;
                }

                // True means Interrupt does need to be taken care of
                return true;
            }
            else
                // False means Interrupt has to be ignored
                return false;
        }
        public bool DoAbortThread()
        {
            if (myThread != null)
            {
                if (myThread.ThreadState == System.Threading.ThreadState.Running ||
                        myThread.ThreadState == System.Threading.ThreadState.WaitSleepJoin)
                {
                    // Abort thread
                    this.SleepThread = false;
                    this.AbortThread = true;

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

        public void HandleThread()
        {
            CGlobals.DebugOutput("CDevicesController|HandleThread|Begin Thread");

            try
            {
                // Create default local endpoint
                this.localhostEntry = Dns.GetHostEntry(Dns.GetHostName());
                this.localEndPoint = new IPEndPoint(localhostEntry.AddressList[0], 0);

                // Create default UDP Socket 
                this.udpSocket = new Socket(localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
#if USE_DREAMSCREEN
                // Create new UDP Client for boradcasts
                this.udpClientDreamScreen = new UdpClientBroadcast(this.udpPortNumberDreamScreen);
                this.StartUdpListeningDreamScreem();
#endif
                this.udpClientAmbiBack = new UdpClientBroadcast(this.udpPortNumberAmbiBack);
                this.StartUdpListeningAmbiBack();
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
                CGlobals.DebugOutput("CDevicesController|HandleThread|Exception: " + ex.ToString());

                this.AbortThread = true;
            }

            while (AbortThread == false)
            {
                try
                {
                    while (true)
                    {
                        if (this.DoEnumDevices)
                        {
                            // Trigger EnumDevices
                            this.DoEnumDevices = false;
                            this.EnumDevices();
                        }

                        // Sleep a little 
                        Thread.Sleep(100); 
                    }
                }
                catch (ThreadInterruptedException)
                {
                    // Interrupted
                    CGlobals.DebugOutput("CDevicesController|HandleThread|Interrupted");

                    while (SleepThread)
                    {
                        // Use Sleep instead of SpinWait 
                        Thread.Sleep(1);
                        // Thread.SpinWait(10000000);
                    }

                    // Interrupted
                    CGlobals.DebugOutput("CDevicesController|HandleThread|Restart");
                }
                catch (ThreadAbortException)
                {
                    // Aborted
                    CGlobals.DebugOutput("CDevicesController|HandleThread|Aborted");

                    this.AbortThread = true;
                }
                catch (Exception ex)
                {
                    CGlobals.DebugOutput(ex);
                    CGlobals.DebugOutput("CDevicesController|HandleThread|Exception: " + ex.ToString());

                    this.AbortThread = true;
                }
            }

            CGlobals.DebugOutput("CDevicesController|HandleThread|Finished Thread");
        }

        public void StartThread()
        {
            try
            {
                // Reset Abort Thread
                this.AbortThread = false;

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
                    this.SleepThread = false;
                }

                else if (myThread.ThreadState == System.Threading.ThreadState.Aborted)
                {
                    // Start the Thread
                    myThread.Start();
                }
                else
                {
                    CGlobals.DebugOutput("CDevicesController|StartThread|Unexpected Thread STATE: " + myThread.ThreadState.ToString());
                }
            }
            catch (System.InvalidOperationException)
            {
                // Bla we don't care
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
                CGlobals.DebugOutput("CDevicesController|StartThread|Exception: " + ex.ToString());
            }
        }

#endregion



    }
}
