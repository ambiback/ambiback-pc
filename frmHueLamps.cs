using AmbiBack.PhilipsHue;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace AmbiBack
{
    /// <summary>
    /// Assistant after connecting the bridge: which Hue lights light up with the screen, where each one
    /// stands, and on which monitor the PC group runs. Sets the light devices' membership and screen
    /// sectors, creates or updates the group and assigns it to the monitor.
    /// </summary>
    public class frmHueLamps : Form
    {
        /// <summary>Places and their screen sectors (12 sectors counterclockwise from bottom right; see CDevicePhilipsHueLight).</summary>
        private static readonly string[] PlaceKeys = { "auto", "left", "top_left", "top", "top_right", "right", "bottom_right", "bottom", "bottom_left" };
        private static readonly int[][] PlaceSectors =
        {
            null,
            new[] { 6, 7, 8 },
            new[] { 5, 6 },
            new[] { 3, 4, 5 },
            new[] { 2, 3 },
            new[] { 0, 1, 2 },
            new[] { 11, 0 },
            new[] { 9, 10, 11 },
            new[] { 8, 9 },
        };

        private readonly frmMain mainForm;
        private readonly Label lblTitle = new Label();
        private readonly Label lblIntro = new Label();
        private readonly Panel pnlLamps = new Panel();
        private readonly Label lblMonitor = new Label();
        private readonly ComboBox cbMonitor = new ComboBox();
        private readonly Label lblStatus = new Label();
        private readonly Button cmdOK = new Button();
        private readonly Button cmdCancel = new Button();
        private readonly List<Row> rows = new List<Row>();
        private readonly List<AmbiMonitor> monitors;

        private class Row
        {
            public CDevicePhilipsHueLight Lamp;
            public CheckBox Check;
            public ComboBox Place;
        }

        public frmHueLamps(frmMain main)
        {
            mainForm = main;
            monitors = main != null ? main.GetMonitors() : new List<AmbiMonitor>();

            Theme.ApplyForm(this);
            Text = L.T("hue_lamps_title");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.None;   // sizes are scaled with DialogStyle.Px

            int pad = DialogStyle.Px(this, 16), gap = DialogStyle.Px(this, 8);
            ClientSize = new Size(DialogStyle.Px(this, 520), DialogStyle.Px(this, 470));
            Controls.Add(lblTitle);
            DialogStyle.Header(this, lblTitle, null, L.T("hue_lamps_heading"));

            lblIntro.Text = L.F("hue_lamps_intro", HuePcGroup.AreaName);
            lblIntro.Location = new Point(pad, lblTitle.Bottom + pad);
            lblIntro.Size = new Size(ClientSize.Width - 2 * pad, DialogStyle.Px(this, 36));
            Controls.Add(lblIntro);

            int bottom = ClientSize.Height - DialogStyle.Px(this, 46);
            lblStatus.Location = new Point(pad, bottom - DialogStyle.Px(this, 30));
            lblStatus.Size = new Size(ClientSize.Width - 2 * pad, DialogStyle.Px(this, 24));
            lblStatus.ForeColor = Theme.SecondaryText;
            Controls.Add(lblStatus);

            int monitorTop = lblStatus.Top - DialogStyle.Px(this, 34);
            DialogStyle.FieldLabel(lblMonitor, L.T("hue_lamps_monitor"));
            lblMonitor.Location = new Point(pad, monitorTop + DialogStyle.Px(this, 4));
            Controls.Add(lblMonitor);
            cbMonitor.DropDownStyle = ComboBoxStyle.DropDownList;
            cbMonitor.Location = new Point(pad + DialogStyle.Px(this, 170), monitorTop);
            cbMonitor.Width = ClientSize.Width - cbMonitor.Left - pad;
            cbMonitor.Items.Add(L.T("hue_lamps_no_monitor"));
            foreach (AmbiMonitor m in monitors)
                cbMonitor.Items.Add(m.Title);
            Controls.Add(cbMonitor);

            pnlLamps.Location = new Point(pad, lblIntro.Bottom + DialogStyle.Px(this, 6));
            pnlLamps.Size = new Size(ClientSize.Width - 2 * pad, monitorTop - gap - pnlLamps.Top);
            pnlLamps.AutoScroll = true;
            pnlLamps.BorderStyle = BorderStyle.FixedSingle;
            pnlLamps.BackColor = Theme.Card;
            Controls.Add(pnlLamps);

            Controls.Add(cmdOK);
            Controls.Add(cmdCancel);
            DialogStyle.Buttons(this, cmdOK, L.T("hue_lamps_ok"), cmdCancel, bottom);
            cmdOK.Click += cmdOK_Click;
            cmdCancel.DialogResult = DialogResult.Cancel;
            AcceptButton = cmdOK;
            CancelButton = cmdCancel;

            FillLamps();
            SelectMonitor();
            UpdateState();
        }

        private void FillLamps()
        {
            var lamps = new List<CDevicePhilipsHueLight>();
            if (CGlobals.mainConfig?.Devices != null)
                foreach (CDevice d in CGlobals.mainConfig.Devices.GetDeviceList())
                    if (d is CDevicePhilipsHueLight lamp && lamp.HueLight != null && lamp.HueLight.IsColorLight)
                        lamps.Add(lamp);
            lamps.Sort((a, b) => string.Compare(a.GetDeviceName(), b.GetDeviceName(), StringComparison.CurrentCultureIgnoreCase));

            int y = DialogStyle.Px(this, 6), rowH = DialogStyle.Px(this, 30), comboW = DialogStyle.Px(this, 150);
            foreach (CDevicePhilipsHueLight lamp in lamps)
            {
                var row = new Row { Lamp = lamp };
                row.Check = new CheckBox
                {
                    Text = lamp.GetDeviceName(),
                    Checked = lamp.InGroup,
                    AutoEllipsis = true,
                    Location = new Point(DialogStyle.Px(this, 8), y + DialogStyle.Px(this, 2)),
                    Size = new Size(pnlLamps.ClientSize.Width - comboW - DialogStyle.Px(this, 40), DialogStyle.Px(this, 24)),
                    BackColor = Theme.Card,
                };
                row.Place = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Location = new Point(pnlLamps.ClientSize.Width - comboW - DialogStyle.Px(this, 24), y),
                    Width = comboW,
                };
                foreach (string key in PlaceKeys)
                    row.Place.Items.Add(L.T("hue_pos_" + key));
                row.Place.SelectedIndex = PlaceFromSectors(lamp.ScreenSectors, lamp.InGroup);
                row.Place.Enabled = row.Check.Checked;
                row.Check.CheckedChanged += (s, e) =>
                {
                    row.Place.Enabled = row.Check.Checked;
                    UpdateState();
                };
                pnlLamps.Controls.Add(row.Check);
                pnlLamps.Controls.Add(row.Place);
                rows.Add(row);
                y += rowH;
            }
            if (lamps.Count == 0)
            {
                lblStatus.ForeColor = Theme.Destructive;
                lblStatus.Text = L.T("hue_lamps_none");
            }
        }

        /// <summary>The place whose sectors match the lamp exactly, otherwise Automatic.</summary>
        private static int PlaceFromSectors(byte[] sectors, bool inGroup)
        {
            if (!inGroup || sectors == null)
                return 0;
            for (int p = 1; p < PlaceSectors.Length; p++)
            {
                bool match = true;
                for (int i = 0; i < sectors.Length && match; i++)
                    match = (sectors[i] == 1) == Array.IndexOf(PlaceSectors[p], i) >= 0;
                if (match)
                    return p;
            }
            return 0;
        }

        /// <summary>Monitor the group already runs on, else the selected monitor, else the first one.</summary>
        private void SelectMonitor()
        {
            CDeviceHueEntertainment group = HuePcGroup.FindDevice();
            int index = -1;
            for (int i = 0; i < monitors.Count && group != null; i++)
                if (monitors[i].GetDevices().Contains(group))
                    index = i;
            if (index < 0 && mainForm?.selectedAmbiScreen != null)
                index = monitors.IndexOf(mainForm.selectedAmbiScreen);
            if (index < 0 && monitors.Count > 0)
                index = 0;
            cbMonitor.SelectedIndex = index + 1;
        }

        private void UpdateState()
        {
            int n = 0;
            foreach (Row r in rows)
                if (r.Check.Checked) n++;
            if (rows.Count == 0)
                return;
            bool tooMany = n > HuePcGroup.MaxLights;
            lblStatus.ForeColor = tooMany ? Theme.Destructive : Theme.SecondaryText;
            lblStatus.Text = tooMany ? L.F("hue_new_too_many", HuePcGroup.MaxLights) : L.F("hue_lamps_selected", n);
            cmdOK.Enabled = !tooMany;
        }

        /// <summary>Automatic places for the lamps without a fixed place: left and right first, then top, then bottom.</summary>
        internal static int[] AutoPlaces(int count)
        {
            switch (count)
            {
                case 0: return new int[0];
                case 1: return new[] { 3 };                          // top
                case 2: return new[] { 1, 5 };                       // left, right
                case 3: return new[] { 1, 3, 5 };                    // left, top, right
                case 4: return new[] { 1, 3, 5, 7 };                 // left, top, right, bottom
                default:
                    int[] order = { 1, 2, 3, 4, 5, 6, 7, 8 };        // around the screen
                    var result = new int[count];
                    for (int i = 0; i < count; i++)
                        result[i] = order[i % order.Length];
                    return result;
            }
        }

        private void cmdOK_Click(object sender, EventArgs e)
        {
            // Members and their places
            var chosen = new List<Row>();
            foreach (Row r in rows)
            {
                r.Lamp.InGroup = r.Check.Checked;
                if (r.Check.Checked)
                    chosen.Add(r);
            }
            var autoRows = chosen.FindAll(r => r.Place.SelectedIndex == 0);
            int[] auto = AutoPlaces(autoRows.Count);
            foreach (Row r in chosen)
            {
                int place = r.Place.SelectedIndex == 0 ? auto[autoRows.IndexOf(r)] : r.Place.SelectedIndex;
                var sectors = new byte[r.Lamp.ScreenSectors?.Length ?? 12];
                foreach (int i in PlaceSectors[place])
                    if (i < sectors.Length)
                        sectors[i] = 1;
                r.Lamp.ScreenSectors = sectors;
            }

            Cursor = Cursors.WaitCursor;
            Enabled = false;
            try
            {
                // Create or update the group now, so it can be assigned to the monitor right away
                CDeviceHueEntertainment group = HuePcGroup.FindDevice();
                if (chosen.Count > 0 && (group == null || !group.IsThreadRunningSafe()))
                {
                    HuePcGroup.Layout layout = HuePcGroup.Configure(HuePcGroup.Members(), true);
                    group = HuePcGroup.EnsureDevice(layout.Area);
                }
                HuePcGroup.MembershipChanged();   // a running group picks the change up by itself

                int monitor = cbMonitor.SelectedIndex - 1;
                if (group != null && monitor >= 0 && monitor < monitors.Count)
                    mainForm?.AssignDeviceToMonitor(group, monitors[monitor]);
                mainForm?.RefreshHueLampChecks();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput("frmHueLamps|OK: " + ex.Message);
                lblStatus.ForeColor = Theme.Destructive;
                lblStatus.Text = HueClipV2.IsAreaLimitError(ex) ? L.T("hue_area_limit") : L.F("hue_area_err", ex.Message);
            }
            finally
            {
                Cursor = Cursors.Default;
                Enabled = true;
            }
        }
    }
}
