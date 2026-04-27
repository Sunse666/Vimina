using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Vimina.Core.Config;

namespace Vimina.Controls;

public partial class MarkerWindow : Window
{
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_SHOWWINDOW = 0x0040;

    public string Label { get; private set; } = "";

    public MarkerWindow(string label, int x, int y)
    {
        InitializeComponent();
        Label = label;
        LabelText.Text = label;
        // Don't set Left/Top here, we'll use SetWindowPos after the window is shown
        UpdateColor("default");

        // Store the desired position
        _desiredX = x;
        _desiredY = y;

        // Hook into the window loaded event to set position using Win32 API
        Loaded += OnWindowLoaded;
    }

    private int _desiredX;
    private int _desiredY;

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        // Get the window handle
        var hwnd = new WindowInteropHelper(this).Handle;

        // Use SetWindowPos to set exact screen coordinates (ignoring DPI scaling)
        SetWindowPos(hwnd, HWND_TOPMOST, _desiredX, _desiredY, 0, 0,
            SWP_NOSIZE | SWP_SHOWWINDOW);
    }

    public void UpdateColor(string state)
    {
        var brush = state.ToLower() switch
        {
            "match" => ParseColor(ConfigManager.Current.BackgroundColor_Match),
            "prefix" => ParseColor(ConfigManager.Current.BackgroundColor_Prefix),
            "invalid" => ParseColor(ConfigManager.Current.BackgroundColor_Invalid),
            _ => ParseColor(ConfigManager.Current.BackgroundColor_Default),
        };
        MarkerBorder.Background = brush;
    }

    private static SolidColorBrush ParseColor(string hex)
    {
        try
        {
            var clean = hex.Replace("0x", "").Replace("0X", "");
            var num = int.Parse(clean, System.Globalization.NumberStyles.HexNumber);
            // 0xRRGGBB format: R is high byte, B is low byte
            var r = (num >> 16) & 0xFF;
            var g = (num >> 8) & 0xFF;
            var b = num & 0xFF;
            return new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)r, (byte)g, (byte)b));
        }
        catch
        {
            return System.Windows.Media.Brushes.Cyan;
        }
    }
}
