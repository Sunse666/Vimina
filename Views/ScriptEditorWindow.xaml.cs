using System.Diagnostics;
using System.IO;
using System.Windows;
using Vimina.Core.Scripting;

namespace Vimina.Views;

public partial class ScriptEditorWindow : Window
{
    private string? _currentFile;
    private VmaEngine? _vmaEngine;
    private bool _isRunning;

    public ScriptEditorWindow()
    {
        InitializeComponent();
        _vmaEngine = new VmaEngine();
        _vmaEngine.OnLog += msg => Dispatcher.Invoke(() => Output.AppendText($"\n[日志] {msg}"));
        _vmaEngine.OnProgress += (current, total) => 
            Dispatcher.Invoke(() => Title = $"脚本编辑器 - 运行中 ({current}/{total})");
    }

    private void BtnNew_Click(object sender, RoutedEventArgs e)
    {
        Editor.Text = "";
        _currentFile = null;
        Title = "脚本编辑器 - 新建";
        Output.Text = "就绪";
    }

    private void BtnOpen_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "VMA 脚本 (*.vma)|*.vma|所有文件 (*.*)|*.*",
            Title = "打开脚本"
        };
        if (dlg.ShowDialog() == true)
        {
            _currentFile = dlg.FileName;
            Editor.Text = File.ReadAllText(_currentFile);
            Title = $"脚本编辑器 - {Path.GetFileName(_currentFile)}";
            Output.Text = $"已打开: {_currentFile}";
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (_currentFile == null)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "VMA 脚本 (*.vma)|*.vma",
                Title = "保存脚本"
            };
            if (dlg.ShowDialog() == true)
                _currentFile = dlg.FileName;
            else
                return;
        }
        File.WriteAllText(_currentFile, Editor.Text);
        Title = $"脚本编辑器 - {Path.GetFileName(_currentFile)}";
        Output.AppendText($"\n[保存] {_currentFile}");
    }

    private async void BtnRun_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning)
        {
            // 如果正在运行，则停止
            _vmaEngine?.Stop();
            Output.AppendText("\n[停止] 用户中断");
            return;
        }

        var script = Editor.Text;
        if (string.IsNullOrWhiteSpace(script))
        {
            Output.Text = "错误: 脚本为空";
            return;
        }

        Output.Text = "[开始运行脚本...]";
        _isRunning = true;
        StatusText.Text = "运行中...";
        
        try
        {
            var result = await _vmaEngine!.RunAsync(script);
            
            if (result.Success)
            {
                Output.AppendText($"\n[完成] 成功执行 {result.LinesExecuted} 行");
                StatusText.Text = "完成";
            }
            else
            {
                Output.AppendText($"\n[错误] {result.Error}");
                StatusText.Text = "错误";
            }
            
            if (result.Log.Count > 0)
            {
                Output.AppendText("\n[输出日志]");
                foreach (var log in result.Log)
                {
                    Output.AppendText($"\n  {log}");
                }
            }
        }
        catch (Exception ex)
        {
            Output.AppendText($"\n[异常] {ex.Message}");
            StatusText.Text = "异常";
        }
        finally
        {
            _isRunning = false;
            UpdateTitle();
        }
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning)
        {
            _vmaEngine?.Stop();
            Output.AppendText("\n[停止] 用户中断");
            StatusText.Text = "已停止";
            _isRunning = false;
            UpdateTitle();
        }
    }

    private void BtnClearOutput_Click(object sender, RoutedEventArgs e)
    {
        Output.Text = "等待脚本执行...";
        OutputStats.Text = "";
    }

    private void Editor_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdateLineNumbers();
    }

    private void Editor_ScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e)
    {
        // 同步行号滚动
        if (LineNumbers != null)
        {
            LineNumbers.ScrollToVerticalOffset(Editor.VerticalOffset);
        }
    }

    private void UpdateLineNumbers()
    {
        if (LineNumbers == null || Editor == null) return;
        
        var lineCount = Editor.Text.Split('\n').Length;
        var lines = string.Join("\n", Enumerable.Range(1, lineCount));
        LineNumbers.Text = lines;
    }

    private void UpdateTitle()
    {
        var fileName = _currentFile != null ? Path.GetFileName(_currentFile) : "未命名.vma";
        FileNameText.Text = fileName;
    }

    private void BtnCompile_Click(object sender, RoutedEventArgs e)
    {
        if (_currentFile == null)
        {
            Output.Text = "错误: 请先保存脚本";
            return;
        }

        Output.Text = "[编译中...]";
        
        try
        {
            // 检查编译器是否存在
            var compilerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "vma-compiler.exe");
            if (!File.Exists(compilerPath))
            {
                Output.Text = "错误: 编译器未找到，请确保 vma-compiler.exe 在 tools 目录中";
                return;
            }

            // 启动编译进程
            var outputPath = Path.ChangeExtension(_currentFile, ".exe");
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = compilerPath,
                    Arguments = $"\"{_currentFile}\" \"{outputPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                Output.Text = $"[编译成功]\n输出: {outputPath}";
                if (!string.IsNullOrEmpty(output))
                    Output.AppendText($"\n{output}");
            }
            else
            {
                Output.Text = $"[编译失败]\n错误: {error}\n输出: {output}";
            }
        }
        catch (Exception ex)
        {
            Output.Text = $"[编译异常] {ex.Message}";
        }
    }
}
