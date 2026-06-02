# Script PowerShell para gerar os binários publicados antes de rodar o Inno Setup
# Uso: powershell -ExecutionPolicy Bypass -File installer\publish.ps1

$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")

Write-Host "==> Publicando ProjetoVarejo.Desktop..." -ForegroundColor Cyan
dotnet publish src/ProjetoVarejo.Desktop -c Release -r win-x64 --self-contained false -o publish/desktop

Write-Host ""
Write-Host "==> Publicando ProjetoVarejo.Api..." -ForegroundColor Cyan
dotnet publish src/ProjetoVarejo.Api -c Release -r win-x64 --self-contained false -o publish/api

Write-Host ""
Write-Host "==> Pronto. Agora compile installer\setup.iss no Inno Setup 6+." -ForegroundColor Green
Write-Host "   Arquivo gerado em: installer\output\ProjetoVarejo-Setup-1.0.0.exe"
