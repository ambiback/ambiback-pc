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
    public partial class frmConnectHue : Form
    {
        // --- Needed for interaction
        public frmMain m_ParentForm;

        public frmConnectHue()
        {
            InitializeComponent();
            ApplyStyle();
        }

        private void cmdOK_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            this.Enabled = false;

            string szHueDeviceIP = txtDevice.Text;

            try
            {
                // Init Hue Configuration
                Configuration.DeviceIP = System.Net.IPAddress.Parse(szHueDeviceIP);
                Configuration.AppID = CGlobals.szHueAppID;

                if (Configuration.RegisterNewUser())
                {
                    // Save on Success!
                    Properties.Settings.Default.szHueDeviceIP = szHueDeviceIP;
                    Properties.Settings.Default.szHueUsername = Configuration.Username;
                    Properties.Settings.Default.Save();
                }

                // Init HUE SubSystem
                CGlobals.mainConfig.InitHueDevices();

                // Add HUE Devices
                CGlobals.deviceController.EnumHueDevices(); 

                // And close the window
                this.Close();
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                DialogResult ds = MessageBox.Show(this, L.F("hue_err", txtDevice.Text, ex.Message), L.T("hue_err_title"),
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
        }
    }
}
