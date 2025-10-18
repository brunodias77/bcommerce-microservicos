#!/bin/bash

# B-Commerce - Script para parar todos os containers (WSL Version)
# Autor: B-Commerce Team
# Data: $(date +%Y-%m-%d)
# Compatível com Windows WSL + Docker Desktop

set -e

echo "🛑 Parando B-Commerce Infrastructure (WSL)..."
echo "============================================="

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

echo "📋 Status atual dos containers:"
docker_compose_cmd ps

echo ""
echo "🛑 Parando todos os containers..."
docker_compose_cmd stop

echo ""
echo "⏳ Aguardando containers pararem completamente..."
sleep 3

echo ""
echo "📊 Verificando se todos os containers pararam:"

# Verificação mais robusta para WSL
running_containers=$(docker_compose_cmd ps --services --filter "status=running" 2>/dev/null || true)

if [ -z "$running_containers" ]; then
    echo "✅ Todos os containers foram parados com sucesso!"
else
    echo "⚠️  Alguns containers ainda estão rodando:"
    echo "$running_containers"
    echo ""
    echo "🔄 Tentando parar containers restantes..."
    docker_compose_cmd down
    
    # Verificação final
    sleep 2
    final_check=$(docker_compose_cmd ps --services --filter "status=running" 2>/dev/null || true)
    if [ -z "$final_check" ]; then
        echo "✅ Todos os containers foram parados com sucesso!"
    else
        echo "⚠️  Alguns containers ainda podem estar rodando. Verifique manualmente."
    fi
fi

echo ""
echo "📋 Status final dos containers:"
docker_compose_cmd ps

echo ""
echo "✅ B-Commerce Infrastructure parada com sucesso no WSL!"
echo ""
echo "💡 Comandos úteis:"
echo "🚀 Para iniciar novamente: ./Infra/docker/start-wsl.sh"
echo "📊 Para verificar status: ./Infra/docker/status-wsl.sh"
echo "🧹 Para limpar tudo: ./Infra/docker/cleanup-wsl.sh"
echo "🔍 Para ver logs: docker-compose logs [nome-do-serviço]"
echo "   ou: docker compose logs [nome-do-serviço]"

echo ""
echo "📝 Nota: Os volumes de dados foram preservados."
echo "   Os dados dos bancos e configurações permanecerão intactos."
echo ""
echo "🐧 Executado no WSL - Docker Desktop Integration"