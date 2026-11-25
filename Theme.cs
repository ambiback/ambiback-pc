using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AmbiBack
{
    /// <summary>
    /// Colors, fonts and small helpers for a calm, light look with one accent color.
    /// Same palette as the AmbiBack USB Flasher. The accent is only used for the main action
    /// of an area and for the selected monitor.
    /// </summary>
    public static class Theme
    {
        // Accent: white text on it has a contrast of 5.4:1
        public static readonly Color Accent = Color.FromArgb(0x0B, 0x6F, 0xA8);
        public static readonly Color AccentHover = Color.FromArgb(0x09, 0x5C, 0x8C);
        public static readonly Color AccentDisabled = Color.FromArgb(0xA9, 0xC6, 0xD8);

        public static readonly Color Surface = Color.FromArgb(0xF5, 0xF6, 0xF8);
        public static readonly Color Card = Color.White;
        public static readonly Color Separator = Color.FromArgb(0xDD, 0xE1, 0xE6);
        public static readonly Color PrimaryText = Color.FromArgb(0x22, 0x26, 0x2B);      // 15.1:1 on white
        public static readonly Color SecondaryText = Color.FromArgb(0x5A, 0x63, 0x6E);    // 6.1:1 on white, 5.7:1 on Surface
        public static readonly Color Destructive = Color.FromArgb(0xB3, 0x26, 0x1E);      // 6.6:1 on white
        public static readonly Color Success = Color.FromArgb(0x1E, 0x7B, 0x34);          // 5.4:1 on white
        public static readonly Color Warning = Color.FromArgb(0x9A, 0x5B, 0x00);          // 5.0:1 on white

        // Border of secondary buttons: 3:1 against white (non-text contrast)
        public static readonly Color ControlBorder = Color.FromArgb(0x93, 0x9B, 0xA6);
        public static readonly Color HoverBack = Color.FromArgb(0xEA, 0xF1, 0xF7);
        public static readonly Color PressedBack = Color.FromArgb(0xD9, 0xE6, 0xF0);
        public static readonly Color SelectedBack = Color.FromArgb(0xE3, 0xEE, 0xF6);

        public static readonly Font BaseFont = new Font("Segoe UI", 9F);
        public static readonly Font TitleFont = new Font("Segoe UI Semibold", 12F);
        public static readonly Font SectionFont = new Font("Segoe UI Semibold", 10.5F);
        public static readonly Font CardTitleFont = new Font("Segoe UI Semibold", 9F);
        public static readonly Font SmallFont = new Font("Segoe UI", 8.25F);
        public static readonly Font LogFont = new Font("Consolas", 9F);

        /// <summary>Base look of a form: Segoe UI and the light surface color.</summary>
        public static void ApplyForm(Form f)
        {
            f.Font = BaseFont;
            f.BackColor = Surface;
            f.ForeColor = PrimaryText;
        }

        /// <summary>Style a button as the one prominent action of its area.</summary>
        public static void StylePrimary(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = AccentHover;
            b.FlatAppearance.MouseDownBackColor = AccentHover;
            b.ForeColor = Color.White;
            b.UseVisualStyleBackColor = false;
            b.Cursor = Cursors.Hand;
            EventHandler update = (s, e) => b.BackColor = b.Enabled ? Accent : AccentDisabled;
            b.EnabledChanged += update;
            update(b, EventArgs.Empty);
        }

        /// <summary>Style a normal (secondary) button: white, thin border, light blue on hover.</summary>
        public static void StyleSecondary(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = ControlBorder;
            b.FlatAppearance.MouseOverBackColor = HoverBack;
            b.FlatAppearance.MouseDownBackColor = PressedBack;
            b.BackColor = Card;
            b.ForeColor = PrimaryText;
            b.UseVisualStyleBackColor = false;
            b.Cursor = Cursors.Hand;
        }

        /// <summary>Style a button whose action cannot be undone (red text, secondary look).</summary>
        public static void StyleDestructive(Button b)
        {
            StyleSecondary(b);
            b.ForeColor = Destructive;
        }

        /// <summary>Style a hint label: secondary text color, no frame or colored background.</summary>
        public static void StyleHint(Label l)
        {
            l.BorderStyle = BorderStyle.None;
            l.BackColor = Color.Transparent;
            l.ForeColor = SecondaryText;
            l.Font = SmallFont;
        }

        /// <summary>Style a toggle (RadioButton with button appearance) for the Easy / Advanced switch.</summary>
        public static void StyleSegment(RadioButton rb)
        {
            rb.Appearance = Appearance.Button;
            rb.FlatStyle = FlatStyle.Flat;
            rb.FlatAppearance.BorderColor = ControlBorder;
            rb.FlatAppearance.CheckedBackColor = Accent;
            rb.FlatAppearance.MouseOverBackColor = HoverBack;
            rb.BackColor = Card;
            rb.TextAlign = ContentAlignment.MiddleCenter;
            rb.Cursor = Cursors.Hand;
            EventHandler update = (s, e) => rb.ForeColor = rb.Checked ? Color.White : PrimaryText;
            rb.CheckedChanged += update;
            update(rb, EventArgs.Empty);
        }

        /// <summary>Flat menus, tool strips and status strips.</summary>
        public static void StyleStrip(ToolStrip ts)
        {
            ts.Renderer = new FlatToolStripRenderer();
            ts.BackColor = (ts is MenuStrip || ts is StatusStrip) ? Card : Surface;
            ts.ForeColor = PrimaryText;
            ts.Font = BaseFont;
            ts.GripStyle = ToolStripGripStyle.Hidden;
            ts.Padding = new Padding(6, 2, 6, 2);
        }

        /// <summary>Path of a rectangle with rounded corners.</summary>
        public static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        /// <summary>Icon font of Windows 11 (Segoe Fluent Icons) or Windows 10 (Segoe MDL2 Assets), null when neither exists.</summary>
        private static string glyphFont;
        private static bool glyphFontChecked;

        public static string GlyphFontName
        {
            get
            {
                if (!glyphFontChecked)
                {
                    glyphFontChecked = true;
                    foreach (string name in new[] { "Segoe Fluent Icons", "Segoe MDL2 Assets" })
                    {
                        using (var f = new Font(name, 10f))
                        {
                            if (f.Name == name)
                            {
                                glyphFont = name;
                                break;
                            }
                        }
                    }
                }
                return glyphFont;
            }
        }

        /// <summary>
        /// Monochrome symbol from the Windows icon font as bitmap (size in pixels), so all toolbar icons share
        /// one look and stay sharp at every DPI. Returns null when the icon font is missing.
        /// </summary>
        public static Bitmap Glyph(char glyph, Color color, int size)
        {
            string font = GlyphFontName;
            if (font == null)
                return null;
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            using (var f = new Font(font, size * 0.75f, GraphicsUnit.Pixel))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                TextRenderer.DrawText(g, glyph.ToString(), f, new Rectangle(0, 0, size, size), color,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            }
            return bmp;
        }

        /// <summary>Glyphs of the Windows icon font used in the toolbars.</summary>
        public static class Glyphs
        {
            public const char Play = '';
            public const char Stop = '';
            public const char Lightbulb = '';
            public const char Settings = '';
            public const char Search = '';
            public const char Add = '';
            public const char Delete = '';
            public const char Broom = '';
            public const char View = '';
            public const char Checkbox = '';
            public const char CheckboxChecked = '';
            public const char Globe = '';
        }

        /// <summary>Small filled circle used as status dot next to a status text.</summary>
        public static void DrawStatusDot(Graphics g, Color c, int x, int yCenter, int size = 8)
        {
            var old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(c))
                g.FillEllipse(b, x, yCenter - size / 2, size, size);
            g.SmoothingMode = old;
        }
    }

    /// <summary>
    /// GroupBox drawn as a white card with rounded corners, a light border and a semibold title,
    /// instead of the etched frame. Layout and child handling stay those of the GroupBox.
    /// </summary>
    public class CardGroupBox : GroupBox
    {
        public CardGroupBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Card;
        }

        /// <summary>Space below the card that shows the parent background (for cards docked on top of each other).</summary>
        [System.ComponentModel.DefaultValue(0)]
        public int BottomGap { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = Theme.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1 - BottomGap), 6))
            using (var fill = new SolidBrush(Theme.Card))
            using (var pen = new Pen(Theme.Separator))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }
            g.SmoothingMode = SmoothingMode.Default;
            TextRenderer.DrawText(g, Text, Theme.CardTitleFont, new Point(9, 3),
                Enabled ? Theme.PrimaryText : Theme.SecondaryText, TextFormatFlags.NoPrefix);
        }
    }

    /// <summary>Flat tool strip and menu: plain background, light hover, no gradients.</summary>
    public class FlatToolStripRenderer : ToolStripProfessionalRenderer
    {
        /// <summary>Tag of tool strip buttons drawn as segment (checked = accent background).</summary>
        public const string SegmentTag = "segment";

        /// <summary>Tag of the one prominent tool strip button (accent background, white text).</summary>
        public const string PrimaryTag = "primary";

        public FlatToolStripRenderer() : base(new FlatColorTable()) { RoundedEdges = false; }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is ToolStripDropDown) { base.OnRenderToolStripBackground(e); return; }
            using (var b = new SolidBrush(e.ToolStrip.BackColor))
                e.Graphics.FillRectangle(b, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is ToolStripDropDown)
            {
                using (var p = new Pen(Theme.Separator))
                    e.Graphics.DrawRectangle(p, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                return;
            }
            using (var p = new Pen(Theme.Separator))
            {
                if (e.ToolStrip is StatusStrip)
                    e.Graphics.DrawLine(p, 0, 0, e.ToolStrip.Width, 0);
                else
                    e.Graphics.DrawLine(p, 0, e.ToolStrip.Height - 1, e.ToolStrip.Width, e.ToolStrip.Height - 1);
            }
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var btn = e.Item as ToolStripButton;
            bool on = btn != null && btn.Checked;
            if (PrimaryTag.Equals(e.Item.Tag))
            {
                Color back = !e.Item.Enabled ? Theme.AccentDisabled : (e.Item.Selected || e.Item.Pressed ? Theme.AccentHover : Theme.Accent);
                var r = new Rectangle(0, 0, e.Item.Width - 1, e.Item.Height - 1);
                var old = e.Graphics.SmoothingMode;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var b = new SolidBrush(back))
                using (var path = Theme.RoundedRect(r, Math.Max(2, e.Item.Height / 8)))
                    e.Graphics.FillPath(b, path);
                e.Graphics.SmoothingMode = old;
                return;
            }
            if (on && SegmentTag.Equals(e.Item.Tag))
            {
                using (var b = new SolidBrush(e.Item.Selected ? Theme.AccentHover : Theme.Accent))
                    e.Graphics.FillRectangle(b, new Rectangle(Point.Empty, e.Item.Size));
                return;
            }
            if (SegmentTag.Equals(e.Item.Tag))
            {
                // Unselected segment: framed, so the pair reads as one switch
                using (var b = new SolidBrush(e.Item.Pressed ? Theme.PressedBack : (e.Item.Selected ? Theme.HoverBack : Theme.Card)))
                    e.Graphics.FillRectangle(b, new Rectangle(Point.Empty, e.Item.Size));
                using (var p = new Pen(Theme.ControlBorder))
                    e.Graphics.DrawRectangle(p, 0, 0, e.Item.Width - 1, e.Item.Height - 1);
                return;
            }
            if (!e.Item.Selected && !e.Item.Pressed && !on) return;
            Color c = e.Item.Pressed ? Theme.PressedBack : (on ? Theme.SelectedBack : Theme.HoverBack);
            using (var b = new SolidBrush(c))
                e.Graphics.FillRectangle(b, new Rectangle(Point.Empty, e.Item.Size));
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (PrimaryTag.Equals(e.Item.Tag)) e.TextColor = Color.White;
            else if (!e.Item.Enabled) e.TextColor = Theme.SecondaryText;
            base.OnRenderItemText(e);
        }

        private class FlatColorTable : ProfessionalColorTable
        {
            public override Color MenuItemSelected { get { return Theme.HoverBack; } }
            public override Color MenuItemBorder { get { return Theme.HoverBack; } }
            public override Color MenuItemSelectedGradientBegin { get { return Theme.HoverBack; } }
            public override Color MenuItemSelectedGradientEnd { get { return Theme.HoverBack; } }
            public override Color MenuItemPressedGradientBegin { get { return Theme.PressedBack; } }
            public override Color MenuItemPressedGradientEnd { get { return Theme.PressedBack; } }
            public override Color MenuItemPressedGradientMiddle { get { return Theme.PressedBack; } }
            public override Color MenuBorder { get { return Theme.Separator; } }
            public override Color ToolStripDropDownBackground { get { return Theme.Card; } }
            public override Color ImageMarginGradientBegin { get { return Theme.Card; } }
            public override Color ImageMarginGradientMiddle { get { return Theme.Card; } }
            public override Color ImageMarginGradientEnd { get { return Theme.Card; } }
            public override Color SeparatorDark { get { return Theme.Separator; } }
            public override Color SeparatorLight { get { return Theme.Card; } }
            public override Color CheckBackground { get { return Theme.SelectedBack; } }
            public override Color CheckSelectedBackground { get { return Theme.PressedBack; } }
            public override Color CheckPressedBackground { get { return Theme.PressedBack; } }
            public override Color ButtonSelectedBorder { get { return Theme.HoverBack; } }
            public override Color ButtonCheckedHighlightBorder { get { return Theme.SelectedBack; } }
        }
    }
}
