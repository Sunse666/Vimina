# Vimina

<p align="center">
  <img src="assets/logo/logo.png" alt="Vimina Logo" width="128">
</p>

<p align="center">
  <strong>GUI-to-Text · AI-Friendly Windows Desktop Automation</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows-blue?style=flat-square" alt="Platform">
  <img src="https://img.shields.io/badge/Engine-FlaUI_UIA3-orange?style=flat-square" alt="Engine">
  <img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" alt="License">
</p>

---

## Overview

Inspired by [Vimium](https://github.com/philc/vimium), Vimina is a Windows desktop automation tool that **converts GUI to text**.

Using UIA3 protocol to identify window controls, it transforms interface elements into structured JSON data, enabling AI assistants to understand and operate desktop applications **without vision models**.

<p align="center">
  <!-- Screenshot -->
  <img src="assets/Screenshot.png" alt="Screenshot" width="600">
</p>

---

## Why Vimina?

### 🤖 AI-Friendly Design

| Aspect | Vision Model Approach | Vimina Approach |
|--------|----------------------|-----------------|
| **Input Data** | Screenshot (~MB) | JSON Text (~KB) |
| **API Cost** | High (image tokens) | **Ultra-low (text only)** |
| **Positioning** | Pixel-level guessing | **Control-level precision** |
| **Control Properties** | Visual inference | **Native semantic info** |
| **Background Operation** | ❌ Not supported | **✅ Full support** |

### 📊 Scan Result Example

```json
{
  "controls": [
    {"label": "A", "type": "Edit", "name": "Username", "x": 120, "y": 50, "enabled": true},
    {"label": "B", "type": "Edit", "name": "Password", "x": 120, "y": 90, "enabled": true},
    {"label": "C", "type": "Button", "name": "Login", "x": 150, "y": 140, "enabled": true}
  ]
}
```

AI understands interface structure directly and generates precise operation scripts:

```vma
clickLabel("A")
input("myuser")
clickLabel("B")
input("mypass")
clickLabel("C")
```

### ⚡ Core Advantages

- **🎯 Precise Positioning** - Control-level recognition, resolution & position independent
- **👻 Background Operation** - No mouse movement, no window switching, silent execution
- **🔌 HTTP API** - Complete RESTful interface, one line of code to call
- **📜 VMA Script** - Variables, loops, functions supported, compilable to standalone exe
- **💰 Zero AI Cost** - Script generated once, reused infinitely

---

## Quick Start

### Installation

1. Download from [GitHub Releases](https://github.com/Sunse666/Vimina/releases)
2. Extract to any directory
3. Run `Vimina.exe`

### Basic Usage

| Shortcut | Function |
|----------|----------|
| `Alt + F` | Show/hide control labels |
| `Alt + R` | Refresh labels |
| `Esc` | Clear all labels |
| `A-Z` | Type label letters to click |
| `Backspace` | Delete entered character |

### Workflow

```
1. Focus target window
2. Press Alt+F to show labels
3. Type label letters (e.g. DJ)
4. Auto-click corresponding control
```

<p align="center">
  <!-- Demo GIF -->
  <img src="assets/demo.gif" alt="Demo" width="500">
</p>

### HTTP API

```bash
# Scan window
curl http://localhost:51401/api/scan

# Click by label
curl -X POST http://localhost:51401/api/click -d '{"label":"DJ"}'

# Click by coordinates
curl http://localhost:51401/api/click/500/300

# Background click (no mouse movement)
curl "http://localhost:51401/api/click/500/300?useBackend=1"
```

### VMA Script Example

```vma
// Automation script example
activate("Notepad")
scan()
clickLabel("A")
input("Hello World")
keyPress("Ctrl+S")
```

---

## AI Integration

### Python Example

```python
import requests

# Scan window, get structured data
result = requests.get("http://localhost:51401/api/scanAll").json()

# AI analyzes interface, decides and executes
requests.post("http://localhost:51401/api/click", json={"label": "A"})
requests.post("http://localhost:51401/api/input", json={"text": "Hello"})
```

### AI Workflow

```
┌─────────────┐     scan()     ┌─────────────┐   Understand   ┌─────────────┐
│  GUI Window │ ─────────────▶ │ Structured  │ ─────────────▶ │ AI Assistant│
│             │                │    JSON     │                │             │
└─────────────┘                └─────────────┘                └─────────────┘
                                                                     │
                                     ┌───────────────────────────────┘
                                     ▼
                              ┌─────────────┐
                              │   Execute   │
                              │ clickLabel  │
                              └─────────────┘
```

---

## Screenshots

<p align="center">
  <img src="assets/Screenshot.png" alt="Main Window" width="400">
  <img src="assets/Screenshot2.png" alt="Labels Display" width="400">
</p>

<p align="center">
  <img src="assets/config.png" alt="Config Window" width="400">
  <img src="assets/script.png" alt="Script Editor" width="400">
</p>

---

## Documentation & Resources

### 📖 Full Documentation

| Document | Description |
|----------|-------------|
| [Getting Started](https://sunse666.github.io/Vimina-docs/getting-started/) | Installation and basic operations |
| [Basics](https://sunse666.github.io/Vimina-docs/basics/) | Label system, shortcuts |
| [HTTP API](https://sunse666.github.io/Vimina-docs/api/) | Complete API endpoints and examples |
| [VMA Script](https://sunse666.github.io/Vimina-docs/vma/) | Script syntax, functions, examples |
| [Configuration](https://sunse666.github.io/Vimina-docs/config/) | Label styles, click modes |

### 📊 Detailed Comparisons

| Comparison | Description |
|------------|-------------|
| [vs AutoHotkey](https://sunse666.github.io/Vimina-docs/vs-anjian/) | Feature comparison |
| [vs OpenClaw](https://sunse666.github.io/Vimina-docs/vs-openclaw/) | Technical approach comparison |
| [vs Codex Computer Use](https://sunse666.github.io/Vimina-docs/vs-codex/) | AI vision approach comparison |
| [Full Comparison](https://sunse666.github.io/Vimina-docs/compare/) | Complete comparison of all solutions |

### 🔗 Other Resources

| Resource | Link |
|----------|------|
| 📥 **Download Latest** | [GitHub Releases](https://github.com/Sunse666/Vimina/releases) |
| 💻 **Source Code** | [GitHub Repository](https://github.com/Sunse666/Vimina) |
| 🐛 **Report Issues** | [GitHub Issues](https://github.com/Sunse666/Vimina/issues) |

---

## System Requirements

- **OS**: Windows 10 / 11
- **Runtime**: 
  - Small version: Requires [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
  - Self-contained version: No runtime installation needed
- **Installation**: Extract and run, no installation needed

## Download

| Version | Size | Description |
|---------|------|-------------|
| **Small** | ~3 MB | Requires .NET 8 Runtime |
| **Self-contained** | ~180 MB | No dependencies, works out of the box |

Download from [GitHub Releases](https://github.com/Sunse666/Vimina/releases).

---

## Comparison

| Feature | Vimina | AutoHotkey | OpenClaw | Codex Computer Use |
|---------|--------|------------|----------|-------------------|
| Control Recognition | ✅ UIA3 | ❌ None | ⚠️ Skills | Vision-based |
| Background Operation | ✅ Full | ⚠️ Partial | ⚠️ Config | ❌ |
| HTTP API | ✅ | ❌ | ✅ | ✅ |
| AI Integration | ✅ Low-cost | ❌ | ✅ High-cost | High-cost |
| Browser Support | ⚠️ UIA | ❌ | ✅ Playwright | ✅ |
| Open Source | ✅ MIT | ✅ GPL | ✅ MIT | ❌ |

Detailed comparison: [Vimina vs Others](https://sunse666.github.io/Vimina-docs/compare/)

---

## License

[GPL License](LICENSE)

---

<p align="center">
  Made with 💚 by <a href="https://github.com/Sunse666">Sunse666</a>
</p>
