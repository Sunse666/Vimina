using System.Runtime.InteropServices;

namespace Vimina.Core.Input;

public class KeyboardHook : IDisposable
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;

    private IntPtr _hookId = IntPtr.Zero;
    private readonly LowLevelKeyboardProc _proc;

    public event EventHandler<KeyboardHookEventArgs>? KeyPressed;
    public event EventHandler<KeyboardHookEventArgs>? KeyIntercepted;

    public bool IsActive { get; set; } = true;

    public KeyboardHook()
    {
        _proc = HookCallback;
        _hookId = SetHook(_proc);
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule?.ModuleName ?? ""), 0);
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var vkCode = Marshal.ReadInt32(lParam);
            var isKeyDown = wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN;

            var args = new KeyboardHookEventArgs(vkCode, isKeyDown);
            KeyPressed?.Invoke(this, args);

            if (IsActive && args.Handled)
            {
                KeyIntercepted?.Invoke(this, args);
                return (IntPtr)1;
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }
}

public class KeyboardHookEventArgs : EventArgs
{
    public int VkCode { get; }
    public bool IsKeyDown { get; }
    public bool Handled { get; set; }
    public char Char => VkCode is >= 0x41 and <= 0x5A ? (char)VkCode : '\0';
    public bool IsLetter => VkCode is >= 0x41 and <= 0x5A;
    public bool IsBackspace => VkCode == 0x08;
    public bool IsEscape => VkCode == 0x1B;

    public KeyboardHookEventArgs(int vkCode, bool isKeyDown)
    {
        VkCode = vkCode;
        IsKeyDown = isKeyDown;
    }
}
