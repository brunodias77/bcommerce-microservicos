#!/bin/bash

# B-Commerce - Script para parar todos os containers
# Autor: B-Commerce Team
# Data: $(date +%Y-%m-%d)

set -e

echo "🛑 Parando B-Commerce Infrastructure..."
echo "====================================="

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

echo "📋 Status atual dos containers:"
docker-compose ps

echo "\n🛑 Parando todos os containers..."
docker-compose stop

echo "\n⏳ Aguardando containers pararem completamente..."
sleep 3

echo "\n📊 Verificando se todos os containers pararam:"
running_containers=$(docker-compose ps --services --filter "status=running" 2>/dev/null || true)

if [ -z "$running_containers" ]; then
    echo "✅ Todos os containers foram parados com sucesso!"
else
    echo "⚠️  Alguns containers ainda estão rodando:"
    echo "$running_containers"
    echo "\n🔄 Tentando parar containers restantes..."
    docker-compose down
fi

echo "\n📋 Status final dos containers:"
docker-compose ps

echo "\n✅ B-Commerce Infrastructure parada com sucesso!"
echo "\n💡 Comandos úteis:"
echo "🚀 Para iniciar novamente: ./scripts/start.sh"
echo "📊 Para verificar status: ./scripts/status.sh"
echo "🧹 Para limpar tudo: ./scripts/cleanup.sh"
echo "🔍 Para ver logs: docker-compose logs [nome-do-serviço]"

echo "\n📝 Nota: Os volumes de dados foram preservados."
echo "   Os dados dos bancos e configurações permanecerão intactos."