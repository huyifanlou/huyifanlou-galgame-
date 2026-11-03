# AGENTS.md —— 本工作区（Unity开发galgame）开发约定

## 定位

校园 × 恋爱 × 悬疑 × 奇幻题材的 **2.5D 像素风叙事游戏（HD-2D）** Unity 版。
- 本仓库（`Unity开发galgame`）是**唯一继续开发线**。
- web 版（Three.js）已冻结：仓库 `chuanzhibei-game`，tag `web-v1.0-frozen-20261015`（v0.40）——只作历史与资产配方来源，不在其上继续开发。

## 硬约定

- **浏览器验收一律使用本机 Edge**：打开页面 `start msedge <url>`；无头截图 `msedge --headless=new --enable-unsafe-swiftshader ...`。不使用 ZCode 内置浏览器。
- **一切改动留痕**：docs/ 下按编号写说明（做了什么/为什么/验证方式），git 提交用中文 conventional 风格（`feat(模块): ...`）。
- **每轮优化做完就把游戏打开给作者看**：构建完立刻后台启动 `Game/Build/Win/Campus.exe`（不带 `-shot`，保持运行），
  并在回复里指明建议查看的地点与键位；已有旧实例先 `taskkill //IM Campus.exe //F` 再开（旧进程持有的是旧构建）。
  静态对比图只是附件，不能代替实机。
- **需要操作本机软件时直接动手**（CLI 优先），需要提权时提前说明"将弹 UAC 请点是"。
- 剧本双版本保留：`D:\AI\galgame-原版.docx` / `D:\AI\galgame-美化版.docx`（不合并、不改名）。
- 游戏模式：**主线推进剧情 + 支线日常，玩家自选**。
- **新会话先读 `docs/89-交接说明.md`** —— 当前进度、各轮踩坑、未处理项与下一步优先级都在那一页。

## 工程速查

```bash
# 资产：全 C# 程序化生成（运行时创建 + 编辑器对拍导出）；JS 工具已退役到 tools/legacy-js/（见 docs/06）
# 对拍导出贴图 / 世界数据 JSON：
"$EDITOR" -batchmode -quit -projectPath "$(pwd)/Game" -executeMethod TextureDump.DumpBatch
"$EDITOR" -batchmode -quit -projectPath "$(pwd)/Game" -executeMethod WorldValidator.DumpJsonBatch
# 布点/密度校验（跑的是游戏默认构建的当前世界 TownData；贴地密度基准 160px/米，见 docs/09、docs/10）：
"$EDITOR" -batchmode -quit -projectPath "$(pwd)/Game" -executeMethod WorldValidator.RunBatch
# 小镇总平面四条净距规则（建筑↔道路/步道/河道/建筑，docs/18）：
"$EDITOR" -batchmode -quit -projectPath "$(pwd)/Game" -executeMethod WorldValidator.TownBatch
# 旧校园专用数据集（只有 -campus 启动才用）：WorldValidator.RunCampus

# 编辑器（Unity 2022.3.62f3c1 中国版；许可证需 id.unity.cn 账号登录 Hub 一次）
EDITOR="D:/AI/Unity/Editor/2022.3.62f3c1/Editor/Unity.exe"

# 首次初始化：贴图导入设置 + 生成 Main 场景
"$EDITOR" -batchmode -quit -projectPath "$(pwd)/Game" -executeMethod Bootstrap.SetupProjectBatch

# 构建 Windows 播放器 / WebGL
"$EDITOR" -batchmode -quit -projectPath "$(pwd)/Game" -executeMethod Bootstrap.BuildWindowsBatch
"$EDITOR" -batchmode -quit -projectPath "$(pwd)/Game" -executeMethod Bootstrap.BuildWebGLBatch

# 截图验收（与 web 版 URL 参数一一对应）
Game/Build/Win/Campus.exe -shot shots/x.png -time 18.5 -px 0 -pz 4 -at -21,7.5 -freeze 1
```

## 室内系统速查（必读：`docs/89-交接说明.md`、`docs/35`、`docs/34`）

现有 **13 间**可进入室内（家 / 教学楼 / 图书馆 / 餐厅 / 超市 / 食堂 / 宿舍楼 / 礼堂 / 实验楼 /
奶茶店 / 照相馆 / 书店 / 便利店），程序化建的封闭盒子在世界外 x≈900 起的一片独立坐标区。
入口按**交互点名字**映射（`InteriorManager.ToId`），改建筑名会静默断掉入口。改这块之前先看 docs/89 第五节，几个硬约定：

- 碰撞是**自写 AABB + 楼层掩码**（不是物理引擎）；地面高度是函数 `FloorAt`；每个实体四周有 0.32m 垫且**无高度层**（所以椅子目前坐不进去）。
  **楼梯两侧必须有挡墙**，否则侧面走出去会瞬间跳层。
- 墙面用 `RoomSkin(rect, 材质, 门洞...)` 贴 4cm 薄片（不参与碰撞）。
  **门洞坐标规则**：墙号 0/1（沿 X）用世界 X；墙号 2/3（沿 Z）用世界 Z，**不要加 ox**。
- **吊牌/吊灯接顶走 `HangRods(..., ceil)`**（吊顶高度由 `CeilingSlab()` 记进 `_ceilY`，`MAX_ROD=2.8`），不要写死杆长。
- **朝向约定**：`Chair` 靠背在局部 −Z，`ry=0` = 坐者面向 +Z；桌在南→0、北→180、西→90、东→270。
- 室内相机 `dist 5.2 / pitch 0.26`，贴墙时自动抬高俯视；第一人称 `V` 切换，鼠标直接转视角。

验收（比截图可靠）：

```bash
Campus.exe -indoor home|school [-floor 1] [-px X -pz Z]     # 直接开在室内
Campus.exe -indoor home -px 896 -pz -2.5 -at 890,-2.5 -fp 1 -pitch -0.15 -shot out.png
Campus.exe -indoor home -px 899 -pz -4.6 -at 899,6 -sim w,200 -frames 240   # 模拟走，读 [Sim] 坐标
```

建完室内自动跑 `Validate()`，`Player.log` 里应看到「校验完成：N 个碰撞盒，问题 0 处」。

## 文档索引

- `docs/00-立项与迁移说明.md` —— 引擎选择（含中国网络实测）、冻结策略、迁移策略
- `docs/01-迁移中修复的问题.md` —— BUG-001（lcgRand 忽略参数）根因与修复
- `docs/02-移植进度与运行手册.md` —— 进度表、批量命令、web→Unity 参数对照
- `docs/03`~`docs/10` —— 首屏上线 / 世界扩容 / 布局重整 / 单一语言（JS 退役） / 贴图生成器移植与对拍 / 美化后处理 / 像素密度收尾①②
- `docs/11`~`docs/88` —— 材质写实化 / 物理与流动水 / 世界边界 / 光照与写实渲染 / 校园与小镇总平面多轮 / 樱花与巨樱 / 神社区域 / 学校区与校舍 / 室内系统（34~46、82~86） / 住宅区商铺区扩边界与路网路灯联动（87~88）
- **`docs/89-交接说明.md` —— 新会话入口**：当前状态、各轮踩坑、未处理项、下一步优先级
- 世界数值（建筑/道路/植被/路灯/交互点）：**当前默认世界是 `Game/Assets/Scripts/TownData.cs`**（`BOUND=150`，46 栋 / 11 街 / 137 灯）；
  `CampusData.cs` 只在 `-campus` 启动时用。web 期的 `assets/world.json` 只作对拍参考。
