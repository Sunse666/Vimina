using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Indentation;
using ICSharpCode.AvalonEdit.Rendering;
using Vimina.Core.Scripting;

using MediaBrush = System.Windows.Media.Brush;
using MediaPen = System.Windows.Media.Pen;
using MediaColor = System.Windows.Media.Color;

namespace Vimina.Views;

public partial class ScriptEditorWindow : Window
{
    private string? _currentFile;
    private VmaEngine? _vmaEngine;
    private bool _isRunning;
    private FoldingManager? _foldingManager;
    private VmaFoldingStrategy? _foldingStrategy;
    private BracketHighlightRenderer? _bracketHighlighter;

    public ScriptEditorWindow()
    {
        InitializeComponent();
        _vmaEngine = new VmaEngine();
        _vmaEngine.OnLog += msg => Dispatcher.Invoke(() => Output.AppendText($"\n[日志] {msg}"));
        _vmaEngine.OnProgress += (current, total) => 
            Dispatcher.Invoke(() => StatusText.Text = $"运行中 ({current}/{total})");
        
        InitializeSyntaxHighlighting();
        InitializeFunctionComboBox();
        InitializeAutoCompletion();
        InitializeSmartIndent();
        InitializeBracketMatcher();
        InitializeCodeFolding();
        InitializeEditorOptions();
    }

    #region Smart Indent

    private void InitializeSmartIndent()
    {
        Editor.TextArea.IndentationStrategy = new VmaIndentationStrategy(Editor.Options);
    }

    private class VmaIndentationStrategy : IIndentationStrategy
    {
        private readonly TextEditorOptions _options;
        private static readonly string[] IncreaseIndentKeywords = { "if", "else", "elseif", "while", "for", "loop", "function", "foreach" };
        private static readonly string[] DecreaseIndentKeywords = { "endif", "endwhile", "endloop", "endfunction", "end", "else", "elseif" };
        private static readonly string[] BlockEndKeywords = { "endif", "endwhile", "endloop", "endfunction", "end" };

        public VmaIndentationStrategy(TextEditorOptions options)
        {
            _options = options;
        }

        public void IndentLine(TextDocument document, DocumentLine line)
        {
            if (line.LineNumber <= 1)
            {
                document.Insert(line.Offset, "");
                return;
            }

            var previousLine = document.GetLineByNumber(line.LineNumber - 1);
            var previousLineText = document.GetText(previousLine).Trim();
            var currentLineText = document.GetText(line).TrimStart();

            var indentation = GetIndentation(document, previousLine);

            if (ShouldIncreaseIndent(previousLineText))
            {
                indentation = IncreaseIndent(indentation);
            }

            if (ShouldDecreaseIndent(currentLineText))
            {
                indentation = DecreaseIndent(indentation);
            }

            var currentIndentation = GetLineIndentation(document, line);
            if (currentIndentation != indentation)
            {
                var lineText = document.GetText(line);
                var trimmedStart = lineText.TrimStart();
                document.Replace(line.Offset, line.Length, indentation + trimmedStart);
            }
        }

        public void IndentLines(TextDocument document, int beginLine, int endLine)
        {
            for (int i = beginLine; i <= endLine; i++)
            {
                var line = document.GetLineByNumber(i);
                IndentLine(document, line);
            }
        }

        private string GetIndentation(TextDocument document, DocumentLine line)
        {
            var text = document.GetText(line);
            var indent = "";
            foreach (var c in text)
            {
                if (c == ' ' || c == '\t')
                    indent += c;
                else
                    break;
            }
            return indent;
        }

        private string GetLineIndentation(TextDocument document, DocumentLine line)
        {
            var text = document.GetText(line);
            var indent = "";
            foreach (var c in text)
            {
                if (c == ' ' || c == '\t')
                    indent += c;
                else
                    break;
            }
            return indent;
        }

        private bool ShouldIncreaseIndent(string lineText)
        {
            var trimmed = lineText.TrimEnd();
            if (string.IsNullOrEmpty(trimmed)) return false;

            var lower = trimmed.ToLowerInvariant();
            foreach (var keyword in IncreaseIndentKeywords)
            {
                if (lower.StartsWith(keyword + " ") || lower.StartsWith(keyword + "(") || lower == keyword)
                {
                    if (keyword == "else" || keyword == "elseif")
                    {
                        return !lower.Contains("endif") && !lower.Contains("end");
                    }
                    return true;
                }
            }
            return false;
        }

        private bool ShouldDecreaseIndent(string lineText)
        {
            if (string.IsNullOrEmpty(lineText)) return false;

            var lower = lineText.ToLowerInvariant().Trim();
            foreach (var keyword in BlockEndKeywords)
            {
                if (lower == keyword || lower.StartsWith(keyword + " "))
                {
                    return true;
                }
            }
            if (lower == "else" || lower.StartsWith("else ") || lower.StartsWith("elseif "))
            {
                return true;
            }
            return false;
        }

        private string IncreaseIndent(string indent)
        {
            if (_options.ConvertTabsToSpaces)
                return indent + new string(' ', _options.IndentationSize);
            else
                return indent + "\t";
        }

        private string DecreaseIndent(string indent)
        {
            if (string.IsNullOrEmpty(indent)) return indent;

            if (_options.ConvertTabsToSpaces && indent.Length >= _options.IndentationSize)
            {
                return indent.Substring(0, indent.Length - _options.IndentationSize);
            }
            else if (indent.EndsWith("\t"))
            {
                return indent.Substring(0, indent.Length - 1);
            }
            else if (indent.EndsWith("  "))
            {
                return indent.Substring(0, indent.Length - 2);
            }
            return indent;
        }
    }

    #endregion

    #region Bracket Matcher

    private void InitializeBracketMatcher()
    {
        _bracketHighlighter = new BracketHighlightRenderer(Editor.TextArea.TextView);
        Editor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
    }

    private void Caret_PositionChanged(object? sender, EventArgs e)
    {
        _bracketHighlighter?.UpdateHighlight(Editor.Document, Editor.TextArea.Caret.Offset);
    }

    private class BracketHighlightRenderer : IBackgroundRenderer
    {
        private readonly TextView _textView;
        private BracketSearchResult? _result;
        private readonly MediaPen _borderPen;
        private readonly MediaBrush _backgroundBrush;

        public BracketHighlightRenderer(TextView textView)
        {
            _textView = textView;
            _borderPen = new MediaPen(new SolidColorBrush(MediaColor.FromRgb(0x00, 0xd4, 0xff)), 1);
            _borderPen.Freeze();
            _backgroundBrush = new SolidColorBrush(MediaColor.FromArgb(0x30, 0x00, 0xd4, 0xff));
            _backgroundBrush.Freeze();
        }

        public KnownLayer Layer => KnownLayer.Background;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (_result == null) return;

            var builder = new BackgroundGeometryBuilder
            {
                CornerRadius = 2,
                AlignToWholePixels = true,
                BorderThickness = _borderPen.Thickness
            };

            var startSegment = new TextSegment { StartOffset = _result.OpeningBracketOffset, Length = _result.OpeningBracketLength };
            var endSegment = new TextSegment { StartOffset = _result.ClosingBracketOffset, Length = _result.ClosingBracketLength };

            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, startSegment))
            {
                builder.AddRectangle(textView, rect);
            }
            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, endSegment))
            {
                builder.AddRectangle(textView, rect);
            }

            var geometry = builder.CreateGeometry();
            if (geometry != null)
            {
                drawingContext.DrawGeometry(_backgroundBrush, _borderPen, geometry);
            }
        }

        public void UpdateHighlight(TextDocument document, int offset)
        {
            if (offset < 0 || offset >= document.TextLength)
            {
                _result = null;
                _textView.InvalidateLayer(Layer);
                return;
            }

            _result = SearchForBrackets(document, offset);
            _textView.InvalidateLayer(Layer);
        }

        private BracketSearchResult? SearchForBrackets(TextDocument document, int offset)
        {
            var openBrackets = "([{";
            var closeBrackets = ")]}";
            char openBracket = '\0', closeBracket = '\0';
            int searchOffset = offset;

            if (offset > 0)
            {
                var charBefore = document.GetCharAt(offset - 1);
                var idx = openBrackets.IndexOf(charBefore);
                if (idx >= 0)
                {
                    openBracket = charBefore;
                    closeBracket = closeBrackets[idx];
                    searchOffset = offset;
                }
                else
                {
                    idx = closeBrackets.IndexOf(charBefore);
                    if (idx >= 0)
                    {
                        openBracket = openBrackets[idx];
                        closeBracket = charBefore;
                        searchOffset = offset - 1;
                    }
                }
            }

            if (offset < document.TextLength)
            {
                var charAt = document.GetCharAt(offset);
                var idx = openBrackets.IndexOf(charAt);
                if (idx >= 0)
                {
                    openBracket = charAt;
                    closeBracket = closeBrackets[idx];
                    searchOffset = offset + 1;
                }
                else
                {
                    idx = closeBrackets.IndexOf(charAt);
                    if (idx >= 0)
                    {
                        openBracket = openBrackets[idx];
                        closeBracket = charAt;
                        searchOffset = offset;
                    }
                }
            }

            if (openBracket == '\0') return null;

            if (document.GetCharAt(searchOffset - 1) == openBracket)
            {
                return SearchForward(document, searchOffset - 1, openBracket, closeBracket);
            }
            else
            {
                return SearchBackward(document, searchOffset, openBracket, closeBracket);
            }
        }

        private BracketSearchResult? SearchForward(TextDocument document, int offset, char openBracket, char closeBracket)
        {
            var length = 1;
            var depth = 1;
            var i = offset + 1;

            while (i < document.TextLength)
            {
                var c = document.GetCharAt(i);
                if (c == openBracket) depth++;
                else if (c == closeBracket)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return new BracketSearchResult(offset, length, i, length);
                    }
                }
                i++;
            }
            return null;
        }

        private BracketSearchResult? SearchBackward(TextDocument document, int offset, char openBracket, char closeBracket)
        {
            var length = 1;
            var depth = 1;
            var i = offset - 1;

            while (i >= 0)
            {
                var c = document.GetCharAt(i);
                if (c == closeBracket) depth++;
                else if (c == openBracket)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return new BracketSearchResult(i, length, offset, length);
                    }
                }
                i--;
            }
            return null;
        }
    }

    private class BracketSearchResult
    {
        public int OpeningBracketOffset { get; }
        public int OpeningBracketLength { get; }
        public int ClosingBracketOffset { get; }
        public int ClosingBracketLength { get; }

        public BracketSearchResult(int openingOffset, int openingLength, int closingOffset, int closingLength)
        {
            OpeningBracketOffset = openingOffset;
            OpeningBracketLength = openingLength;
            ClosingBracketOffset = closingOffset;
            ClosingBracketLength = closingLength;
        }
    }

    #endregion

    #region Code Folding

    private void InitializeCodeFolding()
    {
        _foldingManager = FoldingManager.Install(Editor.TextArea);
        _foldingStrategy = new VmaFoldingStrategy();
        Editor.Document.TextChanged += (s, e) => UpdateFoldings();
        UpdateFoldings();
    }

    private void UpdateFoldings()
    {
        if (_foldingManager == null || _foldingStrategy == null) return;
        _foldingStrategy.UpdateFoldings(_foldingManager, Editor.Document);
    }

    private class VmaFoldingStrategy
    {
        private static readonly Regex FoldingStartPattern = new Regex(
            @"^\s*(function|loop\s|while\s|for\s|foreach\s|if\s)", 
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
        private static readonly Regex FoldingEndPattern = new Regex(
            @"^\s*(endfunction|endloop|endwhile|next|endif|end)\s*$", 
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public void UpdateFoldings(FoldingManager manager, TextDocument document)
        {
            var newFoldings = new List<NewFolding>();
            var stack = new Stack<(int StartLine, int StartOffset, string Type)>();

            for (int i = 1; i <= document.LineCount; i++)
            {
                var line = document.GetLineByNumber(i);
                var text = document.GetText(line);

                var startMatch = FoldingStartPattern.Match(text);
                if (startMatch.Success)
                {
                    var type = startMatch.Groups[1].Value.ToLowerInvariant();
                    stack.Push((i, line.Offset, type));
                }

                var endMatch = FoldingEndPattern.Match(text);
                if (endMatch.Success && stack.Count > 0)
                {
                    var (startLine, startOffset, type) = stack.Pop();
                    var endOffset = line.Offset + line.Length;
                    newFoldings.Add(new NewFolding(startOffset, endOffset)
                    {
                        Name = $"{type} ... end",
                        DefaultClosed = false
                    });
                }
            }

            manager.UpdateFoldings(newFoldings, -1);
        }
    }

    #endregion

    #region Auto Completion

    private CompletionWindow? _completionWindow;
    private OverloadInsightWindow? _insightWindow;

    private static readonly Dictionary<string, string[]> FunctionSignatures = new()
    {
        ["sleep"] = new[] { "sleep(ms)" },
        ["click"] = new[] { "click(x, y)", "click(x, y, useBackend=1)", "click(x, y, useMouse=1)" },
        ["rightClick"] = new[] { "rightClick(x, y)", "rightClick(x, y, useBackend=1)" },
        ["doubleClick"] = new[] { "doubleClick(x, y)", "doubleClick(x, y, useBackend=1)" },
        ["moveTo"] = new[] { "moveTo(x, y)" },
        ["drag"] = new[] { "drag(x1, y1, x2, y2)" },
        ["getMousePos"] = new[] { "getMousePos()" },
        ["clickByTitle"] = new[] { "clickByTitle(\"title\", x, y)", "clickByTitle(\"title\", x, y, useBackend=1)" },
        ["input"] = new[] { "input(\"text\")" },
        ["keyPress"] = new[] { "keyPress(\"key\")", "keyPress(\"Ctrl+A\")" },
        ["keyDown"] = new[] { "keyDown(\"key\")" },
        ["keyUp"] = new[] { "keyUp(\"key\")" },
        ["clickLabel"] = new[] { "clickLabel(\"label\")" },
        ["activate"] = new[] { "activate(\"title\")" },
        ["activateWindow"] = new[] { "activateWindow(\"title\")" },
        ["findWindow"] = new[] { "findWindow(varName, \"title\")" },
        ["closeWindow"] = new[] { "closeWindow(\"title\")" },
        ["minimizeWindow"] = new[] { "minimizeWindow(\"title\")" },
        ["maximizeWindow"] = new[] { "maximizeWindow(\"title\")" },
        ["restoreWindow"] = new[] { "restoreWindow(\"title\")" },
        ["waitFor"] = new[] { "waitFor(\"title\", timeout=30)" },
        ["scanWindow"] = new[] { "scanWindow(\"title\")" },
        ["scan"] = new[] { "scan()" },
        ["show"] = new[] { "show()" },
        ["hide"] = new[] { "hide()" },
        ["getWindows"] = new[] { "getWindows()" },
        ["screenshot"] = new[] { "screenshot()", "screenshot(\"filename.png\")" },
        ["getScreenSize"] = new[] { "getScreenSize(varName)" },
        ["rand"] = new[] { "rand(min, max)", "rand(max)" },
        ["random"] = new[] { "random(min, max)" },
        ["log"] = new[] { "log(\"message\")" },
        ["msg"] = new[] { "msg(\"message\")" },
        ["sqrt"] = new[] { "sqrt(x)" },
        ["pow"] = new[] { "pow(x, y)" },
        ["min"] = new[] { "min(a, b)" },
        ["max"] = new[] { "max(a, b)" },
        ["abs"] = new[] { "abs(x)" },
        ["len"] = new[] { "len(varName, expression)" },
        ["beep"] = new[] { "beep()", "beep(freq, duration)" },
        ["getTimestamp"] = new[] { "getTimestamp(varName)" },
        ["getVersion"] = new[] { "getVersion(varName)" },
        ["exit"] = new[] { "exit()" },
        ["loop"] = new[] { "loop(count)" },
        ["while"] = new[] { "while condition" },
        ["for"] = new[] { "for i = start to end", "for i = start to end step n" },
        ["foreach"] = new[] { "foreach item in array" },
        ["if"] = new[] { "if condition" },
        ["function"] = new[] { "function name(params)" },
        ["var"] = new[] { "var name = value", "var name: type = value" },
        ["array"] = new[] { "array name = [items]" },
    };

    private static readonly string[] Keywords = new[]
    {
        "sleep", "click", "rightClick", "doubleClick", "moveTo", "drag", "getMousePos",
        "clickByTitle", "input", "keyPress", "keyDown", "keyUp", "clickLabel",
        "activate", "activateWindow", "findWindow", "closeWindow", "minimizeWindow",
        "maximizeWindow", "restoreWindow", "waitFor", "scanWindow", "scan", "show", "hide",
        "getWindows", "screenshot", "getScreenSize", "rand", "random", "log", "msg",
        "sqrt", "pow", "min", "max", "abs", "len", "beep", "getTimestamp", "getVersion", "exit",
        "if", "else", "elseif", "endif", "while", "endwhile", "for", "endloop", "loop",
        "function", "endfunction", "var", "array", "goto", "foreach", "return", "break", "continue"
    };

    private void InitializeAutoCompletion()
    {
        Editor.TextArea.TextEntered += TextArea_TextEntered;
        Editor.TextArea.TextEntering += TextArea_TextEntering;
        Editor.TextArea.PreviewKeyDown += TextArea_PreviewKeyDown;
    }

    private void TextArea_PreviewKeyDown(object? sender, System.Windows.Input.KeyEventArgs e)
    {
        if (_completionWindow != null && _completionWindow.IsVisible)
        {
            if (e.Key == Key.Tab)
            {
                if (_completionWindow.CompletionList.SelectedItem != null)
                {
                    _completionWindow.CompletionList.RequestInsertion(e);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Enter)
            {
                if (_completionWindow.CompletionList.SelectedItem != null)
                {
                    _completionWindow.CompletionList.RequestInsertion(e);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Escape)
            {
                _completionWindow.Close();
                e.Handled = true;
            }
            else if (e.Key == Key.Up || e.Key == Key.Down)
            {
                e.Handled = false;
            }
        }
    }

    private void TextArea_TextEntered(object? sender, TextCompositionEventArgs e)
    {
        if (e.Text == "(")
        {
            var offset = Editor.CaretOffset;
            Editor.Document.Insert(offset, ")");
            Editor.CaretOffset = offset;
            ShowInsightWindow();
        }
        else if (e.Text == "\"")
        {
            var offset = Editor.CaretOffset;
            Editor.Document.Insert(offset, "\"");
            Editor.CaretOffset = offset;
        }
        else if (e.Text == "[")
        {
            var offset = Editor.CaretOffset;
            Editor.Document.Insert(offset, "]");
            Editor.CaretOffset = offset;
        }
        else if (char.IsLetter(e.Text[0]) || char.IsDigit(e.Text[0]) || e.Text == "_")
        {
            if (_completionWindow != null && _completionWindow.IsVisible)
            {
                UpdateCompletionWindow();
            }
            else
            {
                ShowCompletionWindow();
            }
        }
    }

    private void TextArea_TextEntering(object? sender, TextCompositionEventArgs e)
    {
        if (e.Text.Length > 0 && _completionWindow != null && _completionWindow.IsVisible)
        {
            if (!char.IsLetterOrDigit(e.Text[0]) && e.Text != "_")
            {
                _completionWindow.Close();
            }
        }
    }

    private void UpdateCompletionWindow()
    {
        if (_completionWindow == null || !_completionWindow.IsVisible) return;

        var line = Editor.Document.GetLineByOffset(Editor.CaretOffset);
        var lineText = Editor.Document.GetText(line);
        var caretPos = Editor.CaretOffset - line.Offset;
        
        var startPos = caretPos;
        while (startPos > 0 && (char.IsLetterOrDigit(lineText[startPos - 1]) || lineText[startPos - 1] == '_'))
        {
            startPos--;
        }
        var prefix = lineText.Substring(startPos, caretPos - startPos).ToLowerInvariant();
        
        _completionWindow.StartOffset = line.Offset + startPos;
        _completionWindow.EndOffset = Editor.CaretOffset;
        
        var matches = Keywords.Where(k => k.ToLowerInvariant().StartsWith(prefix)).ToList();
        
        var data = _completionWindow.CompletionList.CompletionData;
        data.Clear();
        
        foreach (var keyword in matches)
        {
            var description = GetKeywordDescription(keyword);
            data.Add(new VmaCompletionData(keyword, description, prefix.Length));
        }
        
        if (data.Count == 0)
        {
            _completionWindow.Close();
        }
        else if (data.Count > 0)
        {
            _completionWindow.CompletionList.SelectedItem = data[0];
        }
    }

    private void ShowCompletionWindow()
    {
        var line = Editor.Document.GetLineByOffset(Editor.CaretOffset);
        var lineText = Editor.Document.GetText(line);
        var caretPos = Editor.CaretOffset - line.Offset;
        
        var startPos = caretPos;
        while (startPos > 0 && (char.IsLetterOrDigit(lineText[startPos - 1]) || lineText[startPos - 1] == '_'))
        {
            startPos--;
        }
        var prefix = lineText.Substring(startPos, caretPos - startPos).ToLowerInvariant();
        var prefixLength = caretPos - startPos;
        
        if (string.IsNullOrEmpty(prefix) || prefix.Length < 1)
        {
            return;
        }
        
        var matches = Keywords.Where(k => k.ToLowerInvariant().StartsWith(prefix)).ToList();
        if (!matches.Any())
        {
            return;
        }
        
        var wordStartOffset = line.Offset + startPos;
        
        _completionWindow = new CompletionWindow(Editor.TextArea);
        _completionWindow.StartOffset = wordStartOffset;
        _completionWindow.EndOffset = Editor.CaretOffset;
        
        _completionWindow.Background = new SolidColorBrush(MediaColor.FromRgb(0x1a, 0x1a, 0x2e));
        _completionWindow.Foreground = new SolidColorBrush(MediaColor.FromRgb(0xe0, 0xe0, 0xe0));
        _completionWindow.BorderBrush = new SolidColorBrush(MediaColor.FromRgb(0x00, 0xd4, 0xff));
        _completionWindow.BorderThickness = new Thickness(1);
        
        _completionWindow.CompletionList.ListBox.Background = new SolidColorBrush(MediaColor.FromRgb(0x1a, 0x1a, 0x2e));
        _completionWindow.CompletionList.ListBox.BorderThickness = new Thickness(0);
        
        var data = _completionWindow.CompletionList.CompletionData;
        
        foreach (var keyword in matches)
        {
            var description = GetKeywordDescription(keyword);
            data.Add(new VmaCompletionData(keyword, description, prefixLength));
        }
        
        _completionWindow.Show();
        
        if (data.Count > 0)
        {
            _completionWindow.CompletionList.SelectedItem = data[0];
        }
        
        _completionWindow.Closed += (s, e) => _completionWindow = null;
    }

    private void ShowInsightWindow()
    {
        var line = Editor.Document.GetLineByOffset(Editor.CaretOffset);
        var lineText = Editor.Document.GetText(line);
        
        var match = Regex.Match(lineText, @"(\w+)\s*\(");
        if (match.Success)
        {
            var funcName = match.Groups[1].Value.ToLowerInvariant();
            if (FunctionSignatures.TryGetValue(funcName, out var signatures))
            {
                _insightWindow = new OverloadInsightWindow(Editor.TextArea);
                _insightWindow.Provider = new VmaOverloadProvider(signatures);
                _insightWindow.Background = new SolidColorBrush(MediaColor.FromRgb(0x1a, 0x1a, 0x2e));
                _insightWindow.Foreground = new SolidColorBrush(MediaColor.FromRgb(0xe0, 0xe0, 0xe0));
                _insightWindow.BorderBrush = new SolidColorBrush(MediaColor.FromRgb(0x00, 0xd4, 0xff));
                _insightWindow.Show();
                _insightWindow.Closed += (s, e) => _insightWindow = null;
            }
        }
    }

    private static string GetKeywordDescription(string keyword)
    {
        return keyword.ToLowerInvariant() switch
        {
            "sleep" => "延时等待 - sleep(ms)",
            "click" => "鼠标左键点击 - click(x, y)",
            "rightClick" => "鼠标右键点击 - rightClick(x, y)",
            "doubleClick" => "鼠标双击 - doubleClick(x, y)",
            "moveTo" => "移动鼠标 - moveTo(x, y)",
            "drag" => "拖拽操作 - drag(x1, y1, x2, y2)",
            "input" => "输入文本 - input(\"text\")",
            "keyPress" => "按键 - keyPress(\"key\")",
            "loop" => "循环 - loop(count)",
            "while" => "条件循环 - while condition",
            "for" => "计数循环 - for i = start to end",
            "if" => "条件判断 - if condition",
            "function" => "定义函数 - function name(params)",
            "var" => "定义变量 - var name = value",
            "array" => "定义数组 - array name = [items]",
            "scan" => "扫描当前窗口控件",
            "scanWindow" => "扫描指定窗口 - scanWindow(\"title\")",
            "clickLabel" => "点击标签 - clickLabel(\"label\")",
            "activate" => "激活窗口 - activate(\"title\")",
            "log" => "输出日志 - log(\"message\")",
            "msg" => "弹窗提示 - msg(\"message\")",
            "screenshot" => "屏幕截图 - screenshot()",
            _ => $"VMA 命令: {keyword}"
        };
    }

    private class VmaCompletionData : ICompletionData
    {
        private readonly int _prefixLength;

        public VmaCompletionData(string text, string description, int prefixLength)
        {
            Text = text;
            Description = description;
            _prefixLength = prefixLength;
        }

        public ImageSource? Image => null;
        public string Text { get; }
        public object Content => Text;
        public object Description { get; }
        public double Priority => 0;

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            var startOffset = completionSegment.Offset;
            var endOffset = completionSegment.EndOffset;
            textArea.Document.Replace(startOffset, endOffset - startOffset, Text);
        }
    }

    private class VmaOverloadProvider : IOverloadProvider
    {
        private readonly string[] _overloads;
        private int _selectedIndex;

        public VmaOverloadProvider(string[] overloads)
        {
            _overloads = overloads;
            _selectedIndex = 0;
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                _selectedIndex = value;
                OnPropertyChanged(nameof(CurrentHeader));
                OnPropertyChanged(nameof(CurrentContent));
            }
        }

        public int Count => _overloads.Length;
        public string CurrentIndexText => $"({_selectedIndex + 1}/{_overloads.Length})";
        public object CurrentHeader => _overloads[_selectedIndex];
        public object CurrentContent => "";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    #endregion

    #region Syntax Highlighting

    private void InitializeSyntaxHighlighting()
    {
        var xshd = @"<?xml version=""1.0""?>
<SyntaxDefinition name=""VMA"" xmlns=""http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008"">
    <Color name=""Keyword"" foreground=""#569cd6"" fontWeight=""bold""/>
    <Color name=""ControlFlow"" foreground=""#c586c0"" fontWeight=""bold""/>
    <Color name=""Function"" foreground=""#dcdcaa""/>
    <Color name=""String"" foreground=""#ce9178""/>
    <Color name=""Number"" foreground=""#b5cea8""/>
    <Color name=""Comment"" foreground=""#6a9955"" fontStyle=""italic""/>
    <Color name=""Operator"" foreground=""#d4d4d4""/>
    <Color name=""Variable"" foreground=""#9cdcfe""/>
    <Color name=""Label"" foreground=""#4ec9b0""/>
    <Color name=""Boolean"" foreground=""#569cd6""/>
    <Color name=""Builtin"" foreground=""#4fc1ff""/>

    <RuleSet>
        <Span color=""Comment"" begin=""//"" />
        <Span color=""Comment"" multiline=""true"" begin=""/\*"" end=""\*/"" />

        <Span color=""String"">
            <Begin>""""</Begin>
            <End>""""</End>
            <RuleSet>
                <Span begin=""\\"" end=""."" />
            </RuleSet>
        </Span>
        
        <Span color=""String"">
            <Begin>''</Begin>
            <End>''</End>
        </Span>

        <Rule color=""Number"">
            \b0[xX][0-9a-fA-F]+|(\b\d+(\.\d*)?|\.\d+)([eE][+-]?\d+)?
        </Rule>

        <Keywords color=""ControlFlow"">
            <Word>if</Word>
            <Word>else</Word>
            <Word>elseif</Word>
            <Word>endif</Word>
            <Word>end</Word>
            <Word>while</Word>
            <Word>endwhile</Word>
            <Word>for</Word>
            <Word>foreach</Word>
            <Word>in</Word>
            <Word>to</Word>
            <Word>step</Word>
            <Word>endloop</Word>
            <Word>loop</Word>
            <Word>break</Word>
            <Word>continue</Word>
            <Word>return</Word>
            <Word>goto</Word>
        </Keywords>

        <Keywords color=""Keyword"">
            <Word>function</Word>
            <Word>endfunction</Word>
            <Word>var</Word>
            <Word>array</Word>
        </Keywords>

        <Keywords color=""Boolean"">
            <Word>true</Word>
            <Word>false</Word>
            <Word>null</Word>
        </Keywords>

        <Keywords color=""Function"">
            <Word>sleep</Word>
            <Word>click</Word>
            <Word>rightClick</Word>
            <Word>doubleClick</Word>
            <Word>moveTo</Word>
            <Word>drag</Word>
            <Word>getMousePos</Word>
            <Word>clickByTitle</Word>
            <Word>input</Word>
            <Word>keyPress</Word>
            <Word>keyDown</Word>
            <Word>keyUp</Word>
            <Word>clickLabel</Word>
            <Word>clickTag</Word>
            <Word>activate</Word>
            <Word>activateWindow</Word>
            <Word>findWindow</Word>
            <Word>closeWindow</Word>
            <Word>minimizeWindow</Word>
            <Word>maximizeWindow</Word>
            <Word>restoreWindow</Word>
            <Word>waitFor</Word>
            <Word>scanWindow</Word>
            <Word>scan</Word>
            <Word>show</Word>
            <Word>hide</Word>
            <Word>getWindows</Word>
            <Word>screenshot</Word>
            <Word>getScreenSize</Word>
            <Word>getControlByTag</Word>
            <Word>getControlList</Word>
            <Word>getWindowList</Word>
        </Keywords>

        <Keywords color=""Builtin"">
            <Word>rand</Word>
            <Word>random</Word>
            <Word>log</Word>
            <Word>msg</Word>
            <Word>sqrt</Word>
            <Word>pow</Word>
            <Word>min</Word>
            <Word>max</Word>
            <Word>abs</Word>
            <Word>len</Word>
            <Word>substr</Word>
            <Word>split</Word>
            <Word>beep</Word>
            <Word>getTimestamp</Word>
            <Word>getVersion</Word>
            <Word>type</Word>
            <Word>isArray</Word>
            <Word>length</Word>
            <Word>toInt</Word>
            <Word>toString</Word>
            <Word>floor</Word>
            <Word>ceil</Word>
            <Word>round</Word>
            <Word>exit</Word>
            <Word>scroll</Word>
            <Word>key</Word>
            <Word>type</Word>
        </Keywords>

        <Rule color=""Operator"">
            [-+*/^=&lt;&gt;!&amp;|]+|(&amp;amp;&amp;amp;)|(\|\|)
        </Rule>

        <Rule color=""Label"">
            ^\s*\w+:
        </Rule>
    </RuleSet>
</SyntaxDefinition>";

        using var reader = new StringReader(xshd);
        using var xmlReader = new XmlTextReader(reader);
        var highlighting = HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
        HighlightingManager.Instance.RegisterHighlighting("VMA", new[] { ".vma" }, highlighting);
        
        Editor.SyntaxHighlighting = highlighting;
    }

    #endregion

    #region Editor Options

    private void InitializeEditorOptions()
    {
        Editor.Options.EnableVirtualSpace = false;
        Editor.Options.WordWrapIndentation = 4;
        Editor.Options.InheritWordWrapIndentation = true;
        Editor.Options.ShowSpaces = false;
        Editor.Options.ShowTabs = false;
        Editor.Options.ShowEndOfLine = false;
        Editor.Options.HighlightCurrentLine = true;
        Editor.Options.EnableRectangularSelection = true;
        Editor.Options.EnableTextDragDrop = true;
        Editor.Options.EnableHyperlinks = false;
        Editor.Options.CutCopyWholeLine = true;
        Editor.Options.ConvertTabsToSpaces = true;
        Editor.Options.IndentationSize = 4;
    }

    #endregion

    #region Function ComboBox

    private void InitializeFunctionComboBox()
    {
        var functions = new[]
        {
            "-- 插入函数模板 --",
            "",
            "【延时】 sleep(1000)",
            "【延时】 sleep(500)",
            "【延时】 sleep(100)",
            "",
            "【鼠标】 click(x, y)",
            "【鼠标】 click(x, y, useBackend=1)",
            "【鼠标】 click(x, y, useBackend=1, useMouse=1)",
            "【鼠标】 rightClick(x, y)",
            "【鼠标】 rightClick(x, y, useBackend=1)",
            "【鼠标】 doubleClick(x, y)",
            "【鼠标】 doubleClick(x, y, useBackend=1)",
            "【鼠标】 moveTo(x, y)",
            "【鼠标】 drag(x1, y1, x2, y2)",
            "【鼠标】 getMousePos()",
            "【鼠标】 scroll(direction)",
            "",
            "【窗口点击】 clickByTitle(\"title\", x, y)",
            "【窗口点击】 clickByTitle(\"title\", x, y, useBackend=1)",
            "【窗口点击】 clickByTitle(\"title\", x, y, useBackend=1, useMouse=1)",
            "【窗口点击-右键】 clickByTitle(\"title\", x, y, rightClick=1)",
            "【窗口点击-右键】 clickByTitle(\"title\", x, y, useBackend=1, rightClick=1)",
            "【窗口点击-双击】 clickByTitle(\"title\", x, y, doubleClick=1)",
            "【窗口点击-双击】 clickByTitle(\"title\", x, y, useBackend=1, doubleClick=1)",
            "【窗口点击-后台右键】 clickByTitle(\"title\", x, y, useBackend=1, rightClick=1)",
            "【窗口点击-后台双击】 clickByTitle(\"title\", x, y, useBackend=1, doubleClick=1)",
            "【窗口点击-前台右键】 clickByTitle(\"title\", x, y, rightClick=1)",
            "【窗口点击-前台双击】 clickByTitle(\"title\", x, y, doubleClick=1)",
            "",
            "【键盘】 input(\"text\")",
            "【键盘】 keyPress(\"Enter\")",
            "【键盘】 keyPress(\"Ctrl+A\")",
            "【键盘】 keyPress(\"Ctrl+C\")",
            "【键盘】 keyPress(\"Ctrl+V\")",
            "【键盘】 keyPress(\"Alt+F4\")",
            "【键盘】 keyDown(\"Shift\")",
            "【键盘】 keyUp(\"Shift\")",
            "【键盘】 type(\"text\")",
            "【键盘】 key(\"key\")",
            "",
            "【标签】 scan()",
            "【标签】 scanWindow(\"title\")",
            "【标签】 clickLabel(\"A\")",
            "【标签】 clickTag(\"tag\")",
            "【标签】 show()",
            "【标签】 hide()",
            "",
            "【窗口】 activate(\"title\")",
            "【窗口】 activateWindow(\"title\")",
            "【窗口】 findWindow(varName, \"title\")",
            "【窗口】 closeWindow(\"title\")",
            "【窗口】 minimizeWindow(\"title\")",
            "【窗口】 maximizeWindow(\"title\")",
            "【窗口】 restoreWindow(\"title\")",
            "【窗口】 waitFor(\"title\", 30)",
            "【窗口】 waitFor(\"title\", 60)",
            "【窗口】 getWindows()",
            "",
            "【工具】 log(\"message\")",
            "【工具】 msg(\"message\")",
            "【工具】 screenshot()",
            "【工具】 screenshot(\"filename.png\")",
            "【工具】 getScreenSize(varName)",
            "【工具】 rand(1, 100)",
            "【工具】 random(1, 100)",
            "【工具】 randomFloat(0.0, 1.0)",
            "【工具】 beep()",
            "【工具】 beep(freq, duration)",
            "【工具】 getTimestamp(varName)",
            "【工具】 getVersion(varName)",
            "【工具】 exit()",
            "",
            "【数学】 abs(x)",
            "【数学】 floor(x)",
            "【数学】 ceil(x)",
            "【数学】 round(x)",
            "【数学】 sqrt(x)",
            "【数学】 pow(x, y)",
            "【数学】 min(a, b)",
            "【数学】 max(a, b)",
            "",
            "【数组】 array name = [1, 2, 3]",
            "【数组】 len(varName, array)",
            "",
            "【流程】 loop(3) ... endloop",
            "【流程】 for i = 1 to 10 ... endloop",
            "【流程】 for i = 1 to 10 step 2 ... endloop",
            "【流程】 foreach item in array ... endloop",
            "【流程】 while condition ... endwhile",
            "【流程】 if condition ... endif",
            "【流程】 if condition ... else ... endif",
            "【流程】 function name() ... endfunction",
            "【流程】 function name(param1, param2) ... endfunction",
            "【流程】 return value",
            "【流程】 break",
            "【流程】 continue",
            "【流程】 goto(\"label\")",
            "【流程】 label:",
        };

        FunctionComboBox.ItemsSource = functions;
        FunctionComboBox.SelectedIndex = 0;
    }

    private void FunctionComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (FunctionComboBox.SelectedIndex <= 0) return;
        
        var selectedItem = FunctionComboBox.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(selectedItem) || string.IsNullOrWhiteSpace(selectedItem))
        {
            FunctionComboBox.SelectedIndex = 0;
            return;
        }
        
        var functionCode = selectedItem;
        
        if (selectedItem.Contains("】 "))
        {
            functionCode = selectedItem.Substring(selectedItem.IndexOf("】 ") + 2);
        }
        
        if (functionCode.Contains(" ... "))
        {
            var parts = functionCode.Split(new[] { " ... " }, StringSplitOptions.None);
            if (parts.Length == 2)
            {
                var indent = Editor.Options.ConvertTabsToSpaces 
                    ? new string(' ', Editor.Options.IndentationSize) 
                    : "\t";
                functionCode = parts[0] + "\n" + indent + "\n" + parts[1];
            }
        }
        
        var caretOffset = Editor.CaretOffset;
        var text = Editor.Document.Text;
        
        if (caretOffset > 0 && text.Length > 0 && caretOffset <= text.Length)
        {
            var lastNewLine = text.LastIndexOf('\n', Math.Min(caretOffset - 1, text.Length - 1));
            var currentLineStart = lastNewLine == -1 ? 0 : lastNewLine + 1;
            var currentLine = text.Substring(currentLineStart, Math.Min(caretOffset - currentLineStart, text.Length - currentLineStart)).Trim();
            
            if (!string.IsNullOrEmpty(currentLine))
            {
                functionCode = "\n" + functionCode;
            }
        }
        
        Editor.Document.Insert(caretOffset, functionCode);
        Editor.CaretOffset = caretOffset + functionCode.Length;
        
        FunctionComboBox.SelectedIndex = 0;
        Editor.Focus();
    }

    #endregion

    #region Button Handlers

    private void BtnNew_Click(object sender, RoutedEventArgs e)
    {
        Editor.Clear();
        _currentFile = null;
        FileNameText.Text = "未命名.vma";
        StatusText.Text = "就绪";
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
            Editor.ScrollToHome();
            FileNameText.Text = Path.GetFileName(_currentFile);
            StatusText.Text = "就绪";
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
        File.WriteAllText(_currentFile, Editor.Document.Text);
        FileNameText.Text = Path.GetFileName(_currentFile);
        StatusText.Text = "已保存";
        Output.AppendText($"\n[保存] {_currentFile}");
    }

    private async void BtnRun_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning)
        {
            _vmaEngine?.Stop();
            Output.AppendText("\n[停止] 用户中断");
            return;
        }

        var script = Editor.Document.Text;
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
        }
    }

    private void BtnClearOutput_Click(object sender, RoutedEventArgs e)
    {
        Output.Text = "等待脚本执行...";
        OutputStats.Text = "";
    }

    private void Editor_TextChanged(object? sender, EventArgs e)
    {
        if (_currentFile != null && !_isRunning)
        {
            FileNameText.Text = Path.GetFileName(_currentFile) + " *";
        }
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
            var compilerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "vma-compiler.exe");
            if (!File.Exists(compilerPath))
            {
                Output.Text = "错误: 编译器未找到，请确保 vma-compiler.exe 在 tools 目录中";
                return;
            }

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

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
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
        WindowState = WindowState == WindowState.Maximized 
            ? WindowState.Normal 
            : WindowState.Maximized;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    #endregion
}
