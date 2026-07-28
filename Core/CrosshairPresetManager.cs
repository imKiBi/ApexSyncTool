using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApexSyncTool.Core
{
    /// <summary>准心预设配置（序列化为 config.json 保存到 backups/Mycrosshair/{名称}/ 下）</summary>
    public class CrosshairConfig
    {
        public int Style { get; set; } = 1;                 // 对应 CrosshairStyle 枚举
        public int ColorArgb { get; set; } = -16711936;     // 默认 Lime
        public int Size { get; set; } = 24;
        public float Thickness { get; set; } = 2f;
        public int Gap { get; set; } = 4;
        public int Opacity { get; set; } = 100;
        public bool UseImage { get; set; } = false;         // true 时同目录下 image.png 为自定义准心图
    }

    /// <summary>
    /// 准心预设管理：每个预设一个文件夹（config.json + 可选 image.png），
    /// 存放在程序目录 backups/Mycrosshair 下。
    /// </summary>
    public class CrosshairPresetManager
    {
        private readonly string _root;
        public string Root => _root;

        public CrosshairPresetManager()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _root = Path.Combine(baseDir, "backups", "Mycrosshair");
            try { Directory.CreateDirectory(_root); } catch { }
        }

        /// <summary>按修改时间倒序列出预设名</summary>
        public List<string> ListPresets()
        {
            var list = new List<string>();
            try
            {
                foreach (var dir in Directory.GetDirectories(_root))
                {
                    if (File.Exists(Path.Combine(dir, "config.json")))
                        list.Add(Path.GetFileName(dir));
                }
                list.Sort((a, b) => Directory.GetLastWriteTime(Path.Combine(_root, b))
                                         .CompareTo(Directory.GetLastWriteTime(Path.Combine(_root, a))));
            }
            catch { }
            return list;
        }

        public CrosshairConfig LoadConfig(string name)
        {
            try
            {
                string p = Path.Combine(_root, name, "config.json");
                if (!File.Exists(p)) return null;
                return JsonSerializer.Deserialize(File.ReadAllText(p), CrosshairConfigJsonContext.Default.CrosshairConfig);
            }
            catch { return null; }
        }

        /// <summary>读取预设的自定义图片；没有则返回 null</summary>
        public Image LoadImage(string name)
        {
            try
            {
                string p = Path.Combine(_root, name, "image.png");
                if (!File.Exists(p)) return null;
                // 必须通过流读取并复制，避免文件被长期锁定
                using (var fs = new FileStream(p, FileMode.Open, FileAccess.Read))
                    return Image.FromStream(fs);
            }
            catch { return null; }
        }

        public bool SavePreset(string name, CrosshairConfig cfg, Image customImage)
        {
            try
            {
                string dir = Path.Combine(_root, name);
                Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(cfg, CrosshairConfigJsonContext.Default.CrosshairConfig);
                File.WriteAllText(Path.Combine(dir, "config.json"), json);

                string imgPath = Path.Combine(dir, "image.png");
                if (cfg.UseImage && customImage != null)
                {
                    customImage.Save(imgPath, System.Drawing.Imaging.ImageFormat.Png);
                }
                else if (File.Exists(imgPath))
                {
                    File.Delete(imgPath); // 程序自己生成的附属文件，覆盖保存时清理
                }
                return true;
            }
            catch { return false; }
        }

        /// <summary>重命名预设（整体移动文件夹）；目标名已存在时返回 false</summary>
        public bool RenamePreset(string oldName, string newName)
        {
            try
            {
                string src = Path.Combine(_root, oldName);
                string dst = Path.Combine(_root, newName);
                if (!Directory.Exists(src) || Directory.Exists(dst)) return false;
                Directory.Move(src, dst);
                return true;
            }
            catch { return false; }
        }

        /// <summary>删除预设（优先送入回收站）</summary>
        public bool DeletePreset(string name)
        {
            string dir = Path.Combine(_root, name);
            if (!Directory.Exists(dir)) return true;
            try
            {
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(
                    dir,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                return true;
            }
            catch
            {
                try { Directory.Delete(dir, true); return true; } catch { return false; }
            }
        }

        /// <summary>清理非法文件名字符</summary>
        public static string SanitizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            name = name.Trim().TrimEnd('.');
            if (name.Length > 40) name = name.Substring(0, 40);
            return name.Length == 0 ? null : name;
        }
    }

    /// <summary>源生成 JSON 上下文（PublishTrimmed 下替代反射序列化）</summary>
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(CrosshairConfig))]
    internal partial class CrosshairConfigJsonContext : JsonSerializerContext { }
}
