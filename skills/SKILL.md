---
name: "vimina-automation"
description: "Vimina is a Windows desktop automation tool that converts GUI to structured text via UIA3 protocol, enabling AI to understand and operate desktop applications without vision models. Invoke when user needs Windows GUI automation, interface operations, or control clicking."
---

# Vimina Windows Automation Skill

## Core Concept: GUI-to-Text Replaces Vision Models

### Why Vimina Instead of Vision Models?

Traditional AI GUI interaction uses vision models to analyze screenshots:

```
Vision Model Approach:
Screenshot → Upload image (~1000+ tokens) → AI analyzes → Guesses coordinates → Execute click
Problems: High cost, low accuracy, slow speed, privacy leak
```

Vimina's approach converts GUI to structured text:

```
Vimina Approach:
scan() → Get JSON (~50 tokens) → AI understands structure → Precise click
Advantages: Low cost, high accuracy, fast speed, privacy safe
```

### Cost Comparison

| Operation | Vision Model | Vimina | Savings |
|-----------|--------------|--------|---------|
| Get interface info | ~1500 tokens (screenshot) | ~50 tokens (JSON) | **97%** |
| Identify button position | ~300 tokens | ~10 tokens | **97%** |
| Execute click | ~200 tokens | ~5 tokens | **98%** |
| **Total per task** | **~2000 tokens** | **~65 tokens** | **97%** |

### Accuracy Comparison

| Dimension | Vision Model | Vimina |
|-----------|--------------|--------|
| Positioning accuracy | Pixel-level guessing (70-90%) | Control-level precision (100%) |
| Background operations | ❌ Not supported | ✅ Fully supported |
| Hidden elements | ❌ Cannot detect | ✅ Can retrieve |
| Control properties | Visual inference needed | Native semantic info |

---

## Quick Start: Command Line Guide

### Prerequisites

Ensure Vimina.exe is running, API server defaults to `http://localhost:51401`.

### 1. Get Screen Information

#### Scan Foreground Window
```bash
# Scan all controls (including text, images, etc.)
curl http://localhost:51401/api/scanAll

# Scan interactive controls only
curl http://localhost:51401/api/scan
```

#### Scan Specific Window
```bash
# Scan by window title (supports fuzzy matching)
curl "http://localhost:51401/api/scanAllByTitle?title=Notepad"
curl "http://localhost:51401/api/scanAllByTitle?title=bilibili"

# Activate window then scan
curl "http://localhost:51401/api/activate?title=Notepad"
curl "http://localhost:51401/api/scanAll"
```

### 2. Generated Files Explanation

Scanning generates JSON files in the `data/` directory:

#### scan_result_lite.json (AI-Friendly, Recommended)

Minimal format, one string per control, saves AI tokens:

```json
{
  "success": true,
  "window": {
    "title": "bilibili",
    "className": "Chrome_WidgetWin_1"
  },
  "summary": {
    "totalControls": 142
  },
  "controls": [
    "DJ: Search box (Edit) [250, 210]",
    "DK: Title bar (Header) [100, 50]",
    "DL: 8 (Button) [600, 400]",
    "DM: https://space.bilibili.com/1698752197 (Image) [300, 500]",
    "DN: Username (Image) [350, 520]",
    "DO: Guangdong (Thumb) [400, 540]"
  ]
}
```

**Format**: `Label: Name (Type) [CenterX, CenterY]`

**Advantages**:
- Small file, low token consumption
- Complete info: label, name, type, coordinates
- AI can directly understand interface structure

#### scan_result_tree.json (Control Tree Structure)

Contains control hierarchy with parent-child relationships. **Read together with scan_result_lite.json for complete analysis.**

```json
{
  "controlTree": {
    "name": "bilibili",
    "x": 0, "y": 0,
    "children": [
      {
        "name": "Search box",
        "x": 100, "y": 200,
        "children": []
      },
      {
        "name": "Video List",
        "x": 0, "y": 100,
        "children": [
          { "name": "Video Title 1", "x": 100, "y": 150 },
          { "name": "Video Title 2", "x": 100, "y": 200 }
        ]
      }
    ]
  }
}
```

**Fields**:
- `name`: Control text content
- `x, y`: Control position
- `children`: Nested child controls

**Usage**: Combine with `scan_result_lite.json` - lite provides labels/types/center coordinates, tree provides hierarchy.

#### label_map.json (Label-Coordinate Mapping)

For quickly finding coordinates by label:

```json
{
  "DJ": { "centerX": 250, "centerY": 210 },
  "DK": { "centerX": 100, "centerY": 50 },
  "DL": { "centerX": 600, "centerY": 400 }
}
```

**Usage**: Quickly locate coordinates when executing clicks

#### scan_result.json (Complete Information)

Contains all control properties:

```json
{
  "success": true,
  "window": { "handle": 66838, "title": "...", "className": "..." },
  "summary": {
    "totalControls": 142,
    "byType": { "Button": 25, "Edit": 1, "List": 28, "Image": 16 }
  },
  "controls": [
    {
      "label": "DJ",
      "name": "Search box",
      "type": "Edit",
      "typeDesc": "Edit Box",
      "x": 100, "y": 200, "width": 300, "height": 20,
      "centerX": 250, "centerY": 210,
      "automationId": "",
      "className": "Edit",
      "isEnabled": true,
      "isVisible": true,
      "isKeyboardFocusable": true
    }
  ]
}
```

**Usage**: Use when detailed control properties are needed

### 3. Execute Click Operations

#### Click by Label (Recommended)
```bash
# Left click
curl -X POST http://localhost:51401/api/click -H "Content-Type: application/json" -d "{\"label\":\"DJ\"}"

# Right click
curl -X POST http://localhost:51401/api/click -H "Content-Type: application/json" -d "{\"label\":\"DJ\",\"right\":true}"

# Double click
curl -X POST http://localhost:51401/api/click -H "Content-Type: application/json" -d "{\"label\":\"DJ\",\"double\":true}"
```

#### Click by Coordinates
```bash
# Normal click
curl http://localhost:51401/api/click/500/300

# Backend click (no mouse movement)
curl "http://localhost:51401/api/click/500/300?useBackend=1"

# Right click
curl http://localhost:51401/api/clickR/500/300

# Double click
curl http://localhost:51401/api/dblclick/500/300
```

#### Background Window Click
```bash
# Click in specified window without affecting current work
curl -X POST http://localhost:51401/api/clickByTitle -H "Content-Type: application/json" -d "{\"title\":\"Notepad\",\"x\":200,\"y\":150,\"useBackend\":true,\"bringToFront\":false}"
```

### 4. Keyboard Input
```bash
# Input text
curl -X POST http://localhost:51401/api/input -H "Content-Type: application/json" -d "{\"text\":\"Hello World\"}"
```

### 5. Window Management
```bash
# Get all windows
curl http://localhost:51401/api/windows

# Activate window
curl "http://localhost:51401/api/activate?title=Notepad"
```

---

## AI Standard Workflow with Vimina

### Step 1: Scan Window to Get Interface Info

```bash
curl "http://localhost:51401/api/scanAllByTitle?title=bilibili"
```

### Step 2: Read scan_result_lite.json

AI reads the generated `data/scan_result_lite.json` to understand interface structure:

```
Interface contains:
- DJ: Search box (Edit) [250, 210] - Can input search content
- DK: Submit (Button) [350, 210] - Click to search
- DL: Video title (Image) [100, 400] - Video thumbnail
- DM: Like (Button) [200, 500] - Like button
...
```

### Step 3: Execute Operations

Based on user intent, AI selects appropriate labels to execute clicks:

```bash
# Click search box
curl -X POST http://localhost:51401/api/click -H "Content-Type: application/json" -d "{\"label\":\"DJ\"}"

# Input search content
curl -X POST http://localhost:51401/api/input -H "Content-Type: application/json" -d "{\"text\":\"Vimina tutorial\"}"

# Click search button
curl -X POST http://localhost:51401/api/click -H "Content-Type: application/json" -d "{\"label\":\"DK\"}"
```

---

## Vimina's Unique Advantages

### 1. Background Operation Capability

Can operate background applications without activating windows:

```bash
# Input text in minimized Notepad
curl -X POST http://localhost:51401/api/clickByTitle -H "Content-Type: application/json" -d "{\"title\":\"Notepad\",\"x\":100,\"y\":50,\"useBackend\":true}"
curl -X POST http://localhost:51401/api/input -H "Content-Type: application/json" -d "{\"text\":\"Background input text\"}"
```

### 2. Precise Control Recognition

Based on UIA3 protocol, retrieves native control properties:

- Control type (Button, Edit, MenuItem...)
- Control name/text
- Enabled status (isEnabled)
- Visibility (isVisible)
- Precise coordinates

### 3. Zero AI Cost Execution

After one scan, subsequent operations don't need AI analysis:

```bash
# Scan once
curl "http://localhost:51401/api/scanAllByTitle?title=TargetWindow"

# Subsequent operations use labels directly, no AI analysis needed
curl -X POST http://localhost:51401/api/click -d '{"label":"DJ"}'
curl -X POST http://localhost:51401/api/click -d '{"label":"DK"}'
curl -X POST http://localhost:51401/api/click -d '{"label":"DL"}'
```

### 4. Privacy Safe

- No screenshots needed, interface info retrieved as text
- All operations execute locally, data never leaves machine
- No dependency on cloud APIs

---

## Complete API Reference

### Scan Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/scan` | GET | Get scan results (interactive controls only) |
| `/api/scanAll` | GET | Scan all controls (including text, images) |
| `/api/scanByTitle?title=xxx` | GET/POST | Scan by window title |
| `/api/scanAllByTitle?title=xxx` | GET/POST | Scan all controls by title |

### Click Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/click` | POST | Click by label |
| `/api/click/{x}/{y}` | GET | Click at coordinates |
| `/api/click/{x}/{y}?useBackend=1` | GET | Backend click (no mouse movement) |
| `/api/clickR/{x}/{y}` | GET | Right-click at coordinates |
| `/api/dblclick/{x}/{y}` | GET | Double-click at coordinates |
| `/api/clickByTitle` | POST | Background window click |

### Other Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/windows` | GET | Get window list |
| `/api/activate?title=xxx` | GET | Activate window |
| `/api/input` | POST | Keyboard input |
| `/api/mouse` | GET | Get mouse position |
| `/api/vma/run` | POST | Execute VMA script |

---

## Comparison with Other Approaches

| Feature | Vimina | Vision Model | OpenClaw |
|---------|--------|--------------|----------|
| Control recognition | ✅ UIA3 precise | Visual guessing | Skills |
| Background operations | ✅ Full support | ❌ | ⚠️ Config dependent |
| HTTP API | ✅ Complete | ❌ | ✅ |
| AI integration cost | ✅ Very low | High | Medium |
| Token consumption | ~50 | ~2000 | ~500 |
| Open source | ✅ MIT | ❌ | ✅ MIT |

---

## Common Issues

| Issue | Solution |
|-------|----------|
| Cannot connect to API | Ensure Vimina.exe is running |
| Control not found | Use `/api/scanAll` to see all elements |
| Backend click fails | Some controls require foreground focus |
| Browser control recognition incomplete | Enable accessibility support in Chrome etc. |

---

## Resources

- **Documentation**: https://sunse666.github.io/Vimina-docs/
- **GitHub**: https://github.com/Sunse666/Vimina
- **Download**: https://github.com/Sunse666/Vimina/releases
