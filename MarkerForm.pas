unit MarkerForm;

{$mode objfpc}{$H+}

interface

uses
  Classes, SysUtils, Forms, Controls, Graphics, Dialogs, StdCtrls, ExtCtrls,
  LCLType, LCLIntf, Config;

type
  TfrmMarker = class(TForm)
    lblMarker: TLabel;
  private
    FLabelText: string;
  public
    constructor CreateNew(AOwner: TComponent; X, Y: Integer; 
      const ALabel: string; const Cfg: TLabelConfig); reintroduce;
    property LabelText: string read FLabelText;
    procedure SetBackgroundColor(AColor: TColor);
  end;

implementation

constructor TfrmMarker.CreateNew(AOwner: TComponent; X, Y: Integer; 
  const ALabel: string; const Cfg: TLabelConfig);
var
  LabelWidth, LabelHeight: Integer;
begin
  inherited CreateNew(AOwner);
  
  FLabelText := ALabel;
  LabelWidth := Length(ALabel) * Cfg.FontSize + 6;
  LabelHeight := Cfg.FontSize + 6;
  
  BorderStyle := bsNone;
  FormStyle := fsSystemStayOnTop;
  Position := poDesigned;
  Left := X;
  Top := Y;
  Width := LabelWidth;
  Height := LabelHeight;
  
  lblMarker := TLabel.Create(Self);
  lblMarker.Parent := Self;
  lblMarker.Align := alClient;
  lblMarker.Alignment := taCenter;
  lblMarker.Layout := tlCenter;
  lblMarker.Caption := ALabel;
  lblMarker.Font.Name := 'Consolas';
  lblMarker.Font.Size := Cfg.FontSize;
  lblMarker.Font.Style := [fsBold];
  lblMarker.Font.Color := Cfg.TextColor;
  lblMarker.Color := Cfg.BackgroundColor_Default;
  lblMarker.Transparent := False;
  
  {$IFDEF WINDOWS}
  // 设置窗口为工具窗口，不在任务栏显示
  SetWindowLong(Handle, GWL_EXSTYLE, 
    GetWindowLong(Handle, GWL_EXSTYLE) or WS_EX_TOOLWINDOW and not WS_EX_APPWINDOW);
  {$ENDIF}
end;

procedure TfrmMarker.SetBackgroundColor(AColor: TColor);
begin
  lblMarker.Color := AColor;
  Invalidate;
end;

end.