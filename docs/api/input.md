# 鼠标键盘

## 鼠标操作

### 移动鼠标

移动鼠标到指定坐标。

```http
GET /api/mouse/move/{x}/{y}
```

**示例：**

```bash
curl http://localhost:8080/api/mouse/move/500/300
```

---

### 获取鼠标位置

返回当前鼠标位置。

```http
GET /api/mouse/position
```

**响应：**

```json
{
  "x": 500,
  "y": 300
}
```

---

### 鼠标滚轮

滚动鼠标滚轮。

```http
POST /api/mouse/scroll
Content-Type: application/json

{"delta": 1}
```

**参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| delta | int | 滚动量，正数向上，负数向下 |

---

## 键盘操作

### 发送文本

发送文本按键。

```http
POST /api/keys
Content-Type: application/json

{"keys": "Hello World"}
```

---

### 发送快捷键

发送组合快捷键。

```http
POST /api/hotkey
Content-Type: application/json

{"keys": ["ctrl", "c"]}
```

**常用快捷键：**

| 快捷键 | 说明 |
|--------|------|
| `["ctrl", "c"]` | 复制 |
| `["ctrl", "v"]` | 粘贴 |
| `["ctrl", "a"]` | 全选 |
| `["ctrl", "s"]` | 保存 |
| `["alt", "f4"]` | 关闭窗口 |
| `["ctrl", "z"]` | 撤销 |

---

### 支持的按键名称

```
enter, tab, escape, space, backspace, delete
up, down, left, right, home, end, pageup, pagedown
f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11, f12
ctrl, alt, shift, win
insert, capslock, numlock, scrolllock
```

---

## 输入示例

=== "Python"

    ```python
    import requests
    
    base = "http://localhost:8080"
    
    # 移动鼠标
    requests.get(f"{base}/api/mouse/move/100/100")
    
    # 输入文本
    requests.post(f"{base}/api/keys", json={"keys": "Hello"})
    
    # 发送快捷键
    requests.post(f"{base}/api/hotkey", json={"keys": ["ctrl", "a"]})
    ```

=== "JavaScript"

    ```javascript
    const base = "http://localhost:8080";
    
    // 移动鼠标
    fetch(`${base}/api/mouse/move/100/100`);
    
    // 输入文本
    fetch(`${base}/api/keys`, {
      method: "POST",
      headers: {"Content-Type": "application/json"},
      body: JSON.stringify({keys: "Hello"})
    });
    
    // 发送快捷键
    fetch(`${base}/api/hotkey`, {
      method: "POST",
      headers: {"Content-Type": "application/json"},
      body: JSON.stringify({keys: ["ctrl", "a"]})
    });
    ```
