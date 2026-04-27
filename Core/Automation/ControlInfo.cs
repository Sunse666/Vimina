namespace Vimina.Core.Automation;

public class ControlInfo
{
    public string Label { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public int TypeNum { get; set; }
    public string TypeDesc { get; set; } = "";
    public string ActionHint { get; set; } = "";
    public bool IsInteractive { get; set; }

    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int CenterX => X + Width / 2;
    public int CenterY => Y + Height / 2;

    public string AutomationId { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string HelpText { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
    public bool IsVisible { get; set; } = true;
    public bool IsKeyboardFocusable { get; set; }
    public bool IsOffscreen { get; set; }

    public long Hwnd { get; set; }
}

public class WindowScanResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Hint { get; set; }
    public string Timestamp { get; set; } = DateTime.Now.ToString("O");
    public WindowInfo? Window { get; set; }
    public ScanSummary? Summary { get; set; }
    public List<string>? QuickReference { get; set; }
    public List<ControlGroup>? ControlGroups { get; set; }
    public List<ControlInfo>? Controls { get; set; }
}

public class WindowInfo
{
    public long Handle { get; set; }
    public string Title { get; set; } = "";
    public string ClassName { get; set; } = "";
}

public class ScanSummary
{
    public int TotalControls { get; set; }
    public Dictionary<string, int>? ByType { get; set; }
    public string Description { get; set; } = "";
}

public class ControlGroup
{
    public string TypeName { get; set; } = "";
    public string TypeDescription { get; set; } = "";
    public string? ActionHint { get; set; }
    public List<ControlGroupItem>? Items { get; set; }
}

public class ControlGroupItem
{
    public string Label { get; set; } = "";
    public string Name { get; set; } = "";
    public string Position { get; set; } = "";
}

public class LabelMap : Dictionary<string, LabelPosition> { }

public class LabelPosition
{
    public int CenterX { get; set; }
    public int CenterY { get; set; }
}
