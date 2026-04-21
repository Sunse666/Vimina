unit ScriptEditorForm;

{$mode objfpc}{$H+}
{$codepage UTF8}

interface

uses
  Classes, SysUtils, Forms, Controls, Graphics, Dialogs, StdCtrls, ExtCtrls,
  ComCtrls, Menus, fpjson, jsonparser, VMAEngine, VMACompiler, Windows, Types;

type
  TScriptThread = class(TThread)
  private
    FEngine: TVMAEngine;
    FScript: string;
    FResult: TJSONObject;
  protected
    procedure Execute; override;
  public
    constructor Create(AEngine: TVMAEngine; const AScript: string);
    property Result: TJSONObject read FResult;
  end;

  TfrmScriptEditor = class(TForm)
    MainMenu: TMainMenu;
    mnuFile: TMenuItem;
    mnuNew: TMenuItem;
    mnuOpen: TMenuItem;
    mnuSave: TMenuItem;
    mnuSaveAs: TMenuItem;
    mnuN1: TMenuItem;
    mnuExit: TMenuItem;
    mnuRun: TMenuItem;
    mnuRunScript: TMenuItem;
    mnuStopScript: TMenuItem;
    mnuN2: TMenuItem;
    mnuCompile: TMenuItem;
    mnuGetPos: TMenuItem;
    mnuHelp: TMenuItem;
    mnuAbout: TMenuItem;
    mnuCommands: TMenuItem;
    OpenDialog: TOpenDialog;
    SaveDialog: TSaveDialog;
    pnlMain: TPanel;
    pnlEditor: TPanel;
    memScript: TMemo;
    Splitter: TSplitter;
    pnlBottom: TPanel;
    pnlLog: TPanel;
    lblLog: TLabel;
    memLog: TMemo;
    pnlStatus: TPanel;
    lblStatus: TLabel;
    pbProgress: TProgressBar;
    tmrUpdate: TTimer;
    procedure FormCreate(Sender: TObject);
    procedure FormClose(Sender: TObject; var CloseAction: TCloseAction);
    procedure FormDestroy(Sender: TObject);
    procedure mnuNewClick(Sender: TObject);
    procedure mnuOpenClick(Sender: TObject);
    procedure mnuSaveClick(Sender: TObject);
    procedure mnuSaveAsClick(Sender: TObject);
    procedure mnuExitClick(Sender: TObject);
    procedure mnuRunScriptClick(Sender: TObject);
    procedure mnuStopScriptClick(Sender: TObject);
    procedure mnuCompileClick(Sender: TObject);
    procedure mnuGetPosClick(Sender: TObject);
    procedure mnuAboutClick(Sender: TObject);
    procedure mnuCommandsClick(Sender: TObject);
    procedure tmrUpdateTimer(Sender: TObject);
  private
    FCurrentFile: string;
    FModified: Boolean;
    FVMAEngine: TVMAEngine;
    FDataDir: string;
    FConfigFile: string;
    FRunning: Boolean;
    FScriptThread: TScriptThread;
    
    procedure SetModified(Value: Boolean);
    procedure UpdateTitle;
    procedure CheckSave;
    function DoSave: Boolean;
    function DoSaveAs: Boolean;
    procedure RunScript;
    procedure StopScript;
    procedure Log(const Msg: string);
    procedure ShowPosInfo(X, Y: Integer);
  public
    property DataDir: string read FDataDir write FDataDir;
    property ConfigFile: string read FConfigFile write FConfigFile;
  end;

var
  frmScriptEditor: TfrmScriptEditor;

implementation

{$R *.lfm}

{ TScriptThread }

constructor TScriptThread.Create(AEngine: TVMAEngine; const AScript: string);
begin
  inherited Create(True);
  FreeOnTerminate := True;
  FEngine := AEngine;
  FScript := AScript;
  FResult := nil;
end;

procedure TScriptThread.Execute;
begin
  FResult := FEngine.Run(FScript);
end;

{ TfrmScriptEditor }

procedure TfrmScriptEditor.FormCreate(Sender: TObject);
begin
  FCurrentFile := '';
  FModified := False;
  FRunning := False;
  FScriptThread := nil;
  
  UpdateTitle;
  
  memScript.Clear;
  memScript.Lines.Add('// VMA 脚本示例');
  memScript.Lines.Add('// 按 F5 运行脚本');
  memScript.Lines.Add('');
  memScript.Lines.Add('// 获取鼠标位置');
  memScript.Lines.Add('getMousePos()');
  memScript.Lines.Add('log("鼠标位置: " + mouseX + ", " + mouseY)');
  memScript.Lines.Add('');
  memScript.Lines.Add('// 点击示例');
  memScript.Lines.Add('// click(100, 200)');
  memScript.Lines.Add('');
  memScript.Lines.Add('// 输入文本');
  memScript.Lines.Add('// input("Hello World")');
  memScript.Lines.Add('');
  memScript.Lines.Add('// 等待窗口');
  memScript.Lines.Add('// waitFor("记事本", 30)');
  memScript.Lines.Add('');
  memScript.Lines.Add('// 循环示例');
  memScript.Lines.Add('// for i = 1 to 10');
  memScript.Lines.Add('//   log("循环: " + i)');
  memScript.Lines.Add('//   sleep(500)');
  memScript.Lines.Add('// next');
  
  tmrUpdate.Enabled := True;
end;

procedure TfrmScriptEditor.FormClose(Sender: TObject; var CloseAction: TCloseAction);
begin
  if FRunning then
  begin
    StopScript;
    Sleep(100);
  end;
  
  CheckSave;
  CloseAction := caFree;
end;

procedure TfrmScriptEditor.FormDestroy(Sender: TObject);
begin
  if Assigned(FVMAEngine) then
    FVMAEngine.Free;
end;

procedure TfrmScriptEditor.mnuNewClick(Sender: TObject);
begin
  CheckSave;
  memScript.Clear;
  memScript.Lines.Add('// 新建 VMA 脚本');
  FCurrentFile := '';
  FModified := False;
  UpdateTitle;
end;

procedure TfrmScriptEditor.mnuOpenClick(Sender: TObject);
var
  SL: TStringList;
begin
  CheckSave;
  
  OpenDialog.Filter := 'VMA 脚本文件 (*.vma)|*.vma|所有文件 (*.*)|*.*';
  OpenDialog.DefaultExt := 'vma';
  
  if OpenDialog.Execute then
  begin
    SL := TStringList.Create;
    try
      SL.LoadFromFile(OpenDialog.FileName);
      memScript.Text := SL.Text;
      FCurrentFile := OpenDialog.FileName;
      FModified := False;
      UpdateTitle;
      Log('打开文件: ' + FCurrentFile);
    finally
      SL.Free;
    end;
  end;
end;

procedure TfrmScriptEditor.mnuSaveClick(Sender: TObject);
begin
  DoSave;
end;

procedure TfrmScriptEditor.mnuSaveAsClick(Sender: TObject);
begin
  DoSaveAs;
end;

procedure TfrmScriptEditor.mnuExitClick(Sender: TObject);
begin
  Close;
end;

procedure TfrmScriptEditor.mnuRunScriptClick(Sender: TObject);
begin
  RunScript;
end;

procedure TfrmScriptEditor.mnuStopScriptClick(Sender: TObject);
begin
  StopScript;
end;

procedure TfrmScriptEditor.mnuCompileClick(Sender: TObject);
var
  Compiler: TVMACompiler;
  OutputFile: string;
  RuntimePath: string;
begin
  if memScript.Text = '' then
  begin
    ShowMessage('脚本为空');
    Exit;
  end;
  
  SaveDialog.Filter := '可执行文件 (*.exe)|*.exe';
  SaveDialog.DefaultExt := 'exe';
  SaveDialog.FileName := '';
  
  if not SaveDialog.Execute then
    Exit;
    
  OutputFile := SaveDialog.FileName;
  if ExtractFileExt(OutputFile) = '' then
    OutputFile := OutputFile + '.exe';
    
  RuntimePath := ExtractFilePath(ParamStr(0)) + 'VMARuntime.exe';
  if not FileExists(RuntimePath) then
  begin
    ShowMessage('未找到 VMARuntime.exe' + LineEnding + '请确保 VMARuntime.exe 与程序在同一目录');
    Exit;
  end;
  
  Compiler := TVMACompiler.Create(RuntimePath, ExtractFilePath(OutputFile));
  try
    if Compiler.CompileScript(memScript.Text, OutputFile) then
    begin
      Log('编译成功: ' + OutputFile);
      ShowMessage('编译成功!' + LineEnding + '输出文件: ' + OutputFile);
    end
    else
    begin
      Log('编译失败');
      ShowMessage('编译失败');
    end;
  finally
    Compiler.Free;
  end;
end;

procedure TfrmScriptEditor.mnuGetPosClick(Sender: TObject);
var
  P: TPoint;
begin
  GetCursorPos(P);
  ShowPosInfo(P.X, P.Y);
end;

procedure TfrmScriptEditor.mnuAboutClick(Sender: TObject);
begin
  ShowMessage('VMA 脚本编辑器 v1.0' + LineEnding + LineEnding +
              'Vimina 自动化脚本编辑和运行环境' + LineEnding + LineEnding +
              '支持的命令:' + LineEnding +
              '- click(x, y) 点击坐标' + LineEnding +
              '- rightClick(x, y) 右键点击' + LineEnding +
              '- doubleClick(x, y) 双击' + LineEnding +
              '- move(x, y) 移动鼠标' + LineEnding +
              '- drag(x1, y1, x2, y2) 拖拽' + LineEnding +
              '- input(text) 输入文本' + LineEnding +
              '- keyPress(key) 按键' + LineEnding +
              '- sleep(ms) 等待' + LineEnding +
              '- activate(title) 激活窗口' + LineEnding +
              '- waitFor(title, timeout) 等待窗口' + LineEnding +
              '- log(msg) 输出日志');
end;

procedure TfrmScriptEditor.mnuCommandsClick(Sender: TObject);
begin
  ShowMessage('VMA 脚本命令参考:' + LineEnding + LineEnding +
              '【鼠标操作】' + LineEnding +
              'click(x, y, rightClick=false, doubleClick=false, useBackend=false)' + LineEnding +
              'clickByTitle(title, x, y, rightClick=false, doubleClick=false, useBackend=false)' + LineEnding +
              'moveTo(x, y)' + LineEnding +
              'drag(x1, y1, x2, y2)' + LineEnding +
              'getMousePos() - 返回 "x,y"，设置 mouseX, mouseY 变量' + LineEnding +
              LineEnding +
              '【键盘操作】' + LineEnding +
              'input(text) - 输入文本' + LineEnding +
              'keyPress(key) - 按键 (支持组合键如 "Ctrl+C")' + LineEnding +
              'keyDown(key) - 按下按键' + LineEnding +
              'keyUp(key) - 释放按键' + LineEnding +
              LineEnding +
              '【窗口操作】' + LineEnding +
              'activate(title) - 激活窗口' + LineEnding +
              'findWindow(title) - 查找窗口' + LineEnding +
              'closeWindow(title) - 关闭窗口' + LineEnding +
              'minimizeWindow(title) - 最小化窗口' + LineEnding +
              'maximizeWindow(title) - 最大化窗口' + LineEnding +
              'restoreWindow(title) - 还原窗口' + LineEnding +
              'waitFor(title, timeout=30) - 等待窗口出现' + LineEnding +
              'windowExists(title) - 检查窗口是否存在' + LineEnding +
              'windowActive(title) - 检查窗口是否激活' + LineEnding +
              LineEnding +
              '【流程控制】' + LineEnding +
              'sleep(ms) - 等待毫秒' + LineEnding +
              'loop N ... endloop - 循环N次' + LineEnding +
              'while condition ... endwhile - 条件循环' + LineEnding +
              'for i = start to end step N ... next - 计数循环' + LineEnding +
              'foreach item in array ... next - 遍历数组' + LineEnding +
              'if condition ... elseif condition ... else ... endif - 条件判断' + LineEnding +
              'break - 跳出循环' + LineEnding +
              'continue - 继续下次循环' + LineEnding +
              'goto label - 跳转到标签' + LineEnding +
              LineEnding +
              '【变量和函数】' + LineEnding +
              'var name = value - 定义变量' + LineEnding +
              'array name = [1, 2, 3] - 定义数组' + LineEnding +
              'function funcName(param1, param2) ... endfunction - 定义函数' + LineEnding +
              LineEnding +
              '【内置函数】' + LineEnding +
              'rand(min, max) - 随机数' + LineEnding +
              'abs(x) - 绝对值' + LineEnding +
              'floor(x), ceil(x) - 取整' + LineEnding +
              'min(a, b, ...), max(a, b, ...) - 最小/最大值' + LineEnding +
              'toInt(x), toString(x) - 类型转换' + LineEnding +
              'length(arr) - 数组/字符串长度' + LineEnding +
              'type(x) - 获取类型' + LineEnding +
              'log(msg) - 输出日志' + LineEnding +
              'screenshot() - 截图');
end;

procedure TfrmScriptEditor.tmrUpdateTimer(Sender: TObject);
begin
  if FRunning and Assigned(FVMAEngine) then
  begin
    lblStatus.Caption := Format('运行中 - 行: %d', [FVMAEngine.CurrentLine + 1]);
    pbProgress.Position := FVMAEngine.CurrentLine + 1;
    
    if FVMAEngine.Log.Count > memLog.Lines.Count then
    begin
      while memLog.Lines.Count < FVMAEngine.Log.Count do
        memLog.Lines.Add(FVMAEngine.Log[memLog.Lines.Count]);
      memLog.SelStart := Length(memLog.Text);
    end;
    
    if not FVMAEngine.Running then
    begin
      FRunning := False;
      lblStatus.Caption := '执行完成';
      mnuRunScript.Enabled := True;
      mnuStopScript.Enabled := False;
      
      if FVMAEngine.Log.Count > memLog.Lines.Count then
      begin
        while memLog.Lines.Count < FVMAEngine.Log.Count do
          memLog.Lines.Add(FVMAEngine.Log[memLog.Lines.Count]);
      end;
      
      Log('脚本执行完成');
    end;
  end;
end;

procedure TfrmScriptEditor.SetModified(Value: Boolean);
begin
  FModified := Value;
  UpdateTitle;
end;

procedure TfrmScriptEditor.UpdateTitle;
var
  Title: string;
begin
  Title := 'VMA 脚本编辑器';
  if FCurrentFile <> '' then
    Title := Title + ' - ' + ExtractFileName(FCurrentFile)
  else
    Title := Title + ' - 未命名';
    
  if FModified then
    Title := Title + ' *';
    
  Caption := Title;
end;

procedure TfrmScriptEditor.CheckSave;
begin
  if FModified then
  begin
    case MessageDlg('保存', '是否保存当前文件?', mtConfirmation, [mbYes, mbNo, mbCancel], 0) of
      mrYes: DoSave;
      mrCancel: Abort;
    end;
  end;
end;

function TfrmScriptEditor.DoSave: Boolean;
begin
  Result := False;
  
  if FCurrentFile = '' then
  begin
    Result := DoSaveAs;
    Exit;
  end;
  
  try
    memScript.Lines.SaveToFile(FCurrentFile);
    FModified := False;
    UpdateTitle;
    Log('保存文件: ' + FCurrentFile);
    Result := True;
  except
    on E: Exception do
      ShowMessage('保存失败: ' + E.Message);
  end;
end;

function TfrmScriptEditor.DoSaveAs: Boolean;
begin
  Result := False;
  
  SaveDialog.Filter := 'VMA 脚本文件 (*.vma)|*.vma|所有文件 (*.*)|*.*';
  SaveDialog.DefaultExt := 'vma';
  
  if SaveDialog.Execute then
  begin
    FCurrentFile := SaveDialog.FileName;
    Result := DoSave;
  end;
end;

procedure TfrmScriptEditor.RunScript;
var
  Script: string;
begin
  if FRunning then
  begin
    ShowMessage('脚本正在运行中');
    Exit;
  end;
  
  Script := memScript.Text;
  if Trim(Script) = '' then
  begin
    ShowMessage('脚本为空');
    Exit;
  end;
  
  memLog.Clear;
  Log('开始执行脚本...');
  
  if Assigned(FVMAEngine) then
    FVMAEngine.Free;
    
  FVMAEngine := TVMAEngine.Create(FDataDir, FConfigFile);
  
  FRunning := True;
  mnuRunScript.Enabled := False;
  mnuStopScript.Enabled := True;
  lblStatus.Caption := '运行中...';
  pbProgress.Max := memScript.Lines.Count;
  pbProgress.Position := 0;
  
  FScriptThread := TScriptThread.Create(FVMAEngine, Script);
  FScriptThread.Start;
end;

procedure TfrmScriptEditor.StopScript;
begin
  if not FRunning then
    Exit;
    
  if Assigned(FVMAEngine) then
    FVMAEngine.Stop;
    
  FRunning := False;
  mnuRunScript.Enabled := True;
  mnuStopScript.Enabled := False;
  lblStatus.Caption := '已停止';
  Log('脚本已停止');
end;

procedure TfrmScriptEditor.Log(const Msg: string);
begin
  memLog.Lines.Add(FormatDateTime('hh:nn:ss', Now) + ' ' + Msg);
end;

procedure TfrmScriptEditor.ShowPosInfo(X, Y: Integer);
var
  Wnd: HWND;
  WndTitle: array[0..255] of Char;
  WndClass: array[0..255] of Char;
  WndRect: TRect;
begin
  Wnd := WindowFromPoint(Types.Point(X, Y));
  GetWindowText(Wnd, WndTitle, 256);
  GetClassName(Wnd, WndClass, 256);
  GetWindowRect(Wnd, WndRect);
  
  Log(Format('鼠标位置: %d, %d', [X, Y]));
  Log(Format('窗口句柄: 0x%x', [Wnd]));
  Log(Format('窗口标题: %s', [WndTitle]));
  Log(Format('窗口类名: %s', [WndClass]));
  Log(Format('窗口位置: %d, %d, %d, %d', [WndRect.Left, WndRect.Top, WndRect.Right, WndRect.Bottom]));
  
  memScript.Lines.Add(Format('// 鼠标位置: %d, %d', [X, Y]));
  memScript.Lines.Add(Format('// 窗口: %s', [WndTitle]));
  memScript.Lines.Add(Format('click(%d, %d)', [X, Y]));
end;

end.
