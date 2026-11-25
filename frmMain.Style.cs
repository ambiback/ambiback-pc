using System;
using System.Drawing;
using System.Windows.Forms;

namespace AmbiBack
{
    /// <summary>Look, texts, Easy / Advanced mode and status display of the main window.</summary>
    public partial class frmMain
    {
        private ToolStripButton btnModeEasy, btnModeAdvanced;
        private ToolStripMenuItem mnuModeEasy, mnuModeAdvanced, mnuFind, mnuWifi, mnuCleanup;
        private ToolStripMenuItem trayStatus, trayStart, trayStop;
        private Label lblDevicesEmpty;

        // App toolbar: status, Hue Bridge and options, mode switch, start / stop all
        private ToolStrip tsApp;
        private ToolStripLabel tbStatus;
        private ToolStripButton tbHue, tbOptions, tbStartStop;
        private ToolStripMenuItem mnuStartAll, mnuStopAll;

        private int Px(int v) { return (int)Math.Round(v * DeviceDpi / 96.0); }

        private void ApplyStyle()
        {
            Theme.ApplyForm(this);
            // The new font scales the window and its minimum size; keep both usable on small screens
            Load += (s, e) => FitToScreen();

            // Menus and strips
            foreach (ToolStrip ts in new ToolStrip[] { MainMenu, tsDevices, stbMain })
                Theme.StyleStrip(ts);
            contextMenuDevices.Renderer = new FlatToolStripRenderer();
            contextMenuTaskIcon.Renderer = new FlatToolStripRenderer();
            tsDevices.Padding = new Padding(Px(6), Px(4), Px(6), Px(4));
            toolStripMenuItem1.Visible = false;   // empty placeholder item
            // Top level menus without icons, as usual on Windows
            foreach (ToolStripItem i in MainMenu.Items)
                i.Image = null;

            BuildAppToolbar();

            // View menu: mode first, list styles only in Advanced
            mnuModeEasy = new ToolStripMenuItem();
            mnuModeAdvanced = new ToolStripMenuItem();
            mnuModeEasy.Click += (s, e) => SetMode(false);
            mnuModeAdvanced.Click += (s, e) => SetMode(true);
            viewToolStripMenuItem.DropDownItems.Insert(0, mnuModeEasy);
            viewToolStripMenuItem.DropDownItems.Insert(1, mnuModeAdvanced);
            viewToolStripMenuItem.DropDownItems.Insert(2, new ToolStripSeparator());

            // Devices menu: every toolbar command is also in the menu
            mnuFind = new ToolStripMenuItem(null, null, btnEnumDevices_Click) { ShortcutKeys = Keys.F5 };
            mnuWifi = new ToolStripMenuItem(null, null, connectWifiToolStripMenuItem_Click);
            mnuCleanup = new ToolStripMenuItem(null, null, btnCleanup_Click);
            devicesToolStripMenuItem.DropDownItems.Insert(0, mnuFind);
            devicesToolStripMenuItem.DropDownItems.Insert(1, new ToolStripSeparator());
            devicesToolStripMenuItem.DropDownItems.Add(mnuWifi);
            devicesToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            devicesToolStripMenuItem.DropDownItems.Add(mnuCleanup);
            devicesToolStripMenuItem.DropDownOpening += (s, e) =>
            {
                bool sel = LVDevices.SelectedItems.Count > 0;
                viewDetailsToolStripMenuItem.Enabled = sel;
                deleteToolStripMenuItem.Enabled = sel;
                mnuWifi.Enabled = sel && currentSelectedDevice != null && currentSelectedDevice.IsSerial();
            };


            // Device toolbar: the same symbol set as the app toolbar
            SetGlyph(btnEnumDevices, Theme.Glyphs.Search, Theme.PrimaryText);
            SetGlyph(btnAdd, Theme.Glyphs.Add, Theme.PrimaryText);
            SetGlyph(btnCleanup, Theme.Glyphs.Broom, Theme.PrimaryText);
            SetGlyph(btnDelete, Theme.Glyphs.Delete, Theme.Destructive);
            SetGlyph(btnChangeView, Theme.Glyphs.View, Theme.PrimaryText);

            // Device toolbar: Delete on the right, in red
            btnDelete.Alignment = ToolStripItemAlignment.Right;
            btnDelete.ForeColor = Theme.Destructive;
            btnConfig.Visible = false;   // same as Device Details, which is reachable by double click and menu

            // Device list
            LVDevices.GridLines = false;
            LVDevices.BorderStyle = BorderStyle.None;
            LVDevices.OwnerDraw = true;
            LVDevices.DrawColumnHeader += (s, e) => e.DrawDefault = true;
            LVDevices.DrawItem += (s, e) => { if (LVDevices.View != View.Details) e.DrawDefault = true; };
            LVDevices.DrawSubItem += LVDevices_DrawSubItem;
            // Empty state: a label inside the list, below the column headers
            lblDevicesEmpty = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Theme.Card,
                ForeColor = Theme.SecondaryText,
            };
            LVDevices.Controls.Add(lblDevicesEmpty);
            LVDevices.Resize += (s, e) => PlaceEmptyLabel();
            PlaceEmptyLabel();

            // Tabs: white pages
            foreach (TabPage p in new[] { tabMain, tabAdvanced, tabExpert, tabDebug })
            {
                p.UseVisualStyleBackColor = false;
                p.BackColor = Theme.Card;
            }
            foreach (Button b in new[] { cmdGetInfo, cmdReboot, txtStartDebug, cmdStopDebugging, cmdClearDebug })
                Theme.StyleSecondary(b);
            txtDebug.Font = Theme.LogFont;

            // Monitor column
            panScreens.BackColor = Theme.Surface;
            panScreens.Padding = new Padding(Px(8));
            // The monitor cards are scaled to the new font when they get their handle, so lay them out on every layout pass
            panScreens.Layout += (s, e) => LayoutMonitors();
            splitMain.BackColor = Theme.Separator;
            splitMain.Width = 1;
            tsConMain.ContentPanel.BackColor = Theme.Card;
            tsConMain.TopToolStripPanel.BackColor = Theme.Surface;

            // Status bar: device summary on the left, last message on the right
            toolStripStatusLabel1.ImageAlign = ContentAlignment.MiddleLeft;
            lblDebug.Spring = true;
            lblDebug.TextAlign = ContentAlignment.MiddleRight;
            lblDebug.ForeColor = Theme.SecondaryText;

            // Tray menu: status line and start/stop for all monitors
            trayStatus = new ToolStripMenuItem { Enabled = false };
            trayStart = new ToolStripMenuItem(null, null, (s, e) => StartStopAll(true));
            trayStop = new ToolStripMenuItem(null, null, (s, e) => StartStopAll(false));
            contextMenuTaskIcon.Items.Insert(0, trayStatus);
            contextMenuTaskIcon.Items.Insert(1, new ToolStripSeparator());
            contextMenuTaskIcon.Items.Insert(2, trayStart);
            contextMenuTaskIcon.Items.Insert(3, trayStop);
            contextMenuTaskIcon.Items.Insert(4, new ToolStripSeparator());
            contextMenuTaskIcon.Opening += (s, e) => UpdateTrayMenu();

            L.LanguageChanged += (s, e) => { if (!IsDisposed) BeginInvoke((Action)ApplyTexts); };
            ApplyTexts();
            SetMode(Properties.Settings.Default.bAdvancedMode, false);
        }

        private void ApplyTexts()
        {
            fileToolStripMenuItem.Text = L.T("m_file");
            optionsToolStripMenuItem.Text = L.T("m_options");
            hideAmbiBackToolStripMenuItem.Text = L.T("m_hide");
            saveSettingsToolStripMenuItem.Text = L.T("m_save");
            exitToolStripMenuItem.Text = L.T("m_exit");
            devicesToolStripMenuItem.Text = L.T("m_devices");
            mnuFind.Text = L.T("m_find");
            addDeviceToolStripMenuItem.Text = L.T("m_add");
            viewDetailsToolStripMenuItem.Text = L.T("m_details");
            mnuWifi.Text = L.T("m_wifi");
            mnuCleanup.Text = L.T("m_cleanup");
            deleteToolStripMenuItem.Text = L.T("m_delete");
            toolsToolStripMenuItem.Text = L.T("m_tools");
            connectHueBridgeToolStripMenuItem.Text = L.T("m_hue");
            testVideosToolStripMenuItem.Text = L.T("m_videos");
            viewToolStripMenuItem.Text = L.T("m_view");
            mnuModeEasy.Text = L.T("m_easy");
            mnuModeAdvanced.Text = L.T("m_advanced");
            mnuDetails.Text = L.T("m_view_details");
            mnuList.Text = L.T("m_view_list");
            mnuTile.Text = L.T("m_view_tile");
            mnuSmallIcons.Text = L.T("m_view_small");
            mnuLargeIcons.Text = L.T("m_view_large");
            helpToolStripMenuItem.Text = L.T("m_help");
            mnuAbout.Text = L.T("m_about");
            btnModeEasy.Text = L.T("mode_easy");
            btnModeAdvanced.Text = L.T("mode_advanced");
            tbHue.Text = L.T("tb_hue");
            tbHue.ToolTipText = L.T("tb_hue_tip");
            tbOptions.Text = L.T("tb_options");
            tbOptions.ToolTipText = L.T("tb_options_tip");
            mnuStartAll.Text = L.T("tray_start");
            mnuStopAll.Text = L.T("tray_stop");

            btnEnumDevices.Text = L.T("tb_find");
            btnAdd.Text = L.T("tb_add");
            btnConfig.Text = L.T("tb_details");
            btnCleanup.Text = L.T("tb_cleanup");
            btnDelete.Text = L.T("tb_delete");
            btnChangeView.Text = L.T("tb_view");
            chDeviceType.Text = L.T("col_type");
            chDeviceName.Text = L.T("col_name");
            chDeviceIP.Text = L.T("col_ip");
            chDeviceStatus.Text = L.T("col_status");
            toolStripMenuItemShowDetails.Text = L.T("ctx_details");
            connectWifiToolStripMenuItem.Text = L.T("ctx_wifi");
            deleteToolStripMenuItem1.Text = L.T("ctx_delete");
            lblDevicesEmpty.Text = L.T("devices_empty");

            tabMain.Text = L.T("tab_colors");
            tabAdvanced.Text = L.T("tab_device");
            tabExpert.Text = L.T("tab_expert");
            tabDebug.Text = L.T("tab_debug");
            lblRed.Text = L.T("lbl_red");
            label4.Text = L.T("lbl_green");
            lblBlue.Text = L.T("lbl_blue");
            lblColorBoost.Text = L.T("lbl_boost");
            lblValueGain.Text = L.T("lbl_gain");
            lblMaxBrightness.Text = L.T("lbl_maxbright");
            lblMinimumLuminosity.Text = L.T("lbl_minlum");
            lblConnectionType.Text = L.T("lbl_conntype");
            label2.Text = L.T("lbl_devname");
            label1.Text = L.T("lbl_smoothing");
            lblFrameRate.Text = L.T("lbl_fps");
            label3.Text = L.T("lbl_inner");
            lblDebugSyslog.Text = L.T("lbl_syslog_server");
            label5.Text = L.T("lbl_syslog_port");
            txtStartDebug.Text = L.T("btn_debug_start");
            cmdStopDebugging.Text = L.T("btn_debug_stop");
            gbDebug.Text = L.T("gb_debug_output");
            chkShowDebugMessages.Text = L.T("chk_debug_serial");
            cmdGetInfo.Text = L.T("btn_getinfo");
            cmdReboot.Text = L.T("btn_reboot");
            cmdClearDebug.Text = L.T("btn_clear");
            CGlobals.CONNECTIONTYPES[(byte)ConnectionTypes.TCP] = L.T("conn_tcp");
            CGlobals.CONNECTIONTYPES[(byte)ConnectionTypes.UDP] = L.T("conn_udp");
            CGlobals.CONNECTIONTYPES[(byte)ConnectionTypes.USB] = L.T("conn_usb");
            // Replacing the items must not send the connection type to the selected device again
            cbConnectionType.SelectedValueChanged -= cbConnectionType_SelectedValueChanged;
            for (int i = 0; i < cbConnectionType.Items.Count && i < CGlobals.CONNECTIONTYPES.Length; i++)
                cbConnectionType.Items[i] = CGlobals.CONNECTIONTYPES[i];
            cbConnectionType.SelectedValueChanged += cbConnectionType_SelectedValueChanged;

            showToolStripMenuItem.Text = L.T("tray_show");
            exitFromTaskIcon.Text = L.T("tray_exit");
            trayStart.Text = L.T("tray_start");
            trayStop.Text = L.T("tray_stop");

            foreach (Control c in panScreens.Controls)
            {
                var mon = c as AmbiMonitor;
                if (mon != null) mon.ApplyTexts();
            }
            foreach (ListViewItem item in LVDevices.Items)
            {
                var dev = item.Tag as CDevice;
                if (dev != null) ListviewHandleOnlineState(dev, item);
            }
            UpdateDeviceSummary();
        }

        /// <summary>Easy: monitors, devices, colors and device settings. Advanced: also the Debug tab and list styles.</summary>
        private void SetMode(bool advanced, bool save = true)
        {
            btnModeEasy.Checked = mnuModeEasy.Checked = !advanced;
            btnModeAdvanced.Checked = mnuModeAdvanced.Checked = advanced;
            btnModeEasy.ForeColor = advanced ? Theme.PrimaryText : Color.White;
            btnModeAdvanced.ForeColor = advanced ? Color.White : Theme.PrimaryText;
            advancedMode = advanced;
            UpdateTabs(currentSelectedDevice);
            foreach (ToolStripItem i in new ToolStripItem[] { mnuDetails, mnuList, mnuTile, mnuSmallIcons, mnuLargeIcons, btnChangeView })
                i.Visible = advanced;
            if (save && Properties.Settings.Default.bAdvancedMode != advanced)
            {
                Properties.Settings.Default.bAdvancedMode = advanced;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>Minimum size as designed (960 x 720 at 96 DPI), window not larger than the working area.</summary>
        private void FitToScreen()
        {
            Rectangle wa = Screen.FromControl(this).WorkingArea;
            MinimumSize = new Size(Math.Min(Px(960), wa.Width), Math.Min(Px(720), wa.Height));
            Size = new Size(Math.Min(Width, wa.Width), Math.Min(Height, wa.Height));
            if (StartPosition == FormStartPosition.CenterScreen)
                Location = new Point(wa.Left + (wa.Width - Width) / 2, wa.Top + (wa.Height - Height) / 2);
        }

        private bool advancedMode;

        /// <summary>
        /// Colors and Device always; Debug only in Advanced mode and for devices with debug support.
        /// The Expert tab stays hidden, as before (it was disabled in the original code).
        /// </summary>
        private void UpdateTabs(CDevice device)
        {
            bool debug = advancedMode && device != null && device.DebugSupport;
            if (tcMain.TabPages.Contains(tabExpert)) tcMain.TabPages.Remove(tabExpert);
            if (!tcMain.TabPages.Contains(tabMain)) tcMain.TabPages.Insert(0, tabMain);
            if (!tcMain.TabPages.Contains(tabAdvanced)) tcMain.TabPages.Insert(1, tabAdvanced);
            if (debug && !tcMain.TabPages.Contains(tabDebug)) tcMain.TabPages.Add(tabDebug);
            if (!debug && tcMain.TabPages.Contains(tabDebug)) tcMain.TabPages.Remove(tabDebug);
        }

        /// <summary>Stack the monitor cards in the left column with a gap between them.</summary>
        private bool layingOutMonitors;

        private void LayoutMonitors()
        {
            if (layingOutMonitors) return;
            layingOutMonitors = true;
            try { LayoutMonitorsCore(); } finally { layingOutMonitors = false; }
        }

        private void LayoutMonitorsCore()
        {
            int gap = Px(8), y = panScreens.Padding.Top - panScreens.VerticalScroll.Value;
            int content = panScreens.Padding.Vertical;
            foreach (Control c in panScreens.Controls)
                if (c is AmbiMonitor) content += c.Height + gap;
            // Leave room for the vertical scroll bar so no horizontal one appears
            int w = panScreens.Width - panScreens.Padding.Horizontal
                - (content > panScreens.Height ? SystemInformation.VerticalScrollBarWidth : 0) - 2;
            foreach (Control c in panScreens.Controls)
            {
                var mon = c as AmbiMonitor;
                if (mon == null) continue;
                mon.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                // The designer minimum width (scaled with the font) would push the card under the scroll bar
                if (mon.MinimumSize.Width > 0) mon.MinimumSize = new Size(0, mon.MinimumSize.Height);
                mon.SetBounds(panScreens.Padding.Left, y, w, mon.Height);
                y += mon.Height + gap;
            }
        }

        private void PlaceEmptyLabel()
        {
            if (lblDevicesEmpty == null) return;
            int header = Px(28);
            lblDevicesEmpty.SetBounds(0, header, Math.Max(0, LVDevices.ClientSize.Width), Math.Max(0, LVDevices.ClientSize.Height - header));
            lblDevicesEmpty.Visible = LVDevices.Items.Count == 0 && LVDevices.View == View.Details;
        }

        /// <summary>Status column with a colored dot; the dot color is stored in the sub item's Tag.</summary>
        private void LVDevices_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (e.ColumnIndex != 3 || !(e.SubItem.Tag is Color))
            {
                e.DrawDefault = true;
                return;
            }
            bool sel = e.Item.Selected;
            using (var b = new SolidBrush(sel ? SystemColors.Highlight : LVDevices.BackColor))
                e.Graphics.FillRectangle(b, e.Bounds);
            int x = e.Bounds.X + Px(6), size = Px(8);
            Theme.DrawStatusDot(e.Graphics, sel ? Color.White : (Color)e.SubItem.Tag, x, e.Bounds.Y + e.Bounds.Height / 2, size);
            var r = new Rectangle(x + size + Px(6), e.Bounds.Y, e.Bounds.Width - size - Px(12), e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, LVDevices.Font, r, sel ? SystemColors.HighlightText : Theme.PrimaryText,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        private int RunningMonitors(out int total)
        {
            int running = 0;
            total = 0;
            foreach (Control c in panScreens.Controls)
            {
                var mon = c as AmbiMonitor;
                if (mon == null) continue;
                total++;
                if (mon.AmbiBackRunning) running++;
            }
            return running;
        }

        private Bitmap statusDot;
        private Color statusDotColor;

        /// <summary>"3 devices, 2 online · running on 1 of 2 monitors" with a status dot, also used for the tray tooltip.</summary>
        internal void UpdateDeviceSummary()
        {
            if (lblDevicesEmpty == null) return;
            int count = 0, online = 0;
            foreach (ListViewItem item in LVDevices.Items)
            {
                var dev = item.Tag as CDevice;
                if (dev == null) continue;
                count++;
                if (dev.IsSerial() || dev.IsOnline()) online++;
            }
            int total, running = RunningMonitors(out total);
            string text = count == 0 ? L.T("devices_none") : L.F("devices_summary", count, online);
            if (running > 0) text += " · " + L.F("running_summary", running, total);
            toolStripStatusLabel1.Text = text;

            Color dot = running > 0 ? Theme.Success : (online > 0 ? Theme.Accent : Theme.ControlBorder);
            if (statusDot == null || statusDotColor != dot)
            {
                var bmp = new Bitmap(Px(12), Px(12));
                using (var g = Graphics.FromImage(bmp))
                    Theme.DrawStatusDot(g, dot, Px(2), bmp.Height / 2, Px(8));
                toolStripStatusLabel1.Image = bmp;
                if (statusDot != null) statusDot.Dispose();
                statusDot = bmp;
                statusDotColor = dot;
            }

            UpdateAppToolbar(running, total);

            string tip = L.T("app") + " – " + (running > 0 ? L.F("tray_running", running, total) : L.T("tray_idle"));
            nficon.Text = tip.Length > 63 ? tip.Substring(0, 63) : tip;
            PlaceEmptyLabel();
        }

        /// <summary>
        /// Toolbar under the menu: live status on the left; Hue Bridge and Options in the middle; the mode
        /// switch and the one prominent action, Start All / Stop All, on the right. Every item is also in a menu.
        /// </summary>
        private void BuildAppToolbar()
        {
            tsApp = new ToolStrip
            {
                Dock = DockStyle.Top,
                ImageScalingSize = new Size(Px(20), Px(20)),
                AutoSize = false,
                Height = Px(52),
                CanOverflow = true,
                Stretch = true,
            };
            Theme.StyleStrip(tsApp);
            tsApp.BackColor = Theme.Card;
            tsApp.Padding = new Padding(Px(12), Px(8), Px(12), Px(8));

            tbStatus = new ToolStripLabel { Margin = new Padding(0, 0, Px(16), 0), ImageAlign = ContentAlignment.MiddleLeft };
            tbStatus.Font = Theme.CardTitleFont;

            tbHue = AppButton(Theme.Glyphs.Lightbulb, (s, e) => connectHueBridgeToolStripMenuItem_Click(s, e));
            tbOptions = AppButton(Theme.Glyphs.Settings, (s, e) => optionsToolStripMenuItem.PerformClick());

            // Right edge (added from the right): Start All / Stop All, then the mode switch
            tbStartStop = new ToolStripButton
            {
                Alignment = ToolStripItemAlignment.Right,
                Tag = FlatToolStripRenderer.PrimaryTag,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                AutoSize = false,
                Size = new Size(Px(150), Px(34)),
                Padding = new Padding(Px(10), 0, Px(12), 0),
                Margin = new Padding(Px(12), 0, 0, 0),
                Font = Theme.CardTitleFont,
            };
            tbStartStop.Click += (s, e) =>
            {
                int total, running = RunningMonitors(out total);
                StartStopAll(running == 0);
            };

            btnModeAdvanced = new ToolStripButton { Alignment = ToolStripItemAlignment.Right, Tag = FlatToolStripRenderer.SegmentTag, Padding = new Padding(Px(8), 0, Px(8), 0), AutoSize = false, Height = Px(30) };
            btnModeEasy = new ToolStripButton { Alignment = ToolStripItemAlignment.Right, Tag = FlatToolStripRenderer.SegmentTag, Padding = new Padding(Px(8), 0, Px(8), 0), AutoSize = false, Height = Px(30) };
            btnModeEasy.Click += (s, e) => SetMode(false);
            btnModeAdvanced.Click += (s, e) => SetMode(true);
            btnModeEasy.TextChanged += (s, e) => btnModeEasy.Width = TextRenderer.MeasureText(btnModeEasy.Text, tsApp.Font).Width + Px(20);
            btnModeAdvanced.TextChanged += (s, e) => btnModeAdvanced.Width = TextRenderer.MeasureText(btnModeAdvanced.Text, tsApp.Font).Width + Px(20);

            tsApp.Items.AddRange(new ToolStripItem[]
            {
                tbStatus,
                new ToolStripSeparator(),
                tbHue,
                tbOptions,
                tbStartStop,
                btnModeAdvanced,
                btnModeEasy,
            });

            // Below the menu bar, above the monitor column and the device list
            Controls.Add(tsApp);
            Controls.SetChildIndex(tsApp, Controls.GetChildIndex(MainMenu));

            // File menu: Start All / Stop All
            mnuStartAll = new ToolStripMenuItem(null, null, (s, e) => StartStopAll(true));
            mnuStopAll = new ToolStripMenuItem(null, null, (s, e) => StartStopAll(false));
            fileToolStripMenuItem.DropDownItems.Insert(0, mnuStartAll);
            fileToolStripMenuItem.DropDownItems.Insert(1, mnuStopAll);
            fileToolStripMenuItem.DropDownItems.Insert(2, new ToolStripSeparator());
            fileToolStripMenuItem.DropDownOpening += (s, e) =>
            {
                int total, running = RunningMonitors(out total);
                mnuStartAll.Enabled = running < total;
                mnuStopAll.Enabled = running > 0;
            };
        }

        private ToolStripButton AppButton(char glyph, EventHandler click)
        {
            var b = new ToolStripButton
            {
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(Px(8), Px(4), Px(10), Px(4)),
                Margin = new Padding(0, 0, Px(4), 0),
            };
            SetGlyph(b, glyph, Theme.PrimaryText);
            b.Click += click;
            return b;
        }

        private void SetGlyph(ToolStripItem item, char glyph, Color color)
        {
            Bitmap bmp = Theme.Glyph(glyph, color, Px(20));
            if (bmp == null)
                return;    // no icon font: keep the designer image
            item.Image = bmp;
            item.ImageScaling = ToolStripItemImageScaling.None;
        }

        private Bitmap toolbarDot;
        private Color toolbarDotColor;
        private int toolbarRunning = -1;

        /// <summary>Status on the left and the Start All / Stop All button follow the monitors.</summary>
        private void UpdateAppToolbar(int running, int total)
        {
            if (tsApp == null) return;
            tbStatus.Text = running > 0 ? L.F("tray_running", running, total) : L.T("tray_idle");
            Color dot = running > 0 ? Theme.Success : Theme.ControlBorder;
            if (toolbarDot == null || toolbarDotColor != dot)
            {
                var bmp = new Bitmap(Px(14), Px(14));
                using (var g = Graphics.FromImage(bmp))
                    Theme.DrawStatusDot(g, dot, Px(2), bmp.Height / 2, Px(10));
                tbStatus.Image = bmp;
                toolbarDot?.Dispose();
                toolbarDot = bmp;
                toolbarDotColor = dot;
            }
            bool anyRunning = running > 0;
            tbStartStop.Text = anyRunning ? L.T("tb_stop_all") : L.T("tb_start_all");
            tbStartStop.ToolTipText = anyRunning ? L.T("tray_stop") : L.T("tray_start");
            if (toolbarRunning != (anyRunning ? 1 : 0))
            {
                toolbarRunning = anyRunning ? 1 : 0;
                tbStartStop.Image?.Dispose();
                tbStartStop.Image = Theme.Glyph(anyRunning ? Theme.Glyphs.Stop : Theme.Glyphs.Play, Color.White, Px(16));
                tbStartStop.ImageScaling = ToolStripItemImageScaling.None;
            }
            tbStartStop.Enabled = total > 0;
        }

        /// <summary>The monitor cards in their order.</summary>
        internal System.Collections.Generic.List<AmbiMonitor> GetMonitors()
        {
            var list = new System.Collections.Generic.List<AmbiMonitor>();
            foreach (Control c in panScreens.Controls)
                if (c is AmbiMonitor mon)
                    list.Add(mon);
            return list;
        }

        /// <summary>Links a device to one monitor only (removed from the others) and starts it when that monitor runs.</summary>
        internal void AssignDeviceToMonitor(CDevice device, AmbiMonitor target)
        {
            foreach (AmbiMonitor mon in GetMonitors())
            {
                if (mon != target && mon.GetDevices().Contains(device))
                {
                    mon.RemoveDevice(device);
                    if (mon.AmbiBackRunning)
                        device.AbortThread();
                }
            }
            if (!target.GetDevices().Contains(device))
            {
                target.AddDevice(device);
                if (target.MonitorEnabled && target.AmbiBackRunning)
                    device.StartThread();
            }
            foreach (ListViewItem item in LVDevices.Items)
                if (item.Tag == device && selectedAmbiScreen != null)
                    item.Checked = selectedAmbiScreen.GetDevices().Contains(device);
        }

        /// <summary>Checkboxes of the Hue lights show their group membership.</summary>
        internal void RefreshHueLampChecks()
        {
            foreach (ListViewItem item in LVDevices.Items)
                if (item.Tag is CDevicePhilipsHueLight lamp && item.Checked != lamp.InGroup)
                    item.Checked = lamp.InGroup;
            if (selectedAmbiScreen != null)
                selectedAmbiScreen.SetControlsBySelectedDevice();
        }

        private void UpdateTrayMenu()
        {
            int total, running = RunningMonitors(out total);
            trayStatus.Text = running > 0 ? L.F("tray_running", running, total) : L.T("tray_idle");
            trayStart.Enabled = running < total;
            trayStop.Enabled = running > 0;
        }

        /// <summary>Start or stop every enabled monitor that has linked devices.</summary>
        private void StartStopAll(bool start)
        {
            foreach (Control c in panScreens.Controls)
            {
                var mon = c as AmbiMonitor;
                if (mon == null || !mon.MonitorEnabled) continue;
                if (start && !mon.AmbiBackRunning && mon.GetDevices().Count > 0)
                    mon.btnStart_Click(this, EventArgs.Empty);
                else if (!start && mon.AmbiBackRunning)
                    mon.btnStop_Click(this, EventArgs.Empty);
            }
            UpdateDeviceSummary();
        }
    }
}
