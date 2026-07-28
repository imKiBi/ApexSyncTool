using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using ApexSyncTool.Core;
using ApexSyncTool.UI;

namespace ApexSyncTool
{
    public enum GameMode { Apex, Naraka }

    public partial class MainForm : Form
    {
        private ApexPathManager _pathManager;
        private ConfigParser _configParser;
        private BackupManager _backupManager;
        private SteamAccountManager _steamAccountManager;
        private Logger _logger;
        private string _selectedBackupPath;
        private Dictionary<string, BackupCard> _backupCards = new Dictionary<string, BackupCard>();
        private Timer _toastTimer;
        private Timer _launchCooldownTimer;

        private GameMode _gameMode = GameMode.Apex;
        private string _narakaPath;
        private string _narakaConfigPath;
        private bool _updatingSteamTags = false;
        private CrosshairForm _crosshairForm;
        private TutorialHelper _tutorial;
        private bool _tutStep1Done;
        private bool _tutStep2Done;
        private bool _tutStep3Done;

        public MainForm()
        {
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                _logger = new Logger();
                _pathManager = new ApexPathManager();
                _configParser = new ConfigParser();
                _backupManager = new BackupManager(_pathManager);
                _backupManager.SetBackupPath(GetBackupRoot());
                MigrateLegacyApexBackups();
                _steamAccountManager = new SteamAccountManager();

                _logger.Log("应用启动");

                this.FormBorderStyle = FormBorderStyle.FixedSingle;
                this.MaximizeBox = false;
                this.MinimizeBox = true;

                _toastTimer = new Timer { Interval = 3000 };
                _toastTimer.Tick += (s, e) => { _toastTimer.Stop(); lblToast.Visible = false; };

                // 新手教学
                _tutorial = new TutorialHelper(this);
                _tutorial.HintDismissed += AdvanceTutorial;
                LoadTutorialState();

                // 备份列表空白处右键 → 导入菜单
                flpBackupCards.MouseClick += FlpBackupCards_MouseClick;

                _launchCooldownTimer = new Timer { Interval = 10000 };
                _launchCooldownTimer.Tick += (s, e) =>
                {
                    _launchCooldownTimer.Stop();
                    btnLaunchApex.Enabled = true;
                    btnLaunchApex.Text = _gameMode == GameMode.Apex ? "启动APEX" : "永劫启动";
                };

                RefreshGameStatus();
                LoadBackupCards();
                LoadSteamAccounts();
                InitializeSteamTags();
            }
            catch (Exception ex)
            {
                _logger?.LogError("初始化失败", ex);
                MessageBox.Show("应用初始化失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        private void ShowToast(string message, bool isError = false)
        {
            lblToast.Text = (isError ? "✗ " : "✓ ") + message;
            lblToast.ForeColor = isError ? Color.FromArgb(196, 43, 28) : Color.FromArgb(0, 120, 60);
            lblToast.Visible = true;
            lblToast.BringToFront();
            _toastTimer.Stop();
            _toastTimer.Start();
        }

        // ══════════════════════════════════════════════
        // 新手教学
        // ══════════════════════════════════════════════
        private string TutorialStatePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tutorial.dat");

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (!_tutStep1Done) ShowTutorialStep1();
            else if (_tutStep2Done && !_tutStep3Done) ShowTutorialStep3();
        }

        private void ShowTutorialStep1()
        {
            _tutorial.Show(btnCreateBackup, "← 点击此处备份本机设置 (1/3)", HintPlacement.Right);
        }

        private void ShowTutorialStep2()
        {
            if (flpBackupCards.Controls.Count == 0) return;
            var card = flpBackupCards.Controls[0];
            _tutorial.Show(card, "↑ 右键可以导出该方案 (2/3)", HintPlacement.Below);
        }

        private void ShowTutorialStep3()
        {
            _tutorial.Show(btnPackGo, "将配置和软件全部打包，下次在新电脑打开使用吧 (3/3)", HintPlacement.Above);
            _tutStep3Done = true; // 展示即完成
            SaveTutorialState();
        }

        /// <summary>第二步被关闭后衔接第三步</summary>
        private void TryShowTutorialStep3()
        {
            if (_tutStep2Done && !_tutStep3Done) ShowTutorialStep3();
        }

        /// <summary>气泡被关闭（点击或 5 秒超时）时自动推进到下一个未完成的教程步骤</summary>
        private void AdvanceTutorial()
        {
            if (!_tutStep1Done)
            {
                // 第一步被关闭/超时：标记完成并推进到第二步（无备份卡片则直接跳到第三步）
                CompleteTutorialStep1();
                if (flpBackupCards.Controls.Count > 0) { ShowTutorialStep2(); CompleteTutorialStep2(); }
                else ShowTutorialStep3();
            }
            else
            {
                TryShowTutorialStep3();
            }
        }

        private void CompleteTutorialStep1()
        {
            if (_tutStep1Done) return;
            _tutStep1Done = true;
            _tutorial.Hide();
            SaveTutorialState();
        }

        private void CompleteTutorialStep2()
        {
            if (_tutStep2Done) return;
            _tutStep2Done = true;
            SaveTutorialState();
        }

        private void LoadTutorialState()
        {
            try
            {
                if (!File.Exists(TutorialStatePath)) return;
                foreach (var line in File.ReadAllLines(TutorialStatePath))
                {
                    if (line.StartsWith("step1=")) _tutStep1Done = line.Contains("1");
                    else if (line.StartsWith("step2=")) _tutStep2Done = line.Contains("1");
                    else if (line.StartsWith("step3=")) _tutStep3Done = line.Contains("1");
                }
            }
            catch { }
        }

        private void SaveTutorialState()
        {
            try
            {
                File.WriteAllLines(TutorialStatePath, new[]
                {
                    "step1=" + (_tutStep1Done ? "1" : "0"),
                    "step2=" + (_tutStep2Done ? "1" : "0"),
                    "step3=" + (_tutStep3Done ? "1" : "0")
                });
            }
            catch { }
        }

        // ══════════════════════════════════════════════
        // 游戏模式切换
        // ══════════════════════════════════════════════
        private void BtnMoreFeatures_Click(object sender, EventArgs e)
        {
            var menu = new ContextMenuStrip();
            var apexItem = new ToolStripMenuItem("Apex英雄");
            apexItem.Checked = _gameMode == GameMode.Apex;
            apexItem.Click += (s, ev) => SwitchToApexMode();
            menu.Items.Add(apexItem);

            var narakaItem = new ToolStripMenuItem("永劫无间");
            narakaItem.Checked = _gameMode == GameMode.Naraka;
            narakaItem.Click += (s, ev) => SwitchToNarakaMode();
            menu.Items.Add(narakaItem);

            menu.Show(btnMoreFeatures, new Point(0, -menu.Height));
        }

        private void BtnCrosshair_Click(object sender, EventArgs e)
        {
            if (_crosshairForm == null || _crosshairForm.IsDisposed)
            {
                _crosshairForm = new CrosshairForm();
                DockCrosshairPanel();
                _crosshairForm.Show();
            }
            else if (_crosshairForm.Visible)
            {
                _crosshairForm.Hide(); // 收起面板（屏幕准心保持显示）
            }
            else
            {
                DockCrosshairPanel();
                _crosshairForm.Show();
            }
        }

        /// <summary>把准心面板停靠在主窗口右侧；右侧放不下时改停左侧</summary>
        private void DockCrosshairPanel()
        {
            if (_crosshairForm == null || _crosshairForm.IsDisposed) return;
            var screen = Screen.FromControl(this).WorkingArea;
            int x = this.Right;
            if (x + _crosshairForm.Width > screen.Right)
                x = this.Left - _crosshairForm.Width;
            _crosshairForm.Location = new Point(x, this.Top);
        }

        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            if (_crosshairForm != null && !_crosshairForm.IsDisposed && _crosshairForm.Visible)
                DockCrosshairPanel();
        }

        private void BtnPackGo_Click(object sender, EventArgs e)
        {
            var ans = MessageBox.Show(this,
                "是否将本程序和你的存档一并打包，发送到你的U盘或手机上，下次在新电脑上美美使用！",
                "打包带走", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (ans != DialogResult.Yes) return;

            using (var sfd = new SaveFileDialog())
            {
                sfd.Title = "选择打包保存位置";
                sfd.Filter = "ZIP 压缩包 (*.zip)|*.zip";
                sfd.FileName = "ApexSyncTool_打包_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".zip";
                if (sfd.ShowDialog(this) != DialogResult.OK || string.IsNullOrEmpty(sfd.FileName)) return;

                string sourceDir = AppDomain.CurrentDomain.BaseDirectory;
                using (var progress = new PackProgressForm(sourceDir, sfd.FileName))
                {
                    progress.ShowDialog(this);
                    if (progress.PackSucceeded)
                        ShowToast(string.IsNullOrEmpty(progress.ErrorMessage)
                            ? "打包完成，快拷贝到新电脑吧！"
                            : progress.ErrorMessage);
                    else if (!string.IsNullOrEmpty(progress.ErrorMessage))
                        ShowToast("打包失败: " + progress.ErrorMessage, true);
                }
            }
        }

        private void SwitchToApexMode()
        {
            _gameMode = GameMode.Apex;
            _selectedBackupPath = null;
            this.Text = "Apex - 游戏设置管理器";
            btnLaunchApex.Text = "启动APEX";
            SetSteamSectionVisible(true);
            btnNarakaSwitch.Visible = false;
            lblNarakaSwitchStatus.Visible = false;
            _backupManager.SetBackupPath(GetBackupRoot());
            RefreshGameStatus();
            LoadBackupCards();
            LoadSteamAccounts();
            ShowToast("已切换到 Apex 模式");
        }

        private void SwitchToNarakaMode()
        {
            string programDir = SelectNarakaPath();
            if (string.IsNullOrEmpty(programDir)) return;

            // programDir 为 NarakaBladepoint.exe 所在文件夹（根目录\program），由此推导根目录
            _narakaPath = Directory.GetParent(programDir)?.FullName ?? programDir;
            _narakaConfigPath = Path.Combine(programDir, "NarakaBladepoint_Data", "QualitySettingsData.txt");

            if (!File.Exists(_narakaConfigPath))
            {
                ShowToast("未找到配置文件 QualitySettingsData.txt", true);
                return;
            }

            _gameMode = GameMode.Naraka;
            _selectedBackupPath = null;
            this.Text = "永劫无间 - 游戏设置管理器";
            btnLaunchApex.Text = "永劫启动";
            SetSteamSectionVisible(false);
            btnNarakaSwitch.Visible = true;
            lblNarakaSwitchStatus.Visible = true;
            RefreshNarakaStatus();
            LoadBackupCards();
            UpdateNarakaSwitchStatus();
            ShowToast("已切换到永劫无间模式");
        }

        private string SelectNarakaPath()
        {
            string steamNaraka = AutoDetectNarakaSteam();

            var form = new Form
            {
                Text = "选择永劫无间路径", Width = 420, Height = 200,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false,
                Font = new Font("Microsoft YaHei UI", 9F)
            };

            var lblInfo = new Label { Text = "请选择 NarakaBladepoint.exe 所在文件夹（游戏根目录\\program\\ 下）", Left = 12, Top = 12, Width = 380 };
            var txtPath = new TextBox { Left = 12, Top = 42, Width = 300 };
            var btnBrowse = new Button { Text = "浏览...", Left = 318, Top = 41, Width = 70 };

            btnBrowse.Click += (s, e) =>
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Title = "选择 NarakaBladepoint.exe";
                    ofd.Filter = "NarakaBladepoint.exe|NarakaBladepoint.exe|所有文件|*.*";
                    if (ofd.ShowDialog() == DialogResult.OK)
                        txtPath.Text = Path.GetDirectoryName(ofd.FileName);
                }
            };

            var btnOk = new Button { Text = "确定", Left = 230, Top = 80, Width = 80, DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "取消", Left = 316, Top = 80, Width = 70, DialogResult = DialogResult.Cancel };
            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;

            if (!string.IsNullOrEmpty(steamNaraka))
            {
                var btnAuto = new Button
                {
                    Text = "自动检测: " + steamNaraka,
                    Left = 12, Top = 80, Width = 210, Height = 26,
                    Font = new Font("Microsoft YaHei UI", 8F)
                };
                btnAuto.Click += (s, e) => { txtPath.Text = steamNaraka; };
                form.Controls.Add(btnAuto);
            }

            form.Controls.AddRange(new Control[] { lblInfo, txtPath, btnBrowse, btnOk, btnCancel });

            if (form.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(txtPath.Text))
            {
                string path = txtPath.Text.Trim();
                if (Directory.Exists(path)) return path;
            }
            return null;
        }

        private string AutoDetectNarakaSteam()
        {
            try
            {
                string steamPath = @"C:\Program Files (x86)\Steam";
                try
                {
                    using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam"))
                    {
                        if (key != null)
                        {
                            string p = key.GetValue("InstallPath") as string;
                            if (!string.IsNullOrEmpty(p) && Directory.Exists(p)) steamPath = p;
                        }
                    }
                }
                catch { }

                string[] candidates = new[]
                {
                    Path.Combine(steamPath, "steamapps", "common", "Naraka", "program"),
                    @"E:\Games\Naraka\program", @"D:\Games\Naraka\program", @"C:\Games\Naraka\program"
                };

                foreach (var c in candidates)
                {
                    if (File.Exists(Path.Combine(c, "NarakaBladepoint.exe"))) return c;
                }
            }
            catch { }
            return null;
        }

        private void SetSteamSectionVisible(bool visible)
        {
            lblSteamTitle.Visible = visible;
            lblSteamAccount.Visible = visible;
            cmbSteamAccount.Visible = visible;
            lblAccountName.Visible = visible;
            lblSteamPath.Visible = visible;
            txtSteamParams.Visible = visible;
            btnCopySteamParams.Visible = visible;
            flpSteamTags.Visible = visible;
            btnApplySteam.Visible = visible;
        }

        // ══════════════════════════════════════════════
        // 游戏状态
        // ══════════════════════════════════════════════
        private void RefreshGameStatus()
        {
            if (_gameMode == GameMode.Naraka) { RefreshNarakaStatus(); return; }
            try
            {
                var status = _pathManager.GetGameStatus();
                lblStatus.Text = status.Status;
                lblPath.Text = status.IsInstalled ? status.LocalPath : "未检测到";
                btnApplyToGame.Enabled = status.IsInstalled && !string.IsNullOrEmpty(_selectedBackupPath);
                btnOpenFolder.Enabled = status.IsInstalled;
                btnBrowsePath.Enabled = status.IsInstalled;
            }
            catch (Exception ex) { _logger.LogError("刷新状态失败", ex); lblStatus.Text = "状态检测失败"; }
        }

        private void RefreshNarakaStatus()
        {
            try
            {
                if (!string.IsNullOrEmpty(_narakaPath) && Directory.Exists(_narakaPath))
                {
                    lblStatus.Text = "已安装 (永劫无间)";
                    lblPath.Text = _narakaConfigPath;
                    btnApplyToGame.Enabled = !string.IsNullOrEmpty(_selectedBackupPath);
                    btnOpenFolder.Enabled = true;
                    btnBrowsePath.Enabled = false;
                }
                else
                {
                    lblStatus.Text = "未检测到永劫无间";
                    lblPath.Text = "未检测到";
                    btnApplyToGame.Enabled = false;
                    btnOpenFolder.Enabled = false;
                    btnBrowsePath.Enabled = false;
                }
            }
            catch (Exception ex) { _logger.LogError("刷新永劫状态失败", ex); }
        }

        // ══════════════════════════════════════════════
        // 备份管理
        // ══════════════════════════════════════════════
        private string GetBackupRoot()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string gameFolder = _gameMode == GameMode.Apex ? "apex" : "naraka";
            return Path.Combine(baseDir, "backups", gameFolder);
        }

        /// <summary>
        /// 将旧版平铺在 backups/ 下的 Apex 备份迁移到 backups/apex/
        /// </summary>
        private void MigrateLegacyApexBackups()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string flatRoot = Path.Combine(baseDir, "backups");
                string apexRoot = Path.Combine(flatRoot, "apex");
                if (!Directory.Exists(flatRoot)) return;
                Directory.CreateDirectory(apexRoot);

                foreach (var dir in Directory.GetDirectories(flatRoot))
                {
                    string name = Path.GetFileName(dir);
                    if (string.Equals(name, "apex", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "naraka", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // 仅迁移看起来像 Apex 备份的目录（含 local 或 profile 子目录）
                    if (!Directory.Exists(Path.Combine(dir, "local")) &&
                        !Directory.Exists(Path.Combine(dir, "profile")))
                        continue;

                    string dest = Path.Combine(apexRoot, name);
                    int c = 1;
                    while (Directory.Exists(dest)) dest = Path.Combine(apexRoot, name + "_" + c++);
                    Directory.Move(dir, dest);
                }
            }
            catch (Exception ex) { _logger?.LogError("迁移旧备份失败", ex); }
        }

        private void LoadBackupCards()
        {
            try
            {
                _tutorial?.Hide();
                flpBackupCards.Controls.Clear();
                _backupCards.Clear();

                string backupRoot = GetBackupRoot();
                Directory.CreateDirectory(backupRoot);

                string[] backups;
                if (_gameMode == GameMode.Apex)
                {
                    backups = _backupManager.GetAvailableBackups();
                }
                else
                {
                    backups = Directory.GetDirectories(backupRoot);
                    Array.Sort(backups, (a, b) => Directory.GetLastWriteTime(b).CompareTo(Directory.GetLastWriteTime(a)));
                }

                foreach (var backup in backups)
                {
                    string backupName = Path.GetFileName(backup);
                    var card = new BackupCard();

                    if (_gameMode == GameMode.Apex)
                    {
                        string localPath = Path.Combine(backup, "local");
                        string profilePath = Path.Combine(backup, "profile");
                        var preview = _configParser.ExtractPreview(localPath, profilePath);
                        card.UpdateContent(backupName, preview, backup);
                    }
                    else
                    {
                        // 永劫无间模式：卡面不显示预览三行
                        card.UpdateContent(backupName, null, backup, isNaraka: true);
                    }

                    card.Size = new Size(220, 110);
                    card.Margin = new Padding(5);
                    card.SelectionChanged += Card_SelectionChanged;
                    card.RenameRequested += Card_RenameRequested;
                    card.DeleteRequested += Card_DeleteRequested;
                    card.DuplicateRequested += Card_DuplicateRequested;
                    card.OpenFolderRequested += Card_OpenFolderRequested;
                    card.AdvancedPreviewRequested += Card_AdvancedPreviewRequested;
                    card.KeybindEditRequested += Card_KeybindEditRequested;
                    card.ExportRequested += Card_ExportRequested;
                    card.MouseClick += (s2, e2) => { if (e2.Button == MouseButtons.Right) { _tutorial?.Hide(); TryShowTutorialStep3(); } };

                    flpBackupCards.Controls.Add(card);
                    _backupCards[backup] = card;
                }

                btnApplyToGame.Enabled = backups.Length > 0 && !string.IsNullOrEmpty(_selectedBackupPath);
            }
            catch (Exception ex) { _logger.LogError("加载备份卡片失败", ex); }
        }

        private void BtnCreateBackup_Click(object sender, EventArgs e)
        {
            CompleteTutorialStep1();
            try
            {
                if (_gameMode == GameMode.Apex)
                {
                    var result = _backupManager.CreateBackup();
                    if (result.Success)
                    {
                        LoadBackupCards();
                        ShowToast("备份成功");
                        if (!_tutStep2Done) { ShowTutorialStep2(); CompleteTutorialStep2(); }
                    }
                    else ShowToast("备份失败: " + result.Message, true);
                }
                else
                {
                    if (string.IsNullOrEmpty(_narakaConfigPath) || !File.Exists(_narakaConfigPath))
                    { ShowToast("配置文件不存在", true); return; }

                    string backupRoot = GetBackupRoot();
                    Directory.CreateDirectory(backupRoot);
                    string backupName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    string backupDir = Path.Combine(backupRoot, backupName);
                    Directory.CreateDirectory(backupDir);
                    File.Copy(_narakaConfigPath, Path.Combine(backupDir, "QualitySettingsData.txt"));
                    LoadBackupCards();
                    ShowToast("备份成功");
                    if (!_tutStep2Done) { ShowTutorialStep2(); CompleteTutorialStep2(); }
                }
            }
            catch (Exception ex) { ShowToast("备份异常: " + ex.Message, true); }
        }

        private void BtnApplyToGame_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_selectedBackupPath)) { ShowToast("请先选择备份", true); return; }

                var confirm = MessageBox.Show("确定要应用此备份到游戏吗?\n\n当前配置将被覆盖。",
                    "确认操作", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes) return;

                if (_gameMode == GameMode.Apex)
                {
                    var result = _backupManager.RestoreBackup(_selectedBackupPath, autoBackupCurrent: true);
                    if (result.Success) ShowToast("配置已成功应用到游戏");
                    else if (result.Message.Contains("权限"))
                    {
                        if (MessageBox.Show("需要管理员权限，是否重启?", "权限不足",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                            RestartAsAdmin();
                    }
                    else ShowToast("应用失败: " + result.Message, true);
                }
                else
                {
                    string backupCfg = Path.Combine(_selectedBackupPath, "QualitySettingsData.txt");
                    if (!File.Exists(backupCfg)) { ShowToast("备份文件不完整", true); return; }
                    File.Copy(backupCfg, _narakaConfigPath, true);
                    ShowToast("配置已成功应用到游戏");
                    UpdateNarakaSwitchStatus();
                }
            }
            catch (Exception ex) { ShowToast("应用异常: " + ex.Message, true); }
        }

        // ══════════════════════════════════════════════
        // 备份卡片事件
        // ══════════════════════════════════════════════
        private void Card_SelectionChanged(object sender, BackupCardEventArgs e)
        {
            _selectedBackupPath = e.BackupPath;
            foreach (var card in _backupCards.Values)
                card.SetSelected(card.BackupPath == e.BackupPath);
            btnApplyToGame.Enabled = true;
        }

        private void Card_RenameRequested(object sender, BackupCardEventArgs e)
        {
            try
            {
                string currentName = Path.GetFileName(e.BackupPath);
                var form = new Form { Text = "重命名备份", Width = 300, Height = 150, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
                var textBox = new TextBox { Text = currentName, Left = 90, Top = 20, Width = 190 };
                var okButton = new Button { Text = "确定", Left = 130, Top = 60, Width = 70, DialogResult = DialogResult.OK };
                var cancelButton = new Button { Text = "取消", Left = 210, Top = 60, Width = 70, DialogResult = DialogResult.Cancel };
                form.Controls.AddRange(new Control[] { new Label { Text = "新名称:", Left = 10, Top = 20, Width = 70 }, textBox, okButton, cancelButton });
                form.AcceptButton = okButton; form.CancelButton = cancelButton;

                if (form.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(textBox.Text))
                {
                    string newPath = Path.Combine(Path.GetDirectoryName(e.BackupPath), textBox.Text);
                    if (Directory.Exists(newPath)) { ShowToast("名称已存在", true); return; }
                    Directory.Move(e.BackupPath, newPath);
                    LoadBackupCards(); ShowToast("重命名成功");
                }
            }
            catch (Exception ex) { ShowToast("重命名失败: " + ex.Message, true); }
        }

        private void Card_DeleteRequested(object sender, BackupCardEventArgs e)
        {
            try
            {
                string name = Path.GetFileName(e.BackupPath);
                if (MessageBox.Show("确定要删除备份 \"" + name + "\" 吗？删除后无法恢复。",
                    "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                Directory.Delete(e.BackupPath, true);
                if (_selectedBackupPath == e.BackupPath) _selectedBackupPath = null;
                LoadBackupCards(); ShowToast("备份已删除");
            }
            catch (Exception ex) { ShowToast("删除失败: " + ex.Message, true); }
        }

        private void Card_DuplicateRequested(object sender, BackupCardEventArgs e)
        {
            try
            {
                string name = Path.GetFileName(e.BackupPath);
                string dir = Path.GetDirectoryName(e.BackupPath);
                string dest = Path.Combine(dir, name + "_副本");
                int c = 1;
                while (Directory.Exists(dest)) dest = Path.Combine(dir, name + "_副本" + c++);
                CopyDirectory(e.BackupPath, dest);
                LoadBackupCards(); ShowToast("备份已复制");
            }
            catch (Exception ex) { ShowToast("复制失败: " + ex.Message, true); }
        }

        private void Card_OpenFolderRequested(object sender, BackupCardEventArgs e)
        {
            try { System.Diagnostics.Process.Start("explorer.exe", e.BackupPath); } catch { }
        }

        private void Card_AdvancedPreviewRequested(object sender, BackupCardEventArgs e)
        {
            try
            {
                string backupPath = e.BackupPath;

                if (_gameMode == GameMode.Naraka)
                {
                    string cfgFile = Path.Combine(backupPath, "QualitySettingsData.txt");
                    if (!File.Exists(cfgFile)) { ShowToast("配置文件不存在", true); return; }
                    var form = new Form { Text = "配置预览 - " + Path.GetFileName(backupPath), Width = 500, Height = 400, StartPosition = FormStartPosition.CenterParent, Font = new Font("Microsoft YaHei UI", 9F) };
                    form.Controls.Add(new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, WordWrap = true, Text = File.ReadAllText(cfgFile) });
                    form.ShowDialog(this);
                    return;
                }

                string localPath = Path.Combine(backupPath, "local");
                string profilePath = Path.Combine(backupPath, "profile");
                var preview = _configParser.ExtractPreview(localPath, profilePath);

                var previewForm = new Form { Text = "高级预览 - " + Path.GetFileName(backupPath), Width = 460, Height = 620, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.Sizable, MaximizeBox = false, MinimizeBox = false, Font = new Font("Microsoft YaHei UI", 9F) };

                var panelRename = new Panel { Dock = DockStyle.Top, Height = 38 };
                var txtRename = new TextBox { Text = Path.GetFileName(backupPath), Left = 4, Top = 6, Width = 320 };
                var btnRename = new Button { Text = "重命名", Left = 330, Top = 5, Width = 70, Height = 25 };
                btnRename.Click += (s, ev) =>
                {
                    string newName = txtRename.Text.Trim();
                    if (string.IsNullOrEmpty(newName) || newName == Path.GetFileName(backupPath)) return;
                    string newPath = Path.Combine(Path.GetDirectoryName(backupPath), newName);
                    if (Directory.Exists(newPath)) { MessageBox.Show("名称已存在"); return; }
                    try { Directory.Move(backupPath, newPath); backupPath = newPath; previewForm.Text = "高级预览 - " + newName; LoadBackupCards(); ShowToast("重命名成功"); }
                    catch (Exception ex) { ShowToast("重命名失败: " + ex.Message, true); }
                };
                panelRename.Controls.AddRange(new Control[] { txtRename, btnRename });

                var listView = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true, HeaderStyle = ColumnHeaderStyle.Nonclickable };
                listView.Columns.Add("配置项", 180);
                listView.Columns.Add("值", 240);

                var grpBasic = new ListViewGroup("基础");
                var grpGraphics = new ListViewGroup("画面/图形");
                var grpMouse = new ListViewGroup("鼠标/瞄准");
                var grpAudio = new ListViewGroup("音频/字幕");
                var grpGameplay = new ListViewGroup("游戏功能");
                listView.Groups.AddRange(new[] { grpBasic, grpGraphics, grpMouse, grpAudio, grpGameplay });

                void AddItem(ListViewGroup grp, string name, string value)
                { var item = new ListViewItem(name, grp); item.SubItems.Add(value ?? "N/A"); listView.Items.Add(item); }

                AddItem(grpBasic, "分辨率", preview.Resolution);
                AddItem(grpBasic, "全屏模式", preview.Fullscreen);
                AddItem(grpBasic, "鼠标灵敏度", preview.MouseSensitivity);
                AddItem(grpBasic, "FOV", preview.FOV);
                AddItem(grpBasic, "垂直同步", preview.VSync);
                AddItem(grpBasic, "抗锯齿", preview.AntiAlias);
                AddItem(grpBasic, "配音语言", preview.MilesLanguage);
                AddItem(grpGraphics, "阴影", preview.Shadow);
                AddItem(grpGraphics, "体积光", preview.VolumetricLighting);
                AddItem(grpGraphics, "体积雾", preview.VolumetricFog);
                AddItem(grpGraphics, "环境光遮蔽(SSAO)", preview.SSAO);
                AddItem(grpGraphics, "伽马值", preview.Gamma);
                AddItem(grpGraphics, "各向异性过滤", preview.AnisotropicFiltering);
                AddItem(grpGraphics, "纹理流送内存", preview.StreamMemory);
                AddItem(grpGraphics, "地图细节等级", preview.MapDetail);
                AddItem(grpGraphics, "布娃娃数量", preview.RagdollMax);
                AddItem(grpGraphics, "碎片效果", preview.GibAllow);
                AddItem(grpMouse, "独立倍镜灵敏度", preview.PerScopeSensitivity);
                AddItem(grpMouse, "1x~12x开镜灵敏度", preview.ZoomedSensitivity0 + " / " + preview.ZoomedSensitivity7);
                AddItem(grpMouse, "色盲模式", preview.ColorblindMode);
                AddItem(grpMouse, "准星颜色(RGB)", preview.ReticleColor);
                AddItem(grpAudio, "总音量", preview.SoundVolume);
                AddItem(grpAudio, "对话音量", preview.SoundDialogue);
                AddItem(grpAudio, "背景音乐", preview.SoundMusic);
                AddItem(grpAudio, "游戏音效", preview.SoundSFX);
                AddItem(grpAudio, "字幕", preview.CloseCaption);
                AddItem(grpGameplay, "跑步视角晃动", preview.SprintViewShake);
                AddItem(grpGameplay, "伤害指示器", preview.DamageIndicator);
                AddItem(grpGameplay, "跨平台联机", preview.CrossPlay);

                var btnClose = new Button { Text = "关闭", Dock = DockStyle.Bottom, Height = 36 };
                btnClose.Click += (s, ev) => previewForm.Close();
                previewForm.Controls.Add(listView);
                previewForm.Controls.Add(panelRename);
                previewForm.Controls.Add(btnClose);
                previewForm.ShowDialog(this);
            }
            catch (Exception ex) { ShowToast("预览失败: " + ex.Message, true); }
        }

        private void Card_KeybindEditRequested(object sender, BackupCardEventArgs e)
        {
            if (_gameMode == GameMode.Naraka) { ShowToast("永劫无间暂不支持键位编辑", true); return; }
            try
            {
                string localPath = Path.Combine(e.BackupPath, "local");
                var bindings = _configParser.ExtractKeyBindings(localPath);
                if (bindings.Count == 0) { ShowToast("未找到键位绑定数据", true); return; }

                var form = new Form { Text = "键位编辑 - " + Path.GetFileName(e.BackupPath), Width = 500, Height = 520, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.Sizable, MaximizeBox = false, MinimizeBox = false, Font = new Font("Microsoft YaHei UI", 9F) };
                var dgv = new DataGridView { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false };
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colKey", HeaderText = "按键", ReadOnly = true, FillWeight = 30 });
                dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAction", HeaderText = "绑定动作", FillWeight = 70 });
                foreach (var kvp in bindings) dgv.Rows.Add(kvp.Key, kvp.Value);

                var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 42 };
                var btnSave = new Button { Text = "保存", Width = 80, Height = 30, Left = 10, Top = 6 };
                var btnCancel = new Button { Text = "取消", Width = 80, Height = 30, Left = 100, Top = 6 };
                btnSave.Click += (s, ev) =>
                {
                    var nb = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (DataGridViewRow row in dgv.Rows)
                    { string k = row.Cells["colKey"].Value?.ToString(); string a = row.Cells["colAction"].Value?.ToString() ?? ""; if (!string.IsNullOrEmpty(k)) nb[k] = a; }
                    if (_configParser.SaveKeyBindings(localPath, nb)) { ShowToast("键位已保存"); form.Close(); }
                    else ShowToast("保存失败", true);
                };
                btnCancel.Click += (s, ev) => form.Close();
                panelBottom.Controls.AddRange(new Control[] { btnSave, btnCancel });
                form.Controls.Add(dgv); form.Controls.Add(panelBottom);
                form.ShowDialog(this);
            }
            catch (Exception ex) { ShowToast("键位编辑失败: " + ex.Message, true); }
        }

        // ══════════════════════════════════════════════
        // 导出 / 导入
        // ══════════════════════════════════════════════
        private string GetGameDisplayName() => _gameMode == GameMode.Apex ? "Apex" : "永劫无间";

        private void Card_ExportRequested(object sender, BackupCardEventArgs e)
        {
            try
            {
                if (!Directory.Exists(e.BackupPath)) { ShowToast("备份目录不存在", true); return; }

                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string gameName = GetGameDisplayName();
                string backupName = Path.GetFileName(e.BackupPath);
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string zipPath = Path.Combine(desktop, $"配置-{backupName}-{stamp}.zip");

                using (var fs = new FileStream(zipPath, FileMode.Create))
                using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    // 压缩包结构: 游戏名/备份文件夹名/...
                    AddDirectoryToZip(zip, e.BackupPath, gameName + "/" + backupName);
                }

                ShowToast("已导出到桌面: " + Path.GetFileName(zipPath));
            }
            catch (Exception ex) { ShowToast("导出失败: " + ex.Message, true); }
        }

        private void AddDirectoryToZip(ZipArchive zip, string sourceDir, string entryPrefix)
        {
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                zip.CreateEntryFromFile(file, entryPrefix + "/" + Path.GetFileName(file));
            }
            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                AddDirectoryToZip(zip, dir, entryPrefix + "/" + Path.GetFileName(dir));
            }
        }

        private void FlpBackupCards_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            _tutorial?.Hide();
            TryShowTutorialStep3();
            var menu = new ContextMenuStrip();
            menu.Items.Add(new ToolStripMenuItem("导入到配置列表", null, (s, ev) => ImportFromZip()));
            menu.Show(flpBackupCards, e.Location);
        }

        private void ImportFromZip()
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "选择配置压缩包";
                ofd.Filter = "配置压缩包 (*.zip)|*.zip|所有文件 (*.*)|*.*";
                if (ofd.ShowDialog(this) != DialogResult.OK || string.IsNullOrEmpty(ofd.FileName)) return;

                try
                {
                    using (var zip = ZipFile.OpenRead(ofd.FileName))
                    {
                        if (zip.Entries.Count == 0) { ShowToast("压缩包为空", true); return; }

                        // 第一级目录名 = 游戏名
                        string gameName = zip.Entries[0].FullName.Split('/')[0];
                        string subFolder;
                        bool isApex;
                        if (gameName.Equals("Apex", StringComparison.OrdinalIgnoreCase)) { subFolder = "apex"; isApex = true; }
                        else if (gameName.Equals("永劫无间", StringComparison.OrdinalIgnoreCase)) { subFolder = "naraka"; isApex = false; }
                        else { ShowToast("无法识别压缩包内的游戏: " + gameName, true); return; }

                        string targetRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backups", subFolder);
                        Directory.CreateDirectory(targetRoot);

                        string prefix = gameName + "/";
                        // 收集第二级备份文件夹名
                        var backupNames = zip.Entries
                            .Where(en => en.FullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                            .Select(en => en.FullName.Substring(prefix.Length).Split('/')[0])
                            .Where(n => !string.IsNullOrEmpty(n))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        int imported = 0;
                        string destRootFull = Path.GetFullPath(targetRoot) + Path.DirectorySeparatorChar;

                        foreach (var bn in backupNames)
                        {
                            string destName = bn;
                            string destDir = Path.Combine(targetRoot, destName);
                            int c = 1;
                            while (Directory.Exists(destDir)) { destName = bn + "_导入" + c++; destDir = Path.Combine(targetRoot, destName); }
                            Directory.CreateDirectory(destDir);

                            string bnPrefix = prefix + bn + "/";
                            foreach (var entry in zip.Entries)
                            {
                                if (!entry.FullName.StartsWith(bnPrefix, StringComparison.OrdinalIgnoreCase)) continue;
                                string rel = entry.FullName.Substring(bnPrefix.Length);
                                if (string.IsNullOrEmpty(rel)) continue;

                                string destFile = Path.GetFullPath(Path.Combine(destDir, rel));
                                // Zip Slip 防护：确保目标在备份目录内
                                if (!destFile.StartsWith(destRootFull, StringComparison.OrdinalIgnoreCase) &&
                                    !destFile.StartsWith(Path.GetFullPath(destDir) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                                    continue;

                                if (entry.FullName.EndsWith("/")) { Directory.CreateDirectory(destFile); continue; }
                                Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                                entry.ExtractToFile(destFile, true);
                            }
                            imported++;
                        }

                        if (imported == 0) { ShowToast("未找到可导入的备份", true); return; }

                        // 导入的游戏与当前模式一致时刷新列表
                        if ((isApex && _gameMode == GameMode.Apex) || (!isApex && _gameMode == GameMode.Naraka))
                            LoadBackupCards();

                        ShowToast($"已导入 {imported} 个备份到 {(isApex ? "Apex" : "永劫无间")}");
                    }
                }
                catch (Exception ex) { ShowToast("导入失败: " + ex.Message, true); }
            }
        }

        private void CopyDirectory(string source, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (string file in Directory.GetFiles(source)) File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
            foreach (string dir in Directory.GetDirectories(source)) CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)));
        }

        // ══════════════════════════════════════════════
        // 顶部按钮
        // ══════════════════════════════════════════════
        private void BtnOpenFolder_Click(object sender, EventArgs e)
        {
            try
            {
                if (_gameMode == GameMode.Apex)
                { if (_pathManager.IsApexInstalled) System.Diagnostics.Process.Start("explorer.exe", _pathManager.ApexLocalPath); }
                else
                { if (!string.IsNullOrEmpty(_narakaConfigPath)) System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + _narakaConfigPath + "\""); }
            }
            catch (Exception ex) { ShowToast("打开失败: " + ex.Message, true); }
        }

        private void BtnBrowsePath_Click(object sender, EventArgs e)
        {
            if (_gameMode == GameMode.Naraka) return;
            using (var dialog = new FolderBrowserDialog())
            { dialog.Description = "选择Apex配置文件夹"; if (dialog.ShowDialog() == DialogResult.OK) ShowToast("路径浏览功能开发中"); }
        }

        private void BtnRefreshStatus_Click(object sender, EventArgs e)
        {
            RefreshGameStatus(); LoadBackupCards();
            if (_gameMode == GameMode.Naraka) UpdateNarakaSwitchStatus();
            ShowToast("状态已刷新");
        }

        // ══════════════════════════════════════════════
        // Steam 启动参数
        // ══════════════════════════════════════════════
        private void LoadSteamAccounts()
        {
            try
            {
                cmbSteamAccount.Items.Clear();
                var accounts = _steamAccountManager.GetAllAccounts();
                cmbSteamAccount.Items.Add("全部账户");
                foreach (var a in accounts) cmbSteamAccount.Items.Add(a);
                if (accounts.Count > 0) cmbSteamAccount.SelectedIndex = 1;
                else if (cmbSteamAccount.Items.Count > 0) cmbSteamAccount.SelectedIndex = 0;
                RefreshSteamParamsDisplay();
            }
            catch (Exception ex) { _logger.LogError("加载Steam账户失败", ex); }
        }

        private void CmbSteamAccount_SelectedIndexChanged(object sender, EventArgs e) => RefreshSteamParamsDisplay();

        private void RefreshSteamParamsDisplay()
        {
            try
            {
                if (cmbSteamAccount.SelectedIndex < 0) return;
                if (cmbSteamAccount.SelectedItem is SteamAccount account)
                {
                    txtSteamParams.Text = account.CurrentLaunchOptions ?? string.Empty;
                    lblAccountName.Text = "更新: " + account.LastModified.ToString("yyyy-MM-dd HH:mm");
                    lblSteamPath.Text = "配置文件: " + (account.ConfigPath ?? "未知");
                }
                else { txtSteamParams.Text = string.Empty; lblAccountName.Text = ""; lblSteamPath.Text = ""; }
                UpdateSteamParametersFromTags();
            }
            catch { }
        }

        private void InitializeSteamTags()
        {
            try
            {
                flpSteamTags.Controls.Clear();
                foreach (var preset in LaunchParameterFormatter.GetPresets())
                {
                    var cb = new CheckBox { Text = preset.Key, Tag = preset.Value, AutoSize = true, Margin = new Padding(5, 0, 0, 0) };
                    cb.CheckedChanged += (s, e) =>
                    {
                        if (_updatingSteamTags) return;
                        string pg = (s as CheckBox)?.Tag as string;
                        if (pg != null) { txtSteamParams.Text = LaunchParameterFormatter.Format(LaunchParameterFormatter.ToggleParameter(txtSteamParams.Text, pg)); UpdateSteamParametersFromTags(); }
                    };
                    flpSteamTags.Controls.Add(cb);
                }
            }
            catch { }
        }

        private void UpdateSteamParametersFromTags()
        {
            _updatingSteamTags = true;
            try { foreach (CheckBox cb in flpSteamTags.Controls) { string pg = cb.Tag as string; if (pg != null) cb.Checked = LaunchParameterFormatter.IsParameterActive(txtSteamParams.Text, pg); } }
            finally { _updatingSteamTags = false; }
        }

        private void BtnCopySteamParams_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSteamParams.Text)) { ShowToast("启动参数为空", true); return; }
            try { Clipboard.SetText(txtSteamParams.Text); ShowToast("已复制到剪贴板"); }
            catch (Exception ex) { ShowToast("复制失败: " + ex.Message, true); }
        }

        private void BtnApplySteam_Click(object sender, EventArgs e)
        {
            try
            {
                string parameters = LaunchParameterFormatter.Format(txtSteamParams.Text);
                if (!LaunchParameterFormatter.Validate(parameters)) { ShowToast("启动参数格式不正确", true); return; }
                txtSteamParams.Text = parameters; UpdateSteamParametersFromTags();
                if (_steamAccountManager?.GetAllAccounts().Count == 0) { ShowToast("未检测到Steam账户", true); return; }

                bool success = false;
                if (cmbSteamAccount.SelectedItem?.ToString() == "全部账户")
                    success = _steamAccountManager.SetLaunchOptionsForAll(parameters);
                else if (cmbSteamAccount.SelectedItem is SteamAccount acc)
                    success = _steamAccountManager.SetLaunchOptions(acc.SteamId, parameters);

                if (success) { ShowToast("启动参数已成功应用"); LoadSteamAccounts(); }
                else ShowToast("应用失败，请检查权限或关闭Steam后重试", true);
            }
            catch (Exception ex) { ShowToast("应用异常: " + ex.Message, true); }
        }

        // ══════════════════════════════════════════════
        // 永劫无间小开关
        // ══════════════════════════════════════════════
        private void UpdateNarakaSwitchStatus()
        {
            try
            {
                if (string.IsNullOrEmpty(_narakaConfigPath) || !File.Exists(_narakaConfigPath))
                { lblNarakaSwitchStatus.Text = "配置文件未找到"; return; }

                string json = File.ReadAllText(_narakaConfigPath);
                using var doc = JsonDocument.Parse(json);
                var sys = doc.RootElement.GetProperty("l22SystemQualitySetting");
                if (sys.TryGetProperty("characterAdditionalPhysics1", out var val))
                {
                    bool cur = val.GetBoolean();
                    lblNarakaSwitchStatus.Text = "characterAdditionalPhysics1: " + (cur ? "开启" : "关闭");
                    lblNarakaSwitchStatus.ForeColor = cur ? Color.FromArgb(0, 120, 60) : Color.FromArgb(120, 120, 120);
                }
                else { lblNarakaSwitchStatus.Text = "characterAdditionalPhysics1: 未设置"; lblNarakaSwitchStatus.ForeColor = Color.FromArgb(120, 120, 120); }
            }
            catch (Exception ex) { lblNarakaSwitchStatus.Text = "读取失败"; }
        }

        private void BtnNarakaSwitch_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_narakaConfigPath) || !File.Exists(_narakaConfigPath))
                { ShowToast("配置文件不存在", true); return; }

                var attrs = File.GetAttributes(_narakaConfigPath);
                if ((attrs & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    if (MessageBox.Show("配置文件为只读属性，是否自动取消只读以进行修改？",
                        "文件只读", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
                    try { File.SetAttributes(_narakaConfigPath, attrs & ~FileAttributes.ReadOnly); }
                    catch
                    {
                        ShowToast("无法自动取消只读，已打开文件夹请手动处理", true);
                        System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + _narakaConfigPath + "\"");
                        return;
                    }
                }

                string json = File.ReadAllText(_narakaConfigPath);
                bool newValue;

                if (json.Contains("\"characterAdditionalPhysics1\":true"))
                { json = json.Replace("\"characterAdditionalPhysics1\":true", "\"characterAdditionalPhysics1\":false"); newValue = false; }
                else if (json.Contains("\"characterAdditionalPhysics1\":false"))
                { json = json.Replace("\"characterAdditionalPhysics1\":false", "\"characterAdditionalPhysics1\":true"); newValue = true; }
                else
                {
                    string insertBefore = "\"xboxQualityOption\"";
                    if (json.Contains(insertBefore))
                        json = json.Replace(insertBefore, "\"characterAdditionalPhysics1\":true," + insertBefore);
                    else
                    {
                        int idx = json.LastIndexOf("}}");
                        if (idx > 0) json = json.Insert(idx, ",\"characterAdditionalPhysics1\":true");
                    }
                    newValue = true;
                }

                File.WriteAllText(_narakaConfigPath, json);

                // 修改完成后将文件恢复为只读属性
                bool readOnlySet = false;
                try { File.SetAttributes(_narakaConfigPath, File.GetAttributes(_narakaConfigPath) | FileAttributes.ReadOnly); readOnlySet = true; }
                catch { }

                UpdateNarakaSwitchStatus();
                ShowToast(readOnlySet ? "已将文件保存为只读模式" : "characterAdditionalPhysics1 已" + (newValue ? "开启" : "关闭"));
            }
            catch (Exception ex) { ShowToast("操作失败: " + ex.Message, true); }
        }

        // ══════════════════════════════════════════════
        // 启动按钮
        // ══════════════════════════════════════════════
        private void BtnLaunchApex_Click(object sender, EventArgs e)
        {
            try
            {
                if (_gameMode == GameMode.Apex)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "steam://rungameid/1172470", UseShellExecute = true });
                    ShowToast("已发起启动请求");
                }
                else
                {
                    var form = new Form { Text = "启动永劫无间", Width = 280, Height = 150, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, Font = new Font("Microsoft YaHei UI", 9F) };
                    var btnDir = new Button { Text = "目录启动", Left = 12, Top = 42, Width = 120, Height = 36 };
                    var btnSteam = new Button { Text = "Steam启动", Left = 140, Top = 42, Width = 120, Height = 36 };
                    form.Controls.AddRange(new Control[] { new Label { Text = "选择启动方式:", Left = 12, Top = 12, AutoSize = true }, btnDir, btnSteam });

                    btnDir.Click += (s, ev) =>
                    {
                        string launcher = Path.Combine(_narakaPath, "LauncherGame.exe");
                        if (File.Exists(launcher))
                        { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = launcher, WorkingDirectory = _narakaPath, UseShellExecute = true }); ShowToast("已通过目录启动"); }
                        else ShowToast("LauncherGame.exe 不存在", true);
                        form.Close();
                    };
                    btnSteam.Click += (s, ev) =>
                    { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "steam://rungameid/1203220", UseShellExecute = true }); ShowToast("已通过Steam发起启动请求"); form.Close(); };

                    form.ShowDialog(this);
                }

                btnLaunchApex.Enabled = false;
                btnLaunchApex.Text = "正在请求...";
                _launchCooldownTimer.Stop();
                _launchCooldownTimer.Start();
            }
            catch (Exception ex) { ShowToast("启动失败: " + ex.Message, true); }
        }

        // ══════════════════════════════════════════════
        // 其他
        // ══════════════════════════════════════════════
        private void RestartAsAdmin()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = Environment.ProcessPath, UseShellExecute = true, Verb = "runas" });
                this.Close();
            }
            catch { ShowToast("无法以管理员权限重启", true); }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try { _logger?.Log("应用关闭"); _crosshairForm?.CloseAll(); _toastTimer?.Stop(); _toastTimer?.Dispose(); _launchCooldownTimer?.Stop(); _launchCooldownTimer?.Dispose(); } catch { }
        }
    }
}
