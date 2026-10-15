# tools/source —— 资产配方（来自 web 版 v0.40）

| 文件 | 说明 |
| --- | --- |
| `campus.js` | 校园世界数值（单一数据源）：建筑 / 植被 / 路灯 / 交互点……导出为 `assets/world.json` |
| `textures.js` | 程序化像素贴图生成器（浏览器 canvas API）：导出为 `assets/tex/*.png` |

来源：`chuanzhibei-game` 仓库 tag `web-v1.0-frozen-20261015`（v0.40，提交见该仓库 git log）。
导入时间：2026-10-15（引擎迁移，见 `docs/00-立项与迁移说明.md`）。

后续美术调整直接修改这两个文件并重新运行 `node export-assets.mjs`；
导出使用固定随机种子，画稿可复现。web 版仓库保持冻结、不再改动。
