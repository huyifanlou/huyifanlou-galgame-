---
summary: Unity版像素密度统一与待办
created: 2026-10-16
updated: 2026-10-25
status: absorbed
---

## Summary

校园叙事 RPG 的 Unity 重建已到「整体像素密度统一」阶段：地面/道路/建筑约 160px/米、人物 4× 逻辑格（按作者要求暂不动）。剩余密度项（路面重复周期、别墅/操场/池塘、建筑立面分块以冲 360px/米）与玩法系统待下一窗口一次做完。

## Done

- [2026-10-16] 全项目零 JS：数据 `CampusData.cs` / 校验 `WorldValidator.cs` / 贴图 `TextureLibrary.cs`；JS 工具退役至 `tools/legacy-js/`
- [2026-10-16] 贴图生成器移植 C#（mulberry32 位级一致 + Canvas2D 原语），逐像素对拍后切换运行时为程序化生成（播放器 94→70MB）
- [2026-10-16] 美化①：自写后处理栈——移轴景深/泛光/暗角/色彩分级（`Assets/Shaders/PostFX.shader` + `Scripts/PostFX.cs`，随昼夜联动）
- [2026-10-16] 美化②：路缘石、树冠三层双色调、灌木亮顶、建筑阳台栏杆
- [2026-10-16] 人物：4 帧行走（站立/前迈/并步/后摆）+ 铰接腿（上段贴髋、下段错位、鞋跟下段）+ 身体起伏/侧身前倾/摆臂；精灵 4× 逻辑格（104×152/格，表 416×608）
- [2026-10-16] 像素密度两轮翻倍：草地 153px/米、道路与建筑 160px/米；装饰同步（门 ×4、雨棚 ×4、招牌运行时 2× 放大）
- [2026-10-16] 修复清单：教学楼侧翼悬空 0.7m、左右朝向反转、夜景过暗/白昼过亮、行走身体腿分离、道路交叉口接缝（加铺装块）

## Decisions

- [2026-10-16] 引擎 Unity 2022.3.62f3c1 中国版（国际版在中国镜像全 404）；许可证已由作者激活
- [2026-10-16] 单一语言 C#：程序化资产运行时生成，不再有导出步骤与 world.json
- [2026-10-16] 密度铁律：所有贴地/贴面纹理世界尺度必须对齐（10.4cm 格基准）；改密度只动 tiling/贴图尺寸，不动美术配方
- [2026-10-16] 校验器优先：布点/朝向/密度类改动先加自动校验规则再改数据

## Pitfalls

- [2026-10-16] Unity `SetPixels` 行序自下而上——画布缓冲必须翻转，否则全部贴图纵向镜像
- [2026-10-16] `QuadZ` 的 U 轴镜像修了招牌文字但会镜像精灵左右行 → 行映射需交换（PlayerRig 已注释）
- [2026-10-16] 单张贴图 2048 上限：建筑立面要到 360px/米必须拆分为多块
- [2026-10-16] 像素精灵动作上限低：身体起伏 >2px 必须配合「腿根随身体、脚掌贴地」，否则腿身分离
- [2026-10-16] C#/JS 位级一致要点：LCG 用 uint 回绕 + double；Math.round = floor(x+0.5)；canvas 浮点 fillRect 需按覆盖率抗锯齿
- [2026-10-16] 含中文的 git 提交信息必须走 `.git/COMMIT_MSG_TMP` + `git commit -F`（`-m` 直传会乱码）

## Next

- [ ] 像素统一收尾：①路面石板 2×2→4×4（重复周期 0.8→1.6m）②别墅外墙/操场跑道/池塘贴图 ×2 ③非招牌 PNG 资产复查
- [ ] 建筑立面分块（用户点名目标 360px/米）
- [ ] 玩法系统（截止 2026-11-18 的大头）：迭代 2 日程经营（属性/行动闭环 + 主线/日常入口，需录屏）；迭代 3 剧情对话 + LLM 动态事件导演
- [ ] 5-8 分钟成片（含 ≥2 分钟 AI 协同）、部署上线 + README 复现、技术文档 PDF ≤30 页

## Refs

- 工作区：`D:\AI\GLM\.zcode\workspace\Unity开发galgame`（独立 git 仓库；ZCode 侧栏同名分组）
- 代码：`Game/Assets/Scripts/{CampusData,TextureLibrary,WorldBuilder,PlayerRig,TimeOfDay,PostFX,MatLib}.cs`｜`Game/Assets/Editor/{Bootstrap,WorldValidator}.cs`｜`Game/Assets/Shaders/PostFX.shader`
- 文档：`docs/00~08`（迁移/修复/运行手册/扩容/首屏/单语言/贴图对拍/美化）
- 构建与截图：`Unity.exe -batchmode -executeMethod Bootstrap.BuildWindowsBatch`；`Campus.exe -shot x.png -time 18.5 -px 0 -pz 4 -freeze 1`
- 编辑器工具：Campus/校验世界布点｜Campus/导出世界数据 JSON（对拍）｜Campus/导出 C# 生成贴图（对拍）
