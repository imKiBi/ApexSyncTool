using System;
using System.IO;
using System.Windows.Forms;

namespace ApexSyncTool.Core
{
    /// <summary>
    /// Manages Apex Legends configuration paths and detection
    /// </summary>
    public class ApexPathManager
    {
        private string _apexLocalPath;
        private string _apexProfilePath;
        private string _steamConfigPath;

        public bool IsApexInstalled { get; private set; }
        public string ApexLocalPath => _apexLocalPath;
        public string ApexProfilePath => _apexProfilePath;
        public string SteamConfigPath => _steamConfigPath;

        public ApexPathManager()
        {
            DetectApexPath();
        }

        private void DetectApexPath()
        {
            try
            {
                string userName = Environment.UserName;
                string basePath = $@"C:\Users\{userName}\Saved Games\Respawn\Apex";

                _apexLocalPath = Path.Combine(basePath, "local");
                _apexProfilePath = Path.Combine(basePath, "profile");
                _steamConfigPath = GetSteamConfigPath();

                // Verify all paths exist
                IsApexInstalled = Directory.Exists(_apexLocalPath) && Directory.Exists(_apexProfilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"路径检测失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                IsApexInstalled = false;
            }
        }

        private string GetSteamConfigPath()
        {
            try
            {
                // Get Steam user ID from registry
                string steamPath = @"C:\Program Files (x86)\Steam";
                string userdataPath = Path.Combine(steamPath, "userdata");

                if (Directory.Exists(userdataPath))
                {
                    var userDirs = Directory.GetDirectories(userdataPath);
                    if (userDirs.Length > 0)
                    {
                        return Path.Combine(userDirs[0], "config", "localconfig.vdf");
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public ApexGameStatus GetGameStatus()
        {
            var status = new ApexGameStatus();

            if (!IsApexInstalled)
            {
                status.Status = "未检测到Apex英雄";
                status.IsInstalled = false;
                return status;
            }

            try
            {
                status.IsInstalled = true;
                status.LocalPath = _apexLocalPath;
                status.ProfilePath = _apexProfilePath;

                // Check config files
                status.HasSettings = File.Exists(Path.Combine(_apexLocalPath, "settings.cfg"));
                status.HasVideoConfig = File.Exists(Path.Combine(_apexLocalPath, "videoconfig.txt"));
                status.HasVoiceVolumes = File.Exists(Path.Combine(_apexLocalPath, "voice_volumes.dat"));
                status.HasProfile = File.Exists(Path.Combine(_apexProfilePath, "profile.cfg"));

                status.Status = "已检测到Apex英雄配置";
                return status;
            }
            catch (Exception ex)
            {
                status.Status = $"检测失败: {ex.Message}";
                return status;
            }
        }
    }

    public class ApexGameStatus
    {
        public bool IsInstalled { get; set; }
        public string Status { get; set; }
        public string LocalPath { get; set; }
        public string ProfilePath { get; set; }
        public bool HasSettings { get; set; }
        public bool HasVideoConfig { get; set; }
        public bool HasVoiceVolumes { get; set; }
        public bool HasProfile { get; set; }
    }
}
