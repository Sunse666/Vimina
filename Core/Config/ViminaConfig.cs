namespace Vimina.Core.Config;

public class ViminaConfig
{
    public string BackgroundColor_Default { get; set; } = "0x00DDFF";
    public string BackgroundColor_Match { get; set; } = "0x00FF00";
    public string BackgroundColor_Prefix { get; set; } = "0x00A5FF";
    public string BackgroundColor_Invalid { get; set; } = "0x808080";
    public string TextColor { get; set; } = "0x000000";
    public int FontSize { get; set; } = 12;
    public int FontWeight { get; set; } = 700;
    public int OffsetX { get; set; } = 0;
    public int OffsetY { get; set; } = 18;

    public int MinWidth { get; set; } = 8;
    public int MinHeight { get; set; } = 8;
    public int MaxDepth { get; set; } = 50;

    public int ClickDelay { get; set; } = 30;

    public bool UseMouseClick { get; set; } = false;
    public bool BringToFront { get; set; } = true;
    public bool UseFlaUIClick { get; set; } = true;
}
