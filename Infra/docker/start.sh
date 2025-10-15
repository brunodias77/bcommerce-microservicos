#!/bin/bash

# B-Commerce - Script para iniciar todos os containers
# Autor: B-Commerce Team
# Data: $(date +%Y-%m-%d)

set -e

echo "🚀 Iniciando B-Commerce Infrastructure..."
echo "======================================"

# Verificar se o Docker está rodando
if ! docker info > /dev/null 2>&1; then
    echo "❌ Erro: Docker não está rodando. Por favor, inicie o Docker primeiro."
    exit 1
fi

# Verificar se o docker-compose.yml existe
if [ ! -f "docker-compose.yml" ]; then
    echo "❌ Erro: docker-compose.yml não encontrado no diretório atual."
    echo "   Execute este script a partir da raiz do projeto."
    exit 1
fi

echo "📋 Verificando containers existentes..."
docker-compose ps

echo "\n🔧 Construindo e iniciando containers..."
docker-compose up -d

echo "\n⏳ Aguardando containers ficarem prontos..."
sleep 10

echo "\n📊 Status dos containers:"
docker-compose ps

echo "\n🔍 Verificando saúde dos serviços..."

# Verificar Keycloak
echo "\n🔐 Verificando Keycloak..."
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
echo "\n🔴 Verificando Redis..."
if docker exec b-commerce-redis redis-cli -a redis123 ping > /dev/null 2>&1; then
    echo "✅ Redis está rodando em localhost:6379"
else
    echo "⚠️  Redis pode não estar pronto ainda"
fi

# Verificar RabbitMQ
echo "\n🐰 Verificando RabbitMQ..."
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
echo "\n🗄️  Verificando bancos de dados..."
databases=("user-management-db:5432" "catalog-db:5433" "promotion-db:5434" "cart-db:5435" "order-db:5436" "payment-db:5437" "review-db:5438" "audit-db:5439")

for db in "${databases[@]}"; do
    container_name="b-commerce-${db%:*}"
    if docker exec "$container_name" pg_isready > /dev/null 2>&1; then
        echo "✅ $container_name está pronto"
    else
        echo "⚠️  $container_name pode não estar pronto ainda"
    fi
done

echo "\n🎉 B-Commerce Infrastructure iniciada com sucesso!"
echo "\n📋 Serviços disponíveis:"
echo "   🔐 Keycloak: http://localhost:8080 (admin/admin123)"
echo "   🐰 RabbitMQ Management: http://localhost:15672 (rabbitmq/rabbitmq123)"
echo "   🔴 Redis: localhost:6379 (senha: redis123)"
echo "   🗄️  Databases: localhost:5432-5439"
echo "\n💡 Para configurar os serviços, execute:"
echo "   ./scripts/keycloak/setup-keycloak.sh"
echo "   ./scripts/redis/setup-redis.sh"
echo "   ./scripts/rabbitmq/setup-rabbitmq.sh"
echo "\n📊 Para verificar o status: ./scripts/status.sh"
echo "🛑 Para parar os serviços: ./scripts/stop.sh"