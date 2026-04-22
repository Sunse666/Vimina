# 点击操作

## 通过标签点击

点击指定标签的控件。

```http
POST /api/click
Content-Type: application/json

{"tag": "AB"}
```

**参数：**

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| tag | string | 是 | 控件标签 |

---

## 坐标点击

点击指定坐标。

```http
GET /api/click/{x}/{y}
```

**参数：**

| 参数 | 类型 | 说明 |
|------|------|------|
| x | int | X 坐标 |
| y | int | Y 坐标 |

**示例：**

```bash
curl http://localhost:8080/api/click/100/200
```

---

## 坐标右键点击

在指定坐标右键点击。

```http
GET /api/clickR/{x}/{y}
```

---

## 坐标双击

在指定坐标双击。

```http
GET /api/dblclick/{x}/{y}
```

---

## 后台坐标点击

支持后台点击，不移动鼠标。

```http
POST /api/clickAt
Content-Type: application/json

{
  "x": 100,
  "y": 200,
  "background": true
}
```

**参数：**

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| x | int | 是 | X 坐标 |
| y | int | 是 | Y 坐标 |
| background | bool | 否 | 是否后台点击，默认 false |

---

## 通过窗口标题点击

通过窗口标题和坐标点击，支持后台操作。

```http
POST /api/clickByTitle
Content-Type: application/json

{
  "title": "记事本",
  "x": 100,
  "y": 200,
  "background": true
}
```

**参数：**

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| title | string | 是 | 窗口标题（支持模糊匹配） |
| x | int | 是 | 相对窗口的 X 坐标 |
| y | int | 是 | 相对窗口的 Y 坐标 |
| background | bool | 否 | 是否后台点击 |

---

## 点击示例

=== "Python"

    ```python
    import requests
    
    # 标签点击
    requests.post("http://localhost:8080/api/click", json={"tag": "AB"})
    
    # 坐标点击
    requests.get("http://localhost:8080/api/click/100/200")
    
    # 后台点击
    requests.post("http://localhost:8080/api/clickAt", 
        json={"x": 100, "y": 200, "background": True})
    ```

=== "JavaScript"

    ```javascript
    // 标签点击
    fetch("http://localhost:8080/api/click", {
      method: "POST",
      headers: {"Content-Type": "application/json"},
      body: JSON.stringify({tag: "AB"})
    });
    
    // 坐标点击
    fetch("http://localhost:8080/api/click/100/200");
    
    // 后台点击
    fetch("http://localhost:8080/api/clickAt", {
      method: "POST",
      headers: {"Content-Type": "application/json"},
      body: JSON.stringify({x: 100, y: 200, background: true})
    });
    ```
