# ============================================================
# Gera chaves seguras para producao do ProjetoVarejo
# Execute UMA VEZ e guarde os valores em local seguro
# ============================================================

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Gerador de chaves - ProjetoVarejo" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# JWT Secret Key: 64 bytes (512 bits) -> base64 = 88 chars
$jwtBytes = New-Object byte[] 64
[System.Security.Cryptography.RandomNumberGenerator]::Fill($jwtBytes)
$jwtKey = [Convert]::ToBase64String($jwtBytes)

# API Key: 32 bytes (256 bits) -> hex = 64 chars
$apiBytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Fill($apiBytes)
$apiKey = [BitConverter]::ToString($apiBytes).Replace("-", "").ToLower()

Write-Host "JWT_SECRET_KEY:" -ForegroundColor Yellow
Write-Host "  $jwtKey" -ForegroundColor White
Write-Host ""
Write-Host "API_KEY:" -ForegroundColor Yellow
Write-Host "  $apiKey" -ForegroundColor White
Write-Host ""

# Salvar em arquivo (opcional)
$salvar = Read-Host "Salvar em 'chaves-producao.txt'? [S/N]"
if ($salvar -eq 'S' -or $salvar -eq 's') {
    $saida = @"
# CHAVES DE PRODUCAO - ProjetoVarejo
# Geradas em: $(Get-Date -Format "dd/MM/yyyy HH:mm:ss")
# GUARDE EM LOCAL SEGURO E NAO VERSIONE ESTE ARQUIVO

JWT_SECRET_KEY=$jwtKey
API_KEY=$apiKey
"@
    $saida | Out-File -FilePath "chaves-producao.txt" -Encoding utf8
    Write-Host "Arquivo salvo: chaves-producao.txt" -ForegroundColor Green
    Write-Host "ATENCAO: Adicione 'chaves-producao.txt' ao .gitignore!" -ForegroundColor Red
}

Write-Host ""
Write-Host "Como usar no appsettings.Production.json:" -ForegroundColor Cyan
Write-Host '  "Jwt": { "SecretKey": "<JWT_SECRET_KEY acima>" }' -ForegroundColor Gray
Write-Host '  "ApiKeys": [ "<API_KEY acima>" ]' -ForegroundColor Gray
Write-Host ""
Write-Host "Ou via variaveis de ambiente (mais seguro):" -ForegroundColor Cyan
Write-Host "  set JWT_SECRET_KEY=<valor>" -ForegroundColor Gray
Write-Host "  set API_KEY=<valor>" -ForegroundColor Gray
Write-Host ""
