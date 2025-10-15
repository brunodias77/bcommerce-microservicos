#!/bin/bash

# B-Commerce - Script de configuração do RabbitMQ
# Autor: B-Commerce Team
# Data: $(date +%Y-%m-%d)

set -e

echo "🐰 B-Commerce RabbitMQ Setup"
echo "============================"

# Configurações
RABBITMQ_HOST="localhost"
RABBITMQ_PORT="5672"
RABBITMQ_MANAGEMENT_PORT="15672"
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

# Função para criar virtual host
setup_vhost() {
    echo "🏠 Configurando Virtual Host..."
    
    # Criar virtual host
    execute_rabbitmq_command "rabbitmqctl add_vhost $RABBITMQ_VHOST" "Criando virtual host '$RABBITMQ_VHOST'"
    
    # Configurar permissões para o usuário
    execute_rabbitmq_command "rabbitmqctl set_permissions -p $RABBITMQ_VHOST $RABBITMQ_USER '.*' '.*' '.*'" "Configurando permissões para '$RABBITMQ_USER'"
}

# Função para criar exchanges
setup_exchanges() {
    echo "📡 Configurando Exchanges..."
    
    # Exchange principal para eventos de domínio
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare exchange name=b-commerce.events type=topic durable=true" "Criando exchange de eventos"
    
    # Exchange para comandos
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare exchange name=b-commerce.commands type=direct durable=true" "Criando exchange de comandos"
    
    # Exchange para notificações
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare exchange name=b-commerce.notifications type=fanout durable=true" "Criando exchange de notificações"
    
    # Exchange para dead letter (mensagens com erro)
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare exchange name=b-commerce.dlx type=direct durable=true" "Criando exchange de dead letter"
    
    # Exchanges específicos por serviço
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare exchange name=user-management.events type=topic durable=true" "Criando exchange do User Management"
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare exchange name=catalog.events type=topic durable=true" "Criando exchange do Catalog"
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare exchange name=order.events type=topic durable=true" "Criando exchange do Order"
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare exchange name=payment.events type=topic durable=true" "Criando exchange do Payment"
}

# Função para criar filas
setup_queues() {
    echo "📥 Configurando Queues..."
    
    # Filas para User Management
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare queue name=user.registration durable=true arguments='{\"x-dead-letter-exchange\": \"b-commerce.dlx\", \"x-dead-letter-routing-key\": \"user.registration.failed\"}'" "Criando fila de registro de usuários"
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare queue name=user.authentication durable=true arguments='{\"x-dead-letter-exchange\": \"b-commerce.dlx\", \"x-dead-letter-routing-key\": \"user.authentication.failed\"}'" "Criando fila de autenticação"
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare queue name=user.profile.update durable=true arguments='{\"x-dead-letter-exchange\": \"b-commerce.dlx\", \"x-dead-letter-routing-key\": \"user.profile.update.failed\"}'" "Criando fila de atualização de perfil"
    
    # Filas para Catalog
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare queue name=catalog.product.created durable=true arguments='{\"x-dead-letter-exchange\": \"b-commerce.dlx\", \"x-dead-letter-routing-key\": \"catalog.product.created.failed\"}'" "Criando fila de produtos criados"
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare queue name=catalog.product.updated durable=true arguments='{\"x-dead-letter-exchange\": \"b-commerce.dlx\", \"x-dead-letter-routing-key\": \"catalog.product.updated.failed\"}'" "Criando fila de produtos atualizados"
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare queue name=catalog.inventory.update durable=true arguments='{\"x-dead-letter-exchange\": \"b-commerce.dlx\", \"x-dead-letter-routing-key\": \"catalog.inventory.update.failed\"}'" "Criando fila de atualização de estoque"
    
    # Filas para Cart
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare queue name=cart.item.added durable=true arguments='{\"x-dead-letter-exchange\": \"b-commerce.dlx\", \"x-dead-letter-routing-key\": \"cart.item.added.failed\"}'" "Criando fila de itens adicionados ao carrinho"
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare queue name=cart.checkout durable=true arguments='{\"x-dead-letter-exchange\": \"b-commerce.dlx\", \"x-dead-letter-routing-key\": \"cart.checkout.failed\"}'" "Criando fila de checkout"
    
    # Filas para Order
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare queue name=order.created durable=true arguments='{\"x-dead-letter-exchange\": \"b-commerce.dlx\", \"x-dead-letter-routing-key\": \"order.created.failed\"}'" "Criando fila de pedidos criados"
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare queue name=order.payment.requested durable=true arguments='{\"x-dead-letter-exchange\": \"b-commerce.dlx\", \"x-dead-letter-routing-key\": \"order.payment.requested.failed\"}'" "Criando fila de solicitação de pagamento"
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare queue name=order.status.update durable=true arguments='{\"x-dead-letter-exchange\": \"b-commerce.dlx\", \"x-dead-letter-routing-key\": \"order.status.update.failed\"}'" "Criando fila de atualização de status"
    
    # Filas para Payment
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare queue name=payment.processed durable=true arguments='{\"x-dead-letter-exchange\": \"b-commerce.dlx\", \"x-dead-letter-routing-key\": \"payment.processed.failed\"}'" "Criando fila de pagamentos processados"
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare queue name=payment.failed durable=true arguments='{\"x-dead-letter-exchange\": \"b-commerce.dlx\", \"x-dead-letter-routing-key\": \"payment.failed.failed\"}'" "Criando fila de pagamentos falhados"
    
    # Filas para Promotion
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare queue name=promotion.applied durable=true arguments='{\"x-dead-letter-exchange\": \"b-commerce.dlx\", \"x-dead-letter-routing-key\": \"promotion.applied.failed\"}'" "Criando fila de promoções aplicadas"
    
    # Filas para Review
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare queue name=review.created durable=true arguments='{\"x-dead-letter-exchange\": \"b-commerce.dlx\", \"x-dead-letter-routing-key\": \"review.created.failed\"}'" "Criando fila de avaliações criadas"
    
    # Filas para Audit
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare queue name=audit.log durable=true arguments='{\"x-dead-letter-exchange\": \"b-commerce.dlx\", \"x-dead-letter-routing-key\": \"audit.log.failed\"}'" "Criando fila de logs de auditoria"
    
    # Filas para notificações
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare queue name=notification.email durable=true arguments='{\"x-dead-letter-exchange\": \"b-commerce.dlx\", \"x-dead-letter-routing-key\": \"notification.email.failed\"}'" "Criando fila de emails"
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare queue name=notification.sms durable=true arguments='{\"x-dead-letter-exchange\": \"b-commerce.dlx\", \"x-dead-letter-routing-key\": \"notification.sms.failed\"}'" "Criando fila de SMS"
    
    # Filas de dead letter
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare queue name=dead.letter.queue durable=true" "Criando fila de dead letter"
}

# Função para criar bindings
setup_bindings() {
    echo "🔗 Configurando Bindings..."
    
    # Bindings para eventos de usuário
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare binding source=user-management.events destination=user.registration routing_key=user.registered" "Binding: user.registered"
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare binding source=user-management.events destination=user.authentication routing_key=user.login" "Binding: user.login"
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare binding source=user-management.events destination=user.profile.update routing_key=user.profile.updated" "Binding: user.profile.updated"
    
    # Bindings para eventos de catálogo
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare binding source=catalog.events destination=catalog.product.created routing_key=product.created" "Binding: product.created"
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare binding source=catalog.events destination=catalog.product.updated routing_key=product.updated" "Binding: product.updated"
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare binding source=catalog.events destination=catalog.inventory.update routing_key=inventory.updated" "Binding: inventory.updated"
    
    # Bindings para eventos de pedidos
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare binding source=order.events destination=order.created routing_key=order.created" "Binding: order.created"
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare binding source=order.events destination=order.payment.requested routing_key=order.payment.requested" "Binding: order.payment.requested"
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare binding source=order.events destination=order.status.update routing_key=order.status.*" "Binding: order.status.*"
    
    # Bindings para eventos de pagamento
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare binding source=payment.events destination=payment.processed routing_key=payment.success" "Binding: payment.success"
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare binding source=payment.events destination=payment.failed routing_key=payment.failed" "Binding: payment.failed"
    
    # Bindings para notificações (fanout - todas as filas recebem)
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare binding source=b-commerce.notifications destination=notification.email" "Binding: notifications -> email"
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare binding source=b-commerce.notifications destination=notification.sms" "Binding: notifications -> sms"
    
    # Bindings para dead letter
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST declare binding source=b-commerce.dlx destination=dead.letter.queue routing_key='#'" "Binding: dead letter queue"
}

# Função para configurar políticas
setup_policies() {
    echo "📋 Configurando Políticas..."
    
    # Política de TTL para mensagens (24 horas)
    execute_rabbitmq_command "rabbitmqctl set_policy -p $RABBITMQ_VHOST TTL \".*\" '{\"message-ttl\": 86400000}' --apply-to queues" "Configurando TTL de mensagens (24h)"
    
    # Política de alta disponibilidade
    execute_rabbitmq_command "rabbitmqctl set_policy -p $RABBITMQ_VHOST HA \".*\" '{\"ha-mode\": \"all\"}' --apply-to queues" "Configurando alta disponibilidade"
    
    # Política de limite de tamanho de fila
    execute_rabbitmq_command "rabbitmqctl set_policy -p $RABBITMQ_VHOST MaxLength \".*\" '{\"max-length\": 10000}' --apply-to queues" "Configurando limite máximo de mensagens (10k)"
}

# Função para publicar mensagens de teste
publish_test_messages() {
    echo "📤 Publicando mensagens de teste..."
    
    # Mensagem de teste para registro de usuário
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST publish exchange=user-management.events routing_key=user.registered payload='{\"userId\": 123, \"email\": \"bruno@teste.com\", \"timestamp\": \"$(date -Iseconds)\"}' properties='{\"content_type\": \"application/json\", \"delivery_mode\": 2}'" "Publicando evento de registro de usuário"
    
    # Mensagem de teste para criação de produto
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST publish exchange=catalog.events routing_key=product.created payload='{\"productId\": 456, \"name\": \"Produto Teste\", \"price\": 99.99, \"timestamp\": \"$(date -Iseconds)\"}' properties='{\"content_type\": \"application/json\", \"delivery_mode\": 2}'" "Publicando evento de criação de produto"
    
    # Mensagem de teste para pedido criado
    execute_rabbitmq_command "rabbitmqadmin -H $RABBITMQ_HOST -P $RABBITMQ_MANAGEMENT_PORT -u $RABBITMQ_USER -p $RABBITMQ_PASS -V $RABBITMQ_VHOST publish exchange=order.events routing_key=order.created payload='{\"orderId\": 789, \"userId\": 123, \"total\": 199.98, \"timestamp\": \"$(date -Iseconds)\"}' properties='{\"content_type\": \"application/json\", \"delivery_mode\": 2}'" "Publicando evento de pedido criado"
}

# Função para exibir estatísticas
show_rabbitmq_stats() {
    echo "\n📊 Estatísticas do RabbitMQ:"
    echo "============================="
    
    # Informações do cluster
    echo "🔍 Informações do cluster:"
    docker exec "$RABBITMQ_CONTAINER" rabbitmqctl cluster_status | grep -E "Cluster name|Running nodes" | sed 's/^/   /'
    
    # Lista de virtual hosts
    echo "\n🏠 Virtual Hosts:"
    docker exec "$RABBITMQ_CONTAINER" rabbitmqctl list_vhosts | sed 's/^/   /'
    
    # Lista de exchanges
    echo "\n📡 Exchanges (primeiros 10):"
    docker exec "$RABBITMQ_CONTAINER" rabbitmqctl list_exchanges -p "$RABBITMQ_VHOST" name type | head -10 | sed 's/^/   /'
    
    # Lista de filas
    echo "\n📥 Queues (primeiras 10):"
    docker exec "$RABBITMQ_CONTAINER" rabbitmqctl list_queues -p "$RABBITMQ_VHOST" name messages | head -10 | sed 's/^/   /'
    
    # Conexões ativas
    echo "\n🔌 Conexões ativas:"
    docker exec "$RABBITMQ_CONTAINER" rabbitmqctl list_connections | wc -l | sed 's/^/   Total: /'
    
    # Canais ativos
    echo "\n📺 Canais ativos:"
    docker exec "$RABBITMQ_CONTAINER" rabbitmqctl list_channels | wc -l | sed 's/^/   Total: /'
}

# Verificar se o container do RabbitMQ está rodando
if ! docker ps | grep -q "$RABBITMQ_CONTAINER"; then
    echo "❌ Container do RabbitMQ não está rodando."
    echo "   Execute primeiro: ./scripts/start.sh"
    exit 1
fi

# Aguardar RabbitMQ ficar disponível
wait_for_rabbitmq

# Executar configurações
setup_vhost
setup_exchanges
setup_queues
setup_bindings
setup_policies
publish_test_messages

# Exibir estatísticas
show_rabbitmq_stats

echo "\n🎉 Configuração do RabbitMQ concluída com sucesso!"
echo "\n📋 Resumo da configuração:"
echo "🐰 RabbitMQ Host: $RABBITMQ_HOST:$RABBITMQ_PORT"
echo "🌐 Management UI: http://$RABBITMQ_HOST:$RABBITMQ_MANAGEMENT_PORT"
echo "🐳 Container: $RABBITMQ_CONTAINER"
echo "🏠 Virtual Host: $RABBITMQ_VHOST"
echo "👤 Usuário: $RABBITMQ_USER"
echo "\n🛠️  Estruturas configuradas:"
echo "   • 7 Exchanges (eventos, comandos, notificações, DLX, por serviço)"
echo "   • 15+ Queues (por funcionalidade de cada serviço)"
echo "   • Bindings com routing keys específicos"
echo "   • Políticas de TTL, HA e limites"
echo "   • Mensagens de teste publicadas"
echo "\n💡 Comandos úteis:"
echo "   Management UI: http://$RABBITMQ_HOST:$RABBITMQ_MANAGEMENT_PORT (login: $RABBITMQ_USER/$RABBITMQ_PASS)"
echo "   Status: docker exec -it $RABBITMQ_CONTAINER rabbitmqctl status"
echo "   Queues: docker exec -it $RABBITMQ_CONTAINER rabbitmqctl list_queues -p $RABBITMQ_VHOST"
echo "   Exchanges: docker exec -it $RABBITMQ_CONTAINER rabbitmqctl list_exchanges -p $RABBITMQ_VHOST"
echo "   Logs: docker logs $RABBITMQ_CONTAINER"