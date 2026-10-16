# legacy-js —— 已退役的 JS 工具（2026-10-16）

为达成「全项目单一语言（C#）」，全部 JS 工具已退役，功能移植到 Unity 侧：

| 退役文件 | 现位置（C#） | 对拍结论 |
| --- | --- | --- |
| `campus.js`（世界数据与布点生成） | `Game/Assets/Scripts/CampusData.cs` | 20 组数据逐项完全一致 ✓ |
| `validate-world.mjs`（布点校验器） | `Game/Assets/Editor/WorldValidator.cs` | 零违规 ✓ |
| `textures.js`（程序化贴图生成） | `Game/Assets/Scripts/TextureLibrary.cs` | 结构一致，差异仅亚像素抗锯齿级 ✓ |
| `export-assets.mjs`（贴图导出器） | 运行时按需生成（免导出） | — |

**项目现已零 JS/Node 依赖。** `assets/tex/*.png` 保留为画稿存档与对拍参考（不参与构建）。
