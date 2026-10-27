# 记忆索引 — 校园叙事 RPG（Unity 版）

> 记忆库根：本仓库 `.zcode/memory/`；快照在 `handoffs/`，历史在 `archive/`。

## 活跃任务（≤5）

- **玩法系统迭代 2：日程经营**（属性/行动闭环 + 主线/日常入口，需录屏）——截止 2026-11-18，最高优先；作者 2026-10-26 说先不做，`GameState.cs` 草稿未接线未提交
- 迭代 3：剧情对话 + LLM 动态事件导演
- 室内补量：图书馆/餐厅/超市/食堂已接（docs/43~45），出口净空自动校验已加；其余建筑可进、家具交互物、窗光板、黑板板书
- 主角已换成《我的世界》风方块人（docs/47，替代 docs/46 低模）；NPC 可复用 `PlayerAvatar` 结构
- 成片（5-8min 含 ≥2min AI 协同）+ WebGL 部署 + 技术文档 PDF ≤30 页

## 下一步（≤3）

- 开日程系统：属性面板（精力/心情/学识…）+ 行动点 + 日循环 + 主线/日常入口
- 把家/教学楼/学校/神社等地标接成日程可去地点（已有具名传送 + F 交互）
- 每轮优化后仍要构建并打开 `Game/Build/Win/Campus.exe` 给作者看

## 最近快照

- [小镇世界与单层室内重建完成](handoffs/小镇世界与单层室内重建完成.memo.md) — 2026-10-25（最新，含全部坑与待办）
- [Unity版像素密度统一与待办](handoffs/Unity版像素密度统一与待办.memo.md) — 2026-10-16（已 absorbed）

## 分层文件

- [project.md](project.md) — 项目状态与关键约定（2026-10-27 更新）
- [decisions.md](decisions.md) — 关键决策（引擎/密度铁律/小镇/室内流程）
- [lessons.md](lessons.md) — 坑与经验（渲染/屋顶/室内 z-fight/坐标系）

## 外部记忆

- ZCode CLI 项目记忆：`C:/Users/ASUSA/.zcode/cli/memories/projects/unity-galgame-043cc28e5fb8e038/memory/`
