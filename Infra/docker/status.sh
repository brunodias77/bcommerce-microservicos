#!/bin/bash

# B-Commerce - Script para verificar status dos containers
# Autor: B-Commerce Team
# Data: $(date +%Y-%m-%d)

set -e

echo "📊 B-Commerce Infrastructure Status"
echo "==================================="

# Verificar se o Docker está rodando
if ! docker info > /dev/null 2>&1; then
    echo "❌ Erro: Docker não está rodando."
    exit 1
fi

# Verificar se o docker-compose.yml existe
if [ ! -f "docker-compose.yml" ]; then
    echo "❌ Erro: docker-compose.yml não encontrado no diretório atual."
    echo "   Execute este script a partir da raiz do projeto."
    exit 1
fi

echo "\n🐳 Status dos Containers:"
echo "========================"
docker-compose ps

echo "\n🔍 Verificação Detalhada dos Serviços:"
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
echo "\n🔐 Keycloak:"
check_container "b-commerce-keycloak" "Keycloak Container"
check_container "b-commerce-keycloak-db" "Keycloak Database"
check_service_health "Keycloak" "curl -s http://localhost:8080/health" "API Health Check"

echo "\n🔴 Redis:"
check_container "b-commerce-redis" "Redis Container"
check_service_health "Redis" "docker exec b-commerce-redis redis-cli -a redis123 ping" "Connection Test"

echo "\n🐰 RabbitMQ:"
check_container "b-commerce-rabbitmq" "RabbitMQ Container"
check_service_health "RabbitMQ" "curl -s -u rabbitmq:rabbitmq123 http://localhost:15672/api/overview" "Management API"

echo "\n🗄️  Bancos de Dados dos Microsserviços:"
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

echo "\n📈 Uso de Recursos:"
echo "=================="
echo "💾 Uso de Memória dos Containers:"
docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}" $(docker-compose ps -q) 2>/dev/null || echo "⚠️  Nenhum container rodando"

echo "\n💽 Volumes Docker:"
docker volume ls | grep b-commerce || echo "⚠️  Nenhum volume do B-Commerce encontrado"

echo "\n🌐 Redes Docker:"
docker network ls | grep b-commerce || echo "⚠️  Nenhuma rede do B-Commerce encontrada"

echo "\n🔗 Portas Expostas:"
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

echo "\n📋 Comandos Úteis:"
echo "================="
echo "🚀 Iniciar serviços: ./scripts/start.sh"
echo "🛑 Parar serviços: ./scripts/stop.sh"
echo "🧹 Limpar tudo: ./scripts/cleanup.sh"
echo "🔧 Logs de um serviço: docker-compose logs [nome-do-serviço]"
echo "🔍 Logs em tempo real: docker-compose logs -f [nome-do-serviço]"