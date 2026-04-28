using System.Windows;
using Vimina.Core.Config;

namespace Vimina.Views;

public partial class ConfigWindow : Window
{
    public ConfigWindow()
    {
        InitializeComponent();
        LoadConfig();
    }

    private void LoadConfig()
    {
        var c = ConfigManager.Current;
        ChkMouseClick.IsChecked = c.UseMouseClick;
        ChkBringFront.IsChecked = c.BringToFront;
        ChkFlaUI.IsChecked = c.UseFlaUIClick;
        TxtClickDelay.Text = c.ClickDelay.ToString();
        TxtMinWidth.Text = c.MinWidth.ToString();
        TxtMinHeight.Text = c.MinHeight.ToString();
        TxtMaxDepth.Text = c.MaxDepth.ToString();
        TxtColorDefault.Text = c.BackgroundColor_Default;
        TxtColorMatch.Text = c.BackgroundColor_Match;
        TxtColorPrefix.Text = c.BackgroundColor_Prefix;
        TxtColorInvalid.Text = c.BackgroundColor_Invalid;
        TxtColorText.Text = c.TextColor;
        TxtFontSize.Text = c.FontSize.ToString();
        TxtFontWeight.Text = c.FontWeight.ToString();
        TxtOffsetX.Text = c.OffsetX.ToString();
        TxtOffsetY.Text = c.OffsetY.ToString();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var c = ConfigManager.Current;
        c.UseMouseClick = ChkMouseClick.IsChecked == true;
        c.BringToFront = ChkBringFront.IsChecked == true;
        c.UseFlaUIClick = ChkFlaUI.IsChecked == true;
        if (int.TryParse(TxtClickDelay.Text, out var clickDelay)) c.ClickDelay = clickDelay;
        if (int.TryParse(TxtMinWidth.Text, out var minW)) c.MinWidth = minW;
        if (int.TryParse(TxtMinHeight.Text, out var minH)) c.MinHeight = minH;
        if (int.TryParse(TxtMaxDepth.Text, out var maxD)) c.MaxDepth = maxD;
        c.BackgroundColor_Default = TxtColorDefault.Text;
        c.BackgroundColor_Match = TxtColorMatch.Text;
        c.BackgroundColor_Prefix = TxtColorPrefix.Text;
        c.BackgroundColor_Invalid = TxtColorInvalid.Text;
        c.TextColor = TxtColorText.Text;
        if (int.TryParse(TxtFontSize.Text, out var fs)) c.FontSize = fs;
        if (int.TryParse(TxtFontWeight.Text, out var fw)) c.FontWeight = fw;
        if (int.TryParse(TxtOffsetX.Text, out var ox)) c.OffsetX = ox;
        if (int.TryParse(TxtOffsetY.Text, out var oy)) c.OffsetY = oy;
        ConfigManager.Save();
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
        {
            DragMove();
        }
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void BtnMaximize_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
        }
        else
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
