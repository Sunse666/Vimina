program VMARuntime;

{$mode objfpc}{$H+}
{$codepage UTF8}

uses
  Interfaces, Forms, SysUtils, Windows, Classes,
  VMAEngine;

var
  Engine: TVMAEngine;
  ScriptFile: string;
  Script: TStringList;
  Result: TJSONObject;
  DataDir: string;
  
const
  SCRIPT_MARKER = '===VMA_SCRIPT_START===';
  SCRIPT_END_MARKER = '===VMA_SCRIPT_END===';
  
function ExtractEmbeddedScript: string;
var
  ExePath: string;
  ExeStream, ScriptStream: TFileStream;
  Buffer: array[0..1023] of Byte;
  Marker: string;
  FoundStart, FoundEnd: Boolean;
  BytesRead: LongInt;
  ScriptData: TStringList;
  I: Int64;
  FileSize: Int64;
begin
  Result := '';
  ExePath := ParamStr(0);
  
  ExeStream := TFileStream.Create(ExePath, fmOpenRead or fmShareDenyNone);
  try
    FileSize := ExeStream.Size;
    
    ExeStream.Seek(-Length(SCRIPT_MARKER) - 10000, soFromEnd);
    
    ScriptStream := TFileStream.Create(ExePath + '.tmp', fmCreate);
    try
      ScriptStream.CopyFrom(ExeStream, 0);
    finally
      ScriptStream.Free;
    end;
    
    ScriptData := TStringList.Create;
    try
      ScriptData.LoadFromFile(ExePath + '.tmp');
      
      for I := 0 to ScriptData.Count - 1 do
      begin
        if Pos(SCRIPT_MARKER, ScriptData[I]) > 0 then
        begin
          Result := '';
          for I := I + 1 to ScriptData.Count - 1 do
          begin
            if Pos(SCRIPT_END_MARKER, ScriptData[I]) > 0 then
              Break;
            Result := Result + ScriptData[I] + LineEnding;
          end;
          Break;
        end;
      end;
    finally
      ScriptData.Free;
    end;
    
    DeleteFile(ExePath + '.tmp');
  finally
    ExeStream.Free;
  end;
end;

begin
  DataDir := ExtractFilePath(ParamStr(0)) + 'data' + PathDelim;
  ForceDirectories(DataDir);
  
  Engine := TVMAEngine.Create(DataDir, '');
  try
    if ParamCount >= 1 then
    begin
      ScriptFile := ParamStr(1);
      if FileExists(ScriptFile) then
      begin
        Script := TStringList.Create;
        try
          Script.LoadFromFile(ScriptFile);
          Result := Engine.Run(Script.Text);
          try
            if Result.Get('success', False) then
              WriteLn('脚本执行成功')
            else
              WriteLn('脚本执行失败: ' + Result.Get('error', '未知错误'));
          finally
            Result.Free;
          end;
        finally
          Script.Free;
        end;
      end
      else
        WriteLn('文件不存在: ' + ScriptFile);
    end
    else
    begin
      Script := TStringList.Create;
      try
        Script.Text := ExtractEmbeddedScript;
        if Script.Text <> '' then
        begin
          Result := Engine.Run(Script.Text);
          try
            if Result.Get('success', False) then
              WriteLn('脚本执行成功')
            else
              WriteLn('脚本执行失败: ' + Result.Get('error', '未知错误'));
          finally
            Result.Free;
          end;
        end
        else
        begin
          WriteLn('VMA Runtime v1.0');
          WriteLn('用法: VMARuntime.exe <script.vma>');
          WriteLn('');
          WriteLn('或者将脚本嵌入到可执行文件中运行');
        end;
      finally
        Script.Free;
      end;
    end;
  finally
    Engine.Free;
  end;
end.
