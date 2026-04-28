using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using Vimina.Core.Automation;
using Vimina.Core.Config;
using Vimina.Core.Helpers;
using Vimina.Core.Input;
using Vimina.Controls;

namespace Vimina;

public partial class MainWindow : Window
{
    private TaskbarIcon? _trayIcon;
    private GlobalHotkeyManager? _hotkeys;
    private KeyboardHook? _keyboardHook;
    private readonly InputBuffer _inputBuffer = new();
    private readonly List<MarkerWindow> _markers = new();
    private readonly Dictionary<string, ControlInfo> _controlMap = new();
    private readonly ControlScanner _scanner = new();
    private readonly ClickEngine _clickEngine = new();

    private bool _isMarkerVisible;
    private IntPtr _selfHwnd;

    public MainWindow()
    {
        InitializeComponent();
        ConfigManager.Load();
        SetupTrayIcon();
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _selfHwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        SetupHotkeys();
        SetupKeyboardHook();
        StartApiServer();
    }

    private void SetupTrayIcon()
    {
        System.Drawing.Icon? appIcon = null;
        
        var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.ico");
        if (System.IO.File.Exists(iconPath))
        {
            try
            {
                appIcon = new System.Drawing.Icon(iconPath);
            }
            catch { }
        }
        
        if (appIcon == null)
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("logo.ico");
                if (stream != null)
                {
                    appIcon = new System.Drawing.Icon(stream);
                }
            }
            catch { }
        }
        
        _trayIcon = new TaskbarIcon
        {
            Icon = appIcon ?? System.Drawing.SystemIcons.Application,
            ToolTipText = "Vimina",
            Visibility = Visibility.Visible
        };

        var menu = new ContextMenu();
        menu.Items.Add(CreateMenuItem("打开", (_, _) => ShowWindow()));
        menu.Items.Add(CreateMenuItem("配置", (_, _) => OpenConfig()));
        menu.Items.Add(CreateMenuItem("脚本", (_, _) => OpenScriptEditor()));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem("数据目录", (_, _) => OpenDataDir()));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem("关闭", (_, _) => Close()));

        _trayIcon.ContextMenu = menu;
        _trayIcon.TrayLeftMouseDown += (_, _) => ShowWindow();
    }

    private static MenuItem CreateMenuItem(string header, RoutedEventHandler click)
    {
        var item = new MenuItem { Header = header };
        item.Click += click;
        return item;
    }

    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void SetupHotkeys()
    {
        _hotkeys = new GlobalHotkeyManager(this);
        _hotkeys.RegisterAltF(ToggleMarkers);
        _hotkeys.RegisterAltR(() => { ClearMarkers(); ToggleMarkers(); });
    }

    private void SetupKeyboardHook()
    {
        _keyboardHook = new KeyboardHook();
        _keyboardHook.KeyPressed += OnKeyPressed;

        _inputBuffer.BufferChanged += (_, buffer) =>
        {
            HighlightMarkers(buffer);
        };

        _inputBuffer.BufferConfirmed += (_, label) =>
        {
            if (_controlMap.TryGetValue(label.ToUpperInvariant(), out var ctrl))
            {
                ClearMarkers();
                PerformClick(ctrl);
            }
        };
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        if (!_isMarkerVisible) return;

        if (!e.IsKeyDown) return;

        if (e.IsLetter)
        {
            e.Handled = true;
            var c = e.Char;
            _inputBuffer.Append(c);

            var buffer = _inputBuffer.Buffer;
            if (_controlMap.TryGetValue(buffer, out var ctrl))
            {
                _inputBuffer.Confirm();
                ClearMarkers();
                PerformClick(ctrl);
                return;
            }

            var hasPrefix = _controlMap.Keys.Any(k => k.StartsWith(buffer, StringComparison.OrdinalIgnoreCase));
            if (!hasPrefix)
            {
                _inputBuffer.Clear();
                ClearMarkers();
            }
        }
        else if (e.IsBackspace)
        {
            e.Handled = true;
            _inputBuffer.Backspace();
        }
        else if (e.IsEscape)
        {
            e.Handled = true;
            ClearMarkers();
        }
    }

    private void ToggleMarkers()
    {
        if (_isMarkerVisible)
        {
            ClearMarkers();
            return;
        }

        var foreHwnd = WindowHelper.GetForegroundWindow();
        if (foreHwnd == IntPtr.Zero || foreHwnd == _selfHwnd)
        {
            System.Windows.MessageBox.Show("请先点击要操作的窗口，使其成为前台窗口", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ClearMarkers();
        var controls = _scanner.ScanInteractiveControls(foreHwnd);
        if (controls.Count == 0)
        {
            System.Windows.MessageBox.Show("未找到可交互控件", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        LabelGenerator.Reset();
        var labelMap = new LabelMap();
        var windowInfo = new Core.Helpers.WindowInfo(foreHwnd,
            WindowHelper.GetWindowTitle(foreHwnd),
            WindowHelper.GetWindowClass(foreHwnd), 0);

        foreach (var ctrl in controls)
        {
            ctrl.Label = LabelGenerator.Next();
            labelMap[ctrl.Label] = new LabelPosition { CenterX = ctrl.CenterX, CenterY = ctrl.CenterY };
            _controlMap[ctrl.Label] = ctrl;

            var markerX = ctrl.X + ConfigManager.Current.OffsetX;
            var markerY = ctrl.Y + ConfigManager.Current.OffsetY;
            if (markerX < 0) markerX = 0;
            if (markerY < 0) markerY = ctrl.Y + ctrl.Height + 2;

            var marker = new MarkerWindow(ctrl.Label, markerX, markerY);
            marker.Show();
            _markers.Add(marker);
        }

        _isMarkerVisible = true;

        var result = _scanner.BuildScanResult(controls, windowInfo);
        JsonFileHelper.Save(ConfigManager.ScanResultPath, result);
        JsonFileHelper.Save(ConfigManager.LabelMapPath, labelMap);
    }

    private void HighlightMarkers(string input)
    {
        input = input.ToUpperInvariant();
        foreach (var marker in _markers)
        {
            if (string.IsNullOrEmpty(input))
            {
                marker.UpdateColor("default");
            }
            else if (marker.Label == input)
            {
                marker.UpdateColor("match");
            }
            else if (marker.Label.StartsWith(input))
            {
                marker.UpdateColor("prefix");
            }
            else
            {
                marker.UpdateColor("invalid");
            }
        }
    }

    private void ClearMarkers()
    {
        foreach (var marker in _markers)
            marker.Close();
        _markers.Clear();
        _controlMap.Clear();
        _inputBuffer.Clear();
        _isMarkerVisible = false;
    }

    private void PerformClick(ControlInfo ctrl)
    {
        _clickEngine.ClickAt(ctrl.CenterX, ctrl.CenterY, targetHwnd: new IntPtr(ctrl.Hwnd));
    }

    private void StartApiServer()
    {
        try
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    Dispatcher.Invoke(() => 
                    {
                        ApiStatusText.Text = "API: 启动中...";
                        ApiStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x88, 0x88, 0x88));
                    });
                    
                    var server = new Core.Api.ApiServer(this);
                    server.Start();
                    
                    await Task.Delay(500);
                    
                    Dispatcher.Invoke(() => 
                    {
                        ApiStatusText.Text = $"API: http://localhost:{server.Port}/api";
                        ApiStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0xD4, 0xFF));
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => 
                    {
                        ApiStatusText.Text = $"API: 启动失败 ({ex.Message})";
                        ApiStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x6B, 0x6B));
                    });
                }
            });
        }
        catch (Exception ex)
        {
            ApiStatusText.Text = $"API: 启动失败 ({ex.Message})";
            ApiStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x6B, 0x6B));
        }
    }

    private void BtnConfig_Click(object sender, RoutedEventArgs e)
    {
        OpenConfig();
    }

    private void BtnScript_Click(object sender, RoutedEventArgs e)
    {
        OpenScriptEditor();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
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
        Hide();
        _trayIcon?.ShowBalloonTip("Vimina", "程序已最小化到托盘", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
    }

    private void OpenConfig()
    {
        var win = new Views.ConfigWindow();
        win.Owner = this;
        win.ShowDialog();
        ConfigManager.Load();
    }

    private void OpenScriptEditor()
    {
        var win = new Views.ScriptEditorWindow();
        win.Show();
    }

    private static void OpenDataDir()
    {
        var path = ConfigManager.ScanResultPath;
        var dir = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && System.IO.Directory.Exists(dir))
        {
            System.Diagnostics.Process.Start("explorer.exe", dir);
        }
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        ClearMarkers();
        _keyboardHook?.Dispose();
        _hotkeys?.Dispose();
        _scanner.Dispose();
        _clickEngine.Dispose();
        _trayIcon?.Dispose();
    }
}
