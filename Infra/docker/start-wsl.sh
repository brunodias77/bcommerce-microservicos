#!/bin/bash

# B-Commerce - Script para iniciar todos os containers (WSL Version)
# Autor: B-Commerce Team
# Data: $(date +%Y-%m-%d)
# Compatível com Windows WSL + Docker Desktop

set -e

echo "🚀 Iniciando B-Commerce Infrastructure (WSL)..."
echo "=============================================="

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
    
    # Verificar se o Docker daemon está acessível
    if ! docker info > /dev/null 2>&1; then
        echo "❌ Erro: Docker não está acessível."
        echo "   Certifique-se de que:"
        echo "   1. Docker Desktop está rodando no Windows"
        echo "   2. A integração WSL está habilitada no Docker Desktop"
        echo "   3. Sua distribuição WSL está configurada no Docker Desktop"
        exit 1
    fi
    
    # Verificar se docker-compose está disponível
    if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
        echo "❌ Erro: docker-compose não encontrado."
        echo "   Instale docker-compose ou use 'docker compose' (Docker Desktop inclui compose)"
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

echo "📋 Verificando containers existentes..."
docker_compose_cmd ps

echo ""
echo "🔧 Construindo e iniciando containers..."
docker_compose_cmd up -d

echo ""
echo "⏳ Aguardando containers ficarem prontos..."
sleep 10

echo ""
echo "📊 Status dos containers:"
docker_compose_cmd ps

echo ""
echo "🔍 Verificando saúde dos serviços..."

# Verificar Keycloak
echo ""
echo "🔐 Verificando Keycloak..."
for i in {1..30}; do
    if curl -s http://localhost:8080/health > /dev/null 2>&1; then
        echo "✅ Keycloak está rodando em http://localhost:8080"
        echo "   Admin: admin / admin123"
        break
    else
        echo "⏳ Aguardando Keycloak... ($i/30)"
        sleep 5
    fi
    if [ $i -eq 30 ]; then
        echo "⚠️  Keycloak pode não estar totalmente pronto ainda"
    fi
done

# Verificar Redis
echo ""
echo "🔴 Verificando Redis..."
if docker exec b-commerce-redis redis-cli -a redis123 ping > /dev/null 2>&1; then
    echo "✅ Redis está rodando em localhost:6379"
else
    echo "⚠️  Redis pode não estar pronto ainda"
fi

# Verificar RabbitMQ
echo ""
echo "🐰 Verificando RabbitMQ..."
for i in {1..20}; do
    if curl -s -u rabbitmq:rabbitmq123 http://localhost:15672/api/overview > /dev/null 2>&1; then
        echo "✅ RabbitMQ está rodando em http://localhost:15672"
        echo "   Admin: rabbitmq / rabbitmq123"
        break
    else
        echo "⏳ Aguardando RabbitMQ... ($i/20)"
        sleep 3
    fi
    if [ $i -eq 20 ]; then
        echo "⚠️  RabbitMQ pode não estar totalmente pronto ainda"
    fi
done

# Verificar bancos de dados
echo ""
echo "🗄️  Verificando bancos de dados..."
databases=("user-management-db:5432" "catalog-db:5433" "promotion-db:5434" "cart-db:5435" "order-db:5436" "payment-db:5437" "review-db:5438" "audit-db:5439")

for db in "${databases[@]}"; do
    container_name="b-commerce-${db%:*}"
    if docker exec "$container_name" pg_isready > /dev/null 2>&1; then
        echo "✅ $container_name está pronto"
    else
        echo "⚠️  $container_name pode não estar pronto ainda"
    fi
done

echo ""
echo "🎉 B-Commerce Infrastructure iniciada com sucesso no WSL!"
echo ""
echo "📋 Serviços disponíveis:"
echo "   🔐 Keycloak: http://localhost:8080 (admin/admin123)"
echo "   🐰 RabbitMQ Management: http://localhost:15672 (rabbitmq/rabbitmq123)"
echo "   🔴 Redis: localhost:6379 (senha: redis123)"
echo "   🗄️  Databases: localhost:5432-5439"
echo ""
echo "💡 Para configurar os serviços, execute:"
echo "   ./Infra/keycloak/setup-keycloak.sh"
echo "   ./Infra/redis/setup-redis.sh"
echo "   ./Infra/rabbitmq/setup-rabbitmq.sh"
echo ""
echo "📊 Para verificar o status: ./Infra/docker/status-wsl.sh"
echo "🛑 Para parar os serviços: ./Infra/docker/stop-wsl.sh"
echo ""
echo "🐧 Executando no WSL - Docker Desktop Integration ativa"