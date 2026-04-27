using System.Runtime.InteropServices;
using System.Windows;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Vimina.Core.Config;
using Vimina.Core.Helpers;

namespace Vimina.Core.Automation;

public class ControlScanner : IDisposable
{
    private UIA3Automation? _automation;

    // Win32 API to get actual screen metrics (in pixels, not DIP)
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int SM_CXSCREEN = 0;  // Primary screen width
    private const int SM_CYSCREEN = 1;  // Primary screen height
    private const int SM_CXVIRTUALSCREEN = 78;  // Virtual screen width
    private const int SM_CYVIRTUALSCREEN = 79;  // Virtual screen height
    private const int SM_XVIRTUALSCREEN = 76;   // Virtual screen left
    private const int SM_YVIRTUALSCREEN = 77;   // Virtual screen top

    public List<ControlInfo> ScanInteractiveControls(IntPtr hwnd)
    {
        _automation?.Dispose();
        _automation = new UIA3Automation();

        var window = _automation.FromHandle(hwnd);
        if (window == null) return new List<ControlInfo>();

        var controls = new List<ControlInfo>();
        // Use GetSystemMetrics to get actual pixel dimensions, not WPF DIP values
        // This ensures proper boundary checking with FlaUI's BoundingRectangle (which returns pixels)
        var screenW = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        var screenH = GetSystemMetrics(SM_CYVIRTUALSCREEN);
        var virtualScreenX = GetSystemMetrics(SM_XVIRTUALSCREEN);
        var virtualScreenY = GetSystemMetrics(SM_YVIRTUALSCREEN);

        System.Diagnostics.Debug.WriteLine($"[FlaUI] Virtual screen: ({virtualScreenX},{virtualScreenY}) {screenW}x{screenH}");
        System.Diagnostics.Debug.WriteLine($"[FlaUI] WPF VirtualScreen: {SystemParameters.VirtualScreenWidth}x{SystemParameters.VirtualScreenHeight}");

        EnumerateControls(window, 0, controls, screenW, screenH, virtualScreenX, virtualScreenY, true);
        // Use 1px grid to minimize deduplication and show more controls
        var deduplicated = DeduplicateByGrid(controls, 1);
        System.Diagnostics.Debug.WriteLine($"[FlaUI] ScanInteractiveControls: Found {controls.Count} controls, after deduplication: {deduplicated.Count}");
        return deduplicated;
    }

    public List<ControlInfo> ScanAllControls(IntPtr hwnd)
    {
        _automation?.Dispose();
        _automation = new UIA3Automation();

        var window = _automation.FromHandle(hwnd);
        if (window == null) return new List<ControlInfo>();

        var controls = new List<ControlInfo>();
        // Use GetSystemMetrics to get actual pixel dimensions, not WPF DIP values
        var screenW = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        var screenH = GetSystemMetrics(SM_CYVIRTUALSCREEN);
        var virtualScreenX = GetSystemMetrics(SM_XVIRTUALSCREEN);
        var virtualScreenY = GetSystemMetrics(SM_YVIRTUALSCREEN);

        EnumerateControls(window, 0, controls, screenW, screenH, virtualScreenX, virtualScreenY, false);
        // Use 1px grid to minimize deduplication and show more controls
        return DeduplicateByGrid(controls, 1);
    }

    private void EnumerateControls(AutomationElement element, int level, List<ControlInfo> controls, int screenW, int screenH, int screenX, int screenY, bool interactiveOnly)
    {
        if (level > ConfigManager.Current.MaxDepth) return;

        try
        {
            // Use FindAllChildren to get all direct children
            var children = element.FindAllChildren();
            foreach (var child in children)
            {
                try
                {
                    var ctrlTypeNum = (int)child.ControlType;
                    var ctrlTypeName = child.ControlType.ToString();
                    var isInteractive = ControlTypeInfo.InteractiveTypeNums.Contains(ctrlTypeNum);
                    var childName = child.Name ?? "";

                    // Debug output - log all controls found
                    System.Diagnostics.Debug.WriteLine($"[FlaUI] Level {level}: {ctrlTypeName}({ctrlTypeNum}) - '{childName}' - Interactive: {isInteractive}");

                    if (interactiveOnly && !isInteractive)
                    {
                        // Even if not interactive, continue to scan children
                        EnumerateControls(child, level + 1, controls, screenW, screenH, screenX, screenY, interactiveOnly);
                        continue;
                    }

                    var bounds = child.BoundingRectangle;
                    if (bounds.IsEmpty) continue;

                    var w = (int)bounds.Width;
                    var h = (int)bounds.Height;
                    var x = (int)bounds.X;
                    var y = (int)bounds.Y;

                    if (w < ConfigManager.Current.MinWidth || h < ConfigManager.Current.MinHeight)
                    {
                        EnumerateControls(child, level + 1, controls, screenW, screenH, screenX, screenY, interactiveOnly);
                        continue;
                    }

                    var screenRight = screenX + screenW;
                    var screenBottom = screenY + screenH;
                    if (x + w <= screenX || y + h <= screenY || x >= screenRight || y >= screenBottom)
                    {
                        EnumerateControls(child, level + 1, controls, screenW, screenH, screenX, screenY, interactiveOnly);
                        continue;
                    }

                    var typeInfo = ControlTypeInfo.AllTypes.GetValueOrDefault(ctrlTypeNum, ControlTypeInfo.AllTypes[0]);
                    var interactiveTypeInfo = ControlTypeInfo.InteractiveTypes.GetValueOrDefault(ctrlTypeNum);

                    var info = new ControlInfo
                    {
                        Name = child.Name ?? "",
                        Type = typeInfo.Name,
                        TypeNum = ctrlTypeNum,
                        TypeDesc = typeInfo.Description,
                        ActionHint = interactiveTypeInfo?.ActionHint ?? "",
                        IsInteractive = ControlTypeInfo.InteractiveTypeNums.Contains(ctrlTypeNum),
                        X = x,
                        Y = y,
                        Width = w,
                        Height = h,
                        Hwnd = element.Properties.NativeWindowHandle.ValueOrDefault.ToInt64()
                    };

                    try { info.AutomationId = child.AutomationId ?? ""; } catch { }
                    try { info.ClassName = child.ClassName ?? ""; } catch { }
                    try { info.HelpText = child.HelpText ?? ""; } catch { }
                    try { info.IsEnabled = child.IsEnabled; } catch { }
                    try { info.IsOffscreen = child.IsOffscreen; } catch { }

                    controls.Add(info);
                    EnumerateControls(child, level + 1, controls, screenW, screenH, screenX, screenY, interactiveOnly);
                }
                catch { }
            }
        }
        catch { }
    }

    private static List<ControlInfo> DeduplicateByGrid(List<ControlInfo> controls, int gridSize)
    {
        var seen = new HashSet<string>();
        var result = new List<ControlInfo>();

        foreach (var ctrl in controls)
        {
            var key = $"{ctrl.CenterX / gridSize * gridSize}_{ctrl.CenterY / gridSize * gridSize}";
            if (seen.Add(key))
                result.Add(ctrl);
        }

        return result;
    }

    public WindowScanResult BuildScanResult(List<ControlInfo> controls, Helpers.WindowInfo windowInfo)
    {
        var typeStats = new Dictionary<string, int>();
        var grouped = new Dictionary<string, ControlGroup>();
        var quickRef = new List<string>();

        foreach (var ctrl in controls)
        {
            typeStats[ctrl.Type] = typeStats.GetValueOrDefault(ctrl.Type) + 1;

            if (!grouped.ContainsKey(ctrl.Type))
            {
                grouped[ctrl.Type] = new ControlGroup
                {
                    TypeName = ctrl.Type,
                    TypeDescription = ctrl.TypeDesc,
                    ActionHint = ctrl.ActionHint,
                    Items = new List<ControlGroupItem>()
                };
            }

            grouped[ctrl.Type].Items!.Add(new ControlGroupItem
            {
                Label = ctrl.Label,
                Name = string.IsNullOrEmpty(ctrl.Name) ? ctrl.TypeDesc : ctrl.Name,
                Position = $"({ctrl.CenterX},{ctrl.CenterY})"
            });

            var desc = string.IsNullOrEmpty(ctrl.Name) ? ctrl.TypeDesc : ctrl.Name;
            quickRef.Add($"{ctrl.Label}: {desc} ({ctrl.Type})");
        }

        return new WindowScanResult
        {
            Success = true,
            Timestamp = DateTime.Now.ToString("O"),
            Window = new WindowInfo
            {
                Handle = windowInfo.Hwnd.ToInt64(),
                Title = windowInfo.Title,
                ClassName = windowInfo.ClassName
            },
            Summary = new ScanSummary
            {
                TotalControls = controls.Count,
                ByType = typeStats,
                Description = $"窗口「{windowInfo.Title}」共有 {controls.Count} 个可交互控件"
            },
            QuickReference = quickRef,
            ControlGroups = grouped.Values.ToList(),
            Controls = controls
        };
    }

    public void Dispose()
    {
        _automation?.Dispose();
        _automation = null;
    }
}
