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
    private readonly object _lockObj = new();

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;
    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;

    public List<ControlInfo> ScanInteractiveControls(IntPtr hwnd)
    {
        _automation?.Dispose();
        _automation = new UIA3Automation();

        var window = _automation.FromHandle(hwnd);
        if (window == null) return new List<ControlInfo>();

        var controls = new List<ControlInfo>();
        var screenW = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        var screenH = GetSystemMetrics(SM_CYVIRTUALSCREEN);
        var virtualScreenX = GetSystemMetrics(SM_XVIRTUALSCREEN);
        var virtualScreenY = GetSystemMetrics(SM_YVIRTUALSCREEN);

        System.Diagnostics.Debug.WriteLine($"[FlaUI] Virtual screen: ({virtualScreenX},{virtualScreenY}) {screenW}x{screenH}");
        System.Diagnostics.Debug.WriteLine($"[FlaUI] WPF VirtualScreen: {SystemParameters.VirtualScreenWidth}x{SystemParameters.VirtualScreenHeight}");

        EnumerateControls(window, 0, controls, screenW, screenH, virtualScreenX, virtualScreenY, true);
        var deduplicated = DeduplicateByGrid(controls, 1);
        
        LabelGenerator.Reset();
        foreach (var ctrl in deduplicated)
        {
            ctrl.Label = LabelGenerator.Next();
        }
        
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
        var deduplicated = DeduplicateByGrid(controls, 1);
        
        LabelGenerator.Reset();
        foreach (var ctrl in deduplicated)
        {
            ctrl.Label = LabelGenerator.Next();
        }
        
        return deduplicated;
    }

    private void EnumerateControls(AutomationElement element, int level, List<ControlInfo> controls, int screenW, int screenH, int screenX, int screenY, bool interactiveOnly)
    {
        if (level > ConfigManager.Current.MaxDepth) return;

        try
        {
            var children = element.FindAllChildren();
            var childrenList = children.ToList();
            
            if (level < 3)
            {
                Parallel.ForEach(childrenList, child =>
                {
                    ProcessChildControl(child, level, controls, screenW, screenH, screenX, screenY, interactiveOnly);
                });
            }
            else
            {
                foreach (var child in childrenList)
                {
                    ProcessChildControl(child, level, controls, screenW, screenH, screenX, screenY, interactiveOnly);
                }
            }
        }
        catch { }
    }

    private void ProcessChildControl(AutomationElement child, int level, List<ControlInfo> controls, int screenW, int screenH, int screenX, int screenY, bool interactiveOnly)
    {
        try
        {
            var ctrlTypeNum = (int)child.ControlType;
            var ctrlTypeName = child.ControlType.ToString();
            var isInteractive = ControlTypeInfo.InteractiveTypeNums.Contains(ctrlTypeNum);
            var childName = child.Name ?? "";

            if (interactiveOnly && !isInteractive)
            {
                EnumerateControls(child, level + 1, controls, screenW, screenH, screenX, screenY, interactiveOnly);
                return;
            }

            var bounds = child.BoundingRectangle;
            if (bounds.IsEmpty) return;

            var w = (int)bounds.Width;
            var h = (int)bounds.Height;
            var x = (int)bounds.X;
            var y = (int)bounds.Y;

            if (w < ConfigManager.Current.MinWidth || h < ConfigManager.Current.MinHeight)
            {
                EnumerateControls(child, level + 1, controls, screenW, screenH, screenX, screenY, interactiveOnly);
                return;
            }

            var screenRight = screenX + screenW;
            var screenBottom = screenY + screenH;
            if (x + w <= screenX || y + h <= screenY || x >= screenRight || y >= screenBottom)
            {
                EnumerateControls(child, level + 1, controls, screenW, screenH, screenX, screenY, interactiveOnly);
                return;
            }

            var typeInfo = ControlTypeInfo.AllTypes.GetValueOrDefault(ctrlTypeNum, ControlTypeInfo.AllTypes[0]);
            var interactiveTypeInfo = ControlTypeInfo.InteractiveTypes.GetValueOrDefault(ctrlTypeNum);

            var controlText = GetControlText(child);

            var info = new ControlInfo
            {
                Name = controlText,
                Type = typeInfo.Name,
                TypeNum = ctrlTypeNum,
                TypeDesc = typeInfo.Description,
                ActionHint = interactiveTypeInfo?.ActionHint ?? "",
                IsInteractive = ControlTypeInfo.InteractiveTypeNums.Contains(ctrlTypeNum),
                X = x,
                Y = y,
                Width = w,
                Height = h,
                Hwnd = child.Properties.NativeWindowHandle.ValueOrDefault.ToInt64()
            };

            try { info.AutomationId = child.AutomationId ?? ""; } catch { }
            try { info.ClassName = child.ClassName ?? ""; } catch { }
            try { info.HelpText = child.HelpText ?? ""; } catch { }
            try { info.IsEnabled = child.IsEnabled; } catch { }
            try { info.IsOffscreen = child.IsOffscreen; } catch { }

            lock (_lockObj)
            {
                controls.Add(info);
            }
            
            EnumerateControls(child, level + 1, controls, screenW, screenH, screenX, screenY, interactiveOnly);
        }
        catch { }
    }

    private static string GetControlText(AutomationElement element)
    {
        try
        {
            var name = element.Name;
            if (!string.IsNullOrEmpty(name))
                return name;
        }
        catch { }

        try
        {
            var legacyName = element.Patterns.LegacyIAccessible.Pattern.Name.ValueOrDefault;
            if (!string.IsNullOrEmpty(legacyName))
                return legacyName;
        }
        catch { }

        try
        {
            var value = element.Patterns.Value.Pattern.Value.ValueOrDefault;
            if (!string.IsNullOrEmpty(value))
                return value;
        }
        catch { }

        try
        {
            var text = element.Patterns.Text.Pattern.DocumentRange.GetText(-1);
            if (!string.IsNullOrEmpty(text))
                return text;
        }
        catch { }

        try
        {
            var autoId = element.AutomationId;
            if (!string.IsNullOrEmpty(autoId))
                return autoId;
        }
        catch { }

        return "";
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

    public ScanResultLite BuildScanResultLite(List<ControlInfo> controls, Helpers.WindowInfo windowInfo)
    {
        var controlStrings = controls.Select(c =>
        {
            var name = string.IsNullOrEmpty(c.Name) ? c.TypeDesc : c.Name;
            return $"{c.Label}: {name} ({c.Type}) [{c.CenterX}, {c.CenterY}]";
        }).ToList();

        return new ScanResultLite
        {
            Success = true,
            Timestamp = DateTime.Now.ToString("O"),
            Window = new WindowInfoLite
            {
                Handle = windowInfo.Hwnd.ToInt64(),
                Title = windowInfo.Title,
                ClassName = windowInfo.ClassName
            },
            Summary = new ScanSummaryLite
            {
                TotalControls = controls.Count,
                Description = $"窗口「{windowInfo.Title}」共有 {controls.Count} 个可交互控件"
            },
            Controls = controlStrings
        };
    }

    public void Dispose()
    {
        _automation?.Dispose();
        _automation = null;
    }
}
