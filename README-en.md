# Vimina
<p align="center"> <img src="assets/logo/logo.png" alt="Vimina Logo" width="128"> </p><p align="center"> <strong>Control Windows Applications with Your Keyboard</strong> </p><p align="center"> <img src="https://img.shields.io/badge/Platform-Windows-blue?style=flat-square" alt="Platform"> <img src="https://img.shields.io/badge/Engine-FlaUI-orange?style=flat-square" alt="Engine"> <img src="https://img.shields.io/badge/Inspired_by-Vimium-green?style=flat-square" alt="Inspired"> </p>


## ✨ Introduction

Inspired by the browser extension Vimium, Vimina is a Windows application that replicates its functionality. Using the FlaUI automation framework, it identifies interactive controls in windows and generates unique letter labels for each control. Simply press the keyboard to precisely click any button, link, or input field. It also provides an HTTP API interface for external program calls, enabling easy integration with AI assistants and automation scripts. This completely liberates you from the mouse and boosts efficiency for keyboard enthusiasts.


## 🖼️ Screenshots

### Usage Screenshot

<p align="center"> <img src="assets/Screenshot.png" alt="Screenshot"> </p>

<p align="center"> <img src="assets/Screenshot2.png" alt="Screenshot"> </p>

### Configuration Interface

<p align="center"> <img src="assets/config.png" alt="Config Screenshot"> </p>

### Script Editor

<p align="center"> <img src="assets/script.png" alt="Script Screenshot"> </p>


## 🚀 Features

### 🏷️ Smart Label System
- Automatically identifies interactive controls in windows
- Generates easy-to-type two-letter labels for each control
- Smart label positioning to avoid covering controls

### 🎨 Real-time Visual Feedback
- Yellow—Default state, waiting for input
- Orange—Prefix match, continue typing
- Green—Full match, about to click
- Gray—Invalid label, filtered out

### ⌨️ Keyboard Shortcuts

> [!TIP]
> |Shortcut|Function|
> |---|---|
> |Alt + F|Show / Hide labels|
> |Alt + R|Refresh labels|
> |Esc|Clear all labels|
> |A-Z|Enter label letters|
> |Backspace|Delete entered characters|

### 🎯 Supported Control Types
- Button, CheckBox, RadioButton
- ComboBox, Edit, Slider, Spinner
- Hyperlink, MenuItem, ListItem
- TabItem, TreeItem, ToolBar
- DataItem, SplitButton

### 📌 System Tray
- Automatically minimizes to tray on startup
- Left-click to show main window
- Right-click menu for quick access to configuration and script features

### 🌐 HTTP API Service
- Built-in lightweight HTTP server, default port 51401
- Complete RESTful interface
- Supports control scanning, label clicking, coordinate operations, text input
- Returns JSON format data for AI analysis and program calls
- Supports CORS cross-origin access

### 📜 VMA Script Support
- Supports writing automation script files (.vma)
- Import and execute scripts via right-click menu
- Compile scripts into standalone executable files (.exe)
- Built-in rich script commands, including:
  - Delays, mouse clicks, keyboard operations
  - Window management, control scanning
  - Conditional statements, loop control
  - Variable assignment, log output

### 📝 Script Editor
- Open via right-click menu "Script" or main window "Script" button
- Built-in VMA script editing environment with syntax highlighting
- Supports creating, opening, and saving script files (.vma)
- **Get Position** button: Click and move mouse to automatically get coordinates and insert code
- Supports one-click script execution
- Built-in command prompt dropdown with auto-completion
- **Compile feature**: Compile scripts into standalone .exe files that run without any dependencies

### 🔒 Background Operation Support
- Supports click operations without moving the mouse
- Supports operating background windows without switching focus
- Scan and click controls via window title
- Get all window list, excluding system applications


## 🌐 HTTP API

Vimina has a built-in HTTP server that automatically runs on `http://localhost:51401`

### API Overview

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api` | API information and help |
| GET | `/api/scan` | Get scan results (interactive controls only) |
| GET | `/api/scanAll` | Scan all controls (including text, images, etc.) |
| GET | `/api/scanAllByTitle?title=xxx` | Scan all controls by window title |
| POST | `/api/scanAllByTitle` | Scan all controls by window title |
| POST | `/api/show` | Show labels and scan |
| POST | `/api/hide` | Hide labels |
| POST | `/api/click` | Click control by label |
| POST | `/api/click?right=1` | Right-click by label |
| POST | `/api/click?double=1` | Double-click by label |
| GET | `/api/click/{x}/{y}` | Click at coordinates |
| GET | `/api/click/{x}/{y}?useBackend=1` | Background click at coordinates |
| GET | `/api/clickR/{x}/{y}` | Right-click at coordinates |
| GET | `/api/clickR/{x}/{y}?useBackend=1` | Background right-click at coordinates |
| GET | `/api/dblclick/{x}/{y}` | Double-click at coordinates |
| GET | `/api/dblclick/{x}/{y}?useBackend=1` | Background double-click at coordinates |
| GET | `/api/clickAt?x=&y=&useBackend=1` | Click at coordinates (supports background) |
| POST | `/api/clickAt` | Click at coordinates (supports background) |
| GET | `/api/windows` | Get all window list |
| GET | `/api/scanByTitle?title=xxx` | Scan controls by window title (interactive only) |
| POST | `/api/scanByTitle` | Scan controls by window title (interactive only) |
| GET | `/api/clickByTitle?title=xxx&x=&y=` | Click control by window title (supports background) |
| POST | `/api/clickByTitle` | Click control by window title (supports background) |
| GET | `/api/activate?title=xxx` | Activate window (bring to foreground) |
| GET | `/api/mouse` | Get current mouse position |
| GET | `/api/move/{x}/{y}` | Move mouse |
| GET | `/api/drag/{x1}/{y1}/{x2}/{y2}` | Drag operation |
| POST | `/api/input` | Simulate keyboard text input |
| GET | `/api/status` | Get service status |
| POST | `/api/vma/run` | Run VMA script |
| POST | `/api/vma/runFile` | Run VMA script file |
| GET | `/api/vma/status` | Get VMA script running status |
| POST | `/api/vma/stop` | Stop running VMA script |
| GET | `/api/raw/scan` | Get raw scan results JSON |
| GET | `/api/raw/labels` | Get raw label mapping JSON |

### API Details

#### Scan Controls

```bash
# Trigger scan and show labels
curl -X POST http://localhost:51401/api/show

# Get scan results (interactive controls only)
curl http://localhost:51401/api/scan

# Scan all controls (including text, images, and other non-interactive elements)
curl http://localhost:51401/api/scanAll

# Scan all controls by window title
curl "http://localhost:51401/api/scanAllByTitle?title=Notepad"

# POST method to scan all controls
curl -X POST http://localhost:51401/api/scanAllByTitle \
  -H "Content-Type: application/json" \
  -d '{"title": "Notepad"}'
```

Response example:

```JSON
{
  "success": true,
  "timestamp": "...",
  "window": {
    "title": "Notepad",
    "className": "Notepad",
    "handle": 123456
  },
  "summary": {
    "totalControls": 45,
    "byType": {
      "Text": 20,
      "Button": 5,
      "Edit": 3,
      "Image": 2,
      "Pane": 15
    },
    "description": "Window 'Notepad' has 45 controls (including non-interactive controls)"
  },
  "controls": [
    {
      "name": "File",
      "type": "MenuItem",
      "typeDesc": "Menu Item",
      "isInteractive": true,
      "x": 10,
      "y": 30,
      "width": 50,
      "height": 20,
      "centerX": 35,
      "centerY": 40,
      "automationId": "",
      "className": "MenuItem",
      "isEnabled": true,
      "isVisible": true
    },
    {
      "name": "Welcome",
      "type": "Text",
      "typeDesc": "Text",
      "isInteractive": false,
      "x": 100,
      "y": 150,
      "width": 200,
      "height": 30,
      "centerX": 200,
      "centerY": 165
    }
  ]
}
```

> [!TIP]
> - `/api/scan` and `/api/scanByTitle` only return interactive controls (buttons, input fields, etc.)
> - `/api/scanAll` and `/api/scanAllByTitle` return all controls, including non-interactive elements like text and images
> - All scan results are saved to the `data/scan_result.json` file

### Click Controls

```bash
# Click by label
curl -X POST http://localhost:51401/api/click \
  -H "Content-Type: application/json" \
  -d '{"label": "DJ"}'

# Right-click by label
curl -X POST http://localhost:51401/api/click \
  -H "Content-Type: application/json" \
  -d '{"label": "DJ", "right": true}'

# Double-click by label
curl -X POST http://localhost:51401/api/click \
  -H "Content-Type: application/json" \
  -d '{"label": "DJ", "double": true}'

# Click at coordinates
curl http://localhost:51401/api/click/500/300

# Background click at coordinates
curl "http://localhost:51401/api/click/500/300?useBackend=1"

# Right-click at coordinates
curl http://localhost:51401/api/clickR/500/300

# Background right-click at coordinates
curl "http://localhost:51401/api/clickR/500/300?useBackend=1"

# Double-click at coordinates
curl http://localhost:51401/api/dblclick/500/300

# Background double-click at coordinates
curl "http://localhost:51401/api/dblclick/500/300?useBackend=1"

# Flexible coordinate click (supports background)
curl "http://localhost:51401/api/clickAt?x=500&y=300&useBackend=1"

# POST method click
curl -X POST http://localhost:51401/api/clickAt \
  -H "Content-Type: application/json" \
  -d '{"x": 500, "y": 300, "useBackend": true, "right": false, "double": false}'
```

### Window Management

```bash
# Activate window (bring to foreground)
curl "http://localhost:51401/api/activate?title=Notepad"

# Get all window list
curl http://localhost:51401/api/windows
```

Response example:

```JSON
{
  "success": true,
  "count": 5,
  "windows": [
    {"hwnd": 123456, "title": "Notepad", "className": "Notepad", "processId": 1234},
    {"hwnd": 789012, "title": "bilibili", "className": "Chrome_WidgetWin_1", "processId": 5678}
  ]
}
```

```bash
# Scan controls by window title, supports partial matching
curl "http://localhost:51401/api/scanByTitle?title=Notepad"

# POST method scan
curl -X POST http://localhost:51401/api/scanByTitle \
  -H "Content-Type: application/json" \
  -d '{"title": "Notepad"}'
```

```bash
# Click control by window title
curl "http://localhost:51401/api/clickByTitle?title=Notepad&x=500&y=300"

# Click and bring to foreground
curl "http://127.0.0.1:51401/api/clickByTitle?title=Notepad&x=500&y=300&bringtofront=1&usebackend=0"

# POST method click
curl -X POST http://localhost:51401/api/clickByTitle \
  -H "Content-Type: application/json" \
  -d '{"title": "Notepad", "x": 500, "y": 300, "useBackend": true, "bringToFront": false}'
```

> [!TIP]
> Background click functionality allows operating other windows without disturbing current work, perfect for automation scripts and AI assistant integration

### Mouse Operations

```bash
# Get mouse position
curl http://localhost:51401/api/mouse

# Move mouse
curl http://localhost:51401/api/move/500/300

# Drag (from 100,100 to 500,300)
curl http://localhost:51401/api/drag/100/100/500/300
```

### Text Input

```bash
curl -X POST http://localhost:51401/api/input \
  -H "Content-Type: application/json" \
  -d '{"text": "Hello World"}'
```

### Status Query

```bash
curl http://localhost:51401/api/status
```

Response example:

```JSON
{
  "running": true,
  "hasData": true,
  "lastScan": "...",
  "mousePosition": {"x": 500, "y": 300},
  "screen": [1920, 1080]
}
```

### VMA Script API

```bash
# Run VMA script content
curl -X POST http://localhost:51401/api/vma/run \
  -H "Content-Type: application/json" \
  -d '{"script": "click(500, 300)\nsleep(1000)\nlog(\"Done\")"}'

# Run VMA script file
curl -X POST http://localhost:51401/api/vma/runFile \
  -H "Content-Type: application/json" \
  -d '{"file": "C:\\path\\to\\script.vma"}'
```

Response example:

```JSON
{
  "success": true,
  "log": ["Done"],
  "linesExecuted": 3
}
```

#### VMA Script Control

```bash
# Get script running status
curl http://localhost:51401/api/vma/status
```

Response example:

```JSON
{
  "running": true,
  "paused": false,
  "currentLine": 15,
  "totalLines": 30,
  "variables": {"x": 10, "y": 20}
}
```

```bash
# Stop running script
curl -X POST http://localhost:51401/api/vma/stop
```

#### Raw Data Endpoints

```bash
# Get raw scan results
curl http://localhost:51401/api/raw/scan

# Get raw label mapping
curl http://localhost:51401/api/raw/labels
```

### AI Integration

Vimina's API is designed to be AI-friendly, with scan results containing rich semantic information:

```Python
# Python example: Let AI operate desktop applications
import requests

# Method 1: Scan interactive controls (suitable for operating buttons, input fields, etc.)
requests.post("http://localhost:51401/api/show")
result = requests.get("http://localhost:51401/api/scan").json()

# Method 2: Scan all controls (suitable for AI to understand window content)
result = requests.get("http://localhost:51401/api/scanAll").json()
# Or scan by title
result = requests.get("http://localhost:51401/api/scanAllByTitle?title=Notepad").json()

# AI can get all text, images, and other information in the window
for ctrl in result["controls"]:
    print(f"{ctrl['type']}: {ctrl['name']} at ({ctrl['x']}, {ctrl['y']})")
    if ctrl['isInteractive']:
        print(f"  -> Interactive control, can be clicked")

# AI decides and clicks target control
requests.post("http://localhost:51401/api/click", json={"label": "DJ"})

# Or click directly at coordinates
requests.get("http://localhost:51401/api/click/500/300")
```

> [!TIP]
> - `/api/scanAll` endpoint is designed for AI, can get all elements in the window
> - Returned control information includes `isInteractive` field, AI can determine which controls can be interacted with
> - Scan results include text content, AI can understand the meaning and context of the window

### 📜 VMA Scripts

Vimina supports writing automation script files (.vma), which can be imported and executed via the right-click menu.

#### Usage

**Method 1: Via Script Editor**
1. Right-click menu select "Script" or click main window "Script" button
2. Write script in the editor
3. Click "Run" button to execute script
4. Click "Compile" button to compile script into standalone .exe file

**Method 2: Via Right-click Menu Import**
1. Right-click inside Vimina window
2. Select "Import VMA Script"
3. Select .vma file to run

**Method 3: Via API Call**
```bash
# Run script content
curl -X POST http://localhost:51401/api/vma/run \
  -H "Content-Type: application/json" \
  -d '{"script": "click(500,300)\nsleep(1000)"}'

# Run script file
curl -X POST http://localhost:51401/api/vma/runFile \
  -H "Content-Type: application/json" \
  -d '{"file": "C:\\path\\to\\script.vma"}'
```

#### Compile Scripts

Vimina supports compiling VMA scripts into standalone executable files:

**Method 1: GUI Compilation**

1. Download vma_runtime.exe and place it in the same directory as Vimina.exe
2. Write script in the script editor
3. Click "Compile" button
4. Select output path and filename
5. The generated .exe file can be sent to others to run directly, without installing Vimina or any other environment

**Method 2: Command Line Compilation**

```bash
# Compile script (output to same directory, automatically named script.exe)
# Note: Use relative path or full path to execute Vimina.exe
.\Vimina.exe script.vma

# Compile script and specify output path
.\Vimina.exe -o output.exe script.vma

# Or use full path
D:\path\to\Vimina.exe script.vma

# Or use full parameters
.\Vimina.exe --compile script.vma output.exe
```

**Command Line Parameters:**

| Parameter | Description |
|-----------|-------------|
| `script.vma` | Path to the script file to compile |
| `-o` or `--output` | Specify output file path |
| `-c` or `--compile` | Explicitly specify compile mode |

> [!NOTE]
> When executing Vimina.exe from command line, use relative path (e.g., `.\Vimina.exe`) or full path (e.g., `D:\path\Vimina.exe`), because Vimina.exe is not in the system PATH environment variable.

> [!TIP]
> Compiled programs contain the complete VMA runtime, supporting all script commands including background clicking, window operations, and more

#### Script Commands

##### Delay

```vma
sleep(1000)  # Delay 1000 milliseconds
```

##### Mouse Operations

```vma
click(500, 300)                    # Left click
click(500, 300, useBackend=1)     # Background left click
rightClick(600, 400)               # Right click
rightClick(600, 400, useBackend=1) # Background right click
doubleClick(100, 100)              # Double click
doubleClick(100, 100, useBackend=1) # Background double click
drag(100, 100, 500, 500)           # Drag
moveTo(300, 300)                   # Move mouse
```

##### Window Operations

```vma
clickByTitle("Notepad", 100, 200)                    # Click by title
clickByTitle("Notepad", 100, 200, useBackend=1)     # Background click
clickByTitle("Notepad", 100, 200, useBackend=0, bringToFront=1) # Click and bring to foreground
activate("Notepad")                                    # Activate window to foreground
var hwnd = findWindow("Notepad")                       # Find window
closeWindow("Notepad")                                 # Close window
minimizeWindow("Notepad")                              # Minimize window
maximizeWindow("Notepad")                              # Maximize window
restoreWindow("Notepad")                               # Restore window
```

##### Keyboard Operations

```vma
input("Hello World")    # Input text
keyPress("Ctrl+A")      # Simulate key press
keyPress("Alt+F4")      # Simulate key press
keyDown("Alt")          # Press key down
keyUp("Alt")            # Release key
```

##### Control Scanning

```vma
scan()                  # Scan foreground window
scanWindow("Notepad")   # Scan by title
clickLabel("Button")    # Click label
show()                  # Show labels
hide()                  # Hide labels
```

##### Wait

```vma
waitFor("New Window")              # Wait for window to appear (default 30 second timeout)
waitFor("New Window", timeout=60)  # Wait for window to appear (custom 60 second timeout)
```

##### Get Information

```vma
var pos = getMousePos()      # Get mouse position
var size = getScreenSize()   # Get screen size
var windows = getWindows()   # Get window list
var result = getLastScanResult() # Get scan result
var labels = getLabels()     # Get labels
var num = rand(1, 100)       # Generate random number
```

##### Others

```vma
screenshot()                # Screenshot
log("Script running...")    # Log output
msg("Message")              # Message prompt (logged)
```

##### Control Flow

```vma
// Variable assignment
var count = 10
var name = "Vimina"
var flag = true

// Conditional
if windowexists("Notepad")
    log("Notepad window exists")
end

// Conditional (with else)
if windowexists("Notepad")
    log("Window exists")
else
    log("Window does not exist")
end

// Multiple conditions
if x > 10 and y < 20
    log("Condition met")
end

if x > 10 or y < 5
    log("Any condition met")
end

// Loop execution
loop(3)
    click(100, 100)
    sleep(200)
endloop

// for loop
for i = 1 to 10
    log(i)
end

// for loop (with step)
for i = 0 to 100 step 10
    log(i)
end

// while loop
var x = 0
while x < 10
    x = x + 1
    log(x)
end

// foreach loop
array items = [1, 2, 3, 4, 5]
foreach item in items
    log(item)
end

// Jump control
break       // Exit loop
continue    // Skip this iteration

// Label jump
startLabel:
log("Running...")
goto startLabel  // Jump to label
```

##### Array Operations

```vma
// Define array
array myArr = [1, 2, 3, 4, 5]
array names = ["Alice", "Bob", "Charlie"]

// Array operations
push(myArr, 6)           // Append element to end
pop(myArr)               // Pop last element
shift(myArr)             // Remove first element
unshift(myArr, 0)        // Insert element at beginning

// Access array elements
var first = myArr[1]     // Get first element
myArr[2] = 100           // Set element value

// Get array length
var len = length(myArr)
```

##### Function Definition

```vma
// Define function
function add(a, b)
    return a + b
endfunction

// Call function
var result = add(10, 20)
log(result)  // Output: 30

// Function with conditional return
function max(a, b)
    if a > b
        return a
    end
    return b
endfunction
```

##### Math Functions

```vma
var a = abs(-5)        // Absolute value → 5
var b = floor(3.7)     // Floor → 3
var c = ceil(3.2)      // Ceiling → 4
var d = min(1, 5, 3)   // Minimum → 1
var e = max(1, 5, 3)   // Maximum → 5
var f = toInt("123")   // To integer → 123
var g = toString(123)  // To string → "123"
var h = rand(1, 100)   // Random number 1-100
```

##### Type Checking

```vma
var x = 10
var arr = [1, 2, 3]

// Check type
var t = type(x)           // "number"
var isArr = isArray(arr)  // true

// Check window status
var exists = windowExists("Notepad")      // Window exists
var active = windowActive("Notepad")      // Window active

// Get length
var len = length(arr)      // Array length
var strLen = length("hello") // String length
```

#### Example Script

```vma
// Vimina automation script example

// Basic operations
sleep(1000)
click(500, 300)

// Window operations
activate("Notepad")
clickByTitle("Notepad", 100, 200)

// Keyboard operations
input("Hello World")
keyPress("Ctrl+A")

// Wait for window
waitFor("New Window", timeout=30)

// Conditional
if windowexists("Notepad")
    log("Notepad window exists")
end

// Loop example
loop(5)
    click(100, 100)
    sleep(500)
endloop

// for loop example
for i = 1 to 10
    log("Iteration " ++ i)
    sleep(100)
end

// Array operation example
array positions = [100, 200, 300, 400, 500]
foreach x in positions
    click(x, 300)
    sleep(200)
end

// Function definition example
function clickAndWait(x, y, delay)
    click(x, y)
    sleep(delay)
    log("Clicked (" ++ x ++ ", " ++ y ++ ")")
endfunction

clickAndWait(500, 300, 1000)

// Complex automation example
// Batch click multiple positions
array targets = [
    {x: 100, y: 200},
    {x: 300, y: 400},
    {x: 500, y: 600}
]

foreach target in targets
    click(target.x, target.y)
    sleep(300)
end

log("Automation script completed!")
```

> [!TIP]
> The quickReference field in scan results provides concise control descriptions, suitable as AI context input


## 📦 Installation and Usage

### System Requirements
- Operating System: Windows 10 / 11
- Runtime: .NET Framework 4.6.2+

### Quick Start

1. Download the latest version from Releases page
2. Extract to any directory
3. Run Vimina.exe
4. Open any application window, press Alt + F

### Operation Flow

1. Focus target window
2. Alt+F to show labels
3. Press the letters on the label
4. Automatically click the corresponding control

### Directory Structure

```
Vimina/
├── Vimina.exe                # Main program
├── config.json               # Configuration file
└── data/                     # Data directory
    ├── scan_result.json      # Scan results
    └── label_map.json        # Label mapping
```


## ⚙️ Configuration

The configuration file is `config.json` in the program directory, using JSON format. You can modify configuration through the main interface button or by directly editing the file.

### Label Style

```json
{
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

| Field | Description |
|-------|-------------|
| BackgroundColor_Default | Default background color |
| BackgroundColor_Match | Full match |
| BackgroundColor_Prefix | Prefix match |
| BackgroundColor_Invalid | Invalid label |
| TextColor | Text color |
| FontSize | Font size |
| FontWeight | Font weight |
| OffsetX | Label X offset |
| OffsetY | Label Y offset |

### Filter Settings

```json
{
    "MinWidth": 8,
    "MinHeight": 8,
    "MaxDepth": 50
}
```

| Field | Description |
|-------|-------------|
| MinWidth / MinHeight | Filter out too small controls to avoid dense labels |
| MaxDepth | Limit control tree traversal depth to prevent slow scanning |

### Click Mode

```json
{
    "UseMouseClick": false,
    "BringToFront": true,
    "UseFlaUIClick": true
}
```

| Field | Description |
|-------|-------------|
| UseMouseClick | true=Use mouse movement and click, false=Use background click |
| BringToFront | true=Bring window to foreground before clicking, false=Keep window in background |
| UseFlaUIClick | true=Use FlaUI framework for background click, false=Use winex click |

> [!TIP]
> Background click mode (UseMouseClick=false) can complete click operations without moving the mouse or switching windows

### Performance Related

```json
{
    "ClickDelay": 30
}
```

| Field | Description |
|-------|-------------|
| ClickDelay | Delay after click |

> [!NOTE]
> If clicking is unstable or target application responds slowly, increase ClickDelay value appropriately


## 📋 Configuration Examples

### Default Configuration

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

### Dark Theme Configuration

```json
{
    "BackgroundColor_Default": "0x3D3D3D",
    "BackgroundColor_Match": "0x00FF00",
    "BackgroundColor_Prefix": "0x00A5FF",
    "BackgroundColor_Invalid": "0x1A1A1A",
    "TextColor": "0xFFFFFF",
    "FontSize": 11,
    "FontWeight": 400,
    "OffsetX": 0,
    "OffsetY": 15
}
```

### Fast Scan

```json
{
    "MinWidth": 15,
    "MinHeight": 15,
    "MaxDepth": 25,
    "ClickDelay": 10
}
```

> [!TIP]
> Reducing MaxDepth and increasing MinWidth/MinHeight can significantly improve scanning speed

### Pure Background Click Mode

```json
{
    "UseMouseClick": false,
    "BringToFront": false,
    "UseFlaUIClick": true,
    "ClickDelay": 30
}
```

> [!TIP]
> With this configuration, all click operations are done in the background, without moving the mouse or switching windows, suitable for automation scripts


## 🛠️ Technical Features
- Based on FlaUI automation framework, supports UIA3 protocol
- Smart label generation algorithm, prioritizes easy-to-press key combinations
- Control deduplication mechanism, controls within 10px are automatically merged
- Global keyboard hook, does not affect normal use of other applications
- Single instance protection, prevents duplicate startup
- Automatic mouse position restoration after clicking
- Provides graphical configuration interface


## ❓ FAQ

### Q: No response when pressing Alt+F?

1. Ensure target window is in foreground and has focus
2. Some UWP applications may not support UIA3 protocol

### Q: Label display position is incorrect?

Modify offset values in configuration file:

```json
{
    "OffsetX": -20,
    "OffsetY": -5
}
```

### Q: Scanning is too slow?

Reduce traversal depth and filter small controls:

```json
{
    "MinWidth": 20,
    "MinHeight": 20,
    "MaxDepth": 30
}
```

### Q: Some controls are not recognized?

- Some custom-drawn controls may not properly expose UI automation interfaces
- Try increasing MaxDepth value
- Some control types (like Image, Text) are non-interactive by default

### Q: How to use API in scripts?

```bash
# Complete automation flow example
curl -X POST http://localhost:51401/api/show
sleep 1
curl http://localhost:51401/api/scan
curl -X POST http://localhost:51401/api/click -d '{"label":"DJ"}'
curl -X POST http://localhost:51401/api/hide
```

### Q: How to use VMA scripts?

VMA scripts are Vimina's built-in automation script feature, supporting .vma files for automation tasks.

**Method 1: Via Script Editor**
1. Right-click menu select "Script" or click main window "Script" button
2. Write script in the editor
3. Click "Run" to execute, or click "Compile" to generate standalone .exe file

**Method 2: Via Right-click Menu Import**
1. Right-click inside Vimina window
2. Select "Import VMA Script"
3. Select .vma file to run

**Method 3: Via API Call**

```bash
# Run script content directly
curl -X POST http://localhost:51401/api/vma/run \
  -H "Content-Type: application/json" \
  -d '{"script": "click(500,300)\nsleep(1000)"}'

# Run script file
curl -X POST http://localhost:51401/api/vma/runFile \
  -H "Content-Type: application/json" \
  -d '{"file": "C:\\path\\to\\script.vma"}'
```

### Q: Does API support cross-origin access?

Yes. CORS headers are configured, can be called directly from web pages

### Q: How to compile scripts?

1. Open script editor (right-click menu "Script" or main window "Script" button)
2. Write or open script file
3. Click "Compile" button
4. Select output path
5. Generated .exe file can run directly without any dependencies

> [!TIP]
> Compiled programs contain complete VMA runtime, supporting background clicking, window operations, and all other features

### Q: No response to key input?

Some controls may not support background clicking, try mouse clicking instead

### Q: Background right-click not successful?

Try bringing window to foreground first, then use mouse mode to right-click

### Q: Background activate browser window failed?

Browser windows have protection mechanisms that don't allow forced activation to foreground via API, this is a Windows and browser security restriction.
For browser windows:

1. Manually click to switch to browser window
2. Use background clicking - browser windows can be operated normally in the background

```bash
# Background click button in browser
curl -X POST http://localhost:51401/api/clickByTitle \
  -H "Content-Type: application/json" \
  -d '{"title": "Codeforces", "x": 500, "y": 300, "useBackend": true}'
```

Actually for most operations, background clicking (useBackend: true) is sufficient, no need to switch to foreground. Vimina's background click feature is specifically designed for this situation.

> [!TIP]
> If you really need to switch to foreground, this method also works (switch to foreground first, then right-click)
> ```bash
> curl "http://localhost:51401/api/clickByTitle?title=Codeforces&x=100&y=200&right=1"
> ```

### Q: How does it compare to similar software?

[Codex Computer Use](vsCodex-en.md) [AnJian JingLing](compare-en.md)

---
<p align="center"> Made with 💚 by Vimina </p>
