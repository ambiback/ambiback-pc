namespace AmbiBack
{
    partial class AmbiMonitor
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblTitel = new System.Windows.Forms.Label();
            this.tsMain = new System.Windows.Forms.ToolStrip();
            this.btnEnable = new System.Windows.Forms.ToolStripButton();
            this.btnStart = new System.Windows.Forms.ToolStripButton();
            this.btnStop = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.lblStatus = new System.Windows.Forms.Label();
            this.monPreview = new System.Windows.Forms.PictureBox();
            this.cbLedsVert = new System.Windows.Forms.ComboBox();
            this.lblLedsVert = new System.Windows.Forms.Label();
            this.lblLedsHori = new System.Windows.Forms.Label();
            this.cbLedsHori = new System.Windows.Forms.ComboBox();
            this.tblSectors = new System.Windows.Forms.TableLayoutPanel();
            this.rbSector7 = new System.Windows.Forms.CheckBox();
            this.rbSector2 = new System.Windows.Forms.CheckBox();
            this.rbSector3 = new System.Windows.Forms.CheckBox();
            this.rbSector4 = new System.Windows.Forms.CheckBox();
            this.rbSector5 = new System.Windows.Forms.CheckBox();
            this.rbSector6 = new System.Windows.Forms.CheckBox();
            this.rbSector0 = new System.Windows.Forms.CheckBox();
            this.rbSector8 = new System.Windows.Forms.CheckBox();
            this.rbSector1 = new System.Windows.Forms.CheckBox();
            this.rbSector9 = new System.Windows.Forms.CheckBox();
            this.rbSector10 = new System.Windows.Forms.CheckBox();
            this.rbSector11 = new System.Windows.Forms.CheckBox();
            this.chkTopLeft = new System.Windows.Forms.CheckBox();
            this.chkTopRight = new System.Windows.Forms.CheckBox();
            this.chkBottomLeft = new System.Windows.Forms.CheckBox();
            this.chkBottomRight = new System.Windows.Forms.CheckBox();
            this.tsMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.monPreview)).BeginInit();
            this.tblSectors.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblTitel
            // 
            this.lblTitel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblTitel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitel.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.lblTitel.Location = new System.Drawing.Point(0, 25);
            this.lblTitel.Name = "lblTitel";
            this.lblTitel.Size = new System.Drawing.Size(209, 25);
            this.lblTitel.TabIndex = 26;
            this.lblTitel.Text = "Monitor: [%d]";
            this.lblTitel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tsMain
            // 
            this.tsMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnEnable,
            this.btnStart,
            this.btnStop,
            this.toolStripSeparator4});
            this.tsMain.Location = new System.Drawing.Point(0, 0);
            this.tsMain.Name = "tsMain";
            this.tsMain.Size = new System.Drawing.Size(360, 25);
            this.tsMain.TabIndex = 28;
            this.tsMain.Text = "toolStrip1";
            // 
            // btnEnable
            // 
            this.btnEnable.CheckOnClick = true;
            this.btnEnable.Image = global::AmbiBack.Properties.Resources.selection;
            this.btnEnable.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnEnable.Name = "btnEnable";
            this.btnEnable.Size = new System.Drawing.Size(100, 22);
            this.btnEnable.Text = "Enable Screen";
            this.btnEnable.Click += new System.EventHandler(this.btnEnable_Click);
            // 
            // btnStart
            // 
            this.btnStart.Image = global::AmbiBack.Properties.Resources.media_play;
            this.btnStart.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(48, 22);
            this.btnStart.Text = "Run";
            this.btnStart.ToolTipText = "Run AmbiBack for this Monitor";
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnStop
            // 
            this.btnStop.Image = global::AmbiBack.Properties.Resources.media_stop;
            this.btnStop.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(51, 22);
            this.btnStop.Text = "Stop";
            this.btnStop.ToolTipText = "Stop AmbiBack for this Monitor";
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(6, 25);
            // 
            // lblStatus
            // 
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStatus.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.lblStatus.Location = new System.Drawing.Point(207, 25);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(153, 25);
            this.lblStatus.TabIndex = 29;
            this.lblStatus.Text = "Status: ";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // monPreview
            // 
            this.monPreview.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.monPreview.BackColor = System.Drawing.Color.Transparent;
            this.monPreview.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.monPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.monPreview.ErrorImage = null;
            this.monPreview.Location = new System.Drawing.Point(60, 84);
            this.monPreview.Name = "monPreview";
            this.monPreview.Size = new System.Drawing.Size(240, 135);
            this.monPreview.TabIndex = 23;
            this.monPreview.TabStop = false;
            // 
            // cbLedsVert
            // 
            this.cbLedsVert.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cbLedsVert.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbLedsVert.FormattingEnabled = true;
            this.cbLedsVert.Location = new System.Drawing.Point(75, 248);
            this.cbLedsVert.Name = "cbLedsVert";
            this.cbLedsVert.Size = new System.Drawing.Size(51, 21);
            this.cbLedsVert.TabIndex = 30;
            this.cbLedsVert.SelectedIndexChanged += new System.EventHandler(this.cbLedsVert_SelectedIndexChanged);
            // 
            // lblLedsVert
            // 
            this.lblLedsVert.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblLedsVert.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblLedsVert.Location = new System.Drawing.Point(23, 248);
            this.lblLedsVert.Name = "lblLedsVert";
            this.lblLedsVert.Size = new System.Drawing.Size(51, 21);
            this.lblLedsVert.TabIndex = 31;
            this.lblLedsVert.Text = "Vertical";
            this.lblLedsVert.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblLedsHori
            // 
            this.lblLedsHori.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblLedsHori.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblLedsHori.Location = new System.Drawing.Point(227, 248);
            this.lblLedsHori.Name = "lblLedsHori";
            this.lblLedsHori.Size = new System.Drawing.Size(64, 21);
            this.lblLedsHori.TabIndex = 32;
            this.lblLedsHori.Text = "Horizontal:";
            this.lblLedsHori.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // cbLedsHori
            // 
            this.cbLedsHori.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cbLedsHori.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbLedsHori.FormattingEnabled = true;
            this.cbLedsHori.Location = new System.Drawing.Point(292, 248);
            this.cbLedsHori.Name = "cbLedsHori";
            this.cbLedsHori.Size = new System.Drawing.Size(51, 21);
            this.cbLedsHori.TabIndex = 33;
            this.cbLedsHori.SelectedIndexChanged += new System.EventHandler(this.cbLedsHori_SelectedIndexChanged);
            // 
            // tblSectors
            // 
            this.tblSectors.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.tblSectors.BackColor = System.Drawing.Color.Transparent;
            this.tblSectors.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.tblSectors.ColumnCount = 5;
            this.tblSectors.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tblSectors.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tblSectors.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tblSectors.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tblSectors.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tblSectors.Controls.Add(this.rbSector7, 0, 1);
            this.tblSectors.Controls.Add(this.rbSector2, 4, 0);
            this.tblSectors.Controls.Add(this.rbSector3, 3, 0);
            this.tblSectors.Controls.Add(this.rbSector4, 2, 0);
            this.tblSectors.Controls.Add(this.rbSector5, 1, 0);
            this.tblSectors.Controls.Add(this.rbSector6, 0, 0);
            this.tblSectors.Controls.Add(this.rbSector0, 4, 2);
            this.tblSectors.Controls.Add(this.rbSector8, 0, 2);
            this.tblSectors.Controls.Add(this.rbSector1, 4, 1);
            this.tblSectors.Controls.Add(this.rbSector9, 1, 2);
            this.tblSectors.Controls.Add(this.rbSector10, 2, 2);
            this.tblSectors.Controls.Add(this.rbSector11, 3, 2);
            this.tblSectors.Location = new System.Drawing.Point(75, 101);
            this.tblSectors.Name = "tblSectors";
            this.tblSectors.RowCount = 3;
            this.tblSectors.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.tblSectors.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.tblSectors.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 34F));
            this.tblSectors.Size = new System.Drawing.Size(216, 100);
            this.tblSectors.TabIndex = 34;
            this.tblSectors.Visible = false;
            // 
            // rbSector7
            // 
            this.rbSector7.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.rbSector7.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbSector7.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.rbSector7.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.rbSector7.FlatAppearance.BorderSize = 2;
            this.rbSector7.FlatAppearance.CheckedBackColor = System.Drawing.Color.Lime;
            this.rbSector7.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Yellow;
            this.rbSector7.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rbSector7.Location = new System.Drawing.Point(14, 40);
            this.rbSector7.Name = "rbSector7";
            this.rbSector7.Size = new System.Drawing.Size(16, 16);
            this.rbSector7.TabIndex = 18;
            this.rbSector7.UseVisualStyleBackColor = true;
            this.rbSector7.CheckedChanged += new System.EventHandler(this.rbSector_CheckedChanged);
            // 
            // rbSector2
            // 
            this.rbSector2.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.rbSector2.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbSector2.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.rbSector2.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.rbSector2.FlatAppearance.BorderSize = 2;
            this.rbSector2.FlatAppearance.CheckedBackColor = System.Drawing.Color.Lime;
            this.rbSector2.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Yellow;
            this.rbSector2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rbSector2.Location = new System.Drawing.Point(186, 8);
            this.rbSector2.Name = "rbSector2";
            this.rbSector2.Size = new System.Drawing.Size(16, 16);
            this.rbSector2.TabIndex = 17;
            this.rbSector2.UseVisualStyleBackColor = true;
            this.rbSector2.CheckedChanged += new System.EventHandler(this.rbSector_CheckedChanged);
            // 
            // rbSector3
            // 
            this.rbSector3.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.rbSector3.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbSector3.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.rbSector3.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.rbSector3.FlatAppearance.BorderSize = 2;
            this.rbSector3.FlatAppearance.CheckedBackColor = System.Drawing.Color.Lime;
            this.rbSector3.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Yellow;
            this.rbSector3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rbSector3.Location = new System.Drawing.Point(143, 8);
            this.rbSector3.Name = "rbSector3";
            this.rbSector3.Size = new System.Drawing.Size(16, 16);
            this.rbSector3.TabIndex = 16;
            this.rbSector3.UseVisualStyleBackColor = true;
            this.rbSector3.CheckedChanged += new System.EventHandler(this.rbSector_CheckedChanged);
            // 
            // rbSector4
            // 
            this.rbSector4.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.rbSector4.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbSector4.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.rbSector4.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.rbSector4.FlatAppearance.BorderSize = 2;
            this.rbSector4.FlatAppearance.CheckedBackColor = System.Drawing.Color.Lime;
            this.rbSector4.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Yellow;
            this.rbSector4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rbSector4.Location = new System.Drawing.Point(100, 8);
            this.rbSector4.Name = "rbSector4";
            this.rbSector4.Size = new System.Drawing.Size(16, 16);
            this.rbSector4.TabIndex = 15;
            this.rbSector4.UseVisualStyleBackColor = true;
            this.rbSector4.CheckedChanged += new System.EventHandler(this.rbSector_CheckedChanged);
            // 
            // rbSector5
            // 
            this.rbSector5.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.rbSector5.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbSector5.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.rbSector5.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.rbSector5.FlatAppearance.BorderSize = 2;
            this.rbSector5.FlatAppearance.CheckedBackColor = System.Drawing.Color.Lime;
            this.rbSector5.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Yellow;
            this.rbSector5.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rbSector5.Location = new System.Drawing.Point(57, 8);
            this.rbSector5.Name = "rbSector5";
            this.rbSector5.Size = new System.Drawing.Size(16, 16);
            this.rbSector5.TabIndex = 14;
            this.rbSector5.UseVisualStyleBackColor = true;
            this.rbSector5.CheckedChanged += new System.EventHandler(this.rbSector_CheckedChanged);
            // 
            // rbSector6
            // 
            this.rbSector6.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.rbSector6.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbSector6.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.rbSector6.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.rbSector6.FlatAppearance.BorderSize = 2;
            this.rbSector6.FlatAppearance.CheckedBackColor = System.Drawing.Color.Lime;
            this.rbSector6.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Yellow;
            this.rbSector6.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rbSector6.Location = new System.Drawing.Point(14, 8);
            this.rbSector6.Name = "rbSector6";
            this.rbSector6.Size = new System.Drawing.Size(16, 16);
            this.rbSector6.TabIndex = 13;
            this.rbSector6.UseVisualStyleBackColor = true;
            this.rbSector6.CheckedChanged += new System.EventHandler(this.rbSector_CheckedChanged);
            // 
            // rbSector0
            // 
            this.rbSector0.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.rbSector0.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbSector0.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.rbSector0.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.rbSector0.FlatAppearance.BorderSize = 2;
            this.rbSector0.FlatAppearance.CheckedBackColor = System.Drawing.Color.Lime;
            this.rbSector0.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Yellow;
            this.rbSector0.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rbSector0.Location = new System.Drawing.Point(186, 74);
            this.rbSector0.Name = "rbSector0";
            this.rbSector0.Size = new System.Drawing.Size(16, 16);
            this.rbSector0.TabIndex = 12;
            this.rbSector0.UseVisualStyleBackColor = true;
            this.rbSector0.CheckedChanged += new System.EventHandler(this.rbSector_CheckedChanged);
            // 
            // rbSector8
            // 
            this.rbSector8.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.rbSector8.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbSector8.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.rbSector8.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.rbSector8.FlatAppearance.BorderSize = 2;
            this.rbSector8.FlatAppearance.CheckedBackColor = System.Drawing.Color.Lime;
            this.rbSector8.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Yellow;
            this.rbSector8.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rbSector8.Location = new System.Drawing.Point(14, 74);
            this.rbSector8.Name = "rbSector8";
            this.rbSector8.Size = new System.Drawing.Size(16, 16);
            this.rbSector8.TabIndex = 19;
            this.rbSector8.UseVisualStyleBackColor = true;
            this.rbSector8.CheckedChanged += new System.EventHandler(this.rbSector_CheckedChanged);
            // 
            // rbSector1
            // 
            this.rbSector1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.rbSector1.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbSector1.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.rbSector1.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.rbSector1.FlatAppearance.BorderSize = 2;
            this.rbSector1.FlatAppearance.CheckedBackColor = System.Drawing.Color.Lime;
            this.rbSector1.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Yellow;
            this.rbSector1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rbSector1.Location = new System.Drawing.Point(186, 40);
            this.rbSector1.Name = "rbSector1";
            this.rbSector1.Size = new System.Drawing.Size(16, 16);
            this.rbSector1.TabIndex = 20;
            this.rbSector1.UseVisualStyleBackColor = true;
            this.rbSector1.CheckedChanged += new System.EventHandler(this.rbSector_CheckedChanged);
            // 
            // rbSector9
            // 
            this.rbSector9.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.rbSector9.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbSector9.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.rbSector9.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.rbSector9.FlatAppearance.BorderSize = 2;
            this.rbSector9.FlatAppearance.CheckedBackColor = System.Drawing.Color.Lime;
            this.rbSector9.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Yellow;
            this.rbSector9.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rbSector9.Location = new System.Drawing.Point(57, 74);
            this.rbSector9.Name = "rbSector9";
            this.rbSector9.Size = new System.Drawing.Size(16, 16);
            this.rbSector9.TabIndex = 21;
            this.rbSector9.UseVisualStyleBackColor = true;
            this.rbSector9.CheckedChanged += new System.EventHandler(this.rbSector_CheckedChanged);
            // 
            // rbSector10
            // 
            this.rbSector10.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.rbSector10.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbSector10.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.rbSector10.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.rbSector10.FlatAppearance.BorderSize = 2;
            this.rbSector10.FlatAppearance.CheckedBackColor = System.Drawing.Color.Lime;
            this.rbSector10.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Yellow;
            this.rbSector10.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rbSector10.Location = new System.Drawing.Point(100, 74);
            this.rbSector10.Name = "rbSector10";
            this.rbSector10.Size = new System.Drawing.Size(16, 16);
            this.rbSector10.TabIndex = 22;
            this.rbSector10.UseVisualStyleBackColor = true;
            this.rbSector10.CheckedChanged += new System.EventHandler(this.rbSector_CheckedChanged);
            // 
            // rbSector11
            // 
            this.rbSector11.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.rbSector11.Appearance = System.Windows.Forms.Appearance.Button;
            this.rbSector11.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.rbSector11.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.rbSector11.FlatAppearance.BorderSize = 2;
            this.rbSector11.FlatAppearance.CheckedBackColor = System.Drawing.Color.Lime;
            this.rbSector11.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Yellow;
            this.rbSector11.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rbSector11.Location = new System.Drawing.Point(143, 74);
            this.rbSector11.Name = "rbSector11";
            this.rbSector11.Size = new System.Drawing.Size(16, 16);
            this.rbSector11.TabIndex = 23;
            this.rbSector11.UseVisualStyleBackColor = true;
            this.rbSector11.CheckedChanged += new System.EventHandler(this.rbSector_CheckedChanged);
            // 
            // chkTopLeft
            // 
            this.chkTopLeft.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.chkTopLeft.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkTopLeft.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.chkTopLeft.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.chkTopLeft.FlatAppearance.BorderSize = 2;
            this.chkTopLeft.FlatAppearance.CheckedBackColor = System.Drawing.Color.Lime;
            this.chkTopLeft.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Yellow;
            this.chkTopLeft.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkTopLeft.Location = new System.Drawing.Point(38, 63);
            this.chkTopLeft.Name = "chkTopLeft";
            this.chkTopLeft.Size = new System.Drawing.Size(16, 16);
            this.chkTopLeft.TabIndex = 35;
            this.chkTopLeft.UseVisualStyleBackColor = true;
            this.chkTopLeft.Click += new System.EventHandler(this.chkTopLeft_Click);
            // 
            // chkTopRight
            // 
            this.chkTopRight.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.chkTopRight.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkTopRight.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.chkTopRight.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.chkTopRight.FlatAppearance.BorderSize = 2;
            this.chkTopRight.FlatAppearance.CheckedBackColor = System.Drawing.Color.Lime;
            this.chkTopRight.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Yellow;
            this.chkTopRight.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkTopRight.Location = new System.Drawing.Point(305, 63);
            this.chkTopRight.Name = "chkTopRight";
            this.chkTopRight.Size = new System.Drawing.Size(16, 16);
            this.chkTopRight.TabIndex = 36;
            this.chkTopRight.UseVisualStyleBackColor = true;
            this.chkTopRight.Click += new System.EventHandler(this.chkTopRight_Click);
            // 
            // chkBottomLeft
            // 
            this.chkBottomLeft.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.chkBottomLeft.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkBottomLeft.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.chkBottomLeft.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.chkBottomLeft.FlatAppearance.BorderSize = 2;
            this.chkBottomLeft.FlatAppearance.CheckedBackColor = System.Drawing.Color.Lime;
            this.chkBottomLeft.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Yellow;
            this.chkBottomLeft.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkBottomLeft.Location = new System.Drawing.Point(38, 225);
            this.chkBottomLeft.Name = "chkBottomLeft";
            this.chkBottomLeft.Size = new System.Drawing.Size(16, 16);
            this.chkBottomLeft.TabIndex = 37;
            this.chkBottomLeft.UseVisualStyleBackColor = true;
            this.chkBottomLeft.Click += new System.EventHandler(this.chkBottomLeft_Click);
            // 
            // chkBottomRight
            // 
            this.chkBottomRight.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.chkBottomRight.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkBottomRight.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.chkBottomRight.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.chkBottomRight.FlatAppearance.BorderSize = 2;
            this.chkBottomRight.FlatAppearance.CheckedBackColor = System.Drawing.Color.Lime;
            this.chkBottomRight.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Yellow;
            this.chkBottomRight.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkBottomRight.Location = new System.Drawing.Point(305, 225);
            this.chkBottomRight.Name = "chkBottomRight";
            this.chkBottomRight.Size = new System.Drawing.Size(16, 16);
            this.chkBottomRight.TabIndex = 38;
            this.chkBottomRight.UseVisualStyleBackColor = true;
            this.chkBottomRight.Click += new System.EventHandler(this.chkBottomRight_Click);
            // 
            // AmbiMonitor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.Controls.Add(this.chkBottomRight);
            this.Controls.Add(this.chkBottomLeft);
            this.Controls.Add(this.chkTopRight);
            this.Controls.Add(this.chkTopLeft);
            this.Controls.Add(this.tblSectors);
            this.Controls.Add(this.cbLedsHori);
            this.Controls.Add(this.lblLedsHori);
            this.Controls.Add(this.lblLedsVert);
            this.Controls.Add(this.cbLedsVert);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.tsMain);
            this.Controls.Add(this.lblTitel);
            this.Controls.Add(this.monPreview);
            this.DoubleBuffered = true;
            this.MinimumSize = new System.Drawing.Size(360, 280);
            this.Name = "AmbiMonitor";
            this.Size = new System.Drawing.Size(360, 280);
            this.Load += new System.EventHandler(this.AmbiMonitor_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.AmbiMonitor_Paint);
            this.Enter += new System.EventHandler(this.AmbiMonitor_Enter);
            this.Leave += new System.EventHandler(this.AmbiMonitor_Leave);
            this.tsMain.ResumeLayout(false);
            this.tsMain.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.monPreview)).EndInit();
            this.tblSectors.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.PictureBox monPreview;
        private System.Windows.Forms.Label lblTitel;
        private System.Windows.Forms.ToolStrip tsMain;
        private System.Windows.Forms.ToolStripButton btnStart;
        private System.Windows.Forms.ToolStripButton btnStop;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripButton btnEnable;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.ComboBox cbLedsVert;
        private System.Windows.Forms.Label lblLedsVert;
        private System.Windows.Forms.Label lblLedsHori;
        private System.Windows.Forms.ComboBox cbLedsHori;
        private System.Windows.Forms.TableLayoutPanel tblSectors;
        private System.Windows.Forms.CheckBox rbSector7;
        private System.Windows.Forms.CheckBox rbSector2;
        private System.Windows.Forms.CheckBox rbSector3;
        private System.Windows.Forms.CheckBox rbSector4;
        private System.Windows.Forms.CheckBox rbSector5;
        private System.Windows.Forms.CheckBox rbSector6;
        private System.Windows.Forms.CheckBox rbSector0;
        private System.Windows.Forms.CheckBox rbSector8;
        private System.Windows.Forms.CheckBox rbSector1;
        private System.Windows.Forms.CheckBox rbSector9;
        private System.Windows.Forms.CheckBox rbSector10;
        private System.Windows.Forms.CheckBox rbSector11;
        private System.Windows.Forms.CheckBox chkTopLeft;
        private System.Windows.Forms.CheckBox chkTopRight;
        private System.Windows.Forms.CheckBox chkBottomLeft;
        private System.Windows.Forms.CheckBox chkBottomRight;
    }
}
