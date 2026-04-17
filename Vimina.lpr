program Vimina;

{$mode objfpc}{$H+}

uses
  Interfaces,
  Forms, SysUtils, Windows,
  MainForm, Config, HttpServer;

{$R *.res}

var
  MutexHandle: THandle;
  
begin
  MutexHandle := CreateMutex(nil, True, 'Vimina_55EE5235837B43848C2E8ADD79C8C317');
  if GetLastError = ERROR_ALREADY_EXISTS then
  begin
    MessageBox(0, 'Vimina 已经在运行!', '提示', MB_OK or MB_ICONINFORMATION);
    Exit;
  end;
  
  try
    Application.Scaled:=True;
    Application.MainFormOnTaskBar := True;
    Application.Initialize;
    Application.CreateForm(TfrmMain, frmMain);
    Application.ShowMainForm := True;
    Application.Run;
  finally
    if MutexHandle <> 0 then
      CloseHandle(MutexHandle);
  end;
end.
