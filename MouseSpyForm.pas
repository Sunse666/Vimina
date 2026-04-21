unit MouseSpyForm;

{$mode objfpc}{$H+}
{$codepage UTF8}

interface

uses
  Classes, SysUtils, Forms, Controls, Graphics, Dialogs, StdCtrls, ExtCtrls,
  Windows, UIAutomation, Config, LCLType;

type
  TfrmMouseSpy = class(TForm)
    pnlInfo: TPanel;
    lblMousePos: TLabel;
    lblWindow: TLabel;
    lblHandle: TLabel;
    lblTitle: TLabel;
    lblClass: TLabel;
    lblRect: TLabel;
    lblControl: TLabel;
    lblControlType: TLabel;
    lblControlName: TLabel;
    lblControlRect: TLabel;
    tmrUpdate: TTimer;
    procedure FormCreate(Sender: TObject);
    procedure FormDestroy(Sender: TObject);
    procedure tmrUpdateTimer(Sender: TObject);
    procedure FormClose(Sender: TObject; var CloseAction: TCloseAction);
  private
    FUIAuto: TUIAutomationHelper;
    FLastX, FLastY: Integer;
    procedure UpdateInfo;
    procedure CopyToClipboard(const AText: string);
  public
  end;

var
  frmMouseSpy: TfrmMouseSpy;

implementation

{$R *.lfm}

uses
  Clipbrd, Math;

procedure TfrmMouseSpy.FormCreate(Sender: TObject);
begin
  FUIAuto := TUIAutomationHelper.Create;
  FLastX := -1;
  FLastY := -1;
  
  tmrUpdate.Enabled := True;
  tmrUpdate.Interval := 100;
end;

procedure TfrmMouseSpy.FormDestroy(Sender: TObject);
begin
  FUIAuto.Free;
end;

procedure TfrmMouseSpy.FormClose(Sender: TObject; var CloseAction: TCloseAction);
begin
  CloseAction := caFree;
end;

procedure TfrmMouseSpy.tmrUpdateTimer(Sender: TObject);
begin
  UpdateInfo;
end;

procedure TfrmMouseSpy.UpdateInfo;
var
  P: TPoint;
  Wnd: HWND;
  WndTitle, WndClass: array[0..255] of Char;
  WndRect: TRect;
  CtrlInfo: TControlInfo;
  CtrlCount: Integer;
  CtrlList: array[0..0] of TControlInfo;
begin
  GetCursorPos(P);
  
  if (P.X = FLastX) and (P.Y = FLastY) then
    Exit;
    
  FLastX := P.X;
  FLastY := P.Y;
  
  lblMousePos.Caption := Format('鼠标位置: %d, %d', [P.X, P.Y]);
  
  Wnd := WindowFromPoint(P);
  if Wnd = 0 then
  begin
    lblHandle.Caption := '窗口句柄: 无';
    lblTitle.Caption := '窗口标题: -';
    lblClass.Caption := '窗口类名: -';
    lblRect.Caption := '窗口位置: -';
    Exit;
  end;
  
  GetWindowText(Wnd, WndTitle, 256);
  GetClassName(Wnd, WndClass, 256);
  GetWindowRect(Wnd, WndRect);
  
  lblHandle.Caption := Format('窗口句柄: 0x%x (%d)', [Wnd, Wnd]);
  lblTitle.Caption := Format('窗口标题: %s', [WndTitle]);
  lblClass.Caption := Format('窗口类名: %s', [WndClass]);
  lblRect.Caption := Format('窗口位置: (%d,%d)-(%d,%d) [%dx%d]', [
    WndRect.Left, WndRect.Top, WndRect.Right, WndRect.Bottom,
    WndRect.Right - WndRect.Left, WndRect.Bottom - WndRect.Top]);
  
  CtrlCount := FUIAuto.GetControlAtPoint(P.X, P.Y, CtrlInfo);
  if CtrlCount > 0 then
  begin
    lblControl.Caption := Format('控件: %s', [CtrlInfo.TypeDesc]);
    lblControlType.Caption := Format('控件类型: %s', [CtrlInfo.ControlType]);
    lblControlName.Caption := Format('控件名称: %s', [CtrlInfo.Name]);
    lblControlRect.Caption := Format('控件位置: (%d,%d)-(%d,%d) [%dx%d]', [
      CtrlInfo.X, CtrlInfo.Y, 
      CtrlInfo.X + CtrlInfo.Width, CtrlInfo.Y + CtrlInfo.Height,
      CtrlInfo.Width, CtrlInfo.Height]);
  end
  else
  begin
    lblControl.Caption := '控件: 未找到';
    lblControlType.Caption := '控件类型: -';
    lblControlName.Caption := '控件名称: -';
    lblControlRect.Caption := '控件位置: -';
  end;
end;

procedure TfrmMouseSpy.CopyToClipboard(const AText: string);
begin
  Clipboard.AsText := AText;
end;

end.
