# VMA 脚本执行

通过 HTTP API 执行 VMA 脚本。

## 执行脚本

```http
POST /api/vma/run
Content-Type: application/json

{
  "script": "click(100, 100)\nsleep(500)\ntype(\"Hello\")"
}
```

**响应：**

```json
{
  "success": true,
  "output": "Script executed successfully"
}
```

---

## 参数说明

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| script | string | 是 | VMA 脚本内容 |

---

## 执行示例

=== "Python"

    ```python
    import requests
    
    script = """
    // 自动登录示例
    click(200, 150)
    sleep(100)
    type("admin")
    click(200, 200)
    sleep(100)
    type("password123")
    click(200, 250)
    """
    
    response = requests.post(
        "http://localhost:8080/api/vma/run",
        json={"script": script}
    )
    print(response.json())
    ```

=== "JavaScript"

    ```javascript
    const script = `
    // 自动登录示例
    click(200, 150)
    sleep(100)
    type("admin")
    click(200, 200)
    sleep(100)
    type("password123")
    click(200, 250)
    `;
    
    fetch("http://localhost:8080/api/vma/run", {
      method: "POST",
      headers: {"Content-Type": "application/json"},
      body: JSON.stringify({script})
    })
      .then(r => r.json())
      .then(console.log);
    ```

---

## 与 AI 助手集成

VMA 脚本非常适合与 AI 助手集成：

```python
# AI 助手生成脚本并执行
def ai_automate(user_request):
    # AI 理解用户意图，生成 VMA 脚本
    script = ai_generate_script(user_request)
    
    # 执行脚本
    response = requests.post(
        "http://localhost:8080/api/vma/run",
        json={"script": script}
    )
    
    return response.json()

# 示例
ai_automate("打开记事本，输入 Hello World，然后保存到桌面")
```

更多 VMA 语法请参考 [VMA 脚本语法](../vma/index.md)。
