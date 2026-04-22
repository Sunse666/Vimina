# 语法参考

## 注释

```vma
// 单行注释

/*
多行注释
可以跨越多行
*/
```

## 变量

### 声明变量

```vma
var name = "Vimina"
var count = 10
var pi = 3.14
var enabled = true
```

### 显式类型

```vma
var count: int = 10
var name: string = "Vimina"
var pi: float = 3.14
var enabled: bool = true
```

### 变量赋值

```vma
var x = 10
x = 20          // 重新赋值
x = x + 5       // 运算赋值
```

---

## 运算符

### 算术运算符

| 运算符 | 说明 | 示例 |
|--------|------|------|
| `+` | 加法 | `a + b` |
| `-` | 减法 | `a - b` |
| `*` | 乘法 | `a * b` |
| `/` | 除法 | `a / b` |
| `%` | 取模 | `a % b` |

### 比较运算符

| 运算符 | 说明 | 示例 |
|--------|------|------|
| `==` | 等于 | `a == b` |
| `!=` | 不等于 | `a != b` |
| `>` | 大于 | `a > b` |
| `<` | 小于 | `a < b` |
| `>=` | 大于等于 | `a >= b` |
| `<=` | 小于等于 | `a <= b` |

### 逻辑运算符

| 运算符 | 说明 | 示例 |
|--------|------|------|
| `and` | 与 | `a and b` |
| `or` | 或 | `a or b` |
| `not` | 非 | `not a` |

---

## 控制流

### 条件判断

```vma
if condition
    // 代码块
else
    // 代码块
end
```

**示例：**

```vma
var count = 10

if count > 5
    log("大于5")
else
    log("小于等于5")
end
```

**多条件：**

```vma
var score = 85

if score >= 90
    log("优秀")
else if score >= 60
    log("及格")
else
    log("不及格")
end
```

---

### Loop 循环

固定次数循环：

```vma
loop 5
    log("执行一次")
endloop
```

---

### For 循环

```vma
for i = 1 to 10
    log(i)
end
```

**步长：**

```vma
for i = 1 to 10 step 2
    log(i)  // 1, 3, 5, 7, 9
end
```

---

### While 循环

```vma
var count = 0
while count < 10
    log(count)
    count = count + 1
end
```

---

### Break 和 Continue

```vma
for i = 1 to 10
    if i == 5
        break      // 跳出循环
    end
    log(i)
end
```

```vma
for i = 1 to 10
    if i == 5
        continue   // 跳过本次循环
    end
    log(i)
end
```

---

## 函数

### 定义函数

```vma
function greet(name)
    log("Hello, " + name)
end
```

### 调用函数

```vma
greet("Vimina")
```

### 返回值

```vma
function add(a, b)
    return a + b
end

var result = add(1, 2)
log(result)  // 输出: 3
```

---

## 字符串操作

### 字符串连接

```vma
var s1 = "Hello"
var s2 = "World"
var s3 = s1 + " " + s2  // "Hello World"
```

### 字符串函数

```vma
var s = "Hello World"

var length = len(s)           // 11
var sub = substr(s, 0, 5)     // "Hello"
var parts = split(s, " ")     // ["Hello", "World"]
```

---

## 数组

### 创建数组

```vma
var arr = [1, 2, 3, 4, 5]
var names = ["Alice", "Bob", "Charlie"]
```

### 访问元素

```vma
var first = arr[0]    // 1
var second = arr[1]   // 2
```

### 遍历数组

```vma
for item in arr
    log(item)
end
```
