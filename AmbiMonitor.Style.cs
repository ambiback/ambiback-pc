using System;
using System.Drawing;
using System.Windows.Forms;

namespace AmbiBack
{
    public partial class AmbiMonitor
    {
        private bool _selected;

        /// <summary>Card look: flat tool strip, plain header, border drawn in Paint.</summary>
        internal void ApplyStyle()
        {
            BorderStyle = BorderStyle.None;
            Font = Theme.BaseFont;
            Theme.StyleStrip(tsMain);
            tsMain.BackColor = Color.Transparent;
            tsMain.Padding = new Padding(4, 2, 4, 0);
            foreach (Label l in new[] { lblTitel, lblStatus })
            {
                l.BorderStyle = BorderStyle.None;
                l.BackColor = Color.Transparent;
            }
            lblTitel.Font = Theme.SectionFont;
            lblTitel.ForeColor = Theme.PrimaryText;
            lblTitel.TextAlign = ContentAlignment.MiddleLeft;
            lblTitel.Padding = new Padding(6, 0, 0, 0);
            lblStatus.Font = Theme.BaseFont;
            lblStatus.ForeColor = Theme.SecondaryText;
            lblStatus.TextAlign = ContentAlignment.MiddleRight;
            lblStatus.Padding = new Padding(0, 0, 8, 0);
            lblLedsVert.ForeColor = lblLedsHori.ForeColor = Theme.SecondaryText;
            Paint += AmbiMonitor_PaintBorder;
            // Title on the left half, status on the right half of the header
            Resize += (s, e) =>
            {
                int half = Width / 2;
                lblTitel.Anchor = lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                lblTitel.Width = half;
                lblStatus.SetBounds(half, lblTitel.Top, Width - half - 2, lblTitel.Height);
            };
            // Same symbol set as the main window: check box, play in green, stop in red
            SetGlyph(btnStart, Theme.Glyphs.Play, Theme.Success);
            SetGlyph(btnStop, Theme.Glyphs.Stop, Theme.Destructive);
            SetEnableGlyph(btnEnable.CheckState == CheckState.Checked);
            ApplyTexts();
            SetSelected(false);
        }

        /// <summary>"Use" toggle: empty or ticked check box (designer image when the icon font is missing).</summary>
        private void SetEnableGlyph(bool enabled)
        {
            if (!SetGlyph(btnEnable, enabled ? Theme.Glyphs.CheckboxChecked : Theme.Glyphs.Checkbox, enabled ? Theme.Accent : Theme.SecondaryText))
                btnEnable.Image = enabled ? Properties.Resources.selection_ok : Properties.Resources.selection;
        }

        private bool SetGlyph(ToolStripItem item, char glyph, Color color)
        {
            Bitmap bmp = Theme.Glyph(glyph, color, DialogStyle.Px(this, 16));
            if (bmp == null)
                return false;
            Image old = item.Image;
            item.Image = bmp;
            item.ImageScaling = ToolStripItemImageScaling.None;
            item.ImageTransparentColor = Color.Empty;
            old?.Dispose();    // designer and resource images are separate copies
            return true;
        }

        internal void ApplyTexts()
        {
            btnEnable.Text = L.T("mon_enable");
            btnEnable.ToolTipText = L.T("mon_enable_tip");
            btnStart.Text = L.T("mon_start");
            btnStart.ToolTipText = L.T("mon_start_tip");
            btnStop.Text = L.T("mon_stop");
            btnStop.ToolTipText = L.T("mon_stop_tip");
            lblLedsVert.Text = L.T("mon_vert");
            lblLedsHori.Text = L.T("mon_hori");
            UpdateStatusText();
        }

        /// <summary>Selected monitor: white card with an accent border and accent title.</summary>
        internal void SetSelected(bool selected)
        {
            _selected = selected;
            BackColor = selected ? Theme.Card : Theme.Surface;
            lblTitel.ForeColor = selected ? Theme.Accent : Theme.PrimaryText;
            Invalidate();
        }

        /// <summary>Status on the right of the header; "Stopped" when no capture runs.</summary>
        internal void UpdateStatusText()
        {
            lblStatus.Text = "● " + (!AmbiBackRunning ? L.T("mon_idle") : string.IsNullOrEmpty(szLastStatus) ? L.T("mon_start") + " …" : szLastStatus);
            lblStatus.ForeColor = AmbiBackRunning ? Theme.Success : Theme.SecondaryText;
            // Status bar and tray tooltip show how many monitors run
            if (CGlobals.mainForm != null && !CGlobals.mainForm.IsDisposed)
                CGlobals.mainForm.UpdateDeviceSummary();
        }

        private void AmbiMonitor_PaintBorder(object sender, PaintEventArgs e)
        {
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var p = new Pen(_selected ? Theme.Accent : Theme.Separator, _selected ? 2 : 1))
            {
                if (_selected) r.Inflate(-1, -1);
                e.Graphics.DrawRectangle(p, r);
            }
        }
    }
}
