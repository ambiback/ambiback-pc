namespace AmbiBack
{
    partial class frmMessageBox
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMessageBox));
            this.cmdMore = new System.Windows.Forms.Button();
            this.lblTitleText = new System.Windows.Forms.Label();
            this.lblMore = new System.Windows.Forms.Label();
            this.lblMoreLink = new System.Windows.Forms.LinkLabel();
            this.pbIcon = new System.Windows.Forms.PictureBox();
            this.lblTitle = new System.Windows.Forms.Label();
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.chkAskAgain = new System.Windows.Forms.CheckBox();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.cmdOK = new System.Windows.Forms.Button();
            this.ILIcons = new System.Windows.Forms.ImageList(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.pbIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // cmdMore
            // 
            resources.ApplyResources(this.cmdMore, "cmdMore");
            this.cmdMore.Name = "cmdMore";
            this.cmdMore.Click += new System.EventHandler(this.cmdMore_Click);
            // 
            // lblTitleText
            // 
            resources.ApplyResources(this.lblTitleText, "lblTitleText");
            this.lblTitleText.BackColor = System.Drawing.Color.White;
            this.lblTitleText.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.lblTitleText.Name = "lblTitleText";
            // 
            // lblMore
            // 
            resources.ApplyResources(this.lblMore, "lblMore");
            this.lblMore.Name = "lblMore";
            // 
            // lblMoreLink
            // 
            resources.ApplyResources(this.lblMoreLink, "lblMoreLink");
            this.lblMoreLink.Name = "lblMoreLink";
            this.lblMoreLink.TabStop = true;
            this.lblMoreLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblMoreLink_LinkClicked);
            // 
            // pbIcon
            // 
            resources.ApplyResources(this.pbIcon, "pbIcon");
            this.pbIcon.BackColor = System.Drawing.Color.White;
            this.pbIcon.Name = "pbIcon";
            this.pbIcon.TabStop = false;
            // 
            // lblTitle
            // 
            resources.ApplyResources(this.lblTitle, "lblTitle");
            this.lblTitle.BackColor = System.Drawing.Color.White;
            this.lblTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblTitle.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.lblTitle.Name = "lblTitle";
            // 
            // txtMessage
            // 
            resources.ApplyResources(this.txtMessage, "txtMessage");
            this.txtMessage.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.ReadOnly = true;
            // 
            // chkAskAgain
            // 
            resources.ApplyResources(this.chkAskAgain, "chkAskAgain");
            this.chkAskAgain.Name = "chkAskAgain";
            // 
            // cmdCancel
            // 
            resources.ApplyResources(this.cmdCancel, "cmdCancel");
            this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
            // 
            // cmdOK
            // 
            resources.ApplyResources(this.cmdOK, "cmdOK");
            this.cmdOK.Name = "cmdOK";
            this.cmdOK.Click += new System.EventHandler(this.cmdOK_Click);
            // 
            // ILIcons
            // 
            this.ILIcons.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ILIcons.ImageStream")));
            this.ILIcons.TransparentColor = System.Drawing.Color.Transparent;
            this.ILIcons.Images.SetKeyName(0, "");
            this.ILIcons.Images.SetKeyName(1, "");
            this.ILIcons.Images.SetKeyName(2, "");
            this.ILIcons.Images.SetKeyName(3, "");
            // 
            // frmMessageBox
            // 
            this.AcceptButton = this.cmdOK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cmdCancel;
            this.ControlBox = false;
            this.Controls.Add(this.cmdMore);
            this.Controls.Add(this.lblTitleText);
            this.Controls.Add(this.lblMore);
            this.Controls.Add(this.lblMoreLink);
            this.Controls.Add(this.pbIcon);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.txtMessage);
            this.Controls.Add(this.chkAskAgain);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.cmdOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "frmMessageBox";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Load += new System.EventHandler(this.frmMessageBox_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.frmMessageBox_Paint);
            ((System.ComponentModel.ISupportInitialize)(this.pbIcon)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cmdMore;
        private System.Windows.Forms.Label lblTitleText;
        private System.Windows.Forms.Label lblMore;
        private System.Windows.Forms.LinkLabel lblMoreLink;
        public System.Windows.Forms.PictureBox pbIcon;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.CheckBox chkAskAgain;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.Button cmdOK;
        private System.Windows.Forms.ImageList ILIcons;

    }
}