# Vimina
<p align="center"> <img src="assets/logo/logo.png" alt="Vimina Logo" width="128"> </p><p align="center"> <strong>用键盘来操作窗口应用</strong> </p><p align="center"> <img src="https://img.shields.io/badge/Platform-Windows-blue?style=flat-square" alt="Platform"> <img src="https://img.shields.io/badge/Engine-FlaUI-orange?style=flat-square" alt="Engine"> <img src="https://img.shields.io/badge/Inspired_by-Vimium-green?style=flat-square" alt="Inspired"> </p>


## ✨ 简介
受浏览器插件 Vimium 启发，在 Windows 上复刻功能的软件。通过 FlaUI 自动化框架识别窗口中的可交互控件，为每个控件生成唯一的字母标签，只需敲击键盘即可精准点击任意按钮、链接、输入框，因此彻底解放鼠标，让键盘党的效率飞升。


## 🖼️ 截图
<p align="center"> <img src="assets/Screenshot.png" alt="Screenshot"> </p>


## 🚀 功能特性

### 🏷️ 智能标签系统
- 自动识别窗口中的可交互控件
- 为每个控件生成易于输入的双字母标签
- 标签位置智能偏移，避免遮挡控件

### 🎨 实时视觉反馈
- 黄色—默认状态，等待输入
- 橙色—前缀匹配，继续输入
- 绿色—完全匹配，即将点击
- 灰色—无效标签，已过滤

### ⌨️ 快捷键操作

> [!TIP]
> |快捷键|功能|
> |---|---|
> |Alt + F|显示 / 隐藏标签|
> |Alt + R|刷新标签|
> |Esc|清除所有标签|
> |A-Z|输入标签字母|
> |Backspace|删除已输入字符|

### 🎯 支持的控件类型
- Button、CheckBox、RadioButton
- ComboBox、Edit、Slider、Spinner
- Hyperlink、MenuItem、ListItem
- TabItem、TreeItem、ToolBar
- DataItem、SplitButton

### 📌 系统托盘
- 启动后自动最小化到托盘
- 左键点击显示主窗口
- 右键菜单快速访问配置


## 📦 安装与使用

### 系统要求
- 操作系统：Windows 10 / 11
- 运行时：.NET Framework 4.6.2+

### 快速开始

1. 从 Releases 页面下载最新版本
2. 解压到任意目录
3. 运行 Vimina.exe
4. 打开任意应用窗口，按下 Alt + F

### 操作流程

1. 聚焦目标窗口
2. Alt+F 显示标签
3. 按下标签上的字母
4. 自动点击对应控件

### 目录结构

```
Vimina/
├── Vimina.exe    # 主程序
└── config.ini      # 配置文件
```


## ⚙️ 配置说明

配置文件为程序目录下的 config.ini，使用 INI 格式

### 标签样式

```
[Label]
BackgroundColor_Default = 0x00DDFF    # 默认背景色（青色）
BackgroundColor_Match = 0x00FF00      # 完全匹配时（绿色）
BackgroundColor_Prefix = 0x00A5FF      # 前缀匹配时（橙色）
BackgroundColor_Invalid = 0x808080    # 无效标签（灰色）
TextColor = 0x000000                                # 文字颜色（黑色）
FontSize = 12                                                  # 字体大小
FontWeight = 700                                         # 字体粗细（700=粗体）
OffsetX = 0                                                   # 标签 X 轴偏移
OffsetY = 18                                                  # 标签 Y 轴偏移
```

### 过滤设置

```
[Filter]
MinWidth = 8                          # 最小控件宽度
MinHeight = 8                        # 最小控件高度
MaxDepth = 50                       # 控件树遍历深度
IgnoreWindowClass = Progman,WorkerW,Shell_TrayWnd,Windows.UI.Core.CoreWindow
```

|字段||说明|
|---|---|
|MinWidth / MinHeight|过滤掉过小的控件，避免标签过于密集|
|path|限制控件树遍历深度，防止扫描过慢|
|icon|忽略的窗口类名，用逗号分隔|

### 性能相关

```
[Performance]
ClickDelay = 30                       # 点击后延迟(ms)
```

> [!NOTE]
> 如果点击不稳定或目标应用响应慢，可适当增加 ClickDelay 值


## 📋 配置示例

### 默认配置

```
[Label]
BackgroundColor_Default = 0x00DDFF
BackgroundColor_Match = 0x00FF00
BackgroundColor_Prefix = 0x00A5FF
BackgroundColor_Invalid = 0x808080
TextColor = 0x000000
FontSize = 12
FontWeight = 700
OffsetX = 0
OffsetY = 18

[Filter]
MinWidth = 8
MinHeight = 8
MaxDepth = 50
IgnoreWindowClass = Progman,WorkerW,Shell_TrayWnd,Windows.UI.Core.CoreWindow

[Performance]
ClickDelay = 30
```

### 深色主题配置

```
[Label]
BackgroundColor_Default = 0x3D3D3D
BackgroundColor_Match = 0x00FF00
BackgroundColor_Prefix = 0x00A5FF
BackgroundColor_Invalid = 0x1A1A1A
TextColor = 0xFFFFFF
FontSize = 11
FontWeight = 400
OffsetX = 0
OffsetY = 15
```

### 快速扫描

```
[Filter]
MinWidth = 15
MinHeight = 15
MaxDepth = 25
IgnoreWindowClass = Progman,WorkerW,Shell_TrayWnd,Windows.UI.Core.CoreWindow

[Performance]
ClickDelay = 10
```

> [!TIP]
> 减少 MaxDepth 和增大 MinWidth/MinHeight 可显著提升扫描速度


## 🛠️ 技术特点
- 基于 FlaUI 自动化框架，支持 UIA3 协议
- 智能标签生成算法，优先使用易按的键位组合
- 控件去重机制，相邻 10px 内的控件自动合并
- 全局键盘钩子，不影响其他应用的正常使用
- 单实例运行保护，防止重复启动
- 点击后自动恢复鼠标位置


## ❓ 常见问题

### Q: 按 Alt+F 没有反应？

1. 确保目标窗口在前台且已获得焦点
2. 检查窗口是否在 IgnoreWindowClass 忽略列表中
3. 部分 UWP 应用可能不支持 UIA3 协议

### Q: 标签显示位置不对？

修改配置文件中的偏移值：

```
[Label]
OffsetX = -20    # 向左偏移
OffsetY = -5       # 向上偏移
```

### Q: 扫描速度太慢？

减少遍历深度和过滤小控件：

```
[Filter]
MinWidth = 20
MinHeight = 20
MaxDepth = 30
```

### Q: 某些控件识别不到？

- 部分自绘控件可能未正确暴露 UI 自动化接口
- 尝试增加 MaxDepth 值
- 某些控件类型(如 Image、Text)默认不可交互

### Q: 如何添加忽略的窗口？

在配置文件中追加窗口类名：

```
[Filter]
IgnoreWindowClass = Progman,WorkerW,Shell_TrayWnd,Windows.UI.Core.CoreWindow,MyAppClass
```

> [!TIP]
> 可使用 Spy++ 或 WinSpy 工具查看窗口类名

### Q: 为什么交互使用的是鼠标而不是其他方式？

- 使用其他方式与网页元素交互过于困难
- 部分软件可能会检测或禁止此类交互方式

后续将支持选择更多的交互方式

---
<p align="center"> Made with 💚 by Vimina </p>
