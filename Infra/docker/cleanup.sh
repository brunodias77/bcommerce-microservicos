#!/bin/bash

# B-Commerce - Script para limpar completamente a infraestrutura
# Autor: B-Commerce Team
# Data: $(date +%Y-%m-%d)
# ATENÇÃO: Este script remove TODOS os dados persistentes!

set -e

echo "🧹 B-Commerce Infrastructure Cleanup"
echo "===================================="
echo "⚠️  ATENÇÃO: Este script irá remover:"
echo "   - Todos os containers"
echo "   - Todos os volumes (dados serão perdidos!)"
echo "   - Todas as redes"
echo "   - As imagens serão mantidas"
echo ""

# Função para confirmar ação
confirm_cleanup() {
    echo "❓ Tem certeza que deseja continuar? (digite 'CONFIRMAR' para prosseguir)"
    read -r confirmation
    
    if [ "$confirmation" != "CONFIRMAR" ]; then
        echo "❌ Operação cancelada pelo usuário."
        exit 0
    fi
}

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

# Solicitar confirmação
confirm_cleanup

echo "\n🛑 Parando todos os containers..."
docker-compose down

echo "\n🗑️  Removendo containers, volumes e redes..."
docker-compose down --volumes --remove-orphans

echo "\n🔍 Verificando recursos restantes..."

# Remover containers órfãos do B-Commerce
echo "\n🧹 Limpando containers órfãos do B-Commerce..."
orphan_containers=$(docker ps -a --filter "name=b-commerce" --format "{{.Names}}" 2>/dev/null || true)
if [ -n "$orphan_containers" ]; then
    echo "Removendo containers órfãos: $orphan_containers"
    echo "$orphan_containers" | xargs docker rm -f 2>/dev/null || true
else
    echo "✅ Nenhum container órfão encontrado"
fi

# Remover volumes órfãos do B-Commerce
echo "\n🗄️  Limpando volumes órfãos do B-Commerce..."
orphan_volumes=$(docker volume ls --filter "name=trae-solo-5" --format "{{.Name}}" 2>/dev/null || true)
if [ -n "$orphan_volumes" ]; then
    echo "Removendo volumes órfãos: $orphan_volumes"
    echo "$orphan_volumes" | xargs docker volume rm 2>/dev/null || true
else
    echo "✅ Nenhum volume órfão encontrado"
fi

# Remover redes órfãs do B-Commerce
echo "\n🌐 Limpando redes órfãs do B-Commerce..."
orphan_networks=$(docker network ls --filter "name=trae-solo-5" --format "{{.Name}}" 2>/dev/null | grep -v "bridge\|host\|none" || true)
if [ -n "$orphan_networks" ]; then
    echo "Removendo redes órfãs: $orphan_networks"
    echo "$orphan_networks" | xargs docker network rm 2>/dev/null || true
else
    echo "✅ Nenhuma rede órfã encontrada"
fi

# Limpeza geral do Docker (opcional)
echo "\n🧽 Executando limpeza geral do Docker..."
docker system prune -f --volumes

echo "\n📊 Status final:"
echo "==============="

echo "\n🐳 Containers restantes do B-Commerce:"
remaining_containers=$(docker ps -a --filter "name=b-commerce" --format "table {{.Names}}\t{{.Status}}" 2>/dev/null || true)
if [ -n "$remaining_containers" ]; then
    echo "$remaining_containers"
else
    echo "✅ Nenhum container do B-Commerce encontrado"
fi

echo "\n💾 Volumes restantes do B-Commerce:"
remaining_volumes=$(docker volume ls --filter "name=trae-solo-5" --format "table {{.Name}}\t{{.Driver}}" 2>/dev/null || true)
if [ -n "$remaining_volumes" ]; then
    echo "$remaining_volumes"
else
    echo "✅ Nenhum volume do B-Commerce encontrado"
fi

echo "\n🌐 Redes restantes do B-Commerce:"
remaining_networks=$(docker network ls --filter "name=trae-solo-5" --format "table {{.Name}}\t{{.Driver}}" 2>/dev/null || true)
if [ -n "$remaining_networks" ]; then
    echo "$remaining_networks"
else
    echo "✅ Nenhuma rede do B-Commerce encontrada"
fi

echo "\n🖼️  Imagens do B-Commerce mantidas:"
docker images --filter "reference=*keycloak*" --filter "reference=*postgres*" --filter "reference=*redis*" --filter "reference=*rabbitmq*" --format "table {{.Repository}}\t{{.Tag}}\t{{.Size}}" 2>/dev/null || echo "✅ Imagens disponíveis para uso futuro"

echo "\n✅ Limpeza completa realizada com sucesso!"
echo "\n💡 Para recriar a infraestrutura:"
echo "🚀 Execute: ./scripts/start.sh"
echo "\n📝 Nota: Todas as configurações e dados foram removidos."
echo "   Será necessário reconfigurar os serviços após a próxima inicialização."