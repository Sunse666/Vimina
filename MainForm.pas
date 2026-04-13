unit MainForm;

{$mode objfpc}{$H+}

interface

uses
  Classes, SysUtils, Forms, Controls, Graphics, Dialogs, StdCtrls, ExtCtrls,
  Menus, LCLType, LCLIntf, Windows, Messages, fpjson,
  Config, UIAutomation, HttpServer, MarkerForm;

type
  { TfrmMain }
  TfrmMain = class(TForm)
    lblInfo1: TLabel;
    lblInfo2: TLabel;
    lblInfo3: TLabel;
    lblApi: TLabel;
    TrayIcon: TTrayIcon;
    PopupMenu1: TPopupMenu;
    miShow: TMenuItem;
    miDataDir: TMenuItem;
    miSep: TMenuItem;
    miExit: TMenuItem;
    procedure FormCreate(Sender: TObject);
    procedure FormDestroy(Sender: TObject);
    procedure FormClose(Sender: TObject; var CloseAction: TCloseAction);
    procedure TrayIconClick(Sender: TObject);
    procedure miShowClick(Sender: TObject);
    procedure miDataDirClick(Sender: TObject);
    procedure miExitClick(Sender: TObject);
  private
    FConfig: TViminaConfig;
    FMarkers: TList;
    FControlList: array of TControlInfo;
    FControlCount: Integer;
    FIsMarkerVisible: Boolean;
    FInputBuffer: string;
    FLabelIndex: Integer;
    FHotKeyId_AltF: ATOM;
    FHotKeyId_Esc: ATOM;
    FHotKeyId_AltR: ATOM;
    FKeyboardHook: HHOOK;
    FHttpServer: TViminaHttpServer;
    FDataDir: string;
    FUIAuto: TUIAutomationHelper;
    
    procedure RegisterHotKeys;
    procedure UnregisterHotKeys;
    procedure InstallKeyboardHook;
    procedure UninstallKeyboardHook;
    function GenerateNextLabel: string;
    procedure ToggleMarkers;
    procedure ClearAllMarkers;
    procedure HighlightMarkers(const Input: string);
    function DoScan: Boolean;
    procedure CreateMarkerWindow(X, Y: Integer; const ALabel: string);
    procedure PerformClick(const Ctrl: TControlInfo);
    procedure DeduplicateControls;
    procedure SaveScanResult;
    procedure HttpClickLabel(const ALabel: string);
    procedure HttpShowMarkers;
    procedure HttpHideMarkers;
  protected
    procedure WMHotKey(var Msg: TMessage); message WM_HOTKEY;
    procedure WndProc(var Message: TMessage); override;
  public
  end;

var
  frmMain: TfrmMain;
  
function KeyboardHookProc(nCode: Integer; wParam: WPARAM; lParam: LPARAM): LRESULT; stdcall;

type
  PKBDLLHOOKSTRUCT = ^TKBDLLHOOKSTRUCT;
  TKBDLLHOOKSTRUCT = record
    vkCode: DWORD;
    scanCode: DWORD;
    flags: DWORD;
    time: DWORD;
    dwExtraInfo: ULONG_PTR;
  end;

const
  WH_KEYBOARD_LL = 13;
  LLKHF_INJECTED = $00000010;

implementation

{$R *.lfm}

uses
  Math, StrUtils;

function KeyboardHookProc(nCode: Integer; wParam: WPARAM; lParam: LPARAM): LRESULT; stdcall;
var
  KeyInfo: PKBDLLHOOKSTRUCT;
  VKCode: DWORD;
  Char: string;
  HasPrefix: Boolean;
  I: Integer;
begin
  Result := CallNextHookEx(0, nCode, wParam, lParam);
  
  if (nCode < 0) or not Assigned(frmMain) or not frmMain.FIsMarkerVisible then
    Exit;
    
  KeyInfo := PKBDLLHOOKSTRUCT(lParam);
  
  // 检查是否是注入的输入
  if (KeyInfo^.flags and LLKHF_INJECTED) <> 0 then
    Exit;
    
  if (wParam = WM_KEYDOWN) or (wParam = WM_SYSKEYDOWN) then
  begin
    VKCode := KeyInfo^.vkCode;
    
    // A-Z 键
    if (VKCode >= Ord('A')) and (VKCode <= Ord('Z')) then
    begin
      Char := Chr(VKCode);
      frmMain.FInputBuffer := frmMain.FInputBuffer + Char;
      frmMain.HighlightMarkers(frmMain.FInputBuffer);
      
      // 检查是否完全匹配
      for I := 0 to frmMain.FControlCount - 1 do
      begin
        if frmMain.FControlList[I].LabelText = frmMain.FInputBuffer then
        begin
          frmMain.PerformClick(frmMain.FControlList[I]);
          frmMain.ClearAllMarkers;
          Result := 1;
          Exit;
        end;
      end;
      
      // 检查是否有前缀匹配
      HasPrefix := False;
      for I := 0 to frmMain.FControlCount - 1 do
      begin
        if Pos(frmMain.FInputBuffer, frmMain.FControlList[I].LabelText) = 1 then
        begin
          HasPrefix := True;
          Break;
        end;
      end;
      
      if not HasPrefix then
      begin
        frmMain.FInputBuffer := '';
        frmMain.ClearAllMarkers;
      end;
      
      Result := 1;
    end
    else if VKCode = VK_BACK then
    begin
      if Length(frmMain.FInputBuffer) > 0 then
      begin
        SetLength(frmMain.FInputBuffer, Length(frmMain.FInputBuffer) - 1);
        frmMain.HighlightMarkers(frmMain.FInputBuffer);
      end;
      Result := 1;
    end
    else if VKCode = VK_ESCAPE then
    begin
      frmMain.ClearAllMarkers;
      Result := 1;
    end;
  end;
end;

{ TfrmMain }

procedure TfrmMain.FormCreate(Sender: TObject);
begin
  FDataDir := ExtractFilePath(Application.ExeName) + 'data' + PathDelim;
  ForceDirectories(FDataDir);
  
  FConfig := GetDefaultConfig;
  FMarkers := TList.Create;
  SetLength(FControlList, 1000);  // 最多1000个控件
  FControlCount := 0;
  FIsMarkerVisible := False;
  FInputBuffer := '';
  FLabelIndex := 0;
  
  FUIAuto := TUIAutomationHelper.Create;
  
  RegisterHotKeys;
  InstallKeyboardHook;
  
  // 启动 HTTP 服务器
  FHttpServer := TViminaHttpServer.Create(Self, FDataDir);
  FHttpServer.OnClickLabel := @HttpClickLabel;
  FHttpServer.OnShowMarkers := @HttpShowMarkers;
  FHttpServer.OnHideMarkers := @HttpHideMarkers;
  
  try
    FHttpServer.Active := True;
    lblApi.Caption := Format('API: http://localhost:%d', [FHttpServer.Port]);
    lblApi.Font.Color := clGreen;
  except
    on E: Exception do
    begin
      lblApi.Caption := 'API: 启动失败 - ' + E.Message;
      lblApi.Font.Color := clRed;
    end;
  end;
  
  // 托盘图标
  TrayIcon.Visible := True;
  TrayIcon.Hint := 'Vimina - 桌面自动化工具';
  TrayIcon.PopupMenu := PopupMenu1;
  
  // 默认隐藏主窗口
  WindowState := wsMinimized;
  Application.ShowMainForm := False;
end;

procedure TfrmMain.FormDestroy(Sender: TObject);
begin
  UnregisterHotKeys;
  UninstallKeyboardHook;
  ClearAllMarkers;
  FMarkers.Free;
  FUIAuto.Free;
  
  if Assigned(FHttpServer) then
  begin
    FHttpServer.Active := False;
    FHttpServer.Free;
  end;
end;

procedure TfrmMain.FormClose(Sender: TObject; var CloseAction: TCloseAction);
begin
  ClearAllMarkers;
end;

procedure TfrmMain.TrayIconClick(Sender: TObject);
begin
  if WindowState = wsMinimized then
  begin
    Show;
    WindowState := wsNormal;
    BringToFront;
  end
  else
  begin
    WindowState := wsMinimized;
    Hide;
  end;
end;

procedure TfrmMain.miShowClick(Sender: TObject);
begin
  Show;
  WindowState := wsNormal;
  BringToFront;
end;

procedure TfrmMain.miDataDirClick(Sender: TObject);
begin
  {$IFDEF WINDOWS}
  ShellExecute(0, 'open', PChar(FDataDir), nil, nil, SW_SHOW);
  {$ENDIF}
end;

procedure TfrmMain.miExitClick(Sender: TObject);
begin
  Application.Terminate;
end;

procedure TfrmMain.RegisterHotKeys;
begin
  FHotKeyId_AltF := GlobalAddAtom('Vimina_HotKey_AltF');
  FHotKeyId_Esc := GlobalAddAtom('Vimina_HotKey_Esc');
  FHotKeyId_AltR := GlobalAddAtom('Vimina_HotKey_AltR');
  
  RegisterHotKey(Handle, FHotKeyId_AltF, MOD_ALT, Ord('F'));
  RegisterHotKey(Handle, FHotKeyId_Esc, 0, VK_ESCAPE);
  RegisterHotKey(Handle, FHotKeyId_AltR, MOD_ALT, Ord('R'));
end;

procedure TfrmMain.UnregisterHotKeys;
begin
  UnregisterHotKey(Handle, FHotKeyId_AltF);
  UnregisterHotKey(Handle, FHotKeyId_Esc);
  UnregisterHotKey(Handle, FHotKeyId_AltR);
  
  GlobalDeleteAtom(FHotKeyId_AltF);
  GlobalDeleteAtom(FHotKeyId_Esc);
  GlobalDeleteAtom(FHotKeyId_AltR);
end;

procedure TfrmMain.InstallKeyboardHook;
begin
  FKeyboardHook := SetWindowsHookEx(WH_KEYBOARD_LL, @KeyboardHookProc, HInstance, 0);
end;

procedure TfrmMain.UninstallKeyboardHook;
begin
  if FKeyboardHook <> 0 then
    UnhookWindowsHookEx(FKeyboardHook);
end;

procedure TfrmMain.WMHotKey(var Msg: TMessage);
begin
  if Msg.WParam = FHotKeyId_AltF then
    ToggleMarkers
  else if Msg.WParam = FHotKeyId_Esc then
  begin
    if FIsMarkerVisible then
      ClearAllMarkers;
  end
  else if Msg.WParam = FHotKeyId_AltR then
  begin
    if FIsMarkerVisible then
    begin
      ClearAllMarkers;
      ToggleMarkers;
    end;
  end;
end;

procedure TfrmMain.WndProc(var Message: TMessage);
begin
  inherited WndProc(Message);
end;

function TfrmMain.GenerateNextLabel: string;
var
  Chars: string;
  Index, First, Second: Integer;
begin
  Inc(FLabelIndex);
  
  if FLabelIndex <= Length(PREDEFINED_LABELS) then
    Result := PREDEFINED_LABELS[FLabelIndex - 1]
  else
  begin
    Chars := 'ASDFGHJKLQWERTYUIOPZXCVBNM';
    Index := FLabelIndex - Length(PREDEFINED_LABELS);
    First := (Index - 1) div Length(Chars) + 1;
    Second := ((Index - 1) mod Length(Chars)) + 1;
    
    if First <= Length(Chars) then
      Result := Chars[First] + Chars[Second]
    else
      Result := 'Z' + IntToStr(Index);
  end;
end;

procedure TfrmMain.ToggleMarkers;
var
  I: Integer;
  X, Y: Integer;
begin
  if FIsMarkerVisible then
  begin
    ClearAllMarkers;
    Exit;
  end;
  
  ClearAllMarkers;
  
  if not DoScan then
    Exit;
    
  FIsMarkerVisible := True;
  
  for I := 0 to FControlCount - 1 do
  begin
    X := FControlList[I].X + FConfig.LabelCfg.OffsetX;
    Y := FControlList[I].Y + FConfig.LabelCfg.OffsetY;
    
    if X < 0 then X := 0;
    if Y < 0 then 
      Y := FControlList[I].Y + FControlList[I].Height + 2;
    
    CreateMarkerWindow(X, Y, FControlList[I].LabelText);
  end;
  
  SaveScanResult;
end;

procedure TfrmMain.ClearAllMarkers;
var
  I: Integer;
  Marker: TfrmMarker;
begin
  for I := 0 to FMarkers.Count - 1 do
  begin
    Marker := TfrmMarker(FMarkers[I]);
    Marker.Close;
    Marker.Free;
  end;
  
  FMarkers.Clear;
  FControlCount := 0;
  FIsMarkerVisible := False;
  FInputBuffer := '';
  FLabelIndex := 0;
end;

procedure TfrmMain.HighlightMarkers(const Input: string);
var
  I: Integer;
  Marker: TfrmMarker;
begin
  if Input = '' then
  begin
    for I := 0 to FMarkers.Count - 1 do
    begin
      Marker := TfrmMarker(FMarkers[I]);
      Marker.SetBackgroundColor(FConfig.LabelCfg.BackgroundColor_Default);
    end;
    Exit;
  end;
  
  for I := 0 to FMarkers.Count - 1 do
  begin
    Marker := TfrmMarker(FMarkers[I]);
    
    if Marker.LabelText = Input then
      Marker.SetBackgroundColor(FConfig.LabelCfg.BackgroundColor_Match)
    else if Pos(Input, Marker.LabelText) = 1 then
      Marker.SetBackgroundColor(FConfig.LabelCfg.BackgroundColor_Prefix)
    else
      Marker.SetBackgroundColor(FConfig.LabelCfg.BackgroundColor_Invalid);
  end;
end;

function TfrmMain.DoScan: Boolean;
var
  ForeWnd: HWND;
  WindowTitle: array[0..255] of Char;
  WndClassName: array[0..255] of Char;
  I: Integer;
begin
  Result := False;

  ForeWnd := GetForegroundWindow;
  if (ForeWnd = 0) or (ForeWnd = Handle) then
  begin
    ShowMessage('无前台窗口。请先点击要操作的窗口。');
    Exit;
  end;

  GetWindowText(ForeWnd, WindowTitle, 256);
  GetClassName(ForeWnd, WndClassName, 256);
  
  FControlCount := FUIAuto.GetInteractiveControls(ForeWnd, FControlList);
  
  if FControlCount = 0 then
  begin
    ShowMessage('未找到可交互控件。该窗口可能使用了自绘控件。');
    Exit;
  end;
  
  DeduplicateControls;
  
  FLabelIndex := 0;
  for I := 0 to FControlCount - 1 do
  begin
    FControlList[I].LabelText := GenerateNextLabel;
  end;
  
  Result := True;
end;

procedure TfrmMain.DeduplicateControls;
var
  I, J: Integer;
  Seen: array of Boolean;
  KeyX, KeyY: Integer;
  NewCount: Integer;
begin
  if FControlCount = 0 then
    Exit;
    
  SetLength(Seen, FControlCount);
  FillChar(Seen[0], FControlCount * SizeOf(Boolean), 0);
    
  NewCount := 0;
  
  for I := 0 to FControlCount - 1 do
  begin
    if Seen[I] then
      Continue;
      
    KeyX := (FControlList[I].CenterX div 10) * 10;
    KeyY := (FControlList[I].CenterY div 10) * 10;
    
    // 标记相似位置的控件
    for J := I + 1 to FControlCount - 1 do
    begin
      if not Seen[J] then
      begin
        if (Abs((FControlList[J].CenterX div 10) * 10 - KeyX) < 10) and
           (Abs((FControlList[J].CenterY div 10) * 10 - KeyY) < 10) then
        begin
          Seen[J] := True;
        end;
      end;
    end;
    
    if I <> NewCount then
      FControlList[NewCount] := FControlList[I];
      
    Inc(NewCount);
  end;
  
  FControlCount := NewCount;
end;

procedure TfrmMain.SaveScanResult;
var
  RootJSON, WindowJSON, SummaryJSON: TJSONObject;
  ControlsArray, QuickRefArray: TJSONArray;
  TypeStats: TJSONObject;
  I: Integer;
  CtrlJSON: TJSONObject;
  LabelMapJSON: TJSONObject;
  PosJSON: TJSONObject;
  TypeName: string;
  TypeCount: Integer;
begin
  RootJSON := TJSONObject.Create;
  try
    RootJSON.Add('success', True);
    RootJSON.Add('timestamp', FormatDateTime('yyyy-mm-dd hh:nn:ss', Now));
    
    WindowJSON := TJSONObject.Create;
    WindowJSON.Add('title', 'Current Window');
    WindowJSON.Add('class', 'Unknown');
    RootJSON.Add('window', WindowJSON);
    
    // 统计控件类型
    TypeStats := TJSONObject.Create;
    for I := 0 to FControlCount - 1 do
    begin
      TypeName := FControlList[I].ControlType;
      if TypeStats.IndexOfName(TypeName) >= 0 then
        TypeCount := TypeStats.Integers[TypeName] + 1
      else
        TypeCount := 1;
      TypeStats.Integers[TypeName] := TypeCount;
    end;
    
    SummaryJSON := TJSONObject.Create;
    SummaryJSON.Add('totalControls', FControlCount);
    SummaryJSON.Add('byType', TypeStats);
    RootJSON.Add('summary', SummaryJSON);
    
    // 快速参考
    QuickRefArray := TJSONArray.Create;
    for I := 0 to FControlCount - 1 do
    begin
      QuickRefArray.Add(Format('%s: %s (%s)', [
        FControlList[I].LabelText,
        IfThen(FControlList[I].Name <> '', FControlList[I].Name, FControlList[I].TypeDesc),
        FControlList[I].ControlType
      ]));
    end;
    RootJSON.Add('quickReference', QuickRefArray);
    
    // 控件详情
    ControlsArray := TJSONArray.Create;
    for I := 0 to FControlCount - 1 do
    begin
      CtrlJSON := TJSONObject.Create;
      CtrlJSON.Add('label', FControlList[I].LabelText);
      CtrlJSON.Add('name', FControlList[I].Name);
      CtrlJSON.Add('type', FControlList[I].ControlType);
      CtrlJSON.Add('typeNum', FControlList[I].TypeNum);
      CtrlJSON.Add('typeDesc', FControlList[I].TypeDesc);
      CtrlJSON.Add('actionHint', FControlList[I].ActionHint);
      CtrlJSON.Add('x', FControlList[I].X);
      CtrlJSON.Add('y', FControlList[I].Y);
      CtrlJSON.Add('width', FControlList[I].Width);
      CtrlJSON.Add('height', FControlList[I].Height);
      CtrlJSON.Add('centerX', FControlList[I].CenterX);
      CtrlJSON.Add('centerY', FControlList[I].CenterY);
      ControlsArray.Add(CtrlJSON);
    end;
    RootJSON.Add('controls', ControlsArray);
    
    WriteJsonFile(FDataDir + 'scan_result.json', RootJSON);
  finally
    RootJSON.Free;
  end;
  
  // 保存标签映射
  LabelMapJSON := TJSONObject.Create;
  try
    for I := 0 to FControlCount - 1 do
    begin
      PosJSON := TJSONObject.Create;
      PosJSON.Add('centerX', FControlList[I].CenterX);
      PosJSON.Add('centerY', FControlList[I].CenterY);
      LabelMapJSON.Add(FControlList[I].LabelText, PosJSON);
    end;
    
    WriteJsonFile(FDataDir + 'label_map.json', LabelMapJSON);
  finally
    LabelMapJSON.Free;
  end;
end;

procedure TfrmMain.CreateMarkerWindow(X, Y: Integer; const ALabel: string);
var
  Marker: TfrmMarker;
begin
  Marker := TfrmMarker.CreateNew(nil, X, Y, ALabel, FConfig.LabelCfg);
  Marker.Show;
  FMarkers.Add(Marker);
end;

procedure TfrmMain.PerformClick(const Ctrl: TControlInfo);
var
  OrigPos: TPoint;
begin
  GetCursorPos(OrigPos);
  
  SetCursorPos(Ctrl.CenterX, Ctrl.CenterY);
  Sleep(10);
  mouse_event(MOUSEEVENTF_LEFTDOWN or MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
  Sleep(FConfig.PerfCfg.ClickDelay);
  
  SetCursorPos(OrigPos.X, OrigPos.Y);
end;

procedure TfrmMain.HttpClickLabel(const ALabel: string);
var
  I: Integer;
begin
  for I := 0 to FControlCount - 1 do
  begin
    if FControlList[I].LabelText = UpperCase(ALabel) then
    begin
      PerformClick(FControlList[I]);
      Break;
    end;
  end;
end;

procedure TfrmMain.HttpShowMarkers;
begin
  if not FIsMarkerVisible then
    ToggleMarkers;
end;

procedure TfrmMain.HttpHideMarkers;
begin
  if FIsMarkerVisible then
    ClearAllMarkers;
end;

end.