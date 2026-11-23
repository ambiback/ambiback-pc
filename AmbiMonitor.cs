using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

// Custom
using System.Collections;
using System.Threading;
using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace AmbiBack
{
    public partial class AmbiMonitor : UserControl
    {
        // Helper objects 
        public CScreenHelper screenHelper = null;
        private CDevice selectedDevice = null;
        public string szLastStatus = ""; 

        public ArrayList m_aDevices;

        // Global Bitmaps Helper
        public Bitmap bmpScreen = null;

        // Private Properties
        private bool _SettingsLoading = false;
        private bool _MonitorEnabled = false; // Monitor always enabled!
        private bool _AmbiBackRunning = false;
        private int _ControlHeight = 0;

        private bool bDirectionControls = false;
        private bool bNumControlls = false;
        private bool bSectorControls = false;
        private bool bControlsEnabled = false;
        private long lScreenLastUpdate = 0; // Ticks in Milliseconds since last Screen Update


        #region get/Set functions
        // Properties
        public int Device { get; set; }
        public int ScreenNumber { get; set; }
        public bool MonitorEnabled {
            get { return _MonitorEnabled; }
            set {
                _MonitorEnabled = value;
                // Set CheckBox
                btnEnable.Checked = this._MonitorEnabled;
                // Trigger form events 
                btnEnable_Click(null, null);
            }
        }
        public int GetAdapterCount()
        {
            return (screenHelper == null ? -1 : screenHelper.GetAdapterCount());
        }

        public bool AmbiBackRunning
        {
            get { return _AmbiBackRunning; }
            set { _AmbiBackRunning = value; }
        }
        public void AddDevice(CDevice newDevice) {
            // Add reference to arraylist
            if (newDevice != null)
            {
                m_aDevices.Add(newDevice);

                // Bind necessary events to Selected Device
                newDevice.DebugOutput += new EventDebugOutput(this.EventDebugOutput);
//                        myDevice.UpdateStatus += new EventUpdateStatus(this.DeviceUpdateStatus);
            }
        }
        public void RemoveDevice(CDevice newDevice)
        {
            if (newDevice != null)
                m_aDevices.Remove(newDevice); 
        }
        public void SetTitel(string szTitel) {
            lblTitel.Text = szTitel; 
        }
        public ArrayList GetDevices() {
            return m_aDevices;
        }
        public CDevice GetSelectedDevice()
        {
            return selectedDevice;
        }
        public void SetControlsBySelectedDevice()
        {
            // Do not write back to controller yet
            this._SettingsLoading = true;

            // Check which controls to show / enable
            if (selectedDevice == null) {
                bNumControlls = false;
                bDirectionControls = false;
                bSectorControls = false;
            } else if (selectedDevice.GetType() == typeof(CDeviceAmbiBack)) {
                bNumControlls = true;
                bDirectionControls = true;
                bSectorControls = false;

                // Get properties from Device
                CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)selectedDevice;
                cbLedsHori.Text = nodeMcuManagerDevice.PixelHori.ToString();
                cbLedsVert.Text = nodeMcuManagerDevice.PixelVert.ToString();
            } else if (selectedDevice.GetDeviceType() == DeviceType.PhilipsHueColorLight)
            {
                bNumControlls = false;
                bDirectionControls = false; 
                bSectorControls = true;

                // Get properties from Device
                CDevicePhilipsHueLight philipsHueLight = (CDevicePhilipsHueLight)selectedDevice;

                if (philipsHueLight.ScreenSectors.Length == 12)
                {
                    this.rbSector0.Checked = (philipsHueLight.ScreenSectors[0] == 1) ? true : false;
                    this.rbSector1.Checked = (philipsHueLight.ScreenSectors[1] == 1) ? true : false;
                    this.rbSector2.Checked = (philipsHueLight.ScreenSectors[2] == 1) ? true : false;
                    this.rbSector3.Checked = (philipsHueLight.ScreenSectors[3] == 1) ? true : false;
                    this.rbSector4.Checked = (philipsHueLight.ScreenSectors[4] == 1) ? true : false;
                    this.rbSector5.Checked = (philipsHueLight.ScreenSectors[5] == 1) ? true : false;
                    this.rbSector6.Checked = (philipsHueLight.ScreenSectors[6] == 1) ? true : false;
                    this.rbSector7.Checked = (philipsHueLight.ScreenSectors[7] == 1) ? true : false;
                    this.rbSector8.Checked = (philipsHueLight.ScreenSectors[8] == 1) ? true : false;
                    this.rbSector9.Checked = (philipsHueLight.ScreenSectors[9] == 1) ? true : false;
                    this.rbSector10.Checked = (philipsHueLight.ScreenSectors[10] == 1) ? true : false;
                    this.rbSector11.Checked = (philipsHueLight.ScreenSectors[11] == 1) ? true : false;
                }
            }
            
            // Set visible controls
            this.SetControlsVisibility();

            // Do not write back to controller yet
            this._SettingsLoading = false;

            // Set direction buttons after _SettingsLoading
            this.SetDirectionButtons();
        }
        public void SetControlsVisibility()
        {
            lblLedsHori.Visible = bNumControlls;
            cbLedsHori.Visible = bNumControlls;
            lblLedsVert.Visible = bNumControlls;
            cbLedsVert.Visible = bNumControlls;

            // Set Sector Controls
            if (bSectorControls)
            {
                tblSectors.Top = monPreview.Top;
                tblSectors.Left = monPreview.Left;
                tblSectors.Height = monPreview.Height;
                tblSectors.Width = monPreview.Width;
            }
            tblSectors.Visible = bSectorControls;

            // Set direction controls
            chkTopLeft.Visible = bDirectionControls;
            chkTopRight.Visible = bDirectionControls;
            chkBottomLeft.Visible = bDirectionControls;
            chkBottomRight.Visible = bDirectionControls;
        }
        public void SetControlsEnabledDisabled(bool bEnabled)
        {
            this.bControlsEnabled = bEnabled; 
            lblLedsHori.Enabled = bEnabled;
            cbLedsHori.Enabled = bEnabled;
            lblLedsVert.Enabled = bEnabled;
            cbLedsVert.Enabled = bEnabled;

            tblSectors.Enabled = bEnabled;

            chkTopLeft.Enabled = bEnabled;
            chkTopRight.Enabled = bEnabled;
            chkBottomLeft.Enabled = bEnabled;
            chkBottomRight.Enabled = bEnabled;
        }
        public void SetSelectedDevice(CDevice newDevice)
        {
            // Set selected device reference
            selectedDevice = newDevice;

            // Update Control Stuff
            this.SetControlsBySelectedDevice(); 

            // Update Control
            this.Refresh();
        }
        #endregion

        #region Threading helpers
        delegate void ScreenHelperReceivedScreenshotCallback(object sender, EventArgs e, Bitmap bmpScreenshot);
        delegate void ScreenHelperUpdateStatusCallback(object sender, EventArgs e, string szStatus);
        delegate void EventDebugOutputCallback(object sender, System.EventArgs e, string szDebug);
        #endregion

        #region Callback event functions
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
                catch (ThreadInterruptedException ex)
                {
                    // Handles by upper function
                    throw (ex);
                }
                catch (InvalidAsynchronousStateException ex)
                {
                    Debug.WriteLine("EventDebugOutput callback failed: " + ex.ToString());
                }
                catch (Exception ex)
                {
                    // Ignore exception on shutdown
                    CGlobals.DebugOutput(ex);
                }
            }
            else
            {
                CGlobals.DebugOutput(szDebug);
            }
        }
        private void ScreenHelperUpdateStatus(object sender, EventArgs e, string szStatus)
        {
            // --- Threading stuff
            if (this.InvokeRequired && !CGlobals.appIsStopping)
            {
                ScreenHelperUpdateStatusCallback myCallBack = new ScreenHelperUpdateStatusCallback(ScreenHelperUpdateStatus);
                this.Invoke(myCallBack, new object[] { sender, e, szStatus });
            }
            else
            {
                // Get Screen reference
                CScreenHelper myScreen = (CScreenHelper)sender;
                this.lblStatus.Text = szStatus;
                this.szLastStatus = szStatus; 
            }
        }
        private void ScreenHelperReceivedScreenshot(object sender, EventArgs e, Bitmap bmpScreenshot)
        {
            // Get Screen reference
            CScreenHelper myScreen = (CScreenHelper)sender;

            // --- Threading stuff
            if (this.InvokeRequired && !CGlobals.appIsStopping)
            {
                // Doing the bitmap handling in calling thread now!
                if (myScreen.bmpScreenshot != null)
                {
                    if (myScreen.GetNumOutput() == ScreenNumber)
                    {
                        lock (CGlobals.bmpLock)
                        {
                            // Clone Image to object
                            this.bmpScreen = (Bitmap)bmpScreenshot.Clone();
                            // this.bmpScreen = new Bitmap((Bitmap)bmpScreenshot, new Size(monPreview.Width, monPreview.Height)); // (Bitmap)bmpScreenshot.Clone();

                            // Clone Bmp Copy for each bound device
                            foreach (CDevice myDevice in m_aDevices)
                            {
                                myDevice.bmpScreenCopy = (Bitmap)bmpScreenshot.Clone();
                                // myDevice.bmpScreenCopy = new Bitmap((Bitmap)bmpScreenshot, new Size(CGlobals.SCREENSHOT_WIDTH, CGlobals.SCREENSHOT_HEIGHT));
                            }
                        }

                        // Only update Screen every 100ms
                        long lCurrentTimeTicks = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                        if (lCurrentTimeTicks > (this.lScreenLastUpdate + 100))
                        {
                            // Blur Image if not selected
                            if (CGlobals.mainForm.selectedAmbiScreen != null &&
                                CGlobals.mainForm.selectedAmbiScreen.Name != this.Name)
                            {
                                BitmapFilter.Brightness(bmpScreen, 160);
//                                Graphics gForm = this.CreateGraphics();
//                                ControlPaint.DrawGrid(gForm, new Rectangle(0, 0, this.Width, this.Height), new Size(2, 2), this.BackColor);
                            }

                            // Draw Preview Screen - NOW IN this thread!
                            Graphics g = (tblSectors.Visible ? tblSectors.CreateGraphics() : monPreview.CreateGraphics());
                            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                            // Draw on PB Box
                            g.DrawImage(bmpScreen, 0, 0, monPreview.Width, monPreview.Height);

/*                            // Use threading callback for other Form stuff
                            ScreenHelperReceivedScreenshotCallback myCallBack = new ScreenHelperReceivedScreenshotCallback(ScreenHelperReceivedScreenshot);
                            this.Invoke(myCallBack, new object[] { sender, e, bmpScreenshot });
*/
                            // Set new Last Update
                            this.lScreenLastUpdate = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                        }
                    }
                }
            }
            else
            {
                try
                {
/*                    // Draw Preview Screen
                    Graphics g = (tblSectors.Visible ? tblSectors.CreateGraphics() : monPreview.CreateGraphics());
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;

                    // Draw on PB Box
                    g.DrawImage(bmpScreen, 0, 0, monPreview.Width, monPreview.Height);
                    */

                }
                catch (Exception ex)
                {
                    CGlobals.DebugOutput(ex);
                }
            }
        }
        #endregion

        public AmbiMonitor(int iDevice, int iScreenMonitor)
        {
            Device = iDevice;
            ScreenNumber = iScreenMonitor;
            
            // Create Arraylist for connected devices
            m_aDevices = new ArrayList();
            
            // Set new LastUpdate
            lScreenLastUpdate = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            InitializeComponent();
        }

        #region User Control Functions
        private void DrawDirection(Graphics g, CheckBox chkFrom, CheckBox chkTo, ArrowDirection arrowDirection, bool bStart = false)
        {
            Pen pen = new Pen( (CGlobals.mainForm.selectedAmbiScreen != null && CGlobals.mainForm.selectedAmbiScreen.Name == this.Name ? Color.PaleVioletRed : Color.Gray) );
            pen.StartCap = LineCap.RoundAnchor;
            pen.EndCap = LineCap.ArrowAnchor;
            pen.Width = 6;

            int iStartLeft = 0;
            int iStartTop = 0;
            int iEndLeft = 0;
            int iEndTop = 0;
            if (arrowDirection == ArrowDirection.Left)
            {
                iStartLeft = (bStart ? chkFrom.Left - (chkFrom.Width / 2) : chkFrom.Left + (chkFrom.Width / 2));
                iStartTop = chkFrom.Top + (chkFrom.Height / 2);
                iEndLeft = chkTo.Left + chkTo.Width;
                iEndTop = chkTo.Top + (chkTo.Height / 2);
            } else if (arrowDirection == ArrowDirection.Right) {
                iStartLeft = (bStart ? chkFrom.Left + chkFrom.Width + (chkFrom.Width / 2) : chkFrom.Left + (chkFrom.Width / 2));
                iStartTop = chkFrom.Top + (chkFrom.Height / 2);
                iEndLeft = chkTo.Left;
                iEndTop = chkTo.Top + (chkTo.Height / 2);
            } else if (arrowDirection == ArrowDirection.Up) {
                iStartLeft = chkFrom.Left + (chkFrom.Width / 2);
                iStartTop = (bStart ? chkFrom.Top - (chkFrom.Height / 2): chkFrom.Top + (chkFrom.Height / 2));
                iEndLeft = chkTo.Left + (chkTo.Width / 2);
                iEndTop = chkTo.Top + chkTo.Height;
            } else if (arrowDirection == ArrowDirection.Down) {
                iStartLeft = chkFrom.Left + (chkFrom.Width / 2);
                iStartTop = (bStart ? chkFrom.Top + chkFrom.Height + (chkFrom.Height / 2) : chkFrom.Top + (chkFrom.Height / 2));
                iEndLeft = chkTo.Left + (chkTo.Width / 2);
                iEndTop = chkTo.Top;
            }
            
            // Draw Arrow
            g.DrawLine(pen, iStartLeft,
                            iStartTop,
                            iEndLeft,
                            iEndTop);
        }
        private void SetDirectionButtons()
        {
            if (selectedDevice != null && !this._SettingsLoading)
            {
                if (selectedDevice.GetType() == typeof(CDeviceAmbiBack))
                {
                    // Get properties from Device
                    CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)selectedDevice;

                    // Default all buttons to FALSE first
                    chkBottomLeft.Checked = false;
                    chkTopLeft.Checked = false;
                    chkTopRight.Checked = false;
                    chkBottomRight.Checked = false;

                    if (nodeMcuManagerDevice.FlexSetupPort1 == (byte)DsFlexSetups.Right_Cclock_bottom_right_up) {
                        // CounterClockWise Bottom Right To Up
                        chkBottomRight.Checked = true;
                    }
                    else if (nodeMcuManagerDevice.FlexSetupPort1 == (byte)DsFlexSetups.Top_Cclock_top_right_left)
                    {
                        // CounterClockWise Top Right to Left
                        chkTopRight.Checked = true;
                    }
                    else if (nodeMcuManagerDevice.FlexSetupPort1 == (byte)DsFlexSetups.Left_Cclock_top_left_down)
                    {
                        // CounterClockWise Top Left to Down
                        chkTopLeft.Checked = true;
                    }
                    else if (nodeMcuManagerDevice.FlexSetupPort1 == (byte)DsFlexSetups.Bottom_Cclock_bottom_left_right)
                    {
                        // CounterClockWise Bottom Left to Right
                        chkBottomLeft.Checked = true;
                    }
                    else if (nodeMcuManagerDevice.FlexSetupPort1 == (byte)DsFlexSetups.Right_Clock_top_right_down)
                    {
                        // ClockWise Top Right To Down
                        chkTopRight.Checked = true;
                    }
                    else if (nodeMcuManagerDevice.FlexSetupPort1 == (byte)DsFlexSetups.Top_Clock_top_left_right)
                    {
                        // ClockWise Top Left to Right
                        chkTopLeft.Checked = true;
                    }
                    else if (nodeMcuManagerDevice.FlexSetupPort1 == (byte)DsFlexSetups.Left_Clock_bottom_left_up)
                    {
                        // ClockWise Bottom Left to Up
                        chkBottomLeft.Checked = true;
                    }
                    else if (nodeMcuManagerDevice.FlexSetupPort1 == (byte)DsFlexSetups.Bottom_Clock_bottom_right_left)
                    {
                        // ClockWise Bottom Right to Left
                        chkBottomRight.Checked = true;
                    }
                }

            }
        }
        private void DrawDirections()
        {
            // Abort if controls are hidden
            if (!chkTopLeft.Visible)
                return; 
            Graphics g = this.CreateGraphics();

            if (selectedDevice != null && !this._SettingsLoading)
            {
                if (selectedDevice.GetType() == typeof(CDeviceAmbiBack))
                {
                    // Get properties from Device
                    CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)selectedDevice;

                    if (nodeMcuManagerDevice.FlexSetupPort1 == (byte)DsFlexSetups.Right_Cclock_bottom_right_up)
                    {
                        // CounterClockWise Bottom Right To Up
                        DrawDirection(g, chkBottomRight, chkTopRight, ArrowDirection.Up, true);
                        DrawDirection(g, chkTopRight, chkTopLeft, ArrowDirection.Left);
                        DrawDirection(g, chkTopLeft, chkBottomLeft, ArrowDirection.Down);
                        DrawDirection(g, chkBottomLeft, chkBottomRight, ArrowDirection.Right);
                    }
                    else if (nodeMcuManagerDevice.FlexSetupPort1 == (byte)DsFlexSetups.Top_Cclock_top_right_left)
                    {
                        // CounterClockWise Top Right to Left
                        DrawDirection(g, chkTopRight, chkTopLeft, ArrowDirection.Left, true);
                        DrawDirection(g, chkTopLeft, chkBottomLeft, ArrowDirection.Down);
                        DrawDirection(g, chkBottomLeft, chkBottomRight, ArrowDirection.Right);
                        DrawDirection(g, chkBottomRight, chkTopRight, ArrowDirection.Up);
                    }
                    else if (nodeMcuManagerDevice.FlexSetupPort1 == (byte)DsFlexSetups.Left_Cclock_top_left_down)
                    {
                        // CounterClockWise Top Left to Down
                        DrawDirection(g, chkTopLeft, chkBottomLeft, ArrowDirection.Down, true);
                        DrawDirection(g, chkBottomLeft, chkBottomRight, ArrowDirection.Right);
                        DrawDirection(g, chkBottomRight, chkTopRight, ArrowDirection.Up);
                        DrawDirection(g, chkTopRight, chkTopLeft, ArrowDirection.Left);
                    }
                    else if (nodeMcuManagerDevice.FlexSetupPort1 == (byte)DsFlexSetups.Bottom_Cclock_bottom_left_right)
                    {
                        // CounterClockWise Bottom Left to Right
                        DrawDirection(g, chkBottomLeft, chkBottomRight, ArrowDirection.Right, true);
                        DrawDirection(g, chkBottomRight, chkTopRight, ArrowDirection.Up);
                        DrawDirection(g, chkTopRight, chkTopLeft, ArrowDirection.Left);
                        DrawDirection(g, chkTopLeft, chkBottomLeft, ArrowDirection.Down);
                    }
                    else if (nodeMcuManagerDevice.FlexSetupPort1 == (byte)DsFlexSetups.Right_Clock_top_right_down)
                    {
                        // ClockWise Top Right To Down
                        DrawDirection(g, chkTopRight, chkBottomRight, ArrowDirection.Down, true);
                        DrawDirection(g, chkBottomRight, chkBottomLeft, ArrowDirection.Left);
                        DrawDirection(g, chkBottomLeft, chkTopLeft, ArrowDirection.Up);
                        DrawDirection(g, chkTopLeft, chkTopRight, ArrowDirection.Right);
                    }
                    else if (nodeMcuManagerDevice.FlexSetupPort1 == (byte)DsFlexSetups.Top_Clock_top_left_right)
                    {
                        // ClockWise Top Left to Right
                        DrawDirection(g, chkTopLeft, chkTopRight, ArrowDirection.Right, true);
                        DrawDirection(g, chkTopRight, chkBottomRight, ArrowDirection.Down);
                        DrawDirection(g, chkBottomRight, chkBottomLeft, ArrowDirection.Left);
                        DrawDirection(g, chkBottomLeft, chkTopLeft, ArrowDirection.Up);
                    }
                    else if (nodeMcuManagerDevice.FlexSetupPort1 == (byte)DsFlexSetups.Left_Clock_bottom_left_up)
                    {
                        // ClockWise Bottom Left to Up
                        DrawDirection(g, chkBottomLeft, chkTopLeft, ArrowDirection.Up, true);
                        DrawDirection(g, chkTopLeft, chkTopRight, ArrowDirection.Right);
                        DrawDirection(g, chkTopRight, chkBottomRight, ArrowDirection.Down);
                        DrawDirection(g, chkBottomRight, chkBottomLeft, ArrowDirection.Left);
                    }
                    else if (nodeMcuManagerDevice.FlexSetupPort1 == (byte)DsFlexSetups.Bottom_Clock_bottom_right_left)
                    {
                        // ClockWise Bottom Right to Left
                        DrawDirection(g, chkBottomRight, chkBottomLeft, ArrowDirection.Left, true);
                        DrawDirection(g, chkBottomLeft, chkTopLeft, ArrowDirection.Up);
                        DrawDirection(g, chkTopLeft, chkTopRight, ArrowDirection.Right);
                        DrawDirection(g, chkTopRight, chkBottomRight, ArrowDirection.Down);
                    }
                }

            }
        }
        private void AmbiMonitor_Load(object sender, EventArgs e)
        {
            try
            {
                // Set selectedDevice to 0 first time
                this.SetSelectedDevice(null); 

                // Create screenhelper if not set from outside
                if (screenHelper == null) {
                    screenHelper = new CScreenHelper(Device, ScreenNumber);
                }
                screenHelper.SetPreviewPB(this.monPreview);
                monPreview.Visible = true; /* BackColor is transparent as we draw on parent */

                // Bind events 
                screenHelper.ReceivedScreenshot += new ScreenHelperReceivedScreenshot(this.ScreenHelperReceivedScreenshot);
                screenHelper.DebugOutput += new EventDebugOutput(this.EventDebugOutput);
                screenHelper.UpdateStatus += new EventUpdateStatus(this.ScreenHelperUpdateStatus);

                // Allow more LEDs than DreamScreen!

                object[] numbers = new object[60];
                for (int i = 1; i <= 60; i++)
                    numbers[i - 1] = i;
                cbLedsVert.Enabled = false;
                cbLedsVert.Items.AddRange(numbers);
                cbLedsVert.Enabled = true;

                numbers = new object[120];
                for (int i = 1; i <= 120; i++)
                    numbers[i - 1] = i;
                cbLedsHori.Enabled = false;
                cbLedsHori.Items.AddRange(numbers);
                cbLedsHori.Enabled = true;

                this.rbSector0.Tag = 0;
                this.rbSector1.Tag = 1;
                this.rbSector2.Tag = 2;
                this.rbSector3.Tag = 3;
                this.rbSector4.Tag = 4;
                this.rbSector5.Tag = 5;
                this.rbSector6.Tag = 6;
                this.rbSector7.Tag = 7;
                this.rbSector8.Tag = 8;
                this.rbSector9.Tag = 9;
                this.rbSector10.Tag = 10;
                this.rbSector11.Tag = 11;

                // Store initial Height
                this._ControlHeight = this.Height; 
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput("Failed to init AmbiMonitor: " + ex.ToString());
            }

            // Set form defaults
            this.MonitorEnabled = true; 

        }
        public void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                this.Enabled = false;
                this.btnStart.Enabled = false;
                this.btnStop.Enabled = true;
                
                // Set AmbiBack State to running
                this.AmbiBackRunning = true;

                // --- Start Device Thread(s)
                foreach (CDevice myDevice in m_aDevices)
                {
                    if (myDevice.GetType().ToString() == "AmbiBack.CDeviceAmbiBack")
                    {
                        CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)myDevice;
                        // TODO, check set device settings 

                        // Start Device Handling Thread!
                        myDevice.StartThread();
                    } else if (myDevice.GetType().ToString() == "AmbiBack.CDevicePhilipsHueLight") {
                        CDevicePhilipsHueLight philipsHueLight = (CDevicePhilipsHueLight)myDevice;
                        // TODO, check set device settings 

                        // Start Device Handling Thread!
                        myDevice.StartThread();
                    }
                }
            }
            catch (Exception ex)
            {
                // Output Debug
                CGlobals.DebugOutput(ex);
            }
            finally
            {
                this.Enabled = true;
            }
        }
        public void btnStop_Click(object sender, EventArgs e)
        {
            try
            {
                this.Enabled = false;
                this.btnStart.Enabled = true;
                this.btnStop.Enabled = false;

                // Set AmbiBack State to stopped
                this.AmbiBackRunning = false;

                // --- Start Device Thread(s)
                foreach (CDevice myDevice in m_aDevices)
                {
                    // Stop/Pause sender thread
                    if (myDevice != null)
                        myDevice.SleepThread();
                }
            }
            catch (Exception ex)
            {
                // Output Debug
                CGlobals.DebugOutput(ex);
            }
            finally
            {
                this.Enabled = true;
            }
        }
        private void btnEnable_Click(object sender, EventArgs e)
        {
            if (btnEnable.CheckState == CheckState.Checked) {
                // Start Screenhelper Thread
                screenHelper.StartThread();
                // Set Screen to enabled
                this._MonitorEnabled = true;

                btnStart.Enabled = true;
                btnStop.Enabled = false;
                btnEnable.Image = global::AmbiBack.Properties.Resources.selection_ok;
            }
            else {
                // Stop Screenhelper threads
                if (screenHelper != null)
                    screenHelper.SleepThread();

                // Make sure threads are being stopped / paused
                btnStop_Click(null, e);

                this._MonitorEnabled = false;
                btnStart.Enabled = false;
                btnStop.Enabled = false;
                btnEnable.Image = global::AmbiBack.Properties.Resources.selection;
            }
        }
        private void cbLedsHori_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {   
                if (selectedDevice != null && !this._SettingsLoading)
                {
                    if (selectedDevice.GetType() == typeof(CDeviceAmbiBack))
                    {
                        CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)selectedDevice;
                        nodeMcuManagerDevice.SetPixelHori((byte) Int32.Parse(cbLedsHori.Text));
                        nodeMcuManagerDevice.SendLedHorizontal();
                    }
                }
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput("Failed to set PixelHori: " + ex.ToString());
            }
        }
        private void cbLedsVert_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (selectedDevice != null && !this._SettingsLoading)
                {
                    if (selectedDevice.GetType() == typeof(CDeviceAmbiBack))
                    {
                        CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)selectedDevice;
                        nodeMcuManagerDevice.SetPixelVert((byte)Int32.Parse(cbLedsVert.Text));
                        nodeMcuManagerDevice.SendLedVertical();
                    }
                }
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput("Failed to set PixelHori: " + ex.ToString());
            }
        }
        private void AmbiMonitor_Enter(object sender, EventArgs e)
        {
            // Restore height
//            this.Height = this._ControlHeight;
        }
        private void AmbiMonitor_Leave(object sender, EventArgs e)
        {
            // fold together
            //            this.Height = tsMain.Height + lblTitel.Height; 
        }
        private void rbSector_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox myCheckbox = (CheckBox)sender;
            int iNum = (int) myCheckbox.Tag;

            if (selectedDevice != null && !this._SettingsLoading)
            {
                CDevicePhilipsHueLight philipsHueLight = (CDevicePhilipsHueLight)selectedDevice;
                philipsHueLight.ScreenSectors[iNum] = (byte) (myCheckbox.Checked ? 1 : 0); 
            }
        }
        private void AmbiMonitor_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(this.BackColor); 

            DrawDirections();
            if (CGlobals.mainForm.selectedAmbiScreen != null && 
                CGlobals.mainForm.selectedAmbiScreen.Name != this.Name
                ) {
                if (this.bControlsEnabled)
                    SetControlsEnabledDisabled(false);
// NOT NEEDED ANYMORE
//                ControlPaint.DrawGrid(e.Graphics, new Rectangle(0, 0, this.Width, this.Height), new Size(2, 2), this.BackColor);
            }
            else {
                if (!this.bControlsEnabled)
                    SetControlsEnabledDisabled(true);
                if (bmpScreen != null) {
                    // Update only every 100ms
                    long lCurrentTimeTicks = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    if (lCurrentTimeTicks > (this.lScreenLastUpdate + 1000))
                    {
                        // Draw image again
                        e.Graphics.DrawImage(bmpScreen, monPreview.Left, monPreview.Top, monPreview.Width, monPreview.Height);
                        // Set new Last Update
                        this.lScreenLastUpdate = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    }
                }
            }
        }
        private void chkBottomLeft_Click(object sender, EventArgs e)
        {
            // Set Checked States
            chkBottomLeft.Checked = true;
            chkTopLeft.Checked = false;
            chkTopRight.Checked = false;
            chkBottomRight.Checked = false;

            if (selectedDevice != null && !this._SettingsLoading)
            {
                if (selectedDevice.GetType() == typeof(CDeviceAmbiBack))
                {
                    // Get properties from Device
                    CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)selectedDevice;
                    if (nodeMcuManagerDevice.FlexSetupPort1 == (byte)DsFlexSetups.Left_Clock_bottom_left_up)
                        nodeMcuManagerDevice.SetFlexPort1((byte)DsFlexSetups.Bottom_Cclock_bottom_left_right); 
                    else
                        nodeMcuManagerDevice.SetFlexPort1((byte)DsFlexSetups.Left_Clock_bottom_left_up);
                    nodeMcuManagerDevice.SendLedDirection();
                }
            }

            // Update Control
            this.Refresh();
        }
        private void chkTopLeft_Click(object sender, EventArgs e)
        {
            // Set Checked States
            chkBottomLeft.Checked = false;
            chkTopLeft.Checked = true;
            chkTopRight.Checked = false;
            chkBottomRight.Checked = false;

            if (selectedDevice != null && !this._SettingsLoading)
            {
                if (selectedDevice.GetType() == typeof(CDeviceAmbiBack))
                {
                    // Get properties from Device
                    CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)selectedDevice;
                    if (nodeMcuManagerDevice.FlexSetupPort1 == (byte)DsFlexSetups.Top_Clock_top_left_right)
                        nodeMcuManagerDevice.SetFlexPort1((byte)DsFlexSetups.Left_Cclock_top_left_down);
                    else
                        nodeMcuManagerDevice.SetFlexPort1((byte)DsFlexSetups.Top_Clock_top_left_right);
                    nodeMcuManagerDevice.SendLedDirection();
                }
            }

            // Update Control
            this.Refresh();
        }
        private void chkTopRight_Click(object sender, EventArgs e)
        {
            // Set Checked States
            chkBottomLeft.Checked = false;
            chkTopLeft.Checked = false;
            chkTopRight.Checked = true;
            chkBottomRight.Checked = false;

            if (selectedDevice != null && !this._SettingsLoading)
            {
                if (selectedDevice.GetType() == typeof(CDeviceAmbiBack))
                {
                    // Get properties from Device
                    CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)selectedDevice;
                    if (nodeMcuManagerDevice.FlexSetupPort1 == (byte)DsFlexSetups.Right_Clock_top_right_down)
                        nodeMcuManagerDevice.SetFlexPort1((byte)DsFlexSetups.Top_Cclock_top_right_left);
                    else
                        nodeMcuManagerDevice.SetFlexPort1((byte)DsFlexSetups.Right_Clock_top_right_down);
                    nodeMcuManagerDevice.SendLedDirection();
                }
            }

            // Update Control
            this.Refresh();
        }
        private void chkBottomRight_Click(object sender, EventArgs e)
        {
            // Set Checked States
            chkBottomLeft.Checked = false;
            chkTopLeft.Checked = false;
            chkTopRight.Checked = false;
            chkBottomRight.Checked = true;

            if (selectedDevice != null && !this._SettingsLoading)
            {
                if (selectedDevice.GetType() == typeof(CDeviceAmbiBack))
                {
                    // Get properties from Device
                    CDeviceAmbiBack nodeMcuManagerDevice = (CDeviceAmbiBack)selectedDevice;
                    if (nodeMcuManagerDevice.FlexSetupPort1 == (byte)DsFlexSetups.Bottom_Clock_bottom_right_left)
                        nodeMcuManagerDevice.SetFlexPort1((byte)DsFlexSetups.Right_Cclock_bottom_right_up);
                    else
                        nodeMcuManagerDevice.SetFlexPort1((byte)DsFlexSetups.Bottom_Clock_bottom_right_left);
                    nodeMcuManagerDevice.SendLedDirection(); 
                }
            }

            // Update Control
            this.Refresh();
        }

        #endregion

    }
}
