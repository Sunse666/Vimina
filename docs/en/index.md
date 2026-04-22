---
icon: material/home
---

# Vimina

!!! tip "Tip"

    Vimina is a Windows desktop automation tool inspired by [Vimium](https://github.com/philc/vimium).

    - [x] You can press ++left++ and ++right++ to navigate between pages!
    - [x] You can click the search bar at the top or press <kbd>/</kbd> to quickly find content!
    - [x] You can click the theme button at the top to switch the website theme!

    Enjoy reading! :heart:

## Introduction

Vimina is a Windows desktop automation tool. Based on the **FlaUI** automation framework, it identifies window controls through the **UIA3** protocol and generates letter labels for each interactive control. You can click any button, link, or input field precisely just by typing on the keyboard.

It also provides a complete **HTTP API** interface, supporting integration with AI assistants and automation scripts.

## Core Features

=== "Smart Labels"

    Automatically identifies controls based on UIA3 protocol, generates two-letter labels, resolution-independent, window position changes do not affect operations.

=== "HTTP API"

    Complete RESTful interface, supports CORS, easy to integrate with AI assistants and automation scripts.

=== "VMA Script"

    Concise scripting language, supports variables, functions, loops, etc., can be compiled to standalone exe files.

=== "Background Operations"

    Operate background windows without moving the mouse, does not affect foreground work.

## Quick Links

<div class="grid cards" markdown>

-   :fontawesome-solid-download: **Download Latest Version**

    ---

    Download the latest version from GitHub Releases, extract and run

    [:octicons-arrow-right-24: Download](https://github.com/Sunse666/Vimina/releases)

-   :fontawesome-brands-github: **GitHub Repository**

    ---

    View source code, submit issues or PRs

    [:octicons-arrow-right-24: View](https://github.com/Sunse666/Vimina)

-   :material-api: **HTTP API Docs**

    ---

    Complete API endpoints and examples

    [:octicons-arrow-right-24: View Docs](../api/index.md)

-   :material-script-text: **VMA Script Syntax**

    ---

    Detailed syntax and examples

    [:octicons-arrow-right-24: View Syntax](../vma/index.md)

</div>

## System Requirements

- OS: **Windows 10 / 11**
- Runtime: **.NET Framework 4.6.2+**
- No installation required, extract and run

## Getting Started

```bash
# 1. Download and extract
# 2. Run Vimina.exe
# 3. Open any application window
# 4. Press Alt+F to show control labels
# 5. Type label letters to click controls
```

<div align="center" markdown>
[Quick Start](../getting-started.md){ .md-button }
[Basic Usage](../basics.md){ .md-button }
[HTTP API](../api/index.md){ .md-button }
[VMA Script](../vma/index.md){ .md-button }
</div>
