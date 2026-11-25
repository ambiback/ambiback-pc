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
            NavigateToDevice();
        }

        /// <summary>Open the device's web page (http://address) in the current view.</summary>
        private void NavigateToDevice()
        {
            try
            {
                var uri = new Uri("http://" + txtIpAddress.Text);
                var edge = WebViewObj as Microsoft.Web.WebView2.WinForms.WebView2;
                if (edge != null)
                {
                    if (edge.CoreWebView2 != null)
                        edge.CoreWebView2.Navigate(uri.ToString());
                    else
                        edge.Source = uri;   // navigates once the view is initialized
                }
                else if (WebViewObj is WebBrowser)
                    ((WebBrowser)WebViewObj).Navigate(uri);
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
            }
        }

        private async void frmDeviceAmbiBack_Load(object sender, EventArgs e)
        {
            this.Height = Screen.PrimaryScreen.WorkingArea.Height;
            this.Top = 0;

            Control view = null;
            try
            {
                if (WebViewHost.RuntimeVersion != null)
                {
                    // Edge WebView2 (Chromium)
                    var edge = new Microsoft.Web.WebView2.WinForms.WebView2 { Name = "WebView", Dock = DockStyle.Fill };
                    this.WebViewObj = view = edge;
                    this.Controls.Add(edge);
                    edge.BringToFront();
                    await WebViewHost.InitAsync(edge);
                }
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
                if (view != null) { this.Controls.Remove(view); view.Dispose(); view = null; }
            }

            if (view == null)
            {
                // No WebView2 runtime: the old Internet Explorer control as fallback
                try
                {
                    var ie = new WebBrowser { Name = "WebView", Dock = DockStyle.Fill, ScriptErrorsSuppressed = true };
                    this.WebViewObj = ie;
                    this.Controls.Add(ie);
                    ie.BringToFront();
                }
                catch (Exception ex)
                {
                    CGlobals.DebugOutput(ex);
                }
            }

            if (abDevice != null && !IsDisposed)
                LoadSettingsFromDevice();
        }

        private void cmdReload_Click(object sender, EventArgs e)
        {
            NavigateToDevice();
        }

        private void cmdClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
