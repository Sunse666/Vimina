# VMA Script Language Specification

**Version:** 1.0  
**Status:** Draft  
**Date:** 2026-04-21  
**Contact:** [Contact Information]

---

## 1. Introduction

VMA (Vimina Macro Automation) is a scripting language designed for desktop automation on Windows platforms. It provides a simple, human-readable syntax for automating mouse movements, keyboard input, window management, and UI automation tasks.

### 1.1 Purpose

This specification defines the syntax, semantics, and behavior of the VMA scripting language to ensure interoperability between different implementations.

### 1.2 Scope

This document covers:
- Lexical structure
- Data types and variables
- Control flow statements
- Built-in functions
- User-defined functions
- Error handling

### 1.3 Conformance

An implementation conforms to this specification if it supports all required features described in this document.

---

## 2. Lexical Structure

### 2.1 Character Encoding

VMA scripts MUST be encoded in UTF-8. Implementations MUST support scripts with or without a UTF-8 BOM (Byte Order Mark).

### 2.2 Line Terminators

Implementations MUST accept any of the following line terminators:
- CRLF (U+000D U+000A) - Windows
- LF (U+000A) - Unix
- CR (U+000D) - Mac OS Classic

### 2.3 Whitespace

Whitespace characters (space U+0020, tab U+0009) are significant in string literals but are otherwise ignored between tokens.

### 2.4 Comments

Single-line comments begin with `//` and continue to the end of the line:

```vma
// This is a comment
var x = 10 // This is also a comment
```

### 2.5 Identifiers

Identifiers are used for variable names, function names, and labels. An identifier MUST begin with a letter (A-Z, a-z) or underscore (_), followed by any combination of letters, digits (0-9), or underscores.

```
identifier ::= letter { letter | digit | "_" }
letter     ::= "A"-"Z" | "a"-"z" | "_"
digit      ::= "0"-"9"
```

### 2.6 Literals

#### 2.6.1 Numeric Literals

Numeric literals represent floating-point numbers:

```
number ::= [ "-" ] digit+ [ "." digit+ ]
```

Examples: `42`, `-10`, `3.14`, `-0.5`

#### 2.6.2 String Literals

String literals are delimited by double quotes (`"`) or single quotes (`'`):

```
string ::= '"' { character } '"' | "'" { character } "'"
```

Escape sequences are NOT processed in string literals. The literal content is taken as-is.

Examples: `"Hello, World!"`, `'C:\Path\To\File'`

#### 2.6.3 Boolean Literals

Boolean literals are case-insensitive:

```
boolean ::= "true" | "false" | "TRUE" | "FALSE" | "True" | "False"
```

#### 2.6.4 Null Literal

The null literal represents an undefined or empty value:

```
null ::= "null" | "nil" | "NULL" | "NIL" | "Null" | "Nil"
```

---

## 3. Data Types

VMA supports the following data types:

| Type | Description | Example |
|------|-------------|---------|
| Number | 64-bit floating-point number | `42`, `3.14`, `-10` |
| String | UTF-8 encoded text | `"Hello"`, `'World'` |
| Boolean | True or false | `true`, `false` |
| Array | Ordered collection of values | `[1, 2, 3]` |
| Null | Undefined value | `null` |

### 3.1 Type Coercion

VMA performs automatic type coercion in the following contexts:

| From | To | Rule |
|------|-----|------|
| Number | String | Decimal representation |
| String | Number | Parsed as floating-point; 0 if invalid |
| Boolean | Number | true → 1, false → 0 |
| Boolean | String | "true" or "false" |
| Null | Any | Default value for that type |

---

## 4. Variables

### 4.1 Variable Declaration

Variables are declared using the `var` keyword:

```vma
var name = value
var count = 10
var message = "Hello"
```

Variables can also be declared without initialization:

```vma
var x
```

### 4.2 Variable Assignment

Variables are assigned using the `=` operator:

```vma
x = 20
message = "Updated"
```

### 4.3 Scope

Variables have function-local scope when declared inside a function, and global scope otherwise.

### 4.4 Built-in Variables

The following variables are automatically set by certain functions:

| Variable | Type | Description |
|----------|------|-------------|
| `mouseX` | Number | Current mouse X coordinate |
| `mouseY` | Number | Current mouse Y coordinate |
| `screenWidth` | Number | Screen width in pixels |
| `screenHeight` | Number | Screen height in pixels |
| `lastHwnd` | Number | Last found window handle |

---

## 5. Arrays

### 5.1 Array Declaration

Arrays are declared using the `array` keyword:

```vma
array items = [1, 2, 3, 4, 5]
array names = ["Alice", "Bob", "Charlie"]
array empty = []
```

### 5.2 Array Indexing

Arrays are 1-indexed (first element is at index 1):

```vma
var first = items[1]  // First element
var third = items[3]  // Third element
```

### 5.3 Array Assignment

```vma
items[1] = 100
items[2] = "mixed types are allowed"
```

### 5.4 Array Functions

| Function | Description |
|----------|-------------|
| `push(arr, value)` | Add value to end of array |
| `pop(arr)` | Remove last element |
| `shift(arr)` | Remove first element |
| `unshift(arr, value)` | Add value to beginning |
| `length(arr)` | Return array length |

---

## 6. Operators

### 6.1 Arithmetic Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `+` | Addition | `a + b` |
| `-` | Subtraction | `a - b` |
| `*` | Multiplication | `a * b` |
| `/` | Division | `a / b` |
| `%` | Modulo | `a % b` |

### 6.2 Comparison Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `==` | Equal | `a == b` |
| `!=` | Not equal | `a != b` |
| `<` | Less than | `a < b` |
| `>` | Greater than | `a > b` |
| `<=` | Less than or equal | `a <= b` |
| `>=` | Greater than or equal | `a >= b` |

### 6.3 Logical Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `and` | Logical AND | `a and b` |
| `or` | Logical OR | `a or b` |
| `not` | Logical NOT | `not a` |

### 6.4 String Concatenation

The `+` operator concatenates strings:

```vma
var result = "Hello" + " " + "World"  // "Hello World"
```

---

## 7. Control Flow

### 7.1 Conditional Statements

```vma
if condition
  // statements
endif

if condition
  // statements
else
  // statements
endif

if condition1
  // statements
elseif condition2
  // statements
else
  // statements
endif
```

### 7.2 Loop Statements

#### 7.2.1 Count Loop

```vma
loop 10
  // statements (executed 10 times)
endloop
```

#### 7.2.2 While Loop

```vma
while condition
  // statements
endwhile
```

#### 7.2.3 For Loop

```vma
for i = 1 to 10
  // statements
next

for i = 10 to 1 step -1
  // statements (counting down)
next
```

#### 7.2.4 Foreach Loop

```vma
foreach item in array
  // statements
next
```

### 7.3 Loop Control

| Statement | Description |
|-----------|-------------|
| `break` | Exit the innermost loop |
| `continue` | Skip to next iteration |

### 7.4 Labels and Goto

```vma
start:
  // statements
  if condition
    goto start
  endif
```

---

## 8. Functions

### 8.1 User-Defined Functions

```vma
function functionName(param1, param2)
  // statements
  return value
endfunction
```

Example:

```vma
function add(a, b)
  return a + b
endfunction

var result = add(10, 20)  // result = 30
```

### 8.2 Function Calls

Functions are called with parentheses:

```vma
var result = functionName(arg1, arg2)
```

### 8.3 Named Parameters

Some built-in functions support named parameters:

```vma
click(100, 200, rightClick=true, doubleClick=false)
```

---

## 9. Built-in Functions

### 9.1 Mouse Functions

| Function | Description |
|----------|-------------|
| `click(x, y, rightClick=false, doubleClick=false, useBackend=false)` | Click at coordinates |
| `rightClick(x, y, useBackend=false)` | Right-click at coordinates |
| `doubleClick(x, y, useBackend=false)` | Double-click at coordinates |
| `clickByTitle(title, x, y, rightClick=false, doubleClick=false, useBackend=false, bringToFront=true)` | Click in window by title |
| `moveTo(x, y)` | Move mouse to coordinates |
| `drag(x1, y1, x2, y2)` | Drag from (x1,y1) to (x2,y2) |
| `getMousePos()` | Get current mouse position (sets mouseX, mouseY) |

### 9.2 Keyboard Functions

| Function | Description |
|----------|-------------|
| `input(text)` | Type text at current cursor position |
| `keyPress(key)` | Press and release key |
| `keyDown(key)` | Press key (hold) |
| `keyUp(key)` | Release key |

Key combinations use `+` separator: `keyPress("Ctrl+C")`

### 9.3 Window Functions

| Function | Description |
|----------|-------------|
| `activate(title)` | Activate (bring to front) window by title |
| `findWindow(title)` | Find window by title (sets lastHwnd) |
| `closeWindow(title)` | Close window by title |
| `minimizeWindow(title)` | Minimize window |
| `maximizeWindow(title)` | Maximize window |
| `restoreWindow(title)` | Restore window |
| `waitFor(title, timeout=30)` | Wait for window to appear (seconds) |
| `windowExists(title)` | Returns true if window exists |
| `windowActive(title)` | Returns true if window is active |

### 9.4 Utility Functions

| Function | Description |
|----------|-------------|
| `sleep(ms)` | Pause execution for milliseconds |
| `log(message)` | Output message to log |
| `msg(message)` | Display message box |
| `screenshot()` | Take screenshot and return filename |
| `getScreenSize()` | Get screen dimensions (sets screenWidth, screenHeight) |

### 9.5 Math Functions

| Function | Description |
|----------|-------------|
| `rand(min, max)` | Random integer in range [min, max] |
| `abs(x)` | Absolute value |
| `floor(x)` | Round down to integer |
| `ceil(x)` | Round up to integer |
| `min(a, b, ...)` | Minimum value |
| `max(a, b, ...)` | Maximum value |

### 9.6 Type Functions

| Function | Description |
|----------|-------------|
| `toInt(x)` | Convert to integer |
| `toString(x)` | Convert to string |
| `length(x)` | Length of string or array |
| `type(x)` | Return type name ("number", "string", "boolean", "array", "null") |
| `isArray(x)` | Returns true if x is an array |

---

## 10. Error Handling

### 10.1 Runtime Errors

When a runtime error occurs, the script execution is halted and an error message is logged. Common runtime errors include:

- Division by zero
- Array index out of bounds
- Undefined variable access
- Invalid function arguments

### 10.2 Error Return Values

Functions that may fail return a result object with the following structure:

```json
{
  "success": true|false,
  "error": "error message if failed",
  "line": line_number
}
```

---

## 11. File Format

### 11.1 File Extension

VMA script files use the `.vma` extension.

### 11.2 MIME Type

The MIME type for VMA scripts is `application/vnd.vimina.vma`.

### 11.3 File Structure

A VMA file contains:
1. Optional comments and blank lines at the top
2. Script statements
3. Optional function definitions

Example file:

```vma
// My Automation Script
// Author: Name
// Date: 2026-04-21

// Main script
getMousePos()
log("Starting at: " + mouseX + ", " + mouseY)

// Loop through 10 clicks
for i = 1 to 10
  click(100 + i * 50, 200)
  sleep(500)
next

// Helper function
function doClick(x, y)
  click(x, y)
  log("Clicked at " + x + ", " + y)
endfunction
```

---

## 12. Security Considerations

### 12.1 Script Execution

VMA scripts can control mouse and keyboard input, which may pose security risks if scripts from untrusted sources are executed.

### 12.2 Recommended Practices

1. Review scripts before execution
2. Use sandboxing for untrusted scripts
3. Limit script permissions when possible
4. Log all script actions for audit purposes

### 12.3 Dangerous Operations

The following operations should require user confirmation:
- Sending input to password fields
- Executing external programs
- Modifying system settings

---

## 13. Conformance Requirements

### 13.1 Required Features

A conforming implementation MUST support:
- All data types defined in Section 3
- All operators defined in Section 6
- All control flow statements defined in Section 7
- All built-in functions defined in Section 9

### 13.2 Optional Features

A conforming implementation MAY support:
- Additional built-in functions
- Extension mechanisms
- Debugging facilities

---

## 14. References

- RFC 6838 - Media Type Specifications and Registration Procedures
- RFC 2046 - Multipurpose Internet Mail Extensions (MIME) Part Two

---

## 15. Revision History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-04-21 | Initial specification |

---

## 16. Contact

For questions or feedback regarding this specification, please contact:

- **Email:** [Sunse666@163.com]
- **Website:** https://github.com/Sunse666/Vimina

---

*This document is provided under the [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) license.*
