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

            string szHueDeviceIP = txtDevice.Text.Trim();

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
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show(this, L.F("hue_err", txtDevice.Text, ex.Message), L.T("hue_err_title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally {
                this.Cursor = Cursors.Default;
                this.Enabled = true;
            }
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void frmConnectHue_Load(object sender, EventArgs e)
        {
            txtDevice.Text = Properties.Settings.Default.szHueDeviceIP;
            UpdateHueStatus(null);
            ShowAreas(CGlobals.HueEntertainmentAreas);
            if (CanStream())
                RefreshAreas();

            statusTimer = new Timer { Interval = 1000 };
            statusTimer.Tick += (s, a) => UpdateDeviceColumn();
            statusTimer.Start();
            FormClosed += (s, a) => statusTimer.Dispose();
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
            cmdAddArea.Enabled = CanStream() && lvAreas.SelectedItems.Count > 0;
        }

        private void RefreshAreas()
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
        }
    }
}
