# 67 · CPU/GPU 占用修复（高分屏下锁帧失效）

作者（2026-10-28）：

> CPU和GPU占比异常，这方面性能优化

## 一、根因

`WorldBuilder.Start()` 原来写的是：

```csharp
Application.targetFrameRate = 60;
QualitySettings.vSyncCount = 1;
```

Unity 的规则是：**开启垂直同步后 `targetFrameRate` 被忽略**，帧率跟随显示器刷新率。
本机是 2560×1600 @ **165Hz**，所以游戏实际按 165 FPS 跑（不锁帧时这套场景能跑 ~500 FPS）。
CPU/GPU 都被按 165 FPS 反复提交，占用自然偏高：

| 状态 | FPS | 进程 CPU（单核=100%） | GPU |
| --- | --- | --- | --- |
| 修复前（vSync 1 @165Hz） | 165 | 175% ~ 410%（2~4 核） | 26% ~ 34% |
| 修复后（硬锁 60） | 59.8 | 62% ~ 89%（0.6~0.9 核） | 10.8% ~ 11.7% |

（GPU 百分比为 Windows `GPU Engine` 计数器求和；不锁帧的峰值约 500 FPS，说明 GPU 本身有余量。）

## 二、改法

1. `WorldBuilder.Start()`：

```csharp
QualitySettings.vSyncCount = 0;
Application.targetFrameRate = 60;
```

关垂直同步、用 `targetFrameRate` 硬锁 60。1600×900 窗口下 60 FPS 是叙事游戏的目标帧率，
GPU 仅约 11%，后续加内容仍有大量余量。

2. 诊断参数补充（`TimeOfDay.Init`）：

- `-fpslog`：只每 300 帧记录一次 `[Perf] 近 300 帧平均 … FPS`，**保持 60 帧上限**（用于验证锁帧）；
- `-fps`（原有）：关 vsync、不限帧，用于测性能上限/对比；
- `-noshadow / -noao / -nofx` 保持原样。

## 三、验证

```bash
# 锁帧 + 帧率日志
Campus.exe -fpslog 1
# Player.log 应出现：
# [Perf] 近 300 帧平均 59.8 FPS

# 前台占用采样（PowerShell）
# \Process(Campus)\% Processor Time
# \GPU Engine(*)\Utilization Percentage（按 pid 求和）
```

- 修复后 8 次采样：CPU 61.8~89.2%（单核百分比），GPU 10.8~11.7%，FPS 59.8；
- 修复前同机位 8 次采样：CPU 174.6~410.8%，GPU 25.8~33.8%，实际 165 FPS；
- 画面/内容无变化，仅改变帧节奏。

## 四、取舍与后续

- 165Hz 屏上 60 FPS + 无垂直同步，理论上可能有轻微撕裂；叙事游戏可接受。若以后要 120 FPS，
  把 `targetFrameRate` 提到 120 即可（GPU 约 22%）；
- 启动时构建程序化世界仍需 ~20 s（首屏期间 CPU 单核满载），与帧循环无关，若要缩短需另做分批构建；
- 背景窗口被 DWM 压到低帧率属系统行为，不影响前台。
