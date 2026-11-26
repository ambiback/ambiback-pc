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
        /// <summary>Whether the autostart entry pointed to this copy when the dialog opened.</summary>
        private readonly bool autostartWasThisCopy;

        public frmOptions()
        {
            // The check box shows whether Windows starts this copy, not a setting taken over
            // from another copy; it is bound to the setting, so set that before binding
            autostartWasThisCopy = CGlobals.IsWindowsStartupThisCopy();
            Properties.Settings.Default.bLoadAmbiBackOnWindowsStartup = autostartWasThisCopy;
            InitializeComponent();
        }

        private void cmdOK_Click(object sender, EventArgs e)
        {
            acceptedWithOk = true;

            // Language takes effect at once, the main window updates its texts
            Properties.Settings.Default.szLanguage = SelectedLanguage;
            L.Set(SelectedLanguage);

            // Trigger save settings
            Properties.Settings.Default.Save();

            // Change the Windows autostart only when the user changed the check box here
            if (chkLoadWindowsStartup.Checked != autostartWasThisCopy)
                CGlobals.SetLoadWindowsStartup(chkLoadWindowsStartup.Checked);

            this.Close();
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            // FormClosing restores the bound settings, the dialog was not accepted with OK
            this.Close();
        }

        private void frmOptions_Load(object sender, EventArgs e)
        {
#if (DEBUG)
#else
            // Hide Debug Options
            this.gbDebug.Visible = false;
#endif
            ApplyStyle();
        }
    }
}
