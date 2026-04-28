using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Text.Json;
using Vimina.Core.Automation;
using Vimina.Core.Config;
using Vimina.Core.Helpers;
using Vimina.Core.Scripting;

using File = System.IO.File;

namespace Vimina.Core.Api;

public class ApiServer
{
    private readonly MainWindow _mainWindow;
    private readonly VmaEngine _vmaEngine;
    public int Port { get; private set; } = 51401;

    public ApiServer(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        _vmaEngine = new VmaEngine();
    }

    public void Start()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseKestrel(options =>
        {
            options.ListenAnyIP(Port);
        });

        var app = builder.Build();

        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
            context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type");
            context.Response.Headers.Append("Content-Type", "application/json; charset=utf-8");
            if (context.Request.Method == "OPTIONS")
            {
                context.Response.StatusCode = 200;
                return;
            }
            await next();
        });

        MapEndpoints(app);
        
        _ = app.RunAsync();
    }

    private void MapEndpoints(WebApplication app)
    {
        static bool? ParseBoolParam(IQueryCollection query, string key)
        {
            if (!query.ContainsKey(key)) return null;
            var value = query[key].ToString();
            if (string.IsNullOrEmpty(value)) return null;
            
            return value.ToLowerInvariant() switch
            {
                "1" or "true" or "yes" => true,
                "0" or "false" or "no" => false,
                _ => bool.TryParse(value, out var b) ? b : null
            };
        }
        app.MapGet("/api", () => Results.Json(new
        {
            name = "Vimina API",
            version = "1.5",
            description = "控件自动化操作接口",
            endpoints = new[]
            {
                "GET  /api/scan                          - 获取扫描结果(仅可交互控件)",
                "GET  /api/scanAll                       - 扫描所有控件(包括文本、图片等)",
                "GET  /api/scanAllByTitle?title=xxx      - 通过窗口标题扫描所有控件",
                "POST /api/scanAllByTitle                - 通过窗口标题扫描所有控件",
                "POST /api/click                         - 点击控件",
                "POST /api/click?label=xx&right=1        - 右键点击控件",
                "POST /api/click?label=xx&double=1       - 双击控件",
                "GET  /api/click/{x}/{y}                 - 坐标点击",
                "GET  /api/click/{x}/{y}?useBackend=1    - 坐标点击(后台模式)",
                "GET  /api/clickR/{x}/{y}                - 坐标右键点击",
                "GET  /api/clickR/{x}/{y}?useBackend=1   - 坐标右键点击(后台模式)",
                "GET  /api/dblclick/{x}/{y}              - 坐标双击",
                "GET  /api/dblclick/{x}/{y}?useBackend=1 - 坐标双击(后台模式)",
                "GET  /api/clickAt?x=&y=&useBackend=1    - 坐标点击(可指定后台模式)",
                "POST /api/clickAt                       - 坐标点击",
                "GET  /api/windows                       - 获取所有窗口列表(排除系统应用)",
                "GET  /api/scanByTitle?title=xxx         - 通过窗口标题扫描控件(仅可交互)",
                "POST /api/scanByTitle                   - 通过窗口标题扫描控件(仅可交互)",
                "GET  /api/clickByTitle?title=xxx&x=&y=  - 通过窗口标题点击控件(支持后台)",
                "POST /api/clickByTitle                  - 通过窗口标题点击控件(支持后台)",
                "GET  /api/activate?title=xxx            - 激活窗口(切换到前台)",
                "POST /api/input                         - 输入文本",
                "POST /api/show                          - 显示标签并扫描",
                "POST /api/hide                          - 隐藏标签",
                "GET  /api/status                        - 检查状态",
                "GET  /api/mouse                         - 获取鼠标当前位置",
                "GET  /api/move/{x}/{y}                  - 移动鼠标到指定位置",
                "GET  /api/drag/{x1}/{y1}/{x2}/{y2}      - 拖拽操作",
                "POST /api/keypress                      - 按键",
                "POST /api/keydown                       - 按下键",
                "POST /api/keyup                         - 释放键",
                "GET  /api/mousedown/{button}/{x}/{y}    - 鼠标按下",
                "GET  /api/mouseup/{button}/{x}/{y}      - 鼠标释放",
                "POST /api/vma/run                       - 运行 VMA 脚本",
                "POST /api/vma/runFile                   - 从文件运行 VMA 脚本",
                "GET  /api/vma/stop                      - 停止 VMA 脚本",
                "GET  /api/vma/pause                     - 暂停 VMA 脚本",
                "GET  /api/vma/resume                    - 恢复 VMA 脚本",
                "GET  /api/vma/status                    - 获取 VMA 引擎状态",
                "GET  /api/vma/log                       - 获取 VMA 执行日志",
            }
        }));

        app.MapGet("/api/scan", () =>
        {
            var result = JsonFileHelper.Load<WindowScanResult>(ConfigManager.ScanResultPath);
            return result != null
                ? Results.Json(result)
                : Results.Json(new { error = "无扫描结果" });
        });

        app.MapGet("/api/scanAll", () =>
        {
            var foreHwnd = WindowHelper.GetForegroundWindow();
            if (foreHwnd == IntPtr.Zero) return Results.Json(new { error = "无前台窗口" });

            using var scanner = new ControlScanner();
            var controls = scanner.ScanAllControls(foreHwnd);
            var windowInfo = new Helpers.WindowInfo(foreHwnd,
                WindowHelper.GetWindowTitle(foreHwnd),
                WindowHelper.GetWindowClass(foreHwnd), 0);
            var result = scanner.BuildScanResult(controls, windowInfo);
            return Results.Json(result);
        });

        app.MapPost("/api/click", async (HttpContext context) =>
        {
            var body = await JsonSerializer.DeserializeAsync<ClickRequest>(context.Request.Body);
            if (body?.Label == null) return Results.BadRequest(new { error = "缺少 label" });

            var labelMap = JsonFileHelper.Load<LabelMap>(ConfigManager.LabelMapPath);
            if (labelMap == null || !labelMap.TryGetValue(body.Label, out var pos))
                return Results.Json(new { error = $"标签 {body.Label} 不存在" });

            using var engine = new ClickEngine();
            var result = engine.ClickAt(pos.CenterX, pos.CenterY, body.Right, body.Double);
            return Results.Json(result);
        });

        app.MapGet("/api/click/{x}/{y}", (int x, int y, bool? useBackend) =>
        {
            using var engine = new ClickEngine();
            var result = engine.ClickAt(x, y, useFlaUI: useBackend);
            return Results.Json(result);
        });

        app.MapGet("/api/clickR/{x}/{y}", (int x, int y, bool? useBackend) =>
        {
            using var engine = new ClickEngine();
            var result = engine.ClickAt(x, y, rightClick: true, useFlaUI: useBackend);
            return Results.Json(result);
        });

        app.MapGet("/api/clickM/{x}/{y}", (int x, int y) =>
        {
            using var engine = new ClickEngine();
            var result = engine.ClickAt(x, y, middleClick: true);
            return Results.Json(result);
        });

        app.MapGet("/api/dblclick/{x}/{y}", (int x, int y, bool? useBackend) =>
        {
            using var engine = new ClickEngine();
            var result = engine.ClickAt(x, y, doubleClick: true, useFlaUI: useBackend);
            return Results.Json(result);
        });

        app.MapGet("/api/mousedown/{button}/{x}/{y}", (string button, int x, int y) =>
        {
            MouseHelper.MoveTo(x, y);
            var btn = button.ToLowerInvariant() switch
            {
                "left" => MouseHelper.MouseButton.Left,
                "right" => MouseHelper.MouseButton.Right,
                "middle" => MouseHelper.MouseButton.Middle,
                _ => MouseHelper.MouseButton.Left
            };
            MouseHelper.MouseDown(btn);
            return Results.Json(new { success = true, action = "mousedown", button, x, y });
        });

        app.MapGet("/api/mouseup/{button}/{x}/{y}", (string button, int x, int y) =>
        {
            MouseHelper.MoveTo(x, y);
            var btn = button.ToLowerInvariant() switch
            {
                "left" => MouseHelper.MouseButton.Left,
                "right" => MouseHelper.MouseButton.Right,
                "middle" => MouseHelper.MouseButton.Middle,
                _ => MouseHelper.MouseButton.Left
            };
            MouseHelper.MouseUp(btn);
            return Results.Json(new { success = true, action = "mouseup", button, x, y });
        });

        app.MapPost("/api/keypress", async (HttpContext context) =>
        {
            var body = await JsonSerializer.DeserializeAsync<KeyRequest>(context.Request.Body);
            if (body?.Key == null) return Results.BadRequest(new { error = "缺少 key" });
            
            KeyboardHelper.KeyPress(body.Key);
            return Results.Json(new { success = true, key = body.Key });
        });

        app.MapPost("/api/keydown", async (HttpContext context) =>
        {
            var body = await JsonSerializer.DeserializeAsync<KeyRequest>(context.Request.Body);
            if (body?.Key == null) return Results.BadRequest(new { error = "缺少 key" });
            
            KeyboardHelper.KeyDown(body.Key);
            return Results.Json(new { success = true, key = body.Key, action = "down" });
        });

        app.MapPost("/api/keyup", async (HttpContext context) =>
        {
            var body = await JsonSerializer.DeserializeAsync<KeyRequest>(context.Request.Body);
            if (body?.Key == null) return Results.BadRequest(new { error = "缺少 key" });
            
            KeyboardHelper.KeyUp(body.Key);
            return Results.Json(new { success = true, key = body.Key, action = "up" });
        });

        app.MapGet("/api/activate", (string title) =>
        {
            var hwnd = WindowHelper.FindWindowByTitle(title);
            if (hwnd == IntPtr.Zero) return Results.Json(new { error = $"未找到窗口: {title}" });
            
            var result = WindowHelper.ForceForegroundWindow(hwnd);
            return Results.Json(new { success = result, title, hwnd = hwnd.ToInt64() });
        });

        app.MapPost("/api/activate", async (HttpContext context) =>
        {
            var body = await JsonSerializer.DeserializeAsync<ActivateRequest>(context.Request.Body);
            if (body?.Title == null) return Results.BadRequest(new { error = "缺少 title 参数" });
            
            var hwnd = WindowHelper.FindWindowByTitle(body.Title);
            if (hwnd == IntPtr.Zero) return Results.Json(new { error = $"未找到窗口: {body.Title}" });
            
            var result = WindowHelper.ForceForegroundWindow(hwnd);
            return Results.Json(new { success = result, title = body.Title, hwnd = hwnd.ToInt64() });
        });

        app.MapPost("/api/input", async (HttpContext context) =>
        {
            var body = await JsonSerializer.DeserializeAsync<InputRequest>(context.Request.Body);
            if (body?.Text == null) return Results.BadRequest(new { error = "缺少 text" });
            KeyboardHelper.SendText(body.Text);
            return Results.Json(new { success = true, text = body.Text });
        });

        app.MapPost("/api/show", () =>
        {
            _mainWindow.Dispatcher.Invoke(() => _mainWindow.Show());
            return Results.Json(new { success = true });
        });

        app.MapPost("/api/hide", () =>
        {
            _mainWindow.Dispatcher.Invoke(() => _mainWindow.Hide());
            return Results.Json(new { success = true });
        });

        app.MapGet("/api/status", () => Results.Json(new
        {
            status = "running",
            version = "1.0",
            timestamp = DateTime.Now.ToString("O")
        }));

        app.MapGet("/api/mouse", () =>
        {
            var (x, y) = MouseHelper.GetPosition();
            return Results.Json(new { x, y });
        });

        app.MapGet("/api/move/{x}/{y}", (int x, int y) =>
        {
            MouseHelper.MoveTo(x, y);
            return Results.Json(new { success = true, x, y });
        });

        app.MapGet("/api/drag/{x1}/{y1}/{x2}/{y2}", (int x1, int y1, int x2, int y2) =>
        {
            MouseHelper.Drag(x1, y1, x2, y2);
            return Results.Json(new { success = true, from = new[] { x1, y1 }, to = new[] { x2, y2 } });
        });

        app.MapGet("/api/windows", () =>
        {
            var windows = WindowHelper.GetAllWindows()
                .Select(w => new { w.Title, w.ClassName, hwnd = w.Hwnd.ToInt64(), w.ProcessId })
                .ToList();
            return Results.Json(windows);
        });

        app.MapGet("/api/raw/scan", () =>
        {
            var result = JsonFileHelper.Load<object>(ConfigManager.ScanResultPath);
            return result != null ? Results.Json(result) : Results.Json(new { error = "无扫描结果" });
        });

        app.MapGet("/api/raw/labels", () =>
        {
            var result = JsonFileHelper.Load<object>(ConfigManager.LabelMapPath);
            return result != null ? Results.Json(result) : Results.Json(new { error = "无标签映射" });
        });

        app.MapGet("/api/scanByTitle", (string title) =>
        {
            var hwnd = WindowHelper.FindWindowByTitle(title);
            if (hwnd == IntPtr.Zero) return Results.Json(new { error = $"未找到窗口: {title}" });

            using var scanner = new ControlScanner();
            var controls = scanner.ScanInteractiveControls(hwnd);
            var windowInfo = new Helpers.WindowInfo(hwnd,
                WindowHelper.GetWindowTitle(hwnd),
                WindowHelper.GetWindowClass(hwnd), 0);
            var result = scanner.BuildScanResult(controls, windowInfo);
            var resultLite = scanner.BuildScanResultLite(controls, windowInfo);
            
            var labelMap = BuildLabelMap(controls);
            JsonFileHelper.Save(ConfigManager.ScanResultPath, result);
            JsonFileHelper.Save(ConfigManager.ScanResultLitePath, resultLite);
            JsonFileHelper.Save(ConfigManager.LabelMapPath, labelMap);
            
            return Results.Json(result);
        });

        app.MapPost("/api/scanByTitle", async (HttpContext context) =>
        {
            var body = await JsonSerializer.DeserializeAsync<ScanByTitleRequest>(context.Request.Body);
            if (body?.Title == null) return Results.BadRequest(new { error = "缺少 title" });

            var hwnd = WindowHelper.FindWindowByTitle(body.Title);
            if (hwnd == IntPtr.Zero) return Results.Json(new { error = $"未找到窗口: {body.Title}" });

            using var scanner = new ControlScanner();
            var controls = scanner.ScanInteractiveControls(hwnd);
            var windowInfo = new Helpers.WindowInfo(hwnd,
                WindowHelper.GetWindowTitle(hwnd),
                WindowHelper.GetWindowClass(hwnd), 0);
            var result = scanner.BuildScanResult(controls, windowInfo);
            var resultLite = scanner.BuildScanResultLite(controls, windowInfo);
            
            var labelMap = BuildLabelMap(controls);
            JsonFileHelper.Save(ConfigManager.ScanResultPath, result);
            JsonFileHelper.Save(ConfigManager.ScanResultLitePath, resultLite);
            JsonFileHelper.Save(ConfigManager.LabelMapPath, labelMap);
            
            return Results.Json(result);
        });

        app.MapGet("/api/scanAllByTitle", (string title) =>
        {
            var hwnd = WindowHelper.FindWindowByTitle(title);
            if (hwnd == IntPtr.Zero) return Results.Json(new { error = $"未找到窗口: {title}" });

            using var scanner = new ControlScanner();
            var controls = scanner.ScanAllControls(hwnd);
            var windowInfo = new Helpers.WindowInfo(hwnd,
                WindowHelper.GetWindowTitle(hwnd),
                WindowHelper.GetWindowClass(hwnd), 0);
            var result = scanner.BuildScanResult(controls, windowInfo);
            var resultLite = scanner.BuildScanResultLite(controls, windowInfo);
            
            var labelMap = BuildLabelMap(controls);
            JsonFileHelper.Save(ConfigManager.ScanResultPath, result);
            JsonFileHelper.Save(ConfigManager.ScanResultLitePath, resultLite);
            JsonFileHelper.Save(ConfigManager.LabelMapPath, labelMap);
            
            return Results.Json(result);
        });

        app.MapPost("/api/scanAllByTitle", async (HttpContext context) =>
        {
            var body = await JsonSerializer.DeserializeAsync<ScanByTitleRequest>(context.Request.Body);
            if (body?.Title == null) return Results.BadRequest(new { error = "缺少 title" });

            var hwnd = WindowHelper.FindWindowByTitle(body.Title);
            if (hwnd == IntPtr.Zero) return Results.Json(new { error = $"未找到窗口: {body.Title}" });

            using var scanner = new ControlScanner();
            var controls = scanner.ScanAllControls(hwnd);
            var windowInfo = new Helpers.WindowInfo(hwnd,
                WindowHelper.GetWindowTitle(hwnd),
                WindowHelper.GetWindowClass(hwnd), 0);
            var result = scanner.BuildScanResult(controls, windowInfo);
            var resultLite = scanner.BuildScanResultLite(controls, windowInfo);
            
            var labelMap = BuildLabelMap(controls);
            JsonFileHelper.Save(ConfigManager.ScanResultPath, result);
            JsonFileHelper.Save(ConfigManager.ScanResultLitePath, resultLite);
            JsonFileHelper.Save(ConfigManager.LabelMapPath, labelMap);
            
            return Results.Json(result);
        });

        app.MapGet("/api/clickAt", (int x, int y, bool? right, bool? @double, bool? useBackend) =>
        {
            using var engine = new ClickEngine();
            var result = engine.ClickAt(x, y, right ?? false, @double ?? false, useFlaUI: useBackend);
            return Results.Json(result);
        });

        app.MapPost("/api/clickAt", async (HttpContext context) =>
        {
            var body = await JsonSerializer.DeserializeAsync<ClickAtRequest>(context.Request.Body);
            if (body == null) return Results.BadRequest(new { error = "无效请求" });

            using var engine = new ClickEngine();
            var result = engine.ClickAt(body.X, body.Y, body.Right, body.Double, useFlaUI: body.UseBackend);
            return Results.Json(result);
        });

        app.MapGet("/api/clickByTitle", (HttpContext context) =>
        {
            var query = context.Request.Query;
            var title = query["title"].ToString();
            if (string.IsNullOrEmpty(title)) 
                return Results.BadRequest(new { error = "缺少 title 参数" });
            
            if (!int.TryParse(query["x"], out var x))
                return Results.BadRequest(new { error = "缺少或无效的 x 参数" });
            
            if (!int.TryParse(query["y"], out var y))
                return Results.BadRequest(new { error = "缺少或无效的 y 参数" });

            var right = ParseBoolParam(query, "right") ?? false;
            var doubleClick = ParseBoolParam(query, "double") ?? ParseBoolParam(query, "dbl") ?? false;
            var useBackend = ParseBoolParam(query, "useBackend") ?? ParseBoolParam(query, "usebackend") ?? ParseBoolParam(query, "backend");
            var bringToFront = ParseBoolParam(query, "bringToFront") ?? ParseBoolParam(query, "bringtofront") ?? ParseBoolParam(query, "bringfront");
            var useMouse = ParseBoolParam(query, "useMouse") ?? ParseBoolParam(query, "usemouse") ?? false;

            var hwnd = WindowHelper.FindWindowByTitle(title);
            if (hwnd == IntPtr.Zero) return Results.Json(new { error = $"未找到窗口: {title}" });

            var bringFront = bringToFront ?? (useBackend == true ? false : true);
            if (bringFront)
            {
                WindowHelper.ForceForegroundWindow(hwnd);
                Thread.Sleep(200);
            }

            using var engine = new ClickEngine();
            var useFlaUI = useMouse ? false : useBackend;
            var result = engine.ClickAt(x, y, right, doubleClick, targetHwnd: hwnd, useFlaUI: useFlaUI, bringToFront: bringFront);
            return Results.Json(result);
        });

        app.MapPost("/api/clickByTitle", async (HttpContext context) =>
        {
            var body = await JsonSerializer.DeserializeAsync<ClickByTitleRequest>(context.Request.Body);
            if (body?.Title == null) return Results.BadRequest(new { error = "缺少 title" });

            var hwnd = WindowHelper.FindWindowByTitle(body.Title);
            if (hwnd == IntPtr.Zero) return Results.Json(new { error = $"未找到窗口: {body.Title}" });

            var bringFront = body.BringToFront ?? (body.UseBackend == true ? false : true);
            if (bringFront)
            {
                WindowHelper.ForceForegroundWindow(hwnd);
                Thread.Sleep(200);
            }

            using var engine = new ClickEngine();
            var result = engine.ClickAt(body.X, body.Y, body.Right, body.Double, targetHwnd: hwnd, useFlaUI: body.UseBackend, bringToFront: bringFront);
            return Results.Json(result);
        });

        app.MapPost("/api/vma/run", async (HttpContext context) =>
        {
            var body = await JsonSerializer.DeserializeAsync<VmaScriptRequest>(context.Request.Body);
            if (body?.Script == null) return Results.BadRequest(new { error = "缺少 script" });
            
            try
            {
                var result = await _vmaEngine.RunAsync(body.Script);
                return Results.Json(new 
                { 
                    success = result.Success, 
                    linesExecuted = result.LinesExecuted,
                    error = result.Error,
                    log = result.Log
                });
            }
            catch (Exception ex)
            {
                return Results.Json(new { success = false, error = ex.Message });
            }
        });

        app.MapPost("/api/vma/runFile", async (HttpContext context) =>
        {
            var body = await JsonSerializer.DeserializeAsync<VmaFileRequest>(context.Request.Body);
            if (body?.File == null) return Results.BadRequest(new { error = "缺少 file" });
            
            try
            {
                if (!File.Exists(body.File))
                    return Results.Json(new { success = false, error = $"文件不存在: {body.File}" });
                
                var script = await File.ReadAllTextAsync(body.File);
                var result = await _vmaEngine.RunAsync(script);
                return Results.Json(new 
                { 
                    success = result.Success, 
                    linesExecuted = result.LinesExecuted,
                    error = result.Error,
                    log = result.Log
                });
            }
            catch (Exception ex)
            {
                return Results.Json(new { success = false, error = ex.Message });
            }
        });

        app.MapGet("/api/vma/stop", () =>
        {
            _vmaEngine.Stop();
            return Results.Json(new { success = true, message = "VMA 脚本已停止" });
        });

        app.MapPost("/api/vma/stop", () =>
        {
            _vmaEngine.Stop();
            return Results.Json(new { success = true, message = "VMA 脚本已停止" });
        });

        app.MapGet("/api/vma/pause", () =>
        {
            _vmaEngine.Pause();
            return Results.Json(new { success = true, message = "VMA 脚本已暂停" });
        });

        app.MapGet("/api/vma/resume", () =>
        {
            _vmaEngine.Resume();
            return Results.Json(new { success = true, message = "VMA 脚本已恢复" });
        });

        app.MapGet("/api/vma/status", () =>
        {
            return Results.Json(new 
            { 
                running = _vmaEngine.IsRunning, 
                paused = _vmaEngine.IsPaused,
                currentLine = _vmaEngine.CurrentLine,
                totalLines = _vmaEngine.TotalLines,
                variables = _vmaEngine.Variables
            });
        });

        app.MapGet("/api/vma/log", () =>
        {
            return Results.Json(new { log = _vmaEngine.Log });
        });
    }

    private static LabelMap BuildLabelMap(List<ControlInfo> controls)
    {
        var labelMap = new LabelMap();
        foreach (var ctrl in controls)
        {
            if (!string.IsNullOrEmpty(ctrl.Label))
            {
                labelMap[ctrl.Label] = new LabelPosition 
                { 
                    CenterX = ctrl.CenterX, 
                    CenterY = ctrl.CenterY 
                };
            }
        }
        return labelMap;
    }
}

public class ClickRequest
{
    public string Label { get; set; } = "";
    public bool Right { get; set; }
    public bool Double { get; set; }
}

public class InputRequest
{
    public string Text { get; set; } = "";
}

public class ScanByTitleRequest
{
    public string Title { get; set; } = "";
}

public class ClickAtRequest
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool Right { get; set; }
    public bool Double { get; set; }
    public bool? UseBackend { get; set; }
}

public class ClickByTitleRequest
{
    public string Title { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public bool Right { get; set; }
    public bool Double { get; set; }
    public bool? UseBackend { get; set; }
    public bool? BringToFront { get; set; }
}

public class VmaScriptRequest
{
    public string Script { get; set; } = "";
}

public class VmaFileRequest
{
    public string File { get; set; } = "";
}

public class KeyRequest
{
    public string Key { get; set; } = "";
}

public class ActivateRequest
{
    public string Title { get; set; } = "";
}
