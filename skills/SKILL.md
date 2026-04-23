[SKILL.md](https://github.com/user-attachments/files/27015642/SKILL.md)
---
name: "vimina-automation"
description: "Provides expertise on Vimina Windows UI automation tool, its HTTP API, VMA scripting, and AI integration patterns. Invoke when user asks about Vimina usage, automation scripts, API calls, or comparing UI automation tools."
---

# Vimina Automation Expert

## Overview

Vimina is a Windows desktop automation tool inspired by the Vimium browser extension. It uses the FlaUI framework with UIA3 protocol to identify interactive controls in application windows, generating two-letter labels for keyboard-based precise clicking. It also provides a complete HTTP API for external integration with AI assistants and automation scripts.

**Key Characteristics:**
- **Platform**: Windows 10/11 only
- **Runtime**: .NET Framework 4.6.2+
- **Architecture**: Native Windows UI Automation (UIA3) via FlaUI
- **API Server**: HTTP REST API on port 51401 (default)
- **Script Engine**: Custom VMA language, compilable to .exe
- **Cost**: Completely free, open-source (MIT)

## Core Capabilities

### 1. Smart Label System
- Auto-detects interactive controls (Button, Edit, MenuItem, CheckBox, etc.)
- Generates two-letter labels (DJ, DK, DL...)
- Real-time visual feedback with color coding
- Keyboard-only operation: Alt+F to show labels, type letters to click

### 2. HTTP API (Port 51401)
Key endpoints:
- `POST /api/show` - Scan and display labels on foreground window
- `GET /api/scan` - Get scan results with control metadata
- `POST /api/click` - Click by label (`{"label": "DJ"}`)
- `GET /api/click/{x}/{y}` - Coordinate click
- `POST /api/clickAt` - Flexible coordinate click with backend support
- `GET /api/windows` - List all windows
- `GET /api/scanByTitle?title=xxx` - Scan window by title
- `POST /api/input` - Send keyboard text
- `POST /api/vma/run` - Execute VMA script content
- `POST /api/vma/runFile` - Execute VMA script file

### 3. VMA Script Language
Custom scripting language (.vma files) supporting:
- Variables, arrays, functions
- Control flow: if/else, loop, for, while, foreach
- Mouse: click, rightClick, doubleClick, drag, moveTo
- Keyboard: input, keyPress, keyDown, keyUp
- Window: activate, findWindow, closeWindow, minimize/maximize
- Logic: waitFor, windowExists, windowActive
- Utility: sleep, log, screenshot, rand

**Compilation**: VMA scripts can be compiled to standalone .exe files via the script editor or command line.

### 4. Backend Operations
- Click without moving mouse cursor
- Operate background windows without bringing to foreground
- Window title-based scanning and clicking
- Essential for automation that doesn't interrupt user workflow

## AI Integration: Replace Visual Models with Vimina

### Why Use Vimina Instead of Visual Models?

When an AI needs to interact with a Windows GUI, there are two approaches:

**Approach 1: Visual Model (Screenshot-based)**
- Take screenshot → Upload to AI vision model → AI guesses coordinates → Execute click
- Problems: Expensive (per-image cost), slow (API latency), inaccurate (pixel-level guessing), privacy risk (screenshots leave machine)

**Approach 2: Vimina API (Control-based) - RECOMMENDED**
- Call Vimina API → Get structured control data → AI reads exact metadata → Execute precise click
- Advantages: Free execution, millisecond response, 100% accuracy, no screenshots uploaded, deterministic results

### How AI Should Use Vimina

When the user asks the AI to perform an action on a Windows application, the AI should:

1. **First, check if Vimina is available** (port 51401 is accessible)
2. **Use Vimina to scan the window** instead of asking for a screenshot
3. **Read the structured control data** to understand the UI
4. **Execute precise operations** via Vimina API

### Step-by-Step AI Workflow

```python
import requests

BASE = "http://localhost:51401"

# Step 1: Ensure Vimina is running
try:
    status = requests.get(f"{BASE}/api/status", timeout=2).json()
    if not status.get("running"):
        print("Vimina is not running. Please start Vimina.exe first.")
except:
    print("Cannot connect to Vimina. Ensure Vimina.exe is running.")

# Step 2: Scan the target window
# Option A: Scan foreground window
requests.post(f"{BASE}/api/show")

# Option B: Scan by window title (background window)
requests.get(f"{BASE}/api/scanByTitle?title=记事本")

# Step 3: Get scan results with full control metadata
result = requests.get(f"{BASE}/api/scan").json()

# Step 4: AI analyzes the controls data
# The response contains rich metadata for AI to understand the UI
```

### Scan Result Format (What AI Receives)

The `/api/scan` endpoint returns structured JSON that replaces visual understanding:

```json
{
  "success": true,
  "timestamp": "2026-04-23T10:30:00",
  "summary": {
    "totalControls": 15,
    "description": "Window「记事本」has 15 interactive controls"
  },
  "quickReference": [
    "DJ: 文件 (MenuItem)",
    "DK: 编辑 (MenuItem)",
    "DL: 保存 (Button)",
    "DM: 格式 (MenuItem)",
    "DN: 查看 (MenuItem)",
    "DO: 帮助 (MenuItem)"
  ],
  "controls": [
    {
      "label": "DJ",
      "name": "文件",
      "type": "MenuItem",
      "x": 10,
      "y": 5,
      "width": 40,
      "height": 20,
      "enabled": true,
      "visible": true
    },
    {
      "label": "DL",
      "name": "保存",
      "type": "Button",
      "x": 200,
      "y": 100,
      "width": 80,
      "height": 30,
      "enabled": true,
      "visible": true
    }
  ]
}
```

**Key fields for AI:**
- `quickReference`: Human-readable list of all controls with labels, names, and types
- `controls[].label`: Two-letter code to click this control
- `controls[].name`: Human-readable name (may be in Chinese or English)
- `controls[].type`: Control type (Button, Edit, MenuItem, etc.)
- `controls[].x/y/width/height`: Exact pixel coordinates
- `controls[].enabled`: Whether the control is clickable
- `controls[].visible`: Whether the control is visible

### AI Decision Process Using Vimina

```
User: "Click the Save button in Notepad"

AI Internal Process:
1. Call POST /api/show (or /api/scanByTitle?title=记事本)
2. Call GET /api/scan
3. Parse quickReference: ["DJ: 文件 (MenuItem)", "DK: 编辑 (MenuItem)", "DL: 保存 (Button)", ...]
4. Match "Save" → "保存" → label "DL"
5. Call POST /api/click with {"label": "DL"}
6. Return success to user
```

### Complete API Examples for AI

#### Click by Label (Recommended)
```python
# Left click
requests.post("http://localhost:51401/api/click", json={"label": "DL"})

# Right click
requests.post("http://localhost:51401/api/click", json={"label": "DL", "right": True})

# Double click
requests.post("http://localhost:51401/api/click", json={"label": "DL", "double": True})
```

#### Click by Coordinates (Backend Mode)
```python
# Backend click (doesn't move mouse, doesn't activate window)
requests.post("http://localhost:51401/api/clickAt", json={
    "x": 500,
    "y": 300,
    "useBackend": True,
    "right": False,
    "double": False
})
```

#### Click by Window Title (Background Window)
```python
# Click in a background window without bringing it to front
requests.post("http://localhost:51401/api/clickByTitle", json={
    "title": "记事本",
    "x": 100,
    "y": 200,
    "useBackend": True,
    "bringToFront": False
})
```

#### Input Text
```python
# Send text to currently focused control
requests.post("http://localhost:51401/api/input", json={"text": "Hello World"})
```

#### Window Management
```python
# Get all windows
windows = requests.get("http://localhost:51401/api/windows").json()

# Activate window
requests.get("http://localhost:51401/api/activate?title=记事本")
```

#### Execute VMA Script
```python
# Run script directly
script = """
click(500, 300)
sleep(1000)
input("Automated text")
log("Done")
"""
requests.post("http://localhost:51401/api/vma/run", json={"script": script})

# Run script file
requests.post("http://localhost:51401/api/vma/runFile", json={
    "file": "C:\\path\\to\\script.vma"
})
```

## VMA Script Complete Reference

### Syntax Overview

```vma
// Variables
var count = 10
var name = "Vimina"
var flag = true

// Arrays (1-indexed)
array items = [1, 2, 3, 4, 5]
var first = items[1]  // 1
push(items, 6)
var len = length(items)

// Control Flow
if count > 5
    log("Greater than 5")
else
    log("5 or less")
end

loop(3)
    click(100, 100)
    sleep(200)
endloop

for i = 1 to 10
    log(i)
end

while count > 0
    count = count - 1
end

// Functions
function add(a, b)
    return a + b
endfunction

var result = add(10, 20)  // 30
```

### Mouse Functions

| Function | Description | Example |
|----------|-------------|---------|
| `click(x, y)` | Left click at coordinates | `click(500, 300)` |
| `click(x, y, useBackend=1)` | Backend left click | `click(500, 300, useBackend=1)` |
| `rightClick(x, y)` | Right click | `rightClick(600, 400)` |
| `doubleClick(x, y)` | Double click | `doubleClick(100, 100)` |
| `drag(x1, y1, x2, y2)` | Drag operation | `drag(100, 100, 500, 500)` |
| `moveTo(x, y)` | Move mouse | `moveTo(300, 300)` |

### Keyboard Functions

| Function | Description | Example |
|----------|-------------|---------|
| `input(text)` | Type text | `input("Hello World")` |
| `keyPress(key)` | Press key/combo | `keyPress("Ctrl+A")` |
| `keyDown(key)` | Hold key | `keyDown("Alt")` |
| `keyUp(key)` | Release key | `keyUp("Alt")` |

### Window Functions

| Function | Description | Example |
|----------|-------------|---------|
| `activate(title)` | Bring window to front | `activate("记事本")` |
| `findWindow(title)` | Find window handle | `var hwnd = findWindow("记事本")` |
| `closeWindow(title)` | Close window | `closeWindow("记事本")` |
| `minimizeWindow(title)` | Minimize window | `minimizeWindow("记事本")` |
| `maximizeWindow(title)` | Maximize window | `maximizeWindow("记事本")` |
| `windowExists(title)` | Check existence | `if windowExists("记事本")` |
| `waitFor(title, timeout=30)` | Wait for window | `waitFor("新窗口", timeout=60)` |

### Scanning Functions

| Function | Description | Example |
|----------|-------------|---------|
| `scan()` | Scan foreground window | `scan()` |
| `scanWindow(title)` | Scan by title | `scanWindow("记事本")` |
| `clickLabel(label)` | Click by label | `clickLabel("DJ")` |
| `show()` | Show labels | `show()` |
| `hide()` | Hide labels | `hide()` |

### Utility Functions

| Function | Description | Example |
|----------|-------------|---------|
| `sleep(ms)` | Pause execution | `sleep(1000)` |
| `log(msg)` | Output log | `log("Done")` |
| `msg(text)` | Message box | `msg("Alert")` |
| `screenshot()` | Take screenshot | `screenshot()` |
| `getMousePos()` | Get mouse position | `var pos = getMousePos()` |
| `getScreenSize()` | Get screen dimensions | `var size = getScreenSize()` |
| `rand(min, max)` | Random number | `var r = rand(1, 100)` |

### Math Functions

| Function | Description | Example |
|----------|-------------|---------|
| `abs(x)` | Absolute value | `abs(-5)` → 5 |
| `floor(x)` | Round down | `floor(3.7)` → 3 |
| `ceil(x)` | Round up | `ceil(3.2)` → 4 |
| `min(a, b, ...)` | Minimum | `min(1, 5, 3)` → 1 |
| `max(a, b, ...)` | Maximum | `max(1, 5, 3)` → 5 |
| `toInt(x)` | Convert to int | `toInt("123")` → 123 |
| `toString(x)` | Convert to string | `toString(123)` → "123" |

## AI Integration Patterns

### Pattern 1: Direct API Control
AI calls Vimina HTTP API to perform precise UI operations:

```python
import requests

BASE = "http://localhost:51401"

# Step 1: Scan current window
requests.post(f"{BASE}/api/show")
result = requests.get(f"{BASE}/api/scan").json()

# Step 2: AI analyzes quickReference to find target
# ["DJ: File (MenuItem)", "DK: Edit (MenuItem)", ...]

# Step 3: AI clicks precise control
requests.post(f"{BASE}/api/click", json={"label": "DJ"})
```

### Pattern 2: AI + Vimina Hybrid (Recommended)
AI understands natural language intent, Vimina executes precisely:
- **AI responsibility**: Understand user intent, plan steps, choose targets
- **Vimina responsibility**: Precise control execution (100% deterministic)
- **Cost**: AI only for understanding (~$0.001 per task), execution is free
- **Privacy**: No screenshots uploaded, only text metadata exchanged

### Pattern 3: VMA Script Automation
Write reusable automation scripts:

```vma
// Example: Open Notepad, type text, save
activate("记事本")
sleep(500)
input("Hello World")
keyPress("Ctrl+S")
sleep(300)
input("document.txt")
keyPress("Enter")
log("Task completed")
```

## Comparison with Similar Tools

| Tool | Recognition | Backend Click | HTTP API | AI Integration | Cost | Platform |
|------|-------------|---------------|----------|----------------|------|----------|
| **Vimina** | UIA3 (control-level) | Native | Full REST | Tool Use / API | Free | Windows |
| OpenClaw | Visual + DOM | Via Skills | Limited | Native (MCP) | Free + API | Multi |
| UI Automata | UIA3 + CDP | Yes | MCP Server | MCP Native | Free | Windows |
| AskUI | Visual (pixel) | Yes | Yes | Native | Paid | Multi |
| Codex Computer Use | Visual (AI) | No | Proprietary | Native | Per-use | Cloud |
| Bytebot | Visual (container) | Yes | Yes | Native | Free + API | Linux |
| Ui.Vision | CV/OCR + Selenium | Yes | Limited | Claude API | Free | Multi |

### Vimina's Unique Advantages
1. **100% deterministic control-level recognition** (not visual guess)
2. **Zero cost execution** (no API fees for operations)
3. **True backend clicking** (no mouse movement, no focus steal)
4. **Compile scripts to .exe** (distributable automation)
5. **Privacy-safe** (no screenshots leave the machine)
6. **AI-friendly API** (quickReference gives semantic context to AI)

## Configuration

Config file: `config.json` in program directory

```json
{
    "UseMouseClick": false,
    "BringToFront": true,
    "UseFlaUIClick": true,
    "ClickDelay": 30,
    "MinWidth": 8,
    "MinHeight": 8,
    "MaxDepth": 50,
    "BackgroundColor_Default": "0x00DDFF",
    "BackgroundColor_Match": "0x00FF00",
    "BackgroundColor_Prefix": "0x00A5FF",
    "BackgroundColor_Invalid": "0x808080",
    "TextColor": "0x000000",
    "FontSize": 12,
    "FontWeight": 700,
    "OffsetX": 0,
    "OffsetY": 18
}
```

Key settings:
- `UseMouseClick: false` → Backend click (recommended for automation)
- `BringToFront: false` → Don't activate window (true background operation)
- `MaxDepth` → Control tree traversal depth (lower = faster scan)

## Common Use Cases

1. **Keyboard-only app control** - Eliminate mouse for common actions
2. **RPA (Robotic Process Automation)** - Automate repetitive desktop tasks
3. **AI agent tool** - Give AI precise hands on Windows GUI
4. **Testing automation** - Scripted UI testing with deterministic clicks
5. **Accessibility aid** - Keyboard navigation for mouse-impaired users

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Alt+F no response | Ensure window is focused; some UWP apps lack UIA support |
| Labels misplaced | Adjust `OffsetX`/`OffsetY` in config.json |
| Scan too slow | Decrease `MaxDepth`, increase `MinWidth`/`MinHeight` |
| Control not found | Increase `MaxDepth`; some custom controls lack UIA |
| Backend click fails | Try `UseMouseClick: true` for that specific control |
| Browser activation fails | Use backend click without bringing to front |

## Resources

- GitHub: https://github.com/Sunse666/Vimina
- Documentation: Built-in `html/docs/` (MkDocs format)
- VMA Spec: `docs/VMA-Specification.md`
- Comparison docs: `vsCodex.md`, `vsOpenClaw.md`, `compare.md`
