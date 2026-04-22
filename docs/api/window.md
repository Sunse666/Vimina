# 窗口管理

## 激活窗口

激活指定句柄的窗口。

```http
GET /api/activate/{handle}
```

**参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| handle | int | 窗口句柄 |

**示例：**

```bash
curl http://localhost:8080/api/activate/12345
```

---

## 关闭窗口

关闭指定句柄的窗口。

```http
GET /api/close/{handle}
```

---

## 最小化窗口

最小化指定句柄的窗口。

```http
GET /api/minimize/{handle}
```

---

## 最大化窗口

最大化指定句柄的窗口。

```http
GET /api/maximize/{handle}
```

---

## 通过标题操作

也可以通过窗口标题进行操作：

```http
POST /api/window/activate
Content-Type: application/json

{"title": "记事本"}
```

```http
POST /api/window/close
Content-Type: application/json

{"title": "记事本"}
```

---

## 窗口操作示例

=== "Python"

    ```python
    import requests
    
    # 获取窗口列表
    windows = requests.get("http://localhost:8080/api/windows").json()
    
    # 找到记事本窗口
    for win in windows:
        if "记事本" in win["title"]:
            # 激活窗口
            requests.get(f"http://localhost:8080/api/activate/{win['handle']}")
            break
    ```

=== "JavaScript"

    ```javascript
    // 获取窗口列表并激活记事本
    fetch("http://localhost:8080/api/windows")
      .then(r => r.json())
      .then(windows => {
        const notepad = windows.find(w => w.title.includes("记事本"));
        if (notepad) {
          fetch(`http://localhost:8080/api/activate/${notepad.handle}`);
        }
      });
    ```
