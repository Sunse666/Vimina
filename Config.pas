unit Config;

{$mode objfpc}{$H+}
{$codepage UTF8}

interface

uses
  Classes, SysUtils, Graphics, fpjson, jsonparser, jsonscanner, Windows;

type
  TLabelConfig = record
    BackgroundColor_Default: TColor;
    BackgroundColor_Match: TColor;
    BackgroundColor_Prefix: TColor;
    BackgroundColor_Invalid: TColor;
    TextColor: TColor;
    FontSize: Integer;
    FontWeight: Integer;
    OffsetX: Integer;
    OffsetY: Integer;
  end;

  TFilterConfig = record
    MinWidth: Integer;
    MinHeight: Integer;
    MaxDepth: Integer;
  end;

  TPerformanceConfig = record
    ClickDelay: Integer;
  end;

  TClickModeConfig = record
    UseMouseClick: Boolean;
    BringToFront: Boolean;
    UseBackendClick: Boolean;
  end;

  TViminaConfig = record
    LabelCfg: TLabelConfig;
    FilterCfg: TFilterConfig;
    PerfCfg: TPerformanceConfig;
    ClickModeCfg: TClickModeConfig;
  end;
  PViminaConfig = ^TViminaConfig;

  TControlTypeInfo = record
    Name: string;
    Desc: string;
    Action: string;
  end;

  TControlInfo = record
    Name: string;
    ControlType: string;
    TypeNum: Integer;
    TypeDesc: string;
    ActionHint: string;
    X, Y, Width, Height: Integer;
    CenterX, CenterY: Integer;
    LabelText: string;
    Hwnd: HWND;
  end;

  TWindowInfo = record
    Hwnd: HWND;
    Title: string;
    ClassName: string;
    ProcessId: DWORD;
  end;

const
  PREDEFINED_LABELS: array[0..66] of string = (
    'DJ','DK','DL','SJ','SK','SL','AJ','AK','AL',
    'JD','JK','JL','KD','KJ','KL','LD','LK','LJ',
    'DS','DA','DH','SD','SA','SH','AD','AS','AH',
    'JH','JA','JS','KH','KA','KS','LH','LA','LS',
    'DR','DE','DT','SR','SE','ST','AR','AE','AT',
    'RD','RS','RA','RJ','RK','RL','ED','ES','EA','EJ','EK','EL',
    'TD','TS','TA','TJ','TK','TL','GD','GS','GJ','GK'
  );

function GetDefaultConfig: TViminaConfig;
function LoadConfig(const FileName: string): TViminaConfig;
procedure SaveConfig(const FileName: string; const Cfg: TViminaConfig);
function GetControlTypeInfo(TypeNum: Integer): TControlTypeInfo;
function IsInteractiveControl(TypeNum: Integer): Boolean;
function HexToColor(const HexStr: string): TColor;
function ColorToHex(Color: TColor): string;
procedure WriteJsonFile(const FileName: string; JSON: TJSONData);

implementation

uses
  IniFiles;

function HexToColor(const HexStr: string): TColor;
var
  Hex: string;
  Value: LongInt;
begin
  Hex := HexStr;
  if Pos('0x', LowerCase(Hex)) = 1 then
    Delete(Hex, 1, 2)
  else if Pos('#', Hex) = 1 then
    Delete(Hex, 1, 1);
    
  try
    Value := StrToInt('$' + Hex);
    Result := TColor(Value);
  except
    Result := $00DDFF;
  end;
end;

function ColorToHex(Color: TColor): string;
begin
  Result := '0x' + IntToHex(Color, 6);
end;

procedure WriteJsonFile(const FileName: string; JSON: TJSONData);
var
  FS: TFileStream;
  JSONStr: string;
begin
  JSONStr := JSON.FormatJSON([foSingleLineArray, foSingleLineObject], 2);
  
  FS := TFileStream.Create(FileName, fmCreate);
  try
    FS.WriteBuffer(JSONStr[1], Length(JSONStr));
  finally
    FS.Free;
  end;
end;

function GetDefaultConfig: TViminaConfig;
begin
  with Result.LabelCfg do
  begin
    BackgroundColor_Default := $00DDFF;
    BackgroundColor_Match := $00FF00;
    BackgroundColor_Prefix := $00A5FF;
    BackgroundColor_Invalid := $808080;
    TextColor := clBlack;
    FontSize := 12;
    FontWeight := 700;
    OffsetX := 0;
    OffsetY := 18;
  end;

  with Result.FilterCfg do
  begin
    MinWidth := 8;
    MinHeight := 8;
    MaxDepth := 50;
  end;

  with Result.PerfCfg do
  begin
    ClickDelay := 30;
  end;

  with Result.ClickModeCfg do
  begin
    UseMouseClick := False;
    BringToFront := True;
    UseBackendClick := False;
  end;
end;

function LoadConfig(const FileName: string): TViminaConfig;
var
  JSON: TJSONObject;
  JSONData: TJSONData;
  FS: TFileStream;
  Parser: TJSONParser;
begin
  Result := GetDefaultConfig;
  
  if not FileExists(FileName) then
    Exit;
    
  try
    FS := TFileStream.Create(FileName, fmOpenRead or fmShareDenyWrite);
    try
      Parser := TJSONParser.Create(FS, [joUTF8]);
      try
        JSONData := Parser.Parse;
        if Assigned(JSONData) and (JSONData.JSONType = jtObject) then
        begin
          JSON := TJSONObject(JSONData);
          
          if JSON.Find('BackgroundColor_Default') <> nil then
            Result.LabelCfg.BackgroundColor_Default := HexToColor(JSON.Get('BackgroundColor_Default', '0x00DDFF'));
          if JSON.Find('BackgroundColor_Match') <> nil then
            Result.LabelCfg.BackgroundColor_Match := HexToColor(JSON.Get('BackgroundColor_Match', '0x00FF00'));
          if JSON.Find('BackgroundColor_Prefix') <> nil then
            Result.LabelCfg.BackgroundColor_Prefix := HexToColor(JSON.Get('BackgroundColor_Prefix', '0x00A5FF'));
          if JSON.Find('BackgroundColor_Invalid') <> nil then
            Result.LabelCfg.BackgroundColor_Invalid := HexToColor(JSON.Get('BackgroundColor_Invalid', '0x808080'));
          if JSON.Find('TextColor') <> nil then
            Result.LabelCfg.TextColor := HexToColor(JSON.Get('TextColor', '0x000000'));
            
          Result.LabelCfg.FontSize := JSON.Get('FontSize', 12);
          Result.LabelCfg.FontWeight := JSON.Get('FontWeight', 700);
          Result.LabelCfg.OffsetX := JSON.Get('OffsetX', 0);
          Result.LabelCfg.OffsetY := JSON.Get('OffsetY', 18);
          
          Result.FilterCfg.MinWidth := JSON.Get('MinWidth', 8);
          Result.FilterCfg.MinHeight := JSON.Get('MinHeight', 8);
          Result.FilterCfg.MaxDepth := JSON.Get('MaxDepth', 50);
          
          Result.PerfCfg.ClickDelay := JSON.Get('ClickDelay', 30);
          
          Result.ClickModeCfg.UseMouseClick := JSON.Get('UseMouseClick', False);
          Result.ClickModeCfg.BringToFront := JSON.Get('BringToFront', True);
          Result.ClickModeCfg.UseBackendClick := JSON.Get('UseBackendClick', False);
        end;
        JSONData.Free;
      finally
        Parser.Free;
      end;
    finally
      FS.Free;
    end;
  except
  end;
end;

procedure SaveConfig(const FileName: string; const Cfg: TViminaConfig);
var
  JSON: TJSONObject;
  JSONStr: string;
  FS: TFileStream;
begin
  JSON := TJSONObject.Create;
  try
    JSON.Add('BackgroundColor_Default', ColorToHex(Cfg.LabelCfg.BackgroundColor_Default));
    JSON.Add('BackgroundColor_Match', ColorToHex(Cfg.LabelCfg.BackgroundColor_Match));
    JSON.Add('BackgroundColor_Prefix', ColorToHex(Cfg.LabelCfg.BackgroundColor_Prefix));
    JSON.Add('BackgroundColor_Invalid', ColorToHex(Cfg.LabelCfg.BackgroundColor_Invalid));
    JSON.Add('TextColor', ColorToHex(Cfg.LabelCfg.TextColor));
    JSON.Add('FontSize', Cfg.LabelCfg.FontSize);
    JSON.Add('FontWeight', Cfg.LabelCfg.FontWeight);
    JSON.Add('OffsetX', Cfg.LabelCfg.OffsetX);
    JSON.Add('OffsetY', Cfg.LabelCfg.OffsetY);
    
    JSON.Add('MinWidth', Cfg.FilterCfg.MinWidth);
    JSON.Add('MinHeight', Cfg.FilterCfg.MinHeight);
    JSON.Add('MaxDepth', Cfg.FilterCfg.MaxDepth);
    
    JSON.Add('ClickDelay', Cfg.PerfCfg.ClickDelay);
    
    JSON.Add('UseMouseClick', Cfg.ClickModeCfg.UseMouseClick);
    JSON.Add('BringToFront', Cfg.ClickModeCfg.BringToFront);
    JSON.Add('UseBackendClick', Cfg.ClickModeCfg.UseBackendClick);
    
    JSONStr := JSON.FormatJSON([foSingleLineArray, foSingleLineObject], 2);
  finally
    JSON.Free;
  end;
  
  FS := TFileStream.Create(FileName, fmCreate);
  try
    FS.WriteBuffer(JSONStr[1], Length(JSONStr));
  finally
    FS.Free;
  end;
end;

function GetControlTypeInfo(TypeNum: Integer): TControlTypeInfo;
begin
  case TypeNum of
    2:  begin Result.Name := 'Button'; Result.Desc := '按钮'; Result.Action := '点击触发操作'; end;
    4:  begin Result.Name := 'CheckBox'; Result.Desc := '复选框'; Result.Action := '点击切换选中状态'; end;
    5:  begin Result.Name := 'ComboBox'; Result.Desc := '下拉选择框'; Result.Action := '点击展开选项列表'; end;
    8:  begin Result.Name := 'DataItem'; Result.Desc := '数据项'; Result.Action := '点击选中此项'; end;
    10: begin Result.Name := 'Edit'; Result.Desc := '文本输入框'; Result.Action := '点击后可输入文本'; end;
    14: begin Result.Name := 'Hyperlink'; Result.Desc := '超链接'; Result.Action := '点击打开链接'; end;
    17: begin Result.Name := 'ListItem'; Result.Desc := '列表项'; Result.Action := '点击选中此项'; end;
    20: begin Result.Name := 'MenuItem'; Result.Desc := '菜单项'; Result.Action := '点击执行菜单命令'; end;
    23: begin Result.Name := 'RadioButton'; Result.Desc := '单选按钮'; Result.Action := '点击选中（互斥）'; end;
    27: begin Result.Name := 'Slider'; Result.Desc := '滑块'; Result.Action := '点击或拖动调整值'; end;
    28: begin Result.Name := 'Spinner'; Result.Desc := '数值调节器'; Result.Action := '点击增减数值'; end;
    29: begin Result.Name := 'SplitButton'; Result.Desc := '分隔按钮'; Result.Action := '点击执行或展开更多'; end;
    32: begin Result.Name := 'TabItem'; Result.Desc := '标签页'; Result.Action := '点击切换到此标签页'; end;
    37: begin Result.Name := 'ToolBar'; Result.Desc := '工具栏'; Result.Action := '包含多个工具按钮'; end;
    40: begin Result.Name := 'TreeItem'; Result.Desc := '树节点'; Result.Action := '点击展开/选中节点'; end;
  else
    begin Result.Name := 'Unknown'; Result.Desc := '未知控件'; Result.Action := '尝试点击'; end;
  end;
end;

function IsInteractiveControl(TypeNum: Integer): Boolean;
begin
  Result := TypeNum in [2, 4, 5, 8, 10, 14, 17, 20, 23, 27, 28, 29, 32, 37, 40];
end;

end.
