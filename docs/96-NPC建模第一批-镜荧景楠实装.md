# 96 - NPC 建模第一批：镜荧 / 景楠 实装（CharacterDef 数据驱动）

## 一、背景

- 作者指令：「读取记忆进行下一步，把其他人物和 npc 建模搞起来」。
  此前世界里只有男主樊楼一个方块人，NPC 为零。
- 按记忆约定走「先出皮肤图样张 → 作者挑编号 → 再进 Unity」流程：
  - 第一批样张：镜荧、景楠（第一卷说话量前二且外貌已定：镜荧银发蓝眼尖牙、景楠黑发整齐短发）。
  - 样张：`shots/96-jingying-preview.png`、`shots/96-jingnan-preview.png`（各 A/B 两版，代码原生 Canvas 纸娃娃，沿用 docs/55 版式）。
  - 作者选定：**镜荧 = A 黑礼服+蓝腰封**；**景楠 = A 整齐短发**。
- 外貌未定的莉莉雅 / 瑶清寒 / 紫薇 留到 C 阶段各出 2 版概念稿再挑，本轮不做。

## 二、做了什么

### 1. PlayerAvatar 泛化为数据驱动（`Game/Assets/Scripts/PlayerAvatar.cs`）

- 新增 `CharacterDef`：调色板（肤/发三档/眉/眼五档/嘴/服装各色）+
  `outfit`（sailor | dress）+ `hairStyle`（short | long）+ `face`（default | vampire）+ `fangs`。
- 三个预设：
  - `FanLou`：逐字复刻 docs/55 定稿的字面量与几何，**默认路径不变**（回归见下）。
  - `JingNan`：黑发三档 `#23262e/#3a3f4a/#14161c`、棕眼五档、深眉；同款男版水手校服。
  - `JingYing`：银发三档 `#e9edf3/#ffffff/#b9c2d0`、蓝眼；黑礼服 `#1b2438` +
    蓝腰封 `#2f5f92` + 银边 `#b9bec6`；long 发、vampire 脸、fangs。
  - `FromId(id)`：`jingying` / `jingnan` / 默认樊楼。
- 几何分支（共用 Node/Block/Taper/HairVolume 工具，不新增网格类型）：
  - **dress 分支**：礼服主体 8×12×4 + 领口银边 + 胸前银扣 + 蓝腰封 +
    A 字裙摆（Taper 底 11×5 → 顶 8×4、高 10px）+ 裙摆蓝边；袖改礼服色+银袖口；
    腿改深色连裤袜 5px + 黑鞋（裙摆以下不露肤）。
  - **long 发分支**：侧发壳 / 后发壳底点从 −3.4px 下移到 −16px、高 7.4→20px，
    垂到腰；底边收边同步下移。仍是完整发壳，不做零散发束（docs/55 教训）。
  - **vampire 脸**（`FaceTextureVampire`）：刘海整片下压一排及眉、不画眉（童稚感）、
    大蓝眼（高光 4×4）、小嘴 6px、嘴角两颗 2×2 白虎牙。
- 动画 / 碰撞 / 第一人称隐藏逻辑一律未动；礼服与长发只加视觉块。

### 2. 验收钩子 `-avatar`

- `WorldBuilder.PlayerRigObjects()`：`PlayerAvatar.Create(grp, CharacterDef.FromId(Args.Str("-avatar")))`。
- 不带 `-avatar` = 樊楼，与旧构建等价；带 `-avatar jingying|jingnan` 把玩家模型换成对应角色，
  复用现有相机参数（`-facing/-dist/-pitch/-freeze/-shot/-time`）拍验收图。
- 本轮只做「模型实装 + 验收钩子」；NPC 布点 / 生成系统留到 E 阶段。

## 三、验证

- 构建：`Bootstrap.BuildWindowsBatch` → Succeeded（`shots/build96.log`）。
- 实机截图（`-time 12 -px 4 -pz 4 -at 4,-12 -freeze 1 -nohud`，
  全身 `-dist 3.4 -pitch 0.3`、近景 `-dist 2.4 -pitch 0.35`）：
  - 樊楼回归：`shots/96-fanlou-front.png`、`shots/96-fanlou-face.png`
    —— 与 `shots/55-shell-front3.png` / `55-shell-face3.png` 对比：发壳轮廓、脸布局
    （眉/蓝眼/小嘴）、水手领/红领巾/校徽/腰带逐件一致；差异仅 HUD 与旧版镇景背景。
  - 镜荧：`shots/96-jingying-{front,back,side,face}.png`
    —— 正面银长发垂腰+黑礼服+蓝腰封+银袖口；背面整片发壳收边干净；
    侧面发壳在臂后垂到腰、无稀疏缝；近景刘海及眉、大蓝眼、嘴角虎牙白点。
  - 景楠：`shots/96-jingnan-{front,back,side,face}.png`
    —— 黑发整齐短发壳、棕眼、同款水手校服。
- 运行时：`Player.log`（`%USERPROFILE%/AppData/LocalLow/huyifanlou/他山之石/`）无异常栈。

## 四、教训

- 泛化角色时把默认预设的字面量原样搬进 `CharacterDef`，再跑一遍旧验收图做回归，
  比「小心不改默认分支」更可靠。
- 长发不要拆束：侧/后壳整体下移即可垂腰，正面看是发框、背面看是整片（沿用 docs/55）。
- 吸血鬼脸的童稚感来自「刘海压到眼上 + 不画眉」，虎牙 2×2 就够，再大就成獠牙。
- 礼服裙摆用 Taper（底宽顶窄）一块成型，比叠盒子少共面闪面。

## 五、未处理 / 下一步

- C 阶段：莉莉雅 / 瑶清寒 / 紫薇 各 2 版概念稿 → 作者挑 → 实装（作者已确认走此流程）。
- D 阶段：群像模板（同学甲~癸 / 士兵 / 代表等，按调色板换装复用）。
- E 阶段：NPC 布点与生成（交互点 / 剧情触发挂钩），本轮的 `-avatar` 钩子为其验收入口。