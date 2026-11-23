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
            if (iDeviceID == (int)DeviceType.AmbiBackController) {
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
                        DialogResult ds = MessageBox.Show("The device '" + txtDevice.Text + "' could not be resolved to a valid network address (IP).\r\n", "AmbiBack PC",
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
                        DialogResult ds = MessageBox.Show("The device '" + txtDevice.Text + "' could not be setup correctly, invalid IP address!", "AmbiBack PC",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    this.Cursor = Cursors.Default;
                    DialogResult ds = MessageBox.Show("The device '" + txtDevice.Text + "' could not be setup correctly: \r\n" + ex.Message, "AmbiBack PC",
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

            cbDeviceType.DataSource = arDevices;
            cbDeviceType.DisplayMember = "LongName";

            this.chkAutoDiscover_CheckedChanged(sender, e); 
        }
    }
}
