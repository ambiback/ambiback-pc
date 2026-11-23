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
    public partial class frmSplash : Form
    {
        public frmSplash()
        {
            InitializeComponent();
        }

        private void frmSplash_Load(object sender, EventArgs e)
        {
            // Be patient!
            this.Cursor = Cursors.WaitCursor;

            // Force the background Image to be Transparent!
            System.Drawing.Bitmap imgTrans = new System.Drawing.Bitmap(this.BackgroundImage);
            imgTrans.MakeTransparent(imgTrans.GetPixel(1, 1));
            this.BackgroundImage = imgTrans;
            this.BackColor = Color.MidnightBlue;
            this.TransparencyKey = BackColor;
        }
    }
}
