using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows;

namespace Vimina.Core.Input;

public class GlobalHotkeyManager : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;

    private readonly IntPtr _hwnd;
    private readonly HwndSource _source;
    private int _hotkeyId = 9000;
    private readonly Dictionary<int, Action> _actions = new();

    public GlobalHotkeyManager(System.Windows.Window window)
    {
        var helper = new WindowInteropHelper(window);
        _hwnd = helper.Handle;
        _source = HwndSource.FromHwnd(_hwnd);
        _source.AddHook(WndProc);
    }

    public bool RegisterAltF(Action action)
    {
        var id = _hotkeyId++;
        _actions[id] = action;
        return RegisterHotKey(_hwnd, id, MOD_ALT, (uint)'F');
    }

    public bool RegisterAltR(Action action)
    {
        var id = _hotkeyId++;
        _actions[id] = action;
        return RegisterHotKey(_hwnd, id, MOD_ALT, (uint)'R');
    }

    public bool RegisterEsc(Action action)
    {
        var id = _hotkeyId++;
        _actions[id] = action;
        return RegisterHotKey(_hwnd, id, 0, 0x1B);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_HOTKEY = 0x0312;
        if (msg == WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            if (_actions.TryGetValue(id, out var action))
            {
                action();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        foreach (var id in _actions.Keys)
            UnregisterHotKey(_hwnd, id);
        _source.RemoveHook(WndProc);
    }
}
