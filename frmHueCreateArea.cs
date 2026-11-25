using AmbiBack.PhilipsHue;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AmbiBack
{
    /// <summary>
    /// Creates an entertainment area on the bridge from single lights: pick the lights and where each
    /// one stands relative to the screen. Only lights that can stream are listed (no plugs).
    /// </summary>
    public class frmHueCreateArea : Form
    {
        /// <summary>Screen positions offered per light: key, x (left/right), z (bottom/top). "auto" spreads left to right.</summary>
        private static readonly string[] PositionKeys = { "auto", "left", "top_left", "top", "top_right", "right", "bottom_right", "bottom", "bottom_left", "center" };
        private static readonly double[,] PositionXZ =
        {
            { 0, 0 }, { -1, 0 }, { -1, 1 }, { 0, 1 }, { 1, 1 }, { 1, 0 }, { 1, -1 }, { 0, -1 }, { -1, -1 }, { 0, 0 },
        };
        public const int MaxLights = 10;    // Hue limit per entertainment area

        private readonly Label lblTitle = new Label();
        private readonly Label lblName = new Label();
        private readonly TextBox txtName = new TextBox();
        private readonly Label lblLights = new Label();
        private readonly Label lblStatus = new Label();
        private readonly Panel pnlLights = new Panel();
        private readonly Button cmdCreate = new Button();
        private readonly Button cmdCancel = new Button();
        private readonly List<Row> rows = new List<Row>();

        /// <summary>Id of the created area after DialogResult.OK.</summary>
        public string CreatedAreaId { get; private set; }

        private class Row
        {
            public StreamingLight Light;
            public CheckBox Check;
            public ComboBox Position;
        }

        public frmHueCreateArea()
        {
            Theme.ApplyForm(this);
            Text = L.T("hue_new_title");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.None;   // sizes are scaled with DialogStyle.Px

            int pad = DialogStyle.Px(this, 16), gap = DialogStyle.Px(this, 8);
            ClientSize = new Size(DialogStyle.Px(this, 520), DialogStyle.Px(this, 500));
            Controls.Add(lblTitle);
            DialogStyle.Header(this, lblTitle, null, L.T("hue_new_heading"));

            DialogStyle.FieldLabel(lblName, L.T("hue_new_name"));
            lblName.Location = new Point(pad, lblTitle.Bottom + pad);
            Controls.Add(lblName);
            txtName.Text = "AmbiBack PC";
            txtName.MaxLength = 32;
            txtName.Location = new Point(pad, lblName.Bottom + DialogStyle.Px(this, 4));
            txtName.Width = ClientSize.Width - 2 * pad;
            Controls.Add(txtName);

            DialogStyle.FieldLabel(lblLights, L.F("hue_new_lights", MaxLights));
            lblLights.Location = new Point(pad, txtName.Bottom + DialogStyle.Px(this, 14));
            Controls.Add(lblLights);

            int bottom = ClientSize.Height - DialogStyle.Px(this, 46);
            lblStatus.Location = new Point(pad, bottom - DialogStyle.Px(this, 40));
            lblStatus.Size = new Size(ClientSize.Width - 2 * pad, DialogStyle.Px(this, 34));
            lblStatus.ForeColor = Theme.SecondaryText;
            lblStatus.Text = L.T("hue_new_loading");
            Controls.Add(lblStatus);

            pnlLights.Location = new Point(pad, lblLights.Bottom + DialogStyle.Px(this, 4));
            pnlLights.Size = new Size(ClientSize.Width - 2 * pad, lblStatus.Top - gap - pnlLights.Top);
            pnlLights.AutoScroll = true;
            pnlLights.BorderStyle = BorderStyle.FixedSingle;
            pnlLights.BackColor = Theme.Card;
            Controls.Add(pnlLights);

            Controls.Add(cmdCreate);
            Controls.Add(cmdCancel);
            DialogStyle.Buttons(this, cmdCreate, L.T("hue_new_create"), cmdCancel, bottom);
            cmdCreate.Enabled = false;
            cmdCreate.Click += cmdCreate_Click;
            cmdCancel.DialogResult = DialogResult.Cancel;
            CancelButton = cmdCancel;

            Shown += (s, e) => LoadLights();
        }

        private void LoadLights()
        {
            Task.Run(() => HueClipV2.GetStreamingLights()).ContinueWith(t =>
            {
                if (IsDisposed)
                    return;
                BeginInvoke((Action)(() =>
                {
                    if (t.IsFaulted)
                    {
                        ShowError(t.Exception.GetBaseException().Message);
                        return;
                    }
                    ShowLights(t.Result);
                }));
            }, TaskScheduler.Default);
        }

        private void ShowLights(List<StreamingLight> lights)
        {
            pnlLights.SuspendLayout();
            int y = DialogStyle.Px(this, 6), rowH = DialogStyle.Px(this, 30);
            int comboW = DialogStyle.Px(this, 150);
            foreach (StreamingLight light in lights)
            {
                var row = new Row { Light = light };
                row.Check = new CheckBox
                {
                    Text = string.IsNullOrEmpty(light.Product) ? light.Name : light.Name + "  (" + light.Product + ")",
                    AutoEllipsis = true,
                    Location = new Point(DialogStyle.Px(this, 8), y + DialogStyle.Px(this, 2)),
                    Size = new Size(pnlLights.ClientSize.Width - comboW - DialogStyle.Px(this, 40), DialogStyle.Px(this, 24)),
                    BackColor = Theme.Card,
                };
                row.Position = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Location = new Point(pnlLights.ClientSize.Width - comboW - DialogStyle.Px(this, 24), y),
                    Width = comboW,
                    Enabled = false,
                };
                foreach (string key in PositionKeys)
                    row.Position.Items.Add(L.T("hue_pos_" + key));
                row.Position.SelectedIndex = 0;
                row.Check.CheckedChanged += (s, e) =>
                {
                    row.Position.Enabled = row.Check.Checked;
                    UpdateState();
                };
                pnlLights.Controls.Add(row.Check);
                pnlLights.Controls.Add(row.Position);
                rows.Add(row);
                y += rowH;
            }
            pnlLights.ResumeLayout();
            if (lights.Count == 0)
                ShowError(L.T("hue_new_none"));
            else
                UpdateState();
        }

        private int CheckedCount()
        {
            int n = 0;
            foreach (Row r in rows)
                if (r.Check.Checked) n++;
            return n;
        }

        private void UpdateState()
        {
            int n = CheckedCount();
            lblStatus.ForeColor = n > MaxLights ? Theme.Destructive : Theme.SecondaryText;
            lblStatus.Text = n > MaxLights ? L.F("hue_new_too_many", MaxLights) : L.F("hue_new_selected", n);
            cmdCreate.Enabled = n > 0 && n <= MaxLights;
        }

        private void ShowError(string message)
        {
            lblStatus.ForeColor = Theme.Destructive;
            lblStatus.Text = message;
        }

        /// <summary>Positions of the checked lights; "auto" lights are spread left to right at mid height.</summary>
        internal static List<EntertainmentChannel> ResolvePositions(IList<int> positionIndexes)
        {
            int autoCount = 0;
            foreach (int p in positionIndexes)
                if (p == 0) autoCount++;
            var result = new List<EntertainmentChannel>();
            int autoIndex = 0;
            foreach (int p in positionIndexes)
            {
                double x, z;
                if (p == 0)
                {
                    x = -1 + 2 * (autoIndex + 0.5) / autoCount;
                    z = 0;
                    autoIndex++;
                }
                else
                {
                    x = PositionXZ[p, 0];
                    z = PositionXZ[p, 1];
                }
                // y = 1: the lights stand at the screen side of the room
                result.Add(new EntertainmentChannel { X = x, Y = 1, Z = z });
            }
            return result;
        }

        private void cmdCreate_Click(object sender, EventArgs e)
        {
            string name = txtName.Text.Trim();
            if (name.Length == 0)
            {
                ShowError(L.T("hue_new_no_name"));
                return;
            }
            var chosen = new List<Row>();
            var indexes = new List<int>();
            foreach (Row r in rows)
            {
                if (r.Check.Checked)
                {
                    chosen.Add(r);
                    indexes.Add(r.Position.SelectedIndex);
                }
            }
            List<EntertainmentChannel> positions = ResolvePositions(indexes);
            var lights = new List<KeyValuePair<StreamingLight, EntertainmentChannel>>();
            for (int i = 0; i < chosen.Count; i++)
                lights.Add(new KeyValuePair<StreamingLight, EntertainmentChannel>(chosen[i].Light, positions[i]));

            Cursor = Cursors.WaitCursor;
            Enabled = false;
            try
            {
                CreatedAreaId = HueClipV2.CreateEntertainmentArea(name, lights);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                CGlobals.DebugOutput("frmHueCreateArea|Create: " + ex.Message);
                ShowError(L.F("hue_new_err", ex.Message));
            }
            finally
            {
                Cursor = Cursors.Default;
                Enabled = true;
            }
        }
    }
}
