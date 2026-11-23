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
    public partial class frmOptions : Form
    {
        public frmOptions()
        {
            InitializeComponent();
        }

        private void cmdOK_Click(object sender, EventArgs e)
        {
            // Trigger save settings
            Properties.Settings.Default.Save();

            // Handle Windows Startup control!
            CGlobals.SetLoadWindowsStartup();

            this.Close();
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void frmOptions_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.szDebugFileLog.Length == 0) 
                Properties.Settings.Default.szDebugFileLog = Environment.CurrentDirectory + "\\debug.log";
#if (DEBUG)
#else
            // Hide Debug Options
            this.gbDebug.Visible = false;
#endif
        }
    }
}
