---
name: "vimina-automation-lite"
description: "Vimina Windows automation. Use when user needs desktop automation, GUI operations, or window control."
---

# Vimina Automation (Lite)

⚠️ **MUST follow this workflow. DO NOT use other methods.**

## ⛔ CRITICAL RULE #1: TARGET WINDOW MUST BE IN FOREGROUND

**Before ANY operation (click, input, scan), the target window MUST be in the foreground.**

- Background window operations have extremely high failure rate
- Always call `/api/activate?title=xxx` BEFORE any scan, click, or input
- This is NON-NEGOTIABLE. Every interaction MUST start with activation

## ⛔ CRITICAL: NO QUOTES IN CURL COMMANDS

**NEVER use quotes around URLs in curl commands! This will cause execution failure!**

```
❌ WRONG: curl "http://localhost:51401/api/windows"
          → This becomes: curl /"http://localhost:51401/api/windows/"
          → EXECUTION FAILS!

✅ CORRECT: curl http://localhost:51401/api/windows
           → No quotes, works perfectly!
```

**When generating commands, output URL directly without ANY quotes!**

## Execution Rules
- **Execute immediately**: When user asks to perform any operation, execute directly using Vimina
- **No confirmation needed**: Do NOT ask for user permission before executing
- **No progress reports**: Do NOT report what you are doing during execution
- **Reply after completion**: Only respond after the entire operation is finished
- **NO QUOTES**: NEVER add quotes around URLs in curl commands

## Step 1: Get Windows
```bash
curl http://localhost:51401/api/windows
```
(NO QUOTES around URL!)

## Step 2: Scan Window (MUST activate first, then scan BOTH)
```bash
# MANDATORY: activate first - background operations WILL fail!
curl http://127.0.0.1:51401/api/activate?title=TITLE
curl http://localhost:51401/api/scanAllByTitle?title=TITLE
```
(NO QUOTES! Replace TITLE with actual window title.)

**⛔ If window is not in foreground, scanned controls will be INCOMPLETE and clicks will FAIL!**

**Window Title Matching Tips:**
- Title supports **regex matching** - use partial title is OK
- **Use English if available** - avoids encoding issues
- Example: Window "哔哩哔哩 (bilibili)" → use `title=bilibili` instead of Chinese

## Step 3: Read Coordinates
Read **BOTH** files together:
- `data/scan_result_lite.json` - labels, types, center coordinates
- `data/scan_result_tree.json` - control hierarchy

**scan_result_lite.json**:
```
DJ: button (Button) [100, 200]
```
Format: `Label: Name (Type) [CenterX, CenterY]`

**scan_result_tree.json**:
```json
{
  "controlTree": {
    "name": "Window Title",
    "x": 0, "y": 0,
    "children": [
      { "name": "button", "x": 100, "y": 200, "children": [...] }
    ]
  }
}
```
- `name`: control text, `x, y`: position, `children`: nested controls
- Combine both files: lite for click coordinates, tree for layout understanding

## Step 4: Click (activate window first if needed, ONLY coordinate-based)
```bash
curl http://localhost:51401/api/click/X/Y
curl http://localhost:51401/api/clickR/X/Y
curl http://localhost:51401/api/dblclick/X/Y
```
(NO QUOTES! Background clicks have high failure rate - ensure window is foreground!)

## Step 5: Input Text
```bash
curl -X POST http://localhost:51401/api/input -H Content-Type:application/json -d {text:your_text}
```

## Example
```
User: "Click Notepad File menu"

1. curl http://localhost:51401/api/windows
2. curl http://127.0.0.1:51401/api/activate?title=Notepad  ← MUST activate first!
3. curl http://localhost:51401/api/scanAllByTitle?title=Notepad
4. Read data/scan_result_lite.json → DK: File (MenuItem) [50, 30]
5. curl http://127.0.0.1:51401/api/activate?title=Notepad  ← re-activate before clicking!
6. curl http://localhost:51401/api/click/50/30
```

## URL Encoding for Special Characters
Use URL encoding instead of quotes:
- Space: %20
- Chinese: %E5%93%94%E5%93%A9

```
Title: 哔哩哔哩 → title=%E5%93%94%E5%93%A9%E5%93%94%E5%93%A9
Command: curl http://localhost:51401/api/scanAllByTitle?title=%E5%93%94%E5%93%A9%E5%93%94%E5%93%A9
(NO QUOTES!)
```
