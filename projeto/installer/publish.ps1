# Script PowerShell para gerar os binários publicados antes de rodar o Inno Setup
# Uso: powershell -ExecutionPolicy Bypass -File installer\publish.ps1

$ErrorActionPreference = "Stop"
$Root = Split-Path $PSScriptRoot -Parent
Set-Location $Root

Write-Host "==> Publicando ProjetoVarejo.Desktop.Wpf (ERP principal)..." -ForegroundColor Cyan
dotnet publish src/ProjetoVarejo.Desktop.Wpf -c Release -r win-x64 --self-contained false -o publish/wpf

Write-Host ""
Write-Host "==> Publicando ProjetoVarejo.Desktop (PDV / frente de caixa)..." -ForegroundColor Cyan
dotnet publish src/ProjetoVarejo.Desktop -c Release -r win-x64 --self-contained false -o publish/desktop

Write-Host ""
Write-Host "==> Publicando ProjetoVarejo.Api..." -ForegroundColor Cyan
dotnet publish src/ProjetoVarejo.Api -c Release -r win-x64 --self-contained false -o publish/api

Write-Host ""
Write-Host "==> Publicacao concluida!" -ForegroundColor Green
Write-Host "   wpf\     -> publish\wpf"
Write-Host "   desktop\ -> publish\desktop"
Write-Host "   api\     -> publish\api"
Write-Host ""
Write-Host "Proximo passo: compile installer\setup.iss no Inno Setup 6+"
Write-Host "Arquivo de saida: installer\output\ProjetoERP-Setup-1.0.0.exe"
