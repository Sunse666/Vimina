unit HttpServer;

{$mode objfpc}{$H+}
{$codepage UTF8}

interface

uses
  Classes, SysUtils, fphttpserver, httpdefs, httpprotocol, fpjson, jsonparser,
  Windows, Config, VMAEngine;

type
  TClickLabelEvent = procedure(const ALabel: string; RightClick, DoubleClick: Boolean) of object;
  TScanRequestEvent = procedure of object;
  TShowMarkersEvent = procedure of object;
  THideMarkersEvent = procedure of object;

  TViminaHttpServer = class(TFPHTTPServer)
  private
    FDataDir: string;
    FConfigFile: string;
    FOnClickLabel: TClickLabelEvent;
    FOnScanRequest: TScanRequestEvent;
    FOnShowMarkers: TShowMarkersEvent;
    FOnHideMarkers: THideMarkersEvent;
    FVMAEngine: TVMAEngine;
  protected
    procedure HandleRequest(var ARequest: TFPHTTPConnectionRequest;
      var AResponse: TFPHTTPConnectionResponse); override;
    procedure DoRequest(ARequest: TFPHTTPConnectionRequest;
      AResponse: TFPHTTPConnectionResponse);
  public
    constructor Create(AOwner: TComponent; const ADataDir, AConfigFile: string); reintroduce;
    destructor Destroy; override;
    property OnClickLabel: TClickLabelEvent
      read FOnClickLabel write FOnClickLabel;
    property OnScanRequest: TScanRequestEvent
      read FOnScanRequest write FOnScanRequest;
    property OnShowMarkers: TShowMarkersEvent
      read FOnShowMarkers write FOnShowMarkers;
    property OnHideMarkers: THideMarkersEvent
      read FOnHideMarkers write FOnHideMarkers;
    property VMAEngine: TVMAEngine read FVMAEngine;
  end;

  function ReadJsonFile(const FileName: string): TJSONData;
  function GetWindowList: TJSONArray;
  function FindWindowByTitle(const Title: string): HWND;
  function ActivateWindowByHwnd(Hwnd: HWND): Boolean;

implementation

uses
  Math, jsonscanner, Messages, StrUtils;

const
  SystemWindowClasses: array[0..4] of string = (
    'Shell_TrayWnd', 'Progman', 'WorkerW', 
    'Windows.UI.Core.CoreWindow', 'ApplicationFrameWindow'
  );

function IsSystemWindowClass(const ClassName: string): Boolean;
var
  I: Integer;
begin
  Result := False;
  for I := 0 to High(SystemWindowClasses) do
    if SystemWindowClasses[I] = ClassName then
    begin
      Result := True;
      Exit;
    end;
end;

function ReadJsonFile(const FileName: string): TJSONData;
var
  FS: TFileStream;
  Parser: TJSONParser;
begin
  Result := nil;
  
  if not FileExists(FileName) then
    Exit;
    
  try
    FS := TFileStream.Create(FileName, fmOpenRead or fmShareDenyWrite);
    try
      Parser := TJSONParser.Create(FS, [joUTF8]);
      try
        Result := Parser.Parse;
      finally
        Parser.Free;
      end;
    finally
      FS.Free;
    end;
  except
    Result := nil;
  end;
end;

var
  GWindowList: TJSONArray;
  
function EnumWindowsCallback(Wnd: HWND; lParam: LPARAM): BOOL; stdcall;
var
  WindowTitle: array[0..255] of Char;
  ClassName: array[0..255] of Char;
  PID: DWORD;
  WindowJSON: TJSONObject;
begin
  Result := True;
  
  if not IsWindowVisible(Wnd) then
    Exit;
    
  GetClassName(Wnd, ClassName, 256);
  if IsSystemWindowClass(ClassName) then
    Exit;
  
  GetWindowText(Wnd, WindowTitle, 256);
  if StrLen(WindowTitle) = 0 then
    Exit;
  
  GetWindowThreadProcessId(Wnd, @PID);
  
  WindowJSON := TJSONObject.Create;
  WindowJSON.Add('hwnd', Wnd);
  WindowJSON.Add('title', WindowTitle);
  WindowJSON.Add('className', ClassName);
  WindowJSON.Add('processId', PID);
  
  GWindowList.Add(WindowJSON);
end;

function GetWindowList: TJSONArray;
begin
  GWindowList := TJSONArray.Create;
  EnumWindows(@EnumWindowsCallback, 0);
  Result := GWindowList;
end;

function FindWindowByTitle(const Title: string): HWND;
var
  Wnd: HWND;
  WindowTitle: array[0..255] of Char;
begin
  Result := 0;
  Wnd := GetWindow(GetDesktopWindow, GW_CHILD);
  
  while Wnd <> 0 do
  begin
    if IsWindowVisible(Wnd) then
    begin
      GetWindowText(Wnd, WindowTitle, 256);
      if (Pos(Title, WindowTitle) > 0) or (Pos(WindowTitle, Title) > 0) then
      begin
        Result := Wnd;
        Exit;
      end;
    end;
    Wnd := GetWindow(Wnd, GW_HWNDNEXT);
  end;
end;

function ActivateWindowByHwnd(Hwnd: HWND): Boolean;
begin
  Result := False;
  if Hwnd = 0 then Exit;
  
  if IsIconic(Hwnd) then
    ShowWindow(Hwnd, SW_RESTORE);
    
  SetForegroundWindow(Hwnd);
  Result := True;
end;

constructor TViminaHttpServer.Create(AOwner: TComponent; const ADataDir, AConfigFile: string);
begin
  inherited Create(AOwner);
  FDataDir := ADataDir;
  FConfigFile := AConfigFile;
  Port := 51401;
  Threaded := True;
  FVMAEngine := TVMAEngine.Create(ADataDir, AConfigFile);
end;

destructor TViminaHttpServer.Destroy;
begin
  FVMAEngine.Free;
  inherited Destroy;
end;

procedure TViminaHttpServer.HandleRequest(var ARequest: TFPHTTPConnectionRequest;
  var AResponse: TFPHTTPConnectionResponse);
begin
  try
    DoRequest(ARequest, AResponse);
  except
    on E: Exception do
    begin
      AResponse.Code := 500;
      AResponse.ContentType := 'application/json; charset=utf-8';
      AResponse.Content := Format('{"success":false,"error":"%s"}', [E.Message]);
    end;
  end;
end;

procedure TViminaHttpServer.DoRequest(ARequest: TFPHTTPConnectionRequest;
  AResponse: TFPHTTPConnectionResponse);
var
  Path: string;
  RequestMethod: string;
  JSON, ResultJSON: TJSONObject;
  ScanData, LabelData: TJSONData;
  Body: string;
  X, Y, X1, Y1, X2, Y2: Integer;
  LabelText: string;
  LabelObj: TJSONObject;
  PosObj, MouseObj, ScreenObj, FromObj, ToObj: TJSONObject;
  P: TPoint;
  OrigPos: TPoint;
  RightClick, DoubleClick: Boolean;
  BringToFront: Boolean;
  Title: string;
  TargetHwnd: HWND;
  WindowsArray: TJSONArray;
  I: Integer;
  Text: string;
  QueryValue: string;
  ScreenWidth, ScreenHeight: Integer;
  Endpoints: TJSONArray;
  CtrlJSON: TJSONObject;
  
  procedure DoClick(AX, AY: Integer; ARightClick, ADoubleClick: Boolean);
  begin
    GetCursorPos(OrigPos);
    
    SetCursorPos(AX, AY);
    Sleep(10);
    
    if ADoubleClick then
    begin
      mouse_event(MOUSEEVENTF_LEFTDOWN or MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
      Sleep(50);
      mouse_event(MOUSEEVENTF_LEFTDOWN or MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
    end
    else if ARightClick then
      mouse_event(MOUSEEVENTF_RIGHTDOWN or MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0)
    else
      mouse_event(MOUSEEVENTF_LEFTDOWN or MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
      
    Sleep(30);
    SetCursorPos(OrigPos.X, OrigPos.Y);
  end;
  
  procedure DoDrag(AX1, AY1, AX2, AY2: Integer);
  var
    DragI, DragStepX, DragStepY: Integer;
  begin
    GetCursorPos(OrigPos);
    
    SetCursorPos(AX1, AY1);
    Sleep(50);
    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
    Sleep(50);
    
    DragStepX := (AX2 - AX1) div 10;
    DragStepY := (AY2 - AY1) div 10;
    for DragI := 1 to 10 do
    begin
      SetCursorPos(AX1 + DragStepX * DragI, AY1 + DragStepY * DragI);
      Sleep(10);
    end;
    
    SetCursorPos(AX2, AY2);
    Sleep(50);
    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
    Sleep(30);
    SetCursorPos(OrigPos.X, OrigPos.Y);
  end;
  
  procedure DoInputText(const AText: string);
  var
    InputI: Integer;
    InputVK: Word;
    InputRec: TInput;
  begin
    for InputI := 1 to Length(AText) do
    begin
      InputVK := VkKeyScan(AText[InputI]);
      if InputVK <> 0 then
      begin
        FillChar(InputRec, SizeOf(InputRec), 0);
        InputRec.ki.wVk := InputVK and $FF;
        InputRec.ki.dwFlags := 0;
        SendInput(1, @InputRec, SizeOf(InputRec));
        
        InputRec.ki.dwFlags := KEYEVENTF_KEYUP;
        SendInput(1, @InputRec, SizeOf(InputRec));
        Sleep(10);
      end;
    end;
  end;
  
  function GetQueryParam(const ParamName: string; const Default: string = ''): string;
  var
    Query: string;
    Parts: TStringList;
  begin
    Result := Default;
    if ARequest.QueryFields.Count > 0 then
      Result := ARequest.QueryFields.Values[ParamName];
    if Result = '' then
    begin
      if Pos('?', ARequest.URL) > 0 then
      begin
        Query := Copy(ARequest.URL, Pos('?', ARequest.URL) + 1, MaxInt);
        Parts := TStringList.Create;
        try
          Parts.Delimiter := '&';
          Parts.StrictDelimiter := True;
          Parts.DelimitedText := Query;
          Result := Parts.Values[ParamName];
        finally
          Parts.Free;
        end;
      end;
    end;
    if Result = '' then
      Result := Default;
  end;
  
begin
  Path := ARequest.PathInfo;
  RequestMethod := ARequest.Method;
  
  AResponse.SetCustomHeader('Access-Control-Allow-Origin', '*');
  AResponse.SetCustomHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  AResponse.SetCustomHeader('Access-Control-Allow-Headers', 'Content-Type');
  AResponse.ContentType := 'application/json; charset=utf-8';
  
  if RequestMethod = 'OPTIONS' then
  begin
    AResponse.Content := '';
    Exit;
  end;
  
  ResultJSON := TJSONObject.Create;
  try
    if (Path = '/') or (Path = '/api') then
    begin
      ResultJSON.Add('name', 'Vimina API');
      ResultJSON.Add('version', '1.2');
      ResultJSON.Add('description', '桌面控件自动化操作接口');
      
      Endpoints := TJSONArray.Create;
      Endpoints.Add('GET  /api                          - API信息');
      Endpoints.Add('GET  /api/scan                      - 获取扫描结果');
      Endpoints.Add('POST /api/show                      - 显示标签并扫描');
      Endpoints.Add('POST /api/hide                      - 隐藏标签');
      Endpoints.Add('POST /api/click                     - 通过标签点击控件');
      Endpoints.Add('GET  /api/click/{x}/{y}             - 坐标点击');
      Endpoints.Add('GET  /api/clickR/{x}/{y}            - 坐标右键点击');
      Endpoints.Add('GET  /api/dblclick/{x}/{y}          - 坐标双击');
      Endpoints.Add('GET  /api/clickAt?x=&y=             - 灵活坐标点击');
      Endpoints.Add('POST /api/clickAt                   - 灵活坐标点击');
      Endpoints.Add('GET  /api/windows                   - 获取窗口列表');
      Endpoints.Add('GET  /api/scanByTitle?title=xxx     - 按标题扫描');
      Endpoints.Add('GET  /api/clickByTitle?title=&x=&y= - 按标题点击');
      Endpoints.Add('GET  /api/activate?title=xxx        - 激活窗口');
      Endpoints.Add('GET  /api/mouse                     - 获取鼠标位置');
      Endpoints.Add('GET  /api/move/{x}/{y}              - 移动鼠标');
      Endpoints.Add('GET  /api/drag/{x1}/{y1}/{x2}/{y2}  - 拖拽操作');
      Endpoints.Add('POST /api/input                     - 输入文本');
      Endpoints.Add('GET  /api/status                    - 获取状态');
      Endpoints.Add('POST /api/vma/run                    - 运行VMA脚本');
      Endpoints.Add('POST /api/vma/runFile                - 运行VMA脚本文件');
      Endpoints.Add('GET  /api/vma/status                 - 获取VMA状态');
      Endpoints.Add('POST /api/vma/stop                   - 停止VMA脚本');
      Endpoints.Add('POST /api/vma/pause                  - 暂停VMA脚本');
      Endpoints.Add('POST /api/vma/resume                 - 恢复VMA脚本');
      ResultJSON.Add('endpoints', Endpoints);
    end
    else if Path = '/api/scan' then
    begin
      ScanData := ReadJsonFile(FDataDir + 'scan_result.json');
      if Assigned(ScanData) then
      begin
        AResponse.Content := ScanData.FormatJSON;
        ScanData.Free;
        Exit;
      end
      else
      begin
        ResultJSON.Add('success', False);
        ResultJSON.Add('error', '尚未扫描');
        ResultJSON.Add('hint', '请先按 Alt+F 扫描窗口');
      end;
    end
    else if (Path = '/api/click') and (RequestMethod = 'POST') then
    begin
      Body := ARequest.Content;
      JSON := TJSONObject(GetJSON(Body));
      try
        LabelText := UpperCase(JSON.Get('label', ''));
        RightClick := JSON.Get('right', False) or JSON.Get('rightClick', False);
        DoubleClick := JSON.Get('double', False) or JSON.Get('doubleClick', False);
        
        if LabelText = '' then
        begin
          ResultJSON.Add('success', False);
          ResultJSON.Add('error', '缺少 label 参数');
        end
        else
        begin
          LabelData := ReadJsonFile(FDataDir + 'label_map.json');
          if Assigned(LabelData) then
          begin
            try
              LabelObj := TJSONObject(LabelData).Objects[LabelText];
              if Assigned(LabelObj) then
              begin
                X := LabelObj.Get('centerX', 0);
                Y := LabelObj.Get('centerY', 0);
                
                DoClick(X, Y, RightClick, DoubleClick);
                
                ResultJSON.Add('success', True);
                ResultJSON.Add('message', '已点击控件');
                ResultJSON.Add('label', LabelText);
                ResultJSON.Add('clickType', IfThen(DoubleClick, 'double', IfThen(RightClick, 'right', 'left')));
              end
              else
              begin
                ResultJSON.Add('success', False);
                ResultJSON.Add('error', '未找到标签: ' + LabelText);
              end;
            finally
              LabelData.Free;
            end;
          end
          else
          begin
            ResultJSON.Add('success', False);
            ResultJSON.Add('error', '尚未扫描');
            ResultJSON.Add('hint', '请先按 Alt+F 扫描窗口');
          end;
        end;
      finally
        JSON.Free;
      end;
    end
    else if Pos('/api/click/', Path) = 1 then
    begin
      Delete(Path, 1, Length('/api/click/'));
      X := StrToIntDef(Copy(Path, 1, Pos('/', Path) - 1), -1);
      Delete(Path, 1, Pos('/', Path));
      Y := StrToIntDef(Path, -1);
      
      if (X >= 0) and (Y >= 0) then
      begin
        DoClick(X, Y, False, False);
        ResultJSON.Add('success', True);
        ResultJSON.Add('message', '已点击');
        PosObj := TJSONObject.Create;
        PosObj.Add('x', X);
        PosObj.Add('y', Y);
        ResultJSON.Add('position', PosObj);
      end
      else
      begin
        ResultJSON.Add('success', False);
        ResultJSON.Add('error', '坐标格式错误');
        ResultJSON.Add('hint', '正确格式: /api/click/100/200');
      end;
    end
    else if Pos('/api/clickR/', Path) = 1 then
    begin
      Delete(Path, 1, Length('/api/clickR/'));
      X := StrToIntDef(Copy(Path, 1, Pos('/', Path) - 1), -1);
      Delete(Path, 1, Pos('/', Path));
      Y := StrToIntDef(Path, -1);
      
      if (X >= 0) and (Y >= 0) then
      begin
        DoClick(X, Y, True, False);
        ResultJSON.Add('success', True);
        ResultJSON.Add('message', '已右键点击');
      end;
    end
    else if Pos('/api/dblclick/', Path) = 1 then
    begin
      Delete(Path, 1, Length('/api/dblclick/'));
      X := StrToIntDef(Copy(Path, 1, Pos('/', Path) - 1), -1);
      Delete(Path, 1, Pos('/', Path));
      Y := StrToIntDef(Path, -1);
      
      if (X >= 0) and (Y >= 0) then
      begin
        DoClick(X, Y, False, True);
        ResultJSON.Add('success', True);
        ResultJSON.Add('message', '已双击');
      end;
    end
    else if (Path = '/api/clickAt') or (Pos('/api/clickAt?', Path) = 1) then
    begin
      RightClick := False;
      DoubleClick := False;
      
      if RequestMethod = 'POST' then
      begin
        Body := ARequest.Content;
        if Length(Body) > 0 then
        begin
          JSON := TJSONObject(GetJSON(Body));
          try
            X := JSON.Get('x', -1);
            Y := JSON.Get('y', -1);
            RightClick := JSON.Get('right', False) or JSON.Get('rightClick', False);
            DoubleClick := JSON.Get('double', False) or JSON.Get('doubleClick', False);
          finally
            JSON.Free;
          end;
        end;
      end
      else
      begin
        X := StrToIntDef(GetQueryParam('x'), -1);
        Y := StrToIntDef(GetQueryParam('y'), -1);
        QueryValue := GetQueryParam('right');
        RightClick := (QueryValue = '1') or (QueryValue = 'true');
        QueryValue := GetQueryParam('double');
        DoubleClick := (QueryValue = '1') or (QueryValue = 'true');
      end;
      
      if (X < 0) or (Y < 0) then
      begin
        ResultJSON.Add('success', False);
        ResultJSON.Add('error', '缺少坐标参数');
        ResultJSON.Add('hint', 'GET: /api/clickAt?x=100&y=200  POST: {x:100,y:200}');
      end
      else
      begin
        DoClick(X, Y, RightClick, DoubleClick);
        ResultJSON.Add('success', True);
        ResultJSON.Add('message', IfThen(DoubleClick, '已双击', IfThen(RightClick, '已右键点击', '已点击')));
        PosObj := TJSONObject.Create;
        PosObj.Add('x', X);
        PosObj.Add('y', Y);
        ResultJSON.Add('position', PosObj);
      end;
    end
    else if Path = '/api/mouse' then
    begin
      GetCursorPos(P);
      ResultJSON.Add('success', True);
      PosObj := TJSONObject.Create;
      PosObj.Add('x', P.X);
      PosObj.Add('y', P.Y);
      ResultJSON.Add('position', PosObj);
      ResultJSON.Add('hint', Format('点击此位置: /api/click/%d/%d', [P.X, P.Y]));
    end
    else if Pos('/api/move/', Path) = 1 then
    begin
      Delete(Path, 1, Length('/api/move/'));
      X := StrToIntDef(Copy(Path, 1, Pos('/', Path) - 1), -1);
      Delete(Path, 1, Pos('/', Path));
      Y := StrToIntDef(Path, -1);
      
      if (X >= 0) and (Y >= 0) then
      begin
        SetCursorPos(X, Y);
        ResultJSON.Add('success', True);
        ResultJSON.Add('message', '已移动鼠标');
        PosObj := TJSONObject.Create;
        PosObj.Add('x', X);
        PosObj.Add('y', Y);
        ResultJSON.Add('position', PosObj);
      end
      else
      begin
        ResultJSON.Add('success', False);
        ResultJSON.Add('error', '坐标格式错误');
        ResultJSON.Add('hint', '正确格式: /api/move/100/200');
      end;
    end
    else if Pos('/api/drag/', Path) = 1 then
    begin
      Delete(Path, 1, Length('/api/drag/'));
      X1 := StrToIntDef(Copy(Path, 1, Pos('/', Path) - 1), -1);
      Delete(Path, 1, Pos('/', Path));
      Y1 := StrToIntDef(Copy(Path, 1, Pos('/', Path) - 1), -1);
      Delete(Path, 1, Pos('/', Path));
      X2 := StrToIntDef(Copy(Path, 1, Pos('/', Path) - 1), -1);
      Delete(Path, 1, Pos('/', Path));
      Y2 := StrToIntDef(Path, -1);
      
      if (X1 >= 0) and (Y1 >= 0) and (X2 >= 0) and (Y2 >= 0) then
      begin
        DoDrag(X1, Y1, X2, Y2);
        ResultJSON.Add('success', True);
        ResultJSON.Add('message', '已拖拽');
        FromObj := TJSONObject.Create;
        FromObj.Add('x', X1);
        FromObj.Add('y', Y1);
        ResultJSON.Add('from', FromObj);
        ToObj := TJSONObject.Create;
        ToObj.Add('x', X2);
        ToObj.Add('y', Y2);
        ResultJSON.Add('to', ToObj);
      end
      else
      begin
        ResultJSON.Add('success', False);
        ResultJSON.Add('error', '坐标格式错误');
        ResultJSON.Add('hint', '正确格式: /api/drag/起始x/起始y/结束x/结束y');
      end;
    end
    else if Path = '/api/windows' then
    begin
      WindowsArray := GetWindowList;
      ResultJSON.Add('success', True);
      ResultJSON.Add('count', WindowsArray.Count);
      ResultJSON.Add('windows', WindowsArray);
    end
    else if (Path = '/api/scanByTitle') or (Pos('/api/scanByTitle?', Path) = 1) then
    begin
      if RequestMethod = 'POST' then
      begin
        Body := ARequest.Content;
        if Length(Body) > 0 then
        begin
          JSON := TJSONObject(GetJSON(Body));
          try
            Title := JSON.Get('title', '');
          finally
            JSON.Free;
          end;
        end;
      end
      else
        Title := GetQueryParam('title');
      
      if Title = '' then
      begin
        ResultJSON.Add('success', False);
        ResultJSON.Add('error', '缺少窗口标题参数');
        ResultJSON.Add('hint', 'GET: /api/scanByTitle?title=窗口标题');
      end
      else
      begin
        TargetHwnd := FindWindowByTitle(Title);
        if TargetHwnd = 0 then
        begin
          ResultJSON.Add('success', False);
          ResultJSON.Add('error', '未找到窗口: ' + Title);
          ResultJSON.Add('hint', '请使用 /api/windows 查看所有可用窗口');
        end
        else
        begin
          ResultJSON.Add('success', True);
          ResultJSON.Add('searchTitle', Title);
          ResultJSON.Add('hwnd', TargetHwnd);
          ResultJSON.Add('message', '找到窗口，请使用 Alt+F 在该窗口激活后扫描');
        end;
      end;
    end
    else if (Path = '/api/clickByTitle') or (Pos('/api/clickByTitle?', Path) = 1) then
    begin
      RightClick := False;
      DoubleClick := False;
      BringToFront := True;
      
      if RequestMethod = 'POST' then
      begin
        Body := ARequest.Content;
        if Length(Body) > 0 then
        begin
          JSON := TJSONObject(GetJSON(Body));
          try
            Title := JSON.Get('title', '');
            X := JSON.Get('x', -1);
            Y := JSON.Get('y', -1);
            RightClick := JSON.Get('right', False) or JSON.Get('rightClick', False);
            DoubleClick := JSON.Get('double', False) or JSON.Get('doubleClick', False);
            BringToFront := JSON.Get('bringToFront', True);
          finally
            JSON.Free;
          end;
        end;
      end
      else
      begin
        Title := GetQueryParam('title');
        X := StrToIntDef(GetQueryParam('x'), -1);
        Y := StrToIntDef(GetQueryParam('y'), -1);
        QueryValue := GetQueryParam('right');
        RightClick := (QueryValue = '1') or (QueryValue = 'true');
        QueryValue := GetQueryParam('double');
        DoubleClick := (QueryValue = '1') or (QueryValue = 'true');
        QueryValue := GetQueryParam('bringtofront');
        BringToFront := (QueryValue <> '0') and (QueryValue <> 'false');
      end;
      
      if Title = '' then
      begin
        ResultJSON.Add('success', False);
        ResultJSON.Add('error', '缺少窗口标题参数');
      end
      else if (X < 0) or (Y < 0) then
      begin
        ResultJSON.Add('success', False);
        ResultJSON.Add('error', '缺少坐标参数');
      end
      else
      begin
        TargetHwnd := FindWindowByTitle(Title);
        if TargetHwnd = 0 then
        begin
          ResultJSON.Add('success', False);
          ResultJSON.Add('error', '未找到窗口: ' + Title);
        end
        else
        begin
          if BringToFront then
          begin
            ActivateWindowByHwnd(TargetHwnd);
            Sleep(50);
          end;
          
          DoClick(X, Y, RightClick, DoubleClick);
          
          ResultJSON.Add('success', True);
          ResultJSON.Add('message', '已点击');
          ResultJSON.Add('searchTitle', Title);
          ResultJSON.Add('hwnd', TargetHwnd);
        end;
      end;
    end
    else if (Path = '/api/activate') or (Pos('/api/activate?', Path) = 1) then
    begin
      Title := GetQueryParam('title');
      
      if Title = '' then
      begin
        ResultJSON.Add('success', False);
        ResultJSON.Add('error', '缺少 title 参数');
        ResultJSON.Add('hint', '正确格式: /api/activate?title=窗口标题');
      end
      else
      begin
        TargetHwnd := FindWindowByTitle(Title);
        if TargetHwnd = 0 then
        begin
          ResultJSON.Add('success', False);
          ResultJSON.Add('error', '未找到窗口: ' + Title);
        end
        else
        begin
          ActivateWindowByHwnd(TargetHwnd);
          ResultJSON.Add('success', True);
          ResultJSON.Add('message', '窗口已激活');
          ResultJSON.Add('hwnd', TargetHwnd);
        end;
      end;
    end
    else if (Path = '/api/input') and (RequestMethod = 'POST') then
    begin
      Body := ARequest.Content;
      JSON := TJSONObject(GetJSON(Body));
      try
        Text := JSON.Get('text', '');
        if Text = '' then
        begin
          ResultJSON.Add('success', False);
          ResultJSON.Add('error', '缺少 text 参数');
        end
        else
        begin
          DoInputText(Text);
          ResultJSON.Add('success', True);
          ResultJSON.Add('message', '已输入');
          ResultJSON.Add('text', Text);
        end;
      finally
        JSON.Free;
      end;
    end
    else if (Path = '/api/show') and (RequestMethod = 'POST') then
    begin
      if Assigned(FOnShowMarkers) then
        FOnShowMarkers();
        
      Sleep(100);
      ScanData := ReadJsonFile(FDataDir + 'scan_result.json');
      if Assigned(ScanData) then
      begin
        AResponse.Content := ScanData.FormatJSON;
        ScanData.Free;
        Exit;
      end
      else
      begin
        ResultJSON.Add('success', True);
        ResultJSON.Add('message', '已触发扫描');
      end;
    end
    else if (Path = '/api/hide') and (RequestMethod = 'POST') then
    begin
      if Assigned(FOnHideMarkers) then
        FOnHideMarkers();
        
      ResultJSON.Add('success', True);
      ResultJSON.Add('message', '已隐藏标签');
    end
    else if Path = '/api/status' then
    begin
      ResultJSON.Add('running', True);
      ScanData := ReadJsonFile(FDataDir + 'scan_result.json');
      ResultJSON.Add('hasData', Assigned(ScanData));
      if Assigned(ScanData) then
        ScanData.Free;
        
      GetCursorPos(P);
      ScreenWidth := GetSystemMetrics(SM_CXSCREEN);
      ScreenHeight := GetSystemMetrics(SM_CYSCREEN);
      
      MouseObj := TJSONObject.Create;
      MouseObj.Add('x', P.X);
      MouseObj.Add('y', P.Y);
      ResultJSON.Add('mousePosition', MouseObj);
      
      ScreenObj := TJSONObject.Create;
      ScreenObj.Add('width', ScreenWidth);
      ScreenObj.Add('height', ScreenHeight);
      ResultJSON.Add('screen', ScreenObj);
    end
    else if (Path = '/api/vma/run') and (RequestMethod = 'POST') then
    begin
      Body := ARequest.Content;
      JSON := TJSONObject(GetJSON(Body));
      try
        Text := JSON.Get('script', '');
        if Text = '' then
        begin
          ResultJSON.Add('success', False);
          ResultJSON.Add('error', '缺少 script 参数');
        end
        else
        begin
          ResultJSON.Free;
          ResultJSON := FVMAEngine.Run(Text);
        end;
      finally
        JSON.Free;
      end;
    end
    else if (Path = '/api/vma/runFile') and (RequestMethod = 'POST') then
    begin
      Body := ARequest.Content;
      JSON := TJSONObject(GetJSON(Body));
      try
        Text := JSON.Get('file', '');
        if Text = '' then
        begin
          ResultJSON.Add('success', False);
          ResultJSON.Add('error', '缺少 file 参数');
        end
        else if not FileExists(Text) then
        begin
          ResultJSON.Add('success', False);
          ResultJSON.Add('error', '文件不存在: ' + Text);
        end
        else
        begin
          ResultJSON.Free;
          ResultJSON := FVMAEngine.RunFile(Text);
        end;
      finally
        JSON.Free;
      end;
    end
    else if Path = '/api/vma/status' then
    begin
      ResultJSON.Free;
      ResultJSON := FVMAEngine.GetStatus;
    end
    else if (Path = '/api/vma/stop') and (RequestMethod = 'POST') then
    begin
      FVMAEngine.Stop;
      ResultJSON.Add('success', True);
      ResultJSON.Add('message', '脚本已停止');
    end
    else if (Path = '/api/vma/pause') and (RequestMethod = 'POST') then
    begin
      FVMAEngine.Pause;
      ResultJSON.Add('success', True);
      ResultJSON.Add('message', '脚本已暂停');
    end
    else if (Path = '/api/vma/resume') and (RequestMethod = 'POST') then
    begin
      FVMAEngine.Resume;
      ResultJSON.Add('success', True);
      ResultJSON.Add('message', '脚本已恢复');
    end
    else
    begin
      ResultJSON.Add('success', False);
      ResultJSON.Add('error', '未知接口: ' + Path);
      ResultJSON.Add('hint', '访问 /api 查看所有可用接口');
    end;
    
    AResponse.Content := ResultJSON.FormatJSON;
  finally
    ResultJSON.Free;
  end;
end;

end.
