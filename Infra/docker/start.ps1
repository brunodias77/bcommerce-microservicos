# B-Commerce - Script para iniciar todos os containers (Windows PowerShell)
# Autor: B-Commerce Team
# Data: $(Get-Date -Format "yyyy-MM-dd")

$ErrorActionPreference = "Stop"

Write-Host "Iniciando B-Commerce Infrastructure..." -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green

# Verificar se o Docker esta rodando
try {
    docker info | Out-Null
    Write-Host "Docker esta rodando" -ForegroundColor Green
} catch {
    Write-Host "Erro: Docker nao esta rodando. Por favor, inicie o Docker primeiro." -ForegroundColor Red
    exit 1
}

# Verificar se o docker-compose.yml existe
if (-not (Test-Path "docker-compose.yml")) {
    Write-Host "Erro: docker-compose.yml nao encontrado no diretorio atual." -ForegroundColor Red
    Write-Host "   Execute este script a partir da raiz do projeto." -ForegroundColor Yellow
    exit 1
}

Write-Host "Verificando containers existentes..." -ForegroundColor Cyan
docker-compose ps

Write-Host "`nConstruindo e iniciando containers..." -ForegroundColor Cyan
docker-compose up -d

Write-Host "`nAguardando containers ficarem prontos..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

Write-Host "`nStatus dos containers:" -ForegroundColor Cyan
docker-compose ps

Write-Host "`nVerificando saude dos servicos..." -ForegroundColor Cyan

# Verificar Keycloak
Write-Host "`nVerificando Keycloak..." -ForegroundColor Magenta
for ($i = 1; $i -le 30; $i++) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:8080/health" -TimeoutSec 5 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            Write-Host "Keycloak esta rodando em http://localhost:8080" -ForegroundColor Green
            Write-Host "   Admin: admin / admin123" -ForegroundColor Yellow
            break
        }
    } catch {
        Write-Host "Aguardando Keycloak... ($i/30)" -ForegroundColor Yellow
        Start-Sleep -Seconds 5
    }
    if ($i -eq 30) {
        Write-Host "Keycloak pode nao estar totalmente pronto ainda" -ForegroundColor Yellow
    }
}

# Verificar Redis
Write-Host "`nVerificando Redis..." -ForegroundColor Red
try {
    $redisTest = docker exec b-commerce-redis redis-cli -a redis123 ping 2>$null
    if ($redisTest -eq "PONG") {
        Write-Host "Redis esta rodando em localhost:6379" -ForegroundColor Green
    } else {
        Write-Host "Redis pode nao estar pronto ainda" -ForegroundColor Yellow
    }
} catch {
    Write-Host "Redis pode nao estar pronto ainda" -ForegroundColor Yellow
}

# Verificar RabbitMQ
Write-Host "`nVerificando RabbitMQ..." -ForegroundColor Blue
for ($i = 1; $i -le 20; $i++) {
    try {
        $credentials = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("rabbitmq:rabbitmq123"))
        $headers = @{"Authorization" = "Basic $credentials"}
        $response = Invoke-WebRequest -Uri "http://localhost:15672/api/overview" -Headers $headers -TimeoutSec 3 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            Write-Host "RabbitMQ esta rodando em http://localhost:15672" -ForegroundColor Green
            Write-Host "   Admin: rabbitmq / rabbitmq123" -ForegroundColor Yellow
            break
        }
    } catch {
        Write-Host "Aguardando RabbitMQ... ($i/20)" -ForegroundColor Yellow
        Start-Sleep -Seconds 3
    }
    if ($i -eq 20) {
        Write-Host "RabbitMQ pode nao estar totalmente pronto ainda" -ForegroundColor Yellow
    }
}

# Verificar bancos de dados
Write-Host "`nVerificando bancos de dados..." -ForegroundColor DarkYellow
$databases = @(
    "user-management-db:5432",
    "catalog-db:5433",
    "promotion-db:5434",
    "cart-db:5435",
    "order-db:5436",
    "payment-db:5437",
    "review-db:5438",
    "audit-db:5439"
)

foreach ($db in $databases) {
    $dbName = $db.Split(':')[0]
    $containerName = "b-commerce-$dbName"
    try {
        $pgReady = docker exec $containerName pg_isready 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "$containerName esta pronto" -ForegroundColor Green
        } else {
            Write-Host "$containerName pode nao estar pronto ainda" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "$containerName pode nao estar pronto ainda" -ForegroundColor Yellow
    }
}

Write-Host "`nB-Commerce Infrastructure iniciada com sucesso!" -ForegroundColor Green
Write-Host "`nServicos disponiveis:" -ForegroundColor Cyan
Write-Host "   Keycloak: http://localhost:8080 (admin/admin123)" -ForegroundColor White
Write-Host "   RabbitMQ Management: http://localhost:15672 (rabbitmq/rabbitmq123)" -ForegroundColor White
Write-Host "   Redis: localhost:6379 (senha: redis123)" -ForegroundColor White
Write-Host "   Databases: localhost:5432-5439" -ForegroundColor White
Write-Host "`nPara configurar os servicos, execute:" -ForegroundColor Yellow
Write-Host "   .\\infra\\keycloak\\setup-keycloak.ps1" -ForegroundColor White
Write-Host "   .\\infra\\redis\\setup-redis.ps1" -ForegroundColor White
Write-Host "   .\\infra\\rabbitmq\\setup-rabbitmq.ps1" -ForegroundColor White
Write-Host "`nPara verificar o status: .\\infra\\docker\\status.ps1" -ForegroundColor Yellow
Write-Host "Para parar os servicos: .\\infra\\docker\\stop.ps1" -ForegroundColor Yellow

Write-Host "`nScript concluido com sucesso!" -ForegroundColor Green