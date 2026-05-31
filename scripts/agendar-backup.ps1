# ============================================================
# Agendamento do backup diario do ProjetoVarejo no Windows
# Execute como Administrador UMA ÚNICA VEZ no servidor
# ============================================================

param(
    [string]$HorarioBackup  = "23:00",
    [string]$PastaBackup    = "C:\Backups\ProjetoVarejo",
    [string]$ServidorSQL    = ".\SQLEXPRESS"
)

$nomeTask   = "ProjetoVarejo_BackupDiario"
$descricao  = "Backup diario automatico do banco ProjetoVarejo"

# Garantir que a pasta exista
if (-not (Test-Path $PastaBackup)) {
    New-Item -ItemType Directory -Path $PastaBackup -Force | Out-Null
    Write-Host "Pasta criada: $PastaBackup" -ForegroundColor Green
}

# Comando que sera agendado (sqlcmd executando o script .sql)
$scriptSql = Join-Path $PSScriptRoot "backup-sqlserver.sql"
$comando   = "sqlcmd"
$argumentos = "-S `"$ServidorSQL`" -E -i `"$scriptSql`" -v pasta=`"$PastaBackup\`""

# Remover task anterior se existir
Unregister-ScheduledTask -TaskName $nomeTask -Confirm:$false -ErrorAction SilentlyContinue

# Criar novo agendamento
$acao     = New-ScheduledTaskAction -Execute $comando -Argument $argumentos
$gatilho  = New-ScheduledTaskTrigger -Daily -At $HorarioBackup
$config   = New-ScheduledTaskSettingsSet `
                -StartWhenAvailable `
                -RunOnlyIfNetworkAvailable:$false `
                -ExecutionTimeLimit (New-TimeSpan -Hours 2)
$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest

Register-ScheduledTask `
    -TaskName   $nomeTask `
    -Description $descricao `
    -Action     $acao `
    -Trigger    $gatilho `
    -Settings   $config `
    -Principal  $principal | Out-Null

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Backup agendado com sucesso!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Horario:  $HorarioBackup" -ForegroundColor White
Write-Host "  Pasta:    $PastaBackup" -ForegroundColor White
Write-Host "  Servidor: $ServidorSQL" -ForegroundColor White
Write-Host ""
Write-Host "Para verificar: Get-ScheduledTask -TaskName '$nomeTask'" -ForegroundColor Yellow
Write-Host "Para executar agora: Start-ScheduledTask -TaskName '$nomeTask'" -ForegroundColor Yellow
Write-Host ""
