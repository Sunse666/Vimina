# 基本使用

## 扫描窗口

打开任意应用程序窗口，按 ++alt+f++ 显示控件标签。

Vimina 会自动识别窗口中的所有可交互控件，并为每个控件生成一个双字母标签。

## 键盘点击

输入标签字母（如 `DJ`），Vimina 会自动点击对应的控件。

!!! tip "提示"
    标签输入有超时时间，默认为 2 秒。如果超时，已输入的字母会被清除。

## 隐藏标签

再次按 ++alt+f++ 或按 ++esc++ 可以隐藏标签。

## 常用快捷键

| 快捷键 | 功能 |
|--------|------|
| ++alt+f++ | 显示/隐藏标签 |
| ++esc++ | 隐藏标签 |
| ++alt+q++ | 退出程序 |

## 后台点击

Vimina 支持后台点击，即不激活窗口也能点击控件。这在操作后台窗口时非常有用。

通过 HTTP API 可以实现后台点击：

```bash
curl -X POST http://localhost:8080/api/clickAt \
  -H "Content-Type: application/json" \
  -d '{"x": 100, "y": 200, "background": true}'
```

## 下一步

- 了解 [HTTP API](api/index.md) 进行自动化集成
- 学习 [VMA 脚本](vma/index.md) 编写自动化任务
