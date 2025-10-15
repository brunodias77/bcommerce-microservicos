#!/bin/bash

# B-Commerce - Script de configuração do Redis
# Autor: B-Commerce Team
# Data: $(date +%Y-%m-%d)

set -e

echo "🔴 B-Commerce Redis Setup"
echo "========================"

# Configurações
REDIS_HOST="localhost"
REDIS_PORT="6379"
REDIS_CONTAINER="b-commerce-redis"
REDIS_PASSWORD="redis123"

# Função para verificar se o Redis está disponível
wait_for_redis() {
    echo "⏳ Aguardando Redis ficar disponível..."
    for i in {1..30}; do
        if docker exec "$REDIS_CONTAINER" redis-cli -a "$REDIS_PASSWORD" ping > /dev/null 2>&1; then
            echo "✅ Redis está disponível!"
            return 0
        fi
        echo "   Tentativa $i/30..."
        sleep 2
    done
    echo "❌ Timeout: Redis não ficou disponível em 1 minuto"
    exit 1
}

# Função para executar comando Redis
execute_redis_command() {
    local command="$1"
    local description="$2"
    
    echo "🔧 $description"
    if docker exec "$REDIS_CONTAINER" redis-cli -a "$REDIS_PASSWORD" $command > /dev/null 2>&1; then
        echo "✅ Comando executado com sucesso: $command"
    else
        echo "❌ Erro ao executar comando: $command"
        return 1
    fi
}

# Função para configurar estruturas de dados do B-Commerce
setup_redis_structures() {
    echo "🏗️  Configurando estruturas de dados do B-Commerce..."
    
    # Configurar TTL padrão para sessões (1 hora)
    execute_redis_command "CONFIG SET maxmemory-policy allkeys-lru" "Configurando política de memória"
    
    # Criar estruturas para diferentes serviços
    echo "📦 Configurando estruturas para serviços..."
    
    # User Management - Sessões e cache de usuários
    execute_redis_command "HSET user:config session_timeout 3600" "Configurando timeout de sessão"
    execute_redis_command "HSET user:config max_login_attempts 5" "Configurando tentativas máximas de login"
    execute_redis_command "HSET user:config lockout_duration 900" "Configurando duração do bloqueio"
    
    # Catalog - Cache de produtos e categorias
    execute_redis_command "HSET catalog:config cache_ttl 1800" "Configurando TTL do cache de catálogo"
    execute_redis_command "HSET catalog:config max_search_results 100" "Configurando máximo de resultados de busca"
    
    # Cart - Configurações do carrinho
    execute_redis_command "HSET cart:config expiry_time 86400" "Configurando expiração do carrinho (24h)"
    execute_redis_command "HSET cart:config max_items 50" "Configurando máximo de itens no carrinho"
    
    # Promotion - Cache de promoções
    execute_redis_command "HSET promotion:config cache_ttl 3600" "Configurando TTL do cache de promoções"
    execute_redis_command "HSET promotion:config max_discount_percent 90" "Configurando desconto máximo"
    
    # Order - Cache de pedidos
    execute_redis_command "HSET order:config processing_timeout 1800" "Configurando timeout de processamento"
    execute_redis_command "HSET order:config max_retry_attempts 3" "Configurando tentativas máximas de retry"
    
    # Payment - Configurações de pagamento
    execute_redis_command "HSET payment:config transaction_timeout 300" "Configurando timeout de transação"
    execute_redis_command "HSET payment:config max_amount 10000" "Configurando valor máximo de transação"
    
    # Review - Cache de avaliações
    execute_redis_command "HSET review:config cache_ttl 7200" "Configurando TTL do cache de avaliações"
    execute_redis_command "HSET review:config max_reviews_per_user 10" "Configurando máximo de avaliações por usuário"
    
    # Audit - Configurações de auditoria
    execute_redis_command "HSET audit:config log_retention_days 30" "Configurando retenção de logs"
    execute_redis_command "HSET audit:config max_log_size 1000000" "Configurando tamanho máximo de log"
}

# Função para criar índices e estruturas otimizadas
setup_redis_indexes() {
    echo "📊 Configurando índices e estruturas otimizadas..."
    
    # Criar sets para categorização rápida
    execute_redis_command "SADD categories:electronics 'smartphones' 'laptops' 'tablets'" "Criando categoria eletrônicos"
    execute_redis_command "SADD categories:clothing 'shirts' 'pants' 'shoes'" "Criando categoria roupas"
    execute_redis_command "SADD categories:books 'fiction' 'non-fiction' 'technical'" "Criando categoria livros"
    
    # Criar sorted sets para rankings
    execute_redis_command "ZADD products:popular 100 'product:1' 95 'product:2' 90 'product:3'" "Criando ranking de produtos populares"
    execute_redis_command "ZADD users:active 1000 'user:1' 950 'user:2' 900 'user:3'" "Criando ranking de usuários ativos"
    
    # Configurar listas para filas de processamento
    execute_redis_command "LPUSH queue:email 'welcome_email_template'" "Configurando fila de emails"
    execute_redis_command "LPUSH queue:notifications 'order_confirmation_template'" "Configurando fila de notificações"
    execute_redis_command "LPUSH queue:analytics 'user_behavior_tracking'" "Configurando fila de analytics"
}

# Função para configurar monitoramento
setup_redis_monitoring() {
    echo "📈 Configurando monitoramento..."
    
    # Configurar slow log
    execute_redis_command "CONFIG SET slowlog-log-slower-than 10000" "Configurando slow log (10ms)"
    execute_redis_command "CONFIG SET slowlog-max-len 128" "Configurando tamanho máximo do slow log"
    
    # Configurar notificações de eventos
    execute_redis_command "CONFIG SET notify-keyspace-events Ex" "Configurando notificações de expiração"
    
    # Configurar limites de memória
    execute_redis_command "CONFIG SET maxmemory 256mb" "Configurando limite de memória"
}

# Função para criar dados de exemplo
setup_sample_data() {
    echo "🎯 Criando dados de exemplo..."
    
    # Sessões de exemplo
    execute_redis_command "SETEX session:user123 3600 '{\"userId\": 123, \"email\": \"bruno@teste.com\", \"role\": \"user\"}'" "Criando sessão de exemplo"
    execute_redis_command "SETEX session:admin456 3600 '{\"userId\": 456, \"email\": \"bruno@admin.com\", \"role\": \"admin\"}'" "Criando sessão de admin"
    
    # Cache de produtos de exemplo
    execute_redis_command "SETEX product:1 1800 '{\"id\": 1, \"name\": \"Smartphone XYZ\", \"price\": 999.99, \"category\": \"electronics\"}'" "Criando produto de exemplo"
    execute_redis_command "SETEX product:2 1800 '{\"id\": 2, \"name\": \"Laptop ABC\", \"price\": 1499.99, \"category\": \"electronics\"}'" "Criando laptop de exemplo"
    
    # Carrinho de exemplo
    execute_redis_command "SETEX cart:user123 86400 '{\"items\": [{\"productId\": 1, \"quantity\": 2}, {\"productId\": 2, \"quantity\": 1}], \"total\": 3499.97}'" "Criando carrinho de exemplo"
    
    # Contadores de exemplo
    execute_redis_command "SET counter:total_orders 1250" "Configurando contador de pedidos"
    execute_redis_command "SET counter:total_users 5000" "Configurando contador de usuários"
    execute_redis_command "SET counter:total_products 850" "Configurando contador de produtos"
}

# Função para exibir estatísticas
show_redis_stats() {
    echo "\n📊 Estatísticas do Redis:"
    echo "========================"
    
    # Informações básicas
    echo "🔍 Informações do servidor:"
    docker exec "$REDIS_CONTAINER" redis-cli -a "$REDIS_PASSWORD" INFO server | grep -E "redis_version|os|arch|process_id" | sed 's/^/   /'
    
    # Estatísticas de memória
    echo "\n💾 Uso de memória:"
    docker exec "$REDIS_CONTAINER" redis-cli -a "$REDIS_PASSWORD" INFO memory | grep -E "used_memory_human|used_memory_peak_human|maxmemory_human" | sed 's/^/   /'
    
    # Estatísticas de clientes
    echo "\n👥 Clientes conectados:"
    docker exec "$REDIS_CONTAINER" redis-cli -a "$REDIS_PASSWORD" INFO clients | grep -E "connected_clients|blocked_clients" | sed 's/^/   /'
    
    # Estatísticas de comandos
    echo "\n⚡ Estatísticas de comandos:"
    docker exec "$REDIS_CONTAINER" redis-cli -a "$REDIS_PASSWORD" INFO stats | grep -E "total_commands_processed|instantaneous_ops_per_sec" | sed 's/^/   /'
    
    # Número de chaves
    echo "\n🔑 Número de chaves por database:"
    docker exec "$REDIS_CONTAINER" redis-cli -a "$REDIS_PASSWORD" INFO keyspace | sed 's/^/   /'
}

# Verificar se o container do Redis está rodando
if ! docker ps | grep -q "$REDIS_CONTAINER"; then
    echo "❌ Container do Redis não está rodando."
    echo "   Execute primeiro: ./scripts/start.sh"
    exit 1
fi

# Aguardar Redis ficar disponível
wait_for_redis

# Executar configurações
setup_redis_structures
setup_redis_indexes
setup_redis_monitoring
setup_sample_data

# Exibir estatísticas
show_redis_stats

echo "\n🎉 Configuração do Redis concluída com sucesso!"
echo "\n📋 Resumo da configuração:"
echo "🔴 Redis Host: $REDIS_HOST:$REDIS_PORT"
echo "🐳 Container: $REDIS_CONTAINER"
echo "🔐 Password: $REDIS_PASSWORD"
echo "\n🛠️  Estruturas configuradas:"
echo "   • Configurações por serviço (user, catalog, cart, etc.)"
echo "   • Índices e rankings otimizados"
echo "   • Filas de processamento"
echo "   • Monitoramento e logs"
echo "   • Dados de exemplo"
echo "\n💡 Comandos úteis:"
echo "   Conectar: docker exec -it $REDIS_CONTAINER redis-cli -a $REDIS_PASSWORD"
echo "   Monitor: docker exec -it $REDIS_CONTAINER redis-cli -a $REDIS_PASSWORD monitor"
echo "   Stats: docker exec -it $REDIS_CONTAINER redis-cli -a $REDIS_PASSWORD info"
echo "   Logs: docker logs $REDIS_CONTAINER"