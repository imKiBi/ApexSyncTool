namespace ApexSyncTool
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // Status and Path Section
            this.lblStatusTitle = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblPathTitle = new System.Windows.Forms.Label();
            this.lblPath = new System.Windows.Forms.Label();
            this.btnOpenFolder = new System.Windows.Forms.Button();
            this.btnBrowsePath = new System.Windows.Forms.Button();
            this.btnRefreshStatus = new System.Windows.Forms.Button();

            // Backup Section
            this.lblBackupTitle = new System.Windows.Forms.Label();
            this.flpBackupCards = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCreateBackup = new System.Windows.Forms.Button();
            this.btnApplyToGame = new System.Windows.Forms.Button();

            // Steam Section (Apex mode)
            this.lblSteamTitle = new System.Windows.Forms.Label();
            this.lblSteamAccount = new System.Windows.Forms.Label();
            this.cmbSteamAccount = new System.Windows.Forms.ComboBox();
            this.lblAccountName = new System.Windows.Forms.Label();
            this.lblSteamPath = new System.Windows.Forms.Label();
            this.txtSteamParams = new System.Windows.Forms.TextBox();
            this.btnCopySteamParams = new System.Windows.Forms.Button();
            this.btnApplySteam = new System.Windows.Forms.Button();
            this.flpSteamTags = new System.Windows.Forms.FlowLayoutPanel();

            // Naraka Section
            this.btnNarakaSwitch = new System.Windows.Forms.Button();
            this.lblNarakaSwitchStatus = new System.Windows.Forms.Label();

            // Bottom buttons
            this.btnLaunchApex = new System.Windows.Forms.Button();
            this.btnPackGo = new System.Windows.Forms.Button();
            this.btnMoreFeatures = new System.Windows.Forms.Button();
            this.btnCrosshair = new System.Windows.Forms.Button();

            // Toast bubble
            this.lblToast = new System.Windows.Forms.Label();

            this.SuspendLayout();

            // ═══════════════════════════════════════
            // 游戏状态区域 (顶部)
            // ═══════════════════════════════════════

            this.lblStatusTitle.AutoSize = true;
            this.lblStatusTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblStatusTitle.Location = new System.Drawing.Point(12, 12);
            this.lblStatusTitle.Name = "lblStatusTitle";
            this.lblStatusTitle.Text = "游戏状态";

            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblStatus.Location = new System.Drawing.Point(12, 36);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Text = "检测中...";

            this.lblPathTitle.AutoSize = true;
            this.lblPathTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblPathTitle.Location = new System.Drawing.Point(12, 58);
            this.lblPathTitle.Name = "lblPathTitle";
            this.lblPathTitle.Text = "配置路径";

            this.lblPath.AutoSize = true;
            this.lblPath.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblPath.Location = new System.Drawing.Point(12, 82);
            this.lblPath.Name = "lblPath";
            this.lblPath.Size = new System.Drawing.Size(420, 17);
            this.lblPath.Text = "检测中...";
            this.lblPath.AutoEllipsis = true;

            this.btnOpenFolder.Location = new System.Drawing.Point(480, 12);
            this.btnOpenFolder.Name = "btnOpenFolder";
            this.btnOpenFolder.Size = new System.Drawing.Size(76, 28);
            this.btnOpenFolder.TabIndex = 4;
            this.btnOpenFolder.Text = "打开文件夹";
            this.btnOpenFolder.UseVisualStyleBackColor = true;
            this.btnOpenFolder.Click += new System.EventHandler(this.BtnOpenFolder_Click);

            this.btnBrowsePath.Location = new System.Drawing.Point(562, 12);
            this.btnBrowsePath.Name = "btnBrowsePath";
            this.btnBrowsePath.Size = new System.Drawing.Size(76, 28);
            this.btnBrowsePath.TabIndex = 5;
            this.btnBrowsePath.Text = "浏览修改";
            this.btnBrowsePath.UseVisualStyleBackColor = true;
            this.btnBrowsePath.Click += new System.EventHandler(this.BtnBrowsePath_Click);

            this.btnRefreshStatus.Location = new System.Drawing.Point(644, 12);
            this.btnRefreshStatus.Name = "btnRefreshStatus";
            this.btnRefreshStatus.Size = new System.Drawing.Size(76, 28);
            this.btnRefreshStatus.TabIndex = 6;
            this.btnRefreshStatus.Text = "刷新检测";
            this.btnRefreshStatus.UseVisualStyleBackColor = true;
            this.btnRefreshStatus.Click += new System.EventHandler(this.BtnRefreshStatus_Click);

            // ═══════════════════════════════════════
            // 备份区域 (中部)
            // ═══════════════════════════════════════

            this.lblBackupTitle.AutoSize = true;
            this.lblBackupTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblBackupTitle.Location = new System.Drawing.Point(12, 108);
            this.lblBackupTitle.Name = "lblBackupTitle";
            this.lblBackupTitle.Text = "备份列表";

            this.flpBackupCards.AutoScroll = true;
            this.flpBackupCards.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flpBackupCards.Location = new System.Drawing.Point(12, 132);
            this.flpBackupCards.Name = "flpBackupCards";
            this.flpBackupCards.Size = new System.Drawing.Size(716, 200);
            this.flpBackupCards.TabIndex = 8;
            this.flpBackupCards.WrapContents = true;

            this.btnCreateBackup.Location = new System.Drawing.Point(12, 340);
            this.btnCreateBackup.Name = "btnCreateBackup";
            this.btnCreateBackup.Size = new System.Drawing.Size(100, 32);
            this.btnCreateBackup.TabIndex = 9;
            this.btnCreateBackup.Text = "一键备份";
            this.btnCreateBackup.UseVisualStyleBackColor = true;
            this.btnCreateBackup.Click += new System.EventHandler(this.BtnCreateBackup_Click);

            this.btnApplyToGame.Location = new System.Drawing.Point(120, 340);
            this.btnApplyToGame.Name = "btnApplyToGame";
            this.btnApplyToGame.Size = new System.Drawing.Size(100, 32);
            this.btnApplyToGame.TabIndex = 10;
            this.btnApplyToGame.Text = "应用到游戏";
            this.btnApplyToGame.UseVisualStyleBackColor = true;
            this.btnApplyToGame.Enabled = false;
            this.btnApplyToGame.Click += new System.EventHandler(this.BtnApplyToGame_Click);

            // ═══════════════════════════════════════
            // Steam 启动参数区域 (Apex模式)
            // ═══════════════════════════════════════

            this.lblSteamTitle.AutoSize = true;
            this.lblSteamTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblSteamTitle.Location = new System.Drawing.Point(12, 386);
            this.lblSteamTitle.Name = "lblSteamTitle";
            this.lblSteamTitle.Text = "Steam启动参数";

            this.lblSteamAccount.AutoSize = true;
            this.lblSteamAccount.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblSteamAccount.Location = new System.Drawing.Point(12, 410);
            this.lblSteamAccount.Name = "lblSteamAccount";
            this.lblSteamAccount.Text = "选择账户";

            this.cmbSteamAccount.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSteamAccount.FormattingEnabled = true;
            this.cmbSteamAccount.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.cmbSteamAccount.Location = new System.Drawing.Point(12, 428);
            this.cmbSteamAccount.Name = "cmbSteamAccount";
            this.cmbSteamAccount.Size = new System.Drawing.Size(300, 25);
            this.cmbSteamAccount.TabIndex = 12;
            this.cmbSteamAccount.SelectedIndexChanged += new System.EventHandler(this.CmbSteamAccount_SelectedIndexChanged);

            this.lblAccountName.AutoSize = true;
            this.lblAccountName.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblAccountName.ForeColor = System.Drawing.Color.FromArgb(120, 120, 120);
            this.lblAccountName.Location = new System.Drawing.Point(320, 432);
            this.lblAccountName.Name = "lblAccountName";
            this.lblAccountName.Text = "";

            this.lblSteamPath.AutoSize = true;
            this.lblSteamPath.Font = new System.Drawing.Font("Microsoft YaHei UI", 8F);
            this.lblSteamPath.ForeColor = System.Drawing.Color.FromArgb(120, 120, 120);
            this.lblSteamPath.Location = new System.Drawing.Point(12, 456);
            this.lblSteamPath.Name = "lblSteamPath";
            this.lblSteamPath.Size = new System.Drawing.Size(700, 15);
            this.lblSteamPath.Text = "";
            this.lblSteamPath.AutoEllipsis = true;

            this.txtSteamParams.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.txtSteamParams.Location = new System.Drawing.Point(12, 476);
            this.txtSteamParams.Name = "txtSteamParams";
            this.txtSteamParams.Size = new System.Drawing.Size(620, 24);
            this.txtSteamParams.TabIndex = 13;
            this.txtSteamParams.Multiline = false;

            this.btnCopySteamParams.Location = new System.Drawing.Point(640, 474);
            this.btnCopySteamParams.Name = "btnCopySteamParams";
            this.btnCopySteamParams.Size = new System.Drawing.Size(70, 27);
            this.btnCopySteamParams.TabIndex = 14;
            this.btnCopySteamParams.Text = "一键复制";
            this.btnCopySteamParams.UseVisualStyleBackColor = true;
            this.btnCopySteamParams.Click += new System.EventHandler(this.BtnCopySteamParams_Click);

            this.flpSteamTags.AutoSize = true;
            this.flpSteamTags.Location = new System.Drawing.Point(12, 506);
            this.flpSteamTags.Name = "flpSteamTags";
            this.flpSteamTags.Size = new System.Drawing.Size(710, 30);
            this.flpSteamTags.TabIndex = 15;

            this.btnApplySteam.Location = new System.Drawing.Point(12, 542);
            this.btnApplySteam.Name = "btnApplySteam";
            this.btnApplySteam.Size = new System.Drawing.Size(130, 32);
            this.btnApplySteam.TabIndex = 16;
            this.btnApplySteam.Text = "应用启动项参数";
            this.btnApplySteam.UseVisualStyleBackColor = true;
            this.btnApplySteam.Click += new System.EventHandler(this.BtnApplySteam_Click);

            // ═══════════════════════════════════════
            // 永劫无间小开关 (Naraka模式)
            // ═══════════════════════════════════════

            this.btnNarakaSwitch.Location = new System.Drawing.Point(12, 400);
            this.btnNarakaSwitch.Name = "btnNarakaSwitch";
            this.btnNarakaSwitch.Size = new System.Drawing.Size(160, 36);
            this.btnNarakaSwitch.TabIndex = 20;
            this.btnNarakaSwitch.Text = "永劫无间小开关";
            this.btnNarakaSwitch.Font = new System.Drawing.Font("Microsoft YaHei UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnNarakaSwitch.UseVisualStyleBackColor = true;
            this.btnNarakaSwitch.Visible = false;
            this.btnNarakaSwitch.Click += new System.EventHandler(this.BtnNarakaSwitch_Click);

            this.lblNarakaSwitchStatus.AutoSize = true;
            this.lblNarakaSwitchStatus.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblNarakaSwitchStatus.ForeColor = System.Drawing.Color.FromArgb(80, 80, 80);
            this.lblNarakaSwitchStatus.Location = new System.Drawing.Point(180, 410);
            this.lblNarakaSwitchStatus.Name = "lblNarakaSwitchStatus";
            this.lblNarakaSwitchStatus.Text = "";
            this.lblNarakaSwitchStatus.Visible = false;

            // ═══════════════════════════════════════
            // 底部按钮
            // ═══════════════════════════════════════

            this.btnMoreFeatures.Location = new System.Drawing.Point(12, 600);
            this.btnMoreFeatures.Name = "btnMoreFeatures";
            this.btnMoreFeatures.Size = new System.Drawing.Size(90, 32);
            this.btnMoreFeatures.TabIndex = 18;
            this.btnMoreFeatures.Text = "游戏选择...";
            this.btnMoreFeatures.UseVisualStyleBackColor = true;
            this.btnMoreFeatures.Click += new System.EventHandler(this.BtnMoreFeatures_Click);

            this.btnCrosshair.Location = new System.Drawing.Point(110, 600);
            this.btnCrosshair.Name = "btnCrosshair";
            this.btnCrosshair.Size = new System.Drawing.Size(90, 32);
            this.btnCrosshair.TabIndex = 19;
            this.btnCrosshair.Text = "屏幕准心";
            this.btnCrosshair.UseVisualStyleBackColor = true;
            this.btnCrosshair.Click += new System.EventHandler(this.BtnCrosshair_Click);

            this.btnPackGo.Location = new System.Drawing.Point(500, 598);
            this.btnPackGo.Name = "btnPackGo";
            this.btnPackGo.Size = new System.Drawing.Size(104, 34);
            this.btnPackGo.TabIndex = 20;
            this.btnPackGo.Text = "打包带走";
            this.btnPackGo.UseVisualStyleBackColor = true;
            this.btnPackGo.Click += new System.EventHandler(this.BtnPackGo_Click);

            this.btnLaunchApex.Location = new System.Drawing.Point(612, 596);
            this.btnLaunchApex.Name = "btnLaunchApex";
            this.btnLaunchApex.Size = new System.Drawing.Size(118, 38);
            this.btnLaunchApex.TabIndex = 17;
            this.btnLaunchApex.Text = "启动APEX";
            this.btnLaunchApex.Font = new System.Drawing.Font("Microsoft YaHei UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnLaunchApex.UseVisualStyleBackColor = true;
            this.btnLaunchApex.Click += new System.EventHandler(this.BtnLaunchApex_Click);

            // ═══════════════════════════════════════
            // 气泡提示
            // ═══════════════════════════════════════

            this.lblToast.AutoSize = false;
            this.lblToast.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.lblToast.ForeColor = System.Drawing.Color.FromArgb(0, 120, 60);
            this.lblToast.Location = new System.Drawing.Point(12, 576);
            this.lblToast.Name = "lblToast";
            this.lblToast.Size = new System.Drawing.Size(590, 22);
            this.lblToast.Text = "";
            this.lblToast.Visible = false;
            this.lblToast.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // ═══════════════════════════════════════
            // MainForm
            // ═══════════════════════════════════════
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(748, 650);
            this.Controls.Add(this.lblToast);
            this.Controls.Add(this.btnMoreFeatures);
            this.Controls.Add(this.btnCrosshair);
            this.Controls.Add(this.btnPackGo);
            this.Controls.Add(this.btnLaunchApex);
            this.Controls.Add(this.btnNarakaSwitch);
            this.Controls.Add(this.lblNarakaSwitchStatus);
            this.Controls.Add(this.btnApplySteam);
            this.Controls.Add(this.flpSteamTags);
            this.Controls.Add(this.btnCopySteamParams);
            this.Controls.Add(this.txtSteamParams);
            this.Controls.Add(this.lblSteamPath);
            this.Controls.Add(this.cmbSteamAccount);
            this.Controls.Add(this.lblAccountName);
            this.Controls.Add(this.lblSteamAccount);
            this.Controls.Add(this.lblSteamTitle);
            this.Controls.Add(this.btnApplyToGame);
            this.Controls.Add(this.btnCreateBackup);
            this.Controls.Add(this.flpBackupCards);
            this.Controls.Add(this.lblBackupTitle);
            this.Controls.Add(this.btnRefreshStatus);
            this.Controls.Add(this.btnBrowsePath);
            this.Controls.Add(this.btnOpenFolder);
            this.Controls.Add(this.lblPath);
            this.Controls.Add(this.lblPathTitle);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.lblStatusTitle);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.Name = "MainForm";
            this.Text = "Apex - 游戏设置管理器";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblStatusTitle;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblPathTitle;
        private System.Windows.Forms.Label lblPath;
        private System.Windows.Forms.Button btnOpenFolder;
        private System.Windows.Forms.Button btnBrowsePath;
        private System.Windows.Forms.Button btnRefreshStatus;
        private System.Windows.Forms.Label lblBackupTitle;
        private System.Windows.Forms.FlowLayoutPanel flpBackupCards;
        private System.Windows.Forms.Button btnCreateBackup;
        private System.Windows.Forms.Button btnApplyToGame;
        private System.Windows.Forms.Label lblSteamTitle;
        private System.Windows.Forms.Label lblSteamAccount;
        private System.Windows.Forms.ComboBox cmbSteamAccount;
        private System.Windows.Forms.Label lblAccountName;
        private System.Windows.Forms.Label lblSteamPath;
        private System.Windows.Forms.TextBox txtSteamParams;
        private System.Windows.Forms.Button btnCopySteamParams;
        private System.Windows.Forms.Button btnApplySteam;
        private System.Windows.Forms.FlowLayoutPanel flpSteamTags;
        private System.Windows.Forms.Button btnNarakaSwitch;
        private System.Windows.Forms.Label lblNarakaSwitchStatus;
        private System.Windows.Forms.Button btnLaunchApex;
        private System.Windows.Forms.Button btnPackGo;
        private System.Windows.Forms.Button btnMoreFeatures;
        private System.Windows.Forms.Button btnCrosshair;
        private System.Windows.Forms.Label lblToast;
    }
}
