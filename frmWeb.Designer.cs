namespace AmbiBack
{
    partial class frmWeb
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
            this.webBrowser = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.cmdClose = new System.Windows.Forms.Button();
            this.cmdCloseTop = new System.Windows.Forms.Button();
            this.cbVideoList = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // webBrowser
            // 
            this.webBrowser.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.webBrowser.Location = new System.Drawing.Point(0, 0);
            this.webBrowser.DefaultBackgroundColor = System.Drawing.Color.Black;
            this.webBrowser.Name = "webBrowser";
            this.webBrowser.Size = new System.Drawing.Size(800, 450);
            this.webBrowser.TabIndex = 0;
            // 
            // cmdClose
            // 
            this.cmdClose.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.cmdClose.Image = global::AmbiBack.Properties.Resources.navigate_close1;
            this.cmdClose.Location = new System.Drawing.Point(348, 400);
            this.cmdClose.Name = "cmdClose";
            this.cmdClose.Size = new System.Drawing.Size(100, 50);
            this.cmdClose.TabIndex = 1;
            this.cmdClose.Text = "Close";
            this.cmdClose.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.cmdClose.UseVisualStyleBackColor = true;
            this.cmdClose.Click += new System.EventHandler(this.cmdClose_Click);
            // 
            // cmdCloseTop
            // 
            this.cmdCloseTop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdCloseTop.BackColor = System.Drawing.Color.PaleGreen;
            this.cmdCloseTop.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmdCloseTop.Location = new System.Drawing.Point(757, 0);
            this.cmdCloseTop.Name = "cmdCloseTop";
            this.cmdCloseTop.Size = new System.Drawing.Size(43, 31);
            this.cmdCloseTop.TabIndex = 2;
            this.cmdCloseTop.Text = "X";
            this.cmdCloseTop.UseVisualStyleBackColor = false;
            this.cmdCloseTop.Click += new System.EventHandler(this.cmdCloseTop_Click);
            // 
            // cbVideoList
            // 
            this.cbVideoList.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.cbVideoList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbVideoList.FormattingEnabled = true;
            this.cbVideoList.Location = new System.Drawing.Point(300, 0);
            this.cbVideoList.Name = "cbVideoList";
            this.cbVideoList.Size = new System.Drawing.Size(200, 21);
            this.cbVideoList.TabIndex = 3;
            this.cbVideoList.SelectedIndexChanged += new System.EventHandler(this.cbVideoList_SelectedIndexChanged);
            // 
            // frmWeb
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.cbVideoList);
            this.Controls.Add(this.cmdCloseTop);
            this.Controls.Add(this.cmdClose);
            this.Controls.Add(this.webBrowser);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmWeb";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "frmWeb";
            this.Load += new System.EventHandler(this.frmWeb_Load);
            this.Shown += new System.EventHandler(this.frmWeb_Shown);
            this.ResumeLayout(false);

        }

        #endregion

        private Microsoft.Web.WebView2.WinForms.WebView2 webBrowser;
        private System.Windows.Forms.Button cmdClose;
        private System.Windows.Forms.Button cmdCloseTop;
        private System.Windows.Forms.ComboBox cbVideoList;
    }
}