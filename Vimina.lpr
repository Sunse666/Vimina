program Vimina;

{$mode objfpc}{$H+}

uses
  {$IFDEF UNIX}
  cthreads,
  {$ENDIF}
  {$IFDEF HASAMIGA}
  athreads,
  {$ENDIF}
  Interfaces, // this includes the LCL widgetset
  Forms, SysUtils, Windows,
  MainForm, UIAutomation, HttpServer, MarkerForm, Config;

{$R *.res}

var
  MutexHandle: THandle;
  
begin
  // 单实例检查
  MutexHandle := CreateMutex(nil, True, 'Vimina_55EE5235837B43848C2E8ADD79C8C317');
  if GetLastError = ERROR_ALREADY_EXISTS then
  begin
    MessageBox(0, 'Vimina 已经在运行!', '提示', MB_OK or MB_ICONINFORMATION);
    Exit;
  end;
  
  try
    RequireDerivedFormResource := True;
  Application.Scaled:=True;
    Application.Initialize;
    Application.CreateForm(TfrmMain, frmMain);
    Application.Run;
  finally
    if MutexHandle <> 0 then
      CloseHandle(MutexHandle);
  end;
end.