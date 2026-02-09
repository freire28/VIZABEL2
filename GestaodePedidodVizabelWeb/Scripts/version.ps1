# Script para versionar o projeto
# Uso: .\scripts\version.ps1 -Version "1.0.1" -Type "patch"

param(
    [Parameter(Mandatory=$false)]
    [string]$Version,
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("major", "minor", "patch")]
    [string]$Type = "patch",
    
    [Parameter(Mandatory=$false)]
    [string]$Message = ""
)

$csprojFile = "GestaoPedidosVizabel.csproj"

# Função para obter versão atual
function Get-CurrentVersion {
    $content = Get-Content $csprojFile -Raw
    if ($content -match '<Version>([\d.]+)</Version>') {
        return $matches[1]
    }
    return "1.0.0"
}

# Função para incrementar versão
function Get-NextVersion {
    param([string]$CurrentVersion, [string]$Type)
    
    $parts = $CurrentVersion.Split('.')
    $major = [int]$parts[0]
    $minor = [int]$parts[1]
    $patch = [int]$parts[2]
    
    switch ($Type) {
        "major" {
            $major++
            $minor = 0
            $patch = 0
        }
        "minor" {
            $minor++
            $patch = 0
        }
        "patch" {
            $patch++
        }
    }
    
    return "$major.$minor.$patch"
}

# Obter versão atual
$currentVersion = Get-CurrentVersion
Write-Host "Versão atual: $currentVersion" -ForegroundColor Green

# Determinar nova versão
if ($Version) {
    $newVersion = $Version
} else {
    $newVersion = Get-NextVersion -CurrentVersion $currentVersion -Type $Type
}

Write-Host "Nova versão: $newVersion" -ForegroundColor Yellow

# Atualizar .csproj
$content = Get-Content $csprojFile -Raw
$content = $content -replace '<Version>[\d.]+</Version>', "<Version>$newVersion</Version>"
$content = $content -replace '<AssemblyVersion>[\d.]+</AssemblyVersion>', "<AssemblyVersion>$newVersion.0</AssemblyVersion>"
$content = $content -replace '<FileVersion>[\d.]+</FileVersion>', "<FileVersion>$newVersion.0</FileVersion>"
Set-Content $csprojFile -Value $content -NoNewline

Write-Host "✓ Arquivo .csproj atualizado" -ForegroundColor Green

# Criar tag Git (se Git estiver disponível)
if (Get-Command git -ErrorAction SilentlyContinue) {
    $tagMessage = if ($Message) { $Message } else { "Versão $newVersion" }
    
    Write-Host "`nDeseja criar uma tag Git? (S/N)" -ForegroundColor Cyan
    $response = Read-Host
    
    if ($response -eq "S" -or $response -eq "s") {
        git tag -a "v$newVersion" -m $tagMessage
        Write-Host "✓ Tag Git criada: v$newVersion" -ForegroundColor Green
        Write-Host "`nPara enviar a tag, execute: git push origin v$newVersion" -ForegroundColor Yellow
    }
} else {
    Write-Host "Git não encontrado. Tag não criada." -ForegroundColor Yellow
}

Write-Host "`n✓ Versionamento concluído!" -ForegroundColor Green
Write-Host "Lembre-se de atualizar o CHANGELOG.md e VERSION.md" -ForegroundColor Yellow




















