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

    public ControlInfo ScanAllControlsToTree(IntPtr hwnd)
    {
        _automation?.Dispose();
        _automation = new UIA3Automation();

        var window = _automation.FromHandle(hwnd);
        if (window == null) return new ControlInfo();

        var screenW = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        var screenH = GetSystemMetrics(SM_CYVIRTUALSCREEN);
        var virtualScreenX = GetSystemMetrics(SM_XVIRTUALSCREEN);
        var virtualScreenY = GetSystemMetrics(SM_YVIRTUALSCREEN);

        var rootInfo = CreateWindowRootInfo(window, screenW, screenH, virtualScreenX, virtualScreenY);
        var children = EnumerateControlsToTree(window, 1, screenW, screenH, virtualScreenX, virtualScreenY);
        
        DeduplicateTree(children);
        AssignLabels(children);
        
        rootInfo.Children = children;
        return rootInfo;
    }

    private ControlInfo CreateWindowRootInfo(AutomationElement window, int screenW, int screenH, int screenX, int screenY)
    {
        var bounds = window.BoundingRectangle;
        var typeInfo = ControlTypeInfo.AllTypes.GetValueOrDefault((int)window.ControlType, ControlTypeInfo.AllTypes[0]);
        
        return new ControlInfo
        {
            Label = "Root",
            Name = window.Name ?? "",
            Type = typeInfo.Name,
            TypeNum = (int)window.ControlType,
            TypeDesc = typeInfo.Description,
            ActionHint = "",
            IsInteractive = false,
            X = (int)bounds.X,
            Y = (int)bounds.Y,
            Width = (int)bounds.Width,
            Height = (int)bounds.Height,
            Hwnd = window.Properties.NativeWindowHandle.ValueOrDefault.ToInt64()
        };
    }

    private List<ControlInfo> EnumerateControlsToTree(AutomationElement element, int level, int screenW, int screenH, int screenX, int screenY)
    {
        var result = new List<ControlInfo>();
        
        if (level > ConfigManager.Current.MaxDepth) return result;

        try
        {
            var children = element.FindAllChildren();
            foreach (var child in children)
            {
                try
                {
                    var ctrlTypeNum = (int)child.ControlType;
                    var bounds = child.BoundingRectangle;
                    
                    if (bounds.IsEmpty)
                    {
                        var subChildren = EnumerateControlsToTree(child, level + 1, screenW, screenH, screenX, screenY);
                        result.AddRange(subChildren);
                        continue;
                    }

                    var w = (int)bounds.Width;
                    var h = (int)bounds.Height;
                    var x = (int)bounds.X;
                    var y = (int)bounds.Y;

                    if (w < ConfigManager.Current.MinWidth || h < ConfigManager.Current.MinHeight)
                    {
                        var subChildren = EnumerateControlsToTree(child, level + 1, screenW, screenH, screenX, screenY);
                        result.AddRange(subChildren);
                        continue;
                    }

                    var screenRight = screenX + screenW;
                    var screenBottom = screenY + screenH;
                    if (x + w <= screenX || y + h <= screenY || x >= screenRight || y >= screenBottom)
                    {
                        var subChildren = EnumerateControlsToTree(child, level + 1, screenW, screenH, screenX, screenY);
                        result.AddRange(subChildren);
                        continue;
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

                    var nestedChildren = EnumerateControlsToTree(child, level + 1, screenW, screenH, screenX, screenY);
                    if (nestedChildren.Count > 0)
                    {
                        info.Children = nestedChildren;
                    }

                    result.Add(info);
                }
                catch { }
            }
        }
        catch { }

        return result;
    }

    private void DeduplicateTree(List<ControlInfo> controls)
    {
        var seen = new HashSet<string>();
        var toRemove = new List<int>();

        for (int i = controls.Count - 1; i >= 0; i--)
        {
            var ctrl = controls[i];
            var key = $"{ctrl.CenterX}_{ctrl.CenterY}";
            
            if (seen.Contains(key))
            {
                toRemove.Add(i);
            }
            else
            {
                seen.Add(key);
                if (ctrl.Children != null && ctrl.Children.Count > 0)
                {
                    DeduplicateTree(ctrl.Children);
                }
            }
        }

        foreach (var idx in toRemove)
        {
            controls.RemoveAt(idx);
        }
    }

    private void AssignLabels(List<ControlInfo> controls)
    {
        LabelGenerator.Reset();
        AssignLabelsRecursive(controls);
    }

    private void AssignLabelsRecursive(List<ControlInfo> controls)
    {
        foreach (var ctrl in controls)
        {
            ctrl.Label = LabelGenerator.Next();
            if (ctrl.Children != null && ctrl.Children.Count > 0)
            {
                AssignLabelsRecursive(ctrl.Children);
            }
        }
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

    public ScanResultTree BuildScanResultTree(ControlInfo controlTree, Helpers.WindowInfo windowInfo)
    {
        return new ScanResultTree
        {
            ControlTree = ConvertToTreeNode(controlTree)
        };
    }

    private ControlTreeNode ConvertToTreeNode(ControlInfo info)
    {
        var node = new ControlTreeNode
        {
            Name = string.IsNullOrEmpty(info.Name) ? info.TypeDesc : info.Name,
            X = info.X,
            Y = info.Y
        };

        if (info.Children != null && info.Children.Count > 0)
        {
            node.Children = info.Children.Select(c => ConvertToTreeNode(c)).ToList();
        }

        return node;
    }

    private int CountControlsInTree(ControlInfo? root)
    {
        if (root == null) return 0;
        
        var count = 1;
        if (root.Children != null)
        {
            foreach (var child in root.Children)
            {
                count += CountControlsInTree(child);
            }
        }
        return count;
    }

    private List<string> FlattenTreeToStrings(ControlInfo root)
    {
        var result = new List<string>();
        FlattenTreeToStringsRecursive(root, result);
        return result;
    }

    private void FlattenTreeToStringsRecursive(ControlInfo info, List<string> result)
    {
        if (info.Label != "Root")
        {
            var name = string.IsNullOrEmpty(info.Name) ? info.TypeDesc : info.Name;
            result.Add($"{info.Label}: {name} ({info.Type}) [{info.CenterX}, {info.CenterY}]");
        }

        if (info.Children != null)
        {
            foreach (var child in info.Children)
            {
                FlattenTreeToStringsRecursive(child, result);
            }
        }
    }

    public void Dispose()
    {
        _automation?.Dispose();
        _automation = null;
    }
}
