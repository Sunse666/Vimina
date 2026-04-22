---
icon: material/api
---

# HTTP API

Vimina 提供完整的 HTTP API 接口，默认监听 `http://localhost:8080`。

## 概述

API 采用 RESTful 风格设计，支持以下功能：

- **基础操作** - 获取控件列表、截图、窗口列表等
- **点击操作** - 坐标点击、标签点击、后台点击等
- **窗口管理** - 激活、关闭、最小化、最大化窗口
- **鼠标键盘** - 鼠标移动、键盘输入、快捷键等
- **VMA 脚本** - 执行 VMA 脚本

## 基础信息

| 项目 | 值 |
|------|-----|
| 基础 URL | `http://localhost:8080` |
| 数据格式 | JSON |
| 字符编码 | UTF-8 |
| CORS | 支持 |

## 响应格式

所有 API 响应均为 JSON 格式：

```json
{
  "success": true,
  "data": { ... },
  "message": "操作成功"
}
```

错误响应：

```json
{
  "success": false,
  "error": "错误信息"
}
```

## 快速示例

=== "curl"

    ```bash
    # 获取控件列表
    curl http://localhost:8080/api/controls
    
    # 点击坐标
    curl http://localhost:8080/api/click/100/200
    
    # 发送文本
    curl -X POST http://localhost:8080/api/keys \
      -H "Content-Type: application/json" \
      -d '{"keys": "Hello World"}'
    ```

=== "Python"

    ```python
    import requests
    
    base_url = "http://localhost:8080"
    
    # 获取控件列表
    response = requests.get(f"{base_url}/api/controls")
    controls = response.json()
    
    # 点击坐标
    requests.get(f"{base_url}/api/click/100/200")
    
    # 发送文本
    requests.post(f"{base_url}/api/keys", json={"keys": "Hello World"})
    ```

=== "JavaScript"

    ```javascript
    const baseUrl = "http://localhost:8080";
    
    // 获取控件列表
    fetch(`${baseUrl}/api/controls`)
      .then(r => r.json())
      .then(console.log);
    
    // 点击坐标
    fetch(`${baseUrl}/api/click/100/200`);
    
    // 发送文本
    fetch(`${baseUrl}/api/keys`, {
      method: "POST",
      headers: {"Content-Type": "application/json"},
      body: JSON.stringify({keys: "Hello World"})
    });
    ```

## 详细文档

请查看以下章节了解详细的 API 文档：

- [基础操作](basic.md) - 控件列表、截图、窗口列表
- [点击操作](click.md) - 各种点击方式
- [窗口管理](window.md) - 窗口操作
- [鼠标键盘](input.md) - 输入控制
- [VMA 脚本](vma.md) - 脚本执行
