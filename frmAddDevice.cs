using AmbiBack.PhilipsHue;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AmbiBack
{
    public partial class frmAddDevice : Form
    {
        // --- Needed for interaction
        public frmMain m_ParentForm;

        public frmAddDevice()
        {
            InitializeComponent();
            ApplyStyle();
        }

        private void cmdOK_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            this.Enabled = false;

            string szIP = "";
            string szDNSName = "";
            IPAddress ipAddress = null; 

            // Get Selected DeviceID
            int iDeviceID = ((ListBoxDevice)cbDeviceType.SelectedItem).ID;

            // Try to create Device
            CDevice newDevice = null;

            // WLED: chosen as type, or answering /json/info when the type is detected automatically
            if (iDeviceID == (int)DeviceType.WledDdp || chkAutoDiscover.Checked)
            {
                IPAddress wledAddress = ResolveAddress(txtDevice.Text);
                if (wledAddress != null)
                {
                    CDeviceWled.WledInfo info = CDeviceWled.QueryInfo(wledAddress);
                    if (info != null)
                        newDevice = CDeviceWled.FromInfo(info);
                    else if (iDeviceID == (int)DeviceType.WledDdp)
                        newDevice = new CDeviceWled(txtDevice.Text, wledAddress);   // other DDP receiver
                }
                else if (iDeviceID == (int)DeviceType.WledDdp)
                {
                    this.Cursor = Cursors.Default;
                    this.Enabled = true;
                    MessageBox.Show(this, L.F("add_err_resolve", txtDevice.Text), L.T("add_err_resolve_title"),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // Govee: chosen as type, or answering the LAN scan when the type is detected automatically
            if (newDevice == null && (iDeviceID == (int)DeviceType.GoveeLan || chkAutoDiscover.Checked))
            {
                IPAddress goveeAddress = ResolveAddress(txtDevice.Text);
                CDeviceGovee.GoveeInfo info = goveeAddress != null ? CDeviceGovee.QueryInfo(goveeAddress) : null;
                if (info != null)
                    newDevice = CDeviceGovee.FromInfo(info);
                else if (iDeviceID == (int)DeviceType.GoveeLan)
                {
                    this.Cursor = Cursors.Default;
                    this.Enabled = true;
                    if (goveeAddress == null)
                        MessageBox.Show(this, L.F("add_err_resolve", txtDevice.Text), L.T("add_err_resolve_title"),
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    else
                        MessageBox.Show(this, L.F("add_err_govee", txtDevice.Text), L.T("add_err_govee_title"),
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // OpenRGB: chosen as type, or an SDK server answering when the type is detected automatically;
            // every controller of the server is added
            if (newDevice == null && (iDeviceID == (int)DeviceType.OpenRgb || chkAutoDiscover.Checked))
            {
                IPAddress openRgbAddress = ResolveAddress(string.IsNullOrWhiteSpace(txtDevice.Text) ? "127.0.0.1" : txtDevice.Text);
                List<CDeviceOpenRgb.ControllerInfo> controllers = openRgbAddress != null
                    ? CDeviceOpenRgb.QueryControllers(openRgbAddress) : new List<CDeviceOpenRgb.ControllerInfo>();
                if (controllers.Count == 0 && iDeviceID == (int)DeviceType.OpenRgb)
                {
                    this.Cursor = Cursors.Default;
                    this.Enabled = true;
                    MessageBox.Show(this, L.F("add_err_openrgb", txtDevice.Text), L.T("add_err_openrgb_title"),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                for (int i = 1; i < controllers.Count; i++)
                {
                    CDevice extra = CGlobals.mainConfig.Devices.Add(CDeviceOpenRgb.FromInfo(controllers[i], openRgbAddress));
                    if (extra != null)
                        m_ParentForm.DiscoveredDevice(sender, e, extra);
                }
                if (controllers.Count > 0)
                    newDevice = CDeviceOpenRgb.FromInfo(controllers[0], openRgbAddress);
            }

            // Nanoleaf: pairing with the power button; without an address the first unpaired one found by mDNS
            if (newDevice == null && iDeviceID == (int)DeviceType.Nanoleaf)
            {
                newDevice = PairNanoleaf(txtDevice.Text);
                if (newDevice == null)
                {
                    this.Cursor = Cursors.Default;
                    this.Enabled = true;
                    return;
                }
            }

            if (newDevice == null && iDeviceID == (int)DeviceType.AmbiBackController) {
                // Try to resolv and PING the entry first before adding it
                try
                {
                    IPHostEntry myIPHost = Dns.GetHostEntry(txtDevice.Text);
                    szDNSName = myIPHost.HostName.ToString();
                    ipAddress = myIPHost.AddressList[0];
                    szIP = ipAddress.ToString();
                }
                catch (Exception ex)
                {
                    CGlobals.DebugOutput(ex);

                    // Try to get IP address
                    IPAddress.TryParse(txtDevice.Text, out ipAddress); 

                    if (ipAddress == null)
                    {
                        this.Cursor = Cursors.Default;
                        DialogResult ds = MessageBox.Show(this, L.F("add_err_resolve", txtDevice.Text), L.T("add_err_resolve_title"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        return;
                    }
                }

                try
                {
                    if (ipAddress != null)
                    {
                        // Reached here so create a new default HostEntry
                        newDevice = new CDeviceAmbiBack(DeviceType.AmbiBackController, szIP);
                        newDevice.SetIPAddress(ipAddress);
                    } else {
                        DialogResult ds = MessageBox.Show(this, L.F("add_err_ip", txtDevice.Text), L.T("add_err_ip_title"),
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    this.Cursor = Cursors.Default;
                    DialogResult ds = MessageBox.Show(this, L.F("add_err", txtDevice.Text, ex.Message) + "\r\n\r\n" + L.T("add_err_q"), L.T("add_err_title"),
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);

                    if (ds == DialogResult.Cancel)
                        return;
                    else if (ds == DialogResult.No)
                    {
                        this.Close();
                        return;
                    }
                    else
                    {
                        // Continue
                        szDNSName = txtDevice.Text;
                    }
                }

            }
            else if (iDeviceID == (int)DeviceType.PhilipsHueColorLight) {
                Light hueLight = CGlobals.GetHueLightByUniqueID(txtDevice.Text);
                if (hueLight != null) {
                    // Reached here so create a new default HostEntry
                    newDevice = new CDevicePhilipsHueLight(DeviceType.PhilipsHueColorLight, hueLight.Name);
                    // Set Hue Specific parameters
                    CDevicePhilipsHueLight philipsDevice = (CDevicePhilipsHueLight)newDevice;
                    philipsDevice.SetHueUniqueID(hueLight.UniqueID);
                    philipsDevice.HueType = hueLight.Type;
                    philipsDevice.HueModelID = hueLight.ModelID; 
                }
            }

            // Add if created
            if (newDevice != null) {
                // Create Service ID now!
                newDevice.SetDeviceID();
                newDevice.CheckIsOnline(); 

                // Ádd device to Main config
                newDevice = CGlobals.mainConfig.Devices.Add(newDevice);
                if (newDevice != null)
                {
                    // Add to mainform
                    m_ParentForm.DiscoveredDevice(sender, e, newDevice);
                }
            }

            this.Cursor = Cursors.Default;
            this.Enabled = true;

            // And close the window
            this.Close();
        }

        /// <summary>
        /// Pairs with a Nanoleaf (power button) and reads its layout; null when cancelled or not found
        /// (a message was shown).
        /// </summary>
        private CDeviceNanoleaf PairNanoleaf(string address)
        {
            IPAddress ip;
            if (string.IsNullOrWhiteSpace(address))
            {
                // The first Nanoleaf found that is not in the list yet
                var known = new HashSet<string>();
                foreach (CDevice d in CGlobals.mainConfig.Devices.GetDeviceList())
                    if (d is CDeviceNanoleaf n && n.GetIPAddress() != null)
                        known.Add(n.GetIPAddress().ToString());
                ip = CDeviceNanoleaf.Discover().FirstOrDefault(a => !known.Contains(a.ToString()));
            }
            else
                ip = ResolveAddress(address);
            if (ip == null)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show(this, L.F("add_err_nanoleaf", address), L.T("add_err_nanoleaf_title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            this.Cursor = Cursors.Default;
            if (MessageBox.Show(this, L.F("add_nanoleaf_pair", ip), L.T("add_nanoleaf_title"), MessageBoxButtons.OKCancel, MessageBoxIcon.Information) != DialogResult.OK)
                return null;
            try
            {
                while (true)
                {
                    this.Cursor = Cursors.WaitCursor;
                    string token = CDeviceNanoleaf.Pair(ip);
                    this.Cursor = Cursors.Default;
                    if (token != null)
                    {
                        CDeviceNanoleaf.NanoleafInfo info = CDeviceNanoleaf.QueryInfo(ip, token);
                        if (info != null)
                            return CDeviceNanoleaf.FromInfo(info, ip, token);
                    }
                    if (MessageBox.Show(this, L.F("add_nanoleaf_retry", ip), L.T("add_nanoleaf_title"), MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning) != DialogResult.Retry)
                        return null;
                }
            }
            catch (System.IO.IOException)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show(this, L.F("add_err_nanoleaf", ip), L.T("add_err_nanoleaf_title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
        }

        /// <summary>IPv4 address of an address or host name, null when it cannot be resolved.</summary>
        private static IPAddress ResolveAddress(string szAddress)
        {
            szAddress = (szAddress ?? "").Trim();
            if (IPAddress.TryParse(szAddress, out IPAddress ip))
                return ip;
            try
            {
                return Dns.GetHostEntry(szAddress).AddressList
                    .FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            }
            catch (Exception ex) when (ex is System.Net.Sockets.SocketException || ex is ArgumentException)
            {
                return null;
            }
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void chkAutoDiscover_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAutoDiscover.Checked)
            {
                this.cbDeviceType.Enabled = false;
                this.lblDeviceType.Enabled = false;
            }
            else
            {
                this.cbDeviceType.Enabled = true;
                this.lblDeviceType.Enabled = true;
            }
        }

        private void AddDevice_Load(object sender, EventArgs e)
        {
            ArrayList arDevices = new ArrayList();

            arDevices.Add(new ListBoxDevice(CGlobals.DEVICETYPESDISPLAYNAME[(int)DeviceType.AmbiBackController],
                                            CGlobals.DEVICETYPES[(int)DeviceType.AmbiBackController], 
                                            (int)DeviceType.AmbiBackController ));
            arDevices.Add(new ListBoxDevice(CGlobals.DEVICETYPESDISPLAYNAME[(int)DeviceType.PhilipsHueColorLight],
                                            CGlobals.DEVICETYPES[(int)DeviceType.PhilipsHueColorLight],
                                            (int)DeviceType.PhilipsHueColorLight));
            arDevices.Add(new ListBoxDevice(CGlobals.DEVICETYPESDISPLAYNAME[(int)DeviceType.WledDdp],
                                            CGlobals.DEVICETYPES[(int)DeviceType.WledDdp],
                                            (int)DeviceType.WledDdp));
            arDevices.Add(new ListBoxDevice(CGlobals.DEVICETYPESDISPLAYNAME[(int)DeviceType.GoveeLan],
                                            CGlobals.DEVICETYPES[(int)DeviceType.GoveeLan],
                                            (int)DeviceType.GoveeLan));
            arDevices.Add(new ListBoxDevice(CGlobals.DEVICETYPESDISPLAYNAME[(int)DeviceType.OpenRgb],
                                            CGlobals.DEVICETYPES[(int)DeviceType.OpenRgb],
                                            (int)DeviceType.OpenRgb));
            arDevices.Add(new ListBoxDevice(CGlobals.DEVICETYPESDISPLAYNAME[(int)DeviceType.Nanoleaf],
                                            CGlobals.DEVICETYPES[(int)DeviceType.Nanoleaf],
                                            (int)DeviceType.Nanoleaf));

            cbDeviceType.DataSource = arDevices;
            cbDeviceType.DisplayMember = "LongName";

            this.chkAutoDiscover_CheckedChanged(sender, e); 
        }
    }
}
