unit VMACompiler;

{$mode objfpc}{$H+}
{$codepage UTF8}

interface

uses
  Classes, SysUtils, Forms, Dialogs;

type
  TVMACompiler = class
  private
    FRuntimePath: string;
    FOutputDir: string;
  public
    constructor Create(const ARuntimePath, AOutputDir: string);
    function Compile(const ScriptFile, OutputFile: string): Boolean;
    function CompileScript(const Script, OutputFile: string): Boolean;
  end;

implementation

uses
  Windows, ShellAPI;

const
  SCRIPT_MARKER = '===VMA_SCRIPT_START===';
  SCRIPT_END_MARKER = '===VMA_SCRIPT_END===';

constructor TVMACompiler.Create(const ARuntimePath, AOutputDir: string);
begin
  inherited Create;
  FRuntimePath := ARuntimePath;
  FOutputDir := AOutputDir;
  ForceDirectories(FOutputDir);
end;

function TVMACompiler.Compile(const ScriptFile, OutputFile: string): Boolean;
var
  Script: TStringList;
begin
  Result := False;
  
  if not FileExists(ScriptFile) then
    Exit;
    
  Script := TStringList.Create;
  try
    Script.LoadFromFile(ScriptFile);
    Result := CompileScript(Script.Text, OutputFile);
  finally
    Script.Free;
  end;
end;

function TVMACompiler.CompileScript(const Script, OutputFile: string): Boolean;
var
  RuntimeExe, OutputExe: string;
  RuntimeStream, OutputStream: TFileStream;
  ScriptData: TStringList;
  Buffer: Byte;
  I: Int64;
begin
  Result := False;
  
  RuntimeExe := FRuntimePath;
  if not FileExists(RuntimeExe) then
  begin
    RuntimeExe := ExtractFilePath(ParamStr(0)) + 'VMARuntime.exe';
    if not FileExists(RuntimeExe) then
      Exit;
  end;
  
  if OutputFile = '' then
    OutputExe := FOutputDir + 'compiled_script.exe'
  else
    OutputExe := OutputFile;
    
  if ExtractFileExt(OutputExe) = '' then
    OutputExe := OutputExe + '.exe';
    
  try
    RuntimeStream := TFileStream.Create(RuntimeExe, fmOpenRead or fmShareDenyNone);
    try
      OutputStream := TFileStream.Create(OutputExe, fmCreate);
      try
        OutputStream.CopyFrom(RuntimeStream, 0);
        
        ScriptData := TStringList.Create;
        try
          ScriptData.Add('');
          ScriptData.Add(SCRIPT_MARKER);
          ScriptData.Add(Script);
          ScriptData.Add(SCRIPT_END_MARKER);
          
          ScriptData.SaveToStream(OutputStream);
        finally
          ScriptData.Free;
        end;
        
        Result := True;
      finally
        OutputStream.Free;
      end;
    finally
      RuntimeStream.Free;
    end;
  except
    Result := False;
  end;
end;

end.
