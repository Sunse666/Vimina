---
icon: material/home
---

# Vimina

!!! tip "温馨提示"

    Vimina 是一款 Windows 桌面自动化工具，受 [Vimium](https://github.com/philc/vimium) 启发开发。

    - [x] 您可以通过按下快捷键 ++left++ 和 ++right++ 来快速跳转到上一页和下一页！
    - [x] 您可以通过点击页面顶部搜索栏或按下快捷键 <kbd>/</kbd> 来快速查找想要的内容！
    - [x] 您可以通过点击页面顶部主题按钮来切换网站主题，不同主题带来不同的心情！

    最后，祝您阅读愉快！ :heart:

## 简介

Vimina 是一款 Windows 桌面自动化工具。基于 **FlaUI** 自动化框架，通过 **UIA3** 协议识别窗口控件，为每个可交互控件生成字母标签。只需敲击键盘即可精准点击任意按钮、链接或输入框。

同时提供完整的 **HTTP API** 接口，支持与 AI 助手和自动化脚本集成。

## 核心功能

=== "智能标签"

    基于 UIA3 协议自动识别控件，生成双字母标签，分辨率无关，窗口位置变化不影响操作。

=== "HTTP API"

    完整的 RESTful 接口，支持 CORS，易于与 AI 助手和自动化脚本集成。

=== "VMA 脚本"

    简洁的脚本语言，支持变量、函数、循环等语法，可编译为独立 exe 文件。

=== "后台操作"

    不移动鼠标，直接操作后台窗口，不影响前台工作。

## 快速链接

<div class="grid cards" markdown>

-   :fontawesome-solid-download: **下载最新版本**

    ---

    从 GitHub Releases 下载最新版本，解压即用

    [:octicons-arrow-right-24: 前往下载](https://github.com/Sunse666/Vimina/releases)

-   :fontawesome-brands-github: **GitHub 仓库**

    ---

    查看源代码，提交 Issue 或 PR

    [:octicons-arrow-right-24: 前往仓库](https://github.com/Sunse666/Vimina)

-   :material-api: **HTTP API 文档**

    ---

    完整的 API 端点和调用示例

    [:octicons-arrow-right-24: 查看文档](api/index.md)

-   :material-script-text: **VMA 脚本语法**

    ---

    脚本语言详细语法和案例

    [:octicons-arrow-right-24: 查看语法](vma/index.md)

</div>

## 系统要求

- 操作系统：**Windows 10 / 11**
- 运行时：**.NET Framework 4.6.2+**
- 无需安装，解压即用

## 开始使用

```bash
# 1. 下载并解压
# 2. 运行 Vimina.exe
# 3. 打开任意应用窗口
# 4. 按 Alt+F 显示控件标签
# 5. 输入标签字母点击控件
```

<div align="center" markdown>
[快速开始](getting-started.md){ .md-button }
[基本使用](basics.md){ .md-button }
[HTTP API](api/index.md){ .md-button }
[VMA 脚本](vma/index.md){ .md-button }
</div>
