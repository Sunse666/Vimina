namespace Vimina.Core.Automation;

public static class ControlTypeInfo
{
    // FlaUI C# ControlType enum values (0-40)
    // These are the actual enum values from FlaUI.Core.Definitions.ControlType
    public const int Unknown = 0;
    public const int Custom = 1;
    public const int Button = 2;
    public const int Calendar = 3;
    public const int CheckBox = 4;
    public const int ComboBox = 5;
    public const int DataGrid = 6;
    public const int DataItem = 7;
    public const int Document = 8;
    public const int Edit = 9;
    public const int Group = 10;
    public const int Header = 11;
    public const int HeaderItem = 12;
    public const int Hyperlink = 13;
    public const int Image = 14;
    public const int List = 15;
    public const int ListItem = 16;
    public const int MenuBar = 17;
    public const int Menu = 18;
    public const int MenuItem = 19;
    public const int Pane = 20;
    public const int ProgressBar = 21;
    public const int RadioButton = 22;
    public const int ScrollBar = 23;
    public const int SemanticZoom = 24;
    public const int Separator = 25;
    public const int Slider = 26;
    public const int Spinner = 27;
    public const int SplitButton = 28;
    public const int StatusBar = 29;
    public const int Tab = 30;
    public const int TabItem = 31;
    public const int Table = 32;
    public const int Text = 33;
    public const int Thumb = 34;
    public const int TitleBar = 35;
    public const int ToolBar = 36;
    public const int ToolTip = 37;
    public const int Tree = 38;
    public const int TreeItem = 39;
    public const int Window = 40;

    // Interactive control types based on FlaUI C# enum values
    public static readonly Dictionary<int, ControlTypeData> InteractiveTypes = new()
    {
        [Button] = new("Button", "按钮", "点击触发操作"),
        [CheckBox] = new("CheckBox", "复选框", "点击切换选中状态"),
        [ComboBox] = new("ComboBox", "下拉选择框", "点击展开选项列表"),
        [Document] = new("Document", "文档", "点击查看文档"),
        [Edit] = new("Edit", "文本输入框", "点击后可输入文本"),
        [Hyperlink] = new("Hyperlink", "超链接", "点击打开链接"),
        [ListItem] = new("ListItem", "列表项", "点击选中此项"),
        [MenuItem] = new("MenuItem", "菜单项", "点击执行菜单命令"),
        [RadioButton] = new("RadioButton", "单选按钮", "点击选中"),
        [Slider] = new("Slider", "滑块", "点击或拖动调整值"),
        [Spinner] = new("Spinner", "数值调节器", "点击增减数值"),
        [SplitButton] = new("SplitButton", "分隔按钮", "点击执行或展开更多"),
        [TabItem] = new("TabItem", "标签页", "点击切换到此标签页"),
        [ToolBar] = new("ToolBar", "工具栏", "包含多个工具按钮"),
        [TreeItem] = new("TreeItem", "树节点", "点击展开/选中节点"),
        // Additional types for file explorer and VSCode support
        [DataItem] = new("DataItem", "数据项", "点击选中文件/文件夹"),
        [List] = new("List", "列表", "点击列表"),
        [Pane] = new("Pane", "面板", "点击面板区域"),
        // Additional types for browser support
        [Text] = new("Text", "文本", "点击文本"),
        [Tree] = new("Tree", "树形结构", "点击树形结构"),
    };

    public static readonly Dictionary<int, ControlTypeData> AllTypes = new()
    {
        [Unknown] = new("Unknown", "未知控件"),
        [Custom] = new("Custom", "自定义控件"),
        [Button] = new("Button", "按钮"),
        [Calendar] = new("Calendar", "日历"),
        [CheckBox] = new("CheckBox", "复选框"),
        [ComboBox] = new("ComboBox", "下拉选择框"),
        [DataGrid] = new("DataGrid", "数据网格"),
        [DataItem] = new("DataItem", "数据项"),
        [Document] = new("Document", "文档"),
        [Edit] = new("Edit", "文本输入框"),
        [Group] = new("Group", "分组"),
        [Header] = new("Header", "标题栏"),
        [HeaderItem] = new("HeaderItem", "标题项"),
        [Hyperlink] = new("Hyperlink", "超链接"),
        [Image] = new("Image", "图片"),
        [List] = new("List", "列表"),
        [ListItem] = new("ListItem", "列表项"),
        [MenuBar] = new("MenuBar", "菜单栏"),
        [Menu] = new("Menu", "菜单"),
        [MenuItem] = new("MenuItem", "菜单项"),
        [Pane] = new("Pane", "面板"),
        [ProgressBar] = new("ProgressBar", "进度条"),
        [RadioButton] = new("RadioButton", "单选按钮"),
        [ScrollBar] = new("ScrollBar", "滚动条"),
        [SemanticZoom] = new("SemanticZoom", "语义缩放"),
        [Separator] = new("Separator", "分隔符"),
        [Slider] = new("Slider", "滑块"),
        [Spinner] = new("Spinner", "数值调节器"),
        [SplitButton] = new("SplitButton", "分隔按钮"),
        [StatusBar] = new("StatusBar", "状态栏"),
        [Tab] = new("Tab", "标签页容器"),
        [TabItem] = new("TabItem", "标签页"),
        [Table] = new("Table", "表格"),
        [Text] = new("Text", "文本"),
        [Thumb] = new("Thumb", "缩略图"),
        [TitleBar] = new("TitleBar", "标题栏"),
        [ToolBar] = new("ToolBar", "工具栏"),
        [ToolTip] = new("ToolTip", "工具提示"),
        [Tree] = new("Tree", "树形结构"),
        [TreeItem] = new("TreeItem", "树节点"),
        [Window] = new("Window", "窗口"),
    };

    // UIA Control Type IDs (50000+ range) - used by browsers and some applications
    // These are the actual UIA standard values, different from FlaUI's simplified enum
    public const int UIA_Button = 50000;
    public const int UIA_Calendar = 50001;
    public const int UIA_CheckBox = 50002;
    public const int UIA_ComboBox = 50003;
    public const int UIA_Edit = 50004;
    public const int UIA_Hyperlink = 50005;
    public const int UIA_Image = 50006;
    public const int UIA_ListItem = 50007;
    public const int UIA_List = 50008;
    public const int UIA_Menu = 50009;
    public const int UIA_MenuBar = 50010;
    public const int UIA_MenuItem = 50011;
    public const int UIA_ProgressBar = 50012;
    public const int UIA_RadioButton = 50013;
    public const int UIA_ScrollBar = 50014;
    public const int UIA_Slider = 50015;
    public const int UIA_Spinner = 50016;
    public const int UIA_SplitButton = 50017;
    public const int UIA_StatusBar = 50018;
    public const int UIA_Tab = 50019;
    public const int UIA_TabItem = 50020;
    public const int UIA_Text = 50020;  // Note: same as TabItem in some implementations
    public const int UIA_ToolBar = 50021;
    public const int UIA_ToolTip = 50022;
    public const int UIA_Tree = 50023;
    public const int UIA_TreeItem = 50024;
    public const int UIA_Custom = 50025;
    public const int UIA_Group = 50026;
    public const int UIA_Thumb = 50027;
    public const int UIA_DataGrid = 50028;
    public const int UIA_DataItem = 50029;
    public const int UIA_Document = 50030;
    public const int UIA_SplitPane = 50031;
    public const int UIA_Window = 50032;
    public const int UIA_Pane = 50033;
    public const int UIA_Header = 50034;
    public const int UIA_HeaderItem = 50035;
    public const int UIA_Table = 50036;
    public const int UIA_TitleBar = 50037;
    public const int UIA_Separator = 50038;
    public static readonly HashSet<int> InteractiveTypeNums = new()
    {
        // FlaUI simplified enum values (0-40)
        Button,
        CheckBox,
        ComboBox,
        Document,
        Edit,
        Hyperlink,
        ListItem,
        MenuItem,
        RadioButton,
        Slider,
        Spinner,
        SplitButton,
        TabItem,
        ToolBar,
        TreeItem,
        DataItem,
        List,
        Pane,
        Text,
        Tree,
        17,
        40,
        UIA_Button,      // 50000
        UIA_CheckBox,    // 50002
        UIA_ComboBox,    // 50003
        UIA_Edit,        // 50004
        UIA_Hyperlink,   // 50005 - This is what browsers use!
        UIA_ListItem,    // 50007
        UIA_MenuItem,    // 50011 - This is what browsers use!
        UIA_RadioButton, // 50013
        UIA_Slider,      // 50015
        UIA_Spinner,     // 50016
        UIA_SplitButton, // 50017
        UIA_TabItem,     // 50020
        UIA_ToolBar,     // 50021
        UIA_TreeItem,    // 50024
        UIA_DataItem,    // 50029 - File explorer items
        UIA_List,        // 50008 - List containers
        UIA_Pane,        // 50033 - Panel areas
        14,
        20,
    };
}

public record ControlTypeData(string Name, string Description, string? ActionHint = null);
