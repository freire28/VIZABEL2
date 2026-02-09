# Script para compilar e publicar o projeto para Linux
param(
    [string]$Configuration = "Release",
    [string]$OutputPath = ".\publish\linux-x64"
)

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Publicando projeto para Linux" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# Limpar publicações anteriores
if (Test-Path $OutputPath) {
    Write-Host "Limpando diretório de publicação anterior..." -ForegroundColor Yellow
    Remove-Item -Path $OutputPath -Recurse -Force
}

# Restaurar dependências
Write-Host "`nRestaurando dependências..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Erro ao restaurar dependências!" -ForegroundColor Red
    exit 1
}

# Publicar para Linux
Write-Host "`nCompilando e publicando para Linux-x64..." -ForegroundColor Yellow
dotnet publish `
    --configuration $Configuration `
    --runtime linux-x64 `
    --self-contained false `
    --output $OutputPath `
    /p:PublishSingleFile=false `
    /p:IncludeNativeLibrariesForSelfExtract=true

if ($LASTEXITCODE -ne 0) {
    Write-Host "Erro ao publicar o projeto!" -ForegroundColor Red
    exit 1
}

Write-Host "`n=========================================" -ForegroundColor Green
Write-Host "Publicação concluída com sucesso!" -ForegroundColor Green
Write-Host "Arquivos publicados em: $OutputPath" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green




















