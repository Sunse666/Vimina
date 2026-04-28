using System.Runtime.InteropServices;
using System.Text;

namespace Vimina.Core.Helpers;

public static class WindowHelper
{
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool AllowSetForegroundWindow(uint dwProcessId);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    public static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    public static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    public static readonly IntPtr HWND_TOP = new IntPtr(0);
    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_SHOWWINDOW = 0x0040;
    public const uint SWP_FRAMECHANGED = 0x0020;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool CloseWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public const int SW_RESTORE = 9;
    public const int SW_MINIMIZE = 6;
    public const int SW_MAXIMIZE = 3;
    public const int SW_SHOW = 5;

    public static string GetWindowTitle(IntPtr hwnd)
    {
        var sb = new StringBuilder(512);
        GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    public static string GetWindowClass(IntPtr hwnd)
    {
        var sb = new StringBuilder(256);
        GetClassName(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    public static List<WindowInfo> GetAllWindows()
    {
        var list = new List<WindowInfo>();
        var systemClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Shell_TrayWnd", "Progman", "WorkerW",
            "Windows.UI.Core.CoreWindow", "ApplicationFrameWindow"
        };

        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd)) return true;
            var title = GetWindowTitle(hwnd);
            if (string.IsNullOrEmpty(title)) return true;
            var className = GetWindowClass(hwnd);
            if (systemClasses.Contains(className)) return true;

            GetWindowThreadProcessId(hwnd, out var pid);
            list.Add(new WindowInfo(hwnd, title, className, pid));
            return true;
        }, IntPtr.Zero);

        return list;
    }

    public static IntPtr FindWindowByTitle(string title)
    {
        IntPtr result = IntPtr.Zero;
        var searchTitle = title.Trim();
        
        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd)) return true;
            
            var wt = GetWindowTitle(hwnd);
            if (string.IsNullOrEmpty(wt)) return true;
            
            if (wt.Equals(searchTitle, StringComparison.OrdinalIgnoreCase))
            {
                result = hwnd;
                return false;
            }
            
            if (wt.Contains(searchTitle, StringComparison.OrdinalIgnoreCase))
            {
                result = hwnd;
                return false;
            }
            
            return true;
        }, IntPtr.Zero);
        
        return result;
    }
    
    public static List<WindowInfo> FindWindowsByTitle(string title)
    {
        var list = new List<WindowInfo>();
        var searchTitle = title.Trim();
        
        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd)) return true;
            
            var wt = GetWindowTitle(hwnd);
            if (string.IsNullOrEmpty(wt)) return true;
            
            if (wt.Equals(searchTitle, StringComparison.OrdinalIgnoreCase) ||
                wt.Contains(searchTitle, StringComparison.OrdinalIgnoreCase))
            {
                GetWindowThreadProcessId(hwnd, out var pid);
                list.Add(new WindowInfo(hwnd, wt, GetWindowClass(hwnd), pid));
            }
            
            return true;
        }, IntPtr.Zero);
        
        return list;
    }

    public static bool ForceForegroundWindow(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return false;
        if (!IsWindowVisible(hWnd)) return false;

        if (IsIconic(hWnd))
        {
            ShowWindow(hWnd, SW_RESTORE);
        }

        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == hWnd)
        {
            return true;
        }

        uint foregroundThreadId = GetWindowThreadProcessId(foregroundWindow, out _);
        uint targetThreadId = GetWindowThreadProcessId(hWnd, out _);
        uint currentThreadId = GetCurrentThreadId();

        if (foregroundThreadId != currentThreadId)
        {
            AttachThreadInput(foregroundThreadId, currentThreadId, true);
        }

        bool result = false;
        
        result = SetForegroundWindow(hWnd);
        
        if (!result)
        {
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            result = SetForegroundWindow(hWnd);
        }

        if (foregroundThreadId != currentThreadId)
        {
            AttachThreadInput(foregroundThreadId, currentThreadId, false);
        }

        if (!result)
        {
            result = SetForegroundWindow(hWnd);
        }

        return result;
    }
}

public record WindowInfo(IntPtr Hwnd, string Title, string ClassName, uint ProcessId);

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left, Top, Right, Bottom;
}
