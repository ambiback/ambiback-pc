using AmbiBack.PhilipsHue;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

// Custom
using System.Threading;

namespace AmbiBack
{
    public partial class frmConnectWifi : Form
    {
        // --- Needed for interaction
        public frmMain m_ParentForm;

        public CDevice myDevice = null;

        public frmConnectWifi()
        {
            InitializeComponent();
        }

        private void cmdOK_Click(object sender, EventArgs e)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                if (myDevice != null)
                {
                    if (txtWifiSSID.Text.Length > 0 && txtWifiKey.Text.Length > 0)
                    {
                        if (myDevice.GetType().ToString() == "AmbiBack.CDeviceAmbiBack")
                        {
                            CDeviceAmbiBack ambiBackDevice = (CDeviceAmbiBack)myDevice;

                            // Payload helpers 
                            MemoryStream msPacket = new MemoryStream();
                            BinaryWriter bwPacket = new BinaryWriter(msPacket);

                            // Write Byte array
                            bwPacket.Write("%WIFI:");
                            bwPacket.Write(txtWifiSSID.Text);
                            bwPacket.Write((byte)0x00);
                            bwPacket.Write(txtWifiKey.Text);
                            bwPacket.Write((byte)0x00);

                            // Send byte area to serial
                            byte[] bData = msPacket.ToArray();

                            ambiBackDevice.WriteSerialCommand(bData);

                            // Give time to process DATA!
                            Thread.Sleep(2000);

                            DialogResult ds = MessageBox.Show("Successfully send Wifi network data to device '" + myDevice.GetDeviceName() + "'.\r\n", "AmbiBack PC",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
                DialogResult ds = MessageBox.Show("Failed to send Wifi network data to device '" + myDevice.GetDeviceName() + "'.\r\n", "AmbiBack PC",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }

            // And close the window
            this.Close();
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
