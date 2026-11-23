using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AmbiBack
{
    public partial class frmDeviceDreamScreen : Form
    {
        public CDeviceDreamScreen dsDevice = null;

        public frmDeviceDreamScreen()
        {
            InitializeComponent();
        }

        private void cmdOk_Click(object sender, EventArgs e)
        {
            this.Close(); 
        }

        private void LoadSettingsFromDevice()
        {
            txtName.Text = dsDevice.GetDeviceName();
            txtIpAddress.Text = dsDevice.GetIPAddress().ToString();
            txtGroupName.Text = dsDevice.GroupName;

            // Init Textboxes
            txtInstallationType.Text = "";
            txtLEDPixelsVert.Text = "";
            txtLEDPixelsHori.Text = "";
            txtLedPortPixels1.Text = "";
            txtLedPortPixels2.Text = "";
            txtLedPortSetup1.Text = "";
            txtLedPortSetup2.Text = "";

            // Fill values
            txtInstallationType.Text = CGlobals.INSTALLATIONTYPES[dsDevice.InstallationType];

            if (dsDevice.InstallationType == (byte)DsDeviceSkuSetup.Classic)
            {
                txtLEDPixelsVert.Text = "8";
                txtLEDPixelsHori.Text = "16";
                txtLedPortPixels1.Text = "48";
                txtLedPortPixels2.Text = "48";

                txtLedPortSetup1.Text = CGlobals.FLEXSETUPS[dsDevice.FlexSetupPort1];
                txtLedPortSetup2.Text = CGlobals.FLEXSETUPS[dsDevice.FlexSetupPort2];
            }
            else if (dsDevice.InstallationType == (byte)DsDeviceSkuSetup.Mega)
            {
                txtLEDPixelsVert.Text = "12";
                txtLEDPixelsHori.Text = "24";
                txtLedPortPixels1.Text = "72";
                txtLedPortPixels2.Text = "72";
                txtLedPortSetup1.Text = CGlobals.FLEXSETUPS[dsDevice.FlexSetupPort1];
                txtLedPortSetup2.Text = CGlobals.FLEXSETUPS[dsDevice.FlexSetupPort2];
            }
            else if (dsDevice.InstallationType == (byte)DsDeviceSkuSetup.Xtreme)
            {
                txtLEDPixelsVert.Text = "16";
                txtLEDPixelsHori.Text = "32";
                txtLedPortPixels1.Text = "96";
                txtLedPortPixels2.Text = "96";
                txtLedPortSetup1.Text = CGlobals.FLEXSETUPS[dsDevice.FlexSetupPort1];
                txtLedPortSetup2.Text = CGlobals.FLEXSETUPS[dsDevice.FlexSetupPort2];
            }
            else
            {
                // FLEX SETUP
                txtLEDPixelsVert.Text = dsDevice.PixelVert.ToString();
                txtLEDPixelsHori.Text = dsDevice.PixelHori.ToString();
                txtLedPortPixels1.Text = dsDevice.PixelTotalPort1.ToString();
                txtLedPortPixels2.Text = dsDevice.PixelTotalPort2.ToString();
                txtLedPortSetup1.Text = CGlobals.FLEXSETUPS[dsDevice.FlexSetupPort1];
                txtLedPortSetup2.Text = CGlobals.FLEXSETUPS[dsDevice.FlexSetupPort2];
            }

        }

        private void frmDeviceDreamScreen_Load(object sender, EventArgs e)
        {
            if (dsDevice != null)
                LoadSettingsFromDevice(); 

        }
    }
}
