# 参赛工具

游戏复现只需预装Unity和build-release.ps1；smoke-test.ps1执行独立存档菜单验收。

build_report.py为一月增强报告入口，调用build_month_report.py与month_report_content.py，需要ReportLab、PyMuPDF、Pillow及作者本地shots/录制帧素材；这些属于报告制作依赖，不是游戏运行依赖。报告正文可直接阅读Submission/作品说明/一月增强报告文字索引.md，正式PDF同目录。
