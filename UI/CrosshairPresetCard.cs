using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ApexSyncTool.Core;

namespace ApexSyncTool.UI
{
    /// <summary>
    /// 准心预设卡片：上半部分按配置实时绘制迷你准心预览，下半部分显示名称。
    /// 单击选中、双击应用、右键删除。
    /// </summary>
    public class CrosshairPresetCard : Panel
    {
        private static readonly Color Accent = Color.FromArgb(0, 230, 118);
        private static readonly Color BorderIdle = Color.FromArgb(70, 74, 86);
        private static readonly Color PreviewBg = Color.FromArgb(22, 24, 28);
        private static readonly Color NameBg = Color.FromArgb(34, 37, 46);
        private static readonly Color TextColor = Color.FromArgb(228, 231, 240);

        private readonly CrosshairConfig _cfg;
        private readonly Image _image;
        private readonly Label _lblName;
        private bool _selected;

        public string PresetName { get; }
        public CrosshairConfig Config => _cfg;
        public event EventHandler DeleteRequested;
        public event EventHandler RenameRequested;

        public bool Selected
        {
            get => _selected;
            set { _selected = value; Invalidate(); }
        }

        public CrosshairPresetCard(string name, CrosshairConfig cfg, Image image)
        {
            PresetName = name;
            _cfg = cfg ?? new CrosshairConfig();
            _image = image;

            Size = new Size(118, 88);
            Margin = new Padding(2);
            BackColor = PreviewBg;
            Cursor = Cursors.Hand;
            DoubleBuffered = true;

            _lblName = new Label
            {
                Text = name,
                Left = 1, Top = 62, Width = 116, Height = 25,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = TextColor, BackColor = NameBg,
                Font = new Font("Microsoft YaHei UI", 8.5F),
                AutoEllipsis = true, Cursor = Cursors.Hand
            };
            // 子控件事件穿透到卡片
            _lblName.Click += (s, e) => OnClick(e);
            _lblName.DoubleClick += (s, e) => OnDoubleClick(e);
            _lblName.MouseClick += (s, e) => OnMouseClick(e);
            Controls.Add(_lblName);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 预览区背景
            using (var b = new SolidBrush(PreviewBg)) g.FillRectangle(b, 1, 1, Width - 2, 60);

            float cx = Width / 2f;
            float cy = 31f;

            if (_cfg.UseImage && _image != null)
            {
                // 图片缩放到 44×44 以内
                float k = Math.Min(1f, Math.Min(44f / _image.Width, 44f / _image.Height));
                int w = Math.Max(1, (int)(_image.Width * k));
                int h = Math.Max(1, (int)(_image.Height * k));
                g.DrawImage(_image, cx - w / 2f, cy - h / 2f, w, h);
            }
            else
            {
                var color = Color.FromArgb(_cfg.ColorArgb);
                float thickness = Math.Max(1.2f, _cfg.Thickness * 0.8f);
                using (var pen = new Pen(color, thickness))
                using (var brush = new SolidBrush(color))
                {
                    var style = (CrosshairStyle)Math.Max(0, Math.Min(3, _cfg.Style));
                    switch (style)
                    {
                        case CrosshairStyle.Cross:
                            DrawMiniCross(g, pen, cx, cy);
                            break;
                        case CrosshairStyle.Dot:
                        {
                            float r = Math.Max(2f, Math.Min(8f, _cfg.Thickness + 2f));
                            g.FillEllipse(brush, cx - r, cy - r, r * 2, r * 2);
                            break;
                        }
                        case CrosshairStyle.Circle:
                        {
                            float r = Math.Max(6f, Math.Min(22f, _cfg.Size * 0.75f));
                            g.DrawEllipse(pen, cx - r, cy - r, r * 2, r * 2);
                            break;
                        }
                        case CrosshairStyle.CrossDot:
                        {
                            DrawMiniCross(g, pen, cx, cy);
                            float r = Math.Max(2f, Math.Min(8f, _cfg.Thickness + 2f));
                            g.FillEllipse(brush, cx - r, cy - r, r * 2, r * 2);
                            break;
                        }
                    }
                }
            }

            // 选中边框
            using (var pen = new Pen(_selected ? Accent : BorderIdle, _selected ? 2f : 1f))
                g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        private void DrawMiniCross(Graphics g, Pen pen, float cx, float cy)
        {
            float arm = Math.Max(6f, Math.Min(22f, _cfg.Size * 0.75f));
            float gap = Math.Min(_cfg.Gap * 0.75f, arm - 2f);
            if (gap < 0) gap = 0;
            g.DrawLine(pen, cx - arm, cy, cx - gap, cy);
            g.DrawLine(pen, cx + gap, cy, cx + arm, cy);
            g.DrawLine(pen, cx, cy - arm, cx, cy - gap);
            g.DrawLine(pen, cx, cy + gap, cx, cy + arm);
        }

        // 右键菜单：重命名 / 删除
        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var menu = new ContextMenuStrip();
                menu.Items.Add(new ToolStripMenuItem("重命名", null, (s, ev) => RenameRequested?.Invoke(this, EventArgs.Empty)));
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(new ToolStripMenuItem("删除预设", null, (s, ev) => DeleteRequested?.Invoke(this, EventArgs.Empty)));
                menu.Show(this, e.Location);
            }
            base.OnMouseClick(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _image?.Dispose();
            base.Dispose(disposing);
        }
    }
}
