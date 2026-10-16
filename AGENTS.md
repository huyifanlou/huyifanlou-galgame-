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

## 工程速查

```bash
# 资产：全 C# 程序化生成（运行时创建 + 编辑器对拍导出）；JS 工具已退役到 tools/legacy-js/（见 docs/06）
# 对拍导出贴图 / 世界数据 JSON：
"$EDITOR" -batchmode -quit -projectPath "$(pwd)/Game" -executeMethod TextureDump.DumpBatch
"$EDITOR" -batchmode -quit -projectPath "$(pwd)/Game" -executeMethod WorldValidator.DumpJsonBatch
# 布点/密度校验（贴地密度基准 160px/米，见 docs/09、docs/10）：
"$EDITOR" -batchmode -quit -projectPath "$(pwd)/Game" -executeMethod WorldValidator.RunBatch

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

## 文档索引

- `docs/00-立项与迁移说明.md` —— 引擎选择（含中国网络实测）、冻结策略、迁移策略
- `docs/01-迁移中修复的问题.md` —— BUG-001（lcgRand 忽略参数）根因与修复
- `docs/02-移植进度与运行手册.md` —— 进度表、批量命令、web→Unity 参数对照
- `docs/03`~`docs/10` —— 首屏上线 / 世界扩容 / 布局重整 / 单一语言（JS 退役） / 贴图生成器移植与对拍 / 美化后处理 / 像素密度收尾①②
- 世界数值（建筑/植被/路灯/交互点）：`Game/Assets/Scripts/CampusData.cs`（C# 单一来源；web 期的 `assets/world.json` 只作对拍参考）
