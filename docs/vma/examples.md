# 脚本示例

## 自动登录

```vma
// 自动登录示例
// 假设登录窗口已打开

// 点击用户名输入框
click(200, 150)
sleep(100)

// 输入用户名
type("admin")

// 点击密码输入框
click(200, 200)
sleep(100)

// 输入密码
type("password123")

// 点击登录按钮
click(200, 250)

log("登录完成")
```

---

## 批量处理

```vma
// 批量处理 10 个项目
for i = 1 to 10
    // 点击项目
    click(100, 50 + i * 30)
    sleep(500)
    
    // 处理
    click(300, 200)
    sleep(1000)
    
    // 确认
    key("enter")
    sleep(500)
end

log("批量处理完成")
```

---

## 定时任务

```vma
// 每隔 5 分钟执行一次任务
loop 12  // 执行 12 次，共 1 小时
    // 执行任务
    click(100, 100)
    sleep(500)
    type("检查完成")
    key("enter")
    
    // 等待 5 分钟
    sleep(300000)
endloop

log("定时任务完成")
```

---

## 窗口管理

```vma
// 检查窗口是否存在，不存在则打开
if not windowExists("记事本")
    // 打开记事本（通过 Win+R）
    key("win+r")
    sleep(500)
    type("notepad")
    key("enter")
    sleep(1000)
end

// 激活窗口
activateWindow("记事本")

// 输入内容
type("Hello, Vimina!")

// 保存文件
key("ctrl+s")
sleep(500)
type("test.txt")
key("enter")

log("操作完成")
```

---

## 随机操作

```vma
// 随机点击位置（模拟人工操作）
loop 10
    var x = random(100, 500)
    var y = random(100, 400)
    
    click(x, y)
    
    // 随机等待 0.5-2 秒
    var delay = random(500, 2000)
    sleep(delay)
endloop

log("随机操作完成")
```

---

## 截图监控

```vma
// 定时截图监控
var count = 0

loop 60  // 监控 1 小时（每分钟一次）
    count = count + 1
    
    // 截图保存
    var filename = "screenshot_" + count + ".png"
    screenshot(filename)
    
    log("已保存: " + filename)
    
    // 等待 1 分钟
    sleep(60000)
endloop

log("监控完成")
```

---

## 拖拽操作

```vma
// 拖拽文件
// 从文件列表拖拽到目标位置

// 点击选中文件
click(100, 200)
sleep(100)

// 拖拽到目标位置
drag(100, 200, 500, 300)
sleep(200)

// 释放
key("enter")

log("拖拽完成")
```

---

## 表单填写

```vma
// 自动填写表单

// 填写姓名
click(100, 100)
type("张三")

// 填写邮箱
click(100, 150)
type("zhangsan@example.com")

// 填写电话
click(100, 200)
type("13800138000")

// 选择性别（假设是下拉框）
click(100, 250)
key("down")
key("enter")

// 勾选同意条款
click(100, 300)

// 提交表单
click(100, 350)

log("表单填写完成")
```

---

## 与 AI 助手配合

```vma
// AI 助手生成的自动化脚本
// 用户请求: "帮我整理桌面文件"

// 打开文件管理器
key("win+e")
sleep(1000)

// 导航到桌面
key("ctrl+l")
sleep(200)
type("桌面")
key("enter")
sleep(500)

// 选择所有文件
key("ctrl+a")
sleep(200)

// 创建新文件夹
key("ctrl+shift+n")
sleep(200)
type("整理后的文件")
key("enter")
sleep(200)

// 移动文件
key("ctrl+x")
sleep(200)
dblclick(100, 100)  // 进入新文件夹
sleep(200)
key("ctrl+v")

log("桌面文件整理完成")
```
