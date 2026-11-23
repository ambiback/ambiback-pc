using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

// Custom
using System.Resources;

namespace AmbiBack
{
    public partial class frmMessageBox : Form
    {
        #region Language helpers
        // Create Form Resource manager
        ComponentResourceManager myResources = new ComponentResourceManager(typeof(frmMessageBox));
        #endregion

        #region Variables for interaction
        public string szTitle = "";
        public string szMessage = "";

        public string szMoreLink = "";
        public string szMoreTitle = "";
        public int iMoreMode = 0;
        /*
         * 0 = link
         * 1 = Button
         * 2 = NONE
         * */

        public bool bOwnerFormAvailable = true;
        public bool bShowAskAgain = true;
        public string szRegValueName = "";
        public MessageBoxIcon myIcon = MessageBoxIcon.Information;
        public MessageBoxButtons myButtons = MessageBoxButtons.OKCancel;	// Default
        #endregion

        public frmMessageBox()
        {
            InitializeComponent();
        }

        #region Public Helpers
        public void SetOwnerFormAvailable(bool bValue)
        {
            // Copy Value
            bOwnerFormAvailable = bValue;

            // Center screen if empty parent
            if (bOwnerFormAvailable)
                this.StartPosition = FormStartPosition.CenterParent;
            else
                this.StartPosition = FormStartPosition.CenterScreen;
        }
        #endregion

        private void cmdMore_Click(object sender, EventArgs e)
        {
            // If the value looks like a URL, navigate to it.
            // Otherwise, display it in a message box.
            if (szMoreLink != "" && szMoreLink.StartsWith("www"))
            {
                System.Diagnostics.Process.Start(szMoreLink);
            }
        }

        private void cmdOK_Click(object sender, EventArgs e)
        {
            try
            {
                // Check for AskAgain
                if (bShowAskAgain && chkAskAgain.Checked)
                {
                    // Set to ONE
                    AmbiBack.Properties.Settings.Default.PropertyValues[szRegValueName].PropertyValue = (System.Int32)1;
                    AmbiBack.Properties.Settings.Default.Save();
                }
            }
            catch (Exception ex)
            {
                CGlobals.HandleException("cmdOK_Click frmMessageBox", ex, true);
            }
            finally
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            // Check for AskAgain
            if (bShowAskAgain && chkAskAgain.Checked)
            {
                // Set to ZERO!
                //TODO				CSettings.SaveSingleRegSettings( szRegValueName, 0 );
            }

            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void frmMessageBox_Load(object sender, EventArgs e)
        {
            // Init all fields
            lblTitleText.Text = szTitle;
            txtMessage.Text = szMessage;
            if (bShowAskAgain)
                chkAskAgain.Visible = true;
            else
            {
                chkAskAgain.Visible = false;

                lblMore.Top += chkAskAgain.Height;
                lblMoreLink.Top = lblMore.Top;
                txtMessage.Height += chkAskAgain.Height;
            }

            // --- Setting a link
            if (szMoreLink == "")
            {
                // Hide
                lblMore.Visible = false;
                lblMoreLink.Visible = false;
                cmdMore.Visible = false;
            }
            else
            {
                if (iMoreMode == 0)
                {
                    // Disable
                    cmdMore.Visible = false;

                    // Set the correct link
                    if (szMoreTitle.Length > 0)
                        lblMoreLink.Text = szMoreTitle;
                    else
                        lblMoreLink.Text = szMoreLink;

                    lblMoreLink.Links.Add(0, lblMoreLink.Text.Length, szMoreLink);
                }
                else if (iMoreMode == 1)
                {
                    // Disable
                    lblMore.Visible = false;
                    lblMoreLink.Visible = false;

                    // Set the correct link
                    cmdMore.Text = this.szMoreTitle;
                }
                else if (iMoreMode == 2)
                {
                    // Disable
                    cmdMore.Visible = false;
                    lblMore.Visible = false;
                    lblMoreLink.Visible = false;
                }
            }
            // ---

            // --- Check for Buttons
            if (myButtons == MessageBoxButtons.OK)
            {
                if (cmdMore.Visible)
                    cmdMore.Location = cmdOK.Location;

                cmdOK.Visible = true;
                cmdOK.Location = cmdCancel.Location;
                cmdCancel.Visible = false;

                // So cancel will work as well
                this.CancelButton = cmdOK;
                this.AcceptButton = cmdOK;
            }
            else if (myButtons == MessageBoxButtons.OKCancel)
            {
                cmdOK.Visible = true;
                cmdCancel.Visible = true;
            }
            else if (myButtons == MessageBoxButtons.YesNo)
            {
                cmdOK.Visible = true;
                cmdCancel.Visible = true;
                cmdOK.Text = CLanguage.GetStringFromRessource("szYes", "szYes", myResources);
                cmdCancel.Text = CLanguage.GetStringFromRessource("szNo", "szNo", myResources); 
            }
            // ---

            // --- Set MsgBoxIcon
            if (myIcon == MessageBoxIcon.Information)
                pbIcon.Image = ILIcons.Images[0];
            else if (myIcon == MessageBoxIcon.Warning ||
                myIcon == MessageBoxIcon.Exclamation)
                pbIcon.Image = ILIcons.Images[1];
            else if (myIcon == MessageBoxIcon.Error ||
                myIcon == MessageBoxIcon.Asterisk)
                pbIcon.Image = ILIcons.Images[2];
            else if (myIcon == MessageBoxIcon.Question)
                pbIcon.Image = ILIcons.Images[3];
            else
                pbIcon.Image = ILIcons.Images[0]; // Default = info!
            // ---
        }

        private void lblMoreLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            CGlobals.HandleLinkLableClick(sender, e, lblMoreLink);
        }

        private void frmMessageBox_Paint(object sender, PaintEventArgs e)
        {
            // Draw lines 8; 200
            Pen myGrayPen = new Pen(Color.Gray, 1);
            Pen myLightPen = new Pen(Color.White, 1);

            if (chkAskAgain.Visible)
            {
                e.Graphics.DrawLine(myGrayPen, 8.0F, 187.0F, 432.0F, 187.0F);
                e.Graphics.DrawLine(myLightPen, 8.0F, 188.0F, 432.0F, 188.0F);
            }
            else
            {
                e.Graphics.DrawLine(myGrayPen, 8.0F, 205.0F, 432.0F, 205.0F);
                e.Graphics.DrawLine(myLightPen, 8.0F, 206.0F, 432.0F, 206.0F);
            }
        }
    }
}
