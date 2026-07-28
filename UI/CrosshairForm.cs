using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ApexSyncTool.Core;

namespace ApexSyncTool.UI
{
    /// <summary>
    /// 屏幕准心控制面板：作为主窗口右侧的扩展面板（约 450 宽）展开/收起，
    /// 提供样式/颜色/滑杆/位置微调等全部配置，并管理准心预设（含自定义 PNG 图片）。
    /// 屏幕上的准心由独立的 CrosshairOverlayForm 渲染，收起面板不影响准心显示。
    /// </summary>
    public class CrosshairForm : Form
    {
        private static readonly Color Bg = Color.FromArgb(24, 26, 32);
        private static readonly Color PanelBg = Color.FromArgb(34, 37, 46);
        private static readonly Color TextColor = Color.FromArgb(228, 231, 240);
        private static readonly Color SubColor = Color.FromArgb(140, 146, 160);
        private static readonly Color Accent = Color.FromArgb(0, 230, 118);

        private static readonly Color[] PresetColors =
        {
            Color.Lime,
            Color.FromArgb(255, 60, 60),
            Color.FromArgb(0, 220, 255),
            Color.FromArgb(255, 230, 0),
            Color.White,
            Color.FromArgb(255, 0, 200)
        };

        private readonly CrosshairOverlayForm _overlay;
        private readonly CrosshairPresetManager _presetMgr;
        private readonly List<Button> _swatches = new List<Button>();

        private ComboBox cmbStyle;
        private FlowLayoutPanel flpSliders;
        private Panel rowThickness, rowSize, rowGap, rowOpacity;
        private TrackBar tbThickness, tbSize, tbGap, tbOpacity;
        private Label lblThicknessVal, lblSizeVal, lblGapVal, lblOpacityVal;
        private CheckBox chkVisible, chkDrag;
        private FlowLayoutPanel flpPresets;

        private int _lastStyleIndex = 1;
        private bool _forceClose;   // CloseAll() 置位后才允许真正关闭

        public CrosshairForm()
        {
            _overlay = new CrosshairOverlayForm();
            _presetMgr = new CrosshairPresetManager();
            BuildUi();
        }

        /// <summary>彻底关闭：面板 + 屏幕准心一起关掉</summary>
        public void CloseAll()
        {
            _forceClose = true;
            Close();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (!_overlay.Visible) _overlay.Show();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 标题栏 × = 收起面板（屏幕准心保持显示）
            if (!_forceClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }
            _overlay.Close();
            _overlay.Dispose();
            base.OnFormClosing(e);
        }

        private void BuildUi()
        {
            SuspendLayout();

            Text = "屏幕准心";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.Manual; // 位置由主窗口停靠逻辑决定
            ClientSize = new Size(450, 646);
            BackColor = Bg;
            ForeColor = TextColor;
            Font = new Font("Microsoft YaHei UI", 9F);

            // ══ 样式 ══
            Controls.Add(MkLabel("样式", 16, 12, SubColor));
            cmbStyle = new ComboBox
            {
                Left = 16, Top = 30, Width = 418, DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = PanelBg, ForeColor = TextColor, FlatStyle = FlatStyle.Flat
            };
            cmbStyle.Items.AddRange(new object[] { "十字", "圆点", "圆圈", "十字 + 圆点", "自定义图片" });
            cmbStyle.SelectedIndex = 1; // 默认圆点
            cmbStyle.SelectedIndexChanged += (s, e) =>
            {
                if (cmbStyle.SelectedIndex == 4 && _overlay.CurrentCustomImage == null)
                {
                    // 切到"自定义图片"但还没有图 → 弹出选择窗口
                    if (!ImportCustomImage())
                    {
                        cmbStyle.SelectedIndex = _lastStyleIndex;
                        return;
                    }
                }
                _lastStyleIndex = cmbStyle.SelectedIndex;
                _overlay.SetStyle((CrosshairStyle)cmbStyle.SelectedIndex);
                UpdateSliderVisibility();
            };
            Controls.Add(cmbStyle);

            // ══ 颜色 ══
            Controls.Add(MkLabel("颜色", 16, 64, SubColor));
            int sx = 16;
            foreach (var c in PresetColors)
            {
                var sw = new Button
                {
                    Left = sx, Top = 82, Width = 26, Height = 26,
                    BackColor = c, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
                };
                sw.FlatAppearance.BorderSize = 1;
                sw.FlatAppearance.BorderColor = Color.FromArgb(70, 74, 86);
                sw.Tag = c;
                sw.Click += Swatch_Click;
                _swatches.Add(sw);
                Controls.Add(sw);
                sx += 30;
            }
            Controls.Add(MkButton("自定义颜色", 204, 80, 230, 28, (s, e) =>
            {
                using (var cd = new ColorDialog())
                {
                    cd.FullOpen = true;
                    if (cd.ShowDialog(this) == DialogResult.OK)
                    {
                        _overlay.SetColor(cd.Color);
                        ClearSwatchHighlight();
                    }
                }
            }));

            // ══ 开关 ══
            chkVisible = MkCheckBox("显示准心", 16, 116, true);
            chkVisible.CheckedChanged += (s, e) => _overlay.SetCrosshairVisible(chkVisible.Checked);
            Controls.Add(chkVisible);

            chkDrag = MkCheckBox("自由拖动准心", 122, 116, false);
            chkDrag.CheckedChanged += (s, e) => _overlay.SetClickThrough(!chkDrag.Checked);
            Controls.Add(chkDrag);

            Controls.Add(new Label
            {
                Text = "勾选后可直接把屏幕上的准心拖到任意位置；取消勾选恢复点击穿透。",
                Left = 16, Top = 140, Size = new Size(418, 18), ForeColor = SubColor,
                Font = new Font("Microsoft YaHei UI", 8F)
            });

            // ══ 滑杆（圆点/图片时自动收起无关项） ══
            flpSliders = new FlowLayoutPanel
            {
                Left = 16, Top = 164, Width = 418, Height = 254,
                FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = false
            };
            rowThickness = MakeSliderRow("粗细", 1, 100, 20, out tbThickness, out lblThicknessVal, "2.0");
            tbThickness.ValueChanged += (s, e) =>
            {
                float v = tbThickness.Value / 10f;
                _overlay.SetThickness(v);
                lblThicknessVal.Text = v.ToString("0.0");
            };
            rowSize = MakeSliderRow("大小", 5, 80, 24, out tbSize, out lblSizeVal, "24");
            tbSize.ValueChanged += (s, e) => { _overlay.SetSize(tbSize.Value); lblSizeVal.Text = tbSize.Value.ToString(); };
            rowGap = MakeSliderRow("间隙", 0, 30, 4, out tbGap, out lblGapVal, "4");
            tbGap.ValueChanged += (s, e) => { _overlay.SetGap(tbGap.Value); lblGapVal.Text = tbGap.Value.ToString(); };
            rowOpacity = MakeSliderRow("透明度", 30, 100, 100, out tbOpacity, out lblOpacityVal, "100%");
            tbOpacity.ValueChanged += (s, e) => { _overlay.Opacity = tbOpacity.Value / 100.0; lblOpacityVal.Text = tbOpacity.Value + "%"; };
            flpSliders.Controls.AddRange(new Control[] { rowThickness, rowSize, rowGap, rowOpacity });
            Controls.Add(flpSliders);

            // ══ 位置微调 ══
            Controls.Add(MkLabel("位置", 16, 430, SubColor));
            Controls.Add(MkButton("←", 56, 424, 34, 30, (s, e) => _overlay.MoveBy(-5, 0)));
            Controls.Add(MkButton("↑", 94, 424, 34, 30, (s, e) => _overlay.MoveBy(0, -5)));
            Controls.Add(MkButton("↓", 132, 424, 34, 30, (s, e) => _overlay.MoveBy(0, 5)));
            Controls.Add(MkButton("→", 170, 424, 34, 30, (s, e) => _overlay.MoveBy(5, 0)));
            Controls.Add(MkButton("居中", 212, 424, 70, 30, (s, e) => _overlay.CenterOnScreen()));

            // ══ 准心预设区 ══
            Controls.Add(new Panel { Left = 16, Top = 462, Width = 418, Height = 1, BackColor = Color.FromArgb(58, 62, 74) });
            Controls.Add(MkLabel("准心预设", 16, 470, SubColor));
            Controls.Add(MkButton("保存预设", 150, 466, 88, 26, BtnSavePreset_Click));
            Controls.Add(MkButton("应用准心", 244, 466, 88, 26, BtnApplyPreset_Click));
            Controls.Add(MkButton("导入图片准心", 338, 466, 96, 26, (s, e) =>
            {
                if (ImportCustomImage())
                {
                    cmbStyle.SelectedIndex = 4;
                    _lastStyleIndex = 4;
                    _overlay.SetStyle(CrosshairStyle.CustomImage);
                    UpdateSliderVisibility();
                }
            }));

            flpPresets = new FlowLayoutPanel
            {
                Left = 16, Top = 500, Width = 418, Height = 92,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                AutoScroll = true, BackColor = Bg
            };
            Controls.Add(flpPresets);
            RefreshPresetCards();

            // ══ 收起 / 关闭准心 ══
            var btnCollapse = MkButton("收起", 16, 598, 205, 34, (s, e) => Hide());
            btnCollapse.FlatAppearance.BorderColor = Accent;
            Controls.Add(btnCollapse);
            var btnCloseAll = MkButton("关闭准心", 229, 598, 205, 34, (s, e) => CloseAll());
            btnCloseAll.FlatAppearance.BorderColor = Color.FromArgb(150, 196, 43, 28);
            Controls.Add(btnCloseAll);

            UpdateSliderVisibility();

            ResumeLayout(false);
            PerformLayout();
        }

        /// <summary>圆点隐藏 大小/间隙；自定义图片只保留 透明度</summary>
        private void UpdateSliderVisibility()
        {
            bool isDot = cmbStyle.SelectedIndex == 1;
            bool isImage = cmbStyle.SelectedIndex == 4;
            rowThickness.Visible = !isImage;
            rowSize.Visible = !isDot && !isImage;
            rowGap.Visible = !isDot && !isImage;
        }

        // ══════════════════════════════════════════════
        // 预设功能
        // ══════════════════════════════════════════════
        private void RefreshPresetCards()
        {
            var old = new List<Control>(flpPresets.Controls.Cast<Control>());
            flpPresets.Controls.Clear();
            foreach (var c in old) c.Dispose();
            var names = _presetMgr.ListPresets();
            if (names.Count == 0)
            {
                flpPresets.Controls.Add(new Label
                {
                    Text = "还没有预设 —— 调好准心后点「保存预设」，下次一键应用",
                    AutoSize = true, ForeColor = SubColor, Margin = new Padding(4, 34, 0, 0),
                    Font = new Font("Microsoft YaHei UI", 9F)
                });
                return;
            }
            foreach (var name in names)
            {
                var cfg = _presetMgr.LoadConfig(name);
                if (cfg == null) continue;
                Image img = cfg.UseImage ? _presetMgr.LoadImage(name) : null;
                var card = new CrosshairPresetCard(name, cfg, img);
                card.Click += (s, e) => SelectPresetCard(card);
                card.DoubleClick += (s, e) => { SelectPresetCard(card); ApplyPreset(card.PresetName); };
                card.RenameRequested += (s, e) => RenamePreset(card.PresetName);
                card.DeleteRequested += (s, e) => DeletePreset(card.PresetName);
                flpPresets.Controls.Add(card);
            }
        }

        private void SelectPresetCard(CrosshairPresetCard target)
        {
            foreach (Control c in flpPresets.Controls)
                if (c is CrosshairPresetCard pc) pc.Selected = ReferenceEquals(pc, target);
        }

        private CrosshairPresetCard GetSelectedCard()
        {
            foreach (Control c in flpPresets.Controls)
                if (c is CrosshairPresetCard pc && pc.Selected) return pc;
            return null;
        }

        private void BtnSavePreset_Click(object sender, EventArgs e)
        {
            string def = "预设" + (flpPresets.Controls.Count + 1);
            string name = InputDialog.Show(this, "保存准心预设", "请输入预设名称：", def);
            name = CrosshairPresetManager.SanitizeName(name);
            if (name == null) return;

            bool ok = _presetMgr.SavePreset(name, BuildConfigFromUi(), _overlay.CurrentCustomImage);
            if (!ok)
            {
                MessageBox.Show(this, "预设保存失败，请重试。", "屏幕准心", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            RefreshPresetCards();
            foreach (Control c in flpPresets.Controls)
                if (c is CrosshairPresetCard pc && pc.PresetName == name) SelectPresetCard(pc);
        }

        private void BtnApplyPreset_Click(object sender, EventArgs e)
        {
            var card = GetSelectedCard();
            if (card == null)
            {
                MessageBox.Show(this, "请先在下方点击选择一个预设卡片。", "屏幕准心", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ApplyPreset(card.PresetName);
        }

        private void ApplyPreset(string name)
        {
            var cfg = _presetMgr.LoadConfig(name);
            if (cfg == null) return;
            Image img = cfg.UseImage ? _presetMgr.LoadImage(name) : null;
            ApplyConfig(cfg, img);
        }

        private CrosshairConfig BuildConfigFromUi()
        {
            return new CrosshairConfig
            {
                Style = cmbStyle.SelectedIndex,
                ColorArgb = _overlay.CurrentColor.ToArgb(),
                Size = tbSize.Value,
                Thickness = tbThickness.Value / 10f,
                Gap = tbGap.Value,
                Opacity = tbOpacity.Value,
                UseImage = cmbStyle.SelectedIndex == 4 && _overlay.CurrentCustomImage != null
            };
        }

        /// <summary>把一套预设配置同步到覆盖层与界面控件</summary>
        private void ApplyConfig(CrosshairConfig cfg, Image img)
        {
            if (cfg == null) return;
            _overlay.SetCustomImage(cfg.UseImage ? img : null);

            tbThickness.Value = Clamp((int)Math.Round(cfg.Thickness * 10), tbThickness.Minimum, tbThickness.Maximum);
            tbSize.Value = Clamp(cfg.Size, tbSize.Minimum, tbSize.Maximum);
            tbGap.Value = Clamp(cfg.Gap, tbGap.Minimum, tbGap.Maximum);
            tbOpacity.Value = Clamp(cfg.Opacity == 0 ? 100 : cfg.Opacity, tbOpacity.Minimum, tbOpacity.Maximum);

            int idx = cfg.UseImage ? 4 : Math.Max(0, Math.Min(3, cfg.Style));
            cmbStyle.SelectedIndex = idx;
            _lastStyleIndex = idx;
            _overlay.SetStyle((CrosshairStyle)idx);
            _overlay.SetColor(Color.FromArgb(cfg.ColorArgb));
            ClearSwatchHighlight();
            UpdateSliderVisibility();
        }

        private static int Clamp(int v, int lo, int hi) => Math.Max(lo, Math.Min(hi, v));

        private void RenamePreset(string oldName)
        {
            string newName = InputDialog.Show(this, "重命名预设", "请输入新名称：", oldName);
            newName = CrosshairPresetManager.SanitizeName(newName);
            if (newName == null || newName == oldName) return;

            if (!_presetMgr.RenamePreset(oldName, newName))
            {
                MessageBox.Show(this, "重命名失败：名称已被使用或文件夹被占用。", "屏幕准心",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            RefreshPresetCards();
            foreach (Control c in flpPresets.Controls)
                if (c is CrosshairPresetCard pc && pc.PresetName == newName) SelectPresetCard(pc);
        }

        private void DeletePreset(string name)
        {
            if (MessageBox.Show(this, $"确定删除预设「{name}」吗？\n（会放入回收站）", "删除预设",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            _presetMgr.DeletePreset(name);
            RefreshPresetCards();
        }

        // ══════════════════════════════════════════════
        // 自定义 PNG 准心
        // ══════════════════════════════════════════════
        /// <summary>弹出文件窗口选择 PNG，自动裁剪透明边并应用到覆盖层。返回是否成功。</summary>
        private bool ImportCustomImage()
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "选择准心 PNG 图片";
                ofd.Filter = "PNG 图片|*.png|所有文件|*.*";
                if (ofd.ShowDialog(this) != DialogResult.OK || string.IsNullOrEmpty(ofd.FileName)) return false;
                try
                {
                    Image src;
                    using (var fs = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read))
                        src = Image.FromStream(fs);
                    var cropped = AutoCropTransparent(src);
                    src.Dispose();
                    _overlay.SetCustomImage(cropped);
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "图片加载失败：" + ex.Message, "屏幕准心",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
        }

        /// <summary>超大图先缩到 256px 内，再裁掉全透明边缘</summary>
        private static Bitmap AutoCropTransparent(Image src)
        {
            Image work = src;
            Bitmap scaled = null;
            int maxDim = Math.Max(src.Width, src.Height);
            if (maxDim > 256)
            {
                float k = 256f / maxDim;
                scaled = new Bitmap(Math.Max(1, (int)(src.Width * k)), Math.Max(1, (int)(src.Height * k)));
                using (var g = Graphics.FromImage(scaled))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(src, 0, 0, scaled.Width, scaled.Height);
                }
                work = scaled;
            }

            using (var bmp = new Bitmap(work))
            {
                scaled?.Dispose();
                int minX = bmp.Width, minY = bmp.Height, maxX = -1, maxY = -1;
                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        if (bmp.GetPixel(x, y).A > 8)
                        {
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                }
                if (maxX < 0) return new Bitmap(bmp); // 全透明，原样返回
                int w = maxX - minX + 1, h = maxY - minY + 1;
                var cropped = new Bitmap(w, h);
                using (var g = Graphics.FromImage(cropped))
                    g.DrawImage(bmp, new Rectangle(0, 0, w, h), new Rectangle(minX, minY, w, h), GraphicsUnit.Pixel);
                return cropped;
            }
        }

        // ══════════════════════════════════════════════
        // 控件事件
        // ══════════════════════════════════════════════
        private void Swatch_Click(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is Color c)
            {
                _overlay.SetColor(c);
                ClearSwatchHighlight();
                btn.FlatAppearance.BorderSize = 2;
                btn.FlatAppearance.BorderColor = Accent;
            }
        }

        private void ClearSwatchHighlight()
        {
            foreach (var sw in _swatches)
            {
                sw.FlatAppearance.BorderSize = 1;
                sw.FlatAppearance.BorderColor = Color.FromArgb(70, 74, 86);
            }
        }

        // ══════════════════════════════════════════════
        // 控件工厂
        // ══════════════════════════════════════════════
        private Label MkLabel(string text, int x, int y, Color color) => new Label
        {
            Text = text, Left = x, Top = y, AutoSize = true, ForeColor = color
        };

        private Panel MakeSliderRow(string name, int min, int max, int value,
            out TrackBar tb, out Label val, string initialText)
        {
            var row = new Panel { Width = 418, Height = 56, BackColor = Bg, Margin = new Padding(0, 0, 0, 6) };
            row.Controls.Add(new Label
            {
                Text = name, Left = 2, Top = 2, AutoSize = true, ForeColor = SubColor,
                Font = new Font("Microsoft YaHei UI", 8.5F)
            });
            val = new Label
            {
                Text = initialText, Left = 358, Top = 0, Width = 56,
                TextAlign = ContentAlignment.TopRight, ForeColor = Accent,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold)
            };
            row.Controls.Add(val);
            tb = new TrackBar
            {
                Left = 0, Top = 22, Width = 414, Height = 32,
                Minimum = min, Maximum = max, Value = value,
                TickStyle = TickStyle.None, BackColor = Bg
            };
            row.Controls.Add(tb);
            return row;
        }

        private CheckBox MkCheckBox(string text, int x, int y, bool checked_) => new CheckBox
        {
            Text = text, Left = x, Top = y, AutoSize = true,
            Checked = checked_, ForeColor = TextColor, FlatStyle = FlatStyle.Flat
        };

        private Button MkButton(string text, int x, int y, int w, int h, EventHandler onClick)
        {
            var b = new Button
            {
                Text = text, Left = x, Top = y, Width = w, Height = h,
                FlatStyle = FlatStyle.Flat, BackColor = PanelBg, ForeColor = TextColor,
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(70, 74, 86);
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(46, 50, 62);
            b.Click += onClick;
            return b;
        }
    }

    /// <summary>深色主题简易输入框（保存预设命名用）</summary>
    internal static class InputDialog
    {
        public static string Show(IWin32Window owner, string title, string prompt, string defaultValue)
        {
            using (var dlg = new Form())
            {
                dlg.Text = title;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.ClientSize = new Size(320, 122);
                dlg.BackColor = Color.FromArgb(24, 26, 32);
                dlg.Font = new Font("Microsoft YaHei UI", 9F);

                var lbl = new Label
                {
                    Text = prompt, Left = 14, Top = 12, AutoSize = true,
                    ForeColor = Color.FromArgb(228, 231, 240)
                };
                var txt = new TextBox
                {
                    Left = 14, Top = 36, Width = 290, Text = defaultValue ?? "",
                    BackColor = Color.FromArgb(34, 37, 46), ForeColor = Color.White
                };
                var ok = new Button
                {
                    Text = "确定", Left = 148, Top = 78, Width = 74, Height = 28,
                    FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(34, 37, 46),
                    ForeColor = Color.FromArgb(228, 231, 240), DialogResult = DialogResult.OK
                };
                var cancel = new Button
                {
                    Text = "取消", Left = 230, Top = 78, Width = 74, Height = 28,
                    FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(34, 37, 46),
                    ForeColor = Color.FromArgb(228, 231, 240), DialogResult = DialogResult.Cancel
                };
                dlg.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });
                dlg.AcceptButton = ok;
                dlg.CancelButton = cancel;
                txt.SelectAll();
                return dlg.ShowDialog(owner) == DialogResult.OK ? txt.Text.Trim() : null;
            }
        }
    }
}
