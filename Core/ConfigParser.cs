using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ApexSyncTool.Core
{
    /// <summary>
    /// Parses and extracts key configuration values from Apex config files
    /// </summary>
    public class ConfigParser
    {
        private const string SETTINGS_FILE = "settings.cfg";
        private const string VIDEOCONFIG_FILE = "videoconfig.txt";
        private const string PROFILE_FILE = "profile.cfg";

        /// <summary>
        /// 提取关键配置参数用于预览（中等粒度）
        /// </summary>
        public ConfigPreview ExtractPreview(string localPath, string profilePath)
        {
            var preview = new ConfigPreview();

            try
            {
                // Parse settings.cfg - mouse sensitivity, key bindings, scope sensitivity
                var settingsFile = Path.Combine(localPath, SETTINGS_FILE);
                if (File.Exists(settingsFile))
                {
                    var settings = ParseKeyValueFile(settingsFile);
                    preview.MouseSensitivity = settings.ContainsKey("mouse_sensitivity") 
                        ? settings["mouse_sensitivity"] : "N/A";
                    preview.PerScopeSensitivity = settings.ContainsKey("mouse_use_per_scope_sensitivity_scalars")
                        ? settings["mouse_use_per_scope_sensitivity_scalars"] : "N/A";
                    for (int i = 0; i <= 7; i++)
                    {
                        string key = "mouse_zoomed_sensitivity_scalar_" + i;
                        string val = settings.ContainsKey(key) ? settings[key] : "N/A";
                        switch (i)
                        {
                            case 0: preview.ZoomedSensitivity0 = val; break;
                            case 1: preview.ZoomedSensitivity1 = val; break;
                            case 2: preview.ZoomedSensitivity2 = val; break;
                            case 3: preview.ZoomedSensitivity3 = val; break;
                            case 4: preview.ZoomedSensitivity4 = val; break;
                            case 5: preview.ZoomedSensitivity5 = val; break;
                            case 6: preview.ZoomedSensitivity6 = val; break;
                            case 7: preview.ZoomedSensitivity7 = val; break;
                        }
                    }
                }

                // Parse videoconfig.txt - display, graphics, and audio settings
                var videoFile = Path.Combine(localPath, VIDEOCONFIG_FILE);
                if (File.Exists(videoFile))
                {
                    var video = ParseVideoConfig(videoFile);
                    string width = video.ContainsKey("resolution_width") ? video["resolution_width"] : "?";
                    string height = video.ContainsKey("resolution_height") ? video["resolution_height"] : "?";
                    preview.Resolution = width + "x" + height;
                    preview.Fullscreen = video.ContainsKey("fullscreen") ? video["fullscreen"] : "N/A";
                    preview.VSync = video.ContainsKey("mat_vsync_mode") ? video["mat_vsync_mode"] : "N/A";
                    preview.AntiAlias = video.ContainsKey("mat_antialias_mode") ? video["mat_antialias_mode"] : "N/A";
                    preview.SoundVolume = video.ContainsKey("sound_volume") ? video["sound_volume"] : "N/A";

                    // 画面/图形
                    preview.Shadow = video.ContainsKey("shadow_enable") ? video["shadow_enable"] : "N/A";
                    preview.VolumetricLighting = video.ContainsKey("volumetric_lighting") ? video["volumetric_lighting"] : "N/A";
                    preview.VolumetricFog = video.ContainsKey("volumetric_fog") ? video["volumetric_fog"] : "N/A";
                    preview.SSAO = video.ContainsKey("ssao_quality") ? video["ssao_quality"] : "N/A";
                    preview.Gamma = video.ContainsKey("gamma") ? video["gamma"] : "N/A";
                    preview.AnisotropicFiltering = video.ContainsKey("mat_forceaniso") ? video["mat_forceaniso"] : "N/A";
                    preview.StreamMemory = video.ContainsKey("stream_memory") ? video["stream_memory"] : "N/A";
                    preview.MapDetail = video.ContainsKey("map_detail_level") ? video["map_detail_level"] : "N/A";
                    preview.RagdollMax = video.ContainsKey("cl_ragdoll_maxcount") ? video["cl_ragdoll_maxcount"] : "N/A";
                    preview.GibAllow = video.ContainsKey("cl_gib_allow") ? video["cl_gib_allow"] : "N/A";
                }

                // Parse profile.cfg - FOV, audio, gameplay settings
                var profileFile = Path.Combine(profilePath, PROFILE_FILE);
                if (File.Exists(profileFile))
                {
                    var profile = ParseKeyValueFile(profileFile);
                    preview.FOV = profile.ContainsKey("cl_fovScale") ? profile["cl_fovScale"] : "N/A";
                    preview.MilesLanguage = profile.ContainsKey("miles_language") ? profile["miles_language"] : "N/A";
                    preview.SoundDialogue = profile.ContainsKey("sound_volume_dialogue") ? profile["sound_volume_dialogue"] : "N/A";
                    preview.SoundMusic = profile.ContainsKey("sound_volume_music_game") ? profile["sound_volume_music_game"] : "N/A";
                    preview.SoundSFX = profile.ContainsKey("sound_volume_sfx") ? profile["sound_volume_sfx"] : "N/A";

                    // 鼠标/瞄准
                    preview.ColorblindMode = profile.ContainsKey("colorblind_mode") ? profile["colorblind_mode"] : "N/A";
                    preview.ReticleColor = profile.ContainsKey("reticle_color") ? profile["reticle_color"] : "N/A";

                    // 音频/字幕
                    preview.CloseCaption = profile.ContainsKey("closecaption") ? profile["closecaption"] : "N/A";

                    // 游戏功能
                    preview.SprintViewShake = profile.ContainsKey("sprint_view_shake_style") ? profile["sprint_view_shake_style"] : "N/A";
                    preview.DamageIndicator = profile.ContainsKey("damage_indicator_style_pilot") ? profile["damage_indicator_style_pilot"] : "N/A";
                    preview.CrossPlay = profile.ContainsKey("CrossPlay_user_optin") ? profile["CrossPlay_user_optin"] : "N/A";
                }

                return preview;
            }
            catch (Exception ex)
            {
                preview.Error = $"解析配置失败: {ex.Message}";
                return preview;
            }
        }

        private Dictionary<string, string> ParseKeyValueFile(string filePath)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//") || line.StartsWith("bind"))
                        continue;

                    // Pattern: key "value" or key	"value"
                    var match = Regex.Match(line, @"^(\S+)\s+[""']([^""']*)[""']");
                    if (match.Success)
                    {
                        string key = match.Groups[1].Value;
                        string value = match.Groups[2].Value;
                        if (!dict.ContainsKey(key))
                            dict[key] = value;
                    }
                }
            }
            catch { }

            return dict;
        }

        private Dictionary<string, string> ParseVideoConfig(string filePath)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var content = File.ReadAllText(filePath, Encoding.UTF8);
                
                // Parse VDF-like format: "setting.key" "value"
                var matches = Regex.Matches(content, @"""setting\.(\w+)""\s+""([^""]*)""");
                foreach (Match match in matches)
                {
                    string key = match.Groups[1].Value;
                    string value = match.Groups[2].Value;
                    
                    // 特殊映射
                    if (key == "last_display_width")
                        dict["resolution_width"] = value;
                    else if (key == "last_display_height")
                        dict["resolution_height"] = value;
                    
                    // 通用存储所有 setting 键
                    if (!dict.ContainsKey(key))
                        dict[key] = value;
                }
            }
            catch { }

            return dict;
        }

        /// <summary>
        /// 提取键位绑定 (bind lines from settings.cfg)
        /// 支持格式: bind_US_standard "key" "action" 0, bind_held_US_standard "key" "action" 0, bind "key" "action"
        /// </summary>
        public Dictionary<string, string> ExtractKeyBindings(string localPath)
        {
            var bindings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var settingsFile = Path.Combine(localPath, SETTINGS_FILE);
                if (!File.Exists(settingsFile))
                    return bindings;

                var lines = File.ReadAllLines(settingsFile, Encoding.UTF8);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Pattern: bind_US_standard "key" "action" 0 / bind_held_US_standard "key" "action" 0 / bind "key" "action"
                    var match = Regex.Match(line, @"^(bind\S*)\s+[""'](\S+?)[""']\s+[""']([^""']*)[""']");
                    if (match.Success)
                    {
                        string bindType = match.Groups[1].Value;
                        string key = match.Groups[2].Value;
                        string action = match.Groups[3].Value;
                        // 用 "bindType key" 作为唯一标识，区分同一按键的不同绑定类型
                        string dictKey = bindType + " " + key;
                        bindings[dictKey] = action;
                    }
                }
            }
            catch { }

            return bindings;
        }

        /// <summary>
        /// 保存修改后的键位绑定到 settings.cfg
        /// </summary>
        public bool SaveKeyBindings(string localPath, Dictionary<string, string> bindings)
        {
            try
            {
                var settingsFile = Path.Combine(localPath, SETTINGS_FILE);
                if (!File.Exists(settingsFile))
                    return false;

                var lines = File.ReadAllLines(settingsFile, Encoding.UTF8);
                var newLines = new List<string>();
                var writtenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        newLines.Add(line);
                        continue;
                    }

                    var match = Regex.Match(line, @"^(bind\S*)\s+[""'](\S+?)[""']\s+[""']([^""']*)[""']");
                    if (match.Success)
                    {
                        string bindType = match.Groups[1].Value;
                        string key = match.Groups[2].Value;
                        string dictKey = bindType + " " + key;

                        if (bindings.ContainsKey(dictKey))
                        {
                            // 保留原始行尾的数字参数（如 0 或 1）
                            var trailingMatch = Regex.Match(line, @"""[^""]*""\s+(\d+)\s*$");
                            string trailing = trailingMatch.Success ? " " + trailingMatch.Groups[1].Value : " 0";
                            newLines.Add($"{bindType} \"{key}\" \"{bindings[dictKey]}\"{trailing}");
                            writtenKeys.Add(dictKey);
                        }
                        else
                        {
                            newLines.Add(line);
                        }
                    }
                    else
                    {
                        newLines.Add(line);
                    }
                }

                // 添加新增的绑定
                foreach (var kvp in bindings)
                {
                    if (!writtenKeys.Contains(kvp.Key))
                    {
                        // dictKey format: "bindType key"
                        var parts = kvp.Key.Split(new[] { ' ' }, 2);
                        if (parts.Length == 2)
                        {
                            newLines.Add($"{parts[0]} \"{parts[1]}\" \"{kvp.Value}\" 0");
                        }
                    }
                }

                File.WriteAllLines(settingsFile, newLines, Encoding.UTF8);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool CompareConfigs(string backupLocalPath, string currentLocalPath, string backupProfilePath, string currentProfilePath, out string differences)
        {
            differences = string.Empty;
            var diff = new StringBuilder();

            try
            {
                var current = ExtractPreview(currentLocalPath, currentProfilePath);
                var backup = ExtractPreview(backupLocalPath, backupProfilePath);

                if (current.Resolution != backup.Resolution)
                    diff.AppendLine($"分辨率: {backup.Resolution} → {current.Resolution}");
                if (current.MouseSensitivity != backup.MouseSensitivity)
                    diff.AppendLine($"灵敏度: {backup.MouseSensitivity} → {current.MouseSensitivity}");
                if (current.FOV != backup.FOV)
                    diff.AppendLine($"FOV: {backup.FOV} → {current.FOV}");
                if (current.VSync != backup.VSync)
                    diff.AppendLine($"垂直同步: {backup.VSync} → {current.VSync}");

                differences = diff.ToString();
                return !string.IsNullOrWhiteSpace(differences);
            }
            catch (Exception ex)
            {
                differences = $"对比失败: {ex.Message}";
                return false;
            }
        }
    }

    public class ConfigPreview
    {
        // 基础
        public string Resolution { get; set; } = "N/A";
        public string MouseSensitivity { get; set; } = "N/A";
        public string FOV { get; set; } = "N/A";
        public string Fullscreen { get; set; } = "N/A";
        public string VSync { get; set; } = "N/A";
        public string AntiAlias { get; set; } = "N/A";
        public string SoundVolume { get; set; } = "N/A";
        public string SoundDialogue { get; set; } = "N/A";
        public string SoundMusic { get; set; } = "N/A";
        public string SoundSFX { get; set; } = "N/A";
        public string MilesLanguage { get; set; } = "N/A";
        public string Error { get; set; }

        // 画面/图形
        public string Shadow { get; set; } = "N/A";
        public string VolumetricLighting { get; set; } = "N/A";
        public string VolumetricFog { get; set; } = "N/A";
        public string SSAO { get; set; } = "N/A";
        public string Gamma { get; set; } = "N/A";
        public string AnisotropicFiltering { get; set; } = "N/A";
        public string StreamMemory { get; set; } = "N/A";
        public string MapDetail { get; set; } = "N/A";

        // 鼠标/瞄准
        public string PerScopeSensitivity { get; set; } = "N/A";
        public string ZoomedSensitivity0 { get; set; } = "N/A";
        public string ZoomedSensitivity1 { get; set; } = "N/A";
        public string ZoomedSensitivity2 { get; set; } = "N/A";
        public string ZoomedSensitivity3 { get; set; } = "N/A";
        public string ZoomedSensitivity4 { get; set; } = "N/A";
        public string ZoomedSensitivity5 { get; set; } = "N/A";
        public string ZoomedSensitivity6 { get; set; } = "N/A";
        public string ZoomedSensitivity7 { get; set; } = "N/A";
        public string ColorblindMode { get; set; } = "N/A";
        public string ReticleColor { get; set; } = "N/A";

        // 音频/字幕
        public string CloseCaption { get; set; } = "N/A";

        // 游戏功能
        public string SprintViewShake { get; set; } = "N/A";
        public string DamageIndicator { get; set; } = "N/A";
        public string CrossPlay { get; set; } = "N/A";
        public string RagdollMax { get; set; } = "N/A";
        public string GibAllow { get; set; } = "N/A";

        public override string ToString()
        {
            return $@"=== Apex英雄配置预览 ===
分辨率: {Resolution}
全屏: {Fullscreen}
灵敏度: {MouseSensitivity}
FOV: {FOV}
垂直同步: {VSync}
抗锯齿: {AntiAlias}
音效音量: {SoundVolume}
对话音量: {SoundDialogue}
背景音乐: {SoundMusic}
游戏音效: {SoundSFX}
语言: {MilesLanguage}";
        }
    }
}
