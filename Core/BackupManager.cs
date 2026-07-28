using System;
using System.IO;
using System.Text;

namespace ApexSyncTool.Core
{
    /// <summary>
    /// Manages backup and restore operations for Apex config files
    /// </summary>
    public class BackupManager
    {
        private string _backupPath;
        private ApexPathManager _pathManager;

        // Config file names
        private const string SETTINGS_FILE = "settings.cfg";
        private const string VIDEOCONFIG_FILE = "videoconfig.txt";
        private const string VOICE_VOLUMES_FILE = "voice_volumes.dat";
        private const string PROFILE_FILE = "profile.cfg";

        public BackupManager(ApexPathManager pathManager, string backupBasePath = null)
        {
            _pathManager = pathManager;
            
            // 备份始终保存到程序执行目录下
            _backupPath = string.IsNullOrEmpty(backupBasePath)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backups")
                : backupBasePath;

            // Ensure backup directory exists
            if (!Directory.Exists(_backupPath))
            {
                try
                {
                    Directory.CreateDirectory(_backupPath);
                }
                catch { }
            }
        }

        public string BackupPath => _backupPath;

        /// <summary>
        /// Update the backup root directory at runtime (e.g. per-game subfolder)
        /// </summary>
        public void SetBackupPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            _backupPath = path;
            if (!Directory.Exists(_backupPath))
            {
                try { Directory.CreateDirectory(_backupPath); }
                catch { }
            }
        }

        private bool IsPathWritable(string path)
        {
            try
            {
                string testFile = Path.Combine(path, $".write_test_{Guid.NewGuid()}");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Create a backup with timestamp
        /// </summary>
        public BackupResult CreateBackup(string backupName = null)
        {
            var result = new BackupResult();

            try
            {
                if (!_pathManager.IsApexInstalled)
                {
                    result.Success = false;
                    result.Message = "未检测到Apex英雄配置";
                    return result;
                }

                // Create timestamped backup directory
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string displayName = string.IsNullOrEmpty(backupName) ? timestamp : $"{backupName}_{timestamp}";
                string backupDir = Path.Combine(_backupPath, displayName);

                Directory.CreateDirectory(backupDir);

                // Backup local files
                string localDir = Path.Combine(backupDir, "local");
                Directory.CreateDirectory(localDir);

                CopyFileIfExists(_pathManager.ApexLocalPath, localDir, SETTINGS_FILE);
                CopyFileIfExists(_pathManager.ApexLocalPath, localDir, VIDEOCONFIG_FILE);
                CopyFileIfExists(_pathManager.ApexLocalPath, localDir, VOICE_VOLUMES_FILE);

                // Backup profile file
                string profileDir = Path.Combine(backupDir, "profile");
                Directory.CreateDirectory(profileDir);
                CopyFileIfExists(_pathManager.ApexProfilePath, profileDir, PROFILE_FILE);

                result.Success = true;
                result.BackupPath = backupDir;
                result.Message = $"备份成功: {displayName}";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"备份失败: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Restore backup to current Apex config
        /// </summary>
        public RestoreResult RestoreBackup(string backupPath, bool autoBackupCurrent = true)
        {
            var result = new RestoreResult();

            try
            {
                if (!_pathManager.IsApexInstalled)
                {
                    result.Success = false;
                    result.Message = "未检测到Apex英雄配置";
                    return result;
                }

                // Auto backup current config before restore
                if (autoBackupCurrent)
                {
                    var backupResult = CreateBackup("auto_backup_before_restore");
                    if (!backupResult.Success)
                    {
                        result.Success = false;
                        result.Message = $"创建备份失败: {backupResult.Message}";
                        return result;
                    }
                    result.AutoBackupPath = backupResult.BackupPath;
                }

                // Restore local files
                string localSrcDir = Path.Combine(backupPath, "local");
                if (Directory.Exists(localSrcDir))
                {
                    CopyFileIfExists(localSrcDir, _pathManager.ApexLocalPath, SETTINGS_FILE);
                    CopyFileIfExists(localSrcDir, _pathManager.ApexLocalPath, VIDEOCONFIG_FILE);
                    CopyFileIfExists(localSrcDir, _pathManager.ApexLocalPath, VOICE_VOLUMES_FILE);
                }

                // Restore profile file
                string profileSrcDir = Path.Combine(backupPath, "profile");
                if (Directory.Exists(profileSrcDir))
                {
                    CopyFileIfExists(profileSrcDir, _pathManager.ApexProfilePath, PROFILE_FILE);
                }

                result.Success = true;
                result.Message = "恢复成功";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"恢复失败: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// List all available backups
        /// </summary>
        public string[] GetAvailableBackups()
        {
            try
            {
                if (!Directory.Exists(_backupPath))
                    return new string[0];

                var dirs = Directory.GetDirectories(_backupPath);
                return dirs;
            }
            catch
            {
                return new string[0];
            }
        }

        /// <summary>
        /// Delete a backup
        /// </summary>
        public bool DeleteBackup(string backupPath)
        {
            try
            {
                if (Directory.Exists(backupPath))
                {
                    Directory.Delete(backupPath, true);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private void CopyFileIfExists(string sourceDir, string destDir, string fileName)
        {
            string sourcePath = Path.Combine(sourceDir, fileName);
            string destPath = Path.Combine(destDir, fileName);

            if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, destPath, true);
            }
        }
    }

    public class BackupResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string BackupPath { get; set; }
    }

    public class RestoreResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string AutoBackupPath { get; set; }
    }
}
