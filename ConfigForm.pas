unit ConfigForm;

{$mode objfpc}{$H+}

interface

uses
  Classes, SysUtils, Forms, Controls, Graphics, Dialogs, StdCtrls, ExtCtrls,
  ColorBox, Spin, ButtonPanel, ComCtrls, Config;

type
  TfrmConfig = class(TForm)
    ButtonPanel1: TButtonPanel;
    PageControl1: TPageControl;
    tsLabel: TTabSheet;
    tsFilter: TTabSheet;
    tsClick: TTabSheet;
    
    grpLabelColors: TGroupBox;
    lblDefaultColor: TLabel;
    lblMatchColor: TLabel;
    lblPrefixColor: TLabel;
    lblInvalidColor: TLabel;
    lblTextColor: TLabel;
    cbDefaultColor: TColorBox;
    cbMatchColor: TColorBox;
    cbPrefixColor: TColorBox;
    cbInvalidColor: TColorBox;
    cbTextColor: TColorBox;
    
    grpLabelStyle: TGroupBox;
    lblFontSize: TLabel;
    lblOffsetX: TLabel;
    lblOffsetY: TLabel;
    seFontSize: TSpinEdit;
    seOffsetX: TSpinEdit;
    seOffsetY: TSpinEdit;
    
    grpFilter: TGroupBox;
    lblMinWidth: TLabel;
    lblMinHeight: TLabel;
    lblMaxDepth: TLabel;
    seMinWidth: TSpinEdit;
    seMinHeight: TSpinEdit;
    seMaxDepth: TSpinEdit;
    
    grpClick: TGroupBox;
    chkBringToFront: TCheckBox;
    chkUseMouseClick: TCheckBox;
    lblClickDelay: TLabel;
    seClickDelay: TSpinEdit;
    
    procedure FormCreate(Sender: TObject);
    procedure OKButtonClick(Sender: TObject);
  private
    FConfig: PViminaConfig;
    procedure LoadConfigToUI;
    procedure SaveUIToConfig;
  public
    procedure SetConfig(Config: PViminaConfig);
  end;

var
  frmConfig: TfrmConfig;

implementation

{$R *.lfm}

procedure TfrmConfig.FormCreate(Sender: TObject);
begin
  PageControl1.ActivePage := tsLabel;
end;

procedure TfrmConfig.SetConfig(Config: PViminaConfig);
begin
  FConfig := Config;
  LoadConfigToUI;
end;

procedure TfrmConfig.LoadConfigToUI;
begin
  if FConfig = nil then Exit;
  
  cbDefaultColor.Selected := FConfig^.LabelCfg.BackgroundColor_Default;
  cbMatchColor.Selected := FConfig^.LabelCfg.BackgroundColor_Match;
  cbPrefixColor.Selected := FConfig^.LabelCfg.BackgroundColor_Prefix;
  cbInvalidColor.Selected := FConfig^.LabelCfg.BackgroundColor_Invalid;
  cbTextColor.Selected := FConfig^.LabelCfg.TextColor;
  
  seFontSize.Value := FConfig^.LabelCfg.FontSize;
  seOffsetX.Value := FConfig^.LabelCfg.OffsetX;
  seOffsetY.Value := FConfig^.LabelCfg.OffsetY;
  
  seMinWidth.Value := FConfig^.FilterCfg.MinWidth;
  seMinHeight.Value := FConfig^.FilterCfg.MinHeight;
  seMaxDepth.Value := FConfig^.FilterCfg.MaxDepth;
  
  seClickDelay.Value := FConfig^.PerfCfg.ClickDelay;
  chkBringToFront.Checked := FConfig^.ClickModeCfg.BringToFront;
  chkUseMouseClick.Checked := FConfig^.ClickModeCfg.UseMouseClick;
end;

procedure TfrmConfig.SaveUIToConfig;
begin
  if FConfig = nil then Exit;
  
  FConfig^.LabelCfg.BackgroundColor_Default := cbDefaultColor.Selected;
  FConfig^.LabelCfg.BackgroundColor_Match := cbMatchColor.Selected;
  FConfig^.LabelCfg.BackgroundColor_Prefix := cbPrefixColor.Selected;
  FConfig^.LabelCfg.BackgroundColor_Invalid := cbInvalidColor.Selected;
  FConfig^.LabelCfg.TextColor := cbTextColor.Selected;
  
  FConfig^.LabelCfg.FontSize := seFontSize.Value;
  FConfig^.LabelCfg.OffsetX := seOffsetX.Value;
  FConfig^.LabelCfg.OffsetY := seOffsetY.Value;
  
  FConfig^.FilterCfg.MinWidth := seMinWidth.Value;
  FConfig^.FilterCfg.MinHeight := seMinHeight.Value;
  FConfig^.FilterCfg.MaxDepth := seMaxDepth.Value;
  
  FConfig^.PerfCfg.ClickDelay := seClickDelay.Value;
  FConfig^.ClickModeCfg.BringToFront := chkBringToFront.Checked;
  FConfig^.ClickModeCfg.UseMouseClick := chkUseMouseClick.Checked;
end;

procedure TfrmConfig.OKButtonClick(Sender: TObject);
begin
  SaveUIToConfig;
  ModalResult := mrOK;
end;

end.
