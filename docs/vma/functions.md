# 内置函数

## 鼠标函数

### click(x, y)

左键点击指定坐标。

```vma
click(100, 200)
```

---

### clickR(x, y)

右键点击指定坐标。

```vma
clickR(100, 200)
```

---

### dblclick(x, y)

双击指定坐标。

```vma
dblclick(100, 200)
```

---

### move(x, y)

移动鼠标到指定坐标。

```vma
move(500, 300)
```

---

### drag(fromX, fromY, toX, toY)

从起点拖拽到终点。

```vma
drag(100, 100, 300, 300)
```

---

### scroll(delta)

滚动鼠标滚轮。

```vma
scroll(1)    // 向上滚动
scroll(-1)   // 向下滚动
```

---

### getMousePos()

获取当前鼠标位置。

```vma
var pos = getMousePos()
log(pos.x)   // X 坐标
log(pos.y)   // Y 坐标
```

---

## 键盘函数

### type(text)

输入文本字符串。

```vma
type("Hello World")
```

---

### key(keyName)

发送按键或组合键。

```vma
key("enter")
key("ctrl+c")
key("alt+tab")
```

---

### keyDown(keyName)

按下按键（不释放）。

```vma
keyDown("ctrl")
```

---

### keyUp(keyName)

释放按键。

```vma
keyUp("ctrl")
```

---

## 窗口函数

### activateWindow(title)

激活指定标题的窗口。

```vma
activateWindow("记事本")
```

---

### closeWindow(title)

关闭指定标题的窗口。

```vma
closeWindow("记事本")
```

---

### minimizeWindow(title)

最小化指定标题的窗口。

```vma
minimizeWindow("记事本")
```

---

### maximizeWindow(title)

最大化指定标题的窗口。

```vma
maximizeWindow("记事本")
```

---

### windowExists(title)

检查窗口是否存在。

```vma
if windowExists("记事本")
    log("窗口存在")
end
```

---

### getWindowList()

获取所有窗口列表。

```vma
var windows = getWindowList()
for win in windows
    log(win.title)
end
```

---

## 控件函数

### clickTag(tag)

点击指定标签的控件。

```vma
clickTag("AB")
```

---

### getControlByTag(tag)

获取指定标签的控件信息。

```vma
var ctrl = getControlByTag("AB")
log(ctrl.name)
log(ctrl.bounds.x)
```

---

### getControlList()

获取当前窗口的所有控件列表。

```vma
var controls = getControlList()
for ctrl in controls
    log(ctrl.tag + ": " + ctrl.name)
end
```

---

## 时间函数

### sleep(ms)

暂停执行指定毫秒数。

```vma
sleep(1000)  // 暂停 1 秒
```

---

### getTimestamp()

获取当前时间戳（毫秒）。

```vma
var ts = getTimestamp()
log(ts)
```

---

## 日志函数

### log(message)

输出日志消息。

```vma
log("操作完成")
log("计数: " + count)
```

---

## 截图函数

### screenshot()

截取全屏。

```vma
screenshot()
```

---

### screenshot(filename)

截取全屏并保存到文件。

```vma
screenshot("screen.png")
```

---

### screenshot(x, y, width, height)

截取指定区域。

```vma
screenshot(100, 100, 400, 300)
```

---

## 随机函数

### random(min, max)

生成指定范围内的随机整数。

```vma
var r = random(1, 100)
log(r)
```

---

### randomFloat()

生成 0-1 之间的随机浮点数。

```vma
var r = randomFloat()
log(r)
```

---

## 数学函数

### abs(x)

绝对值。

```vma
var a = abs(-5)  // 5
```

---

### floor(x)

向下取整。

```vma
var f = floor(3.7)  // 3
```

---

### ceil(x)

向上取整。

```vma
var c = ceil(3.2)  // 4
```

---

### round(x)

四舍五入。

```vma
var r = round(3.5)  // 4
```

---

### sqrt(x)

平方根。

```vma
var s = sqrt(16)  // 4
```

---

### pow(x, y)

幂运算。

```vma
var p = pow(2, 3)  // 8
```

---

## 系统函数

### beep()

发出系统提示音。

```vma
beep()
```

---

### exit()

终止脚本执行。

```vma
if error
    exit()
end
```

---

### getVersion()

获取 Vimina 版本号。

```vma
var v = getVersion()
log(v)
```
