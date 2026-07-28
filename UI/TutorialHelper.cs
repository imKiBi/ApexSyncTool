using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ApexSyncTool.UI
{
    /// <summary>教学气泡相对目标控件的位置</summary>
    public enum HintPlacement { Right, Below, Above }

    /// <summary>
    /// 新手教学高亮框：用四条细边条框住目标控件（不遮挡点击），
    /// 并在旁边显示一个可点击关闭的提示气泡。所有元素加在宿主窗体上（按屏幕坐标换算）。
    /// </summary>
    public class TutorialHelper
    {
        private static readonly Color Accent = Color.FromArgb(0, 230, 118);
        private static readonly Color HintFore = Color.FromArgb(20, 22, 26);

        private readonly Form _host;
        private readonly List<Control> _parts = new List<Control>();
        private readonly Timer _autoTimer;

        /// <summary>提示气泡被点击（用户已知晓）时触发</summary>
        public event Action HintDismissed;

        public TutorialHelper(Form host)
        {
            _host = host;
            // 若用户未按标注位置操作，单个标注最多显示 5 秒后自动进入下一步
            _autoTimer = new Timer { Interval = 5000 };
            _autoTimer.Tick += (s, e) =>
            {
                if (_host.IsDisposed) { _autoTimer.Stop(); return; }
                Hide();
                HintDismissed?.Invoke();
            };
        }

        public bool Active => _parts.Count > 0;

        /// <summary>框住目标控件并显示提示。placement 决定气泡位置（右侧/下方/上方）。</summary>
        public void Show(Control target, string hint, HintPlacement placement)
        {
            Hide();
            if (target == null || target.IsDisposed) return;

            var rect = _host.RectangleToClient(target.RectangleToScreen(target.ClientRectangle));
            rect.Inflate(4, 4);
            const int t = 3;

            AddPart(MakeBar(rect.Left, rect.Top, rect.Width, t));
            AddPart(MakeBar(rect.Left, rect.Bottom - t, rect.Width, t));
            AddPart(MakeBar(rect.Left, rect.Top, t, rect.Height));
            AddPart(MakeBar(rect.Right - t, rect.Top, t, rect.Height));

            var lbl = new Label
            {
                Text = hint,
                AutoSize = true,
                BackColor = Accent,
                ForeColor = HintFore,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                Padding = new Padding(7, 5, 7, 5),
                Cursor = Cursors.Hand
            };
            switch (placement)
            {
                case HintPlacement.Below:
                    lbl.Location = new Point(rect.Left, rect.Bottom + 8);
                    break;
                case HintPlacement.Above:
                {
                    // 上方提示：气泡右边缘精确对齐目标右边缘
                    // 用 GetPreferredSize 取真实尺寸（AutoSize 标签在布局前 Width/Height 可能不准）
                    var pref = lbl.GetPreferredSize(Size.Empty);
                    lbl.Location = new Point(Math.Max(4, rect.Right - pref.Width), rect.Top - pref.Height - 8);
                    break;
                }
                default:
                    lbl.Location = new Point(rect.Right + 10, rect.Top + Math.Max(0, (rect.Height - lbl.Height) / 2));
                    break;
            }
            lbl.Click += (s, e) => { Hide(); HintDismissed?.Invoke(); };
            _host.Controls.Add(lbl);
            lbl.BringToFront();
            _parts.Add(lbl);

            _autoTimer.Stop();
            _autoTimer.Start();
        }

        public void Hide()
        {
            _autoTimer.Stop();
            foreach (var c in _parts)
            {
                _host.Controls.Remove(c);
                c.Dispose();
            }
            _parts.Clear();
        }

        private Panel MakeBar(int x, int y, int w, int h) => new Panel
        {
            Left = x, Top = y, Width = w, Height = h, BackColor = Accent
        };

        private void AddPart(Panel p)
        {
            _host.Controls.Add(p);
            p.BringToFront();
            _parts.Add(p);
        }
    }
}
