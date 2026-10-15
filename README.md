# Unity开发galgame

校园 × 恋爱 × 悬疑 × 奇幻题材的 **2.5D 像素风叙事游戏（HD-2D）**——Unity 版工作区。

- 前身：Three.js / Web 版（仓库 `chuanzhibei-game`，冻结基线 tag `web-v1.0-frozen-20261015`，v0.40）
- 本工作区：用 Unity 重建（引擎迁移决策见 `docs/00-立项与迁移说明.md`）
- 参赛：2026 第九届传智杯 · AI 创新应用挑战赛（Vibe Coding 赛道，个人赛 B 组，截止 2026-11-18）

## 目录结构

```
Game/                  Unity 工程（Unity 2022.3.62f3c1 中国版打开）
assets/
  tex/                 从 web 版程序化贴图导出的 PNG（单一来源见 tools/source）
  world.json           校园世界数据（建筑/植被/路灯/交互点，与 web 版同一份数值）
tools/
  source/              web 版 v0.40 的 textures.js / campus.js 快照（资产配方来源）
  export-assets.mjs    导出器：一条命令重建 assets/ 全部内容
docs/                  立项与迁移说明、迭代文档
```

## 复现资产

```bash
cd tools
npm install
node export-assets.mjs      # 重新生成 assets/tex/*.png 与 assets/world.json
```

导出使用固定随机种子（20261015），画稿每次生成完全一致（web 版原为每次加载随机）。

## 打开工程

Unity Hub → Add → 选择本目录下的 `Game/`（编辑器版本 2022.3.62f3c1 中国版）。

## 与 web 版的对应关系

| 项目 | web 版（冻结） | Unity 版（本工作区） |
| --- | --- | --- |
| 程序化贴图 | `src/game/textures.js` 运行时 canvas 生成 | `tools/source/textures.js` 一次性导出 PNG |
| 世界数据 | `src/game/campus.js` | `assets/world.json`（同数值） |
| 场景配方 | `src/game/CampusScene.jsx` | `Game/Assets/Scripts/*.cs`（逐条对位） |
| 截图验收 | 无头 Edge + `?time=&at=&freeze=` | ShotMode 命令行参数 `-shot -time -at -freeze` |

## 运维约定（沿用）

- 剧本双版本保留：`D:\AI\galgame-原版.docx` / `D:\AI\galgame-美化版.docx`
- 游戏模式：主线推进剧情 + 支线日常，玩家自选
- 一切改动留痕（git 提交 + 说明文档）；浏览器验收一律用本机 Edge
