namespace AmbiBack
{
    partial class frmOptions
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmOptions));
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tabGeneral = new System.Windows.Forms.TabPage();
            this.gbDebug = new System.Windows.Forms.GroupBox();
            this.cmdDbgBrowse = new System.Windows.Forms.Button();
            this.lblDebugLog = new System.Windows.Forms.Label();
            this.tabNotifies = new System.Windows.Forms.TabPage();
            this.cmdOK = new System.Windows.Forms.Button();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.chkDbgDelete = new System.Windows.Forms.CheckBox();
            this.txtDebugLog = new System.Windows.Forms.TextBox();
            this.chkDebugLogging = new System.Windows.Forms.CheckBox();
            this.chkDebugWindow = new System.Windows.Forms.CheckBox();
            this.chkLoadWindowsStartup = new System.Windows.Forms.CheckBox();
            this.chkHideOnWindowClose = new System.Windows.Forms.CheckBox();
            this.chkHideOnStartup = new System.Windows.Forms.CheckBox();
            this.tabMain.SuspendLayout();
            this.tabGeneral.SuspendLayout();
            this.gbDebug.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabMain
            // 
            this.tabMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabMain.Controls.Add(this.tabGeneral);
            this.tabMain.Controls.Add(this.tabNotifies);
            this.tabMain.Location = new System.Drawing.Point(3, 3);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(620, 401);
            this.tabMain.TabIndex = 0;
            // 
            // tabGeneral
            // 
            this.tabGeneral.Controls.Add(this.gbDebug);
            this.tabGeneral.Controls.Add(this.chkLoadWindowsStartup);
            this.tabGeneral.Controls.Add(this.chkHideOnWindowClose);
            this.tabGeneral.Controls.Add(this.chkHideOnStartup);
            this.tabGeneral.Location = new System.Drawing.Point(4, 22);
            this.tabGeneral.Name = "tabGeneral";
            this.tabGeneral.Padding = new System.Windows.Forms.Padding(3);
            this.tabGeneral.Size = new System.Drawing.Size(612, 375);
            this.tabGeneral.TabIndex = 0;
            this.tabGeneral.Text = "General";
            this.tabGeneral.UseVisualStyleBackColor = true;
            // 
            // gbDebug
            // 
            this.gbDebug.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbDebug.Controls.Add(this.chkDbgDelete);
            this.gbDebug.Controls.Add(this.cmdDbgBrowse);
            this.gbDebug.Controls.Add(this.txtDebugLog);
            this.gbDebug.Controls.Add(this.lblDebugLog);
            this.gbDebug.Controls.Add(this.chkDebugLogging);
            this.gbDebug.Controls.Add(this.chkDebugWindow);
            this.gbDebug.Location = new System.Drawing.Point(6, 227);
            this.gbDebug.Name = "gbDebug";
            this.gbDebug.Size = new System.Drawing.Size(599, 142);
            this.gbDebug.TabIndex = 8;
            this.gbDebug.TabStop = false;
            this.gbDebug.Text = "Debugging";
            // 
            // cmdDbgBrowse
            // 
            this.cmdDbgBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdDbgBrowse.Image = ((System.Drawing.Image)(resources.GetObject("cmdDbgBrowse.Image")));
            this.cmdDbgBrowse.Location = new System.Drawing.Point(567, 46);
            this.cmdDbgBrowse.Name = "cmdDbgBrowse";
            this.cmdDbgBrowse.Size = new System.Drawing.Size(24, 24);
            this.cmdDbgBrowse.TabIndex = 10;
            // 
            // lblDebugLog
            // 
            this.lblDebugLog.Location = new System.Drawing.Point(8, 46);
            this.lblDebugLog.Name = "lblDebugLog";
            this.lblDebugLog.Size = new System.Drawing.Size(112, 21);
            this.lblDebugLog.TabIndex = 5;
            this.lblDebugLog.Text = "File and Pathname:";
            // 
            // tabNotifies
            // 
            this.tabNotifies.Location = new System.Drawing.Point(4, 22);
            this.tabNotifies.Name = "tabNotifies";
            this.tabNotifies.Padding = new System.Windows.Forms.Padding(3);
            this.tabNotifies.Size = new System.Drawing.Size(612, 375);
            this.tabNotifies.TabIndex = 1;
            this.tabNotifies.Text = "Notifications & Questions";
            this.tabNotifies.UseVisualStyleBackColor = true;
            // 
            // cmdOK
            // 
            this.cmdOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdOK.Location = new System.Drawing.Point(459, 410);
            this.cmdOK.Name = "cmdOK";
            this.cmdOK.Size = new System.Drawing.Size(80, 24);
            this.cmdOK.TabIndex = 4;
            this.cmdOK.Text = "OK";
            this.cmdOK.Click += new System.EventHandler(this.cmdOK_Click);
            // 
            // cmdCancel
            // 
            this.cmdCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cmdCancel.Location = new System.Drawing.Point(539, 410);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(80, 24);
            this.cmdCancel.TabIndex = 3;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
            // 
            // chkDbgDelete
            // 
            this.chkDbgDelete.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.chkDbgDelete.Checked = global::AmbiBack.Properties.Settings.Default.bDebugDeleteExisting;
            this.chkDbgDelete.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkDbgDelete.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::AmbiBack.Properties.Settings.Default, "bDebugDeleteExisting", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.chkDbgDelete.Location = new System.Drawing.Point(8, 73);
            this.chkDbgDelete.Name = "chkDbgDelete";
            this.chkDbgDelete.Size = new System.Drawing.Size(583, 25);
            this.chkDbgDelete.TabIndex = 11;
            this.chkDbgDelete.Text = "Delete existing debuglog on Startup";
            // 
            // txtDebugLog
            // 
            this.txtDebugLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDebugLog.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::AmbiBack.Properties.Settings.Default, "szDebugFileLog", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.txtDebugLog.Location = new System.Drawing.Point(128, 46);
            this.txtDebugLog.Name = "txtDebugLog";
            this.txtDebugLog.Size = new System.Drawing.Size(433, 20);
            this.txtDebugLog.TabIndex = 6;
            this.txtDebugLog.Text = global::AmbiBack.Properties.Settings.Default.szDebugFileLog;
            // 
            // chkDebugLogging
            // 
            this.chkDebugLogging.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.chkDebugLogging.Checked = global::AmbiBack.Properties.Settings.Default.bDebugFileLogging;
            this.chkDebugLogging.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::AmbiBack.Properties.Settings.Default, "bDebugFileLogging", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.chkDebugLogging.Location = new System.Drawing.Point(8, 19);
            this.chkDebugLogging.Name = "chkDebugLogging";
            this.chkDebugLogging.Size = new System.Drawing.Size(583, 21);
            this.chkDebugLogging.TabIndex = 4;
            this.chkDebugLogging.Text = "Enable Debug Logging into file";
            // 
            // chkDebugWindow
            // 
            this.chkDebugWindow.Checked = global::AmbiBack.Properties.Settings.Default.bDebugWindow;
            this.chkDebugWindow.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::AmbiBack.Properties.Settings.Default, "bDebugWindow", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.chkDebugWindow.Location = new System.Drawing.Point(8, 99);
            this.chkDebugWindow.Name = "chkDebugWindow";
            this.chkDebugWindow.Size = new System.Drawing.Size(583, 29);
            this.chkDebugWindow.TabIndex = 3;
            this.chkDebugWindow.Text = "Enable Debug Window";
            this.chkDebugWindow.Visible = false;
            // 
            // chkLoadWindowsStartup
            // 
            this.chkLoadWindowsStartup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.chkLoadWindowsStartup.Checked = global::AmbiBack.Properties.Settings.Default.bLoadAmbiBackOnWindowsStartup;
            this.chkLoadWindowsStartup.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkLoadWindowsStartup.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::AmbiBack.Properties.Settings.Default, "bLoadAmbiBackOnWindowsStartup", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.chkLoadWindowsStartup.Location = new System.Drawing.Point(11, 66);
            this.chkLoadWindowsStartup.Name = "chkLoadWindowsStartup";
            this.chkLoadWindowsStartup.Size = new System.Drawing.Size(591, 23);
            this.chkLoadWindowsStartup.TabIndex = 7;
            this.chkLoadWindowsStartup.Text = "Load AmbiBack on Windows Startup";
            this.chkLoadWindowsStartup.UseVisualStyleBackColor = true;
            // 
            // chkHideOnWindowClose
            // 
            this.chkHideOnWindowClose.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.chkHideOnWindowClose.Checked = global::AmbiBack.Properties.Settings.Default.bHideOnWindowClose;
            this.chkHideOnWindowClose.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkHideOnWindowClose.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::AmbiBack.Properties.Settings.Default, "bHideOnWindowClose", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.chkHideOnWindowClose.Location = new System.Drawing.Point(11, 38);
            this.chkHideOnWindowClose.Name = "chkHideOnWindowClose";
            this.chkHideOnWindowClose.Size = new System.Drawing.Size(591, 24);
            this.chkHideOnWindowClose.TabIndex = 6;
            this.chkHideOnWindowClose.Text = "Hide when the Main windows is closed";
            // 
            // chkHideOnStartup
            // 
            this.chkHideOnStartup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.chkHideOnStartup.Checked = global::AmbiBack.Properties.Settings.Default.bHideOnStartup;
            this.chkHideOnStartup.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::AmbiBack.Properties.Settings.Default, "bHideOnStartup", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.chkHideOnStartup.Location = new System.Drawing.Point(11, 12);
            this.chkHideOnStartup.Name = "chkHideOnStartup";
            this.chkHideOnStartup.Size = new System.Drawing.Size(591, 24);
            this.chkHideOnStartup.TabIndex = 5;
            this.chkHideOnStartup.Text = "Hide on Startup";
            // 
            // frmOptions
            // 
            this.AcceptButton = this.cmdOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cmdCancel;
            this.ClientSize = new System.Drawing.Size(624, 441);
            this.ControlBox = false;
            this.Controls.Add(this.cmdOK);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.tabMain);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "frmOptions";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Options";
            this.Load += new System.EventHandler(this.frmOptions_Load);
            this.tabMain.ResumeLayout(false);
            this.tabGeneral.ResumeLayout(false);
            this.gbDebug.ResumeLayout(false);
            this.gbDebug.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabGeneral;
        private System.Windows.Forms.TabPage tabNotifies;
        private System.Windows.Forms.Button cmdOK;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.GroupBox gbDebug;
        private System.Windows.Forms.CheckBox chkDbgDelete;
        private System.Windows.Forms.Button cmdDbgBrowse;
        private System.Windows.Forms.TextBox txtDebugLog;
        private System.Windows.Forms.Label lblDebugLog;
        private System.Windows.Forms.CheckBox chkDebugLogging;
        private System.Windows.Forms.CheckBox chkDebugWindow;
        private System.Windows.Forms.CheckBox chkLoadWindowsStartup;
        private System.Windows.Forms.CheckBox chkHideOnWindowClose;
        private System.Windows.Forms.CheckBox chkHideOnStartup;
    }
}