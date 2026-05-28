# Script para instalar WSL 2 e configurar Docker
# Execute como Administrador no PowerShell

Write-Host "========================================" -ForegroundColor Green
Write-Host "Instalando WSL 2" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Habilitar WSL
Write-Host "`n1. Habilitando Windows Subsystem for Linux..."
dism.exe /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart

# Habilitar Virtual Machine Platform
Write-Host "`n2. Habilitando Virtual Machine Platform..."
dism.exe /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart

# Instalar kernel do WSL 2
Write-Host "`n3. Baixando e instalando Kernel do WSL 2..."
$ProgressPreference = 'SilentlyContinue'
Invoke-WebRequest -Uri "https://wslstorestorage.blob.core.windows.net/wslblob/wsl_update_x64.msi" -OutFile "$env:TEMP\wsl_update_x64.msi"
msiexec.exe /i "$env:TEMP\wsl_update_x64.msi" /quiet /norestart
Remove-Item "$env:TEMP\wsl_update_x64.msi"

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "IMPORTANTE: Você precisa REINICIAR SEU COMPUTADOR!" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Green
Write-Host "`nApós reiniciar, execute estes comandos:" -ForegroundColor Cyan
Write-Host "  1. wsl --set-default-version 2" -ForegroundColor Cyan
Write-Host "  2. docker ps" -ForegroundColor Cyan
Write-Host "  3. cd C:\Users\victo\Documents\projeto" -ForegroundColor Cyan
Write-Host "  4. docker-compose up" -ForegroundColor Cyan

Read-Host "`nPressione Enter para REINICIAR agora"
Restart-Computer -Force
