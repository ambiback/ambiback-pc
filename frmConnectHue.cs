using AmbiBack.PhilipsHue;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AmbiBack
{
    public partial class frmConnectHue : Form
    {
        // --- Needed for interaction
        public frmMain m_ParentForm;

        private List<EntertainmentArea> areas = new List<EntertainmentArea>();
        private Timer statusTimer;
        private bool loadingAreas;

        public frmConnectHue()
        {
            InitializeComponent();
            ApplyStyle();
        }

        private void cmdOK_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            this.Enabled = false;

            string szHueDeviceIP = BridgeAddress();

            try
            {
                // Init Hue Configuration
                Configuration.DeviceIP = System.Net.IPAddress.Parse(szHueDeviceIP);
                Configuration.AppID = CGlobals.szHueAppID;

                if (Configuration.RegisterNewUser())
                {
                    // Save on Success! The client key (streaming PSK) is stored encrypted for this Windows user.
                    Properties.Settings.Default.szHueDeviceIP = szHueDeviceIP;
                    Properties.Settings.Default.szHueUsername = Configuration.Username;
                    Properties.Settings.Default.szHueClientKey = HueSecrets.Protect(Configuration.ClientKey);
                    Properties.Settings.Default.Save();
                }

                // Init HUE SubSystem
                CGlobals.mainConfig.InitHueDevices();

                // Add HUE Devices
                CGlobals.deviceController.EnumHueDevices();

                // Stay open: the entertainment areas can be chosen now
                UpdateHueStatus(null);
                ShowAreas(CGlobals.HueEntertainmentAreas);

                // Next step: which lights light up, where, and on which monitor
                if (CanStream())
                    BeginInvoke((Action)(() => ChooseLamps()));
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show(this, L.F("hue_err", cbBridge.Text, ex.Message), L.T("hue_err_title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally {
                this.Cursor = Cursors.Default;
                this.Enabled = true;
            }
        }

        /// <summary>Opens the light assistant (members of the PC group, places, monitor).</summary>
        private void ChooseLamps()
        {
            using (var dlg = new frmHueLamps(m_ParentForm))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    RefreshAreas();
            }
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void frmConnectHue_Load(object sender, EventArgs e)
        {
            cbBridge.Text = Properties.Settings.Default.szHueDeviceIP;
            // First time: look for bridges right away
            if (string.IsNullOrWhiteSpace(cbBridge.Text))
                SearchBridges(true);
            UpdateHueStatus(null);
            ShowAreas(CGlobals.HueEntertainmentAreas);
            if (CanStream())
                RefreshAreas();

            statusTimer = new Timer { Interval = 1000 };
            statusTimer.Tick += (s, a) => UpdateDeviceColumn();
            statusTimer.Start();
            FormClosed += (s, a) => statusTimer.Dispose();
        }

        /// <summary>Address from the field: a found bridge ("Name (192.168.0.2)") or what was typed.</summary>
        private string BridgeAddress()
        {
            if (cbBridge.SelectedItem is DiscoveredBridge bridge && cbBridge.Text == bridge.ToString())
                return bridge.Address.ToString();
            string text = cbBridge.Text.Trim();
            var m = System.Text.RegularExpressions.Regex.Match(text, @"\(([^()]+)\)\s*$");
            return m.Success ? m.Groups[1].Value.Trim() : text;
        }

        private bool searching;

        /// <summary>
        /// Looks for Hue bridges on the network (mDNS and ARP, no cloud) and puts them into the list.
        /// With several bridges the list opens so one can be chosen.
        /// </summary>
        private void SearchBridges(bool automatic)
        {
            if (searching)
                return;
            searching = true;
            cmdSearch.Enabled = false;
            Theme.StyleHint(label2);
            label2.Text = L.T("hue_searching");
            Task.Run(() => HueDiscovery.Discover(alwaysCloud: !automatic)).ContinueWith(t =>
            {
                if (IsDisposed)
                    return;
                BeginInvoke((Action)(() =>
                {
                    searching = false;
                    cmdSearch.Enabled = true;
                    List<DiscoveredBridge> found = t.IsFaulted ? new List<DiscoveredBridge>() : t.Result;
                    if (t.IsFaulted)
                        CGlobals.DebugOutput("frmConnectHue|Search: " + t.Exception.GetBaseException().Message);

                    string typed = cbBridge.Text;
                    cbBridge.BeginUpdate();
                    cbBridge.Items.Clear();
                    foreach (DiscoveredBridge b in found)
                        cbBridge.Items.Add(b);
                    cbBridge.EndUpdate();

                    if (found.Count == 0)
                    {
                        label2.Text = L.T("hue_found_none");
                        cbBridge.Text = typed;
                        return;
                    }
                    label2.Text = found.Count == 1 ? L.F("hue_found_one", found[0]) : L.F("hue_found_many", found.Count);
                    label2.ForeColor = Theme.Success;

                    // Keep an address the user typed in meanwhile, unless it is one of the found bridges
                    DiscoveredBridge current = found.Find(b => b.Address.ToString() == typed.Trim());
                    if (current != null || string.IsNullOrWhiteSpace(typed) || !automatic)
                        cbBridge.SelectedItem = current ?? found[0];
                    else
                        cbBridge.Text = typed;
                    if (found.Count > 1)
                    {
                        cbBridge.Focus();
                        cbBridge.DroppedDown = true;
                    }
                }));
            }, TaskScheduler.Default);
        }

        private static bool CanStream()
        {
            return CGlobals.bHueConnected && !string.IsNullOrEmpty(Configuration.ClientKey);
        }

        /// <summary>Pairing state of the bridge; error is shown instead when given.</summary>
        private void UpdateHueStatus(string error)
        {
            if (error != null)
            {
                lblHueStatus.Text = L.F("hue_area_err", error);
                lblHueStatus.ForeColor = Theme.Destructive;
            }
            else if (!CGlobals.bHueConnected)
            {
                lblHueStatus.Text = L.T("hue_st_not_paired");
                lblHueStatus.ForeColor = Theme.SecondaryText;
            }
            else if (string.IsNullOrEmpty(Configuration.ClientKey))
            {
                lblHueStatus.Text = L.T("hue_st_no_key");
                lblHueStatus.ForeColor = Theme.Warning;
            }
            else
            {
                lblHueStatus.Text = L.T("hue_st_ready");
                lblHueStatus.ForeColor = Theme.Success;
            }
            cmdRefresh.Enabled = CanStream() && !loadingAreas;
            cmdNewArea.Enabled = CanStream();
            cmdLamps.Enabled = CanStream();
            cmdDeleteArea.Enabled = CanStream() && lvAreas.SelectedItems.Count > 0;
            cmdAddArea.Enabled = CanStream() && lvAreas.SelectedItems.Count > 0;
        }

        private void RefreshAreas(Action<List<EntertainmentArea>> loaded = null)
        {
            if (loadingAreas)
                return;
            loadingAreas = true;
            cmdRefresh.Enabled = false;
            Task.Run(() => HueClipV2.GetEntertainmentAreas()).ContinueWith(t =>
            {
                if (IsDisposed)
                    return;
                BeginInvoke((Action)(() =>
                {
                    loadingAreas = false;
                    if (t.IsFaulted)
                    {
                        Exception ex = t.Exception.GetBaseException();
                        CGlobals.DebugOutput("frmConnectHue|RefreshAreas: " + ex.Message);
                        UpdateHueStatus(ex.Message);
                        return;
                    }
                    CGlobals.HueEntertainmentAreas = t.Result;
                    CGlobals.deviceController.EnumHueEntertainmentAreas();
                    ShowAreas(t.Result);
                    UpdateHueStatus(null);
                    loaded?.Invoke(t.Result);
                }));
            }, TaskScheduler.Default);
        }

        private void ShowAreas(List<EntertainmentArea> list)
        {
            areas = list ?? new List<EntertainmentArea>();
            lvAreas.BeginUpdate();
            lvAreas.Items.Clear();
            foreach (EntertainmentArea area in areas)
            {
                var item = new ListViewItem(area.Name) { Tag = area };
                item.SubItems.Add(area.Channels.Count.ToString());
                item.SubItems.Add(area.IsActive ? L.T("hue_area_active") : L.T("hue_area_idle"));
                item.SubItems.Add("");
                lvAreas.Items.Add(item);
            }
            lvAreas.EndUpdate();
            lblNoAreas.Visible = CanStream() && areas.Count == 0;
            UpdateDeviceColumn();
        }

        /// <summary>AmbiBack column: added or not, and the live streaming state of the device.</summary>
        private void UpdateDeviceColumn()
        {
            foreach (ListViewItem item in lvAreas.Items)
            {
                EntertainmentArea area = (EntertainmentArea)item.Tag;
                CDeviceHueEntertainment device = CGlobals.deviceController.FindHueEntertainmentDevice(area.ID);
                string text = device == null ? ""
                    : device.IsThreadRunningSafe() && !string.IsNullOrEmpty(device.StreamStatus) ? device.StreamStatus
                    : L.T("hue_area_added");
                if (item.SubItems[3].Text != text)
                    item.SubItems[3].Text = text;
            }
        }

        private void cmdRefresh_Click(object sender, EventArgs e)
        {
            RefreshAreas();
        }

        /// <summary>Creates an own area from single lights and adds it as device right away.</summary>
        private void cmdNewArea_Click(object sender, EventArgs e)
        {
            using (var dlg = new frmHueCreateArea())
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;
                string id = dlg.CreatedAreaId;
                loadingAreas = false;
                RefreshAreas(list =>
                {
                    EntertainmentArea area = list.Find(a => a.ID == id);
                    if (area == null)
                        return;
                    CGlobals.deviceController.AddHueEntertainmentArea(area);
                    foreach (ListViewItem item in lvAreas.Items)
                        item.Selected = ((EntertainmentArea)item.Tag).ID == id;
                    UpdateDeviceColumn();
                });
            }
        }

        private void cmdAddArea_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in lvAreas.SelectedItems)
            {
                EntertainmentArea area = (EntertainmentArea)item.Tag;
                if (area.Channels.Count > HueStreamMessage.MaxChannels)
                    CGlobals.DebugOutput("frmConnectHue|Area has more than 20 channels, only 20 are streamed");
                CGlobals.deviceController.AddHueEntertainmentArea(area);
            }
            UpdateDeviceColumn();
        }

        private void lvAreas_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmdAddArea.Enabled = CanStream() && lvAreas.SelectedItems.Count > 0;
            cmdDeleteArea.Enabled = CanStream() && lvAreas.SelectedItems.Count > 0;
        }

        /// <summary>Deletes the selected areas on the bridge (it holds at most 20) after asking.</summary>
        private void cmdDeleteArea_Click(object sender, EventArgs e)
        {
            var selected = new List<EntertainmentArea>();
            foreach (ListViewItem item in lvAreas.SelectedItems)
                selected.Add((EntertainmentArea)item.Tag);
            if (selected.Count == 0)
                return;
            string names = selected.Count == 1 ? "„" + selected[0].Name + "“" : selected.Count.ToString();
            if (MessageBox.Show(this, L.F("hue_delete_q", names), L.T("hue_delete_title"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            Cursor = Cursors.WaitCursor;
            Enabled = false;
            string error = null;
            try
            {
                foreach (EntertainmentArea area in selected)
                {
                    try
                    {
                        HueClipV2.DeleteEntertainmentArea(area.ID);
                    }
                    catch (Exception ex)
                    {
                        error = ex.Message;
                        CGlobals.DebugOutput("frmConnectHue|Delete " + area.ID + ": " + ex.Message);
                    }
                }
            }
            finally
            {
                Cursor = Cursors.Default;
                Enabled = true;
            }
            RefreshAreas(list => { if (error != null) UpdateHueStatus(error); });
        }
    }
}
