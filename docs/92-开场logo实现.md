# 92 · 开场 logo 实现（F-1 版式 · 隶书烘贴图）

品牌线最后一件：进入游戏时的 logo。docs/91 定的是 F-1（实景底 · 左下署名 · 隶书），
但**开场动画不能拿实景当底**（游戏刚启动时世界还在建），所以落地成"黑底 + 山脊剪影 + 隶书标题"的片头版式，
配色与青石元素沿用 F-1/B 标志。

## 为什么必须烘成贴图

- **Unity 默认 splash 去不掉**：`m_ShowUnitySplashLogo=0` 与自定义 splash 图都是 Pro 版功能，个人版只能在它后面接自研开场。
- **隶书（`LiSu`）是 Windows 自带字体，别人机器不一定有**：运行时 `Font.CreateDynamicFontFromOSFont` 取不到就掉方框。
  所以标题四个字在开发机上一次性烘成 PNG，运行时不再碰系统字体。

## 做了什么

| 文件 | 作用 |
|---|---|
| `Scenario/brand/intro-assets.html` | 素材生成器 + **时间轴预览器**（`?a=preview&t=2.3` 直接把整段动画画成一帧，先验设计再写 C#） |
| `Game/Assets/Resources/intro/intro-char0..3.png` | 隶书「他山之石」逐字，300×300，含青色外发光 |
| `…/intro-ridge.png` | 山脊剪影 1600×360（双层，带受光面） |
| `…/intro-sub.png` | 英文副标 `STONES FROM ANOTHER HILL`（字距 22px） |
| `…/intro-stone.png` | 刻面青石 120×120 |
| `Game/Assets/Scripts/IntroScreen.cs` | 时间轴与绘制（IMGUI `GUI.DrawTexture`，设计分辨率 1600×900 等比缩放居中） |
| `Bootstrap.SetupIntroTextures()` | 每次构建自动跑：这批素材 **Bilinear + 无 mipmap + 无损**（像素贴图那套 Point 会把隶书边缘啃出锯齿） |
| 门控 | `Hud` / `Minimap` 的 `OnGUI` 与 `PlayerRig.Update` 首行都加了 `IntroScreen.Live`，避免 IMGUI 先后顺序不定导致小地图盖在 logo 上、或角色在幕后偷偷移动 |

## 时间轴（3.0s = 2.6s 内容 + 0.4s 淡出）

| 时刻 | 内容 |
|---|---|
| 0.00 | 全黑 |
| 0.35→0.95 | 山脊自下方浮起（alpha 0→0.85，位移 26px→0） |
| 0.90 起 | 标题逐字淡入，每字间隔 90ms、各 0.28s，带轻微上浮 |
| 1.60→1.95 | 青色横线由左划出（250px，底下垫一层同色微光） |
| 2.10→2.45 | 英文副标 + 青石点出现 |
| 2.60→3.00 | 整体淡出，`Live=false`，交回正常画面 |

`空格` 或鼠标点击 → 直接跳到淡出段。

## 验收

```bash
# 冻结到指定时刻拍图（带 -shot 时开场默认不播，必须显式给 -intro）
Campus.exe -intro 0.6 -shot shots/intro/game-0.6.png -freeze 1 -frames 40
Campus.exe -intro 2.5 -shot shots/intro/game-2.5.png -freeze 1 -frames 40
```

- `shots/intro/game-0.6.png`：只有山脊浮起、无标题 ✓
- `shots/intro/game-2.5.png`：完整锁定，**HUD 与小地图都没露出** ✓
- 构建日志：`开场素材导入设置完成：7 张`、`Succeeded` ✓
- 第一版标题整组偏右 55px（字心从 `540+i*210` 起算，块中心落在 855 而不是 800），
  已改成 `485+i*210` 并把横线/青石一起对中，重导后目验居中 ✓

## 已知边界

- 用 IMGUI 画（与现有 HUD 一致），没上 UGUI；素材只有 7 张 PNG，加载成本可忽略。
- `-nointro` 可强制跳过（做对比图时用）。
- 窗口非 16:9 时按等比缩放居中，上下或左右留黑边——这是片头，不做铺满裁切。
