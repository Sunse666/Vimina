# 基础操作

## 获取控件列表

获取当前活动窗口的所有可交互控件。

```http
GET /api/controls
```

**响应示例：**

```json
[
  {
    "tag": "AB",
    "name": "确定",
    "controlType": "Button",
    "className": "Button",
    "bounds": {
      "x": 100,
      "y": 200,
      "width": 80,
      "height": 30
    },
    "isEnabled": true,
    "isOffscreen": false
  }
]
```

**字段说明：**

| 字段 | 类型 | 说明 |
|------|------|------|
| tag | string | 控件标签（双字母） |
| name | string | 控件名称 |
| controlType | string | 控件类型 |
| className | string | 类名 |
| bounds | object | 边界矩形 |
| isEnabled | bool | 是否可用 |
| isOffscreen | bool | 是否在屏幕外 |

---

## 截图

截取当前屏幕。

```http
GET /api/screenshot
```

**响应：** PNG 图片

**示例：**

```bash
curl http://localhost:8080/api/screenshot -o screenshot.png
```

---

## 获取窗口列表

获取所有可见窗口列表。

```http
GET /api/windows
```

**响应示例：**

```json
[
  {
    "handle": 12345,
    "title": "记事本",
    "className": "Notepad",
    "bounds": {
      "x": 100,
      "y": 100,
      "width": 800,
      "height": 600
    },
    "isActive": true
  }
]
```

**字段说明：**

| 字段 | 类型 | 说明 |
|------|------|------|
| handle | int | 窗口句柄 |
| title | string | 窗口标题 |
| className | string | 类名 |
| bounds | object | 边界矩形 |
| isActive | bool | 是否活动窗口 |

---

## 获取状态

获取程序运行状态。

```http
GET /api/status
```

**响应示例：**

```json
{
  "running": true,
  "version": "1.0.0",
  "uptime": 3600,
  "controlsCount": 42
}
```

---

## 健康检查

检查服务是否正常。

```http
GET /api/health
```

**响应：**

```json
{
  "status": "ok"
}
```
