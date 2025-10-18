#!/bin/bash

# B-Commerce - Script para verificar status dos containers (WSL Version)
# Autor: B-Commerce Team
# Data: $(date +%Y-%m-%d)
# Compatível com Windows WSL + Docker Desktop

set -e

echo "📊 B-Commerce Infrastructure Status (WSL)"
echo "=========================================="

# Verificar se estamos no WSL
check_wsl_environment() {
    if [[ ! -f /proc/version ]] || ! grep -qi "microsoft\|wsl" /proc/version 2>/dev/null; then
        echo "⚠️  Aviso: Este script foi otimizado para WSL (Windows Subsystem for Linux)"
        echo "   Continuando execução..."
    else
        echo "✅ Ambiente WSL detectado"
    fi
}

# Verificar se o Docker está rodando (WSL específico)
check_docker_wsl() {
    echo "🐳 Verificando Docker no WSL..."
    
    if ! docker info > /dev/null 2>&1; then
        echo "❌ Erro: Docker não está acessível."
        echo "   Certifique-se de que:"
        echo "   1. Docker Desktop está rodando no Windows"
        echo "   2. A integração WSL está habilitada no Docker Desktop"
        exit 1
    fi
    
    echo "✅ Docker está acessível no WSL"
}

# Função para usar docker-compose ou docker compose
docker_compose_cmd() {
    if command -v docker-compose &> /dev/null; then
        docker-compose "$@"
    else
        docker compose "$@"
    fi
}

# Verificar ambiente WSL
check_wsl_environment

# Verificar Docker
check_docker_wsl

# Verificar se o docker-compose.yml existe
if [ ! -f "docker-compose.yml" ]; then
    echo "❌ Erro: docker-compose.yml não encontrado no diretório atual."
    echo "   Execute este script a partir da raiz do projeto."
    echo "   Diretório atual: $(pwd)"
    exit 1
fi

echo ""
echo "🐳 Status dos Containers:"
echo "========================"
docker_compose_cmd ps

echo ""
echo "🔍 Verificação Detalhada dos Serviços:"
echo "====================================="

# Função para verificar se um container está rodando
check_container() {
    local container_name=$1
    local service_name=$2
    
    if docker ps --format "table {{.Names}}" | grep -q "^$container_name$"; then
        echo "✅ $service_name: Rodando"
        return 0
    else
        echo "❌ $service_name: Parado ou com problemas"
        return 1
    fi
}

# Função para verificar conectividade de serviço
check_service_health() {
    local service_name=$1
    local check_command=$2
    local description=$3
    
    if eval "$check_command" > /dev/null 2>&1; then
        echo "✅ $service_name: $description - OK"
    else
        echo "⚠️  $service_name: $description - Não responsivo"
    fi
}

# Verificar containers principais
echo ""
echo "🔐 Keycloak:"
check_container "b-commerce-keycloak" "Keycloak Container"
check_container "b-commerce-keycloak-db" "Keycloak Database"
check_service_health "Keycloak" "curl -s http://localhost:8080/health" "API Health Check"

echo ""
echo "🔴 Redis:"
check_container "b-commerce-redis" "Redis Container"
check_service_health "Redis" "docker exec b-commerce-redis redis-cli -a redis123 ping" "Connection Test"

echo ""
echo "🐰 RabbitMQ:"
check_container "b-commerce-rabbitmq" "RabbitMQ Container"
check_service_health "RabbitMQ" "curl -s -u rabbitmq:rabbitmq123 http://localhost:15672/api/overview" "Management API"

echo ""
echo "🗄️  Bancos de Dados dos Microsserviços:"
echo "======================================"

# Array com informações dos bancos
declare -A databases=(
    ["b-commerce-user-management-db"]="User Management DB (5432)"
    ["b-commerce-catalog-db"]="Catalog DB (5433)"
    ["b-commerce-promotion-db"]="Promotion DB (5434)"
    ["b-commerce-cart-db"]="Cart DB (5435)"
    ["b-commerce-order-db"]="Order DB (5436)"
    ["b-commerce-payment-db"]="Payment DB (5437)"
    ["b-commerce-review-db"]="Review DB (5438)"
    ["b-commerce-audit-db"]="Audit DB (5439)"
)

for container in "${!databases[@]}"; do
    check_container "$container" "${databases[$container]}"
    if docker ps --format "table {{.Names}}" | grep -q "^$container$"; then
        check_service_health "${databases[$container]}" "docker exec $container pg_isready" "PostgreSQL Ready"
    fi
done

echo ""
echo "📈 Uso de Recursos (WSL):"
echo "========================="
echo "💾 Uso de Memória dos Containers:"

# Verificar se há containers rodando antes de tentar obter stats
running_containers=$(docker_compose_cmd ps -q 2>/dev/null || true)
if [ -n "$running_containers" ]; then
    docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}" $running_containers 2>/dev/null || echo "⚠️  Não foi possível obter estatísticas de recursos"
else
    echo "⚠️  Nenhum container rodando"
fi

echo ""
echo "💽 Volumes Docker:"
docker volume ls | grep -E "(b-commerce|trae-solo)" || echo "⚠️  Nenhum volume do B-Commerce encontrado"

echo ""
echo "🌐 Redes Docker:"
docker network ls | grep -E "(b-commerce|trae-solo)" || echo "⚠️  Nenhuma rede do B-Commerce encontrada"

echo ""
echo "🔗 Portas Expostas:"
echo "=================="
echo "🔐 Keycloak: http://localhost:8080"
echo "🐰 RabbitMQ Management: http://localhost:15672"
echo "🔴 Redis: localhost:6379"
echo "🗄️  User Management DB: localhost:5432"
echo "🗄️  Catalog DB: localhost:5433"
echo "🗄️  Promotion DB: localhost:5434"
echo "🗄️  Cart DB: localhost:5435"
echo "🗄️  Order DB: localhost:5436"
echo "🗄️  Payment DB: localhost:5437"
echo "🗄️  Review DB: localhost:5438"
echo "🗄️  Audit DB: localhost:5439"

echo ""
echo "🐧 Informações do WSL:"
echo "====================="
if [[ -f /proc/version ]]; then
    echo "Kernel: $(cat /proc/version | cut -d' ' -f1-3)"
fi
if command -v wsl.exe &> /dev/null; then
    echo "WSL Version: $(wsl.exe --version 2>/dev/null | head -1 || echo 'WSL 1 ou versão não detectada')"
fi

echo ""
echo "📋 Comandos Úteis (WSL):"
echo "======================="
echo "🚀 Iniciar serviços: ./Infra/docker/start-wsl.sh"
echo "🛑 Parar serviços: ./Infra/docker/stop-wsl.sh"
echo "🧹 Limpar tudo: ./Infra/docker/cleanup-wsl.sh"
echo "🔧 Logs de um serviço: docker-compose logs [nome-do-serviço]"
echo "   ou: docker compose logs [nome-do-serviço]"
echo "🔍 Logs em tempo real: docker-compose logs -f [nome-do-serviço]"
echo "   ou: docker compose logs -f [nome-do-serviço]"

echo ""
echo "💡 Dicas para WSL:"
echo "=================="
echo "• Para acessar arquivos do Windows: /mnt/c/Users/..."
echo "• Para abrir Explorer no diretório atual: explorer.exe ."
echo "• Para verificar integração Docker: docker context ls"