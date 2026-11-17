# 随游戏打包的中文字体

地图/界面/场景字牌：Noto Sans SC（思源黑体）400；标题600。主线正文默认 Noto Serif SC（思源宋体）400，可切换黑体。静态 TTF 在 Game/Assets/Resources/Fonts，SIL OFL 许可证随字体保留，不依赖作者电脑安装的字体。

上游：

- https://github.com/google/fonts/tree/main/ofl/notosanssc
- https://github.com/google/fonts/tree/main/ofl/notoserifsc

下载变量源到 sources/NotoSansSC-variable.ttf、NotoSerifSC-variable.ttf 后，在安装 fontTools 的 Python 环境运行 `build-static-fonts.py` 生成固定字重。变量源、wheel及本机准备环境只作缓存，不提交；发行只需静态 TTF 和许可证。首次准备使用独立 tools/.fontenv，未更改系统字体或系统 Python。
