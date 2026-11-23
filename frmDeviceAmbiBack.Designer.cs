namespace AmbiBack
{
    partial class frmDeviceAmbiBack
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmDeviceAmbiBack));
            this.pageSetupDialog1 = new System.Windows.Forms.PageSetupDialog();
            this.TSTop = new System.Windows.Forms.ToolStrip();
            this.lblDeviceName = new System.Windows.Forms.ToolStripLabel();
            this.txtName = new System.Windows.Forms.ToolStripTextBox();
            this.lblIPAddress = new System.Windows.Forms.ToolStripLabel();
            this.txtIpAddress = new System.Windows.Forms.ToolStripTextBox();
            this.cmdReload = new System.Windows.Forms.ToolStripButton();
            this.cmdClose = new System.Windows.Forms.ToolStripButton();
            this.TSTop.SuspendLayout();
            this.SuspendLayout();
            // 
            // TSTop
            // 
            this.TSTop.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblDeviceName,
            this.txtName,
            this.lblIPAddress,
            this.txtIpAddress,
            this.cmdReload,
            this.cmdClose});
            this.TSTop.Location = new System.Drawing.Point(0, 0);
            this.TSTop.Name = "TSTop";
            this.TSTop.Size = new System.Drawing.Size(784, 25);
            this.TSTop.TabIndex = 17;
            this.TSTop.Text = "TSTop";
            // 
            // lblDeviceName
            // 
            this.lblDeviceName.Name = "lblDeviceName";
            this.lblDeviceName.Size = new System.Drawing.Size(77, 22);
            this.lblDeviceName.Text = "Device Name";
            // 
            // txtName
            // 
            this.txtName.Name = "txtName";
            this.txtName.ReadOnly = true;
            this.txtName.Size = new System.Drawing.Size(200, 25);
            // 
            // lblIPAddress
            // 
            this.lblIPAddress.Name = "lblIPAddress";
            this.lblIPAddress.Size = new System.Drawing.Size(62, 22);
            this.lblIPAddress.Text = "IP Address";
            // 
            // txtIpAddress
            // 
            this.txtIpAddress.Name = "txtIpAddress";
            this.txtIpAddress.Size = new System.Drawing.Size(150, 25);
            // 
            // cmdReload
            // 
            this.cmdReload.Image = global::AmbiBack.Properties.Resources.replace2;
            this.cmdReload.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.cmdReload.Name = "cmdReload";
            this.cmdReload.Size = new System.Drawing.Size(63, 22);
            this.cmdReload.Text = "Reload";
            this.cmdReload.Click += new System.EventHandler(this.cmdReload_Click);
            // 
            // cmdClose
            // 
            this.cmdClose.Image = global::AmbiBack.Properties.Resources.exit;
            this.cmdClose.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.cmdClose.Name = "cmdClose";
            this.cmdClose.Size = new System.Drawing.Size(56, 22);
            this.cmdClose.Text = "Close";
            this.cmdClose.Click += new System.EventHandler(this.cmdClose_Click);
            // 
            // frmDeviceAmbiBack
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.TSTop);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmDeviceAmbiBack";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "AmbiBack Device";
            this.Load += new System.EventHandler(this.frmDeviceAmbiBack_Load);
            this.TSTop.ResumeLayout(false);
            this.TSTop.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.PageSetupDialog pageSetupDialog1;
        private System.Windows.Forms.ToolStrip TSTop;
        private System.Windows.Forms.ToolStripLabel lblDeviceName;
        private System.Windows.Forms.ToolStripTextBox txtName;
        private System.Windows.Forms.ToolStripLabel lblIPAddress;
        private System.Windows.Forms.ToolStripTextBox txtIpAddress;
        private System.Windows.Forms.ToolStripButton cmdReload;
        private System.Windows.Forms.ToolStripButton cmdClose;
    }
}