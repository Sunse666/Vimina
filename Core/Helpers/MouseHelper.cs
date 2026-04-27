using System.Runtime.InteropServices;

namespace Vimina.Core.Helpers;

public static class MouseHelper
{
    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    public const uint MOUSEEVENTF_MOVE = 0x0001;
    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;
    public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    public const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    public const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    public const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    public const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

    public enum MouseButton
    {
        Left,
        Right,
        Middle
    }

    public static (int x, int y) GetPosition()
    {
        GetCursorPos(out var pt);
        return (pt.X, pt.Y);
    }

    public static void MoveTo(int x, int y)
    {
        SetCursorPos(x, y);
    }

    public static void ClickLeft(int x, int y)
    {
        MoveTo(x, y);
        Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
    }

    public static void ClickRight(int x, int y)
    {
        MoveTo(x, y);
        Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
    }

    public static void DoubleClick(int x, int y)
    {
        ClickLeft(x, y);
        Thread.Sleep(100);
        ClickLeft(x, y);
    }

    public static void Drag(int x1, int y1, int x2, int y2)
    {
        MoveTo(x1, y1);
        Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(50);
        MoveTo(x2, y2);
        Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
    }

    public static void ClickMiddle(int x, int y)
    {
        MoveTo(x, y);
        Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, UIntPtr.Zero);
    }

    public static void MouseDown(MouseButton button)
    {
        var flag = button switch
        {
            MouseButton.Left => MOUSEEVENTF_LEFTDOWN,
            MouseButton.Right => MOUSEEVENTF_RIGHTDOWN,
            MouseButton.Middle => MOUSEEVENTF_MIDDLEDOWN,
            _ => MOUSEEVENTF_LEFTDOWN
        };
        mouse_event(flag, 0, 0, 0, UIntPtr.Zero);
    }

    public static void MouseUp(MouseButton button)
    {
        var flag = button switch
        {
            MouseButton.Left => MOUSEEVENTF_LEFTUP,
            MouseButton.Right => MOUSEEVENTF_RIGHTUP,
            MouseButton.Middle => MOUSEEVENTF_MIDDLEUP,
            _ => MOUSEEVENTF_LEFTUP
        };
        mouse_event(flag, 0, 0, 0, UIntPtr.Zero);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int X, Y;
}
