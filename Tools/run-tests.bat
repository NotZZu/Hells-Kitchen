@echo off
setlocal EnableExtensions
rem ============================================================
rem  Hell's Kitchen - Unity EditMode tests, headless run
rem  Usage: Tools\run-tests.bat            (Unity editor must be closed)
rem  Env  : UNITY_EXE  = full path of Unity.exe (optional override)
rem         NOPAUSE=1  = do not wait for a key at the end
rem  Out  : TestResults\editmode.xml (NUnit3), TestResults\editmode.log
rem  Exit : 0 = all passed, 2 = some tests failed, other = run error
rem ============================================================

set "PROJECT=%~dp0.."
for %%I in ("%PROJECT%") do set "PROJECT=%%~fI"

rem Editor version is read from ProjectVersion.txt
for /f "tokens=2 delims= " %%V in ('findstr /b /c:"m_EditorVersion:" "%PROJECT%\ProjectSettings\ProjectVersion.txt"') do set "UNITY_VER=%%V"
if not defined UNITY_EXE set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\%UNITY_VER%\Editor\Unity.exe"

if not exist "%UNITY_EXE%" (
  echo [ERROR] Unity.exe not found: "%UNITY_EXE%"
  echo         Set UNITY_EXE to the full path and run again.
  set "RC=10"
  goto :end
)

rem A running editor locks the project; stop here instead of failing later
tasklist /fi "imagename eq Unity.exe" 2>nul | find /i "Unity.exe" >nul
if not errorlevel 1 (
  echo [ERROR] Unity editor is running. Close it first ^(project lock^).
  set "RC=11"
  goto :end
)

set "OUT=%PROJECT%\TestResults"
if not exist "%OUT%" mkdir "%OUT%"
if exist "%OUT%\editmode.xml" del /q "%OUT%\editmode.xml"

echo Unity   : %UNITY_VER%
echo Project : %PROJECT%
echo Running EditMode tests... ^(a few minutes on first run^)
"%UNITY_EXE%" -batchmode -nographics -projectPath "%PROJECT%" -runTests -testPlatform EditMode -testResults "%OUT%\editmode.xml" -logFile "%OUT%\editmode.log"
set "RC=%ERRORLEVEL%"

echo.
echo Unity exit code: %RC%  ^(0 = passed, 2 = tests failed^)
if exist "%OUT%\editmode.xml" (
  echo Result file    : %OUT%\editmode.xml
  findstr /r /c:"<test-run " "%OUT%\editmode.xml"
) else (
  echo [ERROR] No result XML. See %OUT%\editmode.log
)

:end
if not defined NOPAUSE pause
exit /b %RC%
