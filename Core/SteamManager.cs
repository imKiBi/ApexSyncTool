using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ApexSyncTool.Core
{
    /// <summary>
    /// Manages Steam launch options for Apex Legends (AppID: 1172470)
    /// </summary>
    public class SteamManager
    {
        private const int APEX_APP_ID = 1172470;
        private const string VDF_FILE = "localconfig.vdf";
        
        private string _steamConfigPath;
        private ApexPathManager _pathManager;

        public SteamManager(ApexPathManager pathManager, string steamConfigPath = null)
        {
            _pathManager = pathManager;
            _steamConfigPath = steamConfigPath ?? pathManager.SteamConfigPath;
        }

        public bool IsSteamRunning()
        {
            try
            {
                var processes = Process.GetProcessesByName("steam");
                return processes.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get current Apex launch options from Steam config
        /// </summary>
        public string GetCurrentLaunchOptions()
        {
            try
            {
                if (string.IsNullOrEmpty(_steamConfigPath) || !File.Exists(_steamConfigPath))
                    return string.Empty;

                string content = File.ReadAllText(_steamConfigPath);
                
                // Pattern: "1172470"  {  "LaunchOptions" "..."  }
                var match = Regex.Match(content, 
                    $@"""1172470""\s*\{{[^}}]*?""LaunchOptions""\s+""([^""]*)""",
                    RegexOptions.Singleline);

                return match.Success ? match.Groups[1].Value : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Update launch options in Steam config
        /// </summary>
        public bool SetLaunchOptions(string launchOptions)
        {
            try
            {
                if (string.IsNullOrEmpty(_steamConfigPath) || !File.Exists(_steamConfigPath))
                    return false;

                // Check if Steam is running
                if (IsSteamRunning())
                {
                    return false; // Caller should handle this case
                }

                string content = File.ReadAllText(_steamConfigPath);
                
                // Replace or add LaunchOptions
                // First, try to find and replace existing LaunchOptions
                var pattern = $@"""1172470""\s*\{{([^}}]*?)""LaunchOptions""\s+""[^""]*""";
                var replacement = $@"""1172470"" {{$1""LaunchOptions"" ""{EscapeForVdf(launchOptions)}""";
                
                string newContent = Regex.Replace(content, pattern, replacement, RegexOptions.Singleline);
                
                // If no match, something is wrong
                if (newContent == content)
                {
                    return false;
                }

                File.WriteAllText(_steamConfigPath, newContent);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string EscapeForVdf(string value)
        {
            // Basic VDF escaping
            return value.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        /// <summary>
        /// Get predefined launch option presets
        /// </summary>
        public Dictionary<string, string> GetPresets()
        {
            return new Dictionary<string, string>
            {
                { "竞技模式", "-dev -high -novid +cl_showfps 1" },
                { "低端模式", "-high -novid -nojoy" },
                { "录制模式", "-dev +cl_showfps 0" },
                { "自定义", "" }
            };
        }

        /// <summary>
        /// Validate launch options format
        /// </summary>
        public bool ValidateLaunchOptions(string options)
        {
            // Basic validation - launch options should start with - or +
            if (string.IsNullOrWhiteSpace(options))
                return true;

            var parts = options.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (!part.StartsWith("-") && !part.StartsWith("+"))
                    return false;
            }
            return true;
        }
    }
}
