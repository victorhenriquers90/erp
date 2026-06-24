# Script PowerShell para gerar os binarios publicados antes de rodar o Inno Setup.
# Uso:
#   powershell -ExecutionPolicy Bypass -File installer\publish.ps1
#   powershell -ExecutionPolicy Bypass -File installer\publish.ps1 -IncludeApi
#   powershell -ExecutionPolicy Bypass -File installer\publish.ps1 -IncludeErp

param(
    [switch]$IncludeApi,
    [switch]$IncludeErp
)

$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")

function Invoke-DotnetPublish {
    param(
        [string]$ProjectPath,
        [string]$OutputPath
    )

    & dotnet publish $ProjectPath -c Release -r win-x64 --self-contained false -o $OutputPath
    if ($LASTEXITCODE -ne 0) {
        throw "Falha no publish de '$ProjectPath'. ExitCode=$LASTEXITCODE"
    }
}

Write-Host "==> Publicando PROJETO NEGOCIOS (Desktop)..." -ForegroundColor Cyan
Invoke-DotnetPublish -ProjectPath "src/ProjetoVarejo.Desktop.Wpf" -OutputPath "publish/desktop-wpf"

if ($IncludeErp) {
    Write-Host ""
    Write-Host "==> Publicando PROJETO ERP (Desktop)..." -ForegroundColor Cyan
    Invoke-DotnetPublish -ProjectPath "src/ProjetoVarejo.Desktop.Erp" -OutputPath "publish/desktop-erp"
}

if ($IncludeApi) {
    Write-Host ""
    Write-Host "==> Publicando ProjetoVarejo.Api..." -ForegroundColor Cyan
    Invoke-DotnetPublish -ProjectPath "src/ProjetoVarejo.Api" -OutputPath "publish/api"
}

Write-Host ""
Write-Host "==> Pronto. Agora compile o instalador correspondente no Inno Setup 6+." -ForegroundColor Green
Write-Host "   Arquivo gerado em: installer\output\ProjetoVarejo-Setup-1.0.0.exe"
