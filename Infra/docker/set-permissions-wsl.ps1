# B-Commerce - Script para definir permissões executáveis nos scripts WSL
# Autor: B-Commerce Team
# Data: $(Get-Date -Format "yyyy-MM-dd")
# Para ser executado no PowerShell do Windows antes de usar os scripts no WSL

Write-Host "🔧 Configurando permissões para scripts WSL..." -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green

# Verificar se estamos no diretório correto
if (-not (Test-Path "docker-compose.yml")) {
    Write-Host "❌ Erro: docker-compose.yml não encontrado no diretório atual." -ForegroundColor Red
    Write-Host "   Execute este script a partir da raiz do projeto." -ForegroundColor Yellow
    Write-Host "   Diretório atual: $(Get-Location)" -ForegroundColor Yellow
    exit 1
}

# Lista dos scripts WSL
$wslScripts = @(
    "Infra\docker\start-wsl.sh",
    "Infra\docker\stop-wsl.sh", 
    "Infra\docker\status-wsl.sh",
    "Infra\docker\cleanup-wsl.sh"
)

Write-Host ""
Write-Host "📋 Scripts WSL encontrados:" -ForegroundColor Cyan

foreach ($script in $wslScripts) {
    if (Test-Path $script) {
        Write-Host "✅ $script" -ForegroundColor Green
        
        # Definir atributos do arquivo para garantir compatibilidade com WSL
        $file = Get-Item $script
        $file.Attributes = $file.Attributes -band (-bnot [System.IO.FileAttributes]::ReadOnly)
        
        Write-Host "   → Atributos configurados para WSL" -ForegroundColor Gray
    } else {
        Write-Host "❌ $script - Não encontrado" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "🐧 Instruções para WSL:" -ForegroundColor Yellow
Write-Host "======================" -ForegroundColor Yellow
Write-Host "Para definir as permissões executáveis no WSL, execute os seguintes comandos:" -ForegroundColor White
Write-Host ""
Write-Host "1. Abra o WSL (Ubuntu, Debian, etc.)" -ForegroundColor Cyan
Write-Host "2. Navegue até o diretório do projeto:" -ForegroundColor Cyan
Write-Host "   cd /mnt/c/Users/Bruno\ Dias/Documents/programacao/codigos/dotnet/bcommerce-microservicos" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Execute os comandos de permissão:" -ForegroundColor Cyan
Write-Host "   chmod +x ./Infra/docker/start-wsl.sh" -ForegroundColor Gray
Write-Host "   chmod +x ./Infra/docker/stop-wsl.sh" -ForegroundColor Gray  
Write-Host "   chmod +x ./Infra/docker/status-wsl.sh" -ForegroundColor Gray
Write-Host "   chmod +x ./Infra/docker/cleanup-wsl.sh" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Ou execute todos de uma vez:" -ForegroundColor Cyan
Write-Host "   chmod +x ./Infra/docker/*-wsl.sh" -ForegroundColor Gray
Write-Host ""
Write-Host "✅ Configuração concluída no Windows!" -ForegroundColor Green
Write-Host "   Agora execute os comandos chmod no WSL para finalizar." -ForegroundColor Yellow

Write-Host ""
Write-Host "💡 Comandos úteis após configurar as permissões:" -ForegroundColor Magenta
Write-Host "🚀 Iniciar infraestrutura: ./Infra/docker/start-wsl.sh" -ForegroundColor White
Write-Host "📊 Verificar status: ./Infra/docker/status-wsl.sh" -ForegroundColor White
Write-Host "🛑 Parar serviços: ./Infra/docker/stop-wsl.sh" -ForegroundColor White
Write-Host "🧹 Limpeza completa: ./Infra/docker/cleanup-wsl.sh" -ForegroundColor White