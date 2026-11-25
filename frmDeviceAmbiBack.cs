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
    public partial class frmDeviceAmbiBack : Form
    {
        public object WebViewObj = null; 

        public CDeviceAmbiBack abDevice = null;

        public frmDeviceAmbiBack()
        {
            InitializeComponent();
            ApplyStyle();
        }

        private void LoadSettingsFromDevice()
        {
            txtName.Text = abDevice.GetDeviceName();
            txtIpAddress.Text = abDevice.GetIPAddress().ToString();

            try
            {
                // Init WebBrowser
                if (WebViewObj != null)
                {
                    if (WebViewObj.GetType().ToString() == "System.Windows.Forms.WebBrowser") {
                        WebBrowser WebView = (WebBrowser)(this.WebViewObj);
                        WebView.Navigate(new Uri("http://" + txtIpAddress.Text));
                    }
                }
                // webView.Source = new Uri("http://" + txtIpAddress.Text);
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
        }

        private void frmDeviceAmbiBack_Load(object sender, EventArgs e)
        {
            // Try to create WebControl
            try
            {
                this.WebViewObj = new System.Windows.Forms.WebBrowser();
                WebBrowser WebView = (WebBrowser)(this.WebViewObj);
                // Basic properties
                WebView.Dock = System.Windows.Forms.DockStyle.Fill;
                WebView.Location = new System.Drawing.Point(0, 25);
                WebView.MinimumSize = new System.Drawing.Size(20, 20);
                WebView.Size = new System.Drawing.Size(640, 480);
                WebView.Name = "WebView";

                this.Controls.Add((Control)WebViewObj);

                // Advanced properties
                WebView.BringToFront();
                WebView.ScriptErrorsSuppressed = true;
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }

            if (abDevice != null)
                LoadSettingsFromDevice();

            this.Height = Screen.PrimaryScreen.WorkingArea.Height;
            this.Top = 0; 
        }

        private void cmdReload_Click(object sender, EventArgs e)
        {
            try
            {
                if (WebViewObj != null)
                {
                    if (WebViewObj.GetType().ToString() == "System.Windows.Forms.WebBrowser")
                    {
                        WebBrowser WebView = (WebBrowser)(this.WebViewObj);
                        WebView.Navigate(new Uri("http://" + txtIpAddress.Text));
                    }
                }
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
        }

        private void cmdClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
