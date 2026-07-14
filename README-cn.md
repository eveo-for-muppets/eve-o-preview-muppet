# EVE-O Preview — 个人分支

[![English](https://img.shields.io/badge/lang-English-red.svg)](README.md)
[![简体中文](https://img.shields.io/badge/lang-简体中文-yellow.svg)](README-cn.md)

这是 [EVE-O Preview](https://github.com/Proopai/eve-o-preview) 的个人 Windows 分支。它保留了原版的客户端缩略图和快速切换功能，并增加裁剪预设、批量角色分配、改进的循环组、可配置叠加文字、本地日志状态以及更多本地化内容。

本项目是非官方独立分支，不受 CCP Games 或上游 EVE-O Preview 维护者认可或支持。

## 重要规则提示

上游 EVE-O Preview 过去获讨论及接受的行为，是以只读方式显示**完整且未经修改的 EVE 客户端画面**。本分支可以只显示选定区域，也可以叠加从本地日志读取的信息。这些功能明显改变了原有行为，不能声称受到过去许可说明的覆盖。

- 使用实验功能前，请自行查阅 CCP 当前规则。
- EVE 客户端裁剪画面可能不属于“完整且未经修改的预览”许可范围。
- 本分支不会广播输入、转发点击、自动操作游戏、进行图像识别或控制 EVE 客户端。
- 用户须自行承担配置和使用责任。

相关资料：[CCP 叠加层说明](https://www.eveonline.com/news/view/overlays-isk-buyer-amnesty-and-account-security)及[上游 EVE-O 讨论](https://forums.eveonline.com/t/eve-o-preview-v5-1-0-able-actor-multi-client-preview-switcher-2021-05-08-limited-linux-support/4202)。

## 下载和文档

- [公开版本](https://github.com/Sussic/eve-o-preview-personal/releases)
- [私人测试指南（英文）](README-TESTING.md)
- [更新记录（英文）](CHANGELOG.md)
- [上游仓库](https://github.com/Proopai/eve-o-preview)

当前发布的可执行文件未进行代码签名，Windows 可能提示“未知发布者”。请只从可信仓库下载，并将 ZIP 的 SHA-256 与发布页面提供的校验值比较。

## 主要功能

### 原版工作流程

- 为运行中的 EVE 客户端显示实时、只读缩略图。
- 点击缩略图或使用快捷键切换到对应客户端。
- 全局及每角色位置、尺寸、透明度、缩放和显示设置。
- 保存的循环组和角色快捷键。
- 不广播输入，不进行游戏自动化。

### 本分支新增功能

- **裁剪预设：** 拖动选择标准化区域，为预设命名，并供多个角色复用。
- **批量分配：** 筛选大量角色，选择可见或已上线角色，一次分配同一预设。
- **循环组裁剪：** 将已保存的裁剪预设应用到保存组或临时组成员。
- **临时循环组：** 最多五个仅本次会话有效的组，不更改 JSON 中保存的成员。按住数字键 1–5 并点击缩略图即可切换成员。
- **改进的循环组编辑器：** 可见的前进/后退快捷键录制、顺序、筛选和在线状态。
- **可配置叠加文字：** 分别设置角色名和星系名的字体、颜色及全局位置。
- **星系名：** 仅从本地 EVE 日志读取，不需要 ESI 登录或网络访问。
- **近期受伤边框：** 从游戏日志检测到受到伤害时显示可配置边框；此功能仍在测试。
- **本地化：** 可在运行时选择英语、简体中文和西班牙语。不同测试版本的翻译完整度可能不同。
- **旧配置迁移：** 可读取旧版 EVE-O JSON，并为新增字段补充默认值。

## 系统要求

- Windows 10 或 Windows 11，x64。
- EVE 客户端使用固定窗口或窗口模式；不支持独占全屏。
- 自包含发布包不要求另外安装 .NET。
- 从源码构建需要 .NET 8 SDK。

本分支发布包目前不支持或测试 Linux/Wine，新的 DWM 裁剪流程以 Windows 为目标。

## 安装

1. 从 Releases 下载 Windows x64 ZIP 和 SHA-256 文件。
2. 尽可能验证校验值。
3. 将整个 ZIP 解压到可写目录，例如 `Documents\EVE-O-Preview`。
4. 不要直接在 ZIP 内运行，也不要安装到 `Program Files`。
5. 启动前关闭其他 EVE-O Preview；程序只允许一个实例运行。
6. 运行 `EVE-O-Preview.exe`。

请把 `EVE-O-Preview.locale` 与可执行文件放在同一目录。程序会在可执行文件旁保存 `EVE-O-Preview.json`，因此该目录必须可写。

## 更新及旧配置

旧配置应保留快捷键、循环组成员、别名、缩略图位置、尺寸以及其他可识别设置。保存时，程序会为缺少的新字段写入安全默认值。

首次运行测试版前：

1. 关闭 EVE-O Preview。
2. 将 `EVE-O-Preview.json` 复制为带日期的备份。
3. 把新版本解压到单独目录。
4. 将旧 JSON 复制到新目录。
5. 启动并先检查快捷键、循环顺序、缩略图位置和尺寸，再修改设置。

不要把自己的 JSON 放进发布包；其中可能包含角色名、别名、布局和快捷键。

## 裁剪预设

1. 打开 **Crops（裁剪）** 标签页。
2. 选择 **New（新建）** 并命名。
3. 在 **Capture from** 中选择已打开客户端。
4. 选择来源区域并保存。
5. 筛选或批量选择角色，然后应用分配。

裁剪使用标准化坐标保存，比固定像素更能适应分辨率变化。也可以在 **Cycle Groups（循环组）** 中选择预设并应用到当前成员。

## 循环组

**保存组**写入 JSON 并在重启后保留。**临时组**仅在当前 EVE-O 会话存在，并在活动时覆盖保存成员关系。

- 在循环组页设置前进和后退快捷键。
- 在编辑器中调整保存成员顺序。
- 对临时组 1–5，按住相应数字并点击缩略图来切换角色。
- EVE-O 退出后，临时成员会清空。
- 对临时组应用裁剪会改变这些角色的裁剪分配；临时成员关系本身仍不会保存。

## 本地日志功能

本分支不需要 ESI，也不会上传日志。它读取 Windows 当前“文档”目录下的：

```text
Documents\EVE\logs\Chatlogs
Documents\EVE\logs\Gamelogs
```

如果 OneDrive 重定向了文档目录，程序将使用 Windows 返回的实际路径。

### 星系名

在 EVE 中启用聊天日志写入文件，并在 EVE-O 的 **Overlay** 页启用星系名。程序通过日志头的 `Listener` 将日志关联到角色，并读取 Local 频道/星系变更记录。

### 近期受伤边框

在 EVE 中启用战斗消息写入文件，然后在 **Overlay** 页启用近期受伤高亮。当前解析器识别英文日志中带正伤害数值及 `from` 方向的受到伤害记录；不会把对外攻击、维修、零伤害或非战斗通知当作受伤。

可设置边框颜色、粗细和持续时间。该功能仍属实验；报告问题时请勿公开完整私人日志。

## 鼠标操作

| 操作 | 按键 |
| --- | --- |
| 切换到客户端 | 左键点击缩略图 |
| 最小化客户端 | Ctrl + 左键 |
| 切换普通循环排除 | Shift + 左键 |
| 切换临时组 1–5 | 按住 1–5 + 左键 |
| 返回上一个非 EVE 程序 | Ctrl + Shift + 左键 |
| 移动缩略图 | 右键拖动 |
| 调整缩略图尺寸 | 同时按住左右键并拖动 |

## 从源码构建

```powershell
dotnet restore src\Eve-O-Preview\Eve-O-Preview.csproj
dotnet build src\Eve-O-Preview\Eve-O-Preview.csproj --configuration Release -p:EVEOTARGET=Windows
```

开发运行：

```powershell
dotnet run --project src\Eve-O-Preview\Eve-O-Preview.csproj --configuration Debug -p:EVEOTARGET=Windows
```

测试裁剪时请使用仓库内的 EVE-O-Mock。不要提交生成的 JSON、真实 EVE 日志或个人测试资料。

## 报告问题

请提供版本标签、Windows 版本、重现步骤、预期/实际结果、必要截图、构建输出或已脱敏的短日志，以及是否使用旧配置。不要上传完整聊天/游戏日志或未脱敏配置。

## 鸣谢及许可证

本分支基于 EVE-O Preview，并保留上游历史和版权声明。感谢上游维护者及贡献者创建原始项目。

项目使用 [MIT License](LICENSE)。EVE Online 及相关商标归 CCP Games 所有。
