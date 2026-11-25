using System;
using System.Drawing;
using System.Windows.Forms;

namespace AmbiBack
{
    /// <summary>Layout helpers shared by the dialogs. All sizes are given at 96 DPI and scaled.</summary>
    internal static class DialogStyle
    {
        public static int Px(Control c, int v) { return (int)Math.Round(v * c.DeviceDpi / 96.0); }

        /// <summary>
        /// Header of the small dialogs: white band with a semibold heading and the dialog icon,
        /// separated from the content by a thin line instead of a frame.
        /// </summary>
        public static void Header(Form f, Label title, PictureBox icon, string text)
        {
            title.Text = text;
            title.BorderStyle = BorderStyle.None;
            title.BackColor = Theme.Card;
            title.ForeColor = Theme.PrimaryText;
            title.Font = Theme.SectionFont;
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.Padding = new Padding(Px(f, 14), 0, 0, 0);
            title.Location = new Point(0, 0);
            title.Size = new Size(f.ClientSize.Width, Px(f, 56));
            if (icon != null)
            {
                icon.BackColor = Theme.Card;
                icon.SizeMode = PictureBoxSizeMode.Zoom;
                icon.Size = new Size(Px(f, 32), Px(f, 32));
                icon.Location = new Point(f.ClientSize.Width - icon.Width - Px(f, 14), (title.Height - icon.Height) / 2);
                icon.BringToFront();
            }
            f.Paint += (s, e) =>
            {
                using (var p = new Pen(Theme.Separator))
                    e.Graphics.DrawLine(p, 0, title.Bottom, f.ClientSize.Width, title.Bottom);
            };
        }

        /// <summary>Label above a field (instead of the former yellow boxes).</summary>
        public static void FieldLabel(Label l, string text)
        {
            l.Text = text;
            l.AutoSize = true;
            l.BorderStyle = BorderStyle.None;
            l.BackColor = Color.Transparent;
            l.ForeColor = Theme.PrimaryText;
            l.TextAlign = ContentAlignment.TopLeft;
        }

        /// <summary>OK as primary and Cancel as secondary button at the bottom right.</summary>
        public static void Buttons(Form f, Button ok, string okText, Button cancel, int top)
        {
            int w = Px(f, 96), h = Px(f, 30), gap = Px(f, 8), right = f.ClientSize.Width - Px(f, 16);
            ok.Text = okText;
            ok.Size = new Size(w, h);
            ok.Location = new Point(right - w, top);
            Theme.StylePrimary(ok);
            if (cancel != null)
            {
                cancel.Text = L.T("cancel");
                cancel.Size = new Size(w, h);
                cancel.Location = new Point(ok.Left - gap - w, top);
                Theme.StyleSecondary(cancel);
            }
        }
    }

    public partial class frmAddDevice
    {
        private void ApplyStyle()
        {
            Theme.ApplyForm(this);
            Text = L.T("add_title");
            int pad = DialogStyle.Px(this, 16), y;
            ClientSize = new Size(DialogStyle.Px(this, 440), DialogStyle.Px(this, 280));
            DialogStyle.Header(this, lblTitle, pbIcon, L.T("add_heading"));
            y = lblTitle.Bottom + pad;
            DialogStyle.FieldLabel(label1, L.T("add_prompt"));
            label1.Location = new Point(pad, y);
            txtDevice.Location = new Point(pad, label1.Bottom + DialogStyle.Px(this, 4));
            txtDevice.Width = ClientSize.Width - 2 * pad;
            Theme.StyleHint(label2);
            label2.AutoSize = true;
            label2.Text = L.T("example_host");
            label2.Location = new Point(pad, txtDevice.Bottom + DialogStyle.Px(this, 4));
            chkAutoDiscover.Text = L.T("add_auto");
            chkAutoDiscover.AutoSize = true;
            chkAutoDiscover.Location = new Point(pad, label2.Bottom + DialogStyle.Px(this, 12));
            DialogStyle.FieldLabel(lblDeviceType, L.T("add_type"));
            lblDeviceType.Location = new Point(pad + DialogStyle.Px(this, 18), chkAutoDiscover.Bottom + DialogStyle.Px(this, 10));
            cbDeviceType.Location = new Point(DialogStyle.Px(this, 150), chkAutoDiscover.Bottom + DialogStyle.Px(this, 6));
            cbDeviceType.Width = ClientSize.Width - cbDeviceType.Left - pad;
            DialogStyle.Buttons(this, cmdOK, L.T("add"), cmdCancel, ClientSize.Height - DialogStyle.Px(this, 46));
        }
    }

    public partial class frmConnectHue
    {
        private Label lblHint;
        private Label lblAreas;
        private Label lblHueStatus;
        private Label lblNoAreas;
        private ListView lvAreas;
        private Button cmdRefresh;
        private Button cmdAddArea;

        private void ApplyStyle()
        {
            Theme.ApplyForm(this);
            Text = L.T("hue_title");
            int pad = DialogStyle.Px(this, 16), gap = DialogStyle.Px(this, 8);
            int btnW = DialogStyle.Px(this, 110), btnH = DialogStyle.Px(this, 30);
            ClientSize = new Size(DialogStyle.Px(this, 560), DialogStyle.Px(this, 520));
            DialogStyle.Header(this, lblTitle, pbIcon, L.T("hue_heading"));

            // Pairing: address and Connect in one row
            DialogStyle.FieldLabel(label1, L.T("hue_prompt"));
            label1.Location = new Point(pad, lblTitle.Bottom + pad);
            txtDevice.Location = new Point(pad, label1.Bottom + DialogStyle.Px(this, 4));
            txtDevice.Width = ClientSize.Width - 2 * pad - btnW - gap;
            cmdOK.Text = L.T("connect");
            cmdOK.Size = new Size(btnW, btnH);
            cmdOK.Location = new Point(txtDevice.Right + gap, txtDevice.Top + (txtDevice.Height - btnH) / 2);
            Theme.StylePrimary(cmdOK);
            Theme.StyleHint(label2);
            label2.AutoSize = true;
            label2.Text = L.T("example_host");
            label2.Location = new Point(pad, txtDevice.Bottom + DialogStyle.Px(this, 4));
            lblHint = new Label
            {
                Text = L.T("hue_hint"),
                Location = new Point(pad, label2.Bottom + DialogStyle.Px(this, 8)),
                Size = new Size(ClientSize.Width - 2 * pad, DialogStyle.Px(this, 36)),
                ForeColor = Theme.PrimaryText,
            };
            Controls.Add(lblHint);

            // Entertainment areas (streaming)
            lblAreas = new Label
            {
                Text = L.T("hue_areas"),
                Font = Theme.SectionFont,
                ForeColor = Theme.PrimaryText,
                AutoSize = true,
                Location = new Point(pad, lblHint.Bottom + DialogStyle.Px(this, 10)),
            };
            Controls.Add(lblAreas);
            lblHueStatus = new Label
            {
                Location = new Point(pad, lblAreas.Bottom + DialogStyle.Px(this, 4)),
                Size = new Size(ClientSize.Width - 2 * pad, DialogStyle.Px(this, 34)),
            };
            Controls.Add(lblHueStatus);

            int bottomRow = ClientSize.Height - DialogStyle.Px(this, 46);
            int listTop = lblHueStatus.Bottom + DialogStyle.Px(this, 4);
            int actionRow = bottomRow - btnH - gap;
            lvAreas = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(pad, listTop),
                Size = new Size(ClientSize.Width - 2 * pad, actionRow - gap - listTop),
                Font = Theme.BaseFont,
            };
            int listW = lvAreas.ClientSize.Width;
            lvAreas.Columns.Add(L.T("hue_col_area"), listW * 34 / 100);
            lvAreas.Columns.Add(L.T("hue_col_channels"), listW * 14 / 100, HorizontalAlignment.Right);
            lvAreas.Columns.Add(L.T("hue_col_bridge"), listW * 20 / 100);
            lvAreas.Columns.Add(L.T("hue_col_ambiback"), listW * 30 / 100);
            lvAreas.SelectedIndexChanged += lvAreas_SelectedIndexChanged;
            lvAreas.DoubleClick += cmdAddArea_Click;
            Controls.Add(lvAreas);

            lblNoAreas = new Label
            {
                Text = L.T("hue_no_areas"),
                ForeColor = Theme.SecondaryText,
                BackColor = SystemColors.Window,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(lvAreas.Left + 1, lvAreas.Top + DialogStyle.Px(this, 26)),
                Size = new Size(lvAreas.Width - 2, DialogStyle.Px(this, 40)),
                Visible = false,
            };
            Controls.Add(lblNoAreas);
            lblNoAreas.BringToFront();

            cmdAddArea = new Button { Text = L.T("hue_add_area"), Size = new Size(btnW, btnH), Location = new Point(pad, actionRow), Enabled = false };
            Theme.StyleSecondary(cmdAddArea);
            cmdAddArea.Click += cmdAddArea_Click;
            Controls.Add(cmdAddArea);
            cmdRefresh = new Button { Text = L.T("hue_refresh"), Size = new Size(btnW, btnH), Location = new Point(cmdAddArea.Right + gap, actionRow), Enabled = false };
            Theme.StyleSecondary(cmdRefresh);
            cmdRefresh.Click += cmdRefresh_Click;
            Controls.Add(cmdRefresh);

            // Close at the bottom right; Connect is the default button while the address field has focus
            cmdCancel.Text = L.T("close");
            cmdCancel.Size = new Size(DialogStyle.Px(this, 96), btnH);
            cmdCancel.Location = new Point(ClientSize.Width - pad - cmdCancel.Width, bottomRow);
            Theme.StyleSecondary(cmdCancel);
        }
    }

    public partial class frmConnectWifi
    {
        private CheckBox chkShowKey;

        private void ApplyStyle()
        {
            Theme.ApplyForm(this);
            Text = L.T("wifi_title");
            int pad = DialogStyle.Px(this, 16);
            ClientSize = new Size(DialogStyle.Px(this, 440), DialogStyle.Px(this, 270));
            DialogStyle.Header(this, lblTitle, pbIcon, L.T("wifi_heading"));
            DialogStyle.FieldLabel(label1, L.T("wifi_ssid"));
            label1.Location = new Point(pad, lblTitle.Bottom + pad);
            txtWifiSSID.Location = new Point(pad, label1.Bottom + DialogStyle.Px(this, 4));
            txtWifiSSID.Width = ClientSize.Width - 2 * pad;
            DialogStyle.FieldLabel(label2, L.T("wifi_key"));
            label2.Location = new Point(pad, txtWifiSSID.Bottom + DialogStyle.Px(this, 12));
            txtWifiKey.Location = new Point(pad, label2.Bottom + DialogStyle.Px(this, 4));
            txtWifiKey.Width = ClientSize.Width - 2 * pad;
            txtWifiKey.PasswordChar = '\0';   // the designer '*' would keep masking when the password is shown
            txtWifiKey.UseSystemPasswordChar = true;
            chkShowKey = new CheckBox
            {
                Text = L.T("wifi_show"),
                AutoSize = true,
                Location = new Point(pad, txtWifiKey.Bottom + DialogStyle.Px(this, 6)),
            };
            chkShowKey.CheckedChanged += (s, e) => txtWifiKey.UseSystemPasswordChar = !chkShowKey.Checked;
            Controls.Add(chkShowKey);
            DialogStyle.Buttons(this, cmdOK, L.T("ok"), cmdCancel, ClientSize.Height - DialogStyle.Px(this, 46));
        }
    }

    public partial class frmOptions
    {
        private ComboBox cbLanguage;

        private void ApplyStyle()
        {
            Theme.ApplyForm(this);
            Text = L.T("opt_title");
            TakeSnapshot();
            int pad = DialogStyle.Px(this, 16);
#if DEBUG
            bool showDebug = true;    // the debug options exist in debug builds only
#else
            bool showDebug = false;
#endif

            // One page with cards instead of two tabs (the second tab was empty)
            var general = new CardGroupBox { Text = L.T("opt_general") };
            Controls.Add(general);
            foreach (Control c in new Control[] { chkHideOnStartup, chkHideOnWindowClose, chkLoadWindowsStartup })
                { c.Anchor = AnchorStyles.Top | AnchorStyles.Left; general.Controls.Add(c); }
            Controls.Remove(tabMain);

            int inner = DialogStyle.Px(this, 12), w = DialogStyle.Px(this, 520);
            general.Location = new Point(pad, pad);
            int y = DialogStyle.Px(this, 30);
            foreach (CheckBox c in new[] { chkHideOnStartup, chkHideOnWindowClose, chkLoadWindowsStartup })
            {
                c.AutoSize = true;
                c.Location = new Point(inner, y);
                y += DialogStyle.Px(this, 26);
            }
            chkHideOnStartup.Text = L.T("opt_hide_startup");
            chkHideOnWindowClose.Text = L.T("opt_hide_close");
            chkLoadWindowsStartup.Text = L.T("opt_autostart");

            var lblLang = new Label { Text = L.T("opt_language"), AutoSize = true, Location = new Point(inner, y + DialogStyle.Px(this, 10)) };
            cbLanguage = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(DialogStyle.Px(this, 140), y + DialogStyle.Px(this, 6)), Width = DialogStyle.Px(this, 200) };
            cbLanguage.Items.AddRange(new object[] { L.T("opt_lang_system"), "English", "Deutsch" });
            string lang = Properties.Settings.Default.szLanguage;
            cbLanguage.SelectedIndex = lang == "en" ? 1 : lang == "de" ? 2 : 0;
            general.Controls.Add(lblLang);
            general.Controls.Add(cbLanguage);
            general.Size = new Size(w, cbLanguage.Bottom + inner);

            int bottom = general.Bottom;
            if (showDebug)
            {
                var debug = new CardGroupBox { Text = L.T("opt_debug") };
                Controls.Add(debug);
                foreach (Control c in new Control[] { chkDebugLogging, lblDebugLog, txtDebugLog, cmdDbgBrowse, chkDbgDelete, chkDebugWindow })
                { c.Anchor = AnchorStyles.Top | AnchorStyles.Left; debug.Controls.Add(c); }
                gbDebug.Visible = false;
                debug.Location = new Point(pad, general.Bottom + pad);
                y = DialogStyle.Px(this, 30);
                chkDebugLogging.Text = L.T("opt_debug_file");
                chkDebugLogging.AutoSize = true;
                chkDebugLogging.Location = new Point(inner, y);
                y += DialogStyle.Px(this, 28);
                lblDebugLog.Text = L.T("opt_debug_path");
                lblDebugLog.AutoSize = true;
                lblDebugLog.Location = new Point(inner, y + DialogStyle.Px(this, 4));
                txtDebugLog.Location = new Point(DialogStyle.Px(this, 140), y);
                txtDebugLog.Width = w - txtDebugLog.Left - inner - DialogStyle.Px(this, 32);
                cmdDbgBrowse.Location = new Point(txtDebugLog.Right + DialogStyle.Px(this, 6), y - 1);
                cmdDbgBrowse.Size = new Size(DialogStyle.Px(this, 26), txtDebugLog.Height + 2);
                Theme.StyleSecondary(cmdDbgBrowse);
                var hint = new Label { AutoSize = false, AutoEllipsis = true, Text = L.F("opt_debug_path_hint", AppPaths.DefaultDebugLog), Location = new Point(txtDebugLog.Left, txtDebugLog.Bottom + 2), Size = new Size(w - txtDebugLog.Left - inner, DialogStyle.Px(this, 18)) };
                Theme.StyleHint(hint);
                debug.Controls.Add(hint);
                y = hint.Bottom + DialogStyle.Px(this, 8);
                chkDbgDelete.Text = L.T("opt_debug_delete");
                chkDbgDelete.AutoSize = true;
                chkDbgDelete.Location = new Point(inner, y);
                y += DialogStyle.Px(this, 26);
                chkDebugWindow.Text = L.T("opt_debug_window");
                chkDebugWindow.AutoSize = true;
                chkDebugWindow.Location = new Point(inner, y);
                // chkDebugWindow is hidden in the designer (no debug window yet), so the card ends after chkDbgDelete
                debug.Size = new Size(w, chkDbgDelete.Bottom + inner);
                bottom = debug.Bottom;
            }

            ClientSize = new Size(w + 2 * pad, bottom + DialogStyle.Px(this, 62));
            DialogStyle.Buttons(this, cmdOK, L.T("ok"), cmdCancel, ClientSize.Height - DialogStyle.Px(this, 46));
            AcceptButton = cmdOK;
            CancelButton = cmdCancel;
        }

        // The check boxes are bound to the settings and change them at once. These are the values
        // from when the dialog opened; they are restored unless the dialog is closed with OK.
        private static readonly string[] BoundSettings = { "bHideOnStartup", "bHideOnWindowClose", "bLoadAmbiBackOnWindowsStartup",
            "bDebugWindow", "bDebugFileLogging", "bDebugDeleteExisting", "szDebugFileLog" };
        private readonly System.Collections.Generic.Dictionary<string, object> snapshot = new System.Collections.Generic.Dictionary<string, object>();
        private bool acceptedWithOk;

        private void TakeSnapshot()
        {
            foreach (string name in BoundSettings)
                snapshot[name] = Properties.Settings.Default[name];
            FormClosing += (s, e) => { if (!acceptedWithOk) RestoreSnapshot(); };
        }

        private void RestoreSnapshot()
        {
            foreach (var kv in snapshot)
                Properties.Settings.Default[kv.Key] = kv.Value;
        }

        /// <summary>Language chosen in the dialog ("", "en" or "de").</summary>
        private string SelectedLanguage
        {
            get { return cbLanguage == null ? Properties.Settings.Default.szLanguage : cbLanguage.SelectedIndex == 1 ? "en" : cbLanguage.SelectedIndex == 2 ? "de" : ""; }
        }
    }

    public partial class frmMessageBox
    {
        /// <summary>Plain layout: icon, semibold title and message on the surface, buttons at the bottom right.</summary>
        private void ApplyStyle()
        {
            Theme.ApplyForm(this);
            Text = L.T("app");
            int pad = DialogStyle.Px(this, 16);
            // Positions are set below; anchors would move them again when the size changes
            foreach (Control c in Controls) c.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            lblTitle.Visible = false;
            pbIcon.BackColor = Color.Transparent;
            pbIcon.SizeMode = PictureBoxSizeMode.Zoom;
            pbIcon.Location = new Point(pad, pad);
            pbIcon.Size = new Size(DialogStyle.Px(this, 32), DialogStyle.Px(this, 32));
            lblTitleText.BackColor = Color.Transparent;
            lblTitleText.Font = Theme.SectionFont;
            lblTitleText.ForeColor = Theme.PrimaryText;
            lblTitleText.TextAlign = ContentAlignment.MiddleLeft;
            int left = pbIcon.Right + DialogStyle.Px(this, 12);
            ClientSize = new Size(DialogStyle.Px(this, 480), ClientSize.Height);
            int width = ClientSize.Width - left - pad;
            lblTitleText.Location = new Point(left, pad);
            lblTitleText.Size = new Size(width, pbIcon.Height);
            txtMessage.BackColor = Theme.Surface;
            txtMessage.ForeColor = Theme.PrimaryText;
            txtMessage.Font = Theme.BaseFont;
            txtMessage.Location = new Point(left, lblTitleText.Bottom + DialogStyle.Px(this, 4));

            // Height follows the text; long texts scroll
            Size need = TextRenderer.MeasureText(txtMessage.Text + "\r\n", txtMessage.Font, new Size(width - DialogStyle.Px(this, 8), int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
            int max = DialogStyle.Px(this, 320);
            txtMessage.ScrollBars = need.Height > max ? ScrollBars.Vertical : ScrollBars.None;
            txtMessage.Size = new Size(width, Math.Max(DialogStyle.Px(this, 24), Math.Min(need.Height, max)));
            int y = txtMessage.Bottom + DialogStyle.Px(this, 12);
            chkAskAgain.Text = L.T("ask_again");
            chkAskAgain.Font = Theme.BaseFont;
            chkAskAgain.AutoSize = true;
            chkAskAgain.Location = new Point(left, y);
            if (bShowAskAgain) y = chkAskAgain.Bottom + DialogStyle.Px(this, 12);
            ClientSize = new Size(ClientSize.Width, y + DialogStyle.Px(this, 46));
            int buttonsTop = ClientSize.Height - DialogStyle.Px(this, 46);
            bool yesNo = myButtons == MessageBoxButtons.YesNo;
            if (myButtons == MessageBoxButtons.OK)
                DialogStyle.Buttons(this, cmdOK, L.T("ok"), null, buttonsTop);
            else
                DialogStyle.Buttons(this, cmdOK, L.T(yesNo ? "yes" : "ok"), cmdCancel, buttonsTop);
            if (yesNo) cmdCancel.Text = L.T("no");

            // Optional "more" link or button on the left of the button row
            lblMore.Visible = false;
            lblMoreLink.Location = new Point(pad, buttonsTop + (cmdOK.Height - lblMoreLink.Height) / 2);
            cmdMore.Location = new Point(pad, buttonsTop);
            cmdMore.Height = cmdOK.Height;
            if (iMoreMode == 1 && szMoreLink != "") Theme.StyleSecondary(cmdMore);
        }
    }

    partial class frmAbout
    {
        private void ApplyStyle()
        {
            Theme.ApplyForm(this);
            Text = L.T("about_title");
            labelProductName.Font = Theme.TitleFont;
            labelProductName.Text = L.T("app");
            labelVersion.Text = L.F("about_version", AssemblyVersion);
            labelVersion.ForeColor = Theme.SecondaryText;
            labelCopyright.ForeColor = Theme.SecondaryText;
            labelCompanyName.Visible = false;   // repeats the name in the copyright line
            textBoxDescription.Text = L.T("about_desc");
            textBoxDescription.BorderStyle = BorderStyle.None;
            textBoxDescription.BackColor = Theme.Surface;
            textBoxDescription.ScrollBars = ScrollBars.None;
            okButton.Text = L.T("ok");
            Theme.StylePrimary(okButton);
            okButton.Size = new Size(DialogStyle.Px(this, 96), DialogStyle.Px(this, 30));
        }
    }

    public partial class frmDeviceDreamScreen
    {
        private void ApplyStyle()
        {
            Theme.ApplyForm(this);
            Text = L.T("ds_title");
            lblDeviceName.Text = L.T("dev_name");
            label2.Text = L.T("dev_ip");
            lblGroupName.Text = L.T("ds_group");
            gbInstallatioNSettings.Text = L.T("ds_install");
            lblInstallationType.Text = L.T("ds_install_type");
            lblLEDPixelsVert.Text = L.T("ds_pix_vert");
            lblLEDPixelsHori.Text = L.T("ds_pix_hori");
            lblLedPortPixels1.Text = L.T("ds_port1_pix");
            lblLedPortPixels2.Text = L.T("ds_port2_pix");
            lblLedPortSetup1.Text = L.T("ds_port1_setup");
            lblLedPortSetup2.Text = L.T("ds_port2_setup");
            cmdOk.Text = L.T("close");
            Theme.StylePrimary(cmdOk);
            cmdOk.Size = new Size(DialogStyle.Px(this, 96), DialogStyle.Px(this, 30));
            cmdOk.Location = new Point(ClientSize.Width - cmdOk.Width - DialogStyle.Px(this, 16), ClientSize.Height - DialogStyle.Px(this, 46));
        }
    }

    public partial class frmDeviceAmbiBack
    {
        private void ApplyStyle()
        {
            Theme.ApplyForm(this);
            Theme.StyleStrip(TSTop);
            Text = L.T("dev_title");
            lblDeviceName.Text = L.T("dev_name");
            lblIPAddress.Text = L.T("dev_ip");
            cmdReload.Text = L.T("dev_reload");
            cmdClose.Text = L.T("close");
            cmdClose.Alignment = ToolStripItemAlignment.Right;
        }
    }

    public partial class frmWeb
    {
        private void ApplyStyle()
        {
            cmdClose.Text = L.T("close");
            Theme.StyleSecondary(cmdClose);
            cmdCloseTop.BackColor = Theme.Card;
            cmdCloseTop.ForeColor = Theme.PrimaryText;
            cmdCloseTop.FlatStyle = FlatStyle.Flat;
            cmdCloseTop.FlatAppearance.BorderColor = Theme.ControlBorder;
            cmdCloseTop.Text = "✕";
            cmdCloseTop.Font = Theme.SectionFont;
        }
    }

    public partial class frmSplash
    {
        private void ApplyStyle()
        {
            lblLoading.Text = L.T("loading");
            lblLoading.Font = Theme.SectionFont;
            lblLoading.BackColor = Theme.Card;
            lblLoading.ForeColor = Theme.PrimaryText;
            lblLoading.BorderStyle = BorderStyle.None;
        }
    }
}
