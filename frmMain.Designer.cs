namespace AmbiBack
{
    partial class frmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.MainMenu = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hideAmbiBackToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            this.saveSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.devicesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addDeviceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.viewDetailsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.connectHueBridgeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.testVideosToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuDetails = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuList = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuTile = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuSmallIcons = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuLargeIcons = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.nficon = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuTaskIcon = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.showToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitFromTaskIcon = new System.Windows.Forms.ToolStripMenuItem();
            this.stbMain = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblDebug = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsDevices = new System.Windows.Forms.ToolStrip();
            this.btnChangeView = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.btnEnumDevices = new System.Windows.Forms.ToolStripButton();
            this.btnCleanup = new System.Windows.Forms.ToolStripButton();
            this.btnAdd = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnConfig = new System.Windows.Forms.ToolStripButton();
            this.btnDelete = new System.Windows.Forms.ToolStripButton();
            this.tcMain = new System.Windows.Forms.TabControl();
            this.tabMain = new System.Windows.Forms.TabPage();
            this.tbValueGain = new System.Windows.Forms.TrackBar();
            this.lblValueGain = new System.Windows.Forms.Label();
            this.tbBlue = new System.Windows.Forms.TrackBar();
            this.lblBlue = new System.Windows.Forms.Label();
            this.tbGreen = new System.Windows.Forms.TrackBar();
            this.label4 = new System.Windows.Forms.Label();
            this.tbRed = new System.Windows.Forms.TrackBar();
            this.lblRed = new System.Windows.Forms.Label();
            this.tbMinimumLuminosity = new System.Windows.Forms.TrackBar();
            this.lblMinimumLuminosity = new System.Windows.Forms.Label();
            this.tbMaxBrightness = new System.Windows.Forms.TrackBar();
            this.lblMaxBrightness = new System.Windows.Forms.Label();
            this.tbColorBoost = new System.Windows.Forms.TrackBar();
            this.lblColorBoost = new System.Windows.Forms.Label();
            this.tabAdvanced = new System.Windows.Forms.TabPage();
            this.cbConnectionType = new System.Windows.Forms.ComboBox();
            this.lblConnectionType = new System.Windows.Forms.Label();
            this.txtDeviceName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tbSmoothing = new System.Windows.Forms.TrackBar();
            this.label1 = new System.Windows.Forms.Label();
            this.tbFramesPerSecond = new System.Windows.Forms.TrackBar();
            this.lblFrameRate = new System.Windows.Forms.Label();
            this.tbInnerPicture = new System.Windows.Forms.TrackBar();
            this.label3 = new System.Windows.Forms.Label();
            this.tabExpert = new System.Windows.Forms.TabPage();
            this.cmdStopDebugging = new System.Windows.Forms.Button();
            this.txtStartDebug = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.lblDebugSyslog = new System.Windows.Forms.Label();
            this.txtDebugSyslogPort = new System.Windows.Forms.TextBox();
            this.txtDebugSyslogServer = new System.Windows.Forms.TextBox();
            this.tabDebug = new System.Windows.Forms.TabPage();
            this.gbDebug = new System.Windows.Forms.GroupBox();
            this.chkShowDebugMessages = new System.Windows.Forms.CheckBox();
            this.cmdGetInfo = new System.Windows.Forms.Button();
            this.cmdReboot = new System.Windows.Forms.Button();
            this.txtDebug = new System.Windows.Forms.TextBox();
            this.LVDevices = new System.Windows.Forms.ListView();
            this.chDeviceType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chDeviceName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chDeviceIP = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.chDeviceStatus = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.contextMenuDevices = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItemShowDetails = new System.Windows.Forms.ToolStripMenuItem();
            this.connectWifiToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
            this.deleteToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.ILDevicesLarge = new System.Windows.Forms.ImageList(this.components);
            this.ILDevicesSmall = new System.Windows.Forms.ImageList(this.components);
            this.ILState = new System.Windows.Forms.ImageList(this.components);
            this.TimerCheck = new System.Windows.Forms.Timer(this.components);
            this.TTHelper = new System.Windows.Forms.ToolTip(this.components);
            this.panScreens = new System.Windows.Forms.Panel();
            this.splitMain = new System.Windows.Forms.Splitter();
            this.tsConMain = new System.Windows.Forms.ToolStripContainer();
            this.TimerTaskIcon = new System.Windows.Forms.Timer(this.components);
            this.timerComPort = new System.Windows.Forms.Timer(this.components);
            this.bindingSource1 = new System.Windows.Forms.BindingSource(this.components);
            this.cmdClearDebug = new System.Windows.Forms.Button();
            this.MainMenu.SuspendLayout();
            this.contextMenuTaskIcon.SuspendLayout();
            this.stbMain.SuspendLayout();
            this.tsDevices.SuspendLayout();
            this.tcMain.SuspendLayout();
            this.tabMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbValueGain)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbBlue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbGreen)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbRed)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbMinimumLuminosity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbMaxBrightness)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbColorBoost)).BeginInit();
            this.tabAdvanced.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbSmoothing)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbFramesPerSecond)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbInnerPicture)).BeginInit();
            this.tabExpert.SuspendLayout();
            this.tabDebug.SuspendLayout();
            this.gbDebug.SuspendLayout();
            this.contextMenuDevices.SuspendLayout();
            this.tsConMain.ContentPanel.SuspendLayout();
            this.tsConMain.TopToolStripPanel.SuspendLayout();
            this.tsConMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource1)).BeginInit();
            this.SuspendLayout();
            // 
            // MainMenu
            // 
            this.MainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.devicesToolStripMenuItem,
            this.toolsToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.helpToolStripMenuItem,
            this.toolStripMenuItem1});
            this.MainMenu.Location = new System.Drawing.Point(0, 0);
            this.MainMenu.Name = "MainMenu";
            this.MainMenu.Size = new System.Drawing.Size(944, 24);
            this.MainMenu.TabIndex = 4;
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.optionsToolStripMenuItem,
            this.hideAmbiBackToolStripMenuItem,
            this.toolStripMenuItem3,
            this.saveSettingsToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Image = global::AmbiBack.Properties.Resources.gear_information;
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(53, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.Image = global::AmbiBack.Properties.Resources.preferences;
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.optionsToolStripMenuItem.Text = "Options";
            this.optionsToolStripMenuItem.Click += new System.EventHandler(this.optionsToolStripMenuItem_Click);
            // 
            // hideAmbiBackToolStripMenuItem
            // 
            this.hideAmbiBackToolStripMenuItem.Image = global::AmbiBack.Properties.Resources.navigate_close;
            this.hideAmbiBackToolStripMenuItem.Name = "hideAmbiBackToolStripMenuItem";
            this.hideAmbiBackToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.hideAmbiBackToolStripMenuItem.Text = "Hide AmbiBack";
            this.hideAmbiBackToolStripMenuItem.Click += new System.EventHandler(this.hideAmbiBackToolStripMenuItem_Click);
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(153, 6);
            // 
            // saveSettingsToolStripMenuItem
            // 
            this.saveSettingsToolStripMenuItem.Image = global::AmbiBack.Properties.Resources.save_as;
            this.saveSettingsToolStripMenuItem.Name = "saveSettingsToolStripMenuItem";
            this.saveSettingsToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.saveSettingsToolStripMenuItem.Text = "Save Settings";
            this.saveSettingsToolStripMenuItem.Click += new System.EventHandler(this.saveSettingsToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Image = global::AmbiBack.Properties.Resources.exit;
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // devicesToolStripMenuItem
            // 
            this.devicesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addDeviceToolStripMenuItem,
            this.deleteToolStripMenuItem,
            this.toolStripMenuItem2,
            this.viewDetailsToolStripMenuItem});
            this.devicesToolStripMenuItem.Name = "devicesToolStripMenuItem";
            this.devicesToolStripMenuItem.Size = new System.Drawing.Size(54, 20);
            this.devicesToolStripMenuItem.Text = "Device";
            // 
            // addDeviceToolStripMenuItem
            // 
            this.addDeviceToolStripMenuItem.Name = "addDeviceToolStripMenuItem";
            this.addDeviceToolStripMenuItem.Size = new System.Drawing.Size(137, 22);
            this.addDeviceToolStripMenuItem.Text = "Add Device";
            this.addDeviceToolStripMenuItem.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // deleteToolStripMenuItem
            // 
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.Size = new System.Drawing.Size(137, 22);
            this.deleteToolStripMenuItem.Text = "Delete";
            this.deleteToolStripMenuItem.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(134, 6);
            // 
            // viewDetailsToolStripMenuItem
            // 
            this.viewDetailsToolStripMenuItem.Name = "viewDetailsToolStripMenuItem";
            this.viewDetailsToolStripMenuItem.Size = new System.Drawing.Size(137, 22);
            this.viewDetailsToolStripMenuItem.Text = "View Details";
            this.viewDetailsToolStripMenuItem.Click += new System.EventHandler(this.viewDetailsToolStripMenuItem_Click);
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.connectHueBridgeToolStripMenuItem,
            this.testVideosToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(46, 20);
            this.toolsToolStripMenuItem.Text = "Tools";
            // 
            // connectHueBridgeToolStripMenuItem
            // 
            this.connectHueBridgeToolStripMenuItem.Image = global::AmbiBack.Properties.Resources.devicesBridgesV2;
            this.connectHueBridgeToolStripMenuItem.Name = "connectHueBridgeToolStripMenuItem";
            this.connectHueBridgeToolStripMenuItem.Size = new System.Drawing.Size(267, 22);
            this.connectHueBridgeToolStripMenuItem.Text = "Connect Hue Bridge";
            this.connectHueBridgeToolStripMenuItem.Click += new System.EventHandler(this.connectHueBridgeToolStripMenuItem_Click);
            // 
            // testVideosToolStripMenuItem
            // 
            this.testVideosToolStripMenuItem.Image = global::AmbiBack.Properties.Resources.movie_run;
            this.testVideosToolStripMenuItem.Name = "testVideosToolStripMenuItem";
            this.testVideosToolStripMenuItem.Size = new System.Drawing.Size(267, 22);
            this.testVideosToolStripMenuItem.Text = "Ambient Calibration && Demo Videos";
            this.testVideosToolStripMenuItem.Click += new System.EventHandler(this.testVideosToolStripMenuItem_Click);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuDetails,
            this.mnuList,
            this.mnuTile,
            this.mnuSmallIcons,
            this.mnuLargeIcons});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "View";
            // 
            // mnuDetails
            // 
            this.mnuDetails.Name = "mnuDetails";
            this.mnuDetails.Size = new System.Drawing.Size(134, 22);
            this.mnuDetails.Text = "Details";
            this.mnuDetails.Click += new System.EventHandler(this.mnuDetails_Click);
            // 
            // mnuList
            // 
            this.mnuList.Name = "mnuList";
            this.mnuList.Size = new System.Drawing.Size(134, 22);
            this.mnuList.Text = "List";
            this.mnuList.Click += new System.EventHandler(this.mnuList_Click);
            // 
            // mnuTile
            // 
            this.mnuTile.Name = "mnuTile";
            this.mnuTile.Size = new System.Drawing.Size(134, 22);
            this.mnuTile.Text = "Tile";
            this.mnuTile.Click += new System.EventHandler(this.mnuTile_Click);
            // 
            // mnuSmallIcons
            // 
            this.mnuSmallIcons.Name = "mnuSmallIcons";
            this.mnuSmallIcons.Size = new System.Drawing.Size(134, 22);
            this.mnuSmallIcons.Text = "Small Icons";
            this.mnuSmallIcons.Click += new System.EventHandler(this.mnuSmallIcons_Click);
            // 
            // mnuLargeIcons
            // 
            this.mnuLargeIcons.Name = "mnuLargeIcons";
            this.mnuLargeIcons.Size = new System.Drawing.Size(134, 22);
            this.mnuLargeIcons.Text = "LargeIcons";
            this.mnuLargeIcons.Click += new System.EventHandler(this.mnuLargeIcons_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuAbout});
            this.helpToolStripMenuItem.Image = global::AmbiBack.Properties.Resources.help2;
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(60, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // mnuAbout
            // 
            this.mnuAbout.Image = global::AmbiBack.Properties.Resources.information;
            this.mnuAbout.Name = "mnuAbout";
            this.mnuAbout.Size = new System.Drawing.Size(107, 22);
            this.mnuAbout.Text = "About";
            this.mnuAbout.Click += new System.EventHandler(this.mnuAbout_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(12, 20);
            // 
            // nficon
            // 
            this.nficon.ContextMenuStrip = this.contextMenuTaskIcon;
            this.nficon.Icon = ((System.Drawing.Icon)(resources.GetObject("nficon.Icon")));
            this.nficon.Tag = "";
            this.nficon.Text = "AmbiBack PC";
            this.nficon.Visible = true;
            this.nficon.BalloonTipClosed += new System.EventHandler(this.nficon_BalloonTipClosed);
            this.nficon.BalloonTipShown += new System.EventHandler(this.nficon_BalloonTipShown);
            this.nficon.DoubleClick += new System.EventHandler(this.nficon_DoubleClick);
            this.nficon.MouseMove += new System.Windows.Forms.MouseEventHandler(this.nficon_MouseMove);
            // 
            // contextMenuTaskIcon
            // 
            this.contextMenuTaskIcon.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showToolStripMenuItem,
            this.exitFromTaskIcon});
            this.contextMenuTaskIcon.Name = "contextMenuTaskIcon";
            this.contextMenuTaskIcon.Size = new System.Drawing.Size(104, 48);
            // 
            // showToolStripMenuItem
            // 
            this.showToolStripMenuItem.Image = global::AmbiBack.Properties.Resources.navigate_open;
            this.showToolStripMenuItem.Name = "showToolStripMenuItem";
            this.showToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.showToolStripMenuItem.Text = "Show";
            this.showToolStripMenuItem.Click += new System.EventHandler(this.showToolStripMenuItem_Click);
            // 
            // exitFromTaskIcon
            // 
            this.exitFromTaskIcon.Image = global::AmbiBack.Properties.Resources.exit;
            this.exitFromTaskIcon.Name = "exitFromTaskIcon";
            this.exitFromTaskIcon.Size = new System.Drawing.Size(103, 22);
            this.exitFromTaskIcon.Text = "Exit";
            this.exitFromTaskIcon.Click += new System.EventHandler(this.exitFromTaskIcon_Click);
            // 
            // stbMain
            // 
            this.stbMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.lblDebug});
            this.stbMain.Location = new System.Drawing.Point(0, 659);
            this.stbMain.Name = "stbMain";
            this.stbMain.Size = new System.Drawing.Size(944, 22);
            this.stbMain.TabIndex = 5;
            this.stbMain.Text = "Status";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(39, 17);
            this.toolStripStatusLabel1.Text = "Status";
            // 
            // lblDebug
            // 
            this.lblDebug.Name = "lblDebug";
            this.lblDebug.Size = new System.Drawing.Size(12, 17);
            this.lblDebug.Text = "-";
            // 
            // tsDevices
            // 
            this.tsDevices.Dock = System.Windows.Forms.DockStyle.None;
            this.tsDevices.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnChangeView,
            this.toolStripSeparator2,
            this.btnEnumDevices,
            this.btnCleanup,
            this.btnAdd,
            this.toolStripSeparator1,
            this.btnConfig,
            this.btnDelete});
            this.tsDevices.Location = new System.Drawing.Point(3, 0);
            this.tsDevices.Name = "tsDevices";
            this.tsDevices.Size = new System.Drawing.Size(488, 25);
            this.tsDevices.TabIndex = 8;
            this.tsDevices.Text = "toolStrip1";
            // 
            // btnChangeView
            // 
            this.btnChangeView.Image = global::AmbiBack.Properties.Resources.replace2;
            this.btnChangeView.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnChangeView.Name = "btnChangeView";
            this.btnChangeView.Size = new System.Drawing.Size(90, 22);
            this.btnChangeView.Text = "Toggle View";
            this.btnChangeView.Click += new System.EventHandler(this.btnChangeView_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // btnEnumDevices
            // 
            this.btnEnumDevices.Image = global::AmbiBack.Properties.Resources.view_add;
            this.btnEnumDevices.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnEnumDevices.Name = "btnEnumDevices";
            this.btnEnumDevices.Size = new System.Drawing.Size(93, 22);
            this.btnEnumDevices.Text = "Find Devices";
            this.btnEnumDevices.Click += new System.EventHandler(this.btnEnumDevices_Click);
            // 
            // btnCleanup
            // 
            this.btnCleanup.Image = ((System.Drawing.Image)(resources.GetObject("btnCleanup.Image")));
            this.btnCleanup.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnCleanup.Name = "btnCleanup";
            this.btnCleanup.Size = new System.Drawing.Size(71, 22);
            this.btnCleanup.Text = "Cleanup";
            this.btnCleanup.Click += new System.EventHandler(this.btnCleanup_Click);
            // 
            // btnAdd
            // 
            this.btnAdd.Image = ((System.Drawing.Image)(resources.GetObject("btnAdd.Image")));
            this.btnAdd.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(49, 22);
            this.btnAdd.Text = "Add";
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // btnConfig
            // 
            this.btnConfig.Image = global::AmbiBack.Properties.Resources.preferences;
            this.btnConfig.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnConfig.Name = "btnConfig";
            this.btnConfig.Size = new System.Drawing.Size(101, 22);
            this.btnConfig.Text = "Configuration";
            // 
            // btnDelete
            // 
            this.btnDelete.Image = ((System.Drawing.Image)(resources.GetObject("btnDelete.Image")));
            this.btnDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(60, 22);
            this.btnDelete.Text = "Delete";
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // tcMain
            // 
            this.tcMain.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tcMain.Controls.Add(this.tabMain);
            this.tcMain.Controls.Add(this.tabAdvanced);
            this.tcMain.Controls.Add(this.tabExpert);
            this.tcMain.Controls.Add(this.tabDebug);
            this.tcMain.Location = new System.Drawing.Point(3, 295);
            this.tcMain.Name = "tcMain";
            this.tcMain.SelectedIndex = 0;
            this.tcMain.Size = new System.Drawing.Size(573, 312);
            this.tcMain.TabIndex = 9;
            // 
            // tabMain
            // 
            this.tabMain.AutoScroll = true;
            this.tabMain.Controls.Add(this.tbValueGain);
            this.tabMain.Controls.Add(this.lblValueGain);
            this.tabMain.Controls.Add(this.tbBlue);
            this.tabMain.Controls.Add(this.lblBlue);
            this.tabMain.Controls.Add(this.tbGreen);
            this.tabMain.Controls.Add(this.label4);
            this.tabMain.Controls.Add(this.tbRed);
            this.tabMain.Controls.Add(this.lblRed);
            this.tabMain.Controls.Add(this.tbMinimumLuminosity);
            this.tabMain.Controls.Add(this.lblMinimumLuminosity);
            this.tabMain.Controls.Add(this.tbMaxBrightness);
            this.tabMain.Controls.Add(this.lblMaxBrightness);
            this.tabMain.Controls.Add(this.tbColorBoost);
            this.tabMain.Controls.Add(this.lblColorBoost);
            this.tabMain.Location = new System.Drawing.Point(4, 22);
            this.tabMain.Name = "tabMain";
            this.tabMain.Size = new System.Drawing.Size(565, 286);
            this.tabMain.TabIndex = 2;
            this.tabMain.Text = "Color Options";
            // 
            // tbValueGain
            // 
            this.tbValueGain.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbValueGain.AutoSize = false;
            this.tbValueGain.LargeChange = 10;
            this.tbValueGain.Location = new System.Drawing.Point(177, 175);
            this.tbValueGain.Maximum = 300;
            this.tbValueGain.Minimum = 100;
            this.tbValueGain.Name = "tbValueGain";
            this.tbValueGain.Size = new System.Drawing.Size(383, 26);
            this.tbValueGain.SmallChange = 5;
            this.tbValueGain.TabIndex = 38;
            this.tbValueGain.TickFrequency = 10;
            this.tbValueGain.Value = 100;
            this.tbValueGain.ValueChanged += new System.EventHandler(this.tbValueGain_ValueChanged);
            // 
            // lblValueGain
            // 
            this.lblValueGain.Image = global::AmbiBack.Properties.Resources.contrast16;
            this.lblValueGain.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblValueGain.Location = new System.Drawing.Point(13, 175);
            this.lblValueGain.Name = "lblValueGain";
            this.lblValueGain.Size = new System.Drawing.Size(158, 26);
            this.lblValueGain.TabIndex = 37;
            this.lblValueGain.Text = "Value gain: ";
            this.lblValueGain.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tbBlue
            // 
            this.tbBlue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbBlue.AutoSize = false;
            this.tbBlue.Location = new System.Drawing.Point(177, 94);
            this.tbBlue.Maximum = 100;
            this.tbBlue.Name = "tbBlue";
            this.tbBlue.Size = new System.Drawing.Size(383, 26);
            this.tbBlue.SmallChange = 5;
            this.tbBlue.TabIndex = 36;
            this.tbBlue.TickFrequency = 5;
            this.tbBlue.ValueChanged += new System.EventHandler(this.tbBlue_ValueChanged);
            // 
            // lblBlue
            // 
            this.lblBlue.Image = global::AmbiBack.Properties.Resources.nav_plain_blue;
            this.lblBlue.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblBlue.Location = new System.Drawing.Point(13, 94);
            this.lblBlue.Name = "lblBlue";
            this.lblBlue.Size = new System.Drawing.Size(158, 26);
            this.lblBlue.TabIndex = 35;
            this.lblBlue.Text = "Blue: ";
            this.lblBlue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tbGreen
            // 
            this.tbGreen.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbGreen.AutoSize = false;
            this.tbGreen.Location = new System.Drawing.Point(177, 62);
            this.tbGreen.Maximum = 100;
            this.tbGreen.Name = "tbGreen";
            this.tbGreen.Size = new System.Drawing.Size(383, 26);
            this.tbGreen.SmallChange = 5;
            this.tbGreen.TabIndex = 34;
            this.tbGreen.TickFrequency = 5;
            this.tbGreen.ValueChanged += new System.EventHandler(this.tbGreen_ValueChanged);
            // 
            // label4
            // 
            this.label4.Image = global::AmbiBack.Properties.Resources.nav_plain_green;
            this.label4.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.label4.Location = new System.Drawing.Point(13, 62);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(158, 26);
            this.label4.TabIndex = 33;
            this.label4.Text = "Green: ";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tbRed
            // 
            this.tbRed.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbRed.AutoSize = false;
            this.tbRed.Location = new System.Drawing.Point(177, 30);
            this.tbRed.Maximum = 100;
            this.tbRed.Name = "tbRed";
            this.tbRed.Size = new System.Drawing.Size(383, 26);
            this.tbRed.SmallChange = 5;
            this.tbRed.TabIndex = 32;
            this.tbRed.TickFrequency = 5;
            this.tbRed.ValueChanged += new System.EventHandler(this.tbRed_ValueChanged);
            // 
            // lblRed
            // 
            this.lblRed.Image = global::AmbiBack.Properties.Resources.nav_plain_red;
            this.lblRed.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblRed.Location = new System.Drawing.Point(13, 30);
            this.lblRed.Name = "lblRed";
            this.lblRed.Size = new System.Drawing.Size(158, 26);
            this.lblRed.TabIndex = 31;
            this.lblRed.Text = "Red:";
            this.lblRed.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tbMinimumLuminosity
            // 
            this.tbMinimumLuminosity.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbMinimumLuminosity.AutoSize = false;
            this.tbMinimumLuminosity.Location = new System.Drawing.Point(177, 249);
            this.tbMinimumLuminosity.Maximum = 100;
            this.tbMinimumLuminosity.Name = "tbMinimumLuminosity";
            this.tbMinimumLuminosity.Size = new System.Drawing.Size(383, 26);
            this.tbMinimumLuminosity.SmallChange = 5;
            this.tbMinimumLuminosity.TabIndex = 30;
            this.tbMinimumLuminosity.TickFrequency = 5;
            this.tbMinimumLuminosity.ValueChanged += new System.EventHandler(this.tbMinimumLuminosity_ValueChanged);
            // 
            // lblMinimumLuminosity
            // 
            this.lblMinimumLuminosity.Image = global::AmbiBack.Properties.Resources.luminosity16;
            this.lblMinimumLuminosity.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblMinimumLuminosity.Location = new System.Drawing.Point(13, 249);
            this.lblMinimumLuminosity.Name = "lblMinimumLuminosity";
            this.lblMinimumLuminosity.Size = new System.Drawing.Size(158, 26);
            this.lblMinimumLuminosity.TabIndex = 29;
            this.lblMinimumLuminosity.Text = "Minimum Luminosity:";
            this.lblMinimumLuminosity.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tbMaxBrightness
            // 
            this.tbMaxBrightness.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbMaxBrightness.AutoSize = false;
            this.tbMaxBrightness.Location = new System.Drawing.Point(177, 217);
            this.tbMaxBrightness.Maximum = 100;
            this.tbMaxBrightness.Name = "tbMaxBrightness";
            this.tbMaxBrightness.Size = new System.Drawing.Size(383, 26);
            this.tbMaxBrightness.SmallChange = 5;
            this.tbMaxBrightness.TabIndex = 28;
            this.tbMaxBrightness.TickFrequency = 5;
            this.tbMaxBrightness.ValueChanged += new System.EventHandler(this.tbMaxBrightness_ValueChanged);
            // 
            // lblMaxBrightness
            // 
            this.lblMaxBrightness.Image = global::AmbiBack.Properties.Resources.brightness16;
            this.lblMaxBrightness.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblMaxBrightness.Location = new System.Drawing.Point(13, 217);
            this.lblMaxBrightness.Name = "lblMaxBrightness";
            this.lblMaxBrightness.Size = new System.Drawing.Size(158, 26);
            this.lblMaxBrightness.TabIndex = 27;
            this.lblMaxBrightness.Text = "Max Brightness: ";
            this.lblMaxBrightness.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tbColorBoost
            // 
            this.tbColorBoost.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbColorBoost.AutoSize = false;
            this.tbColorBoost.Location = new System.Drawing.Point(177, 143);
            this.tbColorBoost.Maximum = 100;
            this.tbColorBoost.Name = "tbColorBoost";
            this.tbColorBoost.Size = new System.Drawing.Size(383, 26);
            this.tbColorBoost.SmallChange = 5;
            this.tbColorBoost.TabIndex = 26;
            this.tbColorBoost.TickFrequency = 5;
            this.tbColorBoost.ValueChanged += new System.EventHandler(this.tbColorBoost_ValueChanged);
            // 
            // lblColorBoost
            // 
            this.lblColorBoost.Image = global::AmbiBack.Properties.Resources.colorboost16;
            this.lblColorBoost.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblColorBoost.Location = new System.Drawing.Point(13, 143);
            this.lblColorBoost.Name = "lblColorBoost";
            this.lblColorBoost.Size = new System.Drawing.Size(158, 26);
            this.lblColorBoost.TabIndex = 25;
            this.lblColorBoost.Text = "Color boost: ";
            this.lblColorBoost.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tabAdvanced
            // 
            this.tabAdvanced.BackColor = System.Drawing.SystemColors.Control;
            this.tabAdvanced.Controls.Add(this.cbConnectionType);
            this.tabAdvanced.Controls.Add(this.lblConnectionType);
            this.tabAdvanced.Controls.Add(this.txtDeviceName);
            this.tabAdvanced.Controls.Add(this.label2);
            this.tabAdvanced.Controls.Add(this.tbSmoothing);
            this.tabAdvanced.Controls.Add(this.label1);
            this.tabAdvanced.Controls.Add(this.tbFramesPerSecond);
            this.tabAdvanced.Controls.Add(this.lblFrameRate);
            this.tabAdvanced.Controls.Add(this.tbInnerPicture);
            this.tabAdvanced.Controls.Add(this.label3);
            this.tabAdvanced.Location = new System.Drawing.Point(4, 22);
            this.tabAdvanced.Name = "tabAdvanced";
            this.tabAdvanced.Padding = new System.Windows.Forms.Padding(3);
            this.tabAdvanced.Size = new System.Drawing.Size(565, 286);
            this.tabAdvanced.TabIndex = 4;
            this.tabAdvanced.Text = "Advanced";
            // 
            // cbConnectionType
            // 
            this.cbConnectionType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbConnectionType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbConnectionType.FormattingEnabled = true;
            this.cbConnectionType.Items.AddRange(new object[] {
            "TCP (Good WIFI Only)",
            "UDP (All WIFI)",
            "USB (Serial)"});
            this.cbConnectionType.Location = new System.Drawing.Point(176, 150);
            this.cbConnectionType.Name = "cbConnectionType";
            this.cbConnectionType.Size = new System.Drawing.Size(374, 21);
            this.cbConnectionType.TabIndex = 30;
            this.cbConnectionType.SelectedValueChanged += new System.EventHandler(this.cbConnectionType_SelectedValueChanged);
            // 
            // lblConnectionType
            // 
            this.lblConnectionType.Location = new System.Drawing.Point(11, 150);
            this.lblConnectionType.Name = "lblConnectionType";
            this.lblConnectionType.Size = new System.Drawing.Size(159, 26);
            this.lblConnectionType.TabIndex = 29;
            this.lblConnectionType.Text = "Connection Type";
            this.lblConnectionType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtDeviceName
            // 
            this.txtDeviceName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDeviceName.Location = new System.Drawing.Point(176, 21);
            this.txtDeviceName.Name = "txtDeviceName";
            this.txtDeviceName.Size = new System.Drawing.Size(374, 20);
            this.txtDeviceName.TabIndex = 28;
            this.txtDeviceName.TextChanged += new System.EventHandler(this.txtDeviceName_TextChanged);
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(11, 17);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(159, 26);
            this.label2.TabIndex = 27;
            this.label2.Text = "Device Name";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tbSmoothing
            // 
            this.tbSmoothing.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbSmoothing.AutoSize = false;
            this.tbSmoothing.LargeChange = 10;
            this.tbSmoothing.Location = new System.Drawing.Point(176, 110);
            this.tbSmoothing.Maximum = 1000;
            this.tbSmoothing.Minimum = 10;
            this.tbSmoothing.Name = "tbSmoothing";
            this.tbSmoothing.Size = new System.Drawing.Size(374, 26);
            this.tbSmoothing.SmallChange = 10;
            this.tbSmoothing.TabIndex = 26;
            this.tbSmoothing.TickFrequency = 5;
            this.tbSmoothing.Value = 60;
            this.tbSmoothing.ValueChanged += new System.EventHandler(this.tbSmoothing_ValueChanged);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(11, 110);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(159, 26);
            this.label1.TabIndex = 25;
            this.label1.Text = "Smoothing: ";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tbFramesPerSecond
            // 
            this.tbFramesPerSecond.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbFramesPerSecond.AutoSize = false;
            this.tbFramesPerSecond.LargeChange = 10;
            this.tbFramesPerSecond.Location = new System.Drawing.Point(176, 78);
            this.tbFramesPerSecond.Maximum = 60;
            this.tbFramesPerSecond.Minimum = 1;
            this.tbFramesPerSecond.Name = "tbFramesPerSecond";
            this.tbFramesPerSecond.Size = new System.Drawing.Size(374, 26);
            this.tbFramesPerSecond.SmallChange = 2;
            this.tbFramesPerSecond.TabIndex = 24;
            this.tbFramesPerSecond.TickFrequency = 5;
            this.tbFramesPerSecond.Value = 30;
            this.tbFramesPerSecond.ValueChanged += new System.EventHandler(this.tbFramesPerSecond_ValueChanged);
            // 
            // lblFrameRate
            // 
            this.lblFrameRate.Location = new System.Drawing.Point(11, 78);
            this.lblFrameRate.Name = "lblFrameRate";
            this.lblFrameRate.Size = new System.Drawing.Size(159, 26);
            this.lblFrameRate.TabIndex = 23;
            this.lblFrameRate.Text = "Framerate to send: ";
            this.lblFrameRate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tbInnerPicture
            // 
            this.tbInnerPicture.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbInnerPicture.AutoSize = false;
            this.tbInnerPicture.Location = new System.Drawing.Point(176, 46);
            this.tbInnerPicture.Maximum = 50;
            this.tbInnerPicture.Minimum = 5;
            this.tbInnerPicture.Name = "tbInnerPicture";
            this.tbInnerPicture.Size = new System.Drawing.Size(374, 26);
            this.tbInnerPicture.SmallChange = 2;
            this.tbInnerPicture.TabIndex = 22;
            this.tbInnerPicture.TickFrequency = 5;
            this.tbInnerPicture.Value = 10;
            this.tbInnerPicture.ValueChanged += new System.EventHandler(this.tbInnerPicture_ValueChanged);
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(11, 46);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(159, 26);
            this.label3.TabIndex = 21;
            this.label3.Text = "Inner Picture Usage (Percent):";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tabExpert
            // 
            this.tabExpert.Controls.Add(this.cmdStopDebugging);
            this.tabExpert.Controls.Add(this.txtStartDebug);
            this.tabExpert.Controls.Add(this.label5);
            this.tabExpert.Controls.Add(this.lblDebugSyslog);
            this.tabExpert.Controls.Add(this.txtDebugSyslogPort);
            this.tabExpert.Controls.Add(this.txtDebugSyslogServer);
            this.tabExpert.Location = new System.Drawing.Point(4, 22);
            this.tabExpert.Name = "tabExpert";
            this.tabExpert.Padding = new System.Windows.Forms.Padding(3);
            this.tabExpert.Size = new System.Drawing.Size(565, 286);
            this.tabExpert.TabIndex = 5;
            this.tabExpert.Text = "Expert";
            this.tabExpert.UseVisualStyleBackColor = true;
            // 
            // cmdStopDebugging
            // 
            this.cmdStopDebugging.Location = new System.Drawing.Point(294, 68);
            this.cmdStopDebugging.Name = "cmdStopDebugging";
            this.cmdStopDebugging.Size = new System.Drawing.Size(113, 27);
            this.cmdStopDebugging.TabIndex = 34;
            this.cmdStopDebugging.Text = "Stop Debugging";
            this.cmdStopDebugging.UseVisualStyleBackColor = true;
            this.cmdStopDebugging.Click += new System.EventHandler(this.cmdStopDebug_Click);
            // 
            // txtStartDebug
            // 
            this.txtStartDebug.Location = new System.Drawing.Point(175, 68);
            this.txtStartDebug.Name = "txtStartDebug";
            this.txtStartDebug.Size = new System.Drawing.Size(113, 27);
            this.txtStartDebug.TabIndex = 33;
            this.txtStartDebug.Text = "Start Debugging";
            this.txtStartDebug.UseVisualStyleBackColor = true;
            this.txtStartDebug.Click += new System.EventHandler(this.txtStartDebug_Click);
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(10, 38);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(159, 26);
            this.label5.TabIndex = 31;
            this.label5.Text = "Debug Syslog Port";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDebugSyslog
            // 
            this.lblDebugSyslog.Location = new System.Drawing.Point(10, 12);
            this.lblDebugSyslog.Name = "lblDebugSyslog";
            this.lblDebugSyslog.Size = new System.Drawing.Size(159, 26);
            this.lblDebugSyslog.TabIndex = 29;
            this.lblDebugSyslog.Text = "Debug Syslog Server";
            this.lblDebugSyslog.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtDebugSyslogPort
            // 
            this.txtDebugSyslogPort.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::AmbiBack.Properties.Settings.Default, "DebugSyslogPort", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.txtDebugSyslogPort.Location = new System.Drawing.Point(175, 42);
            this.txtDebugSyslogPort.Name = "txtDebugSyslogPort";
            this.txtDebugSyslogPort.Size = new System.Drawing.Size(410, 20);
            this.txtDebugSyslogPort.TabIndex = 32;
            this.txtDebugSyslogPort.Text = global::AmbiBack.Properties.Settings.Default.DebugSyslogPort;
            // 
            // txtDebugSyslogServer
            // 
            this.txtDebugSyslogServer.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::AmbiBack.Properties.Settings.Default, "DebugSyslogServer", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.txtDebugSyslogServer.Location = new System.Drawing.Point(175, 16);
            this.txtDebugSyslogServer.Name = "txtDebugSyslogServer";
            this.txtDebugSyslogServer.Size = new System.Drawing.Size(410, 20);
            this.txtDebugSyslogServer.TabIndex = 30;
            this.txtDebugSyslogServer.Text = global::AmbiBack.Properties.Settings.Default.DebugSyslogServer;
            // 
            // tabDebug
            // 
            this.tabDebug.Controls.Add(this.gbDebug);
            this.tabDebug.Location = new System.Drawing.Point(4, 22);
            this.tabDebug.Name = "tabDebug";
            this.tabDebug.Padding = new System.Windows.Forms.Padding(3);
            this.tabDebug.Size = new System.Drawing.Size(565, 286);
            this.tabDebug.TabIndex = 3;
            this.tabDebug.Text = "Debug";
            this.tabDebug.UseVisualStyleBackColor = true;
            // 
            // gbDebug
            // 
            this.gbDebug.Controls.Add(this.cmdClearDebug);
            this.gbDebug.Controls.Add(this.chkShowDebugMessages);
            this.gbDebug.Controls.Add(this.cmdGetInfo);
            this.gbDebug.Controls.Add(this.cmdReboot);
            this.gbDebug.Controls.Add(this.txtDebug);
            this.gbDebug.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbDebug.Location = new System.Drawing.Point(3, 3);
            this.gbDebug.Name = "gbDebug";
            this.gbDebug.Size = new System.Drawing.Size(559, 280);
            this.gbDebug.TabIndex = 23;
            this.gbDebug.TabStop = false;
            this.gbDebug.Text = "Debug Output";
            // 
            // chkShowDebugMessages
            // 
            this.chkShowDebugMessages.AutoSize = true;
            this.chkShowDebugMessages.Location = new System.Drawing.Point(13, 23);
            this.chkShowDebugMessages.Name = "chkShowDebugMessages";
            this.chkShowDebugMessages.Size = new System.Drawing.Size(174, 17);
            this.chkShowDebugMessages.TabIndex = 28;
            this.chkShowDebugMessages.Text = "Show Debug Messages (Serial)";
            this.chkShowDebugMessages.UseVisualStyleBackColor = true;
            this.chkShowDebugMessages.CheckedChanged += new System.EventHandler(this.chkShowDebugMessages_CheckedChanged);
            // 
            // cmdGetInfo
            // 
            this.cmdGetInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdGetInfo.Location = new System.Drawing.Point(478, 19);
            this.cmdGetInfo.Name = "cmdGetInfo";
            this.cmdGetInfo.Size = new System.Drawing.Size(75, 23);
            this.cmdGetInfo.TabIndex = 25;
            this.cmdGetInfo.Text = "Get Info";
            this.cmdGetInfo.UseVisualStyleBackColor = true;
            this.cmdGetInfo.Click += new System.EventHandler(this.cmdGetInfo_Click);
            // 
            // cmdReboot
            // 
            this.cmdReboot.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdReboot.Location = new System.Drawing.Point(397, 19);
            this.cmdReboot.Name = "cmdReboot";
            this.cmdReboot.Size = new System.Drawing.Size(75, 23);
            this.cmdReboot.TabIndex = 23;
            this.cmdReboot.Text = "Reboot";
            this.cmdReboot.UseVisualStyleBackColor = true;
            this.cmdReboot.Click += new System.EventHandler(this.cmdReboot_Click);
            // 
            // txtDebug
            // 
            this.txtDebug.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDebug.Location = new System.Drawing.Point(13, 48);
            this.txtDebug.Multiline = true;
            this.txtDebug.Name = "txtDebug";
            this.txtDebug.ReadOnly = true;
            this.txtDebug.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDebug.Size = new System.Drawing.Size(540, 226);
            this.txtDebug.TabIndex = 22;
            // 
            // LVDevices
            // 
            this.LVDevices.Activation = System.Windows.Forms.ItemActivation.OneClick;
            this.LVDevices.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LVDevices.CheckBoxes = true;
            this.LVDevices.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chDeviceType,
            this.chDeviceName,
            this.chDeviceIP,
            this.chDeviceStatus});
            this.LVDevices.ContextMenuStrip = this.contextMenuDevices;
            this.LVDevices.FullRowSelect = true;
            this.LVDevices.GridLines = true;
            this.LVDevices.HideSelection = false;
            this.LVDevices.LargeImageList = this.ILDevicesLarge;
            this.LVDevices.Location = new System.Drawing.Point(2, 3);
            this.LVDevices.MultiSelect = false;
            this.LVDevices.Name = "LVDevices";
            this.LVDevices.Size = new System.Drawing.Size(574, 286);
            this.LVDevices.SmallImageList = this.ILDevicesSmall;
            this.LVDevices.TabIndex = 0;
            this.LVDevices.UseCompatibleStateImageBehavior = false;
            this.LVDevices.View = System.Windows.Forms.View.Details;
            this.LVDevices.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.LVDevices_ItemChecked);
            this.LVDevices.SelectedIndexChanged += new System.EventHandler(this.LVDevices_SelectedIndexChanged);
            this.LVDevices.DoubleClick += new System.EventHandler(this.LVDevices_DoubleClick);
            this.LVDevices.MouseClick += new System.Windows.Forms.MouseEventHandler(this.LVDevices_MouseClick);
            // 
            // chDeviceType
            // 
            this.chDeviceType.Text = "Device Type";
            this.chDeviceType.Width = 200;
            // 
            // chDeviceName
            // 
            this.chDeviceName.Text = "Device Name";
            this.chDeviceName.Width = 150;
            // 
            // chDeviceIP
            // 
            this.chDeviceIP.Text = "IP Address";
            this.chDeviceIP.Width = 100;
            // 
            // chDeviceStatus
            // 
            this.chDeviceStatus.Text = "Device Status";
            this.chDeviceStatus.Width = 100;
            // 
            // contextMenuDevices
            // 
            this.contextMenuDevices.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItemShowDetails,
            this.connectWifiToolStripMenuItem,
            this.toolStripMenuItem4,
            this.deleteToolStripMenuItem1});
            this.contextMenuDevices.Name = "contextMenuDevices";
            this.contextMenuDevices.Size = new System.Drawing.Size(144, 76);
            // 
            // toolStripMenuItemShowDetails
            // 
            this.toolStripMenuItemShowDetails.Image = global::AmbiBack.Properties.Resources.preferences;
            this.toolStripMenuItemShowDetails.Name = "toolStripMenuItemShowDetails";
            this.toolStripMenuItemShowDetails.Size = new System.Drawing.Size(143, 22);
            this.toolStripMenuItemShowDetails.Text = "Show Details";
            this.toolStripMenuItemShowDetails.Click += new System.EventHandler(this.viewDetailsToolStripMenuItem_Click);
            // 
            // connectWifiToolStripMenuItem
            // 
            this.connectWifiToolStripMenuItem.Image = global::AmbiBack.Properties.Resources.earth_network;
            this.connectWifiToolStripMenuItem.Name = "connectWifiToolStripMenuItem";
            this.connectWifiToolStripMenuItem.Size = new System.Drawing.Size(143, 22);
            this.connectWifiToolStripMenuItem.Text = "Wifi Connect";
            this.connectWifiToolStripMenuItem.Click += new System.EventHandler(this.connectWifiToolStripMenuItem_Click);
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            this.toolStripMenuItem4.Size = new System.Drawing.Size(140, 6);
            // 
            // deleteToolStripMenuItem1
            // 
            this.deleteToolStripMenuItem1.Image = global::AmbiBack.Properties.Resources.delete2;
            this.deleteToolStripMenuItem1.Name = "deleteToolStripMenuItem1";
            this.deleteToolStripMenuItem1.Size = new System.Drawing.Size(143, 22);
            this.deleteToolStripMenuItem1.Text = "Delete";
            this.deleteToolStripMenuItem1.Click += new System.EventHandler(this.deleteToolStripMenuItem1_Click);
            // 
            // ILDevicesLarge
            // 
            this.ILDevicesLarge.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ILDevicesLarge.ImageStream")));
            this.ILDevicesLarge.TransparentColor = System.Drawing.Color.Transparent;
            this.ILDevicesLarge.Images.SetKeyName(0, "DreamScreen4K");
            this.ILDevicesLarge.Images.SetKeyName(1, "DreamScreenHD");
            this.ILDevicesLarge.Images.SetKeyName(2, "DreamScreenSideKick");
            this.ILDevicesLarge.Images.SetKeyName(3, "AmbiBackController");
            this.ILDevicesLarge.Images.SetKeyName(4, "ambiback");
            this.ILDevicesLarge.Images.SetKeyName(5, "ambiback_bridge");
            this.ILDevicesLarge.Images.SetKeyName(6, "ambiback_cam");
            this.ILDevicesLarge.Images.SetKeyName(7, "ambiback_pc");
            this.ILDevicesLarge.Images.SetKeyName(8, "PhilipsHueColorLight");
            // 
            // ILDevicesSmall
            // 
            this.ILDevicesSmall.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ILDevicesSmall.ImageStream")));
            this.ILDevicesSmall.TransparentColor = System.Drawing.Color.Transparent;
            this.ILDevicesSmall.Images.SetKeyName(0, "DreamScreen4K");
            this.ILDevicesSmall.Images.SetKeyName(1, "DreamScreenHD");
            this.ILDevicesSmall.Images.SetKeyName(2, "DreamScreenSideKick");
            this.ILDevicesSmall.Images.SetKeyName(3, "AmbiBackController");
            this.ILDevicesSmall.Images.SetKeyName(4, "PhilipsHueColorLight");
            this.ILDevicesSmall.Images.SetKeyName(5, "ambiback");
            this.ILDevicesSmall.Images.SetKeyName(6, "ambiback_bridge");
            this.ILDevicesSmall.Images.SetKeyName(7, "ambiback_cam");
            this.ILDevicesSmall.Images.SetKeyName(8, "ambiback_pc");
            // 
            // ILState
            // 
            this.ILState.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ILState.ImageStream")));
            this.ILState.TransparentColor = System.Drawing.Color.Transparent;
            this.ILState.Images.SetKeyName(0, "running");
            this.ILState.Images.SetKeyName(1, "stopped_offline");
            this.ILState.Images.SetKeyName(2, "stopped_online");
            this.ILState.Images.SetKeyName(3, "disabled");
            this.ILState.Images.SetKeyName(4, "unknown");
            // 
            // TimerCheck
            // 
            this.TimerCheck.Interval = 1000;
            this.TimerCheck.Tick += new System.EventHandler(this.TimerCheck_Tick);
            // 
            // panScreens
            // 
            this.panScreens.AutoScroll = true;
            this.panScreens.Dock = System.Windows.Forms.DockStyle.Left;
            this.panScreens.Location = new System.Drawing.Point(0, 24);
            this.panScreens.Name = "panScreens";
            this.panScreens.Size = new System.Drawing.Size(360, 635);
            this.panScreens.TabIndex = 11;
            // 
            // splitMain
            // 
            this.splitMain.Location = new System.Drawing.Point(360, 24);
            this.splitMain.MinSize = 360;
            this.splitMain.Name = "splitMain";
            this.splitMain.Size = new System.Drawing.Size(5, 635);
            this.splitMain.TabIndex = 13;
            this.splitMain.TabStop = false;
            // 
            // tsConMain
            // 
            // 
            // tsConMain.ContentPanel
            // 
            this.tsConMain.ContentPanel.Controls.Add(this.tcMain);
            this.tsConMain.ContentPanel.Controls.Add(this.LVDevices);
            this.tsConMain.ContentPanel.Size = new System.Drawing.Size(579, 610);
            this.tsConMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tsConMain.Location = new System.Drawing.Point(365, 24);
            this.tsConMain.Name = "tsConMain";
            this.tsConMain.Size = new System.Drawing.Size(579, 635);
            this.tsConMain.TabIndex = 0;
            this.tsConMain.Text = "toolStripContainer1";
            // 
            // tsConMain.TopToolStripPanel
            // 
            this.tsConMain.TopToolStripPanel.Controls.Add(this.tsDevices);
            // 
            // TimerTaskIcon
            // 
            this.TimerTaskIcon.Interval = 2500;
            this.TimerTaskIcon.Tick += new System.EventHandler(this.TimerTaskIcon_Tick);
            // 
            // timerComPort
            // 
            this.timerComPort.Interval = 1000;
            this.timerComPort.Tick += new System.EventHandler(this.timerComPort_Tick);
            // 
            // cmdClearDebug
            // 
            this.cmdClearDebug.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdClearDebug.Location = new System.Drawing.Point(316, 19);
            this.cmdClearDebug.Name = "cmdClearDebug";
            this.cmdClearDebug.Size = new System.Drawing.Size(75, 23);
            this.cmdClearDebug.TabIndex = 29;
            this.cmdClearDebug.Text = "Clear";
            this.cmdClearDebug.UseVisualStyleBackColor = true;
            this.cmdClearDebug.Click += new System.EventHandler(this.cmdClearDebug_Click);
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(944, 681);
            this.Controls.Add(this.tsConMain);
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.panScreens);
            this.Controls.Add(this.stbMain);
            this.Controls.Add(this.MainMenu);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.MainMenu;
            this.MinimumSize = new System.Drawing.Size(960, 720);
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "AmbiBack PC";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmMain_FormClosed);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.Shown += new System.EventHandler(this.frmMain_Shown);
            this.MainMenu.ResumeLayout(false);
            this.MainMenu.PerformLayout();
            this.contextMenuTaskIcon.ResumeLayout(false);
            this.stbMain.ResumeLayout(false);
            this.stbMain.PerformLayout();
            this.tsDevices.ResumeLayout(false);
            this.tsDevices.PerformLayout();
            this.tcMain.ResumeLayout(false);
            this.tabMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.tbValueGain)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbBlue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbGreen)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbRed)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbMinimumLuminosity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbMaxBrightness)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbColorBoost)).EndInit();
            this.tabAdvanced.ResumeLayout(false);
            this.tabAdvanced.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbSmoothing)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbFramesPerSecond)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbInnerPicture)).EndInit();
            this.tabExpert.ResumeLayout(false);
            this.tabExpert.PerformLayout();
            this.tabDebug.ResumeLayout(false);
            this.gbDebug.ResumeLayout(false);
            this.gbDebug.PerformLayout();
            this.contextMenuDevices.ResumeLayout(false);
            this.tsConMain.ContentPanel.ResumeLayout(false);
            this.tsConMain.TopToolStripPanel.ResumeLayout(false);
            this.tsConMain.TopToolStripPanel.PerformLayout();
            this.tsConMain.ResumeLayout(false);
            this.tsConMain.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.MenuStrip MainMenu;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.NotifyIcon nficon;
        private System.Windows.Forms.StatusStrip stbMain;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripStatusLabel lblDebug;
        private System.Windows.Forms.ToolStrip tsDevices;
        private System.Windows.Forms.ToolStripButton btnChangeView;
        private System.Windows.Forms.ToolStripButton btnEnumDevices;
        private System.Windows.Forms.TabControl tcMain;
        private System.Windows.Forms.Timer TimerCheck;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolTip TTHelper;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mnuAbout;
        private System.Windows.Forms.ListView LVDevices;
        private System.Windows.Forms.ColumnHeader chDeviceType;
        private System.Windows.Forms.ColumnHeader chDeviceName;
        private System.Windows.Forms.ColumnHeader chDeviceIP;
        private System.Windows.Forms.ColumnHeader chDeviceStatus;
        private System.Windows.Forms.ImageList ILDevicesLarge;
        private System.Windows.Forms.ImageList ILDevicesSmall;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mnuDetails;
        private System.Windows.Forms.ToolStripMenuItem mnuList;
        private System.Windows.Forms.ToolStripMenuItem mnuSmallIcons;
        private System.Windows.Forms.ToolStripMenuItem mnuLargeIcons;
        private System.Windows.Forms.ToolStripMenuItem mnuTile;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem devicesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewDetailsToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton btnAdd;
        private System.Windows.Forms.ToolStripButton btnDelete;
        private System.Windows.Forms.ToolStripButton btnCleanup;
        private System.Windows.Forms.Panel panScreens;
        private System.Windows.Forms.TabPage tabDebug;
        private System.Windows.Forms.GroupBox gbDebug;
        private System.Windows.Forms.TextBox txtDebug;
        private System.Windows.Forms.Splitter splitMain;
        private System.Windows.Forms.ToolStripContainer tsConMain;
        private System.Windows.Forms.ImageList ILState;
        private System.Windows.Forms.TabPage tabMain;
        private System.Windows.Forms.TrackBar tbColorBoost;
        private System.Windows.Forms.Label lblColorBoost;
        private System.Windows.Forms.TabPage tabAdvanced;
        private System.Windows.Forms.TrackBar tbFramesPerSecond;
        private System.Windows.Forms.Label lblFrameRate;
        private System.Windows.Forms.TrackBar tbInnerPicture;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TabPage tabExpert;
        private System.Windows.Forms.BindingSource bindingSource1;
        private System.Windows.Forms.TrackBar tbMinimumLuminosity;
        private System.Windows.Forms.Label lblMinimumLuminosity;
        private System.Windows.Forms.TrackBar tbMaxBrightness;
        private System.Windows.Forms.Label lblMaxBrightness;
        private System.Windows.Forms.TrackBar tbRed;
        private System.Windows.Forms.Label lblRed;
        private System.Windows.Forms.TrackBar tbBlue;
        private System.Windows.Forms.Label lblBlue;
        private System.Windows.Forms.TrackBar tbGreen;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TrackBar tbSmoothing;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TrackBar tbValueGain;
        private System.Windows.Forms.Label lblValueGain;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem connectHueBridgeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addDeviceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.TextBox txtDeviceName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtDebugSyslogPort;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtDebugSyslogServer;
        private System.Windows.Forms.Label lblDebugSyslog;
        private System.Windows.Forms.Button cmdStopDebugging;
        private System.Windows.Forms.Button txtStartDebug;
        private System.Windows.Forms.ToolStripMenuItem testVideosToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuDevices;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemShowDetails;
        private System.Windows.Forms.ToolStripMenuItem hideAmbiBackToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
        private System.Windows.Forms.ContextMenuStrip contextMenuTaskIcon;
        private System.Windows.Forms.ToolStripMenuItem exitFromTaskIcon;
        private System.Windows.Forms.ToolStripMenuItem showToolStripMenuItem;
        private System.Windows.Forms.Timer TimerTaskIcon;
        private System.Windows.Forms.Timer timerComPort;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton btnConfig;
        private System.Windows.Forms.ToolStripMenuItem saveSettingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem connectWifiToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem1;
        private System.Windows.Forms.ComboBox cbConnectionType;
        private System.Windows.Forms.Label lblConnectionType;
        private System.Windows.Forms.CheckBox chkShowDebugMessages;
        private System.Windows.Forms.Button cmdGetInfo;
        private System.Windows.Forms.Button cmdReboot;
        private System.Windows.Forms.Button cmdClearDebug;
    }
}

