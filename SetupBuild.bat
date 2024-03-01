@echo off
setlocal
set INTERACTIVE=1
echo %cmdcmdline% | find /i "%~0" >nul
if not errorlevel 1 set INTERACTIVE=0

set BASEDIR=%~dp0
pushd %BASEDIR%

cls
powershell -ExecutionPolicy Unrestricted -NoProfile .\bootstrap.ps1;
cls
powershell -ExecutionPolicy Unrestricted -NoProfile .\SetupBuild.ps1;

popd
if %INTERACTIVE% == 0 pause
endlocal