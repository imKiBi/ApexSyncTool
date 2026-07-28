# ApexSyncTool - 游戏设置同步工具

一个Apex和部分其他游戏的配置（Config）备份器，可以用于配置管理、一键更换、打包分享和屏幕准心等实用功能。
轻量级 Windows 桌面应用，用于备份、恢复和同步游戏设置。主要支持 **Apex Legends** 和个别其他游戏，内置屏幕准心、新手教程、一键打包等功能。特别适合网吧和多电脑用户跨设备迁移配置。

**版本**: 2.0.0  
**框架**: .NET 8.0 Windows Forms  
**部署**: 单文件 exe，自包含，无需安装 .NET 运行时  
**体积**: ~42MB（启用 IL 裁剪 + 单文件压缩）

---

## 功能特性

### 游戏支持

- **Apex Legends**: 备份/恢复 settings.cfg、videoconfig.txt、voice_volumes.dat、profile.cfg
- **永劫无间**: 备份/恢复游戏配置文件

### 核心功能

- **一键备份**: 自动时间戳命名，存储在 exe 目录下的 `backups/`
- **配置预览**: 显示分辨率、灵敏度、FOV、音量等关键参数
- **一键导入**: 覆盖模式，导入前自动备份当前配置（保护机制）
- **备份管理**: 卡片式列表，支持右键导出、删除
- **Steam 启动参数**: 预设库（竞技/低端/录制/自定义），自动同步到 Steam

### 屏幕准心

- 5 种准心样式：十字、圆点、圆圈、十字+圆点、自定义图片
- 实时调节：大小、粗细、间距、透明度
- 预设管理：保存/加载/重命名/删除准心方案
- 自定义导入：支持 PNG 图片自动裁剪
- 独立面板：从主窗口右侧展开，可收起/关闭

### 新手教程

- 3 步引导：备份 → 导出 → 打包
- 每步 5 秒自动推进，不阻塞用户
- 气泡提示带步骤编号 (1/3)、(2/3)、(3/3)
- 状态持久化，不会重复显示

### 打包带走

- 将程序和所有备份打包成 ZIP
- 方便在新电脑上直接使用
- 进度窗口显示打包状态

### 其他特性

- 深色主题 UI
- 日志系统（自动记录错误，支持导出）
- 管理员权限自动提权
- 单文件发布，便携免安装

---

## 项目结构

```
ApexSyncTool/
├── Core/                          # 核心业务逻辑
│   ├── ApexPathManager.cs         # Apex 游戏路径检测
│   ├── BackupManager.cs           # 备份/恢复/管理
│   ├── ConfigParser.cs            # 配置文件解析（settings.cfg/videoconfig.txt）
│   ├── CrosshairPresetManager.cs  # 准心预设管理（JSON 源生成序列化）
│   ├── LaunchParameterFormatter.cs# Steam 启动参数格式化
│   ├── Logger.cs                  # 日志系统
│   ├── SteamAccountManager.cs     # Steam 账号管理
│   └── SteamManager.cs            # Steam localconfig.vdf 读写
├── UI/                            # 自定义控件和对话框
│   ├── BackupCard.cs              # 备份卡片控件
│   ├── CrosshairForm.cs           # 准心配置面板（450px 右侧停靠）
│   ├── CrosshairOverlayForm.cs    # 准心透明覆盖层（点击穿透）
│   ├── CrosshairPresetCard.cs     # 准心预设卡片（预览+右键菜单）
│   ├── PackProgressForm.cs        # 打包进度对话框
│   └── TutorialHelper.cs          # 新手教程气泡（5秒自动推进）
├── MainForm.cs                    # 主窗口逻辑
├── MainForm.Designer.cs           # 主窗口设计器
├── MainForm.resx                  # 资源文件
├── Program.cs                     # 程序入口
├── ApexSyncTool.csproj            # 项目配置（.NET 8.0, 裁剪+压缩）
├── Properties/
│   └── AssemblyInfo.cs            # 程序集信息
├── release/
│   └── ApexSyncTool.exe           # 发布成品（~42MB）
└── README.md                      # 本文件
```

### 运行时数据目录

程序首次运行后会在 exe 同级目录创建：

```
[exe目录]/
├── backups/                       # 游戏设置备份
│   ├── Apex_YYYYMMDD_HHMMSS/    # Apex 备份（含 4 个配置文件）
│   ├── Naraka_YYYYMMDD_HHMMSS/  # 永劫无间备份
│   └── Mycrosshair/               # 准心预设
│       └── 预设名/
│           ├── config.json        # 准心参数（JSON）
│           └── image.png          # 自定义准心图（可选）
├── logs/                          # 日志文件
│   └── ApexSync_YYYY-MM-DD.log
└── tutorial.dat                   # 教程状态（step1/2/3 完成标记）
```

---

## 编译与发布

### 环境要求

- .NET 8.0 SDK 或更高版本
- Windows 10/11（WinForms 依赖）

### 编译（调试）

```powershell
cd ApexSyncTool
dotnet build ApexSyncTool.csproj -c Debug
```

### 发布（Release）

```powershell
dotnet publish ApexSyncTool.csproj -c Release -o release -v q --nologo
```

发布配置（已在 csproj 中设置）：

- `PublishSingleFile=true`: 打包为单个 exe
- `SelfContained=true`: 包含 .NET 运行时，无需目标机安装
- `PublishTrimmed=true`: IL 裁剪，移除未使用的框架代码
- `TrimMode=partial`: 部分裁剪模式（WinForms 兼容）
- `EnableCompressionInSingleFile=true`: 压缩打包内容
- `RuntimeIdentifier=win-x64`: 目标平台

输出文件：`release/ApexSyncTool.exe`（约 42MB）

### 裁剪兼容性

程序使用 `System.Text.Json` 源生成器（`CrosshairConfigJsonContext`）替代反射序列化，确保在 IL 裁剪后正常工作。`JsonDocument.Parse`（用于主配置解析）本身就是裁剪安全的。

---

## 使用说明

### 启动

双击 `ApexSyncTool.exe` 即可运行。首次启动会显示新手教程（3 步引导）。

### 备份游戏设置

1. 点击"一键备份"按钮
2. 自动检测游戏并备份配置文件
3. 备份卡片出现在列表中（按时间倒序）

### 导入备份

1. 点击备份卡片选中
2. 左侧显示配置预览
3. 点击"一键导入"
4. 确认对话框中选择"是"（会自动备份当前配置）

### 右键菜单

- **备份卡片右键**: 导出备份到指定位置
- **准心预设卡片右键**: 重命名、删除预设

### 屏幕准心

1. 点击底部"屏幕准心"按钮
2. 右侧展开准心配置面板
3. 选择样式、调节参数
4. 可保存为预设，下次直接加载

### 打包带走

1. 点击右下角"打包带走"
2. 选择保存位置（ZIP 文件）
3. 程序会将 exe 和所有备份打包
4. 在新电脑上解压即可使用

---

## 技术细节

### 配置文件格式

**settings.cfg / profile.cfg** (Source Engine 键值对):
```
key "value"
bind_key "action" 0/1
```

**videoconfig.txt** (VDF 格式):
```
"VideoConfig" {
  "setting.key" "value"
}
```

**voice_volumes.dat** (二进制):
28 字节，直接复制不解析

### Steam 集成

读取/写入 `localconfig.vdf` 中的 LaunchOptions：
```
"1172470" {  // Apex AppID
  "LaunchOptions" "-dev -high -novid"
}
```

### 准心覆盖层

使用透明窗口（`TransparencyKey=#000001`）+ Win32 扩展样式（`WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE`）实现点击穿透，保持置顶。

### JSON 序列化

准心预设使用源生成器（`CrosshairConfigJsonContext`）：
```csharp
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(CrosshairConfig))]
internal partial class CrosshairConfigJsonContext : JsonSerializerContext { }
```

调用：
```csharp
JsonSerializer.Serialize(cfg, CrosshairConfigJsonContext.Default.CrosshairConfig);
JsonSerializer.Deserialize(json, CrosshairConfigJsonContext.Default.CrosshairConfig);
```

---

## 已知限制

1. **硬件兼容性**: 不同硬件的显卡配置可能不兼容（游戏本身的限制）
2. **Steam 账号**: 不同 Steam 账号的 LaunchOptions 独立
3. **voice_volumes.dat**: 恒为 28 字节，不包含详细音量数据
4. **准心覆盖层**: 在某些全屏独占模式下可能不显示（窗口化/无边框窗口正常）
5. **首次启动**: 启用压缩后首次启动多 1-2 秒解压时间

---

## 常见问题

**Q: 为什么导入失败?**  
A: 检查 Steam/游戏是否运行中，检查是否有管理员权限。查看 `logs/` 目录的日志文件了解具体错误。

**Q: 可以跨账号使用吗?**  
A: 可以。应用自动检测当前 Windows 用户名，每个用户有独立的配置位置。

**Q: 打包后的 ZIP 在新电脑上怎么用?**  
A: 解压后直接运行 `ApexSyncTool.exe`，所有备份和准心预设都在 `backups/` 目录下，可以直接导入。

**Q: 准心预设保存在哪里?**  
A: `backups/Mycrosshair/预设名/` 下，包含 `config.json`（参数）和可选的 `image.png`（自定义图）。

**Q: 教程可以重新看吗?**  
A: 删除 exe 目录下的 `tutorial.dat` 文件，下次启动会重新显示教程。

---

## 版本历史

### v2.0.0 (2026-07-21)

- 新增永劫无间支持
- 新增屏幕准心功能（5 种样式、预设管理、自定义图片）
- 新增新手教程（3 步引导，5 秒自动推进）
- 新增"打包带走"功能（一键打包程序和备份）
- 优化发布体积（146MB → 42MB，启用 IL 裁剪 + 压缩）
- 修复 WinForms 裁剪后 UI Automation 崩溃问题
- JSON 序列化改用源生成器（裁剪安全）

### v1.0.0 (2026-07-20)

- 初始版本（MVP）
- Apex Legends 设置备份/恢复
- Steam 启动参数同步
- 配置预览

---

## 反馈与支持

- 日志位置: `logs/` 目录
- 备份位置: `backups/` 目录
- 问题诊断: 导出日志文件查看详细错误

---

**开发状态**: v2.0.0 完成  
**最后更新**: 2026-07-21
