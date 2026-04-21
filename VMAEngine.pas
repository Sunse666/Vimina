unit VMAEngine;

{$mode objfpc}{$H+}
{$codepage UTF8}

interface

uses
  Classes, SysUtils, fpjson, jsonparser, Windows, Messages, Config, UIAutomation;

type
  TVMAVariable = record
    Name: string;
    ValueType: (vtNone, vtNumber, vtString, vtBoolean, vtArray);
    NumberValue: Double;
    StringValue: string;
    BooleanValue: Boolean;
    ArrayValue: TJSONArray;
  end;
  PVMAVariable = ^TVMAVariable;

  TVMAFunction = record
    Name: string;
    Params: string;
    Body: TStringList;
    StartLine: Integer;
  end;
  PVMAFunction = ^TVMAFunction;

  TVMALoopInfo = record
    LoopType: (ltTimes, ltWhile, ltFor, ltForeach);
    Count: Integer;
    Current: Integer;
    Condition: string;
    StartLine: Integer;
    VarName: string;
    StartVal: Integer;
    EndVal: Integer;
    StepVal: Integer;
    ArrName: string;
    Index: Integer;
  end;
  PVMALoopInfo = ^TVMALoopInfo;

  TVMAEngine = class
  private
    FVariables: array of TVMAVariable;
    FVariableCount: Integer;
    FFunctions: array of TVMAFunction;
    FFunctionCount: Integer;
    FLoopStack: array of TVMALoopInfo;
    FLoopStackCount: Integer;
    FLabels: array of record
      Name: string;
      LineNum: Integer;
    end;
    FLabelCount: Integer;
    FLog: TStringList;
    FLines: TStringList;
    FCurrentLine: Integer;
    FRunning: Boolean;
    FPaused: Boolean;
    FShouldStop: Boolean;
    FDataDir: string;
    FConfigFile: string;
    FUIAuto: TUIAutomationHelper;
    
    function FindVariable(const Name: string): Integer;
    function GetVariable(const Name: string): TVMAVariable;
    procedure SetVariable(const Name: string; const Value: TVMAVariable);
    function CreateNumberVar(Value: Double): TVMAVariable;
    function CreateStringVar(const Value: string): TVMAVariable;
    function CreateBooleanVar(Value: Boolean): TVMAVariable;
    function CreateArrayVar: TVMAVariable;
    
    function IsQuotedString(const S: string): Boolean;
    function UnquoteString(const S: string): string;
    function ParseScript(const Script: string): Boolean;
    function EvalExpression(const Expr: string): TVMAVariable;
    function EvalCondition(const Condition: string): Boolean;
    function ParseArgs(const ArgsStr: string): TStringList;
    function ParseNamedArgs(const ArgsStr: string): TJSONObject;
    
    function ExecuteCommand(const Line: string): Boolean;
    
    procedure DoSleep(MS: Integer);
    procedure DoClick(X, Y: Integer; RightClick, DoubleClick: Boolean; UseBackend: Boolean = False);
    procedure DoClickByTitle(const Title: string; X, Y: Integer; RightClick, DoubleClick, UseBackend, BringToFront: Boolean);
    procedure DoMoveTo(X, Y: Integer);
    procedure DoDrag(X1, Y1, X2, Y2: Integer);
    procedure DoInputText(const Text: string);
    procedure DoKeyPress(const Keys: string);
    procedure DoKeyDown(const Key: string);
    procedure DoKeyUp(const Key: string);
    procedure DoActivateWindow(const Title: string);
    function DoFindWindow(const Title: string): HWND;
    procedure DoCloseWindow(const Title: string);
    procedure DoMinimizeWindow(const Title: string);
    procedure DoMaximizeWindow(const Title: string);
    procedure DoRestoreWindow(const Title: string);
    function DoWaitForWindow(const Title: string; Timeout: Integer): Boolean;
    function DoScreenshot: string;
    
    function GetMousePos: TPoint;
    function GetScreenSize: TPoint;
    function GetWindowList: TJSONArray;
    function FindWindowByTitle(const Title: string): HWND;
    
    procedure PushLoop(const LoopInfo: TVMALoopInfo);
    function PopLoop: TVMALoopInfo;
    function PeekLoop: PVMALoopInfo;
    
    function FindFunction(const Name: string): Integer;
    function CallUserFunction(const FuncName, ArgsStr: string): TVMAVariable;
  public
    constructor Create(const ADataDir, AConfigFile: string);
    destructor Destroy; override;
    
    function Run(const Script: string): TJSONObject;
    function RunFile(const FileName: string): TJSONObject;
    procedure Stop;
    procedure Pause;
    procedure Resume;
    function GetStatus: TJSONObject;
    
    property Running: Boolean read FRunning;
    property Paused: Boolean read FPaused;
    property CurrentLine: Integer read FCurrentLine;
    property Log: TStringList read FLog;
  end;

implementation

uses
  Math, StrUtils, DateUtils, ShellAPI, Graphics, LCLIntf, LCLType, IntfGraphics, FPImage;

constructor TVMAEngine.Create(const ADataDir, AConfigFile: string);
begin
  inherited Create;
  FDataDir := ADataDir;
  FConfigFile := AConfigFile;
  FVariableCount := 0;
  FFunctionCount := 0;
  FLoopStackCount := 0;
  FLabelCount := 0;
  FLog := TStringList.Create;
  FLines := TStringList.Create;
  FRunning := False;
  FPaused := False;
  FShouldStop := False;
  FCurrentLine := 0;
  FUIAuto := TUIAutomationHelper.Create;
end;

destructor TVMAEngine.Destroy;
var
  I: Integer;
begin
  for I := 0 to FVariableCount - 1 do
    if FVariables[I].ValueType = vtArray then
      FVariables[I].ArrayValue.Free;
      
  for I := 0 to FFunctionCount - 1 do
    FFunctions[I].Body.Free;
    
  FLog.Free;
  FLines.Free;
  FUIAuto.Free;
  inherited Destroy;
end;

function TVMAEngine.FindVariable(const Name: string): Integer;
var
  I: Integer;
begin
  Result := -1;
  for I := 0 to FVariableCount - 1 do
    if SameText(FVariables[I].Name, Name) then
    begin
      Result := I;
      Exit;
    end;
end;

function TVMAEngine.GetVariable(const Name: string): TVMAVariable;
var
  Idx: Integer;
begin
  Idx := FindVariable(Name);
  if Idx >= 0 then
    Result := FVariables[Idx]
  else
  begin
    Result.ValueType := vtNone;
    Result.NumberValue := 0;
    Result.StringValue := '';
    Result.BooleanValue := False;
  end;
end;

procedure TVMAEngine.SetVariable(const Name: string; const Value: TVMAVariable);
var
  Idx: Integer;
begin
  Idx := FindVariable(Name);
  if Idx >= 0 then
  begin
    if FVariables[Idx].ValueType = vtArray then
      FVariables[Idx].ArrayValue.Free;
    FVariables[Idx] := Value;
    FVariables[Idx].Name := Name;
  end
  else
  begin
    if FVariableCount >= Length(FVariables) then
      SetLength(FVariables, FVariableCount + 100);
    FVariables[FVariableCount] := Value;
    FVariables[FVariableCount].Name := Name;
    Inc(FVariableCount);
  end;
end;

function TVMAEngine.CreateNumberVar(Value: Double): TVMAVariable;
begin
  Result.ValueType := vtNumber;
  Result.NumberValue := Value;
  Result.StringValue := '';
  Result.BooleanValue := Value <> 0;
end;

function TVMAEngine.CreateStringVar(const Value: string): TVMAVariable;
begin
  Result.ValueType := vtString;
  Result.NumberValue := StrToFloatDef(Value, 0);
  Result.StringValue := Value;
  Result.BooleanValue := Value <> '';
end;

function TVMAEngine.CreateBooleanVar(Value: Boolean): TVMAVariable;
begin
  Result.ValueType := vtBoolean;
  Result.NumberValue := Ord(Value);
  Result.StringValue := IfThen(Value, 'true', 'false');
  Result.BooleanValue := Value;
end;

function TVMAEngine.CreateArrayVar: TVMAVariable;
begin
  Result.ValueType := vtArray;
  Result.NumberValue := 0;
  Result.StringValue := '';
  Result.BooleanValue := False;
  Result.ArrayValue := TJSONArray.Create;
end;

function TVMAEngine.IsQuotedString(const S: string): Boolean;
begin
  Result := (Length(S) >= 2) and 
    (((S[1] = '"') and (S[Length(S)] = '"')) or
     ((S[1] = '''') and (S[Length(S)] = '''')));
end;

function TVMAEngine.UnquoteString(const S: string): string;
begin
  if IsQuotedString(S) then
    Result := Copy(S, 2, Length(S) - 2)
  else
    Result := S;
end;

function TVMAEngine.ParseScript(const Script: string): Boolean;
var
  SL: TStringList;
  I: Integer;
  Line, TrimmedLine: string;
  LabelMatch: string;
begin
  Result := True;
  FLines.Clear;
  FLabelCount := 0;
  
  SL := TStringList.Create;
  try
    SL.Text := Script;
    for I := 0 to SL.Count - 1 do
    begin
      Line := SL[I];
      TrimmedLine := Trim(Line);
      
      if (TrimmedLine = '') or (Copy(TrimmedLine, 1, 2) = '//') then
        Continue;
        
      LabelMatch := '';
      if (Length(TrimmedLine) > 1) and (TrimmedLine[Length(TrimmedLine)] = ':') then
      begin
        LabelMatch := Copy(TrimmedLine, 1, Length(TrimmedLine) - 1);
        if FLabelCount >= Length(FLabels) then
          SetLength(FLabels, FLabelCount + 50);
        FLabels[FLabelCount].Name := LabelMatch;
        FLabels[FLabelCount].LineNum := FLines.Count;
        Inc(FLabelCount);
        Continue;
      end;
      
      FLines.Add(TrimmedLine);
    end;
  finally
    SL.Free;
  end;
end;

function TVMAEngine.EvalExpression(const Expr: string): TVMAVariable;
var
  TrimmedExpr: string;
  VarIdx: Integer;
  VarVal: TVMAVariable;
  ArrayMatch, ArrayName, ArrayIndex: string;
  IndexVal: TVMAVariable;
  IndexInt: Integer;
  OpMatch: string;
  LeftExpr, RightExpr, Op: string;
  LeftVal, RightVal: TVMAVariable;
  FuncMatch, FuncName, FuncArgs: string;
  ArgsList: TStringList;
  ArgsArr: array of Double;
  I, J: Integer;
  MinVal, MaxVal: Double;
  P: TPoint;
  Title: string;
  H: HWND;
begin
  Result.ValueType := vtNone;
  Result.NumberValue := 0;
  Result.StringValue := '';
  Result.BooleanValue := False;
  
  TrimmedExpr := Trim(Expr);
  if TrimmedExpr = '' then
    Exit;
    
  if IsQuotedString(TrimmedExpr) then
  begin
    Result := CreateStringVar(UnquoteString(TrimmedExpr));
    Exit;
  end;
  
  if SameText(TrimmedExpr, 'true') then
  begin
    Result := CreateBooleanVar(True);
    Exit;
  end;
  
  if SameText(TrimmedExpr, 'false') then
  begin
    Result := CreateBooleanVar(False);
    Exit;
  end;
  
  if SameText(TrimmedExpr, 'null') or SameText(TrimmedExpr, 'nil') then
    Exit;
  
  if (Length(TrimmedExpr) > 0) and (TrimmedExpr[1] in ['0'..'9', '-', '+', '.']) then
  begin
    if TryStrToFloat(TrimmedExpr, Result.NumberValue) then
    begin
      Result.ValueType := vtNumber;
      Exit;
    end;
  end;
  
  VarIdx := FindVariable(TrimmedExpr);
  if VarIdx >= 0 then
  begin
    Result := FVariables[VarIdx];
    Exit;
  end;
  
  if (Pos('[', TrimmedExpr) > 0) and (TrimmedExpr[Length(TrimmedExpr)] = ']') then
  begin
    I := Pos('[', TrimmedExpr);
    ArrayName := Copy(TrimmedExpr, 1, I - 1);
    ArrayIndex := Copy(TrimmedExpr, I + 1, Length(TrimmedExpr) - I - 1);
    
    IndexVal := EvalExpression(ArrayIndex);
    IndexInt := Trunc(IndexVal.NumberValue);
    
    VarIdx := FindVariable(ArrayName);
    if (VarIdx >= 0) and (FVariables[VarIdx].ValueType = vtArray) then
    begin
      if Assigned(FVariables[VarIdx].ArrayValue) and 
         (IndexInt >= 1) and (IndexInt <= FVariables[VarIdx].ArrayValue.Count) then
      begin
        case FVariables[VarIdx].ArrayValue.Items[IndexInt - 1].JSONType of
          jtNumber: Result := CreateNumberVar(FVariables[VarIdx].ArrayValue.Items[IndexInt - 1].AsFloat);
          jtString: Result := CreateStringVar(FVariables[VarIdx].ArrayValue.Items[IndexInt - 1].AsString);
          jtBoolean: Result := CreateBooleanVar(FVariables[VarIdx].ArrayValue.Items[IndexInt - 1].AsBoolean);
        end;
      end;
      Exit;
    end;
  end;
  
  for I := Length(TrimmedExpr) downto 1 do
  begin
    if TrimmedExpr[I] in ['+', '-', '*', '/', '%'] then
    begin
      LeftExpr := Copy(TrimmedExpr, 1, I - 1);
      RightExpr := Copy(TrimmedExpr, I + 1, MaxInt);
      Op := TrimmedExpr[I];
      
      if (Op = '-') and ((LeftExpr = '') or (RightExpr = '')) then
        Continue;
        
      LeftVal := EvalExpression(LeftExpr);
      RightVal := EvalExpression(RightExpr);
      
      case Op[1] of
        '+': Result := CreateNumberVar(LeftVal.NumberValue + RightVal.NumberValue);
        '-': Result := CreateNumberVar(LeftVal.NumberValue - RightVal.NumberValue);
        '*': Result := CreateNumberVar(LeftVal.NumberValue * RightVal.NumberValue);
        '/': if RightVal.NumberValue <> 0 then
               Result := CreateNumberVar(LeftVal.NumberValue / RightVal.NumberValue);
        '%': if RightVal.NumberValue <> 0 then
               Result := CreateNumberVar(Frac(LeftVal.NumberValue / RightVal.NumberValue) * RightVal.NumberValue);
      end;
      Exit;
    end;
  end;
  
  if (Pos('(', TrimmedExpr) > 0) and (TrimmedExpr[Length(TrimmedExpr)] = ')') then
  begin
    I := Pos('(', TrimmedExpr);
    FuncName := Copy(TrimmedExpr, 1, I - 1);
    FuncArgs := Copy(TrimmedExpr, I + 1, Length(TrimmedExpr) - I - 1);
    
    if SameText(FuncName, 'getMousePos') then
    begin
      P := GetMousePos;
      Result := CreateStringVar(Format('%d,%d', [P.X, P.Y]));
      SetVariable('mouseX', CreateNumberVar(P.X));
      SetVariable('mouseY', CreateNumberVar(P.Y));
      Exit;
    end;
    
    if SameText(FuncName, 'getScreenSize') then
    begin
      P := GetScreenSize;
      Result := CreateStringVar(Format('%d,%d', [P.X, P.Y]));
      SetVariable('screenWidth', CreateNumberVar(P.X));
      SetVariable('screenHeight', CreateNumberVar(P.Y));
      Exit;
    end;
    
    if SameText(FuncName, 'rand') or SameText(FuncName, 'random') then
    begin
      ArgsList := ParseArgs(FuncArgs);
      try
        if ArgsList.Count >= 2 then
        begin
          LeftVal := EvalExpression(ArgsList[0]);
          RightVal := EvalExpression(ArgsList[1]);
          Result := CreateNumberVar(Random(Trunc(RightVal.NumberValue - LeftVal.NumberValue + 1)) + LeftVal.NumberValue);
        end
        else
          Result := CreateNumberVar(Random(101));
      finally
        ArgsList.Free;
      end;
      Exit;
    end;
    
    if SameText(FuncName, 'abs') then
    begin
      VarVal := EvalExpression(FuncArgs);
      Result := CreateNumberVar(Abs(VarVal.NumberValue));
      Exit;
    end;
    
    if SameText(FuncName, 'floor') then
    begin
      VarVal := EvalExpression(FuncArgs);
      Result := CreateNumberVar(Floor(VarVal.NumberValue));
      Exit;
    end;
    
    if SameText(FuncName, 'ceil') then
    begin
      VarVal := EvalExpression(FuncArgs);
      Result := CreateNumberVar(Ceil(VarVal.NumberValue));
      Exit;
    end;
    
    if SameText(FuncName, 'min') then
    begin
      ArgsList := ParseArgs(FuncArgs);
      try
        SetLength(ArgsArr, ArgsList.Count);
        for I := 0 to ArgsList.Count - 1 do
          ArgsArr[I] := EvalExpression(ArgsList[I]).NumberValue;
        MinVal := ArgsArr[0];
        for I := 1 to High(ArgsArr) do
          if ArgsArr[I] < MinVal then MinVal := ArgsArr[I];
        Result := CreateNumberVar(MinVal);
      finally
        ArgsList.Free;
      end;
      Exit;
    end;
    
    if SameText(FuncName, 'max') then
    begin
      ArgsList := ParseArgs(FuncArgs);
      try
        SetLength(ArgsArr, ArgsList.Count);
        for I := 0 to ArgsList.Count - 1 do
          ArgsArr[I] := EvalExpression(ArgsList[I]).NumberValue;
        MaxVal := ArgsArr[0];
        for I := 1 to High(ArgsArr) do
          if ArgsArr[I] > MaxVal then MaxVal := ArgsArr[I];
        Result := CreateNumberVar(MaxVal);
      finally
        ArgsList.Free;
      end;
      Exit;
    end;
    
    if SameText(FuncName, 'toInt') then
    begin
      VarVal := EvalExpression(FuncArgs);
      Result := CreateNumberVar(Trunc(VarVal.NumberValue));
      Exit;
    end;
    
    if SameText(FuncName, 'toString') then
    begin
      VarVal := EvalExpression(FuncArgs);
      Result := CreateStringVar(VarVal.StringValue);
      Exit;
    end;
    
    if SameText(FuncName, 'length') or SameText(FuncName, 'len') then
    begin
      VarVal := EvalExpression(FuncArgs);
      if VarVal.ValueType = vtArray then
        Result := CreateNumberVar(VarVal.ArrayValue.Count)
      else if VarVal.ValueType = vtString then
        Result := CreateNumberVar(Length(VarVal.StringValue))
      else
        Result := CreateNumberVar(0);
      Exit;
    end;
    
    if SameText(FuncName, 'windowExists') or SameText(FuncName, 'windowexists') then
    begin
      if IsQuotedString(FuncArgs) then
        Title := UnquoteString(FuncArgs)
      else
      begin
        VarVal := EvalExpression(FuncArgs);
        Title := VarVal.StringValue;
      end;
      H := FindWindowByTitle(Title);
      Result := CreateBooleanVar(H <> 0);
      Exit;
    end;
    
    if SameText(FuncName, 'windowActive') or SameText(FuncName, 'windowactive') then
    begin
      if IsQuotedString(FuncArgs) then
        Title := UnquoteString(FuncArgs)
      else
      begin
        VarVal := EvalExpression(FuncArgs);
        Title := VarVal.StringValue;
      end;
      H := FindWindowByTitle(Title);
      Result := CreateBooleanVar((H <> 0) and (GetForegroundWindow = H));
      Exit;
    end;
    
    if SameText(FuncName, 'isArray') then
    begin
      VarVal := EvalExpression(FuncArgs);
      Result := CreateBooleanVar(VarVal.ValueType = vtArray);
      Exit;
    end;
    
    if SameText(FuncName, 'type') then
    begin
      VarVal := EvalExpression(FuncArgs);
      case VarVal.ValueType of
        vtNumber: Result := CreateStringVar('number');
        vtString: Result := CreateStringVar('string');
        vtBoolean: Result := CreateStringVar('boolean');
        vtArray: Result := CreateStringVar('array');
      else
        Result := CreateStringVar('null');
      end;
      Exit;
    end;
    
    if FindFunction(FuncName) >= 0 then
    begin
      Result := CallUserFunction(FuncName, FuncArgs);
      Exit;
    end;
  end;
  
  Result := CreateStringVar(TrimmedExpr);
end;

function TVMAEngine.EvalCondition(const Condition: string): Boolean;
var
  TrimmedCond: string;
  LeftExpr, RightExpr, Op: string;
  LeftVal, RightVal: TVMAVariable;
  I, J: Integer;
  InnerCond: string;
begin
  Result := False;
  TrimmedCond := Trim(Condition);
  
  if TrimmedCond = '' then
    Exit;
    
  if SameText(TrimmedCond, 'true') then
  begin
    Result := True;
    Exit;
  end;
  
  if SameText(TrimmedCond, 'false') then
    Exit;
  
  I := Pos(' or ', LowerCase(TrimmedCond));
  if I > 0 then
  begin
    Result := EvalCondition(Copy(TrimmedCond, 1, I - 1)) or
              EvalCondition(Copy(TrimmedCond, I + 4, MaxInt));
    Exit;
  end;
  
  I := Pos(' and ', LowerCase(TrimmedCond));
  if I > 0 then
  begin
    Result := EvalCondition(Copy(TrimmedCond, 1, I - 1)) and
              EvalCondition(Copy(TrimmedCond, I + 5, MaxInt));
    Exit;
  end;
  
  if SameText(Copy(TrimmedCond, 1, 4), 'not ') then
  begin
    Result := not EvalCondition(Copy(TrimmedCond, 5, MaxInt));
    Exit;
  end;
  
  if (Length(TrimmedCond) >= 2) and (TrimmedCond[1] = '(') and (TrimmedCond[Length(TrimmedCond)] = ')') then
  begin
    Result := EvalCondition(Copy(TrimmedCond, 2, Length(TrimmedCond) - 2));
    Exit;
  end;
  
  for I := 1 to Length(TrimmedCond) - 1 do
  begin
    if (TrimmedCond[I] = '=') and (TrimmedCond[I + 1] = '=') then
    begin
      LeftExpr := Copy(TrimmedCond, 1, I - 1);
      RightExpr := Copy(TrimmedCond, I + 2, MaxInt);
      LeftVal := EvalExpression(LeftExpr);
      RightVal := EvalExpression(RightExpr);
      if (LeftVal.ValueType = vtNumber) and (RightVal.ValueType = vtNumber) then
        Result := LeftVal.NumberValue = RightVal.NumberValue
      else
        Result := LeftVal.StringValue = RightVal.StringValue;
      Exit;
    end;
    
    if (TrimmedCond[I] = '!') and (TrimmedCond[I + 1] = '=') then
    begin
      LeftExpr := Copy(TrimmedCond, 1, I - 1);
      RightExpr := Copy(TrimmedCond, I + 2, MaxInt);
      LeftVal := EvalExpression(LeftExpr);
      RightVal := EvalExpression(RightExpr);
      if (LeftVal.ValueType = vtNumber) and (RightVal.ValueType = vtNumber) then
        Result := LeftVal.NumberValue <> RightVal.NumberValue
      else
        Result := LeftVal.StringValue <> RightVal.StringValue;
      Exit;
    end;
    
    if (TrimmedCond[I] = '<') and ((I = Length(TrimmedCond)) or (TrimmedCond[I + 1] <> '=')) then
    begin
      LeftExpr := Copy(TrimmedCond, 1, I - 1);
      RightExpr := Copy(TrimmedCond, I + 1, MaxInt);
      LeftVal := EvalExpression(LeftExpr);
      RightVal := EvalExpression(RightExpr);
      Result := LeftVal.NumberValue < RightVal.NumberValue;
      Exit;
    end;
    
    if (TrimmedCond[I] = '>') and ((I = Length(TrimmedCond)) or (TrimmedCond[I + 1] <> '=')) then
    begin
      LeftExpr := Copy(TrimmedCond, 1, I - 1);
      RightExpr := Copy(TrimmedCond, I + 1, MaxInt);
      LeftVal := EvalExpression(LeftExpr);
      RightVal := EvalExpression(RightExpr);
      Result := LeftVal.NumberValue > RightVal.NumberValue;
      Exit;
    end;
    
    if (TrimmedCond[I] = '<') and (I < Length(TrimmedCond)) and (TrimmedCond[I + 1] = '=') then
    begin
      LeftExpr := Copy(TrimmedCond, 1, I - 1);
      RightExpr := Copy(TrimmedCond, I + 2, MaxInt);
      LeftVal := EvalExpression(LeftExpr);
      RightVal := EvalExpression(RightExpr);
      Result := LeftVal.NumberValue <= RightVal.NumberValue;
      Exit;
    end;
    
    if (TrimmedCond[I] = '>') and (I < Length(TrimmedCond)) and (TrimmedCond[I + 1] = '=') then
    begin
      LeftExpr := Copy(TrimmedCond, 1, I - 1);
      RightExpr := Copy(TrimmedCond, I + 2, MaxInt);
      LeftVal := EvalExpression(LeftExpr);
      RightVal := EvalExpression(RightExpr);
      Result := LeftVal.NumberValue >= RightVal.NumberValue;
      Exit;
    end;
  end;
  
  LeftVal := EvalExpression(TrimmedCond);
  if LeftVal.ValueType = vtBoolean then
    Result := LeftVal.BooleanValue
  else if LeftVal.ValueType = vtNumber then
    Result := LeftVal.NumberValue <> 0
  else
    Result := LeftVal.StringValue <> '';
end;

function TVMAEngine.ParseArgs(const ArgsStr: string): TStringList;
var
  InString: Boolean;
  StringChar: Char;
  Current: string;
  InParen: Integer;
  I: Integer;
  Ch: Char;
begin
  Result := TStringList.Create;
  InString := False;
  StringChar := #0;
  Current := '';
  InParen := 0;
  
  for I := 1 to Length(ArgsStr) do
  begin
    Ch := ArgsStr[I];
    
    if not InString and ((Ch = '"') or (Ch = '''')) then
    begin
      InString := True;
      StringChar := Ch;
      Current := Current + Ch;
    end
    else if InString and (Ch = StringChar) then
    begin
      InString := False;
      Current := Current + Ch;
    end
    else if not InString and (Ch = '(') then
    begin
      Inc(InParen);
      Current := Current + Ch;
    end
    else if not InString and (Ch = ')') then
    begin
      Dec(InParen);
      Current := Current + Ch;
    end
    else if not InString and (Ch = ',') and (InParen = 0) then
    begin
      Result.Add(Trim(Current));
      Current := '';
    end
    else
      Current := Current + Ch;
  end;
  
  if Trim(Current) <> '' then
    Result.Add(Trim(Current));
end;

function TVMAEngine.ParseNamedArgs(const ArgsStr: string): TJSONObject;
var
  ArgsList: TStringList;
  I, EqPos: Integer;
  Arg, Name, Value: string;
begin
  Result := TJSONObject.Create;
  ArgsList := ParseArgs(ArgsStr);
  try
    for I := 0 to ArgsList.Count - 1 do
    begin
      Arg := ArgsList[I];
      EqPos := Pos('=', Arg);
      if EqPos > 0 then
      begin
        Name := Trim(Copy(Arg, 1, EqPos - 1));
        Value := Trim(Copy(Arg, EqPos + 1, MaxInt));
        if (Name <> '') and (Value <> '') then
          Result.Add(Name, Value)
        else
          Result.Add(IntToStr(I), Arg);
      end
      else
        Result.Add(IntToStr(I), Arg);
    end;
  finally
    ArgsList.Free;
  end;
end;

procedure TVMAEngine.DoSleep(MS: Integer);
var
  StartTime: TDateTime;
begin
  StartTime := Now;
  while (MilliSecondsBetween(Now, StartTime) < MS) and not FShouldStop do
  begin
    while FPaused and not FShouldStop do
      Sleep(10);
    Sleep(10);
  end;
end;

procedure TVMAEngine.DoClick(X, Y: Integer; RightClick, DoubleClick: Boolean; UseBackend: Boolean);
var
  OrigPos: TPoint;
begin
  GetCursorPos(OrigPos);
  
  if UseBackend then
  begin
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
  end
  else
  begin
    SetCursorPos(X, Y);
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
end;

procedure TVMAEngine.DoClickByTitle(const Title: string; X, Y: Integer; 
  RightClick, DoubleClick, UseBackend, BringToFront: Boolean);
var
  H: HWND;
  R: TRect;
begin
  H := FindWindowByTitle(Title);
  if H = 0 then
  begin
    FLog.Add('未找到窗口: ' + Title);
    Exit;
  end;
  
  if BringToFront then
  begin
    if IsIconic(H) then
      ShowWindow(H, SW_RESTORE);
    SetForegroundWindow(H);
    Sleep(50);
  end;
  
  if UseBackend then
  begin
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
  end
  else
  begin
    GetWindowRect(H, R);
    DoClick(R.Left + X, R.Top + Y, RightClick, DoubleClick, False);
  end;
  
  FLog.Add(Format('点击窗口: %s (%s)', [Title, IfThen(UseBackend, '后台', '前台')]));
end;

procedure TVMAEngine.DoMoveTo(X, Y: Integer);
begin
  SetCursorPos(X, Y);
end;

procedure TVMAEngine.DoDrag(X1, Y1, X2, Y2: Integer);
var
  I, StepX, StepY: Integer;
begin
  SetCursorPos(X1, Y1);
  Sleep(50);
  mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
  Sleep(50);
  
  StepX := (X2 - X1) div 10;
  StepY := (Y2 - Y1) div 10;
  for I := 1 to 10 do
  begin
    SetCursorPos(X1 + StepX * I, Y1 + StepY * I);
    Sleep(10);
  end;
  
  SetCursorPos(X2, Y2);
  Sleep(50);
  mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
  Sleep(30);
end;

procedure TVMAEngine.DoInputText(const Text: string);
var
  I: Integer;
  VK: Word;
  InputRec: TInput;
begin
  for I := 1 to Length(Text) do
  begin
    VK := VkKeyScan(Text[I]);
    if VK <> 0 then
    begin
      FillChar(InputRec, SizeOf(InputRec), 0);
      InputRec.ki.wVk := VK and $FF;
      InputRec.ki.dwFlags := 0;
      SendInput(1, @InputRec, SizeOf(InputRec));
      
      InputRec.ki.dwFlags := KEYEVENTF_KEYUP;
      SendInput(1, @InputRec, SizeOf(InputRec));
      Sleep(10);
    end;
  end;
end;

procedure TVMAEngine.DoKeyPress(const Keys: string);
var
  Parts: TStringList;
  I: Integer;
  VK: Word;
  InputRec: TInput;
  
  procedure PressKey(KeyVK: Word);
  begin
    FillChar(InputRec, SizeOf(InputRec), 0);
    InputRec.ki.wVk := KeyVK;
    InputRec.ki.dwFlags := 0;
    SendInput(1, @InputRec, SizeOf(InputRec));
    
    InputRec.ki.dwFlags := KEYEVENTF_KEYUP;
    SendInput(1, @InputRec, SizeOf(InputRec));
    Sleep(10);
  end;
  
begin
  if Pos('+', Keys) > 0 then
  begin
    Parts := TStringList.Create;
    try
      Parts.Delimiter := '+';
      Parts.StrictDelimiter := True;
      Parts.DelimitedText := Keys;
      
      for I := 0 to Parts.Count - 2 do
      begin
        VK := VkKeyScan(Parts[I][1]);
        if VK <> 0 then
        begin
          FillChar(InputRec, SizeOf(InputRec), 0);
          InputRec.ki.wVk := VK and $FF;
          InputRec.ki.dwFlags := 0;
          SendInput(1, @InputRec, SizeOf(InputRec));
        end;
      end;
      
      if Parts.Count > 0 then
        PressKey(VkKeyScan(Parts[Parts.Count - 1][1]));
      
      for I := Parts.Count - 2 downto 0 do
      begin
        VK := VkKeyScan(Parts[I][1]);
        if VK <> 0 then
        begin
          FillChar(InputRec, SizeOf(InputRec), 0);
          InputRec.ki.wVk := VK and $FF;
          InputRec.ki.dwFlags := KEYEVENTF_KEYUP;
          SendInput(1, @InputRec, SizeOf(InputRec));
        end;
      end;
    finally
      Parts.Free;
    end;
  end
  else
  begin
    VK := VkKeyScan(Keys[1]);
    if VK <> 0 then
    begin
      FillChar(InputRec, SizeOf(InputRec), 0);
      InputRec.ki.wVk := VK and $FF;
      InputRec.ki.dwFlags := 0;
      SendInput(1, @InputRec, SizeOf(InputRec));
      
      InputRec.ki.dwFlags := KEYEVENTF_KEYUP;
      SendInput(1, @InputRec, SizeOf(InputRec));
    end;
  end;
end;

procedure TVMAEngine.DoKeyDown(const Key: string);
var
  VK: Word;
  InputRec: TInput;
begin
  VK := VkKeyScan(Key[1]);
  if VK <> 0 then
  begin
    FillChar(InputRec, SizeOf(InputRec), 0);
    InputRec.ki.wVk := VK and $FF;
    InputRec.ki.dwFlags := 0;
    SendInput(1, @InputRec, SizeOf(InputRec));
  end;
end;

procedure TVMAEngine.DoKeyUp(const Key: string);
var
  VK: Word;
  InputRec: TInput;
begin
  VK := VkKeyScan(Key[1]);
  if VK <> 0 then
  begin
    FillChar(InputRec, SizeOf(InputRec), 0);
    InputRec.ki.wVk := VK and $FF;
    InputRec.ki.dwFlags := KEYEVENTF_KEYUP;
    SendInput(1, @InputRec, SizeOf(InputRec));
  end;
end;

procedure TVMAEngine.DoActivateWindow(const Title: string);
var
  H: HWND;
begin
  H := FindWindowByTitle(Title);
  if H <> 0 then
  begin
    if IsIconic(H) then
      ShowWindow(H, SW_RESTORE);
    SetForegroundWindow(H);
    FLog.Add('激活窗口: ' + Title);
  end
  else
    FLog.Add('未找到窗口: ' + Title);
end;

function TVMAEngine.DoFindWindow(const Title: string): HWND;
begin
  Result := FindWindowByTitle(Title);
  SetVariable('lastHwnd', CreateNumberVar(Result));
  if Result <> 0 then
    FLog.Add('查找窗口: ' + Title + ' -> 找到')
  else
    FLog.Add('查找窗口: ' + Title + ' -> 未找到');
end;

procedure TVMAEngine.DoCloseWindow(const Title: string);
var
  H: HWND;
begin
  H := FindWindowByTitle(Title);
  if H <> 0 then
  begin
    PostMessage(H, WM_CLOSE, 0, 0);
    FLog.Add('关闭窗口: ' + Title);
  end
  else
    FLog.Add('未找到窗口: ' + Title);
end;

procedure TVMAEngine.DoMinimizeWindow(const Title: string);
var
  H: HWND;
begin
  H := FindWindowByTitle(Title);
  if H <> 0 then
  begin
    ShowWindow(H, SW_MINIMIZE);
    FLog.Add('最小化窗口: ' + Title);
  end
  else
    FLog.Add('未找到窗口: ' + Title);
end;

procedure TVMAEngine.DoMaximizeWindow(const Title: string);
var
  H: HWND;
begin
  H := FindWindowByTitle(Title);
  if H <> 0 then
  begin
    ShowWindow(H, SW_MAXIMIZE);
    FLog.Add('最大化窗口: ' + Title);
  end
  else
    FLog.Add('未找到窗口: ' + Title);
end;

procedure TVMAEngine.DoRestoreWindow(const Title: string);
var
  H: HWND;
begin
  H := FindWindowByTitle(Title);
  if H <> 0 then
  begin
    ShowWindow(H, SW_RESTORE);
    FLog.Add('还原窗口: ' + Title);
  end
  else
    FLog.Add('未找到窗口: ' + Title);
end;

function TVMAEngine.DoWaitForWindow(const Title: string; Timeout: Integer): Boolean;
var
  StartTime: TDateTime;
  H: HWND;
begin
  Result := False;
  StartTime := Now;
  
  while MilliSecondsBetween(Now, StartTime) < Timeout * 1000 do
  begin
    if FShouldStop then
      Exit;
      
    H := FindWindowByTitle(Title);
    if H <> 0 then
    begin
      Result := True;
      FLog.Add('等待窗口: ' + Title + ' -> 找到');
      Exit;
    end;
    
    Sleep(200);
  end;
  
  FLog.Add('等待窗口: ' + Title + ' -> 超时');
end;

function TVMAEngine.DoScreenshot: string;
var
  Bmp: TBitmap;
  ScreenW, ScreenH: Integer;
  FileName: string;
begin
  Result := '';
  
  ScreenW := GetSystemMetrics(SM_CXSCREEN);
  ScreenH := GetSystemMetrics(SM_CYSCREEN);
  
  Bmp := TBitmap.Create;
  try
    Bmp.SetSize(ScreenW, ScreenH);
    BitBlt(Bmp.Canvas.Handle, 0, 0, ScreenW, ScreenH, 
           GetDC(0), 0, 0, SRCCOPY);
    
    FileName := FDataDir + 'vma_screenshot_' + FormatDateTime('yyyymmddhhnnsszzz', Now) + '.png';
    Bmp.SaveToFile(FileName);
    Result := FileName;
    FLog.Add('截图保存: ' + FileName);
  finally
    Bmp.Free;
  end;
end;

function TVMAEngine.GetMousePos: TPoint;
begin
  GetCursorPos(Result);
end;

function TVMAEngine.GetScreenSize: TPoint;
begin
  Result.X := GetSystemMetrics(SM_CXSCREEN);
  Result.Y := GetSystemMetrics(SM_CYSCREEN);
end;

function TVMAEngine.GetWindowList: TJSONArray;
begin
  Result := nil;
end;

function TVMAEngine.FindWindowByTitle(const Title: string): HWND;
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

procedure TVMAEngine.PushLoop(const LoopInfo: TVMALoopInfo);
begin
  if FLoopStackCount >= Length(FLoopStack) then
    SetLength(FLoopStack, FLoopStackCount + 50);
  FLoopStack[FLoopStackCount] := LoopInfo;
  Inc(FLoopStackCount);
end;

function TVMAEngine.PopLoop: TVMALoopInfo;
begin
  if FLoopStackCount > 0 then
  begin
    Dec(FLoopStackCount);
    Result := FLoopStack[FLoopStackCount];
  end;
end;

function TVMAEngine.PeekLoop: PVMALoopInfo;
begin
  if FLoopStackCount > 0 then
    Result := @FLoopStack[FLoopStackCount - 1]
  else
    Result := nil;
end;

function TVMAEngine.FindFunction(const Name: string): Integer;
var
  I: Integer;
begin
  Result := -1;
  for I := 0 to FFunctionCount - 1 do
    if SameText(FFunctions[I].Name, Name) then
    begin
      Result := I;
      Exit;
    end;
end;

function TVMAEngine.CallUserFunction(const FuncName, ArgsStr: string): TVMAVariable;
var
  FuncIdx: Integer;
  FuncDef: PVMAFunction;
  Args: TJSONObject;
  ParamList: TStringList;
  I: Integer;
  ParamName: string;
  ArgVal: TVMAVariable;
  SavedVars: array of TVMAVariable;
  SavedCount: Integer;
  BodyLine: string;
  CmdResult: Boolean;
begin
  Result.ValueType := vtNone;
  
  FuncIdx := FindFunction(FuncName);
  if FuncIdx < 0 then
    Exit;
    
  FuncDef := @FFunctions[FuncIdx];
  
  SavedCount := FVariableCount;
  SetLength(SavedVars, SavedCount);
  for I := 0 to SavedCount - 1 do
    SavedVars[I] := FVariables[I];
  
  Args := ParseNamedArgs(ArgsStr);
  try
    if FuncDef^.Params <> '' then
    begin
      ParamList := TStringList.Create;
      try
        ParamList.Delimiter := ',';
        ParamList.StrictDelimiter := True;
        ParamList.DelimitedText := FuncDef^.Params;
        
        for I := 0 to ParamList.Count - 1 do
        begin
          ParamName := Trim(ParamList[I]);
          if ParamName <> '' then
          begin
            if Args.Find(IntToStr(I)) <> nil then
              ArgVal := EvalExpression(Args.Get(IntToStr(I), ''))
            else
            begin
              ArgVal.ValueType := vtNone;
              ArgVal.NumberValue := 0;
              ArgVal.StringValue := '';
            end;
            SetVariable(ParamName, ArgVal);
          end;
        end;
      finally
        ParamList.Free;
      end;
    end;
    
    for I := 0 to FuncDef^.Body.Count - 1 do
    begin
      if FShouldStop then
        Break;
        
      while FPaused and not FShouldStop do
        Sleep(100);
        
      BodyLine := FuncDef^.Body[I];
      CmdResult := ExecuteCommand(BodyLine);
    end;
  finally
    Args.Free;
  end;
  
  FVariableCount := SavedCount;
  for I := 0 to SavedCount - 1 do
    FVariables[I] := SavedVars[I];
end;

function TVMAEngine.ExecuteCommand(const Line: string): Boolean;
var
  TrimmedLine, LowerLine: string;
  VarMatch, VarName, VarValue: string;
  VarVal: TVMAVariable;
  ArrayMatch, ArrayName, ArrayIndex, ArrayValue: string;
  IndexVal: TVMAVariable;
  IndexInt: Integer;
  ArrDefMatch, ArrName, ArrInit: string;
  NewArr: TVMAVariable;
  InitList: TStringList;
  I, J: Integer;
  ArrOpMatch, ArrOp, ArrOpArg: string;
  FuncMatch, FuncName, FuncArgs: string;
  Args: TJSONObject;
  ArgsList: TStringList;
  X, Y, X1, Y1, X2, Y2: Integer;
  Title, Text, Keys: string;
  RightClick, DoubleClick, UseBackend, BringToFront: Boolean;
  Timeout: Integer;
  LoopInfo: TVMALoopInfo;
  LoopCond: string;
  ForMatch, ForVar, ForStart, ForEnd, ForStep: string;
  StartVal, EndVal, StepVal: Integer;
  ForEachMatch, ForEachVar, ForEachArr: string;
  LabelMatch: string;
  LabelIdx: Integer;
  Loop: PVMALoopInfo;
  CurrentVal: Integer;
  ShouldContinue: Boolean;
  Condition: string;
  CondResult: Boolean;
begin
  Result := True;
  
  if FShouldStop then
  begin
    Result := False;
    Exit;
  end;
  
  while FPaused and not FShouldStop do
    Sleep(100);
    
  if FShouldStop then
  begin
    Result := False;
    Exit;
  end;
  
  TrimmedLine := Trim(Line);
  if (TrimmedLine = '') or (Copy(TrimmedLine, 1, 2) = '//') then
    Exit;
    
  LowerLine := LowerCase(TrimmedLine);
  
  if (Pos('var ', LowerLine) = 1) or 
     ((Pos('=', TrimmedLine) > 0) and (Pos('(', TrimmedLine) = 0)) then
  begin
    if Pos('var ', LowerLine) = 1 then
    begin
      VarMatch := Copy(TrimmedLine, 5, MaxInt);
      I := Pos('=', VarMatch);
      if I > 0 then
      begin
        VarName := Trim(Copy(VarMatch, 1, I - 1));
        VarValue := Trim(Copy(VarMatch, I + 1, MaxInt));
      end
      else
      begin
        VarName := Trim(VarMatch);
        VarValue := '';
      end;
    end
    else
    begin
      I := Pos('=', TrimmedLine);
      VarName := Trim(Copy(TrimmedLine, 1, I - 1));
      VarValue := Trim(Copy(TrimmedLine, I + 1, MaxInt));
    end;
    
    if (Pos('[', VarName) > 0) and (VarName[Length(VarName)] = ']') then
    begin
      I := Pos('[', VarName);
      ArrayName := Copy(VarName, 1, I - 1);
      ArrayIndex := Copy(VarName, I + 1, Length(VarName) - I - 1);
      
      IndexVal := EvalExpression(ArrayIndex);
      IndexInt := Trunc(IndexVal.NumberValue);
      VarVal := EvalExpression(VarValue);
      
      I := FindVariable(ArrayName);
      if (I >= 0) and (FVariables[I].ValueType = vtArray) then
      begin
        if Assigned(FVariables[I].ArrayValue) then
        begin
          while FVariables[I].ArrayValue.Count < IndexInt do
            FVariables[I].ArrayValue.Add(TJSONNull.Create);
          
          case VarVal.ValueType of
            vtNumber: 
              begin
                if IndexInt <= FVariables[I].ArrayValue.Count then
                  FVariables[I].ArrayValue.Delete(IndexInt - 1);
                FVariables[I].ArrayValue.Insert(IndexInt - 1, TJSONFloatNumber.Create(VarVal.NumberValue));
              end;
            vtString:
              begin
                if IndexInt <= FVariables[I].ArrayValue.Count then
                  FVariables[I].ArrayValue.Delete(IndexInt - 1);
                FVariables[I].ArrayValue.Insert(IndexInt - 1, TJSONString.Create(VarVal.StringValue));
              end;
          end;
        end;
      end;
      Exit;
    end;
    
    if VarValue <> '' then
      VarVal := EvalExpression(VarValue)
    else
    begin
      VarVal.ValueType := vtNone;
      VarVal.NumberValue := 0;
      VarVal.StringValue := '';
    end;
    
    SetVariable(VarName, VarVal);
    Exit;
  end;
  
  if SameText(Copy(TrimmedLine, 1, 6), 'array ') then
  begin
    ArrDefMatch := Copy(TrimmedLine, 7, MaxInt);
    I := Pos('=', ArrDefMatch);
    if I > 0 then
    begin
      ArrName := Trim(Copy(ArrDefMatch, 1, I - 1));
      ArrInit := Trim(Copy(ArrDefMatch, I + 1, MaxInt));
      
      NewArr := CreateArrayVar;
      
      if (Length(ArrInit) >= 2) and (ArrInit[1] = '[') and (ArrInit[Length(ArrInit)] = ']') then
      begin
        ArrInit := Copy(ArrInit, 2, Length(ArrInit) - 2);
        if ArrInit <> '' then
        begin
          InitList := ParseArgs(ArrInit);
          try
            for I := 0 to InitList.Count - 1 do
            begin
              VarVal := EvalExpression(InitList[I]);
              case VarVal.ValueType of
                vtNumber: NewArr.ArrayValue.Add(TJSONFloatNumber.Create(VarVal.NumberValue));
                vtString: NewArr.ArrayValue.Add(TJSONString.Create(VarVal.StringValue));
                vtBoolean: NewArr.ArrayValue.Add(TJSONBoolean.Create(VarVal.BooleanValue));
              else
                NewArr.ArrayValue.Add(TJSONNull.Create);
              end;
            end;
          finally
            InitList.Free;
          end;
        end;
      end;
      
      SetVariable(ArrName, NewArr);
    end;
    Exit;
  end;
  
  if SameText(TrimmedLine, 'break') then
  begin
    if FLoopStackCount > 0 then
      PopLoop;
    Exit;
  end;
  
  if SameText(TrimmedLine, 'continue') then
  begin
    if FLoopStackCount > 0 then
    begin
      Loop := PeekLoop;
      FCurrentLine := Loop^.StartLine - 1;
    end;
    Exit;
  end;
  
  if SameText(Copy(TrimmedLine, 1, 7), 'return ') then
  begin
    Exit;
  end;
  
  if SameText(Copy(TrimmedLine, 1, 5), 'goto ') then
  begin
    LabelMatch := Trim(Copy(TrimmedLine, 6, MaxInt));
    for I := 0 to FLabelCount - 1 do
    begin
      if SameText(FLabels[I].Name, LabelMatch) then
      begin
        FCurrentLine := FLabels[I].LineNum - 1;
        Exit;
      end;
    end;
    FLog.Add('未找到标签: ' + LabelMatch);
    Exit;
  end;
  
  if (Pos('(', TrimmedLine) > 0) and (TrimmedLine[Length(TrimmedLine)] = ')') then
  begin
    I := Pos('(', TrimmedLine);
    FuncName := Copy(TrimmedLine, 1, I - 1);
    FuncArgs := Copy(TrimmedLine, I + 1, Length(TrimmedLine) - I - 1);
    
    if SameText(FuncName, 'sleep') or SameText(FuncName, 'msleep') then
    begin
      ArgsList := ParseArgs(FuncArgs);
      try
        if ArgsList.Count > 0 then
        begin
          VarVal := EvalExpression(ArgsList[0]);
          DoSleep(Trunc(VarVal.NumberValue));
        end
        else
          DoSleep(1000);
      finally
        ArgsList.Free;
      end;
      Exit;
    end;
    
    if SameText(FuncName, 'click') or SameText(FuncName, 'leftclick') then
    begin
      Args := ParseNamedArgs(FuncArgs);
      try
        ArgsList := ParseArgs(FuncArgs);
        try
          if ArgsList.Count >= 2 then
          begin
            X := Trunc(EvalExpression(ArgsList[0]).NumberValue);
            Y := Trunc(EvalExpression(ArgsList[1]).NumberValue);
            RightClick := (Args.Get('rightClick', '') = '1') or (Args.Get('rightClick', '') = 'true');
            DoubleClick := (Args.Get('doubleClick', '') = '1') or (Args.Get('doubleClick', '') = 'true');
            UseBackend := (Args.Get('useBackend', '') = '1') or (Args.Get('useBackend', '') = 'true');
            
            DoClick(X, Y, RightClick, DoubleClick, UseBackend);
          end;
        finally
          ArgsList.Free;
        end;
      finally
        Args.Free;
      end;
      Exit;
    end;
    
    if SameText(FuncName, 'rightclick') then
    begin
      ArgsList := ParseArgs(FuncArgs);
      try
        if ArgsList.Count >= 2 then
        begin
          X := Trunc(EvalExpression(ArgsList[0]).NumberValue);
          Y := Trunc(EvalExpression(ArgsList[1]).NumberValue);
          Args := ParseNamedArgs(FuncArgs);
          try
            UseBackend := (Args.Get('useBackend', '') = '1') or (Args.Get('useBackend', '') = 'true');
          finally
            Args.Free;
          end;
          DoClick(X, Y, True, False, UseBackend);
        end;
      finally
        ArgsList.Free;
      end;
      Exit;
    end;
    
    if SameText(FuncName, 'doubleclick') then
    begin
      ArgsList := ParseArgs(FuncArgs);
      try
        if ArgsList.Count >= 2 then
        begin
          X := Trunc(EvalExpression(ArgsList[0]).NumberValue);
          Y := Trunc(EvalExpression(ArgsList[1]).NumberValue);
          Args := ParseNamedArgs(FuncArgs);
          try
            UseBackend := (Args.Get('useBackend', '') = '1') or (Args.Get('useBackend', '') = 'true');
          finally
            Args.Free;
          end;
          DoClick(X, Y, False, True, UseBackend);
        end;
      finally
        ArgsList.Free;
      end;
      Exit;
    end;
    
    if SameText(FuncName, 'clickbytitle') then
    begin
      Args := ParseNamedArgs(FuncArgs);
      try
        Title := Args.Get('title', '');
        if Title = '' then
          Title := Args.Get('0', '');
        if IsQuotedString(Title) then
          Title := UnquoteString(Title);
          
        X := Trunc(EvalExpression(Args.Get('x', Args.Get('1', '0'))).NumberValue);
        Y := Trunc(EvalExpression(Args.Get('y', Args.Get('2', '0'))).NumberValue);
        RightClick := (Args.Get('rightClick', '') = '1') or (Args.Get('rightClick', '') = 'true');
        DoubleClick := (Args.Get('doubleClick', '') = '1') or (Args.Get('doubleClick', '') = 'true');
        UseBackend := (Args.Get('useBackend', '') = '1') or (Args.Get('useBackend', '') = 'true');
        BringToFront := (Args.Get('bringToFront', '') <> '0') and (Args.Get('bringToFront', '') <> 'false');
        
        DoClickByTitle(Title, X, Y, RightClick, DoubleClick, UseBackend, BringToFront);
      finally
        Args.Free;
      end;
      Exit;
    end;
    
    if SameText(FuncName, 'moveto') then
    begin
      ArgsList := ParseArgs(FuncArgs);
      try
        if ArgsList.Count >= 2 then
        begin
          X := Trunc(EvalExpression(ArgsList[0]).NumberValue);
          Y := Trunc(EvalExpression(ArgsList[1]).NumberValue);
          DoMoveTo(X, Y);
        end;
      finally
        ArgsList.Free;
      end;
      Exit;
    end;
    
    if SameText(FuncName, 'drag') then
    begin
      ArgsList := ParseArgs(FuncArgs);
      try
        if ArgsList.Count >= 4 then
        begin
          X1 := Trunc(EvalExpression(ArgsList[0]).NumberValue);
          Y1 := Trunc(EvalExpression(ArgsList[1]).NumberValue);
          X2 := Trunc(EvalExpression(ArgsList[2]).NumberValue);
          Y2 := Trunc(EvalExpression(ArgsList[3]).NumberValue);
          DoDrag(X1, Y1, X2, Y2);
        end;
      finally
        ArgsList.Free;
      end;
      Exit;
    end;
    
    if SameText(FuncName, 'getmousepos') then
    begin
      VarVal := EvalExpression(TrimmedLine);
      Exit;
    end;
    
    if SameText(FuncName, 'input') then
    begin
      if IsQuotedString(FuncArgs) then
        Text := UnquoteString(FuncArgs)
      else
      begin
        VarVal := EvalExpression(FuncArgs);
        Text := VarVal.StringValue;
      end;
      DoInputText(Text);
      Exit;
    end;
    
    if SameText(FuncName, 'keypress') then
    begin
      if IsQuotedString(FuncArgs) then
        Keys := UnquoteString(FuncArgs)
      else
      begin
        VarVal := EvalExpression(FuncArgs);
        Keys := VarVal.StringValue;
      end;
      DoKeyPress(Keys);
      Exit;
    end;
    
    if SameText(FuncName, 'keydown') then
    begin
      if IsQuotedString(FuncArgs) then
        Keys := UnquoteString(FuncArgs)
      else
      begin
        VarVal := EvalExpression(FuncArgs);
        Keys := VarVal.StringValue;
      end;
      DoKeyDown(Keys);
      Exit;
    end;
    
    if SameText(FuncName, 'keyup') then
    begin
      if IsQuotedString(FuncArgs) then
        Keys := UnquoteString(FuncArgs)
      else
      begin
        VarVal := EvalExpression(FuncArgs);
        Keys := VarVal.StringValue;
      end;
      DoKeyUp(Keys);
      Exit;
    end;
    
    if SameText(FuncName, 'activate') or SameText(FuncName, 'activatewindow') then
    begin
      if IsQuotedString(FuncArgs) then
        Title := UnquoteString(FuncArgs)
      else
      begin
        VarVal := EvalExpression(FuncArgs);
        Title := VarVal.StringValue;
      end;
      DoActivateWindow(Title);
      Exit;
    end;
    
    if SameText(FuncName, 'findwindow') then
    begin
      if IsQuotedString(FuncArgs) then
        Title := UnquoteString(FuncArgs)
      else
      begin
        VarVal := EvalExpression(FuncArgs);
        Title := VarVal.StringValue;
      end;
      DoFindWindow(Title);
      Exit;
    end;
    
    if SameText(FuncName, 'closewindow') then
    begin
      if IsQuotedString(FuncArgs) then
        Title := UnquoteString(FuncArgs)
      else
      begin
        VarVal := EvalExpression(FuncArgs);
        Title := VarVal.StringValue;
      end;
      DoCloseWindow(Title);
      Exit;
    end;
    
    if SameText(FuncName, 'minimizewindow') or SameText(FuncName, 'minimize') then
    begin
      if IsQuotedString(FuncArgs) then
        Title := UnquoteString(FuncArgs)
      else
      begin
        VarVal := EvalExpression(FuncArgs);
        Title := VarVal.StringValue;
      end;
      DoMinimizeWindow(Title);
      Exit;
    end;
    
    if SameText(FuncName, 'maximizewindow') or SameText(FuncName, 'maximize') then
    begin
      if IsQuotedString(FuncArgs) then
        Title := UnquoteString(FuncArgs)
      else
      begin
        VarVal := EvalExpression(FuncArgs);
        Title := VarVal.StringValue;
      end;
      DoMaximizeWindow(Title);
      Exit;
    end;
    
    if SameText(FuncName, 'restorewindow') or SameText(FuncName, 'restore') then
    begin
      if IsQuotedString(FuncArgs) then
        Title := UnquoteString(FuncArgs)
      else
      begin
        VarVal := EvalExpression(FuncArgs);
        Title := VarVal.StringValue;
      end;
      DoRestoreWindow(Title);
      Exit;
    end;
    
    if SameText(FuncName, 'waitfor') then
    begin
      ArgsList := ParseArgs(FuncArgs);
      try
        if ArgsList.Count >= 1 then
        begin
          if IsQuotedString(ArgsList[0]) then
            Title := UnquoteString(ArgsList[0])
          else
          begin
            VarVal := EvalExpression(ArgsList[0]);
            Title := VarVal.StringValue;
          end;
          
          if ArgsList.Count >= 2 then
            Timeout := Trunc(EvalExpression(ArgsList[1]).NumberValue)
          else
            Timeout := 30;
            
          DoWaitForWindow(Title, Timeout);
        end;
      finally
        ArgsList.Free;
      end;
      Exit;
    end;
    
    if SameText(FuncName, 'getscreensize') then
    begin
      VarVal := EvalExpression(TrimmedLine);
      Exit;
    end;
    
    if SameText(FuncName, 'screenshot') then
    begin
      DoScreenshot;
      Exit;
    end;
    
    if SameText(FuncName, 'log') then
    begin
      if IsQuotedString(FuncArgs) then
        Text := UnquoteString(FuncArgs)
      else
      begin
        VarVal := EvalExpression(FuncArgs);
        Text := VarVal.StringValue;
      end;
      FLog.Add(Text);
      Exit;
    end;
    
    if SameText(FuncName, 'msg') or SameText(FuncName, 'messagebox') then
    begin
      if IsQuotedString(FuncArgs) then
        Text := UnquoteString(FuncArgs)
      else
      begin
        VarVal := EvalExpression(FuncArgs);
        Text := VarVal.StringValue;
      end;
      FLog.Add('[消息] ' + Text);
      Exit;
    end;
    
    if SameText(FuncName, 'push') or SameText(FuncName, 'pop') or 
       SameText(FuncName, 'shift') or SameText(FuncName, 'unshift') then
    begin
      ArgsList := ParseArgs(FuncArgs);
      try
        if ArgsList.Count >= 1 then
        begin
          ArrName := ArgsList[0];
          I := FindVariable(ArrName);
          
          if (I < 0) or (FVariables[I].ValueType <> vtArray) then
          begin
            NewArr := CreateArrayVar;
            SetVariable(ArrName, NewArr);
            I := FindVariable(ArrName);
          end;
          
          if SameText(FuncName, 'push') and (ArgsList.Count >= 2) then
          begin
            VarVal := EvalExpression(ArgsList[1]);
            case VarVal.ValueType of
              vtNumber: FVariables[I].ArrayValue.Add(TJSONFloatNumber.Create(VarVal.NumberValue));
              vtString: FVariables[I].ArrayValue.Add(TJSONString.Create(VarVal.StringValue));
              vtBoolean: FVariables[I].ArrayValue.Add(TJSONBoolean.Create(VarVal.BooleanValue));
            else
              FVariables[I].ArrayValue.Add(TJSONNull.Create);
            end;
          end
          else if SameText(FuncName, 'pop') then
          begin
            if FVariables[I].ArrayValue.Count > 0 then
              FVariables[I].ArrayValue.Delete(FVariables[I].ArrayValue.Count - 1);
          end
          else if SameText(FuncName, 'shift') then
          begin
            if FVariables[I].ArrayValue.Count > 0 then
              FVariables[I].ArrayValue.Delete(0);
          end
          else if SameText(FuncName, 'unshift') and (ArgsList.Count >= 2) then
          begin
            VarVal := EvalExpression(ArgsList[1]);
            case VarVal.ValueType of
              vtNumber: FVariables[I].ArrayValue.Insert(0, TJSONFloatNumber.Create(VarVal.NumberValue));
              vtString: FVariables[I].ArrayValue.Insert(0, TJSONString.Create(VarVal.StringValue));
              vtBoolean: FVariables[I].ArrayValue.Insert(0, TJSONBoolean.Create(VarVal.BooleanValue));
            else
              FVariables[I].ArrayValue.Insert(0, TJSONNull.Create);
            end;
          end;
        end;
      finally
        ArgsList.Free;
      end;
      Exit;
    end;
    
    if FindFunction(FuncName) >= 0 then
    begin
      CallUserFunction(FuncName, FuncArgs);
      Exit;
    end;
    
    Exit;
  end;
  
  if SameText(Copy(TrimmedLine, 1, 3), 'if ') then
  begin
    Condition := Trim(Copy(TrimmedLine, 4, MaxInt));
    Exit;
  end;
  
  if SameText(TrimmedLine, 'else') or SameText(TrimmedLine, 'elseif') then
  begin
    Exit;
  end;
  
  if SameText(TrimmedLine, 'end') or SameText(TrimmedLine, 'endif') then
  begin
    Exit;
  end;
  
  if SameText(Copy(TrimmedLine, 1, 5), 'loop ') then
  begin
    VarVal := EvalExpression(Trim(Copy(TrimmedLine, 6, MaxInt)));
    
    LoopInfo.LoopType := ltTimes;
    LoopInfo.Count := Trunc(VarVal.NumberValue);
    LoopInfo.Current := 0;
    LoopInfo.StartLine := FCurrentLine + 1;
    
    PushLoop(LoopInfo);
    Exit;
  end;
  
  if SameText(Copy(TrimmedLine, 1, 6), 'while ') then
  begin
    LoopCond := Trim(Copy(TrimmedLine, 7, MaxInt));
    
    LoopInfo.LoopType := ltWhile;
    LoopInfo.Condition := LoopCond;
    LoopInfo.StartLine := FCurrentLine + 1;
    
    PushLoop(LoopInfo);
    Exit;
  end;
  
  if SameText(Copy(TrimmedLine, 1, 4), 'for ') then
  begin
    I := Pos('=', TrimmedLine);
    J := Pos(' to ', LowerCase(TrimmedLine));
    
    if (I > 0) and (J > I) then
    begin
      ForVar := Trim(Copy(TrimmedLine, 5, I - 5));
      ForStart := Trim(Copy(TrimmedLine, I + 1, J - I - 1));
      
      I := Pos(' step ', LowerCase(TrimmedLine));
      if I > 0 then
      begin
        ForEnd := Trim(Copy(TrimmedLine, J + 4, I - J - 4));
        ForStep := Trim(Copy(TrimmedLine, I + 6, MaxInt));
        StepVal := Trunc(EvalExpression(ForStep).NumberValue);
      end
      else
      begin
        ForEnd := Trim(Copy(TrimmedLine, J + 4, MaxInt));
        StepVal := 1;
      end;
      
      StartVal := Trunc(EvalExpression(ForStart).NumberValue);
      EndVal := Trunc(EvalExpression(ForEnd).NumberValue);
      
      if StepVal = 0 then StepVal := 1;
      
      SetVariable(ForVar, CreateNumberVar(StartVal));
      
      LoopInfo.LoopType := ltFor;
      LoopInfo.VarName := ForVar;
      LoopInfo.StartVal := StartVal;
      LoopInfo.EndVal := EndVal;
      LoopInfo.StepVal := StepVal;
      LoopInfo.StartLine := FCurrentLine + 1;
      
      PushLoop(LoopInfo);
    end;
    Exit;
  end;
  
  if SameText(Copy(TrimmedLine, 1, 8), 'foreach ') then
  begin
    I := Pos(' in ', LowerCase(TrimmedLine));
    if I > 0 then
    begin
      ForEachVar := Trim(Copy(TrimmedLine, 9, I - 9));
      ForEachArr := Trim(Copy(TrimmedLine, I + 4, MaxInt));
      
      LoopInfo.LoopType := ltForeach;
      LoopInfo.VarName := ForEachVar;
      LoopInfo.ArrName := ForEachArr;
      LoopInfo.Index := 1;
      LoopInfo.StartLine := FCurrentLine + 1;
      
      PushLoop(LoopInfo);
    end;
    Exit;
  end;
  
  if SameText(TrimmedLine, 'endloop') or SameText(TrimmedLine, 'next') or 
     SameText(TrimmedLine, 'endwhile') then
  begin
    if FLoopStackCount > 0 then
    begin
      Loop := PeekLoop;
      
      case Loop^.LoopType of
        ltTimes:
          begin
            Inc(Loop^.Current);
            if Loop^.Current < Loop^.Count then
              FCurrentLine := Loop^.StartLine - 1
            else
              PopLoop;
          end;
          
        ltWhile:
          begin
            if EvalCondition(Loop^.Condition) then
              FCurrentLine := Loop^.StartLine - 1
            else
              PopLoop;
          end;
          
        ltFor:
          begin
            VarVal := GetVariable(Loop^.VarName);
            CurrentVal := Trunc(VarVal.NumberValue) + Loop^.StepVal;
            SetVariable(Loop^.VarName, CreateNumberVar(CurrentVal));
            
            if Loop^.StepVal > 0 then
              ShouldContinue := CurrentVal <= Loop^.EndVal
            else
              ShouldContinue := CurrentVal >= Loop^.EndVal;
              
            if ShouldContinue then
              FCurrentLine := Loop^.StartLine - 1
            else
              PopLoop;
          end;
          
        ltForeach:
          begin
            I := FindVariable(Loop^.ArrName);
            if (I >= 0) and (FVariables[I].ValueType = vtArray) and
               Assigned(FVariables[I].ArrayValue) then
            begin
              if Loop^.Index <= FVariables[I].ArrayValue.Count then
              begin
                case FVariables[I].ArrayValue.Items[Loop^.Index - 1].JSONType of
                  jtNumber: SetVariable(Loop^.VarName, CreateNumberVar(FVariables[I].ArrayValue.Items[Loop^.Index - 1].AsFloat));
                  jtString: SetVariable(Loop^.VarName, CreateStringVar(FVariables[I].ArrayValue.Items[Loop^.Index - 1].AsString));
                  jtBoolean: SetVariable(Loop^.VarName, CreateBooleanVar(FVariables[I].ArrayValue.Items[Loop^.Index - 1].AsBoolean));
                end;
                Inc(Loop^.Index);
                FCurrentLine := Loop^.StartLine - 1;
              end
              else
                PopLoop;
            end
            else
              PopLoop;
          end;
      end;
    end;
    Exit;
  end;
  
  if SameText(Copy(TrimmedLine, 1, 9), 'function ') then
  begin
    Exit;
  end;
  
  if SameText(TrimmedLine, 'endfunction') or SameText(TrimmedLine, 'end function') then
  begin
    Exit;
  end;
end;

function TVMAEngine.Run(const Script: string): TJSONObject;
var
  I: Integer;
  Line: string;
  InFunctionDef: Boolean;
  CurrentFunc: PVMAFunction;
  FuncName, FuncParams: string;
  ParenPos: Integer;
  InIfBlock: Boolean;
  IfResult: Boolean;
  SkipElse: Boolean;
begin
  Result := TJSONObject.Create;
  
  FVariableCount := 0;
  FFunctionCount := 0;
  FLoopStackCount := 0;
  FLog.Clear;
  FRunning := True;
  FPaused := False;
  FShouldStop := False;
  
  if not ParseScript(Script) then
  begin
    Result.Add('success', False);
    Result.Add('error', '脚本解析失败');
    FRunning := False;
    Exit;
  end;
  
  InFunctionDef := False;
  CurrentFunc := nil;
  InIfBlock := False;
  SkipElse := False;
  
  FCurrentLine := 0;
  while FCurrentLine < FLines.Count do
  begin
    if FShouldStop then
    begin
      Result.Add('success', False);
      Result.Add('error', '脚本已停止');
      Result.Add('line', FCurrentLine + 1);
      Break;
    end;
    
    while FPaused and not FShouldStop do
      Sleep(100);
      
    if FShouldStop then
    begin
      Result.Add('success', False);
      Result.Add('error', '脚本已停止');
      Break;
    end;
    
    Line := FLines[FCurrentLine];
    
    if InFunctionDef then
    begin
      if SameText(Trim(Line), 'endfunction') or SameText(Trim(Line), 'end function') then
      begin
        InFunctionDef := False;
        CurrentFunc := nil;
        Inc(FCurrentLine);
        Continue;
      end;
      
      CurrentFunc^.Body.Add(Line);
      Inc(FCurrentLine);
      Continue;
    end;
    
    if SameText(Copy(Trim(Line), 1, 9), 'function ') then
    begin
      Line := Trim(Copy(Trim(Line), 10, MaxInt));
      ParenPos := Pos('(', Line);
      if ParenPos > 0 then
      begin
        FuncName := Trim(Copy(Line, 1, ParenPos - 1));
        FuncParams := Copy(Line, ParenPos + 1, Length(Line) - ParenPos - 1);
        
        if FFunctionCount >= Length(FFunctions) then
          SetLength(FFunctions, FFunctionCount + 50);
          
        FFunctions[FFunctionCount].Name := FuncName;
        FFunctions[FFunctionCount].Params := FuncParams;
        FFunctions[FFunctionCount].Body := TStringList.Create;
        FFunctions[FFunctionCount].StartLine := FCurrentLine;
        
        InFunctionDef := True;
        CurrentFunc := @FFunctions[FFunctionCount];
        Inc(FFunctionCount);
      end;
      
      Inc(FCurrentLine);
      Continue;
    end;
    
    if not ExecuteCommand(Line) then
    begin
      Result.Add('success', False);
      Result.Add('error', '执行失败');
      Result.Add('line', FCurrentLine + 1);
      Result.Add('content', Line);
      Break;
    end;
    
    Inc(FCurrentLine);
  end;
  
  if Result.Find('success') = nil then
  begin
    Result.Add('success', True);
    Result.Add('linesExecuted', FCurrentLine);
    
    if FLog.Count > 0 then
    begin
      I := Result.IndexOfName('log');
      if I >= 0 then
        Result.Delete(I);
      Result.Add('log', FLog.CommaText);
    end;
  end;
  
  FRunning := False;
end;

function TVMAEngine.RunFile(const FileName: string): TJSONObject;
var
  SL: TStringList;
begin
  Result := TJSONObject.Create;
  
  if not FileExists(FileName) then
  begin
    Result.Add('success', False);
    Result.Add('error', '文件不存在: ' + FileName);
    Exit;
  end;
  
  SL := TStringList.Create;
  try
    SL.LoadFromFile(FileName);
    Result := Run(SL.Text);
  finally
    SL.Free;
  end;
end;

procedure TVMAEngine.Stop;
begin
  FShouldStop := True;
  FRunning := False;
end;

procedure TVMAEngine.Pause;
begin
  FPaused := True;
end;

procedure TVMAEngine.Resume;
begin
  FPaused := False;
end;

function TVMAEngine.GetStatus: TJSONObject;
begin
  Result := TJSONObject.Create;
  Result.Add('running', FRunning);
  Result.Add('paused', FPaused);
  Result.Add('currentLine', FCurrentLine);
  Result.Add('totalLines', FLines.Count);
end;

end.
