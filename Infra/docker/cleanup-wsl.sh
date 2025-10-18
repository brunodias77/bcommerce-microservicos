#!/bin/bash

# B-Commerce - Script para limpar completamente a infraestrutura (WSL Version)
# Autor: B-Commerce Team
# Data: $(date +%Y-%m-%d)
# Compatível com Windows WSL + Docker Desktop
# ATENÇÃO: Este script remove TODOS os dados persistentes!

set -e

echo "🧹 B-Commerce Infrastructure Cleanup (WSL)"
echo "==========================================="
echo "⚠️  ATENÇÃO: Este script irá remover:"
echo "   - Todos os containers"
echo "   - Todos os volumes (dados serão perdidos!)"
echo "   - Todas as redes"
echo "   - As imagens serão mantidas"
echo ""

# Verificar se estamos no WSL
check_wsl_environment() {
    if [[ ! -f /proc/version ]] || ! grep -qi "microsoft\|wsl" /proc/version 2>/dev/null; then
        echo "⚠️  Aviso: Este script foi otimizado para WSL (Windows Subsystem for Linux)"
        echo "   Continuando execução..."
    else
        echo "✅ Ambiente WSL detectado"
    fi
}

# Função para confirmar ação
confirm_cleanup() {
    echo "❓ Tem certeza que deseja continuar? (digite 'CONFIRMAR' para prosseguir)"
    read -r confirmation
    
    if [ "$confirmation" != "CONFIRMAR" ]; then
        echo "❌ Operação cancelada pelo usuário."
        exit 0
    fi
}

# Verificar se o Docker está rodando (WSL específico)
check_docker_wsl() {
    echo "🐳 Verificando Docker no WSL..."
    
    if ! docker info > /dev/null 2>&1; then
        echo "❌ Erro: Docker não está acessível."
        echo "   Certifique-se de que Docker Desktop está rodando no Windows"
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

# Solicitar confirmação
confirm_cleanup

echo ""
echo "🛑 Parando todos os containers..."
docker_compose_cmd down

echo ""
echo "🗑️  Removendo containers, volumes e redes..."
docker_compose_cmd down --volumes --remove-orphans

echo ""
echo "🔍 Verificando recursos restantes..."

# Remover containers órfãos do B-Commerce (WSL específico)
echo ""
echo "🧹 Limpando containers órfãos do B-Commerce..."
orphan_containers=$(docker ps -a --filter "name=b-commerce" --format "{{.Names}}" 2>/dev/null || true)
if [ -n "$orphan_containers" ]; then
    echo "Removendo containers órfãos: $orphan_containers"
    echo "$orphan_containers" | xargs docker rm -f 2>/dev/null || true
else
    echo "✅ Nenhum container órfão encontrado"
fi

# Remover volumes órfãos do B-Commerce (padrão mais amplo para WSL)
echo ""
echo "🗄️  Limpando volumes órfãos do B-Commerce..."
orphan_volumes=$(docker volume ls --filter "name=b-commerce" --format "{{.Name}}" 2>/dev/null || true)
if [ -z "$orphan_volumes" ]; then
    # Tentar padrão alternativo para volumes criados pelo Trae
    orphan_volumes=$(docker volume ls --filter "name=trae-solo" --format "{{.Name}}" 2>/dev/null || true)
fi

if [ -n "$orphan_volumes" ]; then
    echo "Removendo volumes órfãos: $orphan_volumes"
    echo "$orphan_volumes" | xargs docker volume rm 2>/dev/null || true
else
    echo "✅ Nenhum volume órfão encontrado"
fi

# Remover redes órfãs do B-Commerce (WSL específico)
echo ""
echo "🌐 Limpando redes órfãs do B-Commerce..."
orphan_networks=$(docker network ls --filter "name=b-commerce" --format "{{.Name}}" 2>/dev/null | grep -v "bridge\|host\|none" || true)
if [ -z "$orphan_networks" ]; then
    # Tentar padrão alternativo para redes criadas pelo Trae
    orphan_networks=$(docker network ls --filter "name=trae-solo" --format "{{.Name}}" 2>/dev/null | grep -v "bridge\|host\|none" || true)
fi

if [ -n "$orphan_networks" ]; then
    echo "Removendo redes órfãs: $orphan_networks"
    echo "$orphan_networks" | xargs docker network rm 2>/dev/null || true
else
    echo "✅ Nenhuma rede órfã encontrada"
fi

# Limpeza geral do Docker (opcional e segura para WSL)
echo ""
echo "🧽 Executando limpeza geral do Docker..."
docker system prune -f --volumes

echo ""
echo "📊 Status final:"
echo "==============="

echo ""
echo "🐳 Containers restantes do B-Commerce:"
remaining_containers=$(docker ps -a --filter "name=b-commerce" --format "table {{.Names}}\t{{.Status}}" 2>/dev/null || true)
if [ -n "$remaining_containers" ]; then
    echo "$remaining_containers"
else
    echo "✅ Nenhum container do B-Commerce encontrado"
fi

echo ""
echo "💾 Volumes restantes do B-Commerce:"
remaining_volumes=$(docker volume ls --filter "name=b-commerce" --format "table {{.Name}}\t{{.Driver}}" 2>/dev/null || true)
if [ -z "$remaining_volumes" ]; then
    remaining_volumes=$(docker volume ls --filter "name=trae-solo" --format "table {{.Name}}\t{{.Driver}}" 2>/dev/null || true)
fi
if [ -n "$remaining_volumes" ]; then
    echo "$remaining_volumes"
else
    echo "✅ Nenhum volume do B-Commerce encontrado"
fi

echo ""
echo "🌐 Redes restantes do B-Commerce:"
remaining_networks=$(docker network ls --filter "name=b-commerce" --format "table {{.Name}}\t{{.Driver}}" 2>/dev/null || true)
if [ -z "$remaining_networks" ]; then
    remaining_networks=$(docker network ls --filter "name=trae-solo" --format "table {{.Name}}\t{{.Driver}}" 2>/dev/null || true)
fi
if [ -n "$remaining_networks" ]; then
    echo "$remaining_networks"
else
    echo "✅ Nenhuma rede do B-Commerce encontrada"
fi

echo ""
echo "🖼️  Imagens do B-Commerce mantidas:"
docker images --filter "reference=*keycloak*" --filter "reference=*postgres*" --filter "reference=*redis*" --filter "reference=*rabbitmq*" --format "table {{.Repository}}\t{{.Tag}}\t{{.Size}}" 2>/dev/null || echo "✅ Imagens disponíveis para uso futuro"

echo ""
echo "✅ Limpeza completa realizada com sucesso no WSL!"
echo ""
echo "💡 Para recriar a infraestrutura:"
echo "🚀 Execute: ./Infra/docker/start-wsl.sh"
echo ""
echo "📝 Nota: Todas as configurações e dados foram removidos."
echo "   Será necessário reconfigurar os serviços após a próxima inicialização."
echo ""
echo "🐧 Executado no WSL - Docker Desktop Integration"
echo ""
echo "💡 Dicas pós-limpeza:"
echo "• Verifique o espaço em disco liberado: df -h"
echo "• Para ver imagens Docker restantes: docker images"
echo "• Para limpeza mais agressiva: docker system prune -a"