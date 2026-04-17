unit UIAutomation;

{$mode objfpc}{$H+}

interface

uses
  Classes, SysUtils, Windows, ComObj, ActiveX, Variants, Config;

type
  TUIAutomationHelper = class
  private
    FAutomation: OleVariant;
    FInitialized: Boolean;
  public
    constructor Create;
    destructor Destroy; override;
    function GetInteractiveControls(Hwnd: HWND; var Controls: array of TControlInfo; MaxDepth: Integer = 50): Integer;
    function EnumerateElements(RootElement: OleVariant; var Controls: array of TControlInfo; 
      MaxControls: Integer; CurrentLevel: Integer; MaxLevel: Integer): Integer;
  end;

implementation

const
  UIA_ButtonControlTypeId = 50000;
  UIA_CheckBoxControlTypeId = 50002;
  UIA_ComboBoxControlTypeId = 50003;
  UIA_EditControlTypeId = 50004;
  UIA_HyperlinkControlTypeId = 50005;
  UIA_ListItemControlTypeId = 50007;
  UIA_MenuItemControlTypeId = 50011;
  UIA_RadioButtonControlTypeId = 50013;
  UIA_SliderControlTypeId = 50015;
  UIA_SpinnerControlTypeId = 50016;
  UIA_SplitButtonControlTypeId = 50031;
  UIA_TabItemControlTypeId = 50019;
  UIA_TreeItemControlTypeId = 50023;
  UIA_DataItemControlTypeId = 50025;

constructor TUIAutomationHelper.Create;
begin
  inherited Create;
  FInitialized := False;
  
  try
    FAutomation := CreateOleObject('UIAutomationCore.CUIAutomation');
    FInitialized := True;
  except
    on E: Exception do
    begin
      try
        FAutomation := CreateOleObject('UIAutomationCore.CUIAutomation8');
        FInitialized := True;
      except
        FInitialized := False;
      end;
    end;
  end;
end;

destructor TUIAutomationHelper.Destroy;
begin
  if FInitialized then
  begin
    FAutomation := Unassigned;
  end;
  inherited Destroy;
end;

function TUIAutomationHelper.EnumerateElements(RootElement: OleVariant; 
  var Controls: array of TControlInfo; MaxControls: Integer; 
  CurrentLevel: Integer; MaxLevel: Integer): Integer;
var
  Walker: OleVariant;
  Element: OleVariant;
  ControlType: Integer;
  BoundingRect: OleVariant;
  Name: WideString;
  TypeInfo: TControlTypeInfo;
  ScreenWidth, ScreenHeight: Integer;
  Left, Top, Width, Height: Integer;
  Count: Integer;
begin
  Result := 0;
  
  if CurrentLevel > MaxLevel then
    Exit;
    
  if not FInitialized then
    Exit;

  try
    ScreenWidth := GetSystemMetrics(SM_CXSCREEN);
    ScreenHeight := GetSystemMetrics(SM_CYSCREEN);
    
    Walker := FAutomation.ControlViewWalker;
    Element := Walker.GetFirstChildElement(RootElement);
    Count := 0;
    
    while not VarIsNull(Element) and not VarIsEmpty(Element) and (Count < MaxControls) do
    begin
      try
        ControlType := Element.CurrentControlType;
        
        if IsInteractiveControl(ControlType) then
        begin
          BoundingRect := Element.CurrentBoundingRectangle;
          
          Left := Integer(Trunc(Double(BoundingRect.left)));
          Top := Integer(Trunc(Double(BoundingRect.top)));
          Width := Integer(Trunc(Double(BoundingRect.right - BoundingRect.left)));
          Height := Integer(Trunc(Double(BoundingRect.bottom - BoundingRect.top)));
          
          if (Width >= 8) and (Height >= 8) and
             (Left + Width > 0) and (Top + Height > 0) and
             (Left < ScreenWidth) and (Top < ScreenHeight) then
          begin
            TypeInfo := GetControlTypeInfo(ControlType);
            
            try
              Name := Element.CurrentName;
            except
              Name := '';
            end;
            
            Controls[Count].Name := UTF8Encode(Name);
            Controls[Count].ControlType := TypeInfo.Name;
            Controls[Count].TypeNum := ControlType;
            Controls[Count].TypeDesc := TypeInfo.Desc;
            Controls[Count].ActionHint := TypeInfo.Action;
            Controls[Count].X := Left;
            Controls[Count].Y := Top;
            Controls[Count].Width := Width;
            Controls[Count].Height := Height;
            Controls[Count].CenterX := Left + Width div 2;
            Controls[Count].CenterY := Top + Height div 2;
            Controls[Count].LabelText := '';
            Controls[Count].Hwnd := 0;
            
            Inc(Count);
          end;
        end;
        
        if Count < MaxControls then
        begin
          Count := Count + EnumerateElements(Element, Controls[Count], 
            MaxControls - Count, CurrentLevel + 1, MaxLevel);
        end;
        
        Element := Walker.GetNextSiblingElement(Element);
      except
        Element := Walker.GetNextSiblingElement(Element);
      end;
    end;
    
    Result := Count;
  except
    Result := 0;
  end;
end;

function TUIAutomationHelper.GetInteractiveControls(Hwnd: HWND; 
  var Controls: array of TControlInfo; MaxDepth: Integer): Integer;
var
  RootElement: OleVariant;
begin
  Result := 0;
  
  if not FInitialized then
    Exit;

  try
    RootElement := FAutomation.ElementFromHandle(Hwnd);
    
    if VarIsNull(RootElement) or VarIsEmpty(RootElement) then
      Exit;
      
    Result := EnumerateElements(RootElement, Controls, Length(Controls), 0, MaxDepth);
  except
    Result := 0;
  end;
end;

end.
