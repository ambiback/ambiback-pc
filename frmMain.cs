using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

// Custom
using System.IO;
using System.Net;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AmbiBack
{
    #region Public Enums
    // Enum for State
    public enum LVState
    {
        running = 0,
        stopped_offline = 1,
        stopped_online = 2,
        disabled = 3,
        unknown = 3,
    }
    #endregion

    public partial class frmMain : Form
    {
        // Main ScreenHelper
        public CScreenHelper screenHelperMain = null;
        public AmbiMonitor selectedAmbiScreen = null;

        #region Threading helpers
        delegate void EventDebugOutputCallback(object sender, System.EventArgs e, string szDebug);
        delegate void DeviceUpdateStatusCallback(object sender, EventArgs e, string szStatus);
        delegate void DeviceFinderDiscoveredDeviceCallback(object sender, System.EventArgs e, CDevice newDevice);
#if USE_DREAMSCREEN
        delegate void DeviceFinderReceivedDSPacketCallback(object sender, System.EventArgs e, IPAddress remoteIpAddress, DreamScreenPacket dsPacket);
#endif
        #endregion

        private CDevice currentSelectedDevice = null;

        #region API Constants 
        private const int WM_SIZE = 0x0005;
        private const int SIZE_RESTORED = 0;
        private const int SIZE_MINIMIZED = 1;
        private const int SIZE_MAXIMIZED = 2;
        private const int SIZE_MAXSHOW = 3;
        private const int SIZE_MAXHIDE = 4;
        private const int WM_ACTIVATEAPP = 0x001C;
        private const int WM_POWERBROADCAST = 0x0218;
        private const int WM_MENUCHAR = 0x0120;
        private const int WM_SYSCOMMAND = 0x0112;
        private const uint PBT_APMRESUMESUSPEND = 7;
        private const uint PBT_APMQUERYSUSPEND = 0;

        private const int SC_SIZE = 0xF000;
        private const int SC_MOVE = 0xF010;
        private const int SC_MINIMIZE = 0xF020;
        private const int SC_MAXIMIZE = 0xF030;
        private const int SC_NEXTWINDOW = 0xF040;
        private const int SC_PREVWINDOW = 0xF050;
        private const int SC_CLOSE = 0xF060;
        private const int SC_VSCROLL = 0xF070;
        private const int SC_HSCROLL = 0xF080;
        private const int SC_MOUSEMENU = 0xF090;
        private const int SC_KEYMENU = 0xF100;
        private const int SC_ARRANGE = 0xF110;
        private const int SC_RESTORE = 0xF120;
        private const int SC_TASKLIST = 0xF130;
        private const int SC_SCREENSAVE = 0xF140;
        private const int SC_HOTKEY = 0xF150;
        private const int SC_DEFAULT = 0xF160;
        private const int SC_MONITORPOWER = 0xF170;
        private const int SC_CONTEXTHELP = 0xF180;
        private const int SC_SEPARATOR = 0xF00F;
        private const int SCF_ISSECURE = 0x00000001;

        private const int MNC_CLOSE = 1;

        private IntPtr hMonitorOn;
        private IntPtr hDisplayState;
        private IntPtr hPowerSrc;
        private IntPtr hBattCapacity;
        private IntPtr hPowerScheme;
        #endregion

        // Other helpers
        private Point pNotifyIcon;

        public void RegisterForPowerNotifications(IntPtr hwnd)
        {
            hPowerSrc = CGlobals.RegisterPowerSettingNotification(hwnd, ref CGlobals.GUID_ACDC_POWER_SOURCE, CGlobals.DEVICE_NOTIFY_WINDOW_HANDLE);
            hBattCapacity = CGlobals.RegisterPowerSettingNotification(hwnd, ref CGlobals.GUID_BATTERY_PERCENTAGE_REMAINING, CGlobals.DEVICE_NOTIFY_WINDOW_HANDLE);
            hMonitorOn = CGlobals.RegisterPowerSettingNotification(hwnd, ref CGlobals.GUID_MONITOR_POWER_ON, CGlobals.DEVICE_NOTIFY_WINDOW_HANDLE);
            hDisplayState = CGlobals.RegisterPowerSettingNotification(hwnd, ref CGlobals.GUID_CONSOLE_DISPLAY_STATE, CGlobals.DEVICE_NOTIFY_WINDOW_HANDLE);
            hPowerScheme = CGlobals.RegisterPowerSettingNotification(hwnd, ref CGlobals.GUID_POWERSCHEME_PERSONALITY, CGlobals.DEVICE_NOTIFY_WINDOW_HANDLE);
        }
        private void UnregisterForPowerNotifications()
        {
            CGlobals.UnregisterPowerSettingNotification(hBattCapacity);
            CGlobals.UnregisterPowerSettingNotification(hMonitorOn);
            CGlobals.UnregisterPowerSettingNotification(hDisplayState);
            CGlobals.UnregisterPowerSettingNotification(hPowerScheme);
            CGlobals.UnregisterPowerSettingNotification(hPowerSrc);
        }

        public frmMain()
        {
            InitializeComponent();
            ApplyStyle();
            RegisterForPowerNotifications(this.Handle);
        }

        #region helper event functions
        private void DeviceUpdateStatus(object sender, EventArgs e, string szStatus)
        {
            // --- Threading stuff
            if (this.InvokeRequired && !CGlobals.appIsStopping)
            {
                DeviceUpdateStatusCallback myCallBack = new DeviceUpdateStatusCallback(DeviceUpdateStatus);
                this.Invoke(myCallBack, new object[] { sender, e, szStatus });
            }
            else
            {
                // Get Screen reference
                CDevice myScreen = (CDevice)sender;
                this.lblDebug.Text = szStatus;
            }
        }
        private void EventDebugOutput(object sender, EventArgs e, string szDebug)
        {
            // --- Threading stuff
            if (this.InvokeRequired && !CGlobals.appIsStopping)
            {
                try
                {
                    EventDebugOutputCallback myCallBack = new EventDebugOutputCallback(EventDebugOutput);
                    this.Invoke(myCallBack, new object[] { sender, e, szDebug });
                }
                catch (Exception)
                {
                    // Ignore exception on shutdown
                }
            }
            else
            {
                // Get Screen reference
                CDevice myDevice = (CDevice)sender;
                this.txtDebug.AppendText(myDevice.GetDeviceName() +  ": Debug: " + szDebug + (szDebug.EndsWith("\r\n") ? "" : "\r\n") );
                CGlobals.DebugOutput(szDebug);
            }
        }
        public void DiscoveredDevice(object sender, EventArgs e, CDevice newDevice)
        {
            // --- Threading stuff
            if (this.InvokeRequired && !CGlobals.appIsStopping)
            {
                DeviceFinderDiscoveredDeviceCallback myCallBack = new DeviceFinderDiscoveredDeviceCallback(DiscoveredDevice);
                this.Invoke(myCallBack, new object[] { sender, e, newDevice });
            }
            else
            {
                // Add / Update Device in ListView
                ListViewItem deviceListItem = this.AddDeviceToListView(newDevice);

                // Update Online Status
                this.ListviewHandleOnlineState(newDevice, deviceListItem);

                // Preselect last selected device!
                if (deviceListItem.Name == Properties.Settings.Default.device_selected)
                {
                    deviceListItem.Selected = true;
                }
            }
        }
#if USE_DREAMSCREEN
        private void ReceivedDSPacket(object sender, EventArgs e, IPAddress remoteIpAddress, DreamScreenPacket dsPacket)
        {
            // --- Threading stuff
            if (this.InvokeRequired && !CGlobals.appIsStopping)
            {
                DeviceFinderReceivedDSPacketCallback myCallBack = new DeviceFinderReceivedDSPacketCallback(ReceivedDSPacket);
                this.Invoke(myCallBack, new object[] { sender, e, remoteIpAddress, dsPacket });
            }
            else
            {
                bool bTimer = false; 
                try
                {

                    string szPayloadString = Encoding.UTF8.GetString(dsPacket.bPayload);
                    string szPayloadHex = BitConverter.ToString(dsPacket.bPayload.ToArray()).Replace('-', ':');

                    string szSenderIP = remoteIpAddress.ToString();
                    string szSenderName = szSenderIP;

                    // Try to find Device in Devices List 
                    CDeviceDreamScreen dsDevice = (CDeviceDreamScreen) CGlobals.mainConfig.Devices.GetDevice(remoteIpAddress);

                    if (dsDevice == null)
                    {
                        // Fail in this case, handle better later
                        throw (new Exception("Packet received from unknown DEVICE: " + szSenderIP));
                    }
                    else
                        szSenderName = dsDevice.GetDeviceName();

                    // Process Packet data!
                    switch ((DsCommands)dsPacket.u16Command)
                    {
                        case DsCommands.Temperature:
                            txtDebug.AppendText("Temp = '" + dsPacket.bPayload[0].ToString() + "' from Device '" + szSenderName + "' received\r\n");
                            bTimer = true; 
                            break;
                        case DsCommands.Install_SKU_Setup:
                            txtDebug.AppendText("Received Install_SKU_Setup '" + szPayloadHex + "' from Device '" + szSenderName + "' received\r\n");

                            // Set device properties
                            dsDevice.InstallationType = dsPacket.bPayload[0];

                            if (dsPacket.bPayload[0] == (byte)DsDeviceSkuSetup.Classic)
                            {

                                dsDevice.FlexSetupPort1 = (byte)DsFlexSetups.Left_Clock_bottom_left_up;
                                dsDevice.FlexSetupPort2 = (byte)DsFlexSetups.Left_Clock_bottom_left_up;
                            }
                            else if (dsPacket.bPayload[0] == (byte)DsDeviceSkuSetup.Mega)
                            {
                                dsDevice.FlexSetupPort1 = (byte)DsFlexSetups.Left_Clock_bottom_left_up;
                                dsDevice.FlexSetupPort2 = (byte)DsFlexSetups.Left_Clock_bottom_left_up;
                            }
                            else if (dsPacket.bPayload[0] == (byte)DsDeviceSkuSetup.Xtreme)
                            {
                                dsDevice.FlexSetupPort1 = (byte)DsFlexSetups.Left_Clock_bottom_left_up;
                                dsDevice.FlexSetupPort2 = (byte)DsFlexSetups.Left_Clock_bottom_left_up;
                            }
                            else /* Default = FLEX DIY */ 
                            {
                                // Send another packet out to get DIY LED Numbers
                                CGlobals.deviceController.SendDreamscreenCommand((CDeviceDreamScreen)currentSelectedDevice, DsCommands.Install_Flex_Setup, 0xFF, (byte)DsFlags.Receive, new byte[0]);
                            }
                            break;
                        case DsCommands.Install_Flex_Setup:
                            txtDebug.AppendText("Received Install_Flex_Setup '" + szPayloadHex + "' from Device'" + szSenderName + "' received\r\n");
                            if (dsPacket.bPayload.Length > 1)
                            {
                                //  13:1F:64:64:01:01:7A

                                // Set device properties
                                dsDevice.PixelVert = dsPacket.bPayload[0];
                                dsDevice.PixelHori = dsPacket.bPayload[1];
                                dsDevice.PixelTotalPort1 = dsPacket.bPayload[2];
                                dsDevice.PixelTotalPort2 = dsPacket.bPayload[3];
                                dsDevice.FlexSetupPort1 = (byte)(dsPacket.bPayload[4] <= 0x08 ? (DsFlexSetups)dsPacket.bPayload[4] : DsFlexSetups.Left_Clock_bottom_left_up /* Use default for invalid values */);
                                dsDevice.FlexSetupPort2 = (byte)(dsPacket.bPayload[5] <= 0x08 ? (DsFlexSetups)dsPacket.bPayload[5] : DsFlexSetups.Left_Clock_bottom_left_up /* Use default for invalid values */);
                                // Last Byte ???
                            }
                            break;
                        default:
                            txtDebug.AppendText("Unknown DsCommand '" + szPayloadHex + "/" + szPayloadString + "' from Device'" + szSenderName + "' received\r\n");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    CGlobals.DebugOutput(ex);
                }
                finally
                {
                    // Enable Timer!
                    if (bTimer)
                        TimerCheck.Enabled = true;
                }

            }
        }
        private void SetDeviceTabValues()
        {
            if (currentSelectedDevice != null)
            {
                if (currentSelectedDevice.GetDeviceType() == DeviceType.DreamScreen4K ||
                    currentSelectedDevice.GetDeviceType() == DeviceType.DreamScreen4KSolo ||
                    currentSelectedDevice.GetDeviceType() == DeviceType.DreamScreenHD ||
                    currentSelectedDevice.GetDeviceType() == DeviceType.DreamScreenSideKick)
                {
                    // Set DreamScreen specific parameters
                    CDeviceDreamScreen myDSDevice = (CDeviceDreamScreen)currentSelectedDevice;
                }
                else
                {
                    // TODO: Handle Other device!
                }
            }

        }
#endif

        #endregion

        #region Form Functions
        private void TimerCheck_Tick(object sender, EventArgs e)
        {
            // Disable first!
            TimerCheck.Enabled = false;

            // TODO Check AmbiBack
#if USE_DREAMSCREEN
            if (myCheckMode == CheckModes.DreamScreenMonitorDevice)
            {
                if (currentSelectedDevice != null && 
                    currentSelectedDevice.GetDeviceType() == DeviceType.DreamScreen4K ||
                    currentSelectedDevice.GetDeviceType() == DeviceType.DreamScreen4KSolo ||
                    currentSelectedDevice.GetDeviceType() == DeviceType.DreamScreenHD)
                {
#if (DEBUG)
                    // Output Debug
                    txtDebug.AppendText("Read Temp from " + currentSelectedDevice.GetDeviceName() + "(" + currentSelectedDevice.GetIPAddress().ToString() + "): ");
#endif
                    // Send UDP Packet
                    CGlobals.deviceController.SendDreamscreenCommand((CDeviceDreamScreen)currentSelectedDevice, DsCommands.Temperature, ((CDeviceDreamScreen)currentSelectedDevice).GroupNumber, (byte)DsFlags.Read, new byte[0]);
                }
            }
#endif
        }

        private void RemoveDeviceFromListView(CDevice myDevice)
        {
            foreach (ListViewItem myItem in LVDevices.Items)
            {
                if (myItem.Tag == myDevice)
                {
                    LVDevices.Items.Remove(myItem);
                    return;
                }
            }
        }

        private ListViewItem AddDeviceToListView(CDevice myDevice)
        {
            // Define DeviceID
            string szDeviceId = myDevice.GetDeviceID();

            // Used for return
            ListViewItem deviceListItem = null;
#if USE_DREAMSCREEN
            if (myDevice.GetType().ToString() == "AmbiBack.CDeviceDreamScreen")
            {
                CDeviceDreamScreen dreamscreenDevice = (CDeviceDreamScreen)myDevice;
                string szGroupId = "dreamscreen_" + dreamscreenDevice.GroupNumber.ToString();
                if (LVDevices.Groups[szGroupId] == null)
                {
                    ListViewGroup newGroup = new ListViewGroup();
                    newGroup.Name = szGroupId;
                    newGroup.Tag = dreamscreenDevice.GroupNumber;
                    newGroup.Header = L.F("ds_group_header", dreamscreenDevice.GroupNumber, dreamscreenDevice.GroupName);

                    LVDevices.Groups.Add(newGroup);
                }

                if (LVDevices.Items.IndexOfKey(szDeviceId) >= 0)
                {
                    // Item Already exist, update
                    deviceListItem = LVDevices.Items[szDeviceId];

                    deviceListItem.Tag = dreamscreenDevice;
                    deviceListItem.ImageKey = CGlobals.GetImageKey(dreamscreenDevice.GetDeviceType());
                    deviceListItem.Group = LVDevices.Groups[szGroupId];

                    deviceListItem.Text = CGlobals.DEVICETYPESDISPLAYNAME[(int)dreamscreenDevice.GetDeviceType()];
                    deviceListItem.SubItems[1].Text = dreamscreenDevice.GetDeviceName();
                    deviceListItem.SubItems[2].Text = dreamscreenDevice.GetIPAddress().ToString();
                    deviceListItem.SubItems[3].Text = L.T("st_online");
                }
                else
                {
                    // Item doesn exist, create 
                    deviceListItem = new ListViewItem();

                    deviceListItem.Name = szDeviceId;
                    deviceListItem.Tag = dreamscreenDevice;
                    deviceListItem.ImageKey = CGlobals.GetImageKey(dreamscreenDevice.GetDeviceType());
                    deviceListItem.Group = LVDevices.Groups[szGroupId];

                    deviceListItem.Text = CGlobals.DEVICETYPESDISPLAYNAME[(int)dreamscreenDevice.GetDeviceType()];
                    deviceListItem.SubItems.Add(dreamscreenDevice.GetDeviceName());
                    deviceListItem.SubItems.Add(dreamscreenDevice.GetIPAddress().ToString());
                    deviceListItem.SubItems.Add(L.T("st_offline"));

                    LVDevices.Items.Add(deviceListItem);
                }
            } else if (myDevice.GetType().ToString() == "AmbiBack.CDeviceAmbiBack") {
#else
            if (myDevice.GetType().ToString() == "AmbiBack.CDeviceAmbiBack")
            {
#endif
                CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)myDevice;

                if (LVDevices.Items.IndexOfKey(szDeviceId) >= 0)
                {
                    // Item Already exist, update
                    deviceListItem = LVDevices.Items[szDeviceId];

                    deviceListItem.Tag = nodeMcuManagerDevice;
                    deviceListItem.ImageKey = CGlobals.GetImageKey(nodeMcuManagerDevice.GetDeviceType());
                    // deviceListItem.Group = LVDevices.Groups[szGroupId];

                    deviceListItem.Text = CGlobals.DEVICETYPESDISPLAYNAME[(int)nodeMcuManagerDevice.GetDeviceType()];
                    deviceListItem.SubItems[1].Text = nodeMcuManagerDevice.GetDeviceName();
                    deviceListItem.SubItems[2].Text = nodeMcuManagerDevice.GetIPAddress().ToString();
                }
                else
                {
                    // Item doesn exist, create 
                    deviceListItem = new ListViewItem();

                    deviceListItem.Name = szDeviceId;
                    deviceListItem.Tag = nodeMcuManagerDevice;
                    deviceListItem.ImageKey = CGlobals.GetImageKey(nodeMcuManagerDevice.GetDeviceType());
                    //                    deviceListItem.Group = LVDevices.Groups[szGroupId];

                    deviceListItem.Text = CGlobals.DEVICETYPESDISPLAYNAME[(int)nodeMcuManagerDevice.GetDeviceType()];
                    deviceListItem.SubItems.Add(nodeMcuManagerDevice.GetDeviceName());
                    deviceListItem.SubItems.Add(nodeMcuManagerDevice.GetIPAddress().ToString());
                    deviceListItem.SubItems.Add(L.T("st_offline"));


                    LVDevices.Items.Add(deviceListItem);
                }
            }
            else if (myDevice.GetType().ToString() == "AmbiBack.CDevicePhilipsHueLight")
            {
                CDevicePhilipsHueLight philipsHueLightDevice = (CDevicePhilipsHueLight)myDevice;

                if (LVDevices.Items.IndexOfKey(szDeviceId) >= 0)
                {
                    // Item Already exist, update
                    deviceListItem = LVDevices.Items[szDeviceId];

                    deviceListItem.Tag = philipsHueLightDevice;
                    deviceListItem.ImageKey = CGlobals.GetImageKey(philipsHueLightDevice.GetDeviceType());
                    // deviceListItem.Group = LVDevices.Groups[szGroupId];

                    deviceListItem.Text = CGlobals.DEVICETYPESDISPLAYNAME[(int)philipsHueLightDevice.GetDeviceType()];
                    deviceListItem.SubItems[1].Text = philipsHueLightDevice.GetDeviceName();
                    deviceListItem.SubItems[2].Text = philipsHueLightDevice.HueUniqueID;
                }
                else
                {
                    // Item doesn exist, create 
                    deviceListItem = new ListViewItem();

                    deviceListItem.Name = szDeviceId;
                    deviceListItem.Tag = philipsHueLightDevice;
                    deviceListItem.ImageKey = CGlobals.GetImageKey(philipsHueLightDevice.GetDeviceType());
                    //                    deviceListItem.Group = LVDevices.Groups[szGroupId];

                    deviceListItem.Text = CGlobals.DEVICETYPESDISPLAYNAME[(int)philipsHueLightDevice.GetDeviceType()];
                    deviceListItem.SubItems.Add(philipsHueLightDevice.GetDeviceName());
                    deviceListItem.SubItems.Add(philipsHueLightDevice.HueUniqueID);
                    deviceListItem.SubItems.Add(L.T("st_offline"));

                    LVDevices.Items.Add(deviceListItem);
                }
            }
            else if (myDevice is CDeviceHueEntertainment hueArea)
            {
                string szDetails = L.F("hue_area_channels", hueArea.ChannelCount);
                if (LVDevices.Items.IndexOfKey(szDeviceId) >= 0)
                {
                    deviceListItem = LVDevices.Items[szDeviceId];
                    deviceListItem.Tag = hueArea;
                    deviceListItem.ImageKey = CGlobals.GetImageKey(hueArea.GetDeviceType());
                    deviceListItem.Text = CGlobals.DEVICETYPESDISPLAYNAME[(int)hueArea.GetDeviceType()];
                    deviceListItem.SubItems[1].Text = hueArea.GetDeviceName();
                    deviceListItem.SubItems[2].Text = szDetails;
                }
                else
                {
                    deviceListItem = new ListViewItem();
                    deviceListItem.Name = szDeviceId;
                    deviceListItem.Tag = hueArea;
                    deviceListItem.ImageKey = CGlobals.GetImageKey(hueArea.GetDeviceType());
                    deviceListItem.Text = CGlobals.DEVICETYPESDISPLAYNAME[(int)hueArea.GetDeviceType()];
                    deviceListItem.SubItems.Add(hueArea.GetDeviceName());
                    deviceListItem.SubItems.Add(szDetails);
                    deviceListItem.SubItems.Add(L.T("st_offline"));
                    LVDevices.Items.Add(deviceListItem);
                }
            }

            this.ListviewHandleOnlineState(myDevice, deviceListItem);

            // Return listitem
            return deviceListItem;
        }
        private void ListviewHandleOnlineState(CDevice myDevice, ListViewItem deviceListItem)
        {
            if (myDevice.IsSerial())
            {
                deviceListItem.SubItems[3].Text = L.F("st_usb", myDevice.szSerialPort);
                deviceListItem.SubItems[3].Tag = Theme.Accent;
            }
            else if (myDevice.IsOnline())
            {
                deviceListItem.SubItems[3].Text = L.T("st_online");
                deviceListItem.SubItems[3].Tag = Theme.Success;
            }
            else
            {
                deviceListItem.SubItems[3].Text = L.T("st_offline");
                deviceListItem.SubItems[3].Tag = Theme.ControlBorder;
            }
            UpdateDeviceSummary();

        }
        private void InitFormControls()
        {
            // Disable Options by default
            // Enable controls
            LVDevices.Enabled = false;
            tcMain.Enabled = false;

            cbConnectionType.Items.Clear();
            cbConnectionType.Items.Add(CGlobals.CONNECTIONTYPES[(byte)ConnectionTypes.TCP]);
            cbConnectionType.Items.Add(CGlobals.CONNECTIONTYPES[(byte)ConnectionTypes.UDP]);
            cbConnectionType.Items.Add(CGlobals.CONNECTIONTYPES[(byte)ConnectionTypes.USB]);

            // Preinit Devices List
            foreach (CDevice myDevice in CGlobals.mainConfig.Devices.GetDeviceList())
            {
                this.AddDeviceToListView(myDevice);
            }

            #region create screenhelpers
            int iScreenNum = 0;
            // Create main screenhelper
            screenHelperMain = new CScreenHelper(0, 0);

            // Create Main AmbiMonitor Object
            AmbiMonitor mainAmbiScreen = new AmbiMonitor(0, 0);
            mainAmbiScreen.Name = "AmbiScreenScreen" + iScreenNum.ToString();
            mainAmbiScreen.screenHelper = screenHelperMain;
            // Set position and size            
            mainAmbiScreen.Location = new System.Drawing.Point(0, 0);
            mainAmbiScreen.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            mainAmbiScreen.Width = panScreens.Width;
            mainAmbiScreen.SetTitel(L.F("mon_title", 1));

            // Bind events
            mainAmbiScreen.Click += new System.EventHandler(this.AmbiScreen_Click);

            // Add to Monitors Panel
            panScreens.Controls.Add(mainAmbiScreen);
            iScreenNum++;

            // Process additional Screens if available
            for (int iNum = 1; iNum < screenHelperMain.GetOutputCount(); iNum++)
            {
                try
                {
                    // Create anoter AmbiScreen Obj
                    AmbiMonitor newAmbiScreen = new AmbiMonitor(0, iNum);
                    newAmbiScreen.Name = "AmbiScreenScreen" + iScreenNum.ToString();

                    // Set position and size            
                    newAmbiScreen.Location = new System.Drawing.Point(0, (mainAmbiScreen.Height * iScreenNum));
                    newAmbiScreen.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
                    newAmbiScreen.Width = panScreens.Width;
                    newAmbiScreen.SetTitel(L.F("mon_title", iNum + 1));

                    // Bind events
                    newAmbiScreen.Click += new System.EventHandler(this.AmbiScreen_Click);

                    // Add to Monitors Panel
                    panScreens.Controls.Add(newAmbiScreen);
                    iScreenNum++;
                }
                catch (Exception ex)
                {
                    CGlobals.DebugOutput("Failed to create AmbiScreen for " + iNum.ToString() + ": " + ex.ToString());
                }
            }

            if (screenHelperMain.GetAdapterCount() > 1) {
                CGlobals.DebugOutput("TODO ADD MULTIPLE GFX CARDS NOW");
            }

            // Process additional Screens if available
            for (int iScreen = 1; iScreen < screenHelperMain.GetAdapterCount(); iScreen++)
            {
                // Create main screenhelper for this Card
                CScreenHelper screenHelperCardMain = new CScreenHelper(iScreen, 0);

                if (screenHelperCardMain.GetOutputCount() > 0) {

                    // Create Main AmbiMonitor Object
                    AmbiMonitor cardMainAmbiScreen = new AmbiMonitor(iScreen, 0);
                    cardMainAmbiScreen.Name = "AmbiScreenScreen" + iScreenNum.ToString();
                    cardMainAmbiScreen.screenHelper = screenHelperCardMain;
                    // Set position and size            
                    cardMainAmbiScreen.Location = new System.Drawing.Point(0, (mainAmbiScreen.Height * iScreenNum));
                    cardMainAmbiScreen.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
                    cardMainAmbiScreen.Width = panScreens.Width;
                    cardMainAmbiScreen.SetTitel(L.F("mon_title_gpu", iScreenNum + 1, iScreen + 1));

                    // Bind events
                    cardMainAmbiScreen.Click += new System.EventHandler(this.AmbiScreen_Click);

                    // Add to Monitors Panel
                    panScreens.Controls.Add(cardMainAmbiScreen);
                    iScreenNum++;

                    if (screenHelperCardMain.GetOutputCount() > 0)
                    {
                        for (int iNum = 1; iNum < screenHelperCardMain.GetOutputCount(); iNum++)
                        {
                            try
                            {
                                // Create anoter AmbiScreen Obj
                                AmbiMonitor newAmbiScreen = new AmbiMonitor(iScreen, iNum);
                                newAmbiScreen.Name = "AmbiScreenScreen" + iScreenNum.ToString();

                                // Set position and size            
                                newAmbiScreen.Location = new System.Drawing.Point(0, (mainAmbiScreen.Height * iScreenNum));
                                newAmbiScreen.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
                                newAmbiScreen.Width = panScreens.Width;
                                newAmbiScreen.SetTitel(L.F("mon_title_gpu", iScreenNum + 1, iScreen + 1));

                                // Bind events
                                newAmbiScreen.Click += new System.EventHandler(this.AmbiScreen_Click);

                                // Add to Monitors Panel
                                panScreens.Controls.Add(newAmbiScreen);
                                iScreenNum++;
                            }
                            catch (Exception ex)
                            {
                                CGlobals.DebugOutput("Failed to create AmbiScreen for " + iNum.ToString() + ": " + ex.ToString());
                            }
                        }
                    }
                }

            }
            #endregion

            // Assign devices to AmbiScreens
            StringCollection szAmbiScreens = Properties.Settings.Default.szAmbiScreens;
            if (szAmbiScreens != null)
            {
                // Process all AmbiScreens
                foreach (Control myControl in panScreens.Controls)
                {
                    AmbiMonitor myAmbiScreen = (AmbiMonitor)myControl;
                    if (szAmbiScreens.Count > myAmbiScreen.ScreenNumber && szAmbiScreens[myAmbiScreen.ScreenNumber] != null)
                    {
                        string[] szSettings = szAmbiScreens[myAmbiScreen.ScreenNumber].Split('\n');
                        foreach (string szSettingLine in szSettings)
                        {
                            string[] szConfigSetting = szSettingLine.Split('=');
                            if (szConfigSetting.Length > 1)
                            {
                                switch (szConfigSetting[0])
                                {
                                    case "Device":
                                        break;
                                    case "ScreenNumber":
                                        break;
                                    case "MonitorEnabled":
                                        myAmbiScreen.MonitorEnabled = bool.Parse(szConfigSetting[1]);
                                        break;
                                    case "AmbiBackRunning":
                                        myAmbiScreen.AmbiBackRunning = bool.Parse(szConfigSetting[1]);
                                        break;
                                    case "selectedDevice":
                                        // Find device by ID
                                        foreach (CDevice myDevice in CGlobals.mainConfig.Devices.GetDeviceList())
                                        {
                                            if (myDevice.GetDeviceID() == szConfigSetting[1])
                                            {
                                                myAmbiScreen.SetSelectedDevice(myDevice);
                                            }
                                        }
                                        break;
                                    case "Device.Linked":
                                        // Find device by ID
                                        foreach (CDevice myDevice in CGlobals.mainConfig.Devices.GetDeviceList())
                                        {
                                            if (myDevice.GetDeviceID() == szConfigSetting[1])
                                            {
                                                myAmbiScreen.AddDevice(myDevice);
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            // Make sure Ballon can be displayed
            nficon.Tag = false;

            // Enum Comports manuall first!
            timerComPort_Tick(null, null);

            // Start Comport detection Timer
// DISABLED FOR DEBUG
//            timerComPort.Enabled = true;

            LayoutMonitors();
            UpdateDeviceSummary();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            // Set global reference
            CGlobals.mainForm = this;

            // Form defaults
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            // Bind device controller events to local form for now...
            CGlobals.deviceController.DiscoveredDevice += new DeviceFinderDiscoveredDevice(this.DiscoveredDevice);
#if USE_DREAMSCREEN
            CGlobals.deviceController.ReceivedDSPacket += new DeviceFinderReceivedDSPacket(this.ReceivedDSPacket);
#endif

            // Startup checks
            CGlobals.SetLoadWindowsStartup();

            // Init Form Stuff
            this.InitFormControls();

            Application.DoEvents();

            // Trigger Initial device enum
            CGlobals.deviceController.DoEnumDevices = true;

            // Preselect the Screens Panel
            panScreens.Select();

            // App Start is done
            CGlobals.appIsStarting = false;
        }

        /// <summary>
        /// Override windows message loop handling.
        /// </summary>
        /// <param name="m">The Windows <see cref="T:System.Windows.Forms.Message"/> to process.</param>
        protected override void WndProc(ref Message m)
        {
            long wparam = m.WParam.ToInt64();

            switch (m.Msg)
            {
                case WM_SYSCOMMAND:
                    long lparam = m.LParam.ToInt64();
                    if (wparam == SC_KEYMENU)
                        return;

                    CGlobals.DebugOutput("frmmain:WndProc:WM_SYSCOMMAND:" + wparam.ToString("X4"));
                    // Remove last byte from, has other meaning
                    wparam &= 0xFFF0;

                    if (wparam == SC_MONITORPOWER)// || wparam == SC_SCREENSAVE)
                    {
                        if (lparam == -1)
                        {
                            CGlobals.monitorPowerStatus = true;
                            CGlobals.DebugOutput("frmmain:WndProc:WM_SYSCOMMAND:SC_MONITORPOWER:MonitorState:ON");
                        }
                        else
                        {
                            CGlobals.monitorPowerStatus = false;
                            CGlobals.DebugOutput("frmmain:WndProc:WM_SYSCOMMAND:SC_MONITORPOWER:MonitorState:OFF - " + lparam.ToString("X4"));
                        }
                    }
                    break;
                case WM_POWERBROADCAST:
                    CGlobals.DebugOutput("frmmain:WndProc:WM_POWERBROADCAST:" + wparam.ToString("X4"));
                    if (wparam == CGlobals.PBT_POWERSETTINGCHANGE)
                    {
                        // Extract data from message
                        CGlobals.POWERBROADCAST_SETTING ps = (CGlobals.POWERBROADCAST_SETTING)Marshal.PtrToStructure(m.LParam, typeof(CGlobals.POWERBROADCAST_SETTING));
                        // IntPtr pData = (IntPtr)(m.LParam.ToInt32() + Marshal.SizeOf(ps));
                        CGlobals.DebugOutput("frmmain:WndProc:WM_POWERBROADCAST:PBT_POWERSETTINGCHANGE:" + ps.PowerSetting.ToString("X"));
                        if (ps.PowerSetting == CGlobals.GUID_MONITOR_POWER_ON)
                        {
                            CGlobals.DebugOutput("frmmain:WndProc:WM_POWERBROADCAST:PBT_POWERSETTINGCHANGE:GUID_MONITOR_POWER_ON");
                        }
                        else if (ps.PowerSetting == CGlobals.GUID_CONSOLE_DISPLAY_STATE)
                        {
                            // Finally the correct Power Setting GUID!
                            CGlobals.monitorPowerStatus = (ps.Data == 0) ? false : true;
                            CGlobals.DebugOutput("frmmain:WndProc:WM_POWERBROADCAST:PBT_POWERSETTINGCHANGE:GUID_CONSOLE_DISPLAY_STATE:" + CGlobals.monitorPowerStatus.ToString() + "(" + ps.Data.ToString() + ")");
                        }
                    }
                    break;
            }

            base.WndProc(ref m);
        }

        private void AmbiScreen_Click(object sender, EventArgs e)
        {
            if (sender.GetType().ToString() == "AmbiBack.AmbiMonitor")
            {
                if (selectedAmbiScreen != null)
                {
                    selectedAmbiScreen.SetSelected(false);
                }

                // Set selected AmbiScreen Object
                selectedAmbiScreen = (AmbiMonitor)sender;
                selectedAmbiScreen.SetSelected(true);
                selectedAmbiScreen.SetControlsBySelectedDevice();

                // Check Devices for selection first
                foreach (ListViewItem lvItem in LVDevices.Items)
                {
                    if (lvItem.Tag != null)
                    {
                        if (selectedAmbiScreen.GetDevices().Contains(lvItem.Tag))
                        {
                            lvItem.Checked = true;
                        }
                        else
                        {
                            lvItem.Checked = false;
                        }
                    }
                }

                // Preselect last selected device!
                if (selectedAmbiScreen.GetSelectedDevice() != null)
                {
                    foreach (ListViewItem deviceListItem in LVDevices.Items)
                    {
                        if (deviceListItem.Tag != null)
                        {
                            CDevice selectedDevice = (CDevice)deviceListItem.Tag;
                            if (selectedDevice == selectedAmbiScreen.GetSelectedDevice())
                            {
                                deviceListItem.Selected = true;
                                break;
                            }
                        }
                    }
                }

                // Enable controls
                LVDevices.Enabled = true;
                tcMain.Enabled = true;

                // Update GFX on all AmbiMonitor instances
                foreach (Control myControl in panScreens.Controls)
                {
                    AmbiMonitor myAmbiScreen = (AmbiMonitor)myControl;
                    myAmbiScreen.Refresh();
                }
            }

        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Skip code above in this case
            if (e.CloseReason.ToString().Equals("ApplicationExitCall"))
                return;

            if (true /*CSettings.nHideOnWindowClose == 1*/)
            {
                if (e.CloseReason.ToString().Equals("WindowsShutDown") == false)
                {
                    //debug                    MessageBox.Show(e.CloseReason.ToString());

                    this.Hide();
                    e.Cancel = true;
                    return;
                } else {
                    nficon.Visible = false;

                }
            }

            // Reach this position means byebye
            this.exitToolStripMenuItem_Click(sender, e);
        }
        private void frmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            UnregisterForPowerNotifications();
        }

        private void frmMain_Shown(object sender, EventArgs e)
        {
            // Unload splashform
            if (CGlobals.frmLoading != null)
                CGlobals.frmLoading.Close();

            if (CGlobals.IsFirstTime)
            {
                // Handle First time Wizard stuff
            }

            // Preselect Main Monitor
            this.AmbiScreen_Click((object)panScreens.Controls[0], null);

            // Autorun AmbiScreens
            int iNeededHeight = 100;
            foreach (Control myControl in panScreens.Controls)
            {
                AmbiMonitor myAmbiScreen = (AmbiMonitor)myControl;
                if (myAmbiScreen.AmbiBackRunning)
                {
                    myAmbiScreen.btnStart_Click(sender, e);
                }

                // Update GFX on AmbiMonitor
                myAmbiScreen.Update();

                iNeededHeight += myAmbiScreen.Height;
            }

            // Autoresize
            if (Screen.FromControl(this).Bounds.Height > iNeededHeight)
            {
                this.Height = iNeededHeight;
            }
        }

        private void saveAmbibackSettings()
        {
            StringCollection szAmbiScreens = new StringCollection();
            foreach (Control myControl in panScreens.Controls)
            {
                AmbiMonitor myAmbiScreen = (AmbiMonitor)myControl;

                // Create Config Settings for AmbiScreen
                string szSettings = "";
                szSettings += "Device=" + myAmbiScreen.Device.ToString() + "\n";
                szSettings += "ScreenNumber=" + myAmbiScreen.ScreenNumber.ToString() + "\n";
                szSettings += "MonitorEnabled=" + myAmbiScreen.MonitorEnabled.ToString() + "\n";
                szSettings += "AmbiBackRunning=" + myAmbiScreen.AmbiBackRunning.ToString() + "\n";
                if (myAmbiScreen.GetSelectedDevice() != null)
                    szSettings += "selectedDevice=" + myAmbiScreen.GetSelectedDevice().GetDeviceID() + "\n";
                foreach (CDevice myDevice in myAmbiScreen.GetDevices())
                {
                    szSettings += "Device.Linked=" + myDevice.GetDeviceID() + "\n";
                }

                // Save Settings for AmbiScreen
                szAmbiScreens.Add(szSettings);
            }

            Properties.Settings.Default.szAmbiScreens = szAmbiScreens;
            Properties.Settings.Default.Save();

            // Trigger Save Configuration here!
            CGlobals.mainConfig.SaveConfiguration();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            CGlobals.appIsStopping = true; 

            CGlobals.deviceController.DoAbortThread();

            // Store AmbiBack Screen Settings!
            saveAmbibackSettings();

            // Stop Screenhelper Threads
            foreach (Control myControl in panScreens.Controls)
            {
                AmbiMonitor myAmbiScreen = (AmbiMonitor)myControl;
                myAmbiScreen.screenHelper.AbortThread();
            }

            // Stop Device Threads!
            foreach (CDevice myDevice in CGlobals.mainConfig.Devices.GetDeviceList())
            {
                if (myDevice != null)
                    myDevice.AbortThread();
            }

            // And now byebye
            Application.Exit();

            // wtf we still exit? kill kill kill :)
            Process myProcess = Process.GetCurrentProcess();
            myProcess.Kill();
        }

        private void btnEnumDevices_Click(object sender, EventArgs e)
        {
            // Init device enum
            CGlobals.deviceController.DoEnumDevices = true;

            // Enum Comports manual!
            timerComPort_Tick(null, null);

            // Force reading serial device informations
            CGlobals.deviceController.ReadInfoFromComPorts();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                // Show AddHost Dialog
                Form myfrm = new frmAddDevice();
                ((frmAddDevice)myfrm).m_ParentForm = this;
                myfrm.ShowDialog(this);

                //Reset all the buttons and menus
                // SetMenuAndToolbarEntries();
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (LVDevices.SelectedItems.Count > 0 && LVDevices.SelectedItems[0].Tag != null)
            {
                // Get new Selected Device
                CDevice selectedDevice = (CDevice)LVDevices.SelectedItems[0].Tag;

                // Deleting cannot be undone, so ask first
                if (CGlobals.MyMessageBox(this, L.F("msg_delete_title", selectedDevice.GetDeviceName()), L.T("msg_delete_text"),
                    false, "", MessageBoxIcon.Warning, MoreModes.None, "", "", MessageBoxButtons.YesNo) != DialogResult.OK)
                    return;

                // Unset old selected device, this will also avoid that Trackbars send data to controller
                this.currentSelectedDevice = null;

                // Clear binding

                // Process all AmbiScreens
                foreach (Control myControl in panScreens.Controls)
                {
                    AmbiMonitor myAmbiScreen = (AmbiMonitor)myControl;
                    if (myAmbiScreen.GetDevices().Contains(selectedDevice) && myAmbiScreen.AmbiBackRunning)
                    {
                        // Stop Thread
                        selectedDevice.AbortThread();
                        break;
                    }
                }

                // Remove Config obj
                CGlobals.mainConfig.Devices.DeleteEntry(selectedDevice);

                // Remove From LitsView
                LVDevices.Items.Remove(LVDevices.SelectedItems[0]);
                UpdateDeviceSummary();
            }
        }

        private void btnCleanup_Click(object sender, EventArgs e)
        {
            DialogResult myResult = DialogResult.OK;
            if (Properties.Settings.Default.nWarnDelete == 1)
            {
                myResult = CGlobals.MyMessageBox(this, L.T("msg_cleanup_title"),
                    L.T("msg_cleanup_text"),
                    true, "nWarnDelete", MessageBoxIcon.Question, MoreModes.None, "", "", MessageBoxButtons.YesNo);
            }

            if (myResult == DialogResult.OK)
            {
                string szDeleted = "";
                // Check for offline devices and delete them
                ArrayList devicesList = CGlobals.mainConfig.Devices.GetDeviceList();
                for (int iIndex = devicesList.Count - 1; iIndex >= 0; iIndex--)
                {
                    try
                    {
                        // check each device if online
                        CDevice myDevice = (CDevice)devicesList[iIndex];
                        if (!myDevice.CheckIsOnline())
                        {
                            szDeleted += "• " + myDevice.GetDeviceName() + " (" + myDevice.GetIPAddress().ToString() + ")\r\n";
                            this.RemoveDeviceFromListView(myDevice);
                            CGlobals.mainConfig.Devices.DeleteEntry(myDevice);
                        }
                    }
                    catch (Exception ex)
                    {
                        CGlobals.DebugOutput(ex);
                    }
                }

                // Show control message
                CGlobals.MyMessageBox(this, L.T("msg_cleanup_done_title"),
                    szDeleted.Length > 0 ? L.T("msg_cleanup_done") + "\r\n" + szDeleted : L.T("msg_cleanup_none"),
                    false, "", MessageBoxIcon.Information, MessageBoxButtons.OK);
                UpdateDeviceSummary();
            }
        }

        private void tbRed_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                // Get self references
                TrackBar myTrackBar = (TrackBar)sender;

                // Set to running Device!
                if (currentSelectedDevice != null)
                {
                    if (currentSelectedDevice.GetType() == typeof(CDeviceAmbiBack))
                    {
                        CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)currentSelectedDevice;
                        nodeMcuManagerDevice.ColorRedPercent = (byte)myTrackBar.Value;
                        nodeMcuManagerDevice.SendColorRedPercent();
                    }
                    else if (currentSelectedDevice.GetDeviceType() == DeviceType.PhilipsHueColorLight)
                    {
                        CDevicePhilipsHueLight philipsHueDevice = (CDevicePhilipsHueLight)currentSelectedDevice;
                        philipsHueDevice.ColorRedPercent = (byte)myTrackBar.Value;
                    }
                    else if (currentSelectedDevice is CDeviceHueEntertainment hueArea)
                    {
                        hueArea.ColorRedPercent = (byte)myTrackBar.Value;
                    }

                    // Set Tooltip Text
                    string szTooltip = myTrackBar.Value.ToString();
                    TTHelper.SetToolTip(myTrackBar, szTooltip);
                }
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
        }

        private void tbGreen_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                // Get self references
                TrackBar myTrackBar = (TrackBar)sender;

                // Set to running Device!
                if (currentSelectedDevice != null)
                {
                    if (currentSelectedDevice.GetType() == typeof(CDeviceAmbiBack))
                    {
                        CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)currentSelectedDevice;
                        nodeMcuManagerDevice.ColorGreenPercent = (byte)myTrackBar.Value;
                        nodeMcuManagerDevice.SendColorGreenPercent();
                    }
                    else if (currentSelectedDevice.GetDeviceType() == DeviceType.PhilipsHueColorLight)
                    {
                        CDevicePhilipsHueLight philipsHueDevice = (CDevicePhilipsHueLight)currentSelectedDevice;
                        philipsHueDevice.ColorGreenPercent = (byte)myTrackBar.Value;
                    }
                    else if (currentSelectedDevice is CDeviceHueEntertainment hueArea)
                    {
                        hueArea.ColorGreenPercent = (byte)myTrackBar.Value;
                    }
                }

                // Set Tooltip Text
                string szTooltip = myTrackBar.Value.ToString();
                TTHelper.SetToolTip(myTrackBar, szTooltip);
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
        }

        private void tbBlue_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                // Get self references
                TrackBar myTrackBar = (TrackBar)sender;

                // Set to running Device!
                if (currentSelectedDevice != null)
                {
                    if (currentSelectedDevice.GetType() == typeof(CDeviceAmbiBack))
                    {
                        CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)currentSelectedDevice;
                        nodeMcuManagerDevice.ColorBluePercent = (byte)myTrackBar.Value;
                        nodeMcuManagerDevice.SendColorBluePercent();
                    }
                    else if (currentSelectedDevice.GetDeviceType() == DeviceType.PhilipsHueColorLight)
                    {
                        CDevicePhilipsHueLight philipsHueDevice = (CDevicePhilipsHueLight)currentSelectedDevice;
                        philipsHueDevice.ColorGreenPercent = (byte)myTrackBar.Value;
                    }
                    else if (currentSelectedDevice is CDeviceHueEntertainment hueArea)
                    {
                        hueArea.ColorBluePercent = (byte)myTrackBar.Value;
                    }
                }

                // Set Tooltip Text
                string szTooltip = myTrackBar.Value.ToString();
                TTHelper.SetToolTip(myTrackBar, szTooltip);
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
        }

        private void tbMaxBrightness_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                // Get self references
                TrackBar myTrackBar = (TrackBar)sender;

                // Set to running Device!
                if (currentSelectedDevice != null)
                {
                    if (currentSelectedDevice.GetType() == typeof(CDeviceAmbiBack))
                    {
                        CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)currentSelectedDevice;
                        nodeMcuManagerDevice.MaxBrightnessPercent = (byte)myTrackBar.Value;
                        nodeMcuManagerDevice.SendMaxBrightness();
                    }
                    else if (currentSelectedDevice.GetDeviceType() == DeviceType.PhilipsHueColorLight)
                    {
                        CDevicePhilipsHueLight philipsHueDevice = (CDevicePhilipsHueLight)currentSelectedDevice;
                        philipsHueDevice.MaxBrightnessPercent = (byte)myTrackBar.Value;
                    }
                    else if (currentSelectedDevice is CDeviceHueEntertainment hueArea)
                    {
                        hueArea.MaxBrightnessPercent = (byte)myTrackBar.Value;
                    }
                }

                // Set Tooltip Text
                string szTooltip = myTrackBar.Value.ToString();
                TTHelper.SetToolTip(myTrackBar, szTooltip);
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
        }

        private void tbMinimumLuminosity_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                // Get self references
                TrackBar myTrackBar = (TrackBar)sender;

                // Set to running Device!
                if (currentSelectedDevice != null)
                {
                    if (currentSelectedDevice.GetType() == typeof(CDeviceAmbiBack))
                    {
                        CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)currentSelectedDevice;
                        nodeMcuManagerDevice.MinimumLuminosityPercent = (byte)myTrackBar.Value;
                        nodeMcuManagerDevice.SendMinimumLuminosity();
                    }
                    else if (currentSelectedDevice.GetDeviceType() == DeviceType.PhilipsHueColorLight)
                    {
                        CDevicePhilipsHueLight philipsHueDevice = (CDevicePhilipsHueLight)currentSelectedDevice;
                        philipsHueDevice.MinimumLuminosityPercent = (byte)myTrackBar.Value;
                    }
                    else if (currentSelectedDevice is CDeviceHueEntertainment hueArea)
                    {
                        hueArea.MinimumLuminosityPercent = (byte)myTrackBar.Value;
                    }
                }

                // Set Tooltip Text
                string szTooltip = myTrackBar.Value.ToString();
                TTHelper.SetToolTip(myTrackBar, szTooltip);
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
        }

        private void tbSmoothing_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                // Get self references
                TrackBar myTrackBar = (TrackBar)sender;

                // Set to running Device!
                if (currentSelectedDevice != null)
                {
                    if (currentSelectedDevice.GetType() == typeof(CDeviceAmbiBack))
                    {
                        CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)currentSelectedDevice;
                        nodeMcuManagerDevice.FrameDelay = myTrackBar.Value;
                        nodeMcuManagerDevice.SendFrameDelay();
                    }
                    else if (currentSelectedDevice.GetDeviceType() == DeviceType.PhilipsHueColorLight)
                    {
                        CDevicePhilipsHueLight philipsHueDevice = (CDevicePhilipsHueLight)currentSelectedDevice;
                        philipsHueDevice.FrameDelay = (byte)myTrackBar.Value;
                        philipsHueDevice.SendFrameDelay();
                    }
                }

                // Set Tooltip Text
                string szTooltip = myTrackBar.Value.ToString();
                TTHelper.SetToolTip(myTrackBar, szTooltip);
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
        }

        private void tbValueGain_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                // Get self references
                TrackBar myTrackBar = (TrackBar)sender;

                // Set to running Device!
                if (currentSelectedDevice != null)
                {
                    if (currentSelectedDevice.GetType() == typeof(CDeviceAmbiBack))
                    {
                        CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)currentSelectedDevice;
                        nodeMcuManagerDevice.ValueGainPercent = myTrackBar.Value;
                        nodeMcuManagerDevice.SendValueGain();
                    }
                    else if (currentSelectedDevice.GetDeviceType() == DeviceType.PhilipsHueColorLight)
                    {
                        CDevicePhilipsHueLight philipsHueDevice = (CDevicePhilipsHueLight)currentSelectedDevice;
                        philipsHueDevice.ValueGainPercent = (byte)myTrackBar.Value;
                    }
                    else if (currentSelectedDevice is CDeviceHueEntertainment hueArea)
                    {
                        hueArea.ValueGainPercent = (byte)myTrackBar.Value;
                    }
                }

                // Set Tooltip Text
                string szTooltip = myTrackBar.Value.ToString();
                TTHelper.SetToolTip(myTrackBar, szTooltip);
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
        }

        private void tbColorBoost_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                // Get self references
                TrackBar myTrackBar = (TrackBar)sender;

                // Set to running Device!
                if (currentSelectedDevice != null)
                {
#if USE_DREAMSCREEN
                    if (currentSelectedDevice.GetDeviceType() == DeviceType.DreamScreen4K ||
                        currentSelectedDevice.GetDeviceType() == DeviceType.DreamScreen4KSolo ||
                        currentSelectedDevice.GetDeviceType() == DeviceType.DreamScreenHD)
                    {
                        CDeviceDreamScreen dreamscreenDevice = (CDeviceDreamScreen)currentSelectedDevice;
                        dreamscreenDevice.ColorBoostPercent = (byte)myTrackBar.Value;
                    }
                    else if (currentSelectedDevice.GetType() == typeof(CDeviceAmbiBack))
#else
                    if (currentSelectedDevice.GetType() == typeof(CDeviceAmbiBack))
#endif
                    {
                        CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)currentSelectedDevice;
                        nodeMcuManagerDevice.ColorBoostPercent = (byte)myTrackBar.Value;
                        nodeMcuManagerDevice.SendColorBoost();
                    }
                    else if (currentSelectedDevice.GetDeviceType() == DeviceType.PhilipsHueColorLight)
                    {
                        CDevicePhilipsHueLight philipsHueDevice = (CDevicePhilipsHueLight)currentSelectedDevice;
                        philipsHueDevice.ColorBoostPercent = (byte)myTrackBar.Value;
                    }
                    else if (currentSelectedDevice is CDeviceHueEntertainment hueArea)
                    {
                        hueArea.ColorBoostPercent = (byte)myTrackBar.Value;
                    }
                }

                // Set Tooltip Text
                string szTooltip = myTrackBar.Value.ToString();
                TTHelper.SetToolTip(myTrackBar, szTooltip);
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
        }
        private void tbFramesPerSecond_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                // Get self references
                TrackBar myTrackBar = (TrackBar)sender;

                // Set to running Device!
                if (currentSelectedDevice != null)
                {
#if USE_DREAMSCREEN
                    if (currentSelectedDevice.GetDeviceType() == DeviceType.DreamScreen4K ||
                        currentSelectedDevice.GetDeviceType() == DeviceType.DreamScreen4KSolo ||
                        currentSelectedDevice.GetDeviceType() == DeviceType.DreamScreenHD)
                    {
                        CDeviceDreamScreen dreamscreenDevice = (CDeviceDreamScreen)currentSelectedDevice;
                        dreamscreenDevice.FramesPerSecond = (byte)myTrackBar.Value;
                    }
                    else if (currentSelectedDevice.GetType() == typeof(CDeviceAmbiBack))
#else
                    if (currentSelectedDevice.GetType() == typeof(CDeviceAmbiBack))
#endif
                    {
                        CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)currentSelectedDevice;
                        nodeMcuManagerDevice.FramesPerSecond = (byte)myTrackBar.Value;
                    }
                    else if (currentSelectedDevice.GetDeviceType() == DeviceType.PhilipsHueColorLight)
                    {
                        CDevicePhilipsHueLight philipsHueDevice = (CDevicePhilipsHueLight)currentSelectedDevice;
                        philipsHueDevice.FramesPerSecond = (byte)myTrackBar.Value;
                    }
                    else if (currentSelectedDevice is CDeviceHueEntertainment hueArea)
                    {
                        hueArea.FramesPerSecond = (byte)myTrackBar.Value;
                    }
                }

                // Set Tooltip Text
                string szTooltip = myTrackBar.Value.ToString();
                TTHelper.SetToolTip(myTrackBar, szTooltip);
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }

        }
        private void tbInnerPicture_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                // Get self references
                TrackBar myTrackBar = (TrackBar)sender;

                // Set to running Device!
                if (currentSelectedDevice != null)
                {
#if USE_DREAMSCREEN
                    if (currentSelectedDevice.GetDeviceType() == DeviceType.DreamScreen4K ||
                        currentSelectedDevice.GetDeviceType() == DeviceType.DreamScreen4KSolo ||
                        currentSelectedDevice.GetDeviceType() == DeviceType.DreamScreenHD)
                    {
                        CDeviceDreamScreen dreamscreenDevice = (CDeviceDreamScreen)currentSelectedDevice;
                        dreamscreenDevice.InnerSizePercent = (byte)myTrackBar.Value;
                    }
                    else if (currentSelectedDevice.GetType() == typeof(CDeviceAmbiBack))
#else
                    if (currentSelectedDevice.GetType() == typeof(CDeviceAmbiBack))
#endif
                    {
                        CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)currentSelectedDevice;
                        nodeMcuManagerDevice.SetInnerSizePercent((byte)myTrackBar.Value);
                    }
                    else if (currentSelectedDevice is CDeviceHueEntertainment hueArea)
                    {
                        hueArea.InnerSizePercent = (byte)myTrackBar.Value;
                        hueArea.RecalculateScanAreas();
                    }
                }

                // Set Tooltip Text
                string szTooltip = myTrackBar.Value.ToString();
                TTHelper.SetToolTip(myTrackBar, szTooltip);
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
        }
        private void txtStartDebug_Click(object sender, EventArgs e)
        {
            if (currentSelectedDevice != null)
            {
                if (currentSelectedDevice.GetType() == typeof(CDeviceAmbiBack))
                {
                    CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)currentSelectedDevice;
                    nodeMcuManagerDevice.StartSyslogDebugging(txtDebugSyslogServer.Text, txtDebugSyslogPort.Text);
                }
            }
        }

        private void cmdStopDebug_Click(object sender, EventArgs e)
        {
            if (currentSelectedDevice != null)
            {
                if (currentSelectedDevice.GetType() == typeof(CDeviceAmbiBack))
                {
                    CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)currentSelectedDevice;
                    nodeMcuManagerDevice.StopSyslogDebugging();
                }
            }
        }
        private void mnuAbout_Click(object sender, EventArgs e)
        {
            frmAbout myFrm = new frmAbout();
            myFrm.ShowDialog(this);
        }
        #endregion

        #region ListView Functions
        private void mnuDetails_Click(object sender, EventArgs e)
        {
            try
            {
                LVDevices.CheckBoxes = true;
                LVDevices.View = View.Details;
            }
            catch (Exception ex)
            {
                // Output Debug
                CGlobals.DebugOutput(ex);
            }
        }

        private void mnuList_Click(object sender, EventArgs e)
        {
            try
            {
                LVDevices.CheckBoxes = true;
                LVDevices.View = View.List;
            }
            catch (Exception ex)
            {
                // Output Debug
                CGlobals.DebugOutput(ex);
            }
        }
        private void mnuTile_Click(object sender, EventArgs e)
        {
            try
            {
                LVDevices.CheckBoxes = false;
                LVDevices.View = View.Tile;
            }
            catch (Exception ex)
            {
                // Output Debug
                CGlobals.DebugOutput(ex);
            }
        }
        private void mnuSmallIcons_Click(object sender, EventArgs e)
        {
            try
            {
                LVDevices.View = View.SmallIcon;
                LVDevices.CheckBoxes = true;
            }
            catch (Exception ex)
            {
                // Output Debug
                CGlobals.DebugOutput(ex);
            }
        }

        private void mnuLargeIcons_Click(object sender, EventArgs e)
        {
            try
            {
                LVDevices.CheckBoxes = true;
                LVDevices.View = View.LargeIcon;
            }
            catch (Exception ex)
            {
                // Output Debug
                CGlobals.DebugOutput(ex);
            }
        }

        private void btnChangeView_Click(object sender, EventArgs e)
        {
            if (LVDevices.View == View.Details)
                mnuList_Click(sender, e);
            else if (LVDevices.View == View.List)
                mnuTile_Click(sender, e);
            else if (LVDevices.View == View.Tile)
                mnuSmallIcons_Click(sender, e);
            else if (LVDevices.View == View.SmallIcon)
                mnuLargeIcons_Click(sender, e);
            else if (LVDevices.View == View.LargeIcon)
                mnuDetails_Click(sender, e);
        }

        private void LVDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool bEnableOptions = false;

            try
            {
                this.Cursor = Cursors.WaitCursor;

                if (LVDevices.SelectedItems.Count > 0 && LVDevices.SelectedItems[0].Tag != null)
                {
                    // Get new Selected Device
                    CDevice selectedDevice = (CDevice)LVDevices.SelectedItems[0].Tag;

                    // Unset old selected device, this will also avoid that Trackbars send data to controller
                    this.currentSelectedDevice = null;

                    tcMain.Visible = true;
                    string szSelectedTab = tcMain.SelectedTab.Name;

                    // Tabs depend on the device (debug support) and on Easy / Advanced mode
                    UpdateTabs(selectedDevice);
                    if (tcMain.TabPages[szSelectedTab] != null)
                        tcMain.SelectedTab = tcMain.TabPages[szSelectedTab];
                    else
                        tcMain.SelectedTab = tcMain.TabPages["tabMain"];

                    // Set Control Defaults
                    this.txtDeviceName.Text = selectedDevice.GetDeviceName();
                    tbFramesPerSecond.Maximum = selectedDevice.MaxFramesPerSecond;
                    tbFramesPerSecond.TickFrequency = selectedDevice.StepFramesPerSecond;
                    tbFramesPerSecond.SmallChange = selectedDevice.StepFramesPerSecond;
                    tbFramesPerSecond.LargeChange = selectedDevice.StepFramesPerSecond;
                    tbSmoothing.Minimum = selectedDevice.MinFrameDelay;
                    tbSmoothing.Maximum = selectedDevice.MaxFrameDelay;
                    tbSmoothing.TickFrequency = selectedDevice.StepFrameDelay;
                    tbSmoothing.SmallChange = selectedDevice.StepFrameDelay;
                    tbSmoothing.LargeChange = selectedDevice.StepFrameDelay;

                    // Disable/enable controls
                    this.tbRed.Enabled = selectedDevice.EditColorRedPercent;
                    this.tbGreen.Enabled = selectedDevice.EditColorGreenPercent;
                    this.tbBlue.Enabled = selectedDevice.EditColorBluePercent;
                    this.tbColorBoost.Enabled = selectedDevice.EditColorBoostPercent;
                    this.tbValueGain.Enabled = selectedDevice.EditValueGainPercent;

                    this.txtDeviceName.Enabled = selectedDevice.EditDeviceName;
                    this.tbFramesPerSecond.Enabled = selectedDevice.EditFrameDelay;
                    this.tbFramesPerSecond.Enabled = selectedDevice.EditFramesPerSecond;

#if USE_DREAMSCREEN
                    // Device bases settings 
                    if (selectedDevice.GetType().ToString() == "AmbiBack.CDeviceDreamScreen")
                    {
                        CDeviceDreamScreen dreamscreenDevice = (CDeviceDreamScreen)selectedDevice;

                        // Check if selected device is supported!
                        if (selectedDevice.GetDeviceType() == DeviceType.DreamScreen4K ||
                            selectedDevice.GetDeviceType() == DeviceType.DreamScreen4KSolo ||
                            selectedDevice.GetDeviceType() == DeviceType.DreamScreenHD)
                        {
                            tbInnerPicture.Value = dreamscreenDevice.InnerSizePercent;
                            tbFramesPerSecond.Value = dreamscreenDevice.FramesPerSecond;
                            tbColorBoost.Value = dreamscreenDevice.ColorBoostPercent;

                            // Set selected Device 
                            this.currentSelectedDevice = dreamscreenDevice;
                            bEnableOptions = true;

                            // Obtain Installation Settings from device!
                            CGlobals.deviceController.SendDreamscreenCommand((CDeviceDreamScreen)this.currentSelectedDevice, DsCommands.Install_SKU_Setup, 0xFF, (byte)DsFlags.Receive, new byte[0]);
                        }
                    }
                    else if (selectedDevice.GetType().ToString() == "AmbiBack.CDeviceAmbiBack")
#else
                    if (selectedDevice.GetType().ToString() == "AmbiBack.CDeviceAmbiBack")
#endif
                    {
                        CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)selectedDevice;

                        try
                        {
                            // Refresh values from device
                            nodeMcuManagerDevice.ReadOptionsFromDevice();
                        }
                        catch (WebException exweb)
                        {
                            if (exweb.Status == WebExceptionStatus.Timeout)
                            {
                                // Device OFFLINE
                                nodeMcuManagerDevice.SetIsOnline(false);
                            }
                        }

                        // Refresh some properties
                        this.txtDeviceName.Text = nodeMcuManagerDevice.GetDeviceName();
                        LVDevices.SelectedItems[0].SubItems[1].Text = nodeMcuManagerDevice.GetDeviceName();

                        // Set properties
                        tbInnerPicture.Value = nodeMcuManagerDevice.InnerSizePercent;
                        tbFramesPerSecond.Value = nodeMcuManagerDevice.FramesPerSecond;
                        cbConnectionType.SelectedIndex = nodeMcuManagerDevice.ConnectionType;

                        tbRed.Value = nodeMcuManagerDevice.ColorRedPercent;
                        tbGreen.Value = nodeMcuManagerDevice.ColorGreenPercent;
                        tbBlue.Value = nodeMcuManagerDevice.ColorBluePercent;
                        tbValueGain.Value = nodeMcuManagerDevice.ValueGainPercent;
                        tbColorBoost.Value = nodeMcuManagerDevice.ColorBoostPercent;
                        tbMaxBrightness.Value = nodeMcuManagerDevice.MaxBrightnessPercent;
                        tbMinimumLuminosity.Value = nodeMcuManagerDevice.MinimumLuminosityPercent;
                        tbSmoothing.Value = nodeMcuManagerDevice.FrameDelay;

                        // Set new selected Device 
                        this.currentSelectedDevice = nodeMcuManagerDevice;
                        bEnableOptions = true;
                    }
                    else if (selectedDevice.GetType().ToString() == "AmbiBack.CDevicePhilipsHueLight")
                    {
                        CDevicePhilipsHueLight philipsHueLight = (CDevicePhilipsHueLight)selectedDevice;

                        // Refresh values from device
                        //                        philipsHueLight.ReadOptionsFromDevice();

                        // Set properties
                        tbInnerPicture.Value = philipsHueLight.InnerSizePercent;
                        tbFramesPerSecond.Value = philipsHueLight.FramesPerSecond;
                        tbRed.Value = philipsHueLight.ColorRedPercent;
                        tbGreen.Value = philipsHueLight.ColorGreenPercent;
                        tbBlue.Value = philipsHueLight.ColorBluePercent;
                        tbColorBoost.Value = philipsHueLight.ColorBoostPercent;
                        tbMaxBrightness.Value = philipsHueLight.MaxBrightnessPercent;
                        tbMinimumLuminosity.Value = philipsHueLight.MinimumLuminosityPercent;
                        tbSmoothing.Value = (philipsHueLight.FrameDelay >= 100 ? (philipsHueLight.FrameDelay <= 1000 ? philipsHueLight.FrameDelay : 1000) : 100);

                        // Set new selected Device 
                        this.currentSelectedDevice = philipsHueLight;
                        bEnableOptions = true;
                    }
                    else if (selectedDevice is CDeviceHueEntertainment hueArea)
                    {
                        tbInnerPicture.Value = Math.Max(tbInnerPicture.Minimum, Math.Min(tbInnerPicture.Maximum, (int)hueArea.InnerSizePercent));
                        tbFramesPerSecond.Value = Math.Max(tbFramesPerSecond.Minimum, Math.Min(tbFramesPerSecond.Maximum, (int)hueArea.FramesPerSecond));
                        tbRed.Value = hueArea.ColorRedPercent;
                        tbGreen.Value = hueArea.ColorGreenPercent;
                        tbBlue.Value = hueArea.ColorBluePercent;
                        tbColorBoost.Value = hueArea.ColorBoostPercent;
                        tbMaxBrightness.Value = hueArea.MaxBrightnessPercent;
                        tbMinimumLuminosity.Value = hueArea.MinimumLuminosityPercent;

                        this.currentSelectedDevice = hueArea;
                        bEnableOptions = true;
                    }

                    // Also set 
                    if (selectedAmbiScreen != null)
                        selectedAmbiScreen.SetSelectedDevice(this.currentSelectedDevice);

                }
            }
            catch (Exception ex)
            {
                // Output Debug
                txtDebug.AppendText("Exception in LVDevices_SelectedIndexChanged: " + ex.ToString());
                CGlobals.DebugOutput(ex);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }

            tabMain.Enabled = bEnableOptions;
            btnConfig.Enabled = bEnableOptions;
            btnDelete.Enabled = bEnableOptions;
        }

        private void LVDevices_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (e.Item.Tag != null && selectedAmbiScreen != null)
            {
                CDevice selectedDevice = (CDevice)e.Item.Tag;

                if (e.Item.Checked && !selectedAmbiScreen.GetDevices().Contains(e.Item.Tag))
                {
                    // Add device to AmbiScreen
                    selectedAmbiScreen.AddDevice((CDevice)e.Item.Tag);

                    // Start thread if AmbiScreen running
                    if (selectedAmbiScreen.MonitorEnabled && selectedAmbiScreen.AmbiBackRunning)
                        selectedDevice.StartThread();
                }
                if (!e.Item.Checked && selectedAmbiScreen.GetDevices().Contains(e.Item.Tag))
                {
                    // Remove device from to AmbiScreen
                    selectedAmbiScreen.RemoveDevice((CDevice)e.Item.Tag);

                    // Start thread if AmbiScreen running
                    if (selectedAmbiScreen.MonitorEnabled && selectedAmbiScreen.AmbiBackRunning)
                        selectedDevice.AbortThread();

                }

            }
        }

        private void LVDevices_DoubleClick(object sender, EventArgs e)
        {
            // Show Host Dialog
            viewDetailsToolStripMenuItem_Click(sender, e);
        }
        private void viewDetailsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (LVDevices.SelectedItems.Count > 0 && currentSelectedDevice != null)
            {
#if USE_DREAMSCREEN
                if (currentSelectedDevice.GetType().ToString() == "AmbiBack.CDeviceDreamScreen")
                {
                    CDeviceDreamScreen dreamscreenDevice = (CDeviceDreamScreen)currentSelectedDevice;

                    // Create new dialog
                    frmDeviceDreamScreen newFrm = new frmDeviceDreamScreen();
                    newFrm.dsDevice = dreamscreenDevice;

                    // Show the Details dialog
                    newFrm.ShowDialog(this);
                }
                else if (currentSelectedDevice.GetType().ToString() == "AmbiBack.CDeviceAmbiBack")
#else
                if (currentSelectedDevice.GetType().ToString() == "AmbiBack.CDeviceAmbiBack")
#endif
                {
                    CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)currentSelectedDevice;

                    try
                    {
                        // Create new dialog
                        frmDeviceAmbiBack newFrm = new frmDeviceAmbiBack();
                        newFrm.abDevice = nodeMcuManagerDevice;

                        // Show the Details dialog
                        newFrm.ShowDialog(this);
                    }
                    catch (Exception ex)
                    {
                        CGlobals.DebugOutput(ex);
                    }
                }
                else if (currentSelectedDevice.GetType().ToString() == "AmbiBack.CDevicePhilipsHueLight")
                {
                    CDevicePhilipsHueLight philipsHueLight = (CDevicePhilipsHueLight)currentSelectedDevice;
                    // TODO
                }
            }
        }

        private void connectHueBridgeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Show AddHost Dialog
                frmConnectHue hueForm = new frmConnectHue();
                hueForm.m_ParentForm = this;
                hueForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }

        }

        private void txtDeviceName_TextChanged(object sender, EventArgs e)
        {
            try
            {
                // Get self references
                TextBox myTextBox = (TextBox)sender;

                // Set to running Device!
                if (currentSelectedDevice != null)
                {
                    if (currentSelectedDevice.GetType() == typeof(CDeviceAmbiBack))
                    {
                        CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)currentSelectedDevice;
                        nodeMcuManagerDevice.SetDeviceName(myTextBox.Text);
                        nodeMcuManagerDevice.SendDeviceName();
                    }
                    else if (currentSelectedDevice.GetDeviceType() == DeviceType.PhilipsHueColorLight)
                    {
                        CDevicePhilipsHueLight philipsHueDevice = (CDevicePhilipsHueLight)currentSelectedDevice;
                        philipsHueDevice.SetDeviceName(myTextBox.Text);
                        // NOT SUPPORTED TO SAVE NAME YET!
                        philipsHueDevice.SendDeviceName();
                    }
                }

                // Update ListView
                if (currentSelectedDevice != null &&
                    LVDevices.SelectedItems.Count > 0 &&
                    LVDevices.SelectedItems[0].Tag != null)
                {
                    LVDevices.SelectedItems[0].SubItems[1].Text = currentSelectedDevice.GetDeviceName();
                }

                //                // Set Tooltip Text
                //                string szTooltip = myTrackBar.Value.ToString();
                //                TTHelper.SetToolTip(myTrackBar, szTooltip);
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }

        }


        #endregion

        private void testVideosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmWeb newFrm = new frmWeb();
            newFrm.Show(this);
            this.TopLevel = true;
        }

        private void hideAmbiBackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void nficon_BalloonTipShown(object sender, EventArgs e)
        {
            nficon.Tag = true;
        }
        private void nficon_BalloonTipClosed(object sender, EventArgs e)
        {
            nficon.Tag = false;
        }

        private void nficon_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if ((bool)nficon.Tag == false)
                {
                    TimerTaskIcon.Enabled = true;
                }
                pNotifyIcon = Cursor.Position;
            }
            catch (Exception)
            {
                // DO NOTHING
            }
        }

        private void exitFromTaskIcon_Click(object sender, EventArgs e)
        {
            nficon.Tag = false;
            exitToolStripMenuItem_Click(sender, e);
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            nficon.Tag = false;
            this.Show();
        }

        private void nficon_DoubleClick(object sender, EventArgs e)
        {
            nficon.Tag = false;
            this.Show();
        }

        private void TimerTaskIcon_Tick(object sender, EventArgs e)
        {
            try
            {
                TimerTaskIcon.Enabled = false;

                if (pNotifyIcon == Cursor.Position)
                {
                    string szNew = "";
                    int iNum = 1;
                    foreach (Control myControl in panScreens.Controls)
                    {
                        AmbiMonitor myAmbiScreen = (AmbiMonitor)myControl;
                        szNew += L.F("mon_title", iNum) + ": " + (myAmbiScreen.AmbiBackRunning ? (myAmbiScreen.screenHelper != null ? myAmbiScreen.szLastStatus : "") : L.T("mon_idle")) + "\r\n";
                        iNum++;
                    }
                    nficon.BalloonTipIcon = ToolTipIcon.Info;
                    nficon.BalloonTipText = szNew;
                    nficon.ShowBalloonTip(5000);
                }
                else
                {
                    nficon.Tag = false;
                }
            }
            catch (Exception ex)
            {
                CGlobals.HandleException("TimerTaskIcon_Tick", ex);
            }
        }

        private void timerComPort_Tick(object sender, EventArgs e)
        {
            try
            {
                if (CGlobals.deviceController.UpdateComPorts())
                {
                    // Read device informations from all Serial Connections
                    CGlobals.deviceController.ReadInfoFromComPorts();
                }
            }
            catch (Exception ex)
            {
                CGlobals.HandleException("timerComPort_Tick", ex);
            }

        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmOptions myFrm = new frmOptions();
            myFrm.ShowDialog(this);
        }

        private void saveSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveAmbibackSettings();
        }

        private void connectWifiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (LVDevices.SelectedItems.Count > 0 && currentSelectedDevice != null)
            {
                if (currentSelectedDevice.GetType().ToString() == "AmbiBack.CDeviceAmbiBack")
                {
                    try
                    {
                        CDeviceAmbiBack ambiBackDevice = (CDeviceAmbiBack)currentSelectedDevice;

                        // Show AddHost Dialog
                        frmConnectWifi myfrm = new frmConnectWifi();
                        myfrm.m_ParentForm = this;
                        myfrm.myDevice = ambiBackDevice;
                        myfrm.ShowDialog(this);
                    }
                    catch (Exception ex)
                    {
                        CGlobals.DebugOutput(ex);
                    }
                }
            }
        }

        private void deleteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            btnDelete_Click(sender, e);
        }

        private void LVDevices_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                bool bDebiceControls = false;
                bool bUsbControls = false;
                if (LVDevices.SelectedItems.Count > 0 && currentSelectedDevice != null)
                {
                    if (currentSelectedDevice.GetType().ToString() == "AmbiBack.CDeviceAmbiBack")
                    {
                        bDebiceControls = true;
                        try
                        {
                            CDeviceAmbiBack ambiBackDevice = (CDeviceAmbiBack)currentSelectedDevice;
                            if (ambiBackDevice.IsSerial())
                                bUsbControls = true;
                        }
                        catch (Exception ex)
                        {
                            CGlobals.DebugOutput(ex);
                        }
                    }
                }
                deleteToolStripMenuItem1.Enabled = bDebiceControls;
                toolStripMenuItemShowDetails.Enabled = bDebiceControls;
                connectWifiToolStripMenuItem.Enabled = bUsbControls;
            }
        }

        private void cbConnectionType_SelectedValueChanged(object sender, EventArgs e)
        {
            try
            {
                // Get self references
                ComboBox myCombobox= (ComboBox)sender;
                
                // Set to running Device!
                if (currentSelectedDevice != null)
                {
                    if (currentSelectedDevice.GetType() == typeof(CDeviceAmbiBack))
                    {
                        CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)currentSelectedDevice;
                        nodeMcuManagerDevice.ConnectionType = (byte) myCombobox.SelectedIndex;
                    }
                }

                // Set Tooltip Text
                string szTooltip = myCombobox.SelectedItem.ToString();
                TTHelper.SetToolTip(myCombobox, szTooltip);
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
        }

        private void chkShowDebugMessages_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                // Set to running Device!
                if (currentSelectedDevice != null)
                {
                    if (currentSelectedDevice.GetType() == typeof(CDeviceAmbiBack))
                    {
                        CDeviceAmbiBack deviceAmbiBack = (CDeviceAmbiBack)currentSelectedDevice;

                        try
                        {
                            if (deviceAmbiBack.IsSerial())
                            {
                                if (chkShowDebugMessages.Checked)
                                {
                                    // Cleanup!
                                    this.txtDebug.Text = "";

                                    // Open Serial Conenction
                                    deviceAmbiBack.SetSerialDebug(true);
                                    deviceAmbiBack.OpenSerial(false);
                                    deviceAmbiBack.DebugOutput += new EventDebugOutput(this.EventDebugOutput);

                                    cmdReboot.Enabled = true;
                                    cmdGetInfo.Enabled = true;
                                }
                                else
                                {
                                    deviceAmbiBack.SetSerialDebug(false);
                                    deviceAmbiBack.DebugOutput -= new EventDebugOutput(this.EventDebugOutput);

                                    cmdReboot.Enabled = false;
                                    cmdGetInfo.Enabled = false;
                                }
                            }
                        }
                        catch (System.UnauthorizedAccessException ex)
                        {
                            // Show Error message
                            CGlobals.MyMessageBox(this, L.T("msg_serial_title"), L.F("msg_serial_text", deviceAmbiBack.szSerialPort, ex.Message),
                                false, "", MessageBoxIcon.Warning, MessageBoxButtons.OK);

                            // Disable serial connection until next discovery!
                            deviceAmbiBack.SetIsSerial(false);
                            deviceAmbiBack.szSerialPort = "";

                            // trigger display Update!
                            CGlobals.deviceController.HandleDiscoveredDevice(deviceAmbiBack);

                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
        }

        private void cmdReboot_Click(object sender, EventArgs e)
        {
            try
            {
                // Set to running Device!
                if (currentSelectedDevice != null)
                {
                    if (currentSelectedDevice.GetType() == typeof(CDeviceAmbiBack))
                    {
                        CDeviceAmbiBack deviceAmbiBack = (CDeviceAmbiBack)currentSelectedDevice;

                        // Payload helpers 
                        MemoryStream msPacket = new MemoryStream();
                        BinaryWriter bwPacket = new BinaryWriter(msPacket);

                        // Write Byte array
                        bwPacket.Write("%REBOOT:");
                        bwPacket.Write((byte)0x00);

                        // Send byte area to serial
                        byte[] bData = msPacket.ToArray();
                        deviceAmbiBack.WriteSerialCommand(bData);
                    }
                }
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
        }

        private void cmdGetInfo_Click(object sender, EventArgs e)
        {
            try
            {
                // Set to running Device!
                if (currentSelectedDevice != null)
                {
                    if (currentSelectedDevice.GetType() == typeof(CDeviceAmbiBack))
                    {
                        CDeviceAmbiBack deviceAmbiBack = (CDeviceAmbiBack)currentSelectedDevice;

                        // Payload helpers 
                        MemoryStream msPacket = new MemoryStream();
                        BinaryWriter bwPacket = new BinaryWriter(msPacket);

                        // Write Byte array
                        bwPacket.Write("%INFO:");
                        bwPacket.Write((byte)0x00);

                        // Send byte area to serial
                        byte[] bData = msPacket.ToArray();
                        deviceAmbiBack.WriteSerialCommand(bData);
                    }
                }
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
        }

        private void cmdClearDebug_Click(object sender, EventArgs e)
        {
            txtDebug.Text = "";
        }
    }
}
