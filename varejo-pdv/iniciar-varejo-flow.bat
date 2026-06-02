@echo off
setlocal
title Varejo Flow
cd /d "%~dp0"

set "PORT=4173"
set "URL=http://127.0.0.1:%PORT%/"

start "" "%URL%"

where py >nul 2>nul
if %ERRORLEVEL%==0 (
  py -3 -m http.server %PORT% --bind 127.0.0.1
  exit /b
)

where python >nul 2>nul
if %ERRORLEVEL%==0 (
  python -m http.server %PORT% --bind 127.0.0.1
  exit /b
)

echo.
echo Nao encontrei Python instalado no computador.
echo Instale o Python em https://www.python.org/downloads/ e tente novamente.
echo.
pause
