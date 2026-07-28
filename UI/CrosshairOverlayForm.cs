using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ApexSyncTool.UI
{
    public enum CrosshairStyle { Cross, Dot, Circle, CrossDot, CustomImage }

    /// <summary>
    /// 屏幕准心覆盖层：透明、置顶、点击穿透、无边框。
    /// 思路同 FPSDiyAim：魔法色透明键 + WS_EX_TRANSPARENT 点击穿透 + 矢量绘制。
    /// </summary>
    public class CrosshairOverlayForm : Form
    {
        // 魔法色 #000001：视觉透明但保留窗口区域（避免与纯黑准心冲突）
        private static readonly Color MagicColor = Color.FromArgb(0, 0, 1);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020; // 鼠标穿透
        private const int WS_EX_LAYERED = 0x00080000;     // Alpha 混合
        private const int WS_EX_TOOLWINDOW = 0x00000080;  // 不出现在任务栏/Alt-Tab
        private const int WS_EX_NOACTIVATE = 0x08000000;  // 不激活、不抢焦点

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
            => IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong) : SetWindowLong32(hWnd, nIndex, dwNewLong);

        private CrosshairStyle _style = CrosshairStyle.Dot;
        private Color _color = Color.Lime;
        private int _size = 24;          // 十字臂半长 / 圆圈半径
        private float _thickness = 2f;   // 线宽（0.1 步进）
        private int _gap = 4;            // 中心留空
        private float _dotRadius = 4f;   // 圆点半径
        private bool _crosshairVisible = true;
        private bool _clickThrough = true;
        private Image _customImage;      // 自定义 PNG 准心（已裁剪透明边）

        private readonly Timer _keepOnTopTimer;
        private Point _dragOffset;
        private bool _dragging;

        public CrosshairOverlayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(200, 200);
            BackColor = MagicColor;
            TransparencyKey = MagicColor;

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;

            // 每 5 秒强制置顶一次，对抗部分游戏抢占顶层
            _keepOnTopTimer = new Timer { Interval = 5000 };
            _keepOnTopTimer.Tick += (s, e) => KeepOnTop();
            _keepOnTopTimer.Start();

            CenterOnScreen();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation => true;

        // ══════════════════════════════════════════════
        // 配置接口
        // ══════════════════════════════════════════════
        public void SetStyle(CrosshairStyle s) { _style = s; Invalidate(); }
        public void SetColor(Color c) { _color = c; Invalidate(); }
        public void SetSize(int v) { _size = v; Invalidate(); }
        public void SetThickness(float v) { _thickness = v; _dotRadius = Math.Max(1.5f, v + 2f); Invalidate(); }
        public void SetGap(int v) { _gap = v; Invalidate(); }
        public void SetCrosshairVisible(bool v) { _crosshairVisible = v; Invalidate(); }

        // 当前配置只读快照（保存预设用）
        public CrosshairStyle CurrentStyle => _style;
        public Color CurrentColor => _color;
        public int CurrentSize => _size;
        public float CurrentThickness => _thickness;
        public int CurrentGap => _gap;
        public Image CurrentCustomImage => _customImage;

        /// <summary>设置自定义 PNG 准心（传 null 清除）。旧图片会被释放。</summary>
        public void SetCustomImage(Image img)
        {
            var old = _customImage;
            _customImage = img;
            old?.Dispose();
            Invalidate();
        }

        public Point GetCenter() => new Point(Location.X + Width / 2, Location.Y + Height / 2);

        public void MoveBy(int dx, int dy) => Location = new Point(Location.X + dx, Location.Y + dy);

        public void CenterOnScreen()
        {
            var b = Screen.PrimaryScreen.Bounds;
            Location = new Point(b.X + b.Width / 2 - Width / 2, b.Y + b.Height / 2 - Height / 2);
        }

        /// <summary>切换点击穿透。关闭后窗口可被拖动。</summary>
        public void SetClickThrough(bool on)
        {
            _clickThrough = on;
            if (!IsHandleCreated) return;
            int style = GetWindowLong(Handle, GWL_EXSTYLE);
            style = on ? (style | WS_EX_TRANSPARENT) : (style & ~WS_EX_TRANSPARENT);
            SetWindowLongPtr(Handle, GWL_EXSTYLE, new IntPtr(style));
        }

        private void KeepOnTop()
        {
            if (!IsHandleCreated) return;
            SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        // ══════════════════════════════════════════════
        // 绘制
        // ══════════════════════════════════════════════
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.Clear(MagicColor); // 全部涂成魔法色 → 透明
            if (!_crosshairVisible) return;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            float cx = ClientSize.Width / 2f;
            float cy = ClientSize.Height / 2f;

            // 自定义图片准心：按原始尺寸居中绘制
            if (_style == CrosshairStyle.CustomImage)
            {
                if (_customImage == null) return;
                g.DrawImage(_customImage, cx - _customImage.Width / 2f, cy - _customImage.Height / 2f);
                return;
            }

            using (var pen = new Pen(_color, _thickness) { StartCap = LineCap.Flat, EndCap = LineCap.Flat })
            using (var brush = new SolidBrush(_color))
            {
                switch (_style)
                {
                    case CrosshairStyle.Cross:
                        DrawCross(g, pen, cx, cy);
                        break;
                    case CrosshairStyle.Dot:
                        g.FillEllipse(brush, cx - _dotRadius, cy - _dotRadius, _dotRadius * 2, _dotRadius * 2);
                        break;
                    case CrosshairStyle.Circle:
                        g.DrawEllipse(pen, cx - _size, cy - _size, _size * 2, _size * 2);
                        break;
                    case CrosshairStyle.CrossDot:
                        DrawCross(g, pen, cx, cy);
                        g.FillEllipse(brush, cx - _dotRadius, cy - _dotRadius, _dotRadius * 2, _dotRadius * 2);
                        break;
                }
            }
        }

        private void DrawCross(Graphics g, Pen pen, float cx, float cy)
        {
            int inner = Math.Min(_gap, _size);
            g.DrawLine(pen, cx - _size, cy, cx - inner, cy);
            g.DrawLine(pen, cx + inner, cy, cx + _size, cy);
            g.DrawLine(pen, cx, cy - _size, cx, cy - inner);
            g.DrawLine(pen, cx, cy + inner, cx, cy + _size);
        }

        // ══════════════════════════════════════════════
        // 拖动（仅在关闭点击穿透时生效）
        // ══════════════════════════════════════════════
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (!_clickThrough && e.Button == MouseButtons.Left) { _dragging = true; _dragOffset = e.Location; }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_dragging)
            {
                var sp = PointToScreen(e.Location);
                Location = new Point(sp.X - _dragOffset.X, sp.Y - _dragOffset.Y);
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e) { _dragging = false; base.OnMouseUp(e); }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _keepOnTopTimer?.Stop();
            _keepOnTopTimer?.Dispose();
            _customImage?.Dispose();
            _customImage = null;
            base.OnFormClosing(e);
        }
    }
}
