unit MainForm;

{$mode objfpc}{$H+}
{$codepage UTF8}

interface

uses
  Classes, SysUtils, Forms, Controls, Graphics, Dialogs, StdCtrls, ExtCtrls,
  Menus, LCLType, LCLIntf, Windows, Messages,
  Config, fpJSON, UIAutomation, MarkerForm, ConfigForm, HttpServer;

type
  TfrmMain = class(TForm)
    lblInfo1: TLabel;
    lblInfo2: TLabel;
    lblInfo3: TLabel;
    lblApi: TLabel;
    btnConfig: TButton;
    PopupMenu1: TPopupMenu;
    miShow: TMenuItem;
    miConfig: TMenuItem;
    miScriptEditor: TMenuItem;
    miMouseSpy: TMenuItem;
    miSep: TMenuItem;
    miExit: TMenuItem;
    procedure FormCreate(Sender: TObject);
    procedure FormDestroy(Sender: TObject);
    procedure FormShow(Sender: TObject);
    procedure miShowClick(Sender: TObject);
    procedure miConfigClick(Sender: TObject);
    procedure miScriptEditorClick(Sender: TObject);
    procedure miMouseSpyClick(Sender: TObject);
    procedure miExitClick(Sender: TObject);
    procedure btnConfigClick(Sender: TObject);
  private
    FConfig: TViminaConfig;
    FTrayIcon: TTrayIcon;
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
    FDataDir: string;
    FConfigFile: string;
    FUIAuto: TUIAutomationHelper;
    FLastScanHwnd: HWND;
    FHttpServer: TViminaHttpServer;
    
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
    procedure PerformClick(const Ctrl: TControlInfo; RightClick, DoubleClick: Boolean);
    procedure DeduplicateControls;
    procedure SaveScanResult;
    procedure TrayIconClick(Sender: TObject);
    procedure HttpShowMarkers;
    procedure HttpHideMarkers;
  protected
    procedure WMHotKey(var Msg: TMessage); message WM_HOTKEY;
  public
  end;

var
  frmMain: TfrmMain;

const
  WH_KEYBOARD_LL = 13;
  LLKHF_INJECTED = $00000010;

type
  PKBDLLHOOKSTRUCT = ^TKBDLLHOOKSTRUCT;
  TKBDLLHOOKSTRUCT = record
    vkCode: DWORD;
    scanCode: DWORD;
    flags: DWORD;
    time: DWORD;
    dwExtraInfo: ULONG_PTR;
  end;

function KeyboardHookProc(nCode: Integer; wParam: WPARAM; lParam: LPARAM): LRESULT; stdcall;

implementation

{$R *.lfm}

uses
  Math, StrUtils, ScriptEditorForm, MouseSpyForm;

function KeyboardHookProc(nCode: Integer; wParam: WPARAM; lParam: LPARAM): LRESULT; stdcall;
var
  KeyInfo: PKBDLLHOOKSTRUCT;
  VKCode: DWORD;
  Ch: string;
  HasPrefix: Boolean;
  I: Integer;
begin
  Result := CallNextHookEx(0, nCode, wParam, lParam);
  
  if (nCode < 0) or not Assigned(frmMain) or not frmMain.FIsMarkerVisible then
    Exit;
    
  KeyInfo := PKBDLLHOOKSTRUCT(lParam);
  
  if (KeyInfo^.flags and LLKHF_INJECTED) <> 0 then
    Exit;
    
  if (wParam = WM_KEYDOWN) or (wParam = WM_SYSKEYDOWN) then
  begin
    VKCode := KeyInfo^.vkCode;
    
    if (VKCode >= Ord('A')) and (VKCode <= Ord('Z')) then
    begin
      Ch := Chr(VKCode);
      frmMain.FInputBuffer := frmMain.FInputBuffer + Ch;
      frmMain.HighlightMarkers(frmMain.FInputBuffer);
      
      for I := 0 to frmMain.FControlCount - 1 do
      begin
        if frmMain.FControlList[I].LabelText = frmMain.FInputBuffer then
        begin
          frmMain.PerformClick(frmMain.FControlList[I], False, False);
          frmMain.ClearAllMarkers;
          Result := 1;
          Exit;
        end;
      end;
      
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

procedure TfrmMain.FormCreate(Sender: TObject);
begin
  Caption := 'Vimina';
  
  FDataDir := ExtractFilePath(Application.ExeName) + 'data' + PathDelim;
  ForceDirectories(FDataDir);
  
  FConfigFile := ExtractFilePath(Application.ExeName) + 'config.json';
  FConfig := GetDefaultConfig;
  
  FMarkers := TList.Create;
  SetLength(FControlList, 1000);
  FControlCount := 0;
  FIsMarkerVisible := False;
  FInputBuffer := '';
  FLabelIndex := 0;
  FLastScanHwnd := 0;
  
  FUIAuto := TUIAutomationHelper.Create;
  
  RegisterHotKeys;
  InstallKeyboardHook;
  
  FTrayIcon := TTrayIcon.Create(Self);
  FTrayIcon.Hint := 'Vimina - 桌面自动化工具';
  FTrayIcon.PopupMenu := PopupMenu1;
  FTrayIcon.OnClick := @TrayIconClick;
  
  try
    if FileExists(ExtractFilePath(Application.ExeName) + 'logo.ico') then
    begin
      FTrayIcon.Icon.LoadFromFile(ExtractFilePath(Application.ExeName) + 'logo.ico');
      Application.Icon.LoadFromFile(ExtractFilePath(Application.ExeName) + 'logo.ico');
    end;
  except
  end;
  
  FHttpServer := TViminaHttpServer.Create(nil, FDataDir, FConfigFile);
  FHttpServer.OnShowMarkers := @HttpShowMarkers;
  FHttpServer.OnHideMarkers := @HttpHideMarkers;
  try
    FHttpServer.Active := True;
  except
    on E: Exception do
      lblApi.Caption := 'API启动失败: ' + E.Message;
  end;
end;

procedure TfrmMain.FormShow(Sender: TObject);
begin
  FTrayIcon.Visible := True;
  lblApi.Caption := 'API: http://localhost:51401';
  lblApi.Font.Color := clGreen;
  
  Visible := True;
  WindowState := wsNormal;
  BringToFront;
  SetForegroundWindow(Handle);
end;

procedure TfrmMain.FormDestroy(Sender: TObject);
begin
  if Assigned(FHttpServer) then
  begin
    FHttpServer.Active := False;
    FHttpServer.Free;
  end;
  
  UnregisterHotKeys;
  UninstallKeyboardHook;
  ClearAllMarkers;
  FMarkers.Free;
  FUIAuto.Free;
  
  if Assigned(FTrayIcon) then
  begin
    FTrayIcon.Visible := False;
    FTrayIcon.Free;
  end;
end;

procedure TfrmMain.miShowClick(Sender: TObject);
begin
  Show;
  WindowState := wsNormal;
  BringToFront;
end;

procedure TfrmMain.miConfigClick(Sender: TObject);
begin
  btnConfigClick(Sender);
end;

procedure TfrmMain.miScriptEditorClick(Sender: TObject);
var
  Editor: TfrmScriptEditor;
begin
  Editor := TfrmScriptEditor.Create(Application);
  Editor.DataDir := FDataDir;
  Editor.ConfigFile := FConfigFile;
  Editor.Show;
end;

procedure TfrmMain.miMouseSpyClick(Sender: TObject);
var
  Spy: TfrmMouseSpy;
begin
  Spy := TfrmMouseSpy.Create(Application);
  Spy.Show;
end;

procedure TfrmMain.miExitClick(Sender: TObject);
begin
  Application.Terminate;
end;

procedure TfrmMain.btnConfigClick(Sender: TObject);
var
  ConfigDlg: TfrmConfig;
begin
  ConfigDlg := TfrmConfig.Create(Self);
  try
    ConfigDlg.SetConfig(@FConfig);
    if ConfigDlg.ShowModal = mrOK then
    begin
      SaveConfig(FConfigFile, FConfig);
      ShowMessage('配置已保存，部分设置需要重新扫描后生效。');
    end;
  finally
    ConfigDlg.Free;
  end;
end;

procedure TfrmMain.TrayIconClick(Sender: TObject);
begin
  if Visible then
    Hide
  else
  begin
    Show;
    WindowState := wsNormal;
    BringToFront;
  end;
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

function TfrmMain.GenerateNextLabel: string;
begin
  Inc(FLabelIndex);
  if FLabelIndex <= Length(PREDEFINED_LABELS) then
    Result := PREDEFINED_LABELS[FLabelIndex - 1]
  else
    Result := 'Z' + IntToStr(FLabelIndex);
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
  
  FLastScanHwnd := ForeWnd;
  FControlCount := FUIAuto.GetInteractiveControls(ForeWnd, FControlList, FConfig.FilterCfg.MaxDepth);
  
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
    FControlList[I].Hwnd := ForeWnd;
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
  WindowTitle: array[0..255] of Char;
  WndClassName: array[0..255] of Char;
begin
  RootJSON := TJSONObject.Create;
  try
    RootJSON.Add('success', True);
    RootJSON.Add('timestamp', FormatDateTime('yyyy-mm-dd hh:nn:ss', Now));
    
    GetWindowText(FLastScanHwnd, WindowTitle, 256);
    GetClassName(FLastScanHwnd, WndClassName, 256);
    
    WindowJSON := TJSONObject.Create;
    WindowJSON.Add('title', WindowTitle);
    WindowJSON.Add('class', WndClassName);
    WindowJSON.Add('handle', FLastScanHwnd);
    RootJSON.Add('window', WindowJSON);
    
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
    SummaryJSON.Add('description', Format('窗口「%s」共有 %d 个可交互控件', [WindowTitle, FControlCount]));
    RootJSON.Add('summary', SummaryJSON);
    
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

procedure TfrmMain.PerformClick(const Ctrl: TControlInfo; RightClick, DoubleClick: Boolean);
var
  OrigPos: TPoint;
begin
  GetCursorPos(OrigPos);
  
  if FConfig.ClickModeCfg.BringToFront and (Ctrl.Hwnd <> 0) then
  begin
    SetForegroundWindow(Ctrl.Hwnd);
    Sleep(50);
  end;
  
  SetCursorPos(Ctrl.CenterX, Ctrl.CenterY);
  Sleep(10);
  
  if DoubleClick then
  begin
    mouse_event(MOUSEEVENTF_LEFTDOWN or MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
    Sleep(50);
    mouse_event(MOUSEEVENTF_LEFTDOWN or MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
  end
  else if RightClick then
    mouse_event(MOUSEEVENTF_RIGHTDOWN or MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0)
  else
    mouse_event(MOUSEEVENTF_LEFTDOWN or MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
  
  Sleep(FConfig.PerfCfg.ClickDelay);
  SetCursorPos(OrigPos.X, OrigPos.Y);
end;

procedure TfrmMain.HttpShowMarkers;
begin
  if FIsMarkerVisible then
    ClearAllMarkers;
  ToggleMarkers;
end;

procedure TfrmMain.HttpHideMarkers;
begin
  ClearAllMarkers;
end;

end.
