using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;

namespace ApexSyncTool.UI
{
    /// <summary>
    /// 打包带走进度窗口：把程序整个文件夹（本体 + backups 等数据）压缩为 .zip，
    /// 后台线程执行，实时显示进度，可取消。
    /// </summary>
    public class PackProgressForm : Form
    {
        private static readonly Color Bg = Color.FromArgb(24, 26, 32);
        private static readonly Color TextColor = Color.FromArgb(228, 231, 240);
        private static readonly Color SubColor = Color.FromArgb(140, 146, 160);

        private readonly string _sourceDir;
        private readonly string _zipPath;
        private readonly BackgroundWorker _worker;
        private readonly ProgressBar _progressBar;
        private readonly Label _lblStatus;
        private readonly Button _btnCancel;

        public bool PackSucceeded { get; private set; }
        public string ErrorMessage { get; private set; }

        public PackProgressForm(string sourceDir, string zipPath)
        {
            _sourceDir = sourceDir;
            _zipPath = zipPath;

            Text = "正在打包带走...";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 130);
            BackColor = Bg;
            Font = new Font("Microsoft YaHei UI", 9F);

            _lblStatus = new Label
            {
                Text = "正在收集文件...", Left = 16, Top = 14, Width = 388,
                ForeColor = TextColor, AutoEllipsis = true
            };
            _progressBar = new ProgressBar { Left = 16, Top = 42, Width = 388, Height = 22, Minimum = 0, Maximum = 100 };
            _btnCancel = new Button
            {
                Text = "取消", Left = 330, Top = 78, Width = 74, Height = 28,
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(34, 37, 46), ForeColor = TextColor
            };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(70, 74, 86);
            _btnCancel.Click += (s, e) => { _btnCancel.Enabled = false; _worker.CancelAsync(); };

            Controls.AddRange(new Control[] { _lblStatus, _progressBar, _btnCancel });

            _worker = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            _worker.DoWork += Worker_DoWork;
            _worker.ProgressChanged += (s, e) =>
            {
                _progressBar.Value = e.ProgressPercentage;
                if (e.UserState is string msg) _lblStatus.Text = msg;
            };
            _worker.RunWorkerCompleted += (s, e) =>
            {
                if (e.Error != null) ErrorMessage = e.Error.Message;
                if (!PackSucceeded)
                {
                    // 取消或失败 → 清理不完整的压缩包
                    try { if (File.Exists(_zipPath)) File.Delete(_zipPath); } catch { }
                }
                DialogResult = PackSucceeded ? DialogResult.OK : DialogResult.Cancel;
                Close();
            };
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _worker.RunWorkerAsync();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;

            // 收集所有文件（排除输出 zip 本身）
            var files = new List<string>();
            CollectFiles(_sourceDir, files);

            string zipFull = Path.GetFullPath(_zipPath);
            files.RemoveAll(f => string.Equals(Path.GetFullPath(f), zipFull, StringComparison.OrdinalIgnoreCase));

            int total = files.Count;
            if (total == 0) throw new InvalidOperationException("没有找到需要打包的文件");

            string srcFull = Path.GetFullPath(_sourceDir);
            if (!srcFull.EndsWith(Path.DirectorySeparatorChar.ToString())) srcFull += Path.DirectorySeparatorChar;

            int done = 0, skipped = 0;
            using (var fs = new FileStream(_zipPath, FileMode.Create, FileAccess.Write))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                foreach (var file in files)
                {
                    if (worker.CancellationPending) { e.Cancel = true; return; }
                    try
                    {
                        string rel = Path.GetFullPath(file).Substring(srcFull.Length).Replace('\\', '/');
                        zip.CreateEntryFromFile(file, rel, CompressionLevel.Optimal);
                    }
                    catch { skipped++; } // 个别文件被占用等 → 跳过
                    done++;
                    if (done % 5 == 0 || done == total)
                    {
                        int pct = (int)(done * 100L / total);
                        worker.ReportProgress(pct, $"正在压缩 ({done}/{total})：{Path.GetFileName(file)}");
                    }
                }
            }

            if (skipped > 0) ErrorMessage = $"打包完成，但有 {skipped} 个文件被跳过";
            PackSucceeded = true;
        }

        private static void CollectFiles(string dir, List<string> result)
        {
            try
            {
                result.AddRange(Directory.GetFiles(dir));
                foreach (var sub in Directory.GetDirectories(dir))
                    CollectFiles(sub, result);
            }
            catch { }
        }
    }
}
