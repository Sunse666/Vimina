unit HttpServer;

{$mode objfpc}{$H+}

interface

uses
  Classes, SysUtils, fphttpserver, httpdefs, httpprotocol, fpjson, jsonparser,
  Windows, Config;

type
  TClickLabelEvent = procedure(const ALabel: string) of object;
  TScanRequestEvent = procedure of object;
  TShowMarkersEvent = procedure of object;
  THideMarkersEvent = procedure of object;

  TViminaHttpServer = class(TFPHTTPServer)
  private
    FDataDir: string;
    FOnClickLabel: TClickLabelEvent;
    FOnScanRequest: TScanRequestEvent;
    FOnShowMarkers: TShowMarkersEvent;
    FOnHideMarkers: THideMarkersEvent;
  protected
    procedure HandleRequest(var ARequest: TFPHTTPConnectionRequest;
      var AResponse: TFPHTTPConnectionResponse); override;
    procedure DoRequest(ARequest: TFPHTTPConnectionRequest;
      AResponse: TFPHTTPConnectionResponse);
  public
    constructor Create(AOwner: TComponent; const ADataDir: string);
    property OnClickLabel: TClickLabelEvent
      read FOnClickLabel write FOnClickLabel;
    property OnScanRequest: TScanRequestEvent
      read FOnScanRequest write FOnScanRequest;
    property OnShowMarkers: TShowMarkersEvent
      read FOnShowMarkers write FOnShowMarkers;
    property OnHideMarkers: THideMarkersEvent
      read FOnHideMarkers write FOnHideMarkers;
  end;

  function ReadJsonFile(const FileName: string): TJSONData;
  procedure WriteJsonFile(const FileName: string; JSON: TJSONData);

implementation

uses
  Math, jsonscanner;

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

constructor TViminaHttpServer.Create(AOwner: TComponent; const ADataDir: string);
begin
  inherited Create(AOwner);
  FDataDir := ADataDir;
  Port := 51401;
  Threaded := True;
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
  JSON, ResultJSON: TJSONObject;
  ScanData, LabelData: TJSONData;
  Body: string;
  X, Y: Integer;
  LabelText: string;
  LabelObj: TJSONObject;
  PosObj: TJSONObject;
  P: TPoint;
  OrigPos: TPoint;
  
  procedure DoClick(AX, AY: Integer; RightClick: Boolean = False; DoubleClick: Boolean = False);
  begin
    GetCursorPos(OrigPos);
    
    SetCursorPos(AX, AY);
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
      
    Sleep(30);
    SetCursorPos(OrigPos.X, OrigPos.Y);
  end;
  
begin
  Path := ARequest.PathInfo;
  
  // CORS 头
  AResponse.SetCustomHeader('Access-Control-Allow-Origin', '*');
  AResponse.SetCustomHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  AResponse.SetCustomHeader('Access-Control-Allow-Headers', 'Content-Type');
  AResponse.ContentType := 'application/json; charset=utf-8';
  
  // OPTIONS 请求
  if ARequest.Method = 'OPTIONS' then
  begin
    AResponse.Content := '';
    Exit;
  end;
  
  ResultJSON := TJSONObject.Create;
  try
    // API 路由
    if (Path = '/') or (Path = '/api') then
    begin
      ResultJSON.Add('name', 'Vimina API');
      ResultJSON.Add('version', '1.1');
      ResultJSON.Add('description', '桌面控件自动化操作接口');
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
      end;
    end
    else if (Path = '/api/click') and (ARequest.Method = 'POST') then
    begin
      Body := ARequest.Content;
      JSON := TJSONObject(GetJSON(Body));
      try
        LabelText := UpperCase(JSON.Get('label', ''));
        
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
                
                DoClick(X, Y);
                
                ResultJSON.Add('success', True);
                ResultJSON.Add('message', '已点击控件');
                ResultJSON.Add('label', LabelText);
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
          end;
        end;
      finally
        JSON.Free;
      end;
    end
    else if Pos('/api/click/', Path) = 1 then
    begin
      // 解析 /api/click/100/200
      Delete(Path, 1, Length('/api/click/'));
      X := StrToIntDef(Copy(Path, 1, Pos('/', Path) - 1), -1);
      Delete(Path, 1, Pos('/', Path));
      Y := StrToIntDef(Path, -1);
      
      if (X >= 0) and (Y >= 0) then
      begin
        DoClick(X, Y);
        ResultJSON.Add('success', True);
        ResultJSON.Add('message', '已点击');
      end
      else
      begin
        ResultJSON.Add('success', False);
        ResultJSON.Add('error', '坐标格式错误');
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
        DoClick(X, Y, True);
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
    else if Path = '/api/mouse' then
    begin
      GetCursorPos(P);
      ResultJSON.Add('success', True);
      PosObj := TJSONObject.Create;
      ResultJSON.Add('position', PosObj);
      PosObj.Add('x', P.X);
      PosObj.Add('y', P.Y);
    end
    else if (Path = '/api/show') and (ARequest.Method = 'POST') then
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
    else if (Path = '/api/hide') and (ARequest.Method = 'POST') then
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
    end
    else
    begin
      ResultJSON.Add('success', False);
      ResultJSON.Add('error', '未知接口: ' + Path);
    end;
    
    AResponse.Content := ResultJSON.FormatJSON;
  finally
    ResultJSON.Free;
  end;
end;

end.