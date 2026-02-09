# Script para fazer deploy no servidor Linux
param(
    [string]$Server = "191.252.221.249",
    [string]$User = "root",
    [string]$Password = "@3Xsojfb7",
    [string]$RemotePath = "/var/www/gestaopedidos",
    [string]$PublishPath = ".\publish\linux-x64",
    [int]$Port = 22
)

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Fazendo deploy no servidor Linux" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# Verificar se o diretório de publicação existe
if (-not (Test-Path $PublishPath)) {
    Write-Host "Erro: Diretório de publicação não encontrado: $PublishPath" -ForegroundColor Red
    Write-Host "Execute primeiro o script publish-linux.ps1" -ForegroundColor Yellow
    exit 1
}

# Verificar se o PSCP (PuTTY SCP) está disponível
$pscpPath = Get-Command pscp -ErrorAction SilentlyContinue
$plinkPath = Get-Command plink -ErrorAction SilentlyContinue

if (-not $pscpPath -or -not $plinkPath) {
    Write-Host "`nPSCP/PLINK não encontrado. Verificando alternativas..." -ForegroundColor Yellow
    
    # Tentar usar scp do OpenSSH (Windows 10+)
    $scpPath = Get-Command scp -ErrorAction SilentlyContinue
    if ($scpPath) {
        Write-Host "SCP encontrado, mas será necessário configurar chave SSH ou usar PuTTY." -ForegroundColor Yellow
        Write-Host "Recomendamos usar PuTTY para facilitar o processo." -ForegroundColor Yellow
    }
    
    Write-Host "`nPor favor, instale PuTTY (pscp e plink):" -ForegroundColor Red
    Write-Host "Download: https://www.chiark.greenend.org.uk/~sgtatham/putty/latest.html" -ForegroundColor Yellow
    Write-Host "Ou adicione o diretório do PuTTY ao PATH do sistema." -ForegroundColor Yellow
    exit 1
}

Write-Host "Usando PSCP/PLINK (PuTTY)..." -ForegroundColor Green

try {
    # Criar diretório no servidor remoto
    Write-Host "`nCriando diretório no servidor..." -ForegroundColor Yellow
    $plinkArgs = @(
        "-ssh",
        "-P", $Port.ToString(),
        "-pw", $Password,
        "-batch",
        "${User}@${Server}",
        "mkdir -p $RemotePath"
    )
    & plink $plinkArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Aviso: Pode ter havido problema ao criar diretório (pode já existir)" -ForegroundColor Yellow
    }
    
    # Fazer upload dos arquivos
    Write-Host "`nFazendo upload dos arquivos..." -ForegroundColor Yellow
    Write-Host "Isso pode levar alguns minutos..." -ForegroundColor Yellow
    
    # Usar PSCP para fazer upload recursivo
    $pscpArgs = @(
        "-r",
        "-P", $Port.ToString(),
        "-pw", $Password,
        "-batch",
        "$PublishPath\*",
        "${User}@${Server}:${RemotePath}/"
    )
    
    & pscp $pscpArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Erro ao fazer upload dos arquivos!" -ForegroundColor Red
        Write-Host "Verifique:" -ForegroundColor Yellow
        Write-Host "  - Conexão com o servidor" -ForegroundColor Yellow
        Write-Host "  - Credenciais corretas" -ForegroundColor Yellow
        Write-Host "  - Permissões no servidor" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "`n=========================================" -ForegroundColor Green
    Write-Host "Deploy concluído com sucesso!" -ForegroundColor Green
    Write-Host "Arquivos publicados em: ${Server}:${RemotePath}" -ForegroundColor Green
    Write-Host "=========================================" -ForegroundColor Green
    Write-Host "`nPróximos passos:" -ForegroundColor Yellow
    Write-Host "1. Configure o serviço systemd (veja gestaopedidos.service)" -ForegroundColor Yellow
    Write-Host "2. Configure o Nginx ou outro servidor web como proxy reverso" -ForegroundColor Yellow
    Write-Host "3. Inicie o serviço: systemctl start gestaopedidos" -ForegroundColor Yellow
    
} finally {
    # Limpar arquivos temporários
    if ($passwordFile -and (Test-Path $passwordFile)) {
        Remove-Item $passwordFile -Force
    }
}

