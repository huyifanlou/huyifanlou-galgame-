# 记忆索引 — 校园叙事 RPG（Unity 版）

> 记忆库根：本仓库 `.zcode/memory/`；快照在 `handoffs/`，历史在 `archive/`。

## 活跃任务（≤5）

- **小镇世界**（已完成 30+ 轮迭代，docs/18~33）：260→620m 地面+缓丘+密林+远景山脊+铁路隧道收口；46 栋建筑、三桥、游乐场、学校矮墙、室内系统（家/教学楼可进）
- **室内系统**（docs/34）：家 8 空间 + 教学楼（教室/走廊/楼梯），AABB 分层碰撞 + 楼梯插值；窄房间镜头贴脸待"娃娃屋俯视"解决
- 玩法系统：迭代 2 日程经营 + 迭代 3 剧情/LLM 导演（比赛截止 2026-11-18）
- 成片/部署/技术文档（5-8 分钟视频含 ≥2min AI 协同、WebGL 部署 + README、PDF ≤30 页）

## 下一步（≤3）

- 玩法系统迭代 2：日程经营（属性/行动闭环 + 主线/日常入口，需录屏）——距截止只剩一个月，优先级最高
- 室内补量：其余可进建筑 + 家具交互物 + 娃娃屋俯视镜头
- 立面分块冲 360px/米（撞 2048 上限的分块方案）

## 最近快照

- [Unity版像素密度统一与待办](handoffs/Unity版像素密度统一与待办.memo.md) — 2026-10-16（较旧；新进度看 docs/30~34 与 git log）

## 分层文件

- [project.md](project.md) — 项目状态与关键约定（本次建立）
- facts.md / decisions.md / lessons.md — 本次未拆分，内容见快照 Done/Decisions/Pitfalls 三节

## 外部记忆

- ZCode CLI 项目记忆：`C:\Users\ASUSA\.zcode\cli\memories\projects\unity-galgame-043cc28e5fb8e038\memory\`（跨工作区通用条目）
