@echo off
setlocal
set INTERACTIVE=1
echo %cmdcmdline% | find /i "%~0" >nul
if not errorlevel 1 set INTERACTIVE=0
set BASEDIR=%~dp0

:CheckAndCleanBuildLogs
if not exist BuildLogs goto SetupBootStrapAndPullTools
echo Removing build logs
del BuildLogs /S /Q
rd BuildLogs

:SetupBootStrapAndPullTools
pushd %BASEDIR%
powershell -ExecutionPolicy Unrestricted -NoProfile .\bootstrap.ps1
mkdir BuildLogs
goto CheckForLocalMSBuild

:Error
echo CAN'T FIND INSTALLED MSBUILD!!!!!
goto Exit

:CheckForVS2012
echo Checking VS2012
echo Program files msbuild does not exist! using windows directory
set FRAMEWORK_VERSION=v4.0.30319
set MSBUILDEXE="%WINDIR%\Microsoft.NET\Framework\%FRAMEWORK_VERSION%\msbuild.exe"
if not exist %MSBUILDEXE% goto Error
echo Using VS2012
goto SetupMSBuildVariables

:CheckForVS2013
echo Checking VS2013
set FRAMEWORK_VERSION=12.0
set MSBUILDEXE="%PROGRAMFILES(x86)%\MSBuild\%FRAMEWORK_VERSION%\Bin\msbuild.exe"
if not exist %MSBUILDEXE% goto CheckForVS2012
echo Using VS2013
goto SetupMSBuildVariables

:CheckForVS2015
echo Checking VS2015
set FRAMEWORK_VERSION=14.0
set MSBUILDEXE="%PROGRAMFILES(x86)%\MSBuild\%FRAMEWORK_VERSION%\Bin\msbuild.exe"
if not exist %MSBUILDEXE% goto CheckForVS2013
echo Using VS2015
goto SetupMSBuildVariables

:CheckForVS2017
echo Checking VS2017
for /f "usebackq tokens=*" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -version ^[15.0^,16.0^)  -products * -requires Microsoft.Component.MSBuild -property installationPath`) do (
  set InstallDir=%%i
)
set FRAMEWORK_VERSION=15.0
set MSBUILDEXE="%InstallDir%\MSBuild\%FRAMEWORK_VERSION%\Bin\msbuild.exe"
if not exist %MSBUILDEXE% goto CheckForVS2015
echo Using VS2017
goto SetupMSBuildVariables

:CheckForVS2019
echo Checking VS2019
if not exist "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" (
  echo "WARNING: VSWhere is not installed OR You need a minimum of VS 2017 version 15.2 or later (for vswhere.exe)"
  goto CheckForVS2015
)
for /f "usebackq tokens=*" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -version ^[16.0^,17.0^)  -products * -requires Microsoft.Component.MSBuild -property installationPath`) do (
  set InstallDir=%%i
)
set FRAMEWORK_VERSION=16.0
set MSBUILDEXE="%InstallDir%\MSBuild\Current\Bin\msbuild.exe"
if not exist %MSBUILDEXE% goto CheckForVS2017
echo Using VS2019
goto SetupMSBuildVariables

:CheckForLocalMSBuild
echo Checking Local MSBuild
set MSBUILDEXE="tools\System.Utility.DERBS.MSBuild\MSBuild\Bin\MSBuild.exe"
if not exist %MSBUILDEXE% goto CheckForVS2019
echo Using Local MSBuild
goto SetupMSBuildVariables

:SetupMSBuildVariables
set NAME=%~n0
set MSBUILD=%MSBUILDEXE% /nologo /clp:verbosity=normal /clp:summary /flp:verbosity=detailed;logfile=BuildLogs\%NAME%.detailed.log /flp1:errorsonly;logfile=BuildLogs\%NAME%.errors.log /flp2:warningsonly;logfile=BuildLogs\%NAME%.warnings.log /flp3:verbosity=normal;logfile=BuildLogs\%NAME%.log

:RunBuild
echo Starting build
%MSBUILD% %NAME%.proj /nr:false /maxcpucount %*

:Exit
popd
if %INTERACTIVE% == 0 pause
endlocal