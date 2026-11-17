# 他山之石 · STONES FROM ANOTHER HILL

校园、恋爱、悬疑与奇幻题材的 Unity 交互叙事游戏原型。玩家以第一人称探索程序化小镇，通过立绘对白阅读第一卷八章，也可以完成支线日常。角色采用透明立绘立牌；建筑、家具、植被和天气由 C# 与 Shader 构造。

作者：**李玉川 / 大连东软信息学院 / 个人赛 B组**。参赛仓库：<https://github.com/huyifanlou/huyifanlou-galgame->。游戏项目此前未发表、未获奖。本人早期 Three.js 原型与原作小说是历史基础，Unity 版为当前唯一开发线；具体来源及增量见交付报告。

## 10分钟内体验作品

推荐 Windows 10/11 64位、有 DX11 图形支持的电脑。正式交付包包含 `02_可运行游戏`，解压整个目录后双击其中的 **Campus.exe**。必须保留相邻 `Campus_Data`、`UnityPlayer.dll` 和 `MonoBleedingEdge`，不要只拷贝 exe。首次打开进入开始界面，有档可继续；新游戏从家中开始。普通运行不需要 Unity 编辑器、Python、Node 或大模型 API Key。

建议按以下顺序查看，约5分钟：

1. 开始页点击“设置”，确认字体、按键与返回；开始/继续进入游戏。
2. `B` 开始或继续主线；空格/回车推进，`A` 自动，`S` 快进。`Esc` 打开设置。
3. `L` 进入剧情回顾，选章、搜索台词并定位；退出回顾恢复原进度。
4. `J` 查看主线/支线、追踪/前往及独立重置；重置需要再次确认。
5. `M` 查看地图/传送；`F` 进入或离开建筑；`C` 查看人物并展示/收起立绘立牌。
6. `Y` 调整晴雨雪、强度与颗粒大小；`F6` 存读档，`F5/F9` 快速存/读。

移动为 WASD，Shift 奔跑，鼠标转向；探索时 `G` 前往主线地点并继续。上述为默认键位，完整列表及重绑定在 `Esc/H` 设置页。菜单支持方向键与回车。存档位于 Windows 当前用户 `AppData/LocalLow` 下本作品目录，普通存档与验收存档分离；交付包不附带作者存档。

## 从源码构建

源码快照或仓库根目录应同时保留 `Game/` 与 `Scenario/第一卷-初遇/`。使用 **Unity中国版 2022.3.62f3c1**，安装 Windows Mono 构建支持，并在 Hub 完成许可证登录。版本以 `Game/ProjectSettings/ProjectVersion.txt` 为准，依赖锁在 `Game/Packages/packages-lock.json`。全新电脑的编辑器安装、授权与首次依赖下载不计入10分钟便携体验，源工程首次导入时间受网络和硬盘影响。

在仓库根目录打开 PowerShell：

```powershell
$env:UNITY_EDITOR_PATH = 'D:\AI\Unity\Editor\2022.3.62f3c1\Editor\Unity.exe'
powershell -ExecutionPolicy Bypass -File tools/submission/build-release.ps1
# 成功后打开 Game/Build/Win/Campus.exe
```

脚本先初始化主场景/Shader占位，再构建 Windows 64位。稳定入口是 `Bootstrap.SetupProjectBatch` 与 `Bootstrap.BuildWindowsBatch`。构建自动同步八章剧本、字体、图标和立绘。C# 运行时生成当前世界，不需要已退役的 JS 导出器，也不依赖旧 `assets/world.json`。

本项目无运行时大模型接口，无需 `.env` 密钥。`.env.example` 仅说明可选编辑器路径。AI 辅助创作工具和视频/PDF制作依赖不属于游戏运行依赖；未把个人会话或账号凭据打入工程。

## 复现与验证

```powershell
# 开始/继续/新游戏/设置返回/真实退出，使用独立QA存档
powershell -ExecutionPolicy Bypass -File tools/submission/smoke-test.ps1
# 分别运行以查看指定内容
Game/Build/Win/Campus.exe -scenario 1 -scenline 7
Game/Build/Win/Campus.exe -indoor school
Game/Build/Win/Campus.exe -indoor dorm
Game/Build/Win/Campus.exe -px 54 -pz -49 -at 29,-74.5
```

更多验证参数与实际结果见 `docs/149-省赛作品交付与材料验证.md`；最终视频范围以 `docs/151-先交付材料与最终真人游玩录像要求.md` 为准，历史修复见 `docs/142`、`145`、`148`。开始页设置的首轮测试曾漏验画面可见性，后续保留失败反馈、修复与实际截图，不把状态标志当作视觉验收结论。

## 结构与范围

| 路径 | 用途 |
| --- | --- |
| `Game/Assets/Scripts/TownData.cs` | 默认小镇数据；`CampusData.cs` 是旧校区专用集 |
| `Game/Assets/Scripts/`、`Shaders/` | 程序化世界、室内、剧情、UI、存档与渲染 |
| `Game/Assets/Resources/Portraits/`、`Fonts/`、`Title/` | 当前立绘、随包字体、开始背景 |
| `Scenario/第一卷-初遇/` | 第一卷八章正文；构建期同步到 StreamingAssets |
| `docs/89-交接说明.md` | 长期迭代与踩坑记录；早期条目可能被后续决策取代 |
| `tools/submission/` | 构建、便携验收与本次交付制作脚本 |

当前有13间可进入室内、八章线性主线、支线/追踪/传送/回顾与存读档。游乐场暂封闭。没有把已退役的 MMD/角色3D、完整分支好感系统、游戏全程配音或3A级画质当作已完成功能。视频旁白是系统TTS说明，不是游戏角色配音。

## AI协作、来源与交付

AI 用于反馈拆解、C#/Shader实现、验证建议以及作者筛选后的视觉资产。仓库 `Submission/AI协作记录`（交付包04目录）含三条真实可见消息链、时间戳、来源指纹和相应代码/日志，明确区分原话与总结；不伪造历史。2026-11-17作者已授权按模块提交推送当前源码；历史180条提交保留，不伪造过去日期。作品说明与验收材料在 `Submission/`，最终实录视频待补。

本工程未设置对全项目的一揽子开源许可。源码、原作、AI辅助资产与参考资料的来源和权利范围应分别核对；随包 Noto 字体保留 OFL。Unity/第三方包按各自许可使用。早期 Web 原型已冻结，不应继续用于构建本次作品。
