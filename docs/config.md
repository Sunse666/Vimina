# 配置说明

Vimina 的配置文件位于程序目录下的 `config.json`。

## 标签设置

### labelStyle

标签显示样式。

```json
{
  "labelStyle": {
    "fontSize": 14,
    "fontFamily": "Consolas",
    "backgroundColor": "#4CAF50",
    "textColor": "#FFFFFF",
    "borderRadius": 4,
    "padding": 4
  }
}
```

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| fontSize | int | 14 | 字体大小 |
| fontFamily | string | Consolas | 字体名称 |
| backgroundColor | string | #4CAF50 | 背景颜色 |
| textColor | string | #FFFFFF | 文字颜色 |
| borderRadius | int | 4 | 圆角大小 |
| padding | int | 4 | 内边距 |

### labelTimeout

标签输入超时时间（毫秒）。

```json
{
  "labelTimeout": 2000
}
```

## 过滤设置

### controlFilter

控件过滤规则。

```json
{
  "controlFilter": {
    "minWidth": 10,
    "minHeight": 10,
    "ignoreDisabled": true,
    "ignoreOffscreen": true,
    "ignoreTypes": ["Pane", "Image"]
  }
}
```

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| minWidth | int | 10 | 最小宽度 |
| minHeight | int | 10 | 最小高度 |
| ignoreDisabled | bool | true | 忽略禁用控件 |
| ignoreOffscreen | bool | true | 忽略屏幕外控件 |
| ignoreTypes | array | [] | 忽略的控件类型 |

### windowFilter

窗口过滤规则。

```json
{
  "windowFilter": {
    "ignoreMinimized": true,
    "ignoreNoTitle": false,
    "ignoreClasses": ["Shell_TrayWnd", "Progman"]
  }
}
```

## 点击设置

### clickMode

点击模式设置。

```json
{
  "clickMode": {
    "method": "simulate",
    "moveMouse": true,
    "restorePosition": true
  }
}
```

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| method | string | simulate | 点击方式：simulate/invoke |
| moveMouse | bool | true | 是否移动鼠标 |
| restorePosition | bool | true | 是否恢复鼠标位置 |

## 性能设置

### performance

性能相关设置。

```json
{
  "performance": {
    "scanInterval": 100,
    "maxControls": 500,
    "cacheEnabled": true
  }
}
```

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| scanInterval | int | 100 | 扫描间隔（毫秒） |
| maxControls | int | 500 | 最大控件数量 |
| cacheEnabled | bool | true | 是否启用缓存 |

## API 设置

### api

HTTP API 设置。

```json
{
  "api": {
    "enabled": true,
    "port": 8080,
    "host": "localhost",
    "cors": true
  }
}
```

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| enabled | bool | true | 是否启用 API |
| port | int | 8080 | 监听端口 |
| host | string | localhost | 监听地址 |
| cors | bool | true | 是否启用 CORS |

## 快捷键设置

### hotkeys

快捷键设置。

```json
{
  "hotkeys": {
    "showLabels": "Alt+F",
    "hideLabels": "Escape",
    "exit": "Alt+Q"
  }
}
```

## 完整配置示例

```json
{
  "labelStyle": {
    "fontSize": 14,
    "fontFamily": "Consolas",
    "backgroundColor": "#4CAF50",
    "textColor": "#FFFFFF",
    "borderRadius": 4,
    "padding": 4
  },
  "labelTimeout": 2000,
  "controlFilter": {
    "minWidth": 10,
    "minHeight": 10,
    "ignoreDisabled": true,
    "ignoreOffscreen": true,
    "ignoreTypes": []
  },
  "windowFilter": {
    "ignoreMinimized": true,
    "ignoreNoTitle": false,
    "ignoreClasses": ["Shell_TrayWnd", "Progman"]
  },
  "clickMode": {
    "method": "simulate",
    "moveMouse": true,
    "restorePosition": true
  },
  "performance": {
    "scanInterval": 100,
    "maxControls": 500,
    "cacheEnabled": true
  },
  "api": {
    "enabled": true,
    "port": 8080,
    "host": "localhost",
    "cors": true
  },
  "hotkeys": {
    "showLabels": "Alt+F",
    "hideLabels": "Escape",
    "exit": "Alt+Q"
  }
}
```
