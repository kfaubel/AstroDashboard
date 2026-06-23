@echo off
setlocal
REM Quick release script for AstroDashboard
REM Usage: release.cmd [major|minor|patch]

set BUMP=%1
if "%BUMP%"=="" set BUMP=patch

echo Running release with bump type: %BUMP%
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0release.ps1" -BumpType %BUMP%
endlocal
