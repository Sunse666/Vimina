---
icon: material/script-text
---

# VMA 脚本

VMA (Vimina Macro Automation) 是 Vimina 的脚本语言，用于编写自动化任务。

## 概述

VMA 是一种简洁的脚本语言，专为桌面自动化设计：

- **简洁语法** - 类似 Python 的简洁语法
- **丰富函数** - 内置鼠标、键盘、窗口等操作函数
- **控制流** - 支持条件、循环、函数等
- **可编译** - 可编译为独立 exe 文件

## 快速示例

```vma
// 自动登录示例
click(200, 150)      // 点击用户名输入框
sleep(100)
type("admin")        // 输入用户名

click(200, 200)      // 点击密码输入框
sleep(100)
type("password123")  // 输入密码

click(200, 250)      // 点击登录按钮
log("登录完成")
```

## 基本结构

VMA 脚本是一系列命令的集合，每行一个命令：

```vma
// 这是注释
click(100, 100)      // 点击操作
sleep(500)           // 等待
type("Hello World")  // 输入文本
```

## 数据类型

| 类型 | 说明 | 示例 |
|------|------|------|
| `int` | 整数 | `42`, `-10` |
| `float` | 浮点数 | `3.14`, `-0.5` |
| `string` | 字符串 | `"Hello"`, `'World'` |
| `bool` | 布尔值 | `true`, `false` |

## 变量

使用 `var` 声明变量：

```vma
var name = "Vimina"
var count = 10
var pi = 3.14
var enabled = true
```

## 详细文档

请查看以下章节了解详细的语法和函数：

- [语法参考](syntax.md) - 完整的语法说明
- [内置函数](functions.md) - 所有内置函数
- [脚本示例](examples.md) - 实用脚本示例
