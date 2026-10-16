# legacy-js —— 已退役的 JS 工具（2026-10-16）

为达成「全项目单一语言（C#）」，以下 JS 工具已退役，功能整体移植到 Unity 编辑器侧：

| 退役文件 | 现位置（C#） |
| --- | --- |
| `campus.js`（世界数据与布点生成） | `Game/Assets/Scripts/CampusData.cs`（同种子同算法，布局一致） |
| `validate-world.mjs`（布点校验器） | `Game/Assets/Editor/WorldValidator.cs`（菜单「Campus/校验世界布点」） |

仍在使用：`source/textures.js` + `export-assets.mjs`（程序化贴图生成，下一阶段移植到 C# 后一并退役）。
