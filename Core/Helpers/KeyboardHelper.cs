using System.Runtime.InteropServices;
using System.Windows.Input;

namespace Vimina.Core.Helpers;

public static class KeyboardHelper
{
    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    public static extern short VkKeyScan(char ch);

    public const uint KEYEVENTF_KEYUP = 0x0002;
    public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

    public static void SendText(string text)
    {
        foreach (var ch in text)
        {
            var vk = VkKeyScan(ch);
            if (vk == -1)
            {
                SendUnicode(ch);
                continue;
            }
            var keyCode = (byte)(vk & 0xFF);
            var shift = (vk & 0x100) != 0;

            if (shift)
                keybd_event(0x10, 0, 0, UIntPtr.Zero);

            keybd_event(keyCode, 0, 0, UIntPtr.Zero);
            keybd_event(keyCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            if (shift)
                keybd_event(0x10, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            Thread.Sleep(10);
        }
    }

    public static void KeyPress(string key)
    {
        var parts = key.Split('+', StringSplitOptions.TrimEntries);
        var mods = new List<byte>();
        byte? mainKey = null;

        foreach (var part in parts)
        {
            var upper = part.ToUpperInvariant();
            switch (upper)
            {
                case "CTRL": mods.Add(0x11); break;
                case "ALT": mods.Add(0x12); break;
                case "SHIFT": mods.Add(0x10); break;
                case "WIN": mods.Add(0x5B); break;
                default:
                    mainKey = ParseKey(upper);
                    break;
            }
        }

        foreach (var m in mods)
            keybd_event(m, 0, 0, UIntPtr.Zero);

        if (mainKey.HasValue)
        {
            keybd_event(mainKey.Value, 0, 0, UIntPtr.Zero);
            keybd_event(mainKey.Value, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        foreach (var m in mods.AsEnumerable().Reverse())
            keybd_event(m, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void KeyDown(string key)
    {
        var vk = ParseKey(key.ToUpperInvariant());
        if (vk.HasValue)
            keybd_event(vk.Value, 0, 0, UIntPtr.Zero);
    }

    public static void KeyUp(string key)
    {
        var vk = ParseKey(key.ToUpperInvariant());
        if (vk.HasValue)
            keybd_event(vk.Value, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private static byte? ParseKey(string key)
    {
        return key switch
        {
            "A" => 0x41, "B" => 0x42, "C" => 0x43, "D" => 0x44,
            "E" => 0x45, "F" => 0x46, "G" => 0x47, "H" => 0x48,
            "I" => 0x49, "J" => 0x4A, "K" => 0x4B, "L" => 0x4C,
            "M" => 0x4D, "N" => 0x4E, "O" => 0x4F, "P" => 0x50,
            "Q" => 0x51, "R" => 0x52, "S" => 0x53, "T" => 0x54,
            "U" => 0x55, "V" => 0x56, "W" => 0x57, "X" => 0x58,
            "Y" => 0x59, "Z" => 0x5A,
            "0" => 0x30, "1" => 0x31, "2" => 0x32, "3" => 0x33,
            "4" => 0x34, "5" => 0x35, "6" => 0x36, "7" => 0x37,
            "8" => 0x38, "9" => 0x39,
            "ENTER" or "RETURN" => 0x0D,
            "ESC" or "ESCAPE" => 0x1B,
            "TAB" => 0x09,
            "SPACE" => 0x20,
            "BACK" or "BACKSPACE" => 0x08,
            "DELETE" or "DEL" => 0x2E,
            "HOME" => 0x24,
            "END" => 0x23,
            "LEFT" => 0x25,
            "UP" => 0x26,
            "RIGHT" => 0x27,
            "DOWN" => 0x28,
            "F1" => 0x70, "F2" => 0x71, "F3" => 0x72, "F4" => 0x73,
            "F5" => 0x74, "F6" => 0x75, "F7" => 0x76, "F8" => 0x77,
            "F9" => 0x78, "F10" => 0x79, "F11" => 0x7A, "F12" => 0x7B,
            _ => null
        };
    }

    private static void SendUnicode(char ch)
    {
    }
}
