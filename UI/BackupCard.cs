using System;
using System.Drawing;
using System.Windows.Forms;
using ApexSyncTool.Core;

namespace ApexSyncTool.UI
{
    /// <summary>
    /// Custom control for displaying a backup card
    /// </summary>
    public class BackupCard : Panel
    {
        private Label lblTitle;
        private Label lblPreview;
        private Button btnMenu;
        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem _menuKeybindEdit;
        private bool _isSelected = false;

        public string BackupPath { get; set; }
        public string BackupName { get; set; }
        public ConfigPreview PreviewData { get; set; }

        public event EventHandler<BackupCardEventArgs> RenameRequested;
        public event EventHandler<BackupCardEventArgs> DeleteRequested;
        public event EventHandler<BackupCardEventArgs> DuplicateRequested;
        public event EventHandler<BackupCardEventArgs> OpenFolderRequested;
        public event EventHandler<BackupCardEventArgs> SelectionChanged;
        public event EventHandler<BackupCardEventArgs> AdvancedPreviewRequested;
        public event EventHandler<BackupCardEventArgs> KeybindEditRequested;
        public event EventHandler<BackupCardEventArgs> ExportRequested;

        public BackupCard()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.BorderStyle = BorderStyle.FixedSingle;
            this.Size = new Size(220, 110);
            this.BackColor = Color.White;
            this.Padding = new Padding(5);
            this.Cursor = Cursors.Hand;

            // Title Label
            lblTitle = new Label
            {
                Text = "2026-07-20_11:56",
                Location = new Point(5, 5),
                Size = new Size(200, 20),
                Font = new Font("Microsoft Sans Serif", 10, FontStyle.Bold),
                AutoEllipsis = true,
                Cursor = Cursors.Hand
            };

            // Preview Label
            lblPreview = new Label
            {
                Text = "灵敏: 2.5\nFOV: 1.55\n音乐: 0.206",
                Location = new Point(5, 28),
                Size = new Size(200, 50),
                Font = new Font("Microsoft Sans Serif", 9),
                AutoSize = false,
                Cursor = Cursors.Hand
            };

            // Menu Button
            btnMenu = new Button
            {
                Text = "⋮",
                Location = new Point(195, 80),
                Size = new Size(20, 20),
                Font = new Font("Microsoft Sans Serif", 12),
                FlatStyle = FlatStyle.Flat,
                TabStop = false
            };
            btnMenu.Click += BtnMenu_Click;

            // Context Menu
            contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add(new ToolStripMenuItem("高级预览", null, (s, e) => AdvancedPreviewRequested?.Invoke(this, new BackupCardEventArgs { BackupPath = BackupPath })));
            _menuKeybindEdit = new ToolStripMenuItem("键位编辑", null, (s, e) => KeybindEditRequested?.Invoke(this, new BackupCardEventArgs { BackupPath = BackupPath }));
            contextMenu.Items.Add(_menuKeybindEdit);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(new ToolStripMenuItem("重命名", null, (s, e) => RenameRequested?.Invoke(this, new BackupCardEventArgs { BackupPath = BackupPath })));
            contextMenu.Items.Add(new ToolStripMenuItem("删除", null, (s, e) => DeleteRequested?.Invoke(this, new BackupCardEventArgs { BackupPath = BackupPath })));
            contextMenu.Items.Add(new ToolStripMenuItem("复制", null, (s, e) => DuplicateRequested?.Invoke(this, new BackupCardEventArgs { BackupPath = BackupPath })));
            contextMenu.Items.Add(new ToolStripMenuItem("打开文件夹", null, (s, e) => OpenFolderRequested?.Invoke(this, new BackupCardEventArgs { BackupPath = BackupPath })));
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(new ToolStripMenuItem("导出到桌面", null, (s, e) => ExportRequested?.Invoke(this, new BackupCardEventArgs { BackupPath = BackupPath })));

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblPreview);
            this.Controls.Add(btnMenu);

            // 左键点击选中
            this.Click += BackupCard_Click;
            this.MouseEnter += BackupCard_MouseEnter;
            this.MouseLeave += BackupCard_MouseLeave;
            this.MouseClick += BackupCard_MouseClick;
            this.DoubleClick += BackupCard_DoubleClick;

            // 子控件事件穿透
            lblTitle.Click += BackupCard_Click;
            lblTitle.MouseEnter += BackupCard_MouseEnter;
            lblTitle.MouseLeave += BackupCard_MouseLeave;
            lblTitle.MouseClick += BackupCard_MouseClick;
            lblTitle.DoubleClick += BackupCard_DoubleClick;
            lblPreview.Click += BackupCard_Click;
            lblPreview.MouseEnter += BackupCard_MouseEnter;
            lblPreview.MouseLeave += BackupCard_MouseLeave;
            lblPreview.MouseClick += BackupCard_MouseClick;
            lblPreview.DoubleClick += BackupCard_DoubleClick;
        }

        public void UpdateContent(string name, ConfigPreview preview, string path, bool isNaraka = false)
        {
            BackupName = name;
            BackupPath = path;
            PreviewData = preview;

            lblTitle.Text = name;

            // 永劫无间模式：隐藏三行预览与"键位编辑"菜单项
            lblPreview.Visible = !isNaraka;
            _menuKeybindEdit.Visible = !isNaraka;

            if (!isNaraka && preview != null)
            {
                string musicPercent = "N/A";
                if (double.TryParse(preview.SoundMusic, out double musicVal))
                {
                    musicPercent = ((int)Math.Round(musicVal * 100)).ToString() + "%";
                }

                lblPreview.Text = "灵敏: " + preview.MouseSensitivity + "\n" +
                                 "FOV: " + preview.FOV + "\n" +
                                 "音乐音量: " + musicPercent;
            }
        }

        private void BtnMenu_Click(object sender, EventArgs e)
        {
            contextMenu.Show(btnMenu, 0, btnMenu.Height);
        }

        private void BackupCard_Click(object sender, EventArgs e)
        {
            SelectionChanged?.Invoke(this, new BackupCardEventArgs { BackupPath = BackupPath });
        }

        private void BackupCard_MouseClick(object sender, MouseEventArgs e)
        {
            // 右键点击展开菜单
            if (e.Button == MouseButtons.Right)
            {
                contextMenu.Show(this, e.Location);
            }
        }

        private void BackupCard_DoubleClick(object sender, EventArgs e)
        {
            // 双击快捷打开高级预览
            AdvancedPreviewRequested?.Invoke(this, new BackupCardEventArgs { BackupPath = BackupPath });
        }

        private void BackupCard_MouseEnter(object sender, EventArgs e)
        {
            if (!_isSelected)
                this.BackColor = Color.AliceBlue;
        }

        private void BackupCard_MouseLeave(object sender, EventArgs e)
        {
            if (!_isSelected)
                this.BackColor = Color.White;
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            this.BackColor = selected ? Color.LightBlue : Color.White;
        }
    }

    public class BackupCardEventArgs : EventArgs
    {
        public string BackupPath { get; set; }
    }
}
