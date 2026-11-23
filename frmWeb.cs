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

        private void cbVideoList_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Set WebPage Content
            webBrowser.DocumentText = string.Format("<html><head><title></title></head><body>{0}</body></html>",
                "<iframe width=\"100%\" height=\"100%\" " +
                "   src=\"https://www.youtube.com/embed/videoseries?list=" + VIDEOLISTIDS[cbVideoList.SelectedIndex] + "\" " +
                "   frameborder=\"0\" allow=\"accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture\" allowfullscreen></iframe>");
        }

        private void frmWeb_Shown(object sender, EventArgs e)
        {
            // Trigger load
            cbVideoList_SelectedIndexChanged(sender, e);
        }
        
    }
}
