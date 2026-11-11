# 97 - NPC 修订轮：镜荧红瞳/侧扫刘海/礼服细节/穿模修复，景楠立领夹克

## 一、背景

- docs/96 实装后作者看实机给出六条校正（原话）：
  「女主是吸血鬼的红色眼睛，可以放大一点，衣服细节不够，不要齐刘海，长发两边穿模了。
  不需要所有人都穿校服，主要人物要多样化」。
- 按「先出样张再铺开」约定出第二轮样张（同主题最多两轮，本轮即第二轮）：
  - `shots/97-jingying-rev-preview.html / .png`：刘海 A 中分八字 / B 侧扫斜刘海；红眼放大、礼服细节、背后蝴蝶结同图示出。
  - `shots/97-jingnan-outfit-preview.html / .png`：A 衬衫+针织背心+领带 / B 蓝灰立领夹克+白内搭。
  - 作者选定：**镜荧刘海 = A 中分八字**；**景楠服装 = B 立领夹克**。
- 由此解锁本轮实装：镜荧红眼+中分+礼服细节+穿模修复；景楠脱离校服换独立夹克。

## 二、做了什么

全部改动集中在 `Game/Assets/Scripts/PlayerAvatar.cs`（数据预设 + 几何分支 + 脸贴图），
动画/碰撞/第一人称逻辑未动。

### 1. 镜荧修订（CharacterDef.JingYing + FaceTextureVampire + dress 分支）

- **红瞳（非红眼）**：虹膜改红系 `iris #a8323e / irisHi #f2c8cc / pupil #5a1220`；
  **纯白眼白 `#ffffff` 铺满整个 2×2 格**衬出眼睛轮廓、中央红虹膜 8×8（同他人虹膜大小）+ 瞳孔/高光 2×2
  （作者三次澄清「是红瞳不是红眼」——前两版把整眼/整虹膜列铺红被否；眼白曾用近肤色 `#f7f2ec` 轮廓不清，改纯白）；
  占格同他人、整体上移 4px（y20~36）。
- **侧扫斜刘海（样张 B）**：作者看过中分八字实机后改口「换个发型」，落到第二轮样张的 B 版——
  左厚右薄一条斜线（y48 排发 x0~40 / y40 排发 x0~24，斜边深色收边），露右侧额头；
  不画眉保童稚感；小嘴 6px + 嘴角两颗 2×2 虎牙保留。
- **礼服细节**（dress 分支新增块）：蕾丝领 `laceCollar`（`lace #f4f2ec`）、
  胸针银托 `broochSet` + **红宝石** `broochGem`（直接取 iris 色，与眼睛呼应）、银扣 ×2、
  两侧公主线 `seamL/R`、腰封 `sash` + 腰封结 `sashKnot` + 双飘带 `sashTail1/2`
  （`BlockR` euler X −2.86° 贴裙摆斜面）、双层裙摆 `skirtTier`（银边 Taper，尺寸略大于裙面避免共面闪面）、
  裙边蓝 `skirtHem`、**后背蝴蝶结** `bowL/R + bowKnot + bowTailL/R`、袖口蕾丝 `cuffLace`。
- `CharacterDef` 新增 `lace / accentD` 两色字段供上述细节取色。

### 2. 长发两侧穿模修复（两步）

- **根因**：旧 long 侧发壳 depth 6.2–6.8、中心 z−0.2（跨 z−3.3..3.2），与手臂体积（z±2）在肩以下相交 → 侧视发片切进袖子。
- **第一步**：垂缕前移到胸前一层 `sideZ=2.7`（跨 z2.1–3.4）、depth 收窄 1.2–1.4，与臂前缘 z2.0 脱开。
  实机侧视发现副作用：**头侧失去覆盖、露出一大块皮肤**（垂缕已不罩头侧）。
- **第二步**：long 增加 `sideHeadL/R` 头侧发壳（与短发同 footprint：x±4.05、z−0.2、depth 6.2/6.8，
  但只罩头段 y−3.4..+4）；垂缕同时内收到 `sideX=±3.5`——**臂内缘在 x=4**，摆臂绕 X 轴不改变 x，
  垂缕全程 x≤3.975 故走/跑摆臂都扫不到。短发路径字面量不变（樊楼/景楠回归零差异）。

### 3. 景楠立领夹克（outfit 新增第三值 jacket）

- `outfit` 由 sailor|dress 扩为 **sailor|dress|jacket**；景楠预设改 `outfit="jacket"`。
- 夹克分支：蓝灰主体 `coat #2f3e46` + 深色 `coatD #22303a` 立领/下摆/背缝，
  白内搭 T 领 `teeNotch/teeV`、银拉链 `zip`、双口袋盖 `pocketFlapL/R` + 侧袋线、
  袖同色 + 袖口袢带银扣 `cuffStrap/strapButton`；裤 `#26304a` + **白运动鞋** `#d8d8d8`。
- 与水手校服完全切分：三人三套（樊楼水手服 / 镜荧晚礼服 / 景楠立领夹克），落实「主要人物服装多样化」。

### 4. 实机查看：C 键循环切换人物（作者「没有切换人物方便我查看的功能」）

- `PlayerAvatar.Cycle()`：销毁旧 `_model` 子树、按 樊楼→镜荧→景楠→樊楼 顺序换 `CharacterDef` 重建；
  `CharacterDef` 新增 `id / label` 供顺序与提示取名字。
- `PlayerRig` 监听 **C 键** 调 `Cycle()` 并 `ShowToast("人物模型：×××（C 切换）")`；Hud 键位行加「C 人物」。
- 验收钩子 `-cycle`：启动即切一次（配合 `-avatar` 可证明切换链路），`Cycle` 内 `Debug.Log("[Avatar] cycle → …")` 留痕。
- 不重启进程、不动碰撞/相机；第一人称隐藏逻辑下一帧自动套用到新模型。

## 三、验证

- 构建：`Bootstrap.BuildWindowsBatch` 两次均 Succeeded（`shots/build97.log`、`shots/build97b.log`，后者含头侧发壳第二步修复）。
- 实机截图（`-time 12 -px 4 -pz 4 -at 4,-12 -freeze 1 -nohud`，全身 `-dist 3.4 -pitch 0.3`、近景 `-dist 2.4 -pitch 0.35`）：
  - 镜荧：`shots/97-jingying-{front,back,side,face}.png`（第二步修复后重拍）
    —— 近景红瞳（眼白留白、中央红虹膜）+侧扫斜刘海+虎牙；正面礼服细节逐件可见（蕾丝领/红宝石/银扣/腰封结飘带/双层裙边）；
    背面整片发壳垂腰+蝴蝶结露在发下；**侧视头侧有发壳覆盖、垂缕在胸前与手臂零相交**。
  - 景楠：`shots/97-jingnan-{front,back,side,face}.png` —— 立领+白 T 领+银拉链+口袋+袖袢扣+白鞋，非校服。
  - 樊楼回归：`shots/97-fanlou-{front,face}.png` —— 与 docs/55 定稿逐件一致（金发壳/蓝眼/水手领/红领巾/校徽/腰带）。
- 运行时：`Player.log`（`%USERPROFILE%/AppData/LocalLow/huyifanlou/他山之石/`）无异常栈。
- 切换链路：`-avatar jingying -cycle` 启动切到景楠，`shots/97-cycle-proof.png` 为景楠夹克，
  `Player.log` 有 `[Avatar] cycle → 景楠`；实机按 C 循环三人并弹 toast。
- 图标无需同步：md5 核对 `Game/Assets/icon.png` == IC-3 夜街图（不含脸），改眼睛/刘海不影响图标。

## 四、教训

- 修穿模不能只把相交件挪走：挪完要回看**它原来负责的覆盖**丢没丢（本轮垂缕前移后头侧秃了一块，靠头侧发壳补回）。
- 垂缕/飘带类悬垂件与摆臂的安全距离按**臂内缘 x=4** 留：绕 X 摆臂不改变 x，收进 x<4 即全程不相交。
- 腰封飘带贴裙面用 `BlockR` 欧拉角等于裙摆斜率（−2.86°），比竖直盒子少悬空感。
- 层叠裙边/贴片尺寸略大于底面（skirtTier）避免共面闪面；同色相交并集无视觉问题，异色共面才需躲。
- 样张两轮封顶：第二轮就把作者六条校正全部画进样张再挑编号，避免实装后反复返工。

## 五、未处理 / 下一步

- C 阶段：莉莉雅 / 瑶清寒 / 紫薇 各 2 版概念稿 → 作者挑 → 实装（沿用本轮多样化规则，不做校服换皮）。
- D 阶段：群像模板（同学甲~癸 / 士兵 / 代表等，按调色板换装复用）。
- E 阶段：NPC 布点与生成（交互点 / 剧情触发挂钩），`-avatar` 钩子为其验收入口。

## 六、第三轮：否掉侧扫版 → 回退 v4 → 找参考重做（v6 提案，待作者确认）

- 作者看侧扫+红瞳实机后原话：「还是不行，不如上一版，你去找参考重做吧」。
  「上一版」= **v4：中分八字刘海 + 抬高红眼（眼白 `#f7f2ec`、2×2 格、y20~36）**。
- **已回退**：`FaceTextureVampire` 恢复 v4 的中分八字刘海块与抬高红眼块，`JingYing.eyeW` 回 `#f7f2ec`；
  重建 `shots/build97g.log`（Succeeded）并 `taskkill` 旧实例后后台开 `Campus.exe -avatar jingying -time 12`，
  实机当前即 v4 基线（第二~五节描述的侧扫/纯白眼白为被否的 v5，仅作历史）。
- **找参考**：`shots/97-refs/ref1-silver-red.jpg`、`ref2-huashi.jpg`（银发红瞳吸血鬼动漫肖像）。
  提炼眼睛画法＝深色上睫毛线 + 整片红虹膜 + 中央暗瞳 + 单点白高光（两眼同光源）+ 细眼白角 + 浅红下睑。
  LittleSkin 列表 API 可用（`/skinlib/list?keyword=…` 返回 JSON），但**原始贴图端点需登录会话**（未登录 404），
  故本轮参考以动漫肖像为准，未整包搬 MC 皮肤。
- **v6 实装**：`shots/97-refs/face_v6.py` 生成 `97-face-v6.png` 与对照 `97-face-v4-vs-v6.png`；
  相对 v4：加 maroon 上睫毛线 `#5a2a30`、虹膜整片红+暗瞳+左上白高光点、眼白只留两侧细角（`eyeW` 改纯白 `#ffffff`）、
  下睑浅红、眼睛对齐 8px 网格（y24~39）、鼻影缩小。
  作者看样张后补一条「**额头露太多**」→ 刘海收窄：row1 中缝 8px（x28~35）、row2 中缝 16px（x24~39），
  两侧发壳压到眼线上方，额头只留一条中分窄缝；刘海仍中分八字。
  已把上述矩形块移植进 `FaceTextureVampire`（含 `foreach ex in {8,40}` 画眼），重建 `shots/build97h.log`。
- **参考①留作对话立绘**：作者「参考1的图不错，可以留着做对话的时候用」→
  存 `Game/Assets/Resources/Portraits/jingying-dialogue.jpg`（即 `shots/97-refs/ref1-silver-red.jpg`），
  供后续对话 UI 以 `Resources.Load<Texture2D>("Portraits/jingying-dialogue")` 取用；本轮只入库不接 UI。
- **下一步**：实机验收 v6 脸（近景+正/背/侧）；对话 UI 立项时接上述立绘。遵守「换头先出图确认」——本轮样张已给作者并按其「额头露太多」修正后实装。