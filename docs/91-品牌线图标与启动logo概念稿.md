# 91 · 品牌线开工：图标 / 游戏名 / 启动 Logo 概念稿

## 追加（同日第六轮）：桌面图标没变的真因与修复

作者反馈「桌面上没变」。排查结论分三层，**都不是构建问题**：

1. **exe / 窗口 / 任务栏的图标其实已经换了**（`ExtractAssociatedIcon` 抽出 Campus.exe 内嵌图标目验 = IC-3）。
2. **桌面快捷方式是独立的**：docs/17 建的 `校园叙事 RPG.lnk` 用 `IconLocation = Game/Build/icon.ico`，
   而那个 .ico 是 10-18 生成的旧图（草地+主角），改名换图标都不会碰它 → 桌面当然不变。
   已重生成 `Game/Build/icon.ico`（16/24/32/48/64/128/256 七档，PNG 压缩条目，Vista+ 全支持），
   源是 `Scenario/brand/ico/ic-*.png`（由 `icon-ic3-export.html?s=N` 逐档截图）。
3. **快捷方式本身也换了**：旧 `校园叙事 RPG.lnk` 删除，新建 `他山之石.lnk`
   （Target=Campus.exe、WorkingDirectory=Build/Win、IconLocation=新 .ico,0）。

**新坑（已踩两次，记牢）**：Windows PowerShell 5.1 把**无 BOM 的 UTF-8 `.ps1` 按 GBK 读**，
脚本里的中文文件名/路径会变成乱码——这次因此建出了一个叫 `浠栧北涔嬬煶.lnk` 的垃圾快捷方式，
且 `Test-Path` 对真实中文名永远返回 false。**含中文的脚本一律用 `pwsh` 跑**（它按 UTF-8 读），或给文件加 BOM。

若桌面仍显示旧图，是 Explorer 图标缓存：先 F5 / 重开资源管理器窗口；仍不行才清缓存
（`ie4uinit.exe -ClearIconCache` 或删 IconCache.db 重启 explorer，后者有打扰，需先问作者）。

**第二个坑（比缓存更致命）**：用 WScript.Shell 建快捷方式时，脚本里的中文路径被写坏
（.lnk 里 TargetPath/IconLocation 存成了带 U+FFFD 的乱码）→ shell 取不到图标、双击也打不开，
症状就是作者那句「不行啊」，且 `ExtractIconEx` 对 .lnk 恒返回 0、看不出问题。
**解法：脚本里一个中文字都不要放**，路径走 8.3 短名（`GetShortPathName` → `…\workspace\UNITY~1\…`），
快捷方式文件名先用 ASCII（`TASHAN.lnk`）建好，再用 Python `os.rename` 改成「他山之石.lnk」。
最终已目验：桌面第 4 行第 4 列「他山之石」显示 IC-3 图标，`TargetPath` / `IconLocation` 均 `Test-Path` 通过。

## 追加（同日第五轮）：名字与图标落地，头像回退到 IC-3


作者看完动漫画风侧脸后改主意：**头像不要了，用之前的 IC-3**。已落地：

| 项 | 改动 |
|---|---|
| 游戏名 | `productName` 「校园叙事 RPG」→「他山之石」（`Bootstrap.cs:81` + `ProjectSettings.asset`） |
| HUD 标题 | `Hud.cs:55` 写死的「（游戏名待定）· 校园叙事 RPG（Unity 版）」→「他山之石」 |
| 应用图标 | 导出 IC-3（夜街·山·一盏灯）256×256 → `Game/Assets/icon.png`，跑 `Bootstrap.SetAppIconBatch`（回读 8 张，首张=icon）后重新构建 |
| exe | **不变**，仍是 `Game/Build/Win/Campus.exe`（输出名写死在 `Bootstrap.cs:178`，与 productName 无关） |
| 日志目录 | 随 productName 变成 `LocalLow/huyifanlou/他山之石/`，docs/89 已同步 |

图标成品：`Scenario/brand/icon-ic3-256.png`（装机用）与 `icon-ic3-1024.png`（母版），
源是 `icon-ic3-export.html`（128 viewBox 矢量，改样式重导即可）。
导出时对 IC-3 做了三处**小尺寸加固**：灯杆从 2.8 加粗到 3.6、灯头加一圈光晕、
远山与街道地平线加实底——32px 与浅色桌面上都验过（`icon-ic3-check.png`）。

旧图标在 `35538b6` 那次提交里，需要可回滚。

**仍未做**：F-1 版式的开场 logo（Unity splash 之后那段 2.6 秒动画，隶书要烘成贴图）。
这是改名换图标之外的第三件事。

## 追加（同日第四轮）：头像改动漫画风侧脸，手绘到 v3


图像生成服务仍 403（`code 112`，返回体带定价页），确认是套餐/额度限制、非网络问题。作者要求继续手绘。
`Scenario/brand/icon-anime-profile.html`（v2 侧脸）→ `Scenario/brand/icon-anime-v3.html`（v3，当前版）：
纯 SVG 贝塞尔 + 赛璐璐二分，六版（E-1 平静 / E-2 眨眼 / E-3 特写 / C-1 圆形裁切 / C-2 反白底 / C-3 纯藏青），
每版 256/128/64/32 四档。v2→v3 的实质改动是**给脸形、发形、衣领加 stroke 线稿**、发束改弧形尖梢、
眼睛分层（上睑投影 + 瞳孔 + 双高光）、补高领与颈饰（血族大小姐的红宝石 choker）、背景加月与星。

三个坑（都值得记）：

1. **`clipPath` 的 id 和页面容器 id 撞名**：模板里用 `id="r${uid}"`，克隆到第 2 个实例时正好是 `id="r2"`，
   而第二节容器就是 `<div id="r2">`；`getElementById('r2')` 按文档顺序返回**先出现的那个**（clipPath），
   于是整节内容被 append 进了一个 clipPath 里，页面空白且**控制台零报错**。改成 `rcp${uid}` / `ccp${uid}` 解决。
2. **截图窗口太矮会伪装成 JS 故障**：第一次以为第二节没渲染，其实是 `--window-size` 高度不够，卡片在视口外。
   先确认高度，再怀疑代码。
3. **删调试代码不能按第一个换行切**：`str.replace` 后我用 `t.find('\n', i)` 截行，切进了字符串字面量里的
   `\n`，把脚本切出 `SyntaxError: Unexpected identifier 'NO'`。用 `--enable-logging=stderr` 抓 CONSOLE 才看见。

**手绘的天花板**（要跟作者说清）：矢量赛璐璐能做到"干净的侧脸剪影 + 正确的人物特征"，
但和真动漫立绘的差距主要在**面部比例的手绘感、发丝层次、光影过渡**——这些靠贝塞尔硬堆性价比很低。
所以这版适合当**过渡图标**；正式形象还是等能生图或作者给立绘后替换。

## 追加（同日第三轮）：标题定 F-1，图标改女主头像


- 作者定：**标题卡 F-1**（实景底 · 左下署名 · 隶书）、**图标换成女主镜荧头像**。
- `Scenario/brand/icon-portrait.html` / `.png`：64×64 像素格程序化头像，
  按设定表取特征——银白长发、蓝眼、童稚脸、刘海齐眉、小虎牙（血族）、黑蓝晚礼服领。
  7 版：P-1 标准、P-2 眨眼、P-3 收虎牙腮红、P-4 加山脊与右上青石、C-1 近黑夜蓝、C-2 反白底、C-3 青环徽章；
  每版都排 256/128/64/32 四档，**32px 是硬指标**（任务栏就那么大）。
- **整图加深色描边**：银白发在浅色任务栏/白色壁纸上会整个消失，先按四向偏移铺一层深色再画本体。
- 三轮返工的坑（都在这版修掉了）：
  ① `col()` 每列宽度写成 `s*N`（52px）而不是 1 个设备像素，刘海糊成一大块白板；
  ② 背景装饰走了人物坐标系，山脊被头完全挡住 → 装饰改用**未缩放的画布坐标**；
  ③ `render` 里引用了 `makeCtx` 作用域的 `g`，运行时报 `g is not defined`、整页空白
  （headless 截图看不出报错，用 node + DOM 桩跑一遍才定位到）。
- 已知取舍：**这是程序化像素头像，不是立绘**。等镜荧按「先出 MC 皮肤图」的流程做正式立绘后，
  图标应换成那张立绘的脸部裁切；现在这版是过渡。

## 追加（同日第二轮）：作者选定 F 方向 + 隶书


- 细化稿 `Scenario/brand/concepts-f-lishu.html` / `.png`：
  **标题卡 6 版**（F-1 干净压底、F-2 硬描边、F-3 夜樱底、F-4 居中细线片头感、F-5 霓虹发光、F-6 朱印收尾），
  **启动 logo 动效时间轴 5 帧**（0.0 全黑 → 0.35 山脊浮起 → 0.9 隶书逐字淡入 → 1.6 青线划出 → 2.1 副标+石点 → 2.6 淡出，可跳过），
  **图标 3 版**（IC-1 山与青石 / IC-2 朱底隶书「石」/ IC-3 夜街山与一盏灯），各带 128/64/32px 可读性验视。
- 隶书（`LiSu`）实测可用，但**别人机器上不一定有** → 定稿后标题字一次性烘成 PNG 贴图进 `Resources/tex`，
  运行时不用 `CreateDynamicFontFromOSFont` 取隶书，避免掉方框。
- **exe 改名的担心是多余的**：构建输出名写死在 `Bootstrap.cs:178` 的 `Build/Win/Campus.exe`，
  与 `productName` 无关；改名字不影响 `taskkill //IM Campus.exe` 与任何验收命令。
- 修掉的问题：F-6 朱印压住「他」字（移到右下）、F-4 青石位置无意义（收到标题右侧当句点）、
  小标题"五版"实为六版。

## 做了什么


作者定名**《他山之石》**，要求换掉桌面图标与进入游戏时的 logo（不要 Unity 默认启动画面），先出几版看。

- `Scenario/brand/concepts.html` —— 概念稿本体（可在 Edge 里放大逐张看）。
- `Scenario/brand/concepts-v2.png` —— 渲染出的对比总览图（v1 有错字与劣质笔触，已修，见下）。
- `Scenario/brand/ref/` —— 三张**取自当前构建的实景底图**：主街夜景、夜樱林、主街钟楼方向。
- 代码改动（为拍干净底图）：`Args.NoHud` 新增，`Hud.OnGUI` / `Minimap.OnGUI` 首行加 `Args.NoHud` 短路。
  用法 `Campus.exe -nohud -fp 1 -shot ... -time 21.5`；`-fp 1` 顺带把主角模型移出画面。
  已重新构建（`shots/build90b.log`，Succeeded）。

## 为什么走代码原生而不是 AI 出图

`ImageGen` 服务本次返回 **HTTP 403 / code 112**（配额受限，响应里给了定价页），不可用。
更关键的是：中文标题字用 AI 出图极易写错笔画，而本作资产本来就全是程序化生成——
所以标题用**系统真实字体**（雅黑 / 等线 / 楷体 / 隶书 / 宋体）排版，像素版是**先按 14~18px 栅格化、
再最近邻放大**的真像素化（不是把矢量字模糊放大），字形 100% 正确且可复现。

## 六版标题方向（第一节）

| 编号 | 方向 | 说明 |
|---|---|---|
| A | 像素 HD-2D · 小镇夜景 | 实景底 + 像素化标题 + 青色横线 + 英文副标 |
| B | 现代极简 · 几何山石 | 双三角山 + 右上刻面青石，最适合当统一标志 |
| C | 水墨留白 | 宣纸底、枯笔近山、带棱角的墨石，副标"他山之石，可以攻玉" |
| D | 像素 HD-2D · 夜樱 | A 的换底版 |
| E | 竖排 + 朱印 | 行楷竖书 + 四字方印，古典/悬疑味 |
| F | 左下署名 · 都市夜 | 实景底，标题压在左下，霓虹外发光 |

第二节是同一版式换字体的对比；第三节是 6 个图标各缩到 **128 / 64 / 32 px** 的可读性验视
（I-3 朱印到 32px 就糊成红块，已在图注里标出来，只适合当封面角标）。

v1 → v2 修的问题：水墨卡的山画成了灰色波浪（改成浓墨实心山脊 + 淡墨远山两层）、
副标错字"他山**之家**石"、I-2 图标石块压在山上像补丁（移到山顶外侧）、卡片编号顺序与阅读顺序不一致。

## 落地约束（选定后按这个接）

1. `productName` 现为「校园叙事 RPG」→ 改「他山之石」；窗口标题与任务栏跟随。
   HUD 左上那行写死的 `（游戏名待定）· 校园叙事 RPG（Unity 版）`（`Hud.cs:55`）一起改。
2. **exe 名要不要跟着改是决定点**：改成 `他山之石.exe` 会打断 `taskkill //IM Campus.exe`、
   构建脚本与 docs 里所有验收命令；建议 exe 保留 `Campus.exe`，对外显示名走 productName。
3. **Unity 默认 splash 去不掉**：`m_ShowUnitySplashLogo=0` 与自定义 splash 图都是 Pro 版功能，
   个人版只能在它后面接**我们自己的开场 logo**（黑底 → 标题淡入 → 青色横线划出 → 淡出进主菜单）。
   这段由代码控制，就是作者实际会看到的 logo。
4. 桌面图标：`Game/Assets/icon.png` 换成选定方案的 1024 与多尺寸图集。
5. 标题字程序化烘成贴图，不引入外部图片资产，符合本作"资产全程序化"约定。

## 验证方式

```bash
# 重出对比图（--screenshot 必须给绝对路径，相对路径会报"找不到指定的路径"）
"/c/Program Files (x86)/Microsoft/Edge/Application/msedge.exe" --headless=new --disable-gpu \
  --hide-scrollbars --force-device-scale-factor=1 \
  --user-data-dir="C:/Users/ASUSA/AppData/Local/Temp/qoder-edge-shot" \
  --window-size=1680,2960 \
  --screenshot="D:/AI/GLM/.zcode/workspace/Unity开发galgame/Scenario/brand/concepts-v2.png" \
  "file:///D:/AI/GLM/.zcode/workspace/Unity%E5%BC%80%E5%8F%91galgame/Scenario/brand/concepts.html"

# 重拍实景底（-nohud 去 HUD 与小地图，-fp 1 去主角模型）
Game/Build/Win/Campus.exe -nohud -fp 1 -shot Scenario/brand/ref/bg-town-night.png \
  -time 21.5 -freeze 1 -px 6 -pz -34 -at 0,26 -pitch 0.0
```
