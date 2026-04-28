using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Vimina.Core.Config;
using Vimina.Core.Helpers;

namespace Vimina.Core.Automation;

public class ClickEngine : IDisposable
{
    private UIA3Automation? _automation;

    public ClickResult ClickAt(int x, int y, bool rightClick = false, bool doubleClick = false,
        bool middleClick = false, IntPtr? targetHwnd = null, bool? useFlaUI = null, bool? bringToFront = null)
    {
        var config = ConfigManager.Current;
        var useFlaUIClick = useFlaUI ?? config.UseFlaUIClick;
        var bringFront = bringToFront ?? config.BringToFront;

        if (targetHwnd.HasValue && bringFront)
        {
            WindowHelper.ForceForegroundWindow(targetHwnd.Value);
            Thread.Sleep(200);
        }

        if (useFlaUIClick && !middleClick)
        {
            var flaResult = TryFlaUIClick(x, y, rightClick, doubleClick, targetHwnd);
            if (flaResult.Success)
                return flaResult;
        }

        return PerformMouseClick(x, y, rightClick, doubleClick, middleClick);
    }

    private ClickResult TryFlaUIClick(int x, int y, bool rightClick, bool doubleClick, IntPtr? targetHwnd)
    {
        try
        {
            _automation?.Dispose();
            _automation = new UIA3Automation();

            AutomationElement? element = null;

            if (targetHwnd.HasValue)
            {
                var window = _automation.FromHandle(targetHwnd.Value);
                if (window != null)
                {
                    element = FindElementAt(window, x, y);
                }
            }

            element ??= _automation.FromPoint(new System.Drawing.Point(x, y));

            if (element == null)
                return new ClickResult { Success = false, Error = "FlaUI: 未找到元素" };

            var success = TryInvokeClick(element, rightClick, doubleClick);

            if (!success)
            {
                var nativeHandle = element.Properties.NativeWindowHandle.ValueOrDefault;
                if (nativeHandle != IntPtr.Zero)
                {
                    WindowHelper.ForceForegroundWindow(nativeHandle);
                    success = true;
                }
            }

            if (success)
                return new ClickResult { Success = true, Message = "FlaUI 点击成功", X = x, Y = y };

            return new ClickResult { Success = false, Error = "FlaUI: 所有点击方式失败" };
        }
        catch (Exception ex)
        {
            return new ClickResult { Success = false, Error = $"FlaUI 异常: {ex.Message}" };
        }
    }

    private static AutomationElement? FindElementAt(AutomationElement parent, int x, int y)
    {
        try
        {
            var children = parent.FindAllChildren();
            foreach (var child in children)
            {
                var bounds = child.BoundingRectangle;
                if (bounds.Contains(x, y))
                {
                    var deeper = FindElementAt(child, x, y);
                    return deeper ?? child;
                }
            }
        }
        catch { }
        return null;
    }

    private static bool TryInvokeClick(AutomationElement element, bool rightClick, bool doubleClick)
    {
        try
        {
            var btn = element.AsButton();
            if (btn != null) { btn.Invoke(); return true; }
        }
        catch { }

        try
        {
            var chk = element.AsCheckBox();
            if (chk != null) { chk.Toggle(); return true; }
        }
        catch { }

        try
        {
            var radio = element.AsRadioButton();
            if (radio != null) { radio.Patterns.SelectionItem.Pattern.Select(); return true; }
        }
        catch { }

        try
        {
            var tree = element.AsTreeItem();
            if (tree != null)
            {
                try { tree.Expand(); return true; } catch { }
            }
        }
        catch { }

        try
        {
            var listItem = element.AsListBoxItem();
            if (listItem != null) { listItem.Select(); return true; }
        }
        catch { }

        try
        {
            var tab = element.AsTabItem();
            if (tab != null) { tab.Select(); return true; }
        }
        catch { }

        try
        {
            var menu = element.AsMenuItem();
            if (menu != null) { menu.Invoke(); return true; }
        }
        catch { }

        try
        {
            element.Patterns.Invoke.Pattern.Invoke();
            return true;
        }
        catch { }

        return false;
    }

    private static ClickResult PerformMouseClick(int x, int y, bool rightClick, bool doubleClick, bool middleClick = false)
    {
        var (origX, origY) = MouseHelper.GetPosition();

        if (middleClick)
        {
            MouseHelper.ClickMiddle(x, y);
        }
        else if (doubleClick)
        {
            if (rightClick)
                MouseHelper.ClickRight(x, y);
            else
                MouseHelper.DoubleClick(x, y);
        }
        else
        {
            if (rightClick)
                MouseHelper.ClickRight(x, y);
            else
                MouseHelper.ClickLeft(x, y);
        }

        Thread.Sleep(ConfigManager.Current.ClickDelay);
        MouseHelper.MoveTo(origX, origY);

        var message = middleClick ? "中键点击" : (rightClick ? "右键点击" : (doubleClick ? "双击" : "左键点击"));
        return new ClickResult
        {
            Success = true,
            Message = message,
            X = x,
            Y = y
        };
    }

    public void Dispose()
    {
        _automation?.Dispose();
        _automation = null;
    }
}

public class ClickResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Message { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}
