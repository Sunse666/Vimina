# Vimina vs AnJian JingLing Detailed Comparison

This document provides a detailed comparison between Vimina and AnJian JingLing (按键精灵), two automation tools, including their features, pros and cons, and suitable use cases.

---

## 📖 Table of Contents

- [Overview](#overview)
- [Common Features](#common-features)
- [Core Differences](#core-differences)
- [Feature Comparison Table](#feature-comparison-table)
- [Pros and Cons Analysis](#pros-and-cons-analysis)
- [Recommended Use Cases](#recommended-use-cases)
- [Technical Architecture Comparison](#technical-architecture-comparison)
- [How to Choose](#how-to-choose)

---

## Overview

### Vimina

**Vimina** is a Windows desktop automation tool inspired by the browser extension Vimium. Based on the FlaUI automation framework, it identifies window controls through the UIA3 protocol, generates letter labels for each interactive control, and allows users to precisely click with just keyboard input. It also provides a complete HTTP API for integration with AI assistants and external programs.

**Core Philosophy:** Keyboard-first, control-level recognition, AI-friendly

**Open Source Status:** Open source

**Development Language:** Pascal + FlaUI (.NET)

### AnJian JingLing

**AnJian JingLing** (按键精灵) is a well-established Windows automation tool with nearly 20 years of history. It simulates mouse and keyboard operations through recording/writing scripts, supports image recognition, color finding, memory reading, and is widely used in domestic game automation and RPA fields.

**Core Philosophy:** Record and playback, image recognition, script automation

**Open Source Status:** Commercial software

**Development Language:** Proprietary script engine (Q Language)

---

## Common Features

| Feature | Vimina | AnJian JingLing |
|---------|--------|-----------------|
| **Mouse Automation** | ✅ Click, move, drag, scroll | ✅ Click, move, drag, scroll |
| **Keyboard Automation** | ✅ Key press, combinations, text input | ✅ Key press, combinations, text input |
| **Script Writing** | ✅ VMA script language | ✅ Q Language script |
| **Script Compilation** | ✅ Can compile to standalone exe | ✅ Can compile to standalone exe |
| **Background Operations** | ✅ Background click, background input | ✅ Background binding, background operations |
| **Window Management** | ✅ Find, activate, close, minimize, etc. | ✅ Find, activate, close, minimize, etc. |
| **Loop Control** | ✅ loop/for/while/foreach | ✅ For/Do/While loops |
| **Conditional Statements** | ✅ if/else/elseif | ✅ If/ElseIf/Else |
| **Variable System** | ✅ Variables, arrays, functions | ✅ Variables, arrays, functions |
| **Delay Control** | ✅ sleep delay | ✅ Delay |
| **Multi-threading** | ⚠️ Via API | ✅ Native multi-threading support |

---

## Core Differences

### 1. Control Recognition Technology

| Comparison | Vimina | AnJian JingLing |
|------------|--------|-----------------|
| **Core Technology** | FlaUI (UIA3 protocol) | Coordinate positioning + Image recognition |
| **Recognition Method** | Control level, semantic recognition | Pixel/coordinate/image level |
| **Recognition Precision** | Precise to control object | Precise to pixel |
| **Resolution Dependency** | ❌ Completely independent | ⚠️ Partially depends on coordinates |
| **Window Scaling Adaptation** | ✅ Automatic adaptation | ❌ Need readjustment |
| **DPI Scaling Adaptation** | ✅ Automatic adaptation | ⚠️ May need adjustment |
| **Dynamic Layout** | ✅ Automatic adaptation to control position changes | ❌ Need re-recording/adjustment |

**Vimina Control Recognition Example:**
```
After scanning window, recognized:
- Button "OK" → Label DJ
- Edit "Username input" → Label DK
- MenuItem "File" → Label DL

No matter how the window position changes, clicking label DJ will always accurately click the "OK" button
```

**AnJian JingLing Recognition Example:**
```
Recorded: Click at coordinate (500, 300)
Problem: After window moves, coordinate may no longer correspond to correct position
Solution: Use window binding + relative coordinates, or image recognition
```

### 2. Interaction Method

| Comparison | Vimina | AnJian JingLing |
|------------|--------|-----------------|
| **Primary Interaction** | Keyboard label clicking | Mouse recording/script execution |
| **Real-time Feedback** | ✅ Colorful label highlighting | ❌ No visual feedback |
| **Instant Operation** | ✅ Alt+F show labels, direct input | ❌ Need to write/record script first |
| **Learning Curve** | Low (similar to Vimium) | Medium (need to learn script syntax) |
| **Operation Threshold** | Zero code to use | Need to write or record scripts |

**Vimina Interaction Flow:**
```
1. Press Alt+F to scan window
2. See label "DJ" on "OK" button
3. Keyboard input D, J
4. Automatically click "OK" button
```

**AnJian JingLing Interaction Flow:**
```
1. Open script editor
2. Write script: MoveTo 500, 300 : LeftClick 1
3. Or use recording feature to record operations
4. Run script to execute operations
```

### 3. API and Integration Capabilities

| Comparison | Vimina | AnJian JingLing |
|------------|--------|-----------------|
| **HTTP API** | ✅ Complete RESTful API | ❌ No built-in HTTP API |
| **External Calls** | ✅ Any language HTTP call | ⚠️ Limited external call support |
| **AI Integration** | ✅ Native support, JSON response | ❌ Requires additional bridge development |
| **CORS Cross-origin** | ✅ Supported | - |
| **Webhook** | ✅ Can be implemented via API | ❌ Not supported |
| **Command Line** | ✅ Direct curl call | ⚠️ Requires command line parameters |

**Vimina API Example:**
```bash
# Python calling Vimina
import requests

# Scan window
requests.post("http://localhost:51401/api/show")

# Get control list
result = requests.get("http://localhost:51401/api/scan").json()

# Click control
requests.post("http://localhost:51401/api/click", json={"label": "DJ"})
```

```javascript
// Node.js calling Vimina
const axios = require('axios');

// Background click at specified coordinates
await axios.post('http://localhost:51401/api/clickAt', {
    x: 500, y: 300, useBackend: true
});
```

### 4. Script Language Comparison

| Comparison | Vimina (VMA) | AnJian JingLing (Q Language) |
|------------|--------------|------------------------------|
| **Syntax Style** | Python/VB-like | VBScript-like |
| **Variable Declaration** | `var x = 10` | `Dim x = 10` |
| **Function Definition** | `function f() ... endfunction` | `Function f() ... End Function` |
| **Loop Syntax** | `loop(5) ... endloop` / `for...to...end` | `For i = 1 To 5 ... Next` |
| **Conditional Syntax** | `if ... end` | `If ... Then ... End If` |
| **Array Support** | `array arr = [1,2,3]` | `Dim arr(3)` |
| **String Concatenation** | `++` operator | `&` operator |
| **Comments** | `//` single line | `'` single line |

**Vimina Script Example:**
```vma
// VMA script example
var count = 0
array positions = [100, 200, 300, 400, 500]

foreach x in positions
    click(x, 300)
    sleep(200)
    count = count + 1
end

log("Clicked " ++ count ++ " times")
```

**AnJian JingLing Script Example:**
```vb
' Q Language script example
Dim count, positions, x
count = 0

For i = 0 To 4
    x = (i + 1) * 100
    MoveTo x, 300
    LeftClick 1
    Delay 200
    count = count + 1
Next

TracePrint "Clicked " & count & " times"
```

### 5. Image Recognition Capabilities

| Comparison | Vimina | AnJian JingLing |
|------------|--------|-----------------|
| **Find Image** | ❌ Not supported | ✅ Powerful find image feature |
| **Find Color** | ❌ Not supported | ✅ Multi-point color finding |
| **OCR Text Recognition** | ❌ Not supported | ✅ Built-in OCR |
| **Screenshot** | ✅ Supported | ✅ Supported |
| **Image Comparison** | ❌ Not supported | ✅ Supported |

### 6. Game Support

| Comparison | Vimina | AnJian JingLing |
|------------|--------|-----------------|
| **Regular Games** | ⚠️ Limited support | ✅ Complete support |
| **DirectX Games** | ❌ Most not supported | ✅ Supported |
| **OpenGL Games** | ❌ Most not supported | ✅ Supported |
| **Memory Reading** | ❌ Not supported | ✅ Supported |
| **Multi-instance Games** | ⚠️ Via API | ✅ Native multi-instance support |
| **Anti-detection** | ❌ None | ⚠️ Has some measures |

---

## Feature Comparison Table

### Complete Feature Comparison

| Feature Category | Feature Item | Vimina | AnJian JingLing |
|------------------|--------------|--------|-----------------|
| **Basic Operations** | Mouse click | ✅ | ✅ |
| | Mouse move | ✅ | ✅ |
| | Mouse drag | ✅ | ✅ |
| | Keyboard press | ✅ | ✅ |
| | Key combinations | ✅ | ✅ |
| | Text input | ✅ | ✅ |
| **Control Recognition** | UIA control recognition | ✅ | ❌ |
| | Handle recognition | ✅ | ✅ |
| | Image recognition | ❌ | ✅ |
| | Color recognition | ❌ | ✅ |
| | OCR recognition | ❌ | ✅ |
| **Window Operations** | Window find | ✅ | ✅ |
| | Window activate | ✅ | ✅ |
| | Window close | ✅ | ✅ |
| | Window resize | ✅ | ✅ |
| | Multi-window management | ✅ | ✅ |
| **Background Operations** | Background mouse | ✅ | ✅ |
| | Background keyboard | ✅ | ✅ |
| | Background binding | ✅ | ✅ |
| **Script Features** | Variables | ✅ | ✅ |
| | Arrays | ✅ | ✅ |
| | Functions | ✅ | ✅ |
| | Loops | ✅ | ✅ |
| | Conditionals | ✅ | ✅ |
| | Multi-threading | ⚠️ | ✅ |
| | Plugin system | ❌ | ✅ |
| **Integration Capabilities** | HTTP API | ✅ | ❌ |
| | Command line | ✅ | ✅ |
| | External calls | ✅ | ⚠️ |
| | AI integration | ✅ | ❌ |
| **Others** | Recording feature | ❌ | ✅ |
| | Visual editing | ⚠️ | ✅ |
| | Script encryption | ✅ | ✅ |
| | Compile to exe | ✅ | ✅ |
| | Debugging features | ⚠️ | ✅ |

---

## Pros and Cons Analysis

### Vimina

#### ✅ Pros

1. **Control-level Precise Recognition**
   - Based on UIA3 protocol, directly identifies buttons, input boxes, menus, and other controls
   - Does not depend on screen coordinates, window movement/scaling does not affect operations
   - Supports getting control properties (name, type, state, etc.)

2. **Keyboard-first Design**
   - Label system allows users to operate any control without a mouse
   - Similar to Vimium experience, keyboard-friendly
   - Zero code to use, just press Alt+F to start

3. **AI-friendly**
   - Complete HTTP API, supports RESTful calls
   - JSON format response, easy to parse
   - Supports CORS cross-origin, can be called directly from web pages
   - Scan results contain semantic information, suitable for AI analysis

4. **Real-time Visual Feedback**
   - Colorful labels show matching status (yellow/orange/green/gray)
   - Instant display of current input matching status

5. **Stable Background Operations**
   - Can complete clicks without moving the mouse
   - Can operate background windows without switching
   - Suitable for automation scripts and AI assistants

6. **Developer-friendly**
   - Can be called by Python, Node.js, Java, and any other language
   - Complete API documentation
   - Easy to integrate into existing workflows

7. **Lightweight and Open Source**
   - Single exe file, no installation required
   - Open source project, customizable extensions

#### ❌ Cons

1. **Limited Game Support**
   - Most games don't expose UIA interfaces
   - DirectX/OpenGL games cannot recognize controls
   - No image recognition, cannot operate via find image

2. **Small Community Ecosystem**
   - Fewer script libraries
   - Relatively fewer tutorials and examples
   - Small user community size

3. **Missing Image Recognition**
   - No find image/color feature
   - Cannot perform image comparison
   - No OCR text recognition

4. **Missing Recording Feature**
   - Need to manually write scripts
   - No operation recording feature

5. **Limited Multi-threading Support**
   - Script engine executes single-threaded
   - Need to implement concurrency via API

### AnJian JingLing

#### ✅ Pros

1. **Mature Ecosystem**
   - Nearly 20 years of history, large user base
   - Many ready-made script libraries available
   - Rich tutorials and community support

2. **Strong Game Support**
   - Complete image recognition and color finding features
   - Supports memory reading
   - DirectX/OpenGL game support

3. **Recording Feature**
   - Can record operations to automatically generate scripts
   - Lowers usage threshold
   - Quickly create automation scripts

4. **Rich Plugins**
   - Many third-party plugins extend functionality
   - Supports custom plugin development
   - Functionality can be infinitely extended

5. **Visual Editing**
   - Graphical script editor
   - Drag-and-drop interface design
   - Lowers learning curve

6. **Multi-instance/Background**
   - Mature multi-instance window binding
   - Multi-threaded script execution
   - Stable background operations

#### ❌ Cons

1. **Coordinate Dependency**
   - Some features depend on screen coordinates
   - Resolution changes require script adjustment
   - Window position changes may affect scripts

2. **No API Interface**
   - No HTTP API
   - Difficult to be called by external programs
   - Not suitable for integration into modern applications

3. **Difficult AI Integration**
   - No native API support
   - Requires additional bridge program development
   - Not suitable for AI assistant scenarios

4. **Commercial Pricing**
   - Free version has limited features
   - Advanced features require payment
   - Commercial use requires license

5. **Large Size**
   - Large installation package
   - Requires runtime installation

---

## Recommended Use Cases

### Scenarios Recommended for Vimina

| Scenario | Reason |
|----------|--------|
| **Daily Office Operations** | Label system for quick clicking, no need to write scripts |
| **AI Assistant Integration** | HTTP API native support, JSON response |
| **Developer Automation** | Can be called by code, integrated into workflows |
| **RPA Enterprise Applications** | Control-level recognition more stable, coordinate-independent |
| **Cross-resolution Deployment** | Control recognition unaffected by resolution |
| **CI/CD Integration** | HTTP API can be called in pipelines |
| **Web Application Integration** | CORS support, can be called from web pages |
| **Desktop Application Testing** | Control-level operations, more precise testing |

### Scenarios Recommended for AnJian JingLing

| Scenario | Reason |
|----------|--------|
| **Game AFK** | Image recognition, color finding, memory reading |
| **Game Automation** | DirectX/OpenGL game support |
| **Image Recognition Tasks** | Powerful find image/color features |
| **Beginner Entry** | Recording feature, visual editing |
| **Batch Operations** | Mature script libraries ready to use |
| **Multi-instance AFK** | Native multi-instance support |
| **Rapid Prototyping** | Recording feature quickly creates scripts |

### Scenarios Suitable for Both

| Scenario | Description |
|----------|-------------|
| **Batch Data Processing** | Both support script automation |
| **Scheduled Tasks** | Both can work with system scheduled tasks |
| **Window Management** | Both support window operations |
| **Keyboard Mouse Simulation** | Basic features are complete |

---

## Technical Architecture Comparison

### Vimina Technical Architecture

```
┌─────────────────────────────────────────────┐
│                  Vimina                      │
├─────────────────────────────────────────────┤
│  ┌─────────┐  ┌─────────┐  ┌─────────────┐  │
│  │ Label   │  │VMA      │  │ HTTP Server │  │
│  │ System  │  │Engine   │  │             │  │
│  └────┬────┘  └────┬────┘  └──────┬──────┘  │
│       │            │              │          │
│       └────────────┴──────────────┘          │
│                    │                          │
│  ┌─────────────────▼─────────────────────┐   │
│  │            FlaUI (UIA3)                │   │
│  └─────────────────┬─────────────────────┘   │
│                    │                          │
│  ┌─────────────────▼─────────────────────┐   │
│  │         Windows UI Automation          │   │
│  └───────────────────────────────────────┘   │
└─────────────────────────────────────────────┘
```

**Tech Stack:**
- Development Language: Pascal
- Automation Framework: FlaUI (.NET UIA3)
- HTTP Service: wsock.tcp.simpleHttpServer
- Runtime: .NET Framework 4.6.2+

### AnJian JingLing Technical Architecture

```
┌─────────────────────────────────────────────┐
│               AnJian JingLing               │
├─────────────────────────────────────────────┤
│  ┌─────────┐  ┌─────────┐  ┌─────────────┐  │
│  │Recorder │  │ Script  │  │   Plugin    │  │
│  │         │  │ Engine  │  │   System    │  │
│  └────┬────┘  └────┬────┘  └──────┬──────┘  │
│       │            │              │          │
│       └────────────┴──────────────┘          │
│                    │                          │
│  ┌─────────────────▼─────────────────────┐   │
│  │         Windows API / DirectX          │   │
│  └─────────────────┬─────────────────────┘   │
│                    │                          │
│  ┌─────────────────▼─────────────────────┐   │
│  │    Image Recognition / Memory Read /   │   │
│  │              Window Operations          │   │
│  └───────────────────────────────────────┘   │
└─────────────────────────────────────────────┘
```

**Tech Stack:**
- Development Language: Proprietary engine
- Script Language: Q Language
- Image Recognition: Proprietary algorithms
- Memory Reading: Windows API

---

## How to Choose

### Decision Flowchart

```
                    Start
                      │
                      ▼
            ┌─────────────────┐
            │ Need game support?│
            └────────┬────────┘
                     │
         ┌───────────┴───────────┐
         │                       │
        Yes                      No
         │                       │
         ▼                       ▼
┌─────────────────┐    ┌─────────────────┐
│ AnJian JingLing │    │ Need AI         │
│                 │    │ integration?    │
└─────────────────┘    └────────┬────────┘
                                │
                    ┌───────────┴───────────┐
                    │                       │
                   Yes                      No
                    │                       │
                    ▼                       ▼
           ┌─────────────────┐    ┌─────────────────┐
           │    Vimina       │    │   Both work     │
           └─────────────────┘    └─────────────────┘
```

### Quick Selection Guide

| Your Need | Recommended Choice |
|-----------|-------------------|
| I want to quickly operate desktop apps with keyboard | **Vimina** |
| I want AI assistant to help me operate computer | **Vimina** |
| I want to develop automation tool integration | **Vimina** |
| I want to make game AFK scripts | **AnJian JingLing** |
| I need find image/color features | **AnJian JingLing** |
| I'm a beginner, want to get started quickly | **AnJian JingLing** |
| I need cross-resolution usage | **Vimina** |
| I need to operate multiple game windows | **AnJian JingLing** |

---

## Summary

| Dimension | Vimina | AnJian JingLing |
|-----------|--------|-----------------|
| **Positioning** | Modern AI-era automation tool | Traditional automation script tool |
| **Strengths** | Control recognition, API integration, keyboard operations | Game automation, image recognition, community ecosystem |
| **Weaknesses** | Game support, image recognition | API integration, AI-friendly |
| **Target Users** | Developers, keyboard enthusiasts, AI users | Gamers, automation beginners |
| **Learning Curve** | Low | Medium |
| **Price** | Free and open source | Free version limited, Pro version paid |

**Final Recommendation:**

- If you need **desktop application operations, AI integration, keyboard efficiency, developer automation** → Choose **Vimina**
- If you need **game AFK, image recognition, find image/color, quick script recording** → Choose **AnJian JingLing**

Both can be used complementarily, choosing the right tool for different scenarios. Vimina is more suitable for modern developers and AI scenarios, while AnJian JingLing is more suitable for traditional gaming and image automation scenarios.
