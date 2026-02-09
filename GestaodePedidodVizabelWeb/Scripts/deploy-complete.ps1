# Script completo: Compilar, publicar e fazer deploy
param(
    [string]$Server = "191.252.221.249",
    [string]$User = "root",
    [string]$Password = "@3Xsojfb7",
    [string]$RemotePath = "/var/www/gestaopedidos",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Deploy Completo - Gestao Pedidos Vizabel" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# Passo 1: Compilar e publicar
Write-Host "`n[1/3] Compilando projeto para Linux..." -ForegroundColor Yellow
& ".\Scripts\publish-linux.ps1" -Configuration $Configuration

if ($LASTEXITCODE -ne 0) {
    Write-Host "Erro na compilação!" -ForegroundColor Red
    exit 1
}

# Passo 2: Fazer deploy
Write-Host "`n[2/3] Fazendo deploy no servidor..." -ForegroundColor Yellow
& ".\Scripts\deploy-to-server.ps1" `
    -Server $Server `
    -User $User `
    -Password $Password `
    -RemotePath $RemotePath

if ($LASTEXITCODE -ne 0) {
    Write-Host "Erro no deploy!" -ForegroundColor Red
    exit 1
}

# Passo 3: Instruções finais
Write-Host "`n[3/3] Instruções para configuração no servidor:" -ForegroundColor Yellow
Write-Host "`nExecute os seguintes comandos no servidor Linux:" -ForegroundColor Cyan
Write-Host "`n1. Instalar .NET 8 Runtime (se ainda não instalado):" -ForegroundColor White
Write-Host "   wget https://dot.net/v1/dotnet-install.sh" -ForegroundColor Gray
Write-Host "   chmod +x dotnet-install.sh" -ForegroundColor Gray
Write-Host "   ./dotnet-install.sh --channel 8.0 --runtime aspnetcore" -ForegroundColor Gray
Write-Host "   export PATH=`$PATH:`$HOME/.dotnet" -ForegroundColor Gray

Write-Host "`n2. Configurar permissões:" -ForegroundColor White
Write-Host "   chown -R www-data:www-data $RemotePath" -ForegroundColor Gray
Write-Host "   chmod +x $RemotePath/GestaoPedidosVizabel.dll" -ForegroundColor Gray

Write-Host "`n3. Instalar serviço systemd:" -ForegroundColor White
Write-Host "   scp Scripts/gestaopedidos.service root@${Server}:/etc/systemd/system/" -ForegroundColor Gray
Write-Host "   ssh root@${Server} 'systemctl daemon-reload'" -ForegroundColor Gray
Write-Host "   ssh root@${Server} 'systemctl enable gestaopedidos'" -ForegroundColor Gray
Write-Host "   ssh root@${Server} 'systemctl start gestaopedidos'" -ForegroundColor Gray
Write-Host "   ssh root@${Server} 'systemctl status gestaopedidos'" -ForegroundColor Gray

Write-Host "`n4. Configurar Nginx (opcional, para proxy reverso):" -ForegroundColor White
Write-Host "   Veja o arquivo nginx-config.conf para exemplo de configuração" -ForegroundColor Gray

Write-Host "`n=========================================" -ForegroundColor Green
Write-Host "Processo de deploy concluído!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green




















