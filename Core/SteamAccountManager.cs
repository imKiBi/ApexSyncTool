using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ApexSyncTool.Core
{
    /// <summary>
    /// Manages multiple Steam accounts and their configurations
    /// </summary>
    public class SteamAccountManager
    {
        private const string APEX_APP_ID = "1172470";
        private string _steamPath;
        private string _steamUserdataPath;
        private List<SteamAccount> _accounts = new List<SteamAccount>();

        public SteamAccountManager()
        {
            _steamPath = DetectSteamPath();
            DetectSteamAccounts();
        }

        private string DetectSteamPath()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\WOW6432Node\Valve\Steam"))
                {
                    if (key != null)
                    {
                        string path = key.GetValue("InstallPath") as string;
                        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                            return path;
                    }
                }
            }
            catch { }

            // 回退默认路径
            return @"C:\Program Files (x86)\Steam";
        }

        public List<SteamAccount> GetAllAccounts()
        {
            return _accounts;
        }

        public SteamAccount GetAccountById(string steamId)
        {
            return _accounts.FirstOrDefault(a => a.SteamId == steamId);
        }

        private void DetectSteamAccounts()
        {
            try
            {
                _steamUserdataPath = Path.Combine(_steamPath, "userdata");

                if (!Directory.Exists(_steamUserdataPath))
                    return;

                // 读取 loginusers.vdf 获取账户昵称
                var personaNames = ReadPersonaNames();

                var userDirs = Directory.GetDirectories(_steamUserdataPath);

                foreach (var userDir in userDirs)
                {
                    string steamId = Path.GetFileName(userDir);
                    string configPath = Path.Combine(userDir, "config", "localconfig.vdf");

                    if (File.Exists(configPath))
                    {
                        var account = new SteamAccount
                        {
                            SteamId = steamId,
                            ConfigPath = configPath,
                            LastModified = File.GetLastWriteTime(configPath)
                        };

                        // 匹配昵称 (SteamID3 → SteamID64)
                        if (long.TryParse(steamId, out long id3))
                        {
                            string id64 = (id3 + 76561197960265728L).ToString();
                            if (personaNames.ContainsKey(id64))
                                account.PersonaName = personaNames[id64];
                        }

                        // Get current launch options for Apex
                        var launchOptions = GetLaunchOptions(configPath);
                        account.CurrentLaunchOptions = launchOptions;

                        _accounts.Add(account);
                    }
                }

                // Sort by last modified time (newest first)
                _accounts = _accounts.OrderByDescending(a => a.LastModified).ToList();
            }
            catch { }
        }

        private Dictionary<string, string> ReadPersonaNames()
        {
            var names = new Dictionary<string, string>();
            try
            {
                string loginUsersPath = Path.Combine(_steamPath, "config", "loginusers.vdf");
                if (!File.Exists(loginUsersPath))
                    return names;

                string content = File.ReadAllText(loginUsersPath);
                // 匹配 "SteamID64" { ... "PersonaName" "xxx" ... }
                var matches = Regex.Matches(content,
                    @"""(\d{17})""\s*\{[^}]*?""PersonaName""\s+""([^""]*)""",
                    RegexOptions.Singleline);

                foreach (Match match in matches)
                {
                    names[match.Groups[1].Value] = match.Groups[2].Value;
                }
            }
            catch { }
            return names;
        }

        private string GetLaunchOptions(string configPath)
        {
            try
            {
                var lines = File.ReadAllLines(configPath);
                bool inApexBlock = false;
                int braceDepth = 0;

                for (int i = 0; i < lines.Length; i++)
                {
                    string trimmed = lines[i].Trim();

                    if (!inApexBlock)
                    {
                        if (trimmed.Contains("\"" + APEX_APP_ID + "\""))
                        {
                            if (trimmed.Contains("{"))
                            {
                                inApexBlock = true;
                                braceDepth = 1;
                            }
                            else if (i + 1 < lines.Length && lines[i + 1].Trim().StartsWith("{"))
                            {
                                inApexBlock = true;
                                braceDepth = 1;
                                i++;
                            }
                        }
                    }
                    else
                    {
                        foreach (char c in trimmed)
                        {
                            if (c == '{') braceDepth++;
                            else if (c == '}') braceDepth--;
                        }

                        if (braceDepth <= 0)
                        {
                            inApexBlock = false;
                            continue;
                        }

                        if (trimmed.Contains("\"LaunchOptions\""))
                        {
                            var match = Regex.Match(trimmed, @"""LaunchOptions""\s+""([^""]*)""");
                            if (match.Success)
                                return match.Groups[1].Value;
                        }
                    }
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public bool SetLaunchOptions(string steamId, string launchOptions)
        {
            var account = GetAccountById(steamId);
            if (account == null)
                return false;

            return SetLaunchOptionsImpl(account.ConfigPath, launchOptions);
        }

        public bool SetLaunchOptionsForAll(string launchOptions)
        {
            bool allSuccess = true;

            foreach (var account in _accounts)
            {
                if (!SetLaunchOptionsImpl(account.ConfigPath, launchOptions))
                    allSuccess = false;
            }

            return allSuccess;
        }

        private bool SetLaunchOptionsImpl(string configPath, string launchOptions)
        {
            try
            {
                if (!File.Exists(configPath))
                    return false;

                var lines = File.ReadAllLines(configPath);
                bool inApexBlock = false;
                int braceDepth = 0;
                bool modified = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    string trimmed = lines[i].Trim();

                    if (!inApexBlock)
                    {
                        if (trimmed.Contains("\"" + APEX_APP_ID + "\""))
                        {
                            if (trimmed.Contains("{"))
                            {
                                inApexBlock = true;
                                braceDepth = 1;
                            }
                            else if (i + 1 < lines.Length && lines[i + 1].Trim().StartsWith("{"))
                            {
                                inApexBlock = true;
                                braceDepth = 1;
                                i++;
                            }
                        }
                    }
                    else
                    {
                        foreach (char c in trimmed)
                        {
                            if (c == '{') braceDepth++;
                            else if (c == '}') braceDepth--;
                        }

                        if (braceDepth <= 0)
                        {
                            inApexBlock = false;
                            continue;
                        }

                        if (trimmed.Contains("\"LaunchOptions\""))
                        {
                            // 保留原始缩进
                            string indent = lines[i].Substring(0, lines[i].IndexOf('"'));
                            lines[i] = $"{indent}\"LaunchOptions\"\t\t\"{EscapeForVdf(launchOptions)}\"";
                            modified = true;
                            break;
                        }
                    }
                }

                if (!modified)
                    return false;

                File.WriteAllLines(configPath, lines);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string EscapeForVdf(string value)
        {
            return value.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }

    public class SteamAccount
    {
        public string SteamId { get; set; }
        public string ConfigPath { get; set; }
        public string CurrentLaunchOptions { get; set; }
        public string PersonaName { get; set; }
        public DateTime LastModified { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(PersonaName))
                return $"{PersonaName} ({SteamId})";
            return $"{SteamId} (最后修改: {LastModified:yyyy-MM-dd HH:mm:ss})";
        }
    }
}
