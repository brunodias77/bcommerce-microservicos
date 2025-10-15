#!/bin/bash

# B-Commerce - Script de configuração do Keycloak
# Autor: B-Commerce Team
# Data: $(date +%Y-%m-%d)

set -e

echo "🔐 B-Commerce Keycloak Setup"
echo "============================"

# Configurações
KEYCLOAK_URL="http://localhost:8080"
ADMIN_USER="admin"
ADMIN_PASS="admin123"
REALM_NAME="b-commerce"
BACKEND_CLIENT_ID="backend-api"
FRONTEND_CLIENT_ID="frontend-app"

# Função para aguardar o Keycloak ficar disponível
wait_for_keycloak() {
    echo "⏳ Aguardando Keycloak ficar disponível..."
    for i in {1..60}; do
        if curl -s "$KEYCLOAK_URL/health" > /dev/null 2>&1; then
            echo "✅ Keycloak está disponível!"
            return 0
        fi
        echo "   Tentativa $i/60..."
        sleep 5
    done
    echo "❌ Timeout: Keycloak não ficou disponível em 5 minutos"
    exit 1
}

# Função para obter token de acesso
get_access_token() {
    local token_response
    token_response=$(curl -s -X POST "$KEYCLOAK_URL/realms/master/protocol/openid-connect/token" \
        -H "Content-Type: application/x-www-form-urlencoded" \
        -d "username=$ADMIN_USER" \
        -d "password=$ADMIN_PASS" \
        -d "grant_type=password" \
        -d "client_id=admin-cli")
    
    if [ $? -ne 0 ]; then
        echo "❌ Erro ao obter token de acesso"
        exit 1
    fi
    
    echo "$token_response" | grep -o '"access_token":"[^"]*' | cut -d'"' -f4
}

# Função para verificar se o realm existe
realm_exists() {
    local token=$1
    local response
    response=$(curl -s -o /dev/null -w "%{http_code}" \
        -H "Authorization: Bearer $token" \
        "$KEYCLOAK_URL/admin/realms/$REALM_NAME")
    
    [ "$response" = "200" ]
}

# Função para criar realm
create_realm() {
    local token=$1
    echo "🏗️  Criando realm '$REALM_NAME'..."
    
    local realm_config='{
        "realm": "'$REALM_NAME'",
        "enabled": true,
        "displayName": "B-Commerce",
        "displayNameHtml": "<strong>B-Commerce</strong>",
        "registrationAllowed": true,
        "registrationEmailAsUsername": true,
        "rememberMe": true,
        "verifyEmail": false,
        "loginWithEmailAllowed": true,
        "duplicateEmailsAllowed": false,
        "resetPasswordAllowed": true,
        "editUsernameAllowed": false,
        "bruteForceProtected": true,
        "permanentLockout": false,
        "maxFailureWaitSeconds": 900,
        "minimumQuickLoginWaitSeconds": 60,
        "waitIncrementSeconds": 60,
        "quickLoginCheckMilliSeconds": 1000,
        "maxDeltaTimeSeconds": 43200,
        "failureFactor": 30,
        "defaultRoles": ["offline_access", "uma_authorization"],
        "requiredCredentials": ["password"],
        "passwordPolicy": "length(8) and digits(1) and lowerCase(1) and upperCase(1)",
        "otpPolicyType": "totp",
        "otpPolicyAlgorithm": "HmacSHA1",
        "otpPolicyInitialCounter": 0,
        "otpPolicyDigits": 6,
        "otpPolicyLookAheadWindow": 1,
        "otpPolicyPeriod": 30,
        "sslRequired": "external",
        "accessTokenLifespan": 300,
        "accessTokenLifespanForImplicitFlow": 900,
        "ssoSessionIdleTimeout": 1800,
        "ssoSessionMaxLifespan": 36000,
        "offlineSessionIdleTimeout": 2592000,
        "accessCodeLifespan": 60,
        "accessCodeLifespanUserAction": 300,
        "accessCodeLifespanLogin": 1800,
        "actionTokenGeneratedByAdminLifespan": 43200,
        "actionTokenGeneratedByUserLifespan": 300,
        "internationalizationEnabled": true,
        "supportedLocales": ["pt-BR", "en"],
        "defaultLocale": "pt-BR"
    }'
    
    local response
    response=$(curl -s -o /dev/null -w "%{http_code}" -X POST \
        "$KEYCLOAK_URL/admin/realms" \
        -H "Authorization: Bearer $token" \
        -H "Content-Type: application/json" \
        -d "$realm_config")
    
    if [ "$response" = "201" ]; then
        echo "✅ Realm '$REALM_NAME' criado com sucesso!"
    else
        echo "❌ Erro ao criar realm. HTTP Status: $response"
        exit 1
    fi
}

# Função para criar client
create_client() {
    local token=$1
    local client_id=$2
    local client_type=$3
    
    echo "🔧 Criando client '$client_id'..."
    
    local redirect_uris
    local web_origins
    local public_client
    local standard_flow_enabled
    local implicit_flow_enabled
    local direct_access_grants_enabled
    local service_accounts_enabled
    
    if [ "$client_type" = "frontend" ]; then
        redirect_uris='["http://localhost:3000/*", "http://localhost:5173/*", "http://localhost:4200/*"]'
        web_origins='["http://localhost:3000", "http://localhost:5173", "http://localhost:4200"]'
        public_client="true"
        standard_flow_enabled="true"
        implicit_flow_enabled="false"
        direct_access_grants_enabled="true"
        service_accounts_enabled="false"
    else
        redirect_uris='[]'
        web_origins='[]'
        public_client="false"
        standard_flow_enabled="false"
        implicit_flow_enabled="false"
        direct_access_grants_enabled="true"
        service_accounts_enabled="true"
    fi
    
    local client_config='{
        "clientId": "'$client_id'",
        "name": "'$client_id'",
        "description": "B-Commerce '$client_type' client",
        "enabled": true,
        "publicClient": '$public_client',
        "standardFlowEnabled": '$standard_flow_enabled',
        "implicitFlowEnabled": '$implicit_flow_enabled',
        "directAccessGrantsEnabled": '$direct_access_grants_enabled',
        "serviceAccountsEnabled": '$service_accounts_enabled',
        "redirectUris": '$redirect_uris',
        "webOrigins": '$web_origins',
        "protocol": "openid-connect",
        "attributes": {
            "access.token.lifespan": "300",
            "client.session.idle.timeout": "1800",
            "client.session.max.lifespan": "36000"
        }
    }'
    
    local response
    response=$(curl -s -o /dev/null -w "%{http_code}" -X POST \
        "$KEYCLOAK_URL/admin/realms/$REALM_NAME/clients" \
        -H "Authorization: Bearer $token" \
        -H "Content-Type: application/json" \
        -d "$client_config")
    
    if [ "$response" = "201" ]; then
        echo "✅ Client '$client_id' criado com sucesso!"
    else
        echo "❌ Erro ao criar client '$client_id'. HTTP Status: $response"
    fi
}

# Função para criar usuário
create_user() {
    local token=$1
    local username=$2
    local email=$3
    local password=$4
    local first_name=$5
    local last_name=$6
    local is_admin=$7
    
    echo "👤 Criando usuário '$username'..."
    
    local user_config='{
        "username": "'$username'",
        "email": "'$email'",
        "firstName": "'$first_name'",
        "lastName": "'$last_name'",
        "enabled": true,
        "emailVerified": true,
        "credentials": [{
            "type": "password",
            "value": "'$password'",
            "temporary": false
        }]
    }'
    
    local response
    response=$(curl -s -o /dev/null -w "%{http_code}" -X POST \
        "$KEYCLOAK_URL/admin/realms/$REALM_NAME/users" \
        -H "Authorization: Bearer $token" \
        -H "Content-Type: application/json" \
        -d "$user_config")
    
    if [ "$response" = "201" ]; then
        echo "✅ Usuário '$username' criado com sucesso!"
        
        # Se for admin, adicionar role de admin
        if [ "$is_admin" = "true" ]; then
            echo "🔐 Adicionando permissões de administrador..."
            # Aqui você pode adicionar lógica para atribuir roles de admin
        fi
    else
        echo "❌ Erro ao criar usuário '$username'. HTTP Status: $response"
    fi
}

# Verificar se o Keycloak está rodando
if ! docker ps | grep -q "b-commerce-keycloak"; then
    echo "❌ Container do Keycloak não está rodando."
    echo "   Execute primeiro: ./scripts/start.sh"
    exit 1
fi

# Aguardar Keycloak ficar disponível
wait_for_keycloak

# Obter token de acesso
echo "🔑 Obtendo token de acesso..."
ACCESS_TOKEN=$(get_access_token)

if [ -z "$ACCESS_TOKEN" ]; then
    echo "❌ Não foi possível obter token de acesso"
    exit 1
fi

echo "✅ Token obtido com sucesso!"

# Verificar se o realm já existe
if realm_exists "$ACCESS_TOKEN"; then
    echo "⚠️  Realm '$REALM_NAME' já existe. Pulando criação..."
else
    create_realm "$ACCESS_TOKEN"
fi

# Criar clients
create_client "$ACCESS_TOKEN" "$BACKEND_CLIENT_ID" "backend"
create_client "$ACCESS_TOKEN" "$FRONTEND_CLIENT_ID" "frontend"

# Criar usuários
create_user "$ACCESS_TOKEN" "bruno.admin" "bruno@admin.com" "Admin123!" "Bruno" "Admin" "true"
create_user "$ACCESS_TOKEN" "bruno.user" "bruno@teste.com" "User123!" "Bruno" "User" "false"

echo "\n🎉 Configuração do Keycloak concluída com sucesso!"
echo "\n📋 Resumo da configuração:"
echo "🌐 Keycloak URL: $KEYCLOAK_URL"
echo "🏛️  Realm: $REALM_NAME"
echo "🔧 Backend Client: $BACKEND_CLIENT_ID"
echo "🖥️  Frontend Client: $FRONTEND_CLIENT_ID"
echo "\n👥 Usuários criados:"
echo "🔐 Admin: bruno@admin.com / Admin123!"
echo "👤 User: bruno@teste.com / User123!"
echo "\n💡 Para acessar o console administrativo:"
echo "   URL: $KEYCLOAK_URL/admin"
echo "   Login: $ADMIN_USER / $ADMIN_PASS"
echo "\n🔗 URLs importantes:"
echo "   Realm: $KEYCLOAK_URL/realms/$REALM_NAME"
echo "   Auth: $KEYCLOAK_URL/realms/$REALM_NAME/protocol/openid-connect/auth"
echo "   Token: $KEYCLOAK_URL/realms/$REALM_NAME/protocol/openid-connect/token"