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
    public partial class frmWeb : Form
    {
        public static string[] VIDEOLISTIDS = new string[]
        {
            "PLTxMjvc4vaBkPKYsOgre7DrrOXBpRdbDu",
            "PLTxMjvc4vaBkJQQ5akn_0Rktyt4-v4xns",
            "PLTxMjvc4vaBnZFNw6UvXHVyqBNQeB-jLE",
        };
        public static string[] VIDEOLISTNAMES = new string[]
        {
            "AmbiBack Calibration Videos",
            "AmbiBack Color Demo Videos",
            "AmbiBack Triple Monitor Test Videos",
        };


        public frmWeb()
        {
            InitializeComponent();
            ApplyStyle();
        }

        private void frmWeb_Load(object sender, EventArgs e)
        {
            int screenLeft = SystemInformation.VirtualScreen.Left;
            int screenTop = SystemInformation.VirtualScreen.Top;
            int screenWidth = SystemInformation.VirtualScreen.Width;
            int screenHeight = SystemInformation.VirtualScreen.Height;

            this.Size = new System.Drawing.Size(screenWidth, screenHeight);
            this.Location = new System.Drawing.Point(screenLeft, screenTop);

            // Fill Video Lists
            for (int i = 0; i < VIDEOLISTNAMES.Length; i++)
            {
                cbVideoList.Items.Add(VIDEOLISTNAMES[i]);
            }
            // Select item and trigger select
            cbVideoList.SelectedIndex = 0;
        }

        private void cmdClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void cmdCloseTop_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private bool webViewReady;

        private void cbVideoList_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Navigates once the WebView2 is ready (see frmWeb_Shown)
            if (webViewReady && cbVideoList.SelectedIndex >= 0)
                webBrowser.CoreWebView2.Navigate(WebViewHost.PlaylistUrl(VIDEOLISTIDS[cbVideoList.SelectedIndex]));
        }

        private async void frmWeb_Shown(object sender, EventArgs e)
        {
            string listId = VIDEOLISTIDS[Math.Max(0, cbVideoList.SelectedIndex)];
            if (WebViewHost.RuntimeVersion == null)
            {
                // No Edge WebView2 runtime: open the playlist in the default browser instead
                OpenInBrowser("https://www.youtube.com/playlist?list=" + listId);
                Close();
                return;
            }
            try
            {
                await WebViewHost.InitAsync(webBrowser);
                // The window was closed while WebView2 was starting
                if (IsDisposed || Disposing)
                    return;
                WebViewHost.MapPlayerPage(webBrowser);
                // A video in full screen covers the whole window, including the buttons
                webBrowser.CoreWebView2.ContainsFullScreenElementChanged += (s, a) =>
                {
                    if (webBrowser.CoreWebView2.ContainsFullScreenElement)
                    {
                        webBrowser.Dock = DockStyle.Fill;
                        webBrowser.BringToFront();
                    }
                    else
                    {
                        webBrowser.Dock = DockStyle.None;
                        webBrowser.SetBounds(0, 0, ClientSize.Width, ClientSize.Height);
                        webBrowser.SendToBack();
                    }
                };
                webViewReady = true;
                cbVideoList_SelectedIndexChanged(sender, e);
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput(ex);
                // Closed by the user meanwhile: nothing to fall back to
                if (IsDisposed || Disposing)
                    return;
                OpenInBrowser("https://www.youtube.com/playlist?list=" + listId);
                Close();
            }
        }

        private static void OpenInBrowser(string url)
        {
            try { System.Diagnostics.Process.Start(url); }
            catch (Exception ex) { CGlobals.DebugOutput(ex); }
        }
        
    }
}
