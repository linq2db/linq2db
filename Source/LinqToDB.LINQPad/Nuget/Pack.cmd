@echo on
setlocal

REM %~1 = TargetDir (always ends with a backslash \). Packs the LINQPad 5 .lpx plugin.
set "OUTDIR=%~1"

echo Packing lpx
REM .build\bin\LinqToDB.LINQPad\Debug\TFM\..\..\..\..\lpx -> .build\lpx
set "RELDIR=%OUTDIR%..\..\..\..\lpx"
REM .build\bin\LinqToDB.LINQPad\Debug\TFM\..\..\..\..\..\Source\LinqToDB.LINQPad\Nuget
set "RESDIR=%OUTDIR%..\..\..\..\..\Source\LinqToDB.LINQPad\Nuget"

if not exist "%RELDIR%" mkdir "%RELDIR%"

REM Clean previous artifacts, but don't fail if they don't exist
if exist "%RELDIR%\linq2db.LINQPad.lpx" del /q "%RELDIR%\linq2db.LINQPad.lpx"
if exist "%RELDIR%\linq2db.LINQPad.lpx.zip" del /q "%RELDIR%\linq2db.LINQPad.lpx.zip"

REM LINQPad 5 has no use for satellite folders and extra files
for %%L in (cs,de,es,fr,it,ja,ko,pl,pt,pt-BR,ru,tr,zh-Hans,zh-Hant) do (
  if exist "%OUTDIR%%%L" rd /s /q "%OUTDIR%%%L"
)
del /q "%OUTDIR%linq2db*.xml" 2>nul
del /q "%OUTDIR%*.pdb"        2>nul

REM Look for 7z; if not found, fall back to PowerShell Compress-Archive
set "SEVENZ=%ProgramFiles%\7-Zip\7z.exe"
if not exist "%SEVENZ%" set "SEVENZ=%ProgramW6432%\7-Zip\7z.exe"
if not exist "%SEVENZ%" set "SEVENZ=%ProgramFiles(x86)%\7-Zip\7z.exe"

if exist "%SEVENZ%" (
  "%SEVENZ%" -r a "%RELDIR%\linq2db.LINQPad.lpx.zip" ^
    "%OUTDIR%*.*" ^
    "%RESDIR%\Connection.png" "%RESDIR%\FailedConnection.png" "%RESDIR%\header.xml"
) else (
  powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "Compress-Archive -Path '%OUTDIR%*.*','%RESDIR%\Connection.png','%RESDIR%\FailedConnection.png','%RESDIR%\header.xml' -DestinationPath '%RELDIR%\linq2db.LINQPad.lpx.zip' -Force"
)

if not exist "%RELDIR%\linq2db.LINQPad.lpx.zip" (
  echo [ERROR] Archive was not created. Check that input files exist and 7-Zip/PowerShell are available.
  exit /b 1
)

pushd "%RELDIR%"
ren "linq2db.LINQPad.lpx.zip" "linq2db.LINQPad.lpx"
popd

echo "Packed -> %RELDIR%\linq2db.LINQPad.lpx"
endlocal
