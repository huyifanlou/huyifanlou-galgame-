# 126 · 恢复方块人物精修与 MC-MMD-rust 接入核对

2026-11-15。作者否定平滑人物，要求回退并精修之前的建模，提供 MC 3D MMD 模组视频与仓库。

## 本轮实现

PlayerAvatar 恢复方块体、像素脸、原六人服装与动作，保留第一人称换角隐藏修复。撤除实验 AnimeActor/Resources Characters 自动加载分支；离线实验资产保留但不再被角色路径调用。docs/124 平滑造型撤回，docs/123 的实景对话回退继续保持，世界和室内不回滚。

在原模型上做小幅精修：积木块 4mm 微倒角（薄配饰自动限制半径），保留大平面/头身比/直角轮廓；新增分件收尖刘海（不越眼线）、长发后壳拆成九束长短错落的棱面发束；发丝明暗对比收小，人物三档描边收细。没有再换成球体脸和圆柱四肢。C 角色选择说明同步恢复原服装。

## 参考实际核对

- 视频：https://www.bilibili.com/video/BV1SvFszvECQ/ ，本机 Edge 打开并查看 15 秒处，截图 `shots/126-mc-mod/reference-edge.png`。页面标题“【MC模组发布】全新MMD 3D模型皮肤模组发布”。未登录只能预览，未声称完整观看 5 分钟视频。
- 仓库：https://github.com/shiroha-233/MC-MMD-rust ，核对默认 `1.20-vr` 分支 README、LICENSE、THIRD_PARTY_LICENSES、rust_engine/Cargo.toml、src/lib.rs、jni_bridge/mod.rs。
- 视频使用完整 MMD 模型替代 MC 玩家，非仅增加方块网格数量。模型和动作由使用者提供；仓库不是六人角色素材库。本工程未发现 PMX/PMD/VMD/VRM 角色源文件。

## 接入结论

不能把 jar 或现有 Rust 动态库直接拖进 Unity 使用。Mod 集成层是 Fabric/Forge + Java + OpenGL；Rust 当前公开桥接为 JNI，非 Unity C# 可直接调用的 C ABI。

能做的适配：PMX/PMD 解析、VMD 动作/表情、骨骼和物理引擎可作为实现参考或移植模块。Unity 路线优先把授权 PMX 转为 Unity 蒙皮网格/骨骼/BlendShape，VMD 转动画，Unity 自己渲染；如果必须复用 Rust 核心，则增加 C ABI 与 C# P/Invoke、数据生命周期、坐标/蒙皮/物理同步，再单独提供 Windows 插件。现有 JNI/OpenGL 渲染不搬进主相机；该原生插件路线不能自动覆盖 WebGL。

根 LICENSE 为 MIT，允许修改与商业使用，分发须保留版权与许可证；第三方依赖另核对。THIRD_PARTY_LICENSES 的 Rapier 描述与当前 Cargo/README 的 Bullet 路径有差异，实际构建依赖需要逐项确认。模型、贴图、VMD 的授权独立，不能用代码的 MIT 许可推定视频角色可用于比赛/商业分发。

本轮没有接入第三方运行时代码，也没有下载/冒用视频角色。完成旧模型精修与接入可行性核对，不把研究结论写成已经接通 MMD。

## 验证

Windows 构建：`shots/126-mc-mod/build.log`。六人正面截图与换角/运动/第一人称显示回归：同目录；保留 `-avatarcheck` 验收入口。最终构建启动正常实机供作者查看。未提交、未推送。

结果：六人正面截图已逐个查看；check.log 六人换角/运动/隐藏重建全部 PASS，13 间进出通过 13、失败 0，光照绑定通过。补修紫薇旧发顶穿出帽冠，最终以 build-final.log / check-final.log 为准。
