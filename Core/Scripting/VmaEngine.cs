using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vimina.Core.Automation;
using Vimina.Core.Helpers;

namespace Vimina.Core.Scripting;

public class VmaEngine
{
    private readonly Dictionary<string, object?> _variables = new();
    private readonly Dictionary<string, List<object?>> _arrays = new();
    private readonly Stack<LoopContext> _loopStack = new();
    private readonly List<string> _log = new();
    private readonly Dictionary<string, int> _labels = new();
    private readonly Dictionary<string, UserFunction> _userFunctions = new();
    private int _lineIndex;
    private string[] _lines = Array.Empty<string>();
    private bool _isRunning;
    private bool _isPaused;
    private bool _shouldStop;

    // Function definition mode
    private bool _inFunctionDef;
    private UserFunction? _currentFunction;
    private readonly List<string> _functionBody = new();

    // Win32 API for mouse and keyboard
    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;
    private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

    private const int SW_HIDE = 0;
    private const int SW_SHOWNORMAL = 1;
    private const int SW_SHOWMINIMIZED = 2;
    private const int SW_SHOWMAXIMIZED = 3;
    private const int SW_RESTORE = 9;

    private const uint WM_CLOSE = 0x0010;

    [DllImport("kernel32.dll")]
    private static extern long GetTickCount64();

    [DllImport("kernel32.dll")]
    private static extern bool Beep(uint dwFreq, uint dwDuration);

    private struct POINT
    {
        public int X;
        public int Y;
    }

    private ControlScanner? _scanner;
    private ClickEngine? _clickEngine;
    private Dictionary<string, ControlInfo>? _scannedControls;

    public bool IsRunning => _isRunning;
    public bool IsPaused => _isPaused;
    public int CurrentLine => _lineIndex;
    public int TotalLines => _lines.Length;
    public IReadOnlyList<string> Log => _log;
    public IReadOnlyDictionary<string, object?> Variables => _variables;

    public event Action<string>? OnLog;
    public event Action<int, int>? OnProgress;

    public async Task<VmaResult> RunAsync(string script)
    {
        // Parse script and extract labels
        var rawLines = script.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(l => l.Trim())
                             .Where(l => !string.IsNullOrWhiteSpace(l))
                             .ToList();
        
        _labels.Clear();
        _userFunctions.Clear();
        var processedLines = new List<string>();
        
        for (int i = 0; i < rawLines.Count; i++)
        {
            var line = rawLines[i];
            
            // Skip comments
            if (line.StartsWith("//")) continue;
            
            // Check for label definition: label:
            var labelMatch = Regex.Match(line, @"^(\w+):$");
            if (labelMatch.Success)
            {
                _labels[labelMatch.Groups[1].Value] = processedLines.Count;
                continue;
            }
            
            processedLines.Add(line);
        }
        
        _lines = processedLines.ToArray();
        _lineIndex = 0;
        _isRunning = true;
        _isPaused = false;
        _shouldStop = false;
        _log.Clear();
        _loopStack.Clear();
        _inFunctionDef = false;
        _currentFunction = null;
        _functionBody.Clear();

        try
        {
            while (_lineIndex < _lines.Length && _isRunning && !_shouldStop)
            {
                while (_isPaused && _isRunning && !_shouldStop)
                {
                    await Task.Delay(100);
                }

                if (!_isRunning || _shouldStop) break;

                var line = _lines[_lineIndex];
                OnProgress?.Invoke(_lineIndex + 1, _lines.Length);

                var result = await ExecuteLineAsync(line);
                
                if (!result.Success)
                {
                    return new VmaResult 
                    { 
                        Success = false, 
                        Error = $"Line {_lineIndex + 1}: {result.Error}",
                        LinesExecuted = _lineIndex + 1,
                        Log = _log.ToList()
                    };
                }

                if (result.IsLoopEnd && result.ShouldContinue)
                {
                    continue;
                }

                if (result.IsReturn)
                {
                    // Handle return - exit current function or script
                    break;
                }

                _lineIndex++;
            }

            return new VmaResult 
            { 
                Success = true, 
                LinesExecuted = _lineIndex,
                Log = _log.ToList()
            };
        }
        catch (Exception ex)
        {
            return new VmaResult 
            { 
                Success = false, 
                Error = $"Line {_lineIndex + 1}: {ex.Message}",
                LinesExecuted = _lineIndex + 1,
                Log = _log.ToList()
            };
        }
        finally
        {
            _isRunning = false;
        }
    }

    public void Stop()
    {
        _isRunning = false;
    }

    public void Pause()
    {
        _isPaused = true;
    }

    public void Resume()
    {
        _isPaused = false;
    }

    private async Task<VmaExecuteResult> ExecuteLineAsync(string line)
    {
        // Handle function definition mode
        if (_inFunctionDef)
        {
            if (line.Equals("endfunction", StringComparison.OrdinalIgnoreCase) || 
                line.Equals("end function", StringComparison.OrdinalIgnoreCase))
            {
                if (_currentFunction != null)
                {
                    _currentFunction.Body = new List<string>(_functionBody);
                    _userFunctions[_currentFunction.Name] = _currentFunction;
                }
                _inFunctionDef = false;
                _currentFunction = null;
                _functionBody.Clear();
                return VmaExecuteResult.Ok();
            }
            _functionBody.Add(line);
            return VmaExecuteResult.Ok();
        }

        var cmdLower = line.ToLowerInvariant();

        // Function definition
        var funcDefMatch = Regex.Match(line, @"^function\s+(\w+)\s*\((.*)\)$", RegexOptions.IgnoreCase);
        if (funcDefMatch.Success)
        {
            _inFunctionDef = true;
            _currentFunction = new UserFunction
            {
                Name = funcDefMatch.Groups[1].Value,
                Parameters = funcDefMatch.Groups[2].Value.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList(),
                StartLine = _lineIndex
            };
            _functionBody.Clear();
            return VmaExecuteResult.Ok();
        }

        // Variable assignment (support var x = value or x = value)
        var varMatch = Regex.Match(line, @"^(?:var\s+)?(\w+)\s*:\s*(\w+)\s*=\s*(.+)$", RegexOptions.IgnoreCase);
        if (!varMatch.Success)
            varMatch = Regex.Match(line, @"^(?:var\s+)?(\w+)\s*=\s*(.+)$", RegexOptions.IgnoreCase);
        if (varMatch.Success)
        {
            var varName = varMatch.Groups[1].Value;
            var value = EvaluateExpression(varMatch.Groups[varMatch.Groups.Count - 1].Value.Trim());
            _variables[varName] = value;
            return VmaExecuteResult.Ok();
        }

        // Array assignment: arr = [1, 2, 3]
        var arrMatch = Regex.Match(line, @"^(\w+)\s*=\s*\[(.*)\]$");
        if (arrMatch.Success)
        {
            var arrName = arrMatch.Groups[1].Value;
            var values = ParseArguments(arrMatch.Groups[2].Value);
            _arrays[arrName] = values;
            _variables[arrName] = values;
            return VmaExecuteResult.Ok();
        }

        // Goto
        var gotoMatch = Regex.Match(line, @"^goto\s+(\w+)$", RegexOptions.IgnoreCase);
        if (gotoMatch.Success)
        {
            var label = gotoMatch.Groups[1].Value;
            if (_labels.TryGetValue(label, out var targetLine))
            {
                _lineIndex = targetLine - 1; // Will be incremented after this
                return VmaExecuteResult.Ok();
            }
            return VmaExecuteResult.Fail($"Label not found: {label}");
        }

        // Return
        var returnMatch = Regex.Match(line, @"^return\s*(.*)$", RegexOptions.IgnoreCase);
        if (returnMatch.Success)
        {
            var retVal = returnMatch.Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(retVal))
            {
                _variables["_return"] = EvaluateExpression(retVal);
            }
            return new VmaExecuteResult { Success = true, IsReturn = true };
        }

        // Control flow
        if (cmdLower.StartsWith("if "))
        {
            return await ExecuteIfAsync(line);
        }

        if (cmdLower == "else" || cmdLower.StartsWith("else if") || cmdLower == "elseif")
        {
            return await ExecuteElseAsync();
        }

        if (cmdLower == "end" || cmdLower == "endif")
        {
            return VmaExecuteResult.Ok();
        }

        if (cmdLower.StartsWith("loop "))
        {
            return await ExecuteLoopAsync(line);
        }

        if (cmdLower.StartsWith("while "))
        {
            return await ExecuteWhileAsync(line);
        }

        if (cmdLower.StartsWith("for "))
        {
            return await ExecuteForAsync(line);
        }

        if (cmdLower.StartsWith("foreach "))
        {
            return await ExecuteForeachAsync(line);
        }

        if (cmdLower == "break")
        {
            return await ExecuteBreakAsync();
        }

        if (cmdLower == "continue")
        {
            return await ExecuteContinueAsync();
        }

        if (cmdLower == "endloop" || cmdLower == "next" || cmdLower == "wend")
        {
            return await ExecuteLoopEndAsync();
        }

        if (cmdLower == "break")
        {
            return await ExecuteBreakAsync();
        }

        // Utility commands
        if (cmdLower.StartsWith("sleep "))
        {
            var ms = Convert.ToInt32(EvaluateExpression(line.Substring(6).Trim()) ?? 0);
            await Task.Delay(ms);
            return VmaExecuteResult.Ok();
        }

        if (cmdLower.StartsWith("log "))
        {
            var msg = line.Substring(4).Trim();
            if (msg.StartsWith("\"") && msg.EndsWith("\""))
                msg = msg[1..^1];
            _log.Add(msg);
            OnLog?.Invoke(msg);
            return VmaExecuteResult.Ok();
        }

        // Mouse commands
        if (cmdLower == "click" || cmdLower.StartsWith("click "))
        {
            return await ExecuteClickAsync(line, false, false);
        }

        if (cmdLower == "rightclick" || cmdLower.StartsWith("rightclick ") || cmdLower.StartsWith("right click "))
        {
            return await ExecuteClickAsync(line, true, false);
        }

        if (cmdLower == "doubleclick" || cmdLower.StartsWith("doubleclick ") || cmdLower.StartsWith("double click "))
        {
            return await ExecuteClickAsync(line, false, true);
        }

        if (cmdLower.StartsWith("drag "))
        {
            return await ExecuteDragAsync(line);
        }

        if (cmdLower.StartsWith("moveto ") || cmdLower.StartsWith("move to "))
        {
            return await ExecuteMoveToAsync(line);
        }

        if (cmdLower.StartsWith("getmousepos") || cmdLower.StartsWith("get mouse pos"))
        {
            return ExecuteGetMousePos(line);
        }

        // Keyboard commands
        if (cmdLower.StartsWith("input "))
        {
            return await ExecuteInputAsync(line);
        }

        if (cmdLower.StartsWith("keypress ") || cmdLower.StartsWith("key press "))
        {
            return await ExecuteKeyPressAsync(line);
        }

        if (cmdLower.StartsWith("keydown ") || cmdLower.StartsWith("key down "))
        {
            return await ExecuteKeyDownAsync(line);
        }

        if (cmdLower.StartsWith("keyup ") || cmdLower.StartsWith("key up "))
        {
            return await ExecuteKeyUpAsync(line);
        }

        // Window commands
        if (cmdLower.StartsWith("activate ") || cmdLower.StartsWith("activatewindow "))
        {
            return await ExecuteActivateWindowAsync(line);
        }

        if (cmdLower.StartsWith("findwindow ") || cmdLower.StartsWith("find window "))
        {
            return await ExecuteFindWindowAsync(line);
        }

        if (cmdLower.StartsWith("closewindow ") || cmdLower.StartsWith("close window "))
        {
            return await ExecuteCloseWindowAsync(line);
        }

        if (cmdLower.StartsWith("minimizewindow ") || cmdLower.StartsWith("minimize window "))
        {
            return await ExecuteMinimizeWindowAsync(line);
        }

        if (cmdLower.StartsWith("maximizewindow ") || cmdLower.StartsWith("maximize window "))
        {
            return await ExecuteMaximizeWindowAsync(line);
        }

        if (cmdLower.StartsWith("restorewindow ") || cmdLower.StartsWith("restore window "))
        {
            return await ExecuteRestoreWindowAsync(line);
        }

        // Scan commands
        if (cmdLower == "scan")
        {
            return await ExecuteScanAsync();
        }

        if (cmdLower.StartsWith("scanwindow ") || cmdLower.StartsWith("scan window "))
        {
            return await ExecuteScanWindowAsync(line);
        }

        if (cmdLower.StartsWith("clicklabel ") || cmdLower.StartsWith("click label "))
        {
            return await ExecuteClickLabelAsync(line);
        }

        if (cmdLower == "show")
        {
            return await ExecuteShowAsync();
        }

        if (cmdLower == "hide")
        {
            return await ExecuteHideAsync();
        }

        // Array operations
        if (cmdLower.StartsWith("array "))
        {
            return ExecuteArray(line);
        }

        if (cmdLower.StartsWith("push "))
        {
            return ExecutePush(line);
        }

        if (cmdLower.StartsWith("pop "))
        {
            return ExecutePop(line);
        }

        // Math functions
        if (cmdLower.StartsWith("abs ") || cmdLower.StartsWith("floor ") || 
            cmdLower.StartsWith("ceil ") || cmdLower.StartsWith("round ") ||
            cmdLower.StartsWith("sqrt ") || cmdLower.StartsWith("pow ") ||
            cmdLower.StartsWith("min ") || cmdLower.StartsWith("max ") ||
            cmdLower.StartsWith("toint ") || cmdLower.StartsWith("tostring ") ||
            cmdLower.StartsWith("rand ") || cmdLower.StartsWith("random "))
        {
            return ExecuteMathFunction(line);
        }

        // Type checking
        if (cmdLower.StartsWith("type ") || cmdLower.StartsWith("isarray ") ||
            cmdLower.StartsWith("length ") || cmdLower.StartsWith("windowexists ") ||
            cmdLower.StartsWith("windowactive "))
        {
            return ExecuteTypeFunction(line);
        }

        // Screenshot
        if (cmdLower.StartsWith("screenshot"))
        {
            return await ExecuteScreenshotAsync(line);
        }

        // Get screen size
        if (cmdLower.StartsWith("getscreensize") || cmdLower.StartsWith("get screen size"))
        {
            return ExecuteGetScreenSize(line);
        }

        // Msg box
        if (cmdLower.StartsWith("msg "))
        {
            return ExecuteMsg(line);
        }

        // Wait for
        if (cmdLower.StartsWith("waitfor ") || cmdLower.StartsWith("wait for "))
        {
            return await ExecuteWaitForAsync(line);
        }

        // Scroll
        if (cmdLower.StartsWith("scroll "))
        {
            return await ExecuteScrollAsync(line);
        }

        // Type (alias for input)
        if (cmdLower.StartsWith("type "))
        {
            return await ExecuteTypeAsync(line);
        }

        // Key (supports combinations like "ctrl+c")
        if (cmdLower.StartsWith("key "))
        {
            return await ExecuteKeyAsync(line);
        }

        // Click tag (alias for clickLabel)
        if (cmdLower.StartsWith("clicktag ") || cmdLower.StartsWith("click tag "))
        {
            return await ExecuteClickTagAsync(line);
        }

        // Get control by tag
        if (cmdLower.StartsWith("getcontrolbytag ") || cmdLower.StartsWith("get control by tag "))
        {
            return ExecuteGetControlByTag(line);
        }

        // Get control list
        if (cmdLower.StartsWith("getcontrollist ") || cmdLower.StartsWith("get control list "))
        {
            return ExecuteGetControlList(line);
        }

        // Get window list
        if (cmdLower.StartsWith("getwindowlist ") || cmdLower.StartsWith("get window list "))
        {
            return ExecuteGetWindowList(line);
        }

        // Get timestamp
        if (cmdLower.StartsWith("gettimestamp") || cmdLower.StartsWith("get timestamp"))
        {
            return ExecuteGetTimestamp(line);
        }

        // Beep
        if (cmdLower == "beep" || cmdLower.StartsWith("beep "))
        {
            return ExecuteBeep(line);
        }

        // Exit
        if (cmdLower == "exit")
        {
            return ExecuteExit();
        }

        // Get version
        if (cmdLower.StartsWith("getversion") || cmdLower.StartsWith("get version"))
        {
            return ExecuteGetVersion(line);
        }

        // String functions
        if (cmdLower.StartsWith("len "))
        {
            return ExecuteLen(line);
        }

        if (cmdLower.StartsWith("substr "))
        {
            return ExecuteSubstr(line);
        }

        if (cmdLower.StartsWith("split "))
        {
            return ExecuteSplit(line);
        }

        // Function-style calls (e.g., click(100, 200))
        var funcCallMatch = Regex.Match(line, @"^(\w+)\s*\((.*)\)$");
        if (funcCallMatch.Success)
        {
            return await ExecuteFunctionCallAsync(funcCallMatch.Groups[1].Value, funcCallMatch.Groups[2].Value);
        }

        return VmaExecuteResult.Fail($"Unknown command: {line}");
    }

    private object? EvaluateExpression(string expr)
    {
        expr = expr.Trim();

        if ((expr.StartsWith("\"") && expr.EndsWith("\"")) ||
            (expr.StartsWith("'") && expr.EndsWith("'")))
        {
            return expr[1..^1];
        }

        if (double.TryParse(expr, out var num))
        {
            return num;
        }

        if (expr.Equals("true", StringComparison.OrdinalIgnoreCase))
            return true;
        if (expr.Equals("false", StringComparison.OrdinalIgnoreCase))
            return false;

        if (_variables.TryGetValue(expr, out var varValue))
        {
            return varValue;
        }

        var binaryMatch = Regex.Match(expr, @"^(.+?)\s*([+\-*/<>=!]+)\s*(.+)$");
        if (binaryMatch.Success)
        {
            var left = EvaluateExpression(binaryMatch.Groups[1].Value.Trim());
            var op = binaryMatch.Groups[2].Value.Trim();
            var right = EvaluateExpression(binaryMatch.Groups[3].Value.Trim());
            return EvaluateBinaryOperation(left, op, right);
        }

        return expr;
    }

    private object? EvaluateBinaryOperation(object? left, string op, object? right)
    {
        var leftNum = Convert.ToDouble(left ?? 0);
        var rightNum = Convert.ToDouble(right ?? 0);

        return op switch
        {
            "+" => leftNum + rightNum,
            "-" => leftNum - rightNum,
            "*" => leftNum * rightNum,
            "/" => rightNum != 0 ? leftNum / rightNum : 0,
            "==" => Equals(left, right),
            "!=" => !Equals(left, right),
            "<" => leftNum < rightNum,
            ">" => leftNum > rightNum,
            "<=" => leftNum <= rightNum,
            ">=" => leftNum >= rightNum,
            _ => null
        };
    }

    private List<object?> ParseArguments(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return new List<object?>();

        var result = new List<object?>();
        var currentArg = "";
        var inString = false;
        var stringChar = '\0';
        var parenDepth = 0;

        for (int i = 0; i < args.Length; i++)
        {
            var c = args[i];

            if (!inString && (c == '"' || c == '\''))
            {
                inString = true;
                stringChar = c;
                currentArg += c;
            }
            else if (inString && c == stringChar)
            {
                inString = false;
                currentArg += c;
            }
            else if (!inString)
            {
                if (c == '(')
                {
                    parenDepth++;
                    currentArg += c;
                }
                else if (c == ')')
                {
                    parenDepth--;
                    currentArg += c;
                }
                else if (c == ',' && parenDepth == 0)
                {
                    result.Add(EvaluateExpression(currentArg.Trim()));
                    currentArg = "";
                }
                else
                {
                    currentArg += c;
                }
            }
            else
            {
                currentArg += c;
            }
        }

        if (!string.IsNullOrWhiteSpace(currentArg))
        {
            result.Add(EvaluateExpression(currentArg.Trim()));
        }

        return result;
    }

    private async Task<VmaExecuteResult> ExecuteIfAsync(string line)
    {
        var condition = line.Substring(3).Trim();
        var result = EvaluateCondition(condition);
        
        if (!result)
        {
            var depth = 1;
            while (_lineIndex < _lines.Length - 1 && depth > 0)
            {
                _lineIndex++;
                var nextLine = _lines[_lineIndex].ToLowerInvariant();
                if (nextLine.StartsWith("if ")) depth++;
                else if (nextLine == "else" && depth == 1) { depth = 0; break; }
                else if (nextLine == "end" || nextLine == "endif") depth--;
            }
        }

        return VmaExecuteResult.Ok();
    }

    private async Task<VmaExecuteResult> ExecuteElseAsync()
    {
        var depth = 1;
        while (_lineIndex < _lines.Length - 1 && depth > 0)
        {
            _lineIndex++;
            var nextLine = _lines[_lineIndex].ToLowerInvariant();
            if (nextLine.StartsWith("if ")) depth++;
            else if (nextLine == "end" || nextLine == "endif") depth--;
        }
        return VmaExecuteResult.Ok();
    }

    private bool EvaluateCondition(string condition)
    {
        if (condition.Contains(" and ", StringComparison.OrdinalIgnoreCase))
        {
            var parts = condition.Split(new[] { " and " }, StringSplitOptions.None);
            return parts.All(p => EvaluateCondition(p.Trim()));
        }

        if (condition.Contains(" or ", StringComparison.OrdinalIgnoreCase))
        {
            var parts = condition.Split(new[] { " or " }, StringSplitOptions.None);
            return parts.Any(p => EvaluateCondition(p.Trim()));
        }

        var match = Regex.Match(condition, @"^(.+?)\s*([<>=!]+)\s*(.+)$");
        if (match.Success)
        {
            var left = EvaluateExpression(match.Groups[1].Value.Trim());
            var op = match.Groups[2].Value.Trim();
            var right = EvaluateExpression(match.Groups[3].Value.Trim());
            var result = EvaluateBinaryOperation(left, op, right);
            return Convert.ToBoolean(result ?? false);
        }

        var value = EvaluateExpression(condition);
        return Convert.ToBoolean(value ?? false);
    }

    private async Task<VmaExecuteResult> ExecuteLoopAsync(string line)
    {
        var countStr = line.Substring(5).Trim();
        var count = Convert.ToInt32(EvaluateExpression(countStr) ?? 0);
        
        _loopStack.Push(new LoopContext
        {
            Type = LoopType.Times,
            Count = count,
            Current = 0,
            StartLine = _lineIndex
        });

        return VmaExecuteResult.Ok();
    }

    private async Task<VmaExecuteResult> ExecuteWhileAsync(string line)
    {
        var condition = line.Substring(6).Trim();
        
        _loopStack.Push(new LoopContext
        {
            Type = LoopType.While,
            Condition = condition,
            StartLine = _lineIndex
        });

        if (!EvaluateCondition(condition))
        {
            var depth = 1;
            while (_lineIndex < _lines.Length - 1 && depth > 0)
            {
                _lineIndex++;
                var nextLine = _lines[_lineIndex].ToLowerInvariant();
                if (nextLine.StartsWith("while ")) depth++;
                else if (nextLine == "endloop" || nextLine == "wend") depth--;
            }
            _loopStack.Pop();
        }

        return VmaExecuteResult.Ok();
    }

    private async Task<VmaExecuteResult> ExecuteForAsync(string line)
    {
        var match = Regex.Match(line, @"^for\s+(\w+)\s*=\s*(.+)\s+to\s+(.+?)(?:\s+step\s+(.+))?$", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var varName = match.Groups[1].Value;
            var startVal = Convert.ToInt32(EvaluateExpression(match.Groups[2].Value.Trim()) ?? 0);
            var endVal = Convert.ToInt32(EvaluateExpression(match.Groups[3].Value.Trim()) ?? 0);
            var stepVal = match.Groups[4].Success ? Convert.ToInt32(EvaluateExpression(match.Groups[4].Value.Trim()) ?? 1) : 1;

            _variables[varName] = startVal;
            
            _loopStack.Push(new LoopContext
            {
                Type = LoopType.For,
                VarName = varName,
                StartValue = startVal,
                EndValue = endVal,
                StepValue = stepVal,
                StartLine = _lineIndex
            });

            return VmaExecuteResult.Ok();
        }

        return VmaExecuteResult.Fail("Invalid for loop syntax");
    }

    private async Task<VmaExecuteResult> ExecuteLoopEndAsync()
    {
        if (_loopStack.Count == 0)
            return VmaExecuteResult.Ok();

        var loop = _loopStack.Peek();
        bool shouldContinue = false;

        switch (loop.Type)
        {
            case LoopType.Times:
                loop.Current++;
                shouldContinue = loop.Current < loop.Count;
                break;

            case LoopType.While:
                shouldContinue = EvaluateCondition(loop.Condition ?? "false");
                break;

            case LoopType.For:
                var current = Convert.ToInt32(_variables[loop.VarName ?? ""] ?? 0) + loop.StepValue;
                _variables[loop.VarName ?? ""] = current;
                shouldContinue = loop.StepValue > 0 ? current <= loop.EndValue : current >= loop.EndValue;
                break;

            case LoopType.Foreach:
                if (_arrays.TryGetValue(loop.ArrayName ?? "", out var arr))
                {
                    if (loop.Index < arr.Count)
                    {
                        _variables[loop.VarName ?? ""] = arr[loop.Index];
                        loop.Index++;
                        shouldContinue = true;
                    }
                }
                break;
        }

        if (shouldContinue)
        {
            _lineIndex = loop.StartLine;
            return new VmaExecuteResult { Success = true, IsLoopEnd = true, ShouldContinue = true };
        }
        else
        {
            _loopStack.Pop();
            return new VmaExecuteResult { Success = true, IsLoopEnd = true };
        }
    }

    private async Task<VmaExecuteResult> ExecuteBreakAsync()
    {
        if (_loopStack.Count > 0)
        {
            _loopStack.Pop();
            
            var depth = 1;
            while (_lineIndex < _lines.Length - 1 && depth > 0)
            {
                _lineIndex++;
                var nextLine = _lines[_lineIndex].ToLowerInvariant();
                if (nextLine.StartsWith("loop ") || nextLine.StartsWith("while ") || nextLine.StartsWith("for ") || nextLine.StartsWith("foreach "))
                    depth++;
                else if (nextLine == "endloop" || nextLine == "wend" || nextLine == "next")
                    depth--;
            }
        }
        return VmaExecuteResult.Ok();
    }

    private async Task<VmaExecuteResult> ExecuteForeachAsync(string line)
    {
        var match = Regex.Match(line, @"foreach\s+(\w+)\s+in\s+(\w+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var varName = match.Groups[1].Value;
            var arrName = match.Groups[2].Value;

            if (_arrays.TryGetValue(arrName, out var arr) && arr.Count > 0)
            {
                _variables[varName] = arr[0];
                
                _loopStack.Push(new LoopContext
                {
                    Type = LoopType.Foreach,
                    VarName = varName,
                    ArrayName = arrName,
                    Index = 1,
                    StartLine = _lineIndex
                });

                return VmaExecuteResult.Ok();
            }
            else
            {
                // Skip loop body
                var depth = 1;
                while (_lineIndex < _lines.Length - 1 && depth > 0)
                {
                    _lineIndex++;
                    var nextLine = _lines[_lineIndex].ToLowerInvariant();
                    if (nextLine.StartsWith("foreach ")) depth++;
                    else if (nextLine == "endloop" || nextLine == "next") depth--;
                }
                return VmaExecuteResult.Ok();
            }
        }
        return VmaExecuteResult.Fail("Invalid foreach syntax. Use: foreach varName in arrayName");
    }

    private async Task<VmaExecuteResult> ExecuteContinueAsync()
    {
        if (_loopStack.Count > 0)
        {
            var loop = _loopStack.Peek();
            _lineIndex = loop.StartLine;
            
            // Find the end of the loop to trigger loop logic
            var depth = 1;
            while (_lineIndex < _lines.Length - 1 && depth > 0)
            {
                _lineIndex++;
                var nextLine = _lines[_lineIndex].ToLowerInvariant();
                if (nextLine.StartsWith("loop ") || nextLine.StartsWith("while ") || nextLine.StartsWith("for ") || nextLine.StartsWith("foreach "))
                    depth++;
                else if (nextLine == "endloop" || nextLine == "wend" || nextLine == "next")
                    depth--;
            }
            
            // Now execute the loop end logic
            return await ExecuteLoopEndAsync();
        }
        return VmaExecuteResult.Ok();
    }

    #region Mouse Commands

    private async Task<VmaExecuteResult> ExecuteClickAsync(string line, bool rightClick, bool doubleClick)
    {
        var args = line.Contains(' ') ? line.Substring(line.IndexOf(' ') + 1).Trim() : "";
        
        int x, y;
        if (string.IsNullOrEmpty(args))
        {
            // Click at current position
            GetCursorPos(out var pt);
            x = pt.X;
            y = pt.Y;
        }
        else if (args.Contains(','))
        {
            // Click at specified coordinates
            var parts = args.Split(',');
            x = Convert.ToInt32(EvaluateExpression(parts[0].Trim()) ?? 0);
            y = Convert.ToInt32(EvaluateExpression(parts[1].Trim()) ?? 0);
            SetCursorPos(x, y);
        }
        else if (_scannedControls != null && _scannedControls.TryGetValue(args, out var control))
        {
            // Click on label
            x = (int)(control.X + control.Width / 2);
            y = (int)(control.Y + control.Height / 2);
            SetCursorPos(x, y);
        }
        else
        {
            return VmaExecuteResult.Fail($"Invalid click target: {args}");
        }

        await Task.Delay(50);

        if (rightClick)
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
            await Task.Delay(50);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
        }
        else if (doubleClick)
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            await Task.Delay(50);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            await Task.Delay(50);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            await Task.Delay(50);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }
        else
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            await Task.Delay(50);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        return VmaExecuteResult.Ok();
    }

    private async Task<VmaExecuteResult> ExecuteDragAsync(string line)
    {
        var args = line.Substring(5).Trim();
        var parts = args.Split(new[] { " to " }, StringSplitOptions.None);
        if (parts.Length != 2)
        {
            return VmaExecuteResult.Fail("Invalid drag syntax. Use: drag x1,y1 to x2,y2");
        }

        var start = parts[0].Split(',');
        var end = parts[1].Split(',');

        var x1 = Convert.ToInt32(EvaluateExpression(start[0].Trim()) ?? 0);
        var y1 = Convert.ToInt32(EvaluateExpression(start[1].Trim()) ?? 0);
        var x2 = Convert.ToInt32(EvaluateExpression(end[0].Trim()) ?? 0);
        var y2 = Convert.ToInt32(EvaluateExpression(end[1].Trim()) ?? 0);

        SetCursorPos(x1, y1);
        await Task.Delay(100);
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        await Task.Delay(100);
        SetCursorPos(x2, y2);
        await Task.Delay(100);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);

        return VmaExecuteResult.Ok();
    }

    private async Task<VmaExecuteResult> ExecuteMoveToAsync(string line)
    {
        var args = line.Contains("moveto ") ? line.Substring(7).Trim() : line.Substring(8).Trim();
        var parts = args.Split(',');
        
        var x = Convert.ToInt32(EvaluateExpression(parts[0].Trim()) ?? 0);
        var y = Convert.ToInt32(EvaluateExpression(parts[1].Trim()) ?? 0);
        
        SetCursorPos(x, y);
        return VmaExecuteResult.Ok();
    }

    private VmaExecuteResult ExecuteGetMousePos(string line)
    {
        var match = Regex.Match(line, @"getmousepos\s+(\w+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var varName = match.Groups[1].Value;
            GetCursorPos(out var pt);
            _variables[varName] = new Dictionary<string, object?>
            {
                ["x"] = pt.X,
                ["y"] = pt.Y
            };
            return VmaExecuteResult.Ok();
        }
        return VmaExecuteResult.Fail("Invalid syntax. Use: getmousepos varName");
    }

    #endregion

    #region Keyboard Commands

    private async Task<VmaExecuteResult> ExecuteInputAsync(string line)
    {
        var text = line.Substring(6).Trim();
        if (text.StartsWith("\"") && text.EndsWith("\""))
            text = text[1..^1];
        
        foreach (var ch in text)
        {
            SendKeys.SendWait(ch.ToString());
            await Task.Delay(10);
        }
        
        return VmaExecuteResult.Ok();
    }

    private async Task<VmaExecuteResult> ExecuteKeyPressAsync(string line)
    {
        var key = line.Contains("keypress ") ? line.Substring(9).Trim() : line.Substring(10).Trim();
        key = key.Trim('"', '\'');
        
        var vk = ParseKey(key);
        if (vk == 0)
        {
            SendKeys.SendWait(key);
        }
        else
        {
            keybd_event(vk, 0, 0, 0);
            await Task.Delay(50);
            keybd_event(vk, 0, 2, 0);
        }
        
        return VmaExecuteResult.Ok();
    }

    private async Task<VmaExecuteResult> ExecuteKeyDownAsync(string line)
    {
        var key = line.Contains("keydown ") ? line.Substring(8).Trim() : line.Substring(9).Trim();
        key = key.Trim('"', '\'');
        
        var vk = ParseKey(key);
        if (vk != 0)
        {
            keybd_event(vk, 0, 0, 0);
        }
        
        return VmaExecuteResult.Ok();
    }

    private async Task<VmaExecuteResult> ExecuteKeyUpAsync(string line)
    {
        var key = line.Contains("keyup ") ? line.Substring(7).Trim() : line.Substring(8).Trim();
        key = key.Trim('"', '\'');
        
        var vk = ParseKey(key);
        if (vk != 0)
        {
            keybd_event(vk, 0, 2, 0);
        }
        
        return VmaExecuteResult.Ok();
    }

    private byte ParseKey(string key)
    {
        return key.ToUpperInvariant() switch
        {
            "A" => 0x41, "B" => 0x42, "C" => 0x43, "D" => 0x44, "E" => 0x45, "F" => 0x46,
            "G" => 0x47, "H" => 0x48, "I" => 0x49, "J" => 0x4A, "K" => 0x4B, "L" => 0x4C,
            "M" => 0x4D, "N" => 0x4E, "O" => 0x4F, "P" => 0x50, "Q" => 0x51, "R" => 0x52,
            "S" => 0x53, "T" => 0x54, "U" => 0x55, "V" => 0x56, "W" => 0x57, "X" => 0x58,
            "Y" => 0x59, "Z" => 0x5A,
            "0" => 0x30, "1" => 0x31, "2" => 0x32, "3" => 0x33, "4" => 0x34,
            "5" => 0x35, "6" => 0x36, "7" => 0x37, "8" => 0x38, "9" => 0x39,
            "ENTER" or "RETURN" => 0x0D,
            "ESC" or "ESCAPE" => 0x1B,
            "SPACE" => 0x20,
            "TAB" => 0x09,
            "BACK" or "BACKSPACE" => 0x08,
            "DELETE" or "DEL" => 0x2E,
            "INSERT" or "INS" => 0x2D,
            "HOME" => 0x24,
            "END" => 0x23,
            "PAGEUP" or "PGUP" => 0x21,
            "PAGEDOWN" or "PGDN" => 0x22,
            "UP" => 0x26, "DOWN" => 0x28, "LEFT" => 0x25, "RIGHT" => 0x27,
            "F1" => 0x70, "F2" => 0x71, "F3" => 0x72, "F4" => 0x73, "F5" => 0x74,
            "F6" => 0x75, "F7" => 0x76, "F8" => 0x77, "F9" => 0x78, "F10" => 0x79,
            "F11" => 0x7A, "F12" => 0x7B,
            "SHIFT" => 0x10, "CTRL" or "CONTROL" => 0x11, "ALT" => 0x12,
            "WIN" or "LWIN" => 0x5B, "RWIN" => 0x5C,
            "CAPS" or "CAPSLOCK" => 0x14,
            "NUMLOCK" => 0x90, "SCROLLLOCK" => 0x91,
            _ => 0
        };
    }

    #endregion

    #region Window Commands

    private async Task<VmaExecuteResult> ExecuteActivateWindowAsync(string line)
    {
        var title = line.Contains("activatewindow ") ? line.Substring(15).Trim() : line.Substring(9).Trim();
        title = title.Trim('"', '\'');
        
        var hwnd = FindWindowByTitle(title);
        if (hwnd != IntPtr.Zero)
        {
            SetForegroundWindow(hwnd);
            return VmaExecuteResult.Ok();
        }
        
        return VmaExecuteResult.Fail($"Window not found: {title}");
    }

    private async Task<VmaExecuteResult> ExecuteFindWindowAsync(string line)
    {
        var match = Regex.Match(line, @"findwindow\s+(\w+)\s*,\s*""([^""]*)""", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var varName = match.Groups[1].Value;
            var title = match.Groups[2].Value;
            var hwnd = FindWindowByTitle(title);
            _variables[varName] = hwnd.ToInt64();
            return VmaExecuteResult.Ok();
        }
        return VmaExecuteResult.Fail("Invalid syntax. Use: findwindow varName, \"title\"");
    }

    private async Task<VmaExecuteResult> ExecuteCloseWindowAsync(string line)
    {
        var title = line.Contains("closewindow ") ? line.Substring(12).Trim() : line.Substring(12).Trim();
        title = title.Trim('"', '\'');
        
        var hwnd = FindWindowByTitle(title);
        if (hwnd != IntPtr.Zero)
        {
            PostMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            return VmaExecuteResult.Ok();
        }
        
        return VmaExecuteResult.Fail($"Window not found: {title}");
    }

    private async Task<VmaExecuteResult> ExecuteMinimizeWindowAsync(string line)
    {
        var title = line.Contains("minimizewindow ") ? line.Substring(15).Trim() : line.Substring(14).Trim();
        title = title.Trim('"', '\'');
        
        var hwnd = FindWindowByTitle(title);
        if (hwnd != IntPtr.Zero)
        {
            ShowWindow(hwnd, SW_SHOWMINIMIZED);
            return VmaExecuteResult.Ok();
        }
        
        return VmaExecuteResult.Fail($"Window not found: {title}");
    }

    private async Task<VmaExecuteResult> ExecuteMaximizeWindowAsync(string line)
    {
        var title = line.Contains("maximizewindow ") ? line.Substring(15).Trim() : line.Substring(14).Trim();
        title = title.Trim('"', '\'');
        
        var hwnd = FindWindowByTitle(title);
        if (hwnd != IntPtr.Zero)
        {
            ShowWindow(hwnd, SW_SHOWMAXIMIZED);
            return VmaExecuteResult.Ok();
        }
        
        return VmaExecuteResult.Fail($"Window not found: {title}");
    }

    private async Task<VmaExecuteResult> ExecuteRestoreWindowAsync(string line)
    {
        var title = line.Contains("restorewindow ") ? line.Substring(14).Trim() : line.Substring(13).Trim();
        title = title.Trim('"', '\'');
        
        var hwnd = FindWindowByTitle(title);
        if (hwnd != IntPtr.Zero)
        {
            ShowWindow(hwnd, SW_RESTORE);
            return VmaExecuteResult.Ok();
        }
        
        return VmaExecuteResult.Fail($"Window not found: {title}");
    }

    private IntPtr FindWindowByTitle(string title)
    {
        IntPtr result = IntPtr.Zero;
        EnumWindows((hwnd, lParam) =>
        {
            var sb = new System.Text.StringBuilder(256);
            GetWindowText(hwnd, sb, 256);
            var windowTitle = sb.ToString();
            if (windowTitle.Contains(title, StringComparison.OrdinalIgnoreCase))
            {
                result = hwnd;
                return false;
            }
            return true;
        }, IntPtr.Zero);
        return result;
    }

    #endregion

    #region Scan Commands

    private async Task<VmaExecuteResult> ExecuteScanAsync()
    {
        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero)
        {
            return VmaExecuteResult.Fail("No foreground window found");
        }

        _scanner ??= new ControlScanner();
        var controls = await Task.Run(() => _scanner.ScanInteractiveControls(foregroundWindow));
        
        _scannedControls = new Dictionary<string, ControlInfo>();
        var labels = new List<string>();
        
        foreach (var control in controls)
        {
            var label = control.Label;
            if (!string.IsNullOrEmpty(label))
            {
                _scannedControls[label] = control;
                labels.Add(label);
            }
        }

        _variables["labels"] = labels;
        _variables["labelCount"] = labels.Count;

        return VmaExecuteResult.Ok();
    }

    private async Task<VmaExecuteResult> ExecuteScanWindowAsync(string line)
    {
        var title = line.Contains("scanwindow ") ? line.Substring(11).Trim() : line.Substring(11).Trim();
        title = title.Trim('"', '\'');
        
        var hwnd = FindWindowByTitle(title);
        if (hwnd == IntPtr.Zero)
        {
            return VmaExecuteResult.Fail($"Window not found: {title}");
        }

        _scanner ??= new ControlScanner();
        var controls = await Task.Run(() => _scanner.ScanInteractiveControls(hwnd));
        
        _scannedControls = new Dictionary<string, ControlInfo>();
        var labels = new List<string>();
        
        foreach (var control in controls)
        {
            var label = control.Label;
            if (!string.IsNullOrEmpty(label))
            {
                _scannedControls[label] = control;
                labels.Add(label);
            }
        }

        _variables["labels"] = labels;
        _variables["labelCount"] = labels.Count;

        return VmaExecuteResult.Ok();
    }

    private async Task<VmaExecuteResult> ExecuteClickLabelAsync(string line)
    {
        var label = line.Contains("clicklabel ") ? line.Substring(11).Trim() : line.Substring(11).Trim();
        label = label.Trim('"', '\'');

        if (_scannedControls != null && _scannedControls.TryGetValue(label, out var control))
        {
            var x = (int)(control.X + control.Width / 2);
            var y = (int)(control.Y + control.Height / 2);
            
            SetCursorPos(x, y);
            await Task.Delay(50);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            await Task.Delay(50);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            
            return VmaExecuteResult.Ok();
        }

        return VmaExecuteResult.Fail($"Label not found: {label}. Run scan or scanWindow first.");
    }

    private async Task<VmaExecuteResult> ExecuteShowAsync()
    {
        // Show labels - this would need integration with the UI
        // For now, just log the available labels
        if (_scannedControls != null)
        {
            foreach (var kvp in _scannedControls)
            {
                _log.Add($"Label {kvp.Key}: {kvp.Value.Name} at ({kvp.Value.X}, {kvp.Value.Y})");
            }
        }
        return VmaExecuteResult.Ok();
    }

    private async Task<VmaExecuteResult> ExecuteHideAsync()
    {
        // Hide labels - this would need integration with the UI
        return VmaExecuteResult.Ok();
    }

    #endregion

    #region Array Operations

    private VmaExecuteResult ExecuteArray(string line)
    {
        var match = Regex.Match(line, @"array\s+(\w+)\s*=\s*\[(.*)\]", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var arrName = match.Groups[1].Value;
            var values = match.Groups[2].Value.Split(',')
                .Select(v => EvaluateExpression(v.Trim()))
                .ToList();
            _arrays[arrName] = values;
            return VmaExecuteResult.Ok();
        }
        return VmaExecuteResult.Fail("Invalid array syntax. Use: array name = [val1, val2, ...]");
    }

    private VmaExecuteResult ExecutePush(string line)
    {
        var match = Regex.Match(line, @"push\s+(\w+)\s*,\s*(.+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var arrName = match.Groups[1].Value;
            var value = EvaluateExpression(match.Groups[2].Value.Trim());
            if (!_arrays.ContainsKey(arrName))
                _arrays[arrName] = new List<object?>();
            _arrays[arrName].Add(value);
            return VmaExecuteResult.Ok();
        }
        return VmaExecuteResult.Fail("Invalid push syntax. Use: push arrayName, value");
    }

    private VmaExecuteResult ExecutePop(string line)
    {
        var match = Regex.Match(line, @"pop\s+(\w+)(?:\s+(\w+))?", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var arrName = match.Groups[1].Value;
            var varName = match.Groups[2].Success ? match.Groups[2].Value : null;
            
            if (_arrays.TryGetValue(arrName, out var arr) && arr.Count > 0)
            {
                var value = arr[^1];
                arr.RemoveAt(arr.Count - 1);
                if (varName != null)
                    _variables[varName] = value;
                return VmaExecuteResult.Ok();
            }
        }
        return VmaExecuteResult.Fail("Invalid pop syntax. Use: pop arrayName [varName]");
    }

    #endregion

    #region Math Functions

    private VmaExecuteResult ExecuteMathFunction(string line)
    {
        var spaceIdx = line.IndexOf(' ');
        if (spaceIdx < 0) return VmaExecuteResult.Fail("Invalid math function syntax");
        
        var func = line.Substring(0, spaceIdx).ToLowerInvariant();
        var args = line.Substring(spaceIdx + 1).Trim();
        
        // Check for variable assignment: func result = expression
        var assignMatch = Regex.Match(args, @"^(\w+)\s*=\s*(.+)$");
        if (assignMatch.Success)
        {
            var varName = assignMatch.Groups[1].Value;
            var expr = assignMatch.Groups[2].Value;
            var result = EvaluateMathFunction(func, expr);
            _variables[varName] = result;
            return VmaExecuteResult.Ok();
        }
        
        // Direct function call
        var directResult = EvaluateMathFunction(func, args);
        return VmaExecuteResult.Ok();
    }

    private object? EvaluateMathFunction(string func, string args)
    {
        var values = args.Split(',').Select(a => Convert.ToDouble(EvaluateExpression(a.Trim()) ?? 0)).ToList();
        
        return func switch
        {
            "abs" => Math.Abs(values[0]),
            "floor" => Math.Floor(values[0]),
            "ceil" => Math.Ceiling(values[0]),
            "round" => Math.Round(values[0]),
            "sqrt" => Math.Sqrt(values[0]),
            "pow" => values.Count >= 2 ? Math.Pow(values[0], values[1]) : values[0],
            "min" => values.Min(),
            "max" => values.Max(),
            "rand" or "random" => values.Count >= 2 
                ? new Random().Next((int)values[0], (int)values[1]) 
                : new Random().Next((int)values[0]),
            "toint" => (int)values[0],
            "tostring" => values[0].ToString(),
            _ => null
        };
    }

    #endregion

    #region Type Functions

    private VmaExecuteResult ExecuteTypeFunction(string line)
    {
        var spaceIdx = line.IndexOf(' ');
        if (spaceIdx < 0) return VmaExecuteResult.Fail("Invalid type function syntax");
        
        var func = line.Substring(0, spaceIdx).ToLowerInvariant();
        var args = line.Substring(spaceIdx + 1).Trim();
        
        var assignMatch = Regex.Match(args, @"^(\w+)\s*=\s*(.+)$");
        if (assignMatch.Success)
        {
            var varName = assignMatch.Groups[1].Value;
            var expr = assignMatch.Groups[2].Value;
            var result = EvaluateTypeFunction(func, expr);
            _variables[varName] = result;
            return VmaExecuteResult.Ok();
        }
        
        return VmaExecuteResult.Fail("Type functions require variable assignment");
    }

    private object? EvaluateTypeFunction(string func, string expr)
    {
        var value = EvaluateExpression(expr);
        
        return func switch
        {
            "type" => value?.GetType().Name ?? "null",
            "isarray" => value is List<object?>,
            "length" => value switch
            {
                string s => s.Length,
                List<object?> l => l.Count,
                _ => 0
            },
            "windowexists" => FindWindowByTitle(expr.Trim('"', '\'')) != IntPtr.Zero,
            "windowactive" => GetForegroundWindow() == FindWindowByTitle(expr.Trim('"', '\'')),
            _ => null
        };
    }

    #endregion

    #region Utility Commands

    private async Task<VmaExecuteResult> ExecuteScreenshotAsync(string line)
    {
        var filename = line.Contains(' ') ? line.Substring(line.IndexOf(' ') + 1).Trim() : $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        filename = filename.Trim('"', '\'');
        
        try
        {
            var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
            using var bitmap = new Bitmap(bounds.Width, bounds.Height);
            using var g = Graphics.FromImage(bitmap);
            g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            bitmap.Save(filename);
            return VmaExecuteResult.Ok();
        }
        catch (Exception ex)
        {
            return VmaExecuteResult.Fail($"Screenshot failed: {ex.Message}");
        }
    }

    private VmaExecuteResult ExecuteGetScreenSize(string line)
    {
        var match = Regex.Match(line, @"getscreensize\s+(\w+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var varName = match.Groups[1].Value;
            var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
            _variables[varName] = new Dictionary<string, object?>
            {
                ["width"] = bounds.Width,
                ["height"] = bounds.Height
            };
            return VmaExecuteResult.Ok();
        }
        return VmaExecuteResult.Fail("Invalid syntax. Use: getscreensize varName");
    }

    private VmaExecuteResult ExecuteMsg(string line)
    {
        var msg = line.Substring(4).Trim();
        if (msg.StartsWith("\"") && msg.EndsWith("\""))
            msg = msg[1..^1];
        
        MessageBox.Show(msg, "VMA Script", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return VmaExecuteResult.Ok();
    }

    private async Task<VmaExecuteResult> ExecuteWaitForAsync(string line)
    {
        var args = line.Contains("waitfor ") ? line.Substring(8).Trim() : line.Substring(9).Trim();
        
        // waitfor window "title"
        var windowMatch = Regex.Match(args, @"window\s+""([^""]*)""", RegexOptions.IgnoreCase);
        if (windowMatch.Success)
        {
            var title = windowMatch.Groups[1].Value;
            var timeout = 30000; // 30 seconds default
            
            var startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalMilliseconds < timeout)
            {
                if (FindWindowByTitle(title) != IntPtr.Zero)
                    return VmaExecuteResult.Ok();
                await Task.Delay(500);
            }
            return VmaExecuteResult.Fail($"Timeout waiting for window: {title}");
        }
        
        // waitfor ms
        if (int.TryParse(args, out var ms))
        {
            await Task.Delay(ms);
            return VmaExecuteResult.Ok();
        }
        
        return VmaExecuteResult.Fail("Invalid waitfor syntax");
    }

    private async Task<VmaExecuteResult> ExecuteScrollAsync(string line)
    {
        var deltaStr = line.Substring(7).Trim();
        var delta = Convert.ToInt32(EvaluateExpression(deltaStr) ?? 0);
        var wheelDelta = delta * 120; // Standard wheel delta is 120
        mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)wheelDelta, 0);
        await Task.Delay(50);
        return VmaExecuteResult.Ok();
    }

    private async Task<VmaExecuteResult> ExecuteTypeAsync(string line)
    {
        var text = line.Substring(5).Trim();
        if (text.StartsWith("\"") && text.EndsWith("\""))
            text = text[1..^1];
        
        foreach (var ch in text)
        {
            SendKeys.SendWait(ch.ToString());
            await Task.Delay(10);
        }
        return VmaExecuteResult.Ok();
    }

    private async Task<VmaExecuteResult> ExecuteKeyAsync(string line)
    {
        var keyCombo = line.Substring(4).Trim().Trim('"', '\'');
        var keys = keyCombo.Split('+', StringSplitOptions.RemoveEmptyEntries);
        
        // Press all modifier keys
        foreach (var key in keys)
        {
            var vk = ParseKey(key.Trim());
            if (vk != 0)
            {
                keybd_event(vk, 0, 0, 0);
                await Task.Delay(50);
            }
        }
        
        // Release all keys in reverse order
        for (int i = keys.Length - 1; i >= 0; i--)
        {
            var vk = ParseKey(keys[i].Trim());
            if (vk != 0)
            {
                keybd_event(vk, 0, 2, 0);
                await Task.Delay(50);
            }
        }
        
        return VmaExecuteResult.Ok();
    }

    private async Task<VmaExecuteResult> ExecuteClickTagAsync(string line)
    {
        var tag = line.Contains("clicktag ") ? line.Substring(9).Trim() : line.Substring(10).Trim();
        tag = tag.Trim('"', '\'');
        return await ExecuteClickLabelAsync("clickLabel " + tag);
    }

    private VmaExecuteResult ExecuteGetControlByTag(string line)
    {
        var match = Regex.Match(line, @"getcontrolbytag\s+(\w+)\s*,\s*""([^""]*)""", RegexOptions.IgnoreCase);
        if (!match.Success)
            match = Regex.Match(line, @"get control by tag\s+(\w+)\s*,\s*""([^""]*)""", RegexOptions.IgnoreCase);
        
        if (match.Success)
        {
            var varName = match.Groups[1].Value;
            var tag = match.Groups[2].Value;
            
            if (_scannedControls != null && _scannedControls.TryGetValue(tag, out var control))
            {
                _variables[varName] = new Dictionary<string, object?>
                {
                    ["name"] = control.Name,
                    ["type"] = control.Type,
                    ["x"] = control.X,
                    ["y"] = control.Y,
                    ["width"] = control.Width,
                    ["height"] = control.Height,
                    ["tag"] = control.Label
                };
                return VmaExecuteResult.Ok();
            }
            return VmaExecuteResult.Fail($"Control with tag '{tag}' not found");
        }
        return VmaExecuteResult.Fail("Invalid syntax. Use: getcontrolbytag varName, \"tag\"");
    }

    private VmaExecuteResult ExecuteGetControlList(string line)
    {
        var match = Regex.Match(line, @"getcontrollist\s+(\w+)", RegexOptions.IgnoreCase);
        if (!match.Success)
            match = Regex.Match(line, @"get control list\s+(\w+)", RegexOptions.IgnoreCase);
        
        if (match.Success)
        {
            var varName = match.Groups[1].Value;
            var list = new List<Dictionary<string, object?>>();
            
            if (_scannedControls != null)
            {
                foreach (var kvp in _scannedControls)
                {
                    list.Add(new Dictionary<string, object?>
                    {
                        ["tag"] = kvp.Key,
                        ["name"] = kvp.Value.Name,
                        ["type"] = kvp.Value.Type,
                        ["x"] = kvp.Value.X,
                        ["y"] = kvp.Value.Y
                    });
                }
            }
            
            _variables[varName] = list;
            return VmaExecuteResult.Ok();
        }
        return VmaExecuteResult.Fail("Invalid syntax. Use: getcontrollist varName");
    }

    private VmaExecuteResult ExecuteGetWindowList(string line)
    {
        var match = Regex.Match(line, @"getwindowlist\s+(\w+)", RegexOptions.IgnoreCase);
        if (!match.Success)
            match = Regex.Match(line, @"get window list\s+(\w+)", RegexOptions.IgnoreCase);
        
        if (match.Success)
        {
            var varName = match.Groups[1].Value;
            var windows = new List<Dictionary<string, object?>>();
            
            EnumWindows((hwnd, lParam) =>
            {
                if (IsWindowVisible(hwnd))
                {
                    var sb = new System.Text.StringBuilder(256);
                    GetWindowText(hwnd, sb, 256);
                    var title = sb.ToString();
                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        windows.Add(new Dictionary<string, object?>
                        {
                            ["title"] = title,
                            ["hwnd"] = hwnd.ToInt64()
                        });
                    }
                }
                return true;
            }, IntPtr.Zero);
            
            _variables[varName] = windows;
            return VmaExecuteResult.Ok();
        }
        return VmaExecuteResult.Fail("Invalid syntax. Use: getwindowlist varName");
    }

    private VmaExecuteResult ExecuteGetTimestamp(string line)
    {
        var match = Regex.Match(line, @"gettimestamp\s+(\w+)", RegexOptions.IgnoreCase);
        if (!match.Success)
            match = Regex.Match(line, @"get timestamp\s+(\w+)", RegexOptions.IgnoreCase);
        
        if (match.Success)
        {
            var varName = match.Groups[1].Value;
            _variables[varName] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            return VmaExecuteResult.Ok();
        }
        return VmaExecuteResult.Fail("Invalid syntax. Use: gettimestamp varName");
    }

    private VmaExecuteResult ExecuteBeep(string line)
    {
        var freq = 800u;
        var duration = 200u;
        
        var args = line.Length > 5 ? line.Substring(5).Trim() : "";
        if (!string.IsNullOrEmpty(args))
        {
            var parts = args.Split(',');
            if (parts.Length >= 1) freq = Convert.ToUInt32(EvaluateExpression(parts[0].Trim()) ?? 800);
            if (parts.Length >= 2) duration = Convert.ToUInt32(EvaluateExpression(parts[1].Trim()) ?? 200);
        }
        
        Beep(freq, duration);
        return VmaExecuteResult.Ok();
    }

    private VmaExecuteResult ExecuteExit()
    {
        _isRunning = false;
        return VmaExecuteResult.Ok();
    }

    private VmaExecuteResult ExecuteGetVersion(string line)
    {
        var match = Regex.Match(line, @"getversion\s+(\w+)", RegexOptions.IgnoreCase);
        if (!match.Success)
            match = Regex.Match(line, @"get version\s+(\w+)", RegexOptions.IgnoreCase);
        
        if (match.Success)
        {
            var varName = match.Groups[1].Value;
            _variables[varName] = "1.5.0";
            return VmaExecuteResult.Ok();
        }
        return VmaExecuteResult.Fail("Invalid syntax. Use: getversion varName");
    }

    private VmaExecuteResult ExecuteLen(string line)
    {
        var match = Regex.Match(line, @"len\s+(\w+)\s*=\s*(.+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var varName = match.Groups[1].Value;
            var expr = match.Groups[2].Value.Trim();
            var value = EvaluateExpression(expr);
            
            var length = value switch
            {
                string s => s.Length,
                List<object?> l => l.Count,
                _ => 0
            };
            
            _variables[varName] = length;
            return VmaExecuteResult.Ok();
        }
        return VmaExecuteResult.Fail("Invalid syntax. Use: len varName = expression");
    }

    private VmaExecuteResult ExecuteSubstr(string line)
    {
        var match = Regex.Match(line, @"substr\s+(\w+)\s*=\s*(.+),\s*(\d+),\s*(\d+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var varName = match.Groups[1].Value;
            var str = EvaluateExpression(match.Groups[2].Value.Trim())?.ToString() ?? "";
            var start = Convert.ToInt32(match.Groups[3].Value);
            var length = Convert.ToInt32(match.Groups[4].Value);
            
            if (start < 0) start = 0;
            if (start + length > str.Length) length = str.Length - start;
            if (length < 0) length = 0;
            
            _variables[varName] = str.Substring(start, length);
            return VmaExecuteResult.Ok();
        }
        return VmaExecuteResult.Fail("Invalid syntax. Use: substr varName = string, start, length");
    }

    private VmaExecuteResult ExecuteSplit(string line)
    {
        var match = Regex.Match(line, @"split\s+(\w+)\s*=\s*(.+),\s*""([^""]*)""", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var varName = match.Groups[1].Value;
            var str = EvaluateExpression(match.Groups[2].Value.Trim())?.ToString() ?? "";
            var delimiter = match.Groups[3].Value;
            
            var parts = str.Split(new[] { delimiter }, StringSplitOptions.None);
            _variables[varName] = parts.ToList<object?>();
            return VmaExecuteResult.Ok();
        }
        return VmaExecuteResult.Fail("Invalid syntax. Use: split varName = string, \"delimiter\"");
    }

    // Parse named arguments like "x=100, y=200, useBackend=1"
    private Dictionary<string, object?> ParseNamedArguments(string args)
    {
        var result = new Dictionary<string, object?>();
        if (string.IsNullOrWhiteSpace(args)) return result;

        var pairs = args.Split(',');
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
            {
                var name = parts[0].Trim();
                var value = EvaluateExpression(parts[1].Trim());
                result[name] = value;
            }
            else if (parts.Length == 1)
            {
                // Positional argument, store with index
                result[$"_{result.Count}"] = EvaluateExpression(parts[0].Trim());
            }
        }
        return result;
    }

    // Function-style call handler (e.g., click(100, 200) or click(x=100, y=200, useBackend=1))
    private async Task<VmaExecuteResult> ExecuteFunctionCallAsync(string funcName, string args)
    {
        var funcLower = funcName.ToLowerInvariant();
        var namedArgs = ParseNamedArguments(args);
        
        // Helper to get argument value
        object? GetArg(string name, int pos, object? defaultValue = null)
        {
            if (namedArgs.TryGetValue(name, out var val)) return val;
            if (namedArgs.TryGetValue($"_{pos}", out val)) return val;
            return defaultValue;
        }

        switch (funcLower)
        {
            case "click":
                {
                    var x = Convert.ToInt32(GetArg("x", 0, 0));
                    var y = Convert.ToInt32(GetArg("y", 1, 0));
                    var useBackend = Convert.ToBoolean(GetArg("useBackend", 2, false));
                    var useMouse = Convert.ToBoolean(GetArg("useMouse", 3, false));
                    
                    if (useBackend)
                    {
                        // Use FlaUI backend click
                        // TODO: Implement backend click using _clickEngine
                    }
                    
                    SetCursorPos(x, y);
                    await Task.Delay(50);
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                    await Task.Delay(50);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    return VmaExecuteResult.Ok();
                }

            case "rightclick":
            case "clickr":
                {
                    var x = Convert.ToInt32(GetArg("x", 0, 0));
                    var y = Convert.ToInt32(GetArg("y", 1, 0));
                    var useBackend = Convert.ToBoolean(GetArg("useBackend", 2, false));
                    var useMouse = Convert.ToBoolean(GetArg("useMouse", 3, false));
                    
                    SetCursorPos(x, y);
                    await Task.Delay(50);
                    mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                    await Task.Delay(50);
                    mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                    return VmaExecuteResult.Ok();
                }

            case "doubleclick":
            case "dblclick":
                {
                    var x = Convert.ToInt32(GetArg("x", 0, 0));
                    var y = Convert.ToInt32(GetArg("y", 1, 0));
                    var useBackend = Convert.ToBoolean(GetArg("useBackend", 2, false));
                    var useMouse = Convert.ToBoolean(GetArg("useMouse", 3, false));
                    
                    SetCursorPos(x, y);
                    await Task.Delay(50);
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                    await Task.Delay(50);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    await Task.Delay(50);
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                    await Task.Delay(50);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    return VmaExecuteResult.Ok();
                }

            case "clickbytitle":
                {
                    var title = GetArg("title", 0, "")?.ToString() ?? "";
                    var x = Convert.ToInt32(GetArg("x", 1, 0));
                    var y = Convert.ToInt32(GetArg("y", 2, 0));
                    var useBackend = Convert.ToBoolean(GetArg("useBackend", 3, false));
                    var useMouse = Convert.ToBoolean(GetArg("useMouse", 4, false));
                    var rightClick = Convert.ToBoolean(GetArg("rightClick", 5, false));
                    var doubleClick = Convert.ToBoolean(GetArg("doubleClick", 6, false));
                    
                    var hwnd = FindWindowByTitle(title);
                    if (hwnd == IntPtr.Zero)
                        return VmaExecuteResult.Fail($"Window not found: {title}");
                    
                    if (!useBackend)
                    {
                        SetForegroundWindow(hwnd);
                        await Task.Delay(100);
                    }
                    
                    SetCursorPos(x, y);
                    await Task.Delay(50);
                    
                    if (doubleClick)
                    {
                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                        await Task.Delay(50);
                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                        await Task.Delay(50);
                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                        await Task.Delay(50);
                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    }
                    else if (rightClick)
                    {
                        mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                        await Task.Delay(50);
                        mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                    }
                    else
                    {
                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                        await Task.Delay(50);
                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    }
                    return VmaExecuteResult.Ok();
                }

            case "waitfor":
                {
                    var title = GetArg("title", 0, "")?.ToString() ?? "";
                    var timeout = Convert.ToInt32(GetArg("timeout", 1, 30));
                    
                    var startTime = DateTime.Now;
                    while ((DateTime.Now - startTime).TotalSeconds < timeout)
                    {
                        if (FindWindowByTitle(title) != IntPtr.Zero)
                            return VmaExecuteResult.Ok();
                        await Task.Delay(500);
                    }
                    return VmaExecuteResult.Fail($"Timeout waiting for window: {title}");
                }

            case "getwindows":
                {
                    var windows = new List<Dictionary<string, object?>>();
                    EnumWindows((hwnd, lParam) =>
                    {
                        if (IsWindowVisible(hwnd))
                        {
                            var sb = new System.Text.StringBuilder(256);
                            GetWindowText(hwnd, sb, 256);
                            var wtitle = sb.ToString();
                            if (!string.IsNullOrWhiteSpace(wtitle))
                            {
                                windows.Add(new Dictionary<string, object?>
                                {
                                    ["title"] = wtitle,
                                    ["hwnd"] = hwnd.ToInt64()
                                });
                            }
                        }
                        return true;
                    }, IntPtr.Zero);
                    _variables["_"] = windows;
                    return VmaExecuteResult.Ok();
                }

            case "loop":
                {
                    var count = Convert.ToInt32(GetArg("count", 0, 1));
                    _loopStack.Push(new LoopContext
                    {
                        Type = LoopType.Times,
                        Count = count,
                        Current = 0,
                        StartLine = _lineIndex
                    });
                    return VmaExecuteResult.Ok();
                }

            case "while":
                {
                    // while(condition) - condition is in args
                    var condition = args.Trim();
                    _loopStack.Push(new LoopContext
                    {
                        Type = LoopType.While,
                        Condition = condition,
                        StartLine = _lineIndex
                    });
                    if (!EvaluateCondition(condition))
                    {
                        // Skip loop body
                        var depth = 1;
                        while (_lineIndex < _lines.Length - 1 && depth > 0)
                        {
                            _lineIndex++;
                            var nextLine = _lines[_lineIndex].ToLowerInvariant();
                            if (nextLine.StartsWith("while ")) depth++;
                            else if (nextLine == "endloop" || nextLine == "wend" || nextLine == "endwhile") depth--;
                        }
                        _loopStack.Pop();
                    }
                    return VmaExecuteResult.Ok();
                }

            case "move":
            case "moveto":
                {
                    var x = Convert.ToInt32(GetArg("x", 0, 0));
                    var y = Convert.ToInt32(GetArg("y", 1, 0));
                    SetCursorPos(x, y);
                    return VmaExecuteResult.Ok();
                }

            case "drag":
                {
                    var x1 = Convert.ToInt32(GetArg("x1", 0, 0));
                    var y1 = Convert.ToInt32(GetArg("y1", 1, 0));
                    var x2 = Convert.ToInt32(GetArg("x2", 2, 0));
                    var y2 = Convert.ToInt32(GetArg("y2", 3, 0));
                    SetCursorPos(x1, y1);
                    await Task.Delay(100);
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                    await Task.Delay(100);
                    SetCursorPos(x2, y2);
                    await Task.Delay(100);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    return VmaExecuteResult.Ok();
                }

            case "scroll":
                {
                    var delta = Convert.ToInt32(GetArg("delta", 0, 0));
                    var wheelDelta = delta * 120;
                    mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)wheelDelta, 0);
                    await Task.Delay(50);
                    return VmaExecuteResult.Ok();
                }

            case "type":
                {
                    var text = GetArg("text", 0, "")?.ToString() ?? "";
                    foreach (var ch in text)
                    {
                        SendKeys.SendWait(ch.ToString());
                        await Task.Delay(10);
                    }
                    return VmaExecuteResult.Ok();
                }

            case "key":
            case "keypress":
                {
                    var keyCombo = GetArg("key", 0, "")?.ToString() ?? "";
                    var keys = keyCombo.Split('+', StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var key in keys)
                    {
                        var vk = ParseKey(key.Trim());
                        if (vk != 0)
                        {
                            keybd_event(vk, 0, 0, 0);
                            await Task.Delay(50);
                        }
                    }
                    
                    for (int i = keys.Length - 1; i >= 0; i--)
                    {
                        var vk = ParseKey(keys[i].Trim());
                        if (vk != 0)
                        {
                            keybd_event(vk, 0, 2, 0);
                            await Task.Delay(50);
                        }
                    }
                    return VmaExecuteResult.Ok();
                }

            case "keydown":
                {
                    var keyName = GetArg("key", 0, "")?.ToString() ?? "";
                    var vk = ParseKey(keyName.Trim());
                    if (vk != 0) keybd_event(vk, 0, 0, 0);
                    return VmaExecuteResult.Ok();
                }

            case "keyup":
                {
                    var keyName = GetArg("key", 0, "")?.ToString() ?? "";
                    var vk = ParseKey(keyName.Trim());
                    if (vk != 0) keybd_event(vk, 0, 2, 0);
                    return VmaExecuteResult.Ok();
                }

            case "sleep":
                {
                    var ms = Convert.ToInt32(GetArg("ms", 0, 0));
                    await Task.Delay(ms);
                    return VmaExecuteResult.Ok();
                }

            case "log":
                {
                    var msg = GetArg("message", 0, "")?.ToString() ?? "";
                    _log.Add(msg);
                    OnLog?.Invoke(msg);
                    return VmaExecuteResult.Ok();
                }

            case "random":
                {
                    var min = Convert.ToInt32(GetArg("min", 0, 0));
                    var max = Convert.ToInt32(GetArg("max", 1, 100));
                    _variables["_"] = new Random().Next(min, max);
                    return VmaExecuteResult.Ok();
                }

            case "randomfloat":
                _variables["_"] = new Random().NextDouble();
                return VmaExecuteResult.Ok();

            case "abs":
                _variables["_"] = Math.Abs(Convert.ToDouble(GetArg("x", 0, 0)));
                return VmaExecuteResult.Ok();

            case "floor":
                _variables["_"] = Math.Floor(Convert.ToDouble(GetArg("x", 0, 0)));
                return VmaExecuteResult.Ok();

            case "ceil":
                _variables["_"] = Math.Ceiling(Convert.ToDouble(GetArg("x", 0, 0)));
                return VmaExecuteResult.Ok();

            case "round":
                _variables["_"] = Math.Round(Convert.ToDouble(GetArg("x", 0, 0)));
                return VmaExecuteResult.Ok();

            case "sqrt":
                _variables["_"] = Math.Sqrt(Convert.ToDouble(GetArg("x", 0, 0)));
                return VmaExecuteResult.Ok();

            case "pow":
                {
                    var x = Convert.ToDouble(GetArg("x", 0, 0));
                    var y = Convert.ToDouble(GetArg("y", 1, 0));
                    _variables["_"] = Math.Pow(x, y);
                    return VmaExecuteResult.Ok();
                }

            case "min":
                {
                    var x = Convert.ToDouble(GetArg("x", 0, 0));
                    var y = Convert.ToDouble(GetArg("y", 1, 0));
                    _variables["_"] = Math.Min(x, y);
                    return VmaExecuteResult.Ok();
                }

            case "max":
                {
                    var x = Convert.ToDouble(GetArg("x", 0, 0));
                    var y = Convert.ToDouble(GetArg("y", 1, 0));
                    _variables["_"] = Math.Max(x, y);
                    return VmaExecuteResult.Ok();
                }

            case "getmousepos":
                {
                    GetCursorPos(out var pt);
                    _variables["_"] = new Dictionary<string, object?>
                    {
                        ["x"] = pt.X,
                        ["y"] = pt.Y
                    };
                    return VmaExecuteResult.Ok();
                }

            case "gettimestamp":
                _variables["_"] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                return VmaExecuteResult.Ok();

            case "getversion":
                _variables["_"] = "1.5.0";
                return VmaExecuteResult.Ok();

            case "beep":
                {
                    var freq = Convert.ToUInt32(GetArg("freq", 0, 800));
                    var duration = Convert.ToUInt32(GetArg("duration", 1, 200));
                    Beep(freq, duration);
                    return VmaExecuteResult.Ok();
                }

            case "exit":
                _isRunning = false;
                return VmaExecuteResult.Ok();

            case "len":
                {
                    var val = GetArg("value", 0, "");
                    _variables["_"] = val switch
                    {
                        string s => s.Length,
                        List<object?> l => l.Count,
                        _ => 0
                    };
                    return VmaExecuteResult.Ok();
                }

            case "activate":
            case "activatewindow":
                {
                    var title = GetArg("title", 0, "")?.ToString() ?? "";
                    var hwnd = FindWindowByTitle(title);
                    if (hwnd != IntPtr.Zero)
                    {
                        SetForegroundWindow(hwnd);
                        return VmaExecuteResult.Ok();
                    }
                    return VmaExecuteResult.Fail($"Window not found: {title}");
                }

            case "findwindow":
                {
                    var title = GetArg("title", 0, "")?.ToString() ?? "";
                    var hwnd = FindWindowByTitle(title);
                    _variables["_"] = hwnd.ToInt64();
                    return VmaExecuteResult.Ok();
                }

            case "closewindow":
                {
                    var title = GetArg("title", 0, "")?.ToString() ?? "";
                    var hwnd = FindWindowByTitle(title);
                    if (hwnd != IntPtr.Zero)
                    {
                        PostMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                        return VmaExecuteResult.Ok();
                    }
                    return VmaExecuteResult.Fail($"Window not found: {title}");
                }

            case "minimizewindow":
                {
                    var title = GetArg("title", 0, "")?.ToString() ?? "";
                    var hwnd = FindWindowByTitle(title);
                    if (hwnd != IntPtr.Zero)
                    {
                        ShowWindow(hwnd, SW_SHOWMINIMIZED);
                        return VmaExecuteResult.Ok();
                    }
                    return VmaExecuteResult.Fail($"Window not found: {title}");
                }

            case "maximizewindow":
                {
                    var title = GetArg("title", 0, "")?.ToString() ?? "";
                    var hwnd = FindWindowByTitle(title);
                    if (hwnd != IntPtr.Zero)
                    {
                        ShowWindow(hwnd, SW_SHOWMAXIMIZED);
                        return VmaExecuteResult.Ok();
                    }
                    return VmaExecuteResult.Fail($"Window not found: {title}");
                }

            case "restorewindow":
                {
                    var title = GetArg("title", 0, "")?.ToString() ?? "";
                    var hwnd = FindWindowByTitle(title);
                    if (hwnd != IntPtr.Zero)
                    {
                        ShowWindow(hwnd, SW_RESTORE);
                        return VmaExecuteResult.Ok();
                    }
                    return VmaExecuteResult.Fail($"Window not found: {title}");
                }

            case "scanwindow":
                {
                    var title = GetArg("title", 0, "")?.ToString() ?? "";
                    var hwnd = FindWindowByTitle(title);
                    if (hwnd == IntPtr.Zero)
                    {
                        return VmaExecuteResult.Fail($"Window not found: {title}");
                    }

                    _scanner ??= new ControlScanner();
                    var controls = await Task.Run(() => _scanner.ScanInteractiveControls(hwnd));
                    
                    _scannedControls = new Dictionary<string, ControlInfo>();
                    var labels = new List<string>();
                    
                    foreach (var control in controls)
                    {
                        var label = control.Label;
                        if (!string.IsNullOrEmpty(label))
                        {
                            _scannedControls[label] = control;
                            labels.Add(label);
                        }
                    }

                    _variables["labels"] = labels;
                    _variables["labelCount"] = labels.Count;
                    return VmaExecuteResult.Ok();
                }

            case "clicklabel":
                {
                    var label = GetArg("label", 0, "")?.ToString() ?? "";
                    if (_scannedControls != null && _scannedControls.TryGetValue(label, out var control))
                    {
                        var x = (int)(control.X + control.Width / 2);
                        var y = (int)(control.Y + control.Height / 2);
                        
                        SetCursorPos(x, y);
                        await Task.Delay(50);
                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                        await Task.Delay(50);
                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                        
                        return VmaExecuteResult.Ok();
                    }
                    return VmaExecuteResult.Fail($"Label not found: {label}. Run scan or scanWindow first.");
                }

            case "screenshot":
                {
                    var filename = GetArg("filename", 0, $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png")?.ToString() ?? "";
                    try
                    {
                        var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
                        using var bitmap = new Bitmap(bounds.Width, bounds.Height);
                        using var g = Graphics.FromImage(bitmap);
                        g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                        bitmap.Save(filename);
                        return VmaExecuteResult.Ok();
                    }
                    catch (Exception ex)
                    {
                        return VmaExecuteResult.Fail($"Screenshot failed: {ex.Message}");
                    }
                }

            case "array":
                {
                    var arrName = GetArg("name", 0, "")?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(arrName))
                    {
                        _arrays[arrName] = new List<object?>();
                        _variables[arrName] = _arrays[arrName];
                    }
                    return VmaExecuteResult.Ok();
                }

            default:
                return VmaExecuteResult.Fail($"Unknown function: {funcName}");
        }
    }

    #endregion
}

public class VmaResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int LinesExecuted { get; set; }
    public List<string> Log { get; set; } = new();
}

public class VmaExecuteResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public bool IsLoopStart { get; set; }
    public bool IsLoopEnd { get; set; }
    public bool ShouldContinue { get; set; }
    public bool IsReturn { get; set; }

    public static VmaExecuteResult Ok() => new() { Success = true };
    public static VmaExecuteResult Fail(string message) => new() { Success = false, Error = message };
}

public class LoopContext
{
    public LoopType Type { get; set; }
    public int Count { get; set; }
    public int Current { get; set; }
    public int StartLine { get; set; }
    public string? Condition { get; set; }
    public string? VarName { get; set; }
    public int StartValue { get; set; }
    public int EndValue { get; set; }
    public int StepValue { get; set; } = 1;
    public string? ArrayName { get; set; }
    public int Index { get; set; }
}

public enum LoopType
{
    Times,
    While,
    For,
    Foreach
}

public class UserFunction
{
    public string Name { get; set; } = "";
    public List<string> Parameters { get; set; } = new();
    public List<string> Body { get; set; } = new();
    public int StartLine { get; set; }
}
