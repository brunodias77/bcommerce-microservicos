#!/bin/bash

# B-Commerce - Script de configuração do RabbitMQ (versão simplificada)
# Autor: B-Commerce Team
# Data: $(date +%Y-%m-%d)

set -e

echo "🐰 B-Commerce RabbitMQ Setup (Versão Simplificada)"
echo "================================================="

# Configurações
RABBITMQ_CONTAINER="b-commerce-rabbitmq"
RABBITMQ_USER="rabbitmq"
RABBITMQ_PASS="rabbitmq123"
RABBITMQ_VHOST="b-commerce"

# Função para aguardar o RabbitMQ ficar disponível
wait_for_rabbitmq() {
    echo "⏳ Aguardando RabbitMQ ficar disponível..."
    for i in {1..60}; do
        if docker exec "$RABBITMQ_CONTAINER" rabbitmqctl status > /dev/null 2>&1; then
            echo "✅ RabbitMQ está disponível!"
            return 0
        fi
        echo "   Tentativa $i/60..."
        sleep 3
    done
    echo "❌ Timeout: RabbitMQ não ficou disponível em 3 minutos"
    exit 1
}

# Função para executar comando RabbitMQ
execute_rabbitmq_command() {
    local command="$1"
    local description="$2"
    
    echo "🔧 $description"
    if docker exec "$RABBITMQ_CONTAINER" $command > /dev/null 2>&1; then
        echo "✅ Comando executado com sucesso: $command"
    else
        echo "❌ Erro ao executar comando: $command"
        return 1
    fi
}

# Verificar se o container do RabbitMQ está rodando
if ! docker ps | grep -q "$RABBITMQ_CONTAINER"; then
    echo "❌ Container do RabbitMQ não está rodando."
    echo "   Execute primeiro: ./scripts/start.sh"
    exit 1
fi

# Aguardar RabbitMQ ficar disponível
wait_for_rabbitmq

# Configurar Virtual Host (já foi feito pelo script anterior)
echo "🏠 Verificando Virtual Host..."
if docker exec "$RABBITMQ_CONTAINER" rabbitmqctl list_vhosts | grep -q "$RABBITMQ_VHOST"; then
    echo "✅ Virtual host '$RABBITMQ_VHOST' já existe"
else
    execute_rabbitmq_command "rabbitmqctl add_vhost $RABBITMQ_VHOST" "Criando virtual host '$RABBITMQ_VHOST'"
    execute_rabbitmq_command "rabbitmqctl set_permissions -p $RABBITMQ_VHOST $RABBITMQ_USER '.*' '.*' '.*'" "Configurando permissões para '$RABBITMQ_USER'"
fi

# Verificar configuração atual
echo "\n📊 Verificando configuração atual:"
echo "===================================="

# Informações do cluster
echo "🔍 Status do RabbitMQ:"
docker exec "$RABBITMQ_CONTAINER" rabbitmqctl status | grep -E "Status of node|RabbitMQ version" | sed 's/^/   /'

# Lista de virtual hosts
echo "\n🏠 Virtual Hosts:"
docker exec "$RABBITMQ_CONTAINER" rabbitmqctl list_vhosts | sed 's/^/   /'

# Lista de usuários
echo "\n👤 Usuários:"
docker exec "$RABBITMQ_CONTAINER" rabbitmqctl list_users | sed 's/^/   /'

# Permissões no virtual host
echo "\n🔐 Permissões no virtual host '$RABBITMQ_VHOST':"
docker exec "$RABBITMQ_CONTAINER" rabbitmqctl list_permissions -p "$RABBITMQ_VHOST" | sed 's/^/   /'

# Lista de exchanges no virtual host
echo "\n📡 Exchanges no virtual host '$RABBITMQ_VHOST':"
docker exec "$RABBITMQ_CONTAINER" rabbitmqctl list_exchanges -p "$RABBITMQ_VHOST" name type | sed 's/^/   /'

# Lista de filas no virtual host
echo "\n📥 Queues no virtual host '$RABBITMQ_VHOST':"
docker exec "$RABBITMQ_CONTAINER" rabbitmqctl list_queues -p "$RABBITMQ_VHOST" name messages | sed 's/^/   /'

# Plugins habilitados
echo "\n🔌 Plugins habilitados:"
docker exec "$RABBITMQ_CONTAINER" rabbitmq-plugins list | grep "\[E" | sed 's/^/   /'

echo "\n🎉 Verificação do RabbitMQ concluída!"
echo "\n📋 Resumo da configuração:"
echo "🐰 Container: $RABBITMQ_CONTAINER"
echo "🏠 Virtual Host: $RABBITMQ_VHOST"
echo "👤 Usuário: $RABBITMQ_USER"
echo "🌐 Management UI: http://localhost:15672 (login: $RABBITMQ_USER/$RABBITMQ_PASS)"
echo "\n💡 Para criar exchanges e queues manualmente:"
echo "   Acesse: http://localhost:15672"
echo "   Login: $RABBITMQ_USER / $RABBITMQ_PASS"
echo "   Selecione o virtual host: $RABBITMQ_VHOST"
echo "   Vá para as abas 'Exchanges' e 'Queues' para criar os recursos necessários"