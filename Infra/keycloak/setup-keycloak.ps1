# B-Commerce - Script de configuração do Keycloak para Windows
# Autor: B-Commerce Team
# Data: $(Get-Date -Format 'yyyy-MM-dd')

# Configuração de tratamento de erros
$ErrorActionPreference = "Stop"

Write-Host "🔐 B-Commerce Keycloak Setup" -ForegroundColor Cyan
Write-Host "============================" -ForegroundColor Cyan

# Configurações
$KEYCLOAK_URL = "http://localhost:8080"
$ADMIN_USER = "admin"
$ADMIN_PASS = "admin123"
$REALM_NAME = "b-commerce"
$BACKEND_CLIENT_ID = "backend-api"
$FRONTEND_CLIENT_ID = "frontend-app"

# Função para aguardar o Keycloak ficar disponível
function Wait-ForKeycloak {
    Write-Host "⏳ Aguardando Keycloak ficar disponível..." -ForegroundColor Yellow
    
    for ($i = 1; $i -le 60; $i++) {
        try {
            $response = Invoke-RestMethod -Uri "$KEYCLOAK_URL/health" -Method Get -TimeoutSec 5 -ErrorAction SilentlyContinue
            Write-Host "✅ Keycloak está disponível!" -ForegroundColor Green
            return $true
        }
        catch {
            Write-Host "   Tentativa $i/60..." -ForegroundColor Gray
            Start-Sleep -Seconds 5
        }
    }
    
    Write-Host "❌ Timeout: Keycloak não ficou disponível em 5 minutos" -ForegroundColor Red
    exit 1
}

# Função para obter token de acesso
function Get-AccessToken {
    try {
        $body = @{
            username = $ADMIN_USER
            password = $ADMIN_PASS
            grant_type = "password"
            client_id = "admin-cli"
        }
        
        $headers = @{
            "Content-Type" = "application/x-www-form-urlencoded"
        }
        
        $response = Invoke-RestMethod -Uri "$KEYCLOAK_URL/realms/master/protocol/openid-connect/token" -Method Post -Body $body -Headers $headers
        return $response.access_token
    }
    catch {
        Write-Host "❌ Erro ao obter token de acesso: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

# Função para verificar se o realm existe
function Test-RealmExists {
    param(
        [string]$Token
    )
    
    try {
        $headers = @{
            "Authorization" = "Bearer $Token"
        }
        
        $response = Invoke-RestMethod -Uri "$KEYCLOAK_URL/admin/realms/$REALM_NAME" -Method Get -Headers $headers -ErrorAction SilentlyContinue
        return $true
    }
    catch {
        return $false
    }
}

# Função para criar realm
function New-Realm {
    param(
        [string]$Token
    )
    
    Write-Host "🏗️  Criando realm '$REALM_NAME'..." -ForegroundColor Blue
    
    $realmConfig = @{
        realm = $REALM_NAME
        enabled = $true
        displayName = "B-Commerce"
        displayNameHtml = "<strong>B-Commerce</strong>"
        registrationAllowed = $true
        registrationEmailAsUsername = $true
        rememberMe = $true
        verifyEmail = $false
        loginWithEmailAllowed = $true
        duplicateEmailsAllowed = $false
        resetPasswordAllowed = $true
        editUsernameAllowed = $false
        bruteForceProtected = $true
        permanentLockout = $false
        maxFailureWaitSeconds = 900
        minimumQuickLoginWaitSeconds = 60
        waitIncrementSeconds = 60
        quickLoginCheckMilliSeconds = 1000
        maxDeltaTimeSeconds = 43200
        failureFactor = 30
        defaultRoles = @("offline_access", "uma_authorization")
        requiredCredentials = @("password")
        passwordPolicy = "length(8) and digits(1) and lowerCase(1) and upperCase(1)"
        sslRequired = "external"
        accessTokenLifespan = 300
        accessTokenLifespanForImplicitFlow = 900
        ssoSessionIdleTimeout = 1800
        ssoSessionMaxLifespan = 36000
        offlineSessionIdleTimeout = 2592000
        accessCodeLifespan = 60
        accessCodeLifespanUserAction = 300
        accessCodeLifespanLogin = 1800
        actionTokenGeneratedByAdminLifespan = 43200
        actionTokenGeneratedByUserLifespan = 300
        internationalizationEnabled = $true
        supportedLocales = @("pt-BR", "en")
        defaultLocale = "pt-BR"
    }
    
    try {
        $headers = @{
            "Authorization" = "Bearer $Token"
            "Content-Type" = "application/json"
        }
        
        $jsonBody = $realmConfig | ConvertTo-Json -Depth 10
        $response = Invoke-RestMethod -Uri "$KEYCLOAK_URL/admin/realms" -Method Post -Headers $headers -Body $jsonBody
        
        Write-Host "✅ Realm '$REALM_NAME' criado com sucesso!" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Erro ao criar realm. Erro: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

# Função para criar client
function New-Client {
    param(
        [string]$Token,
        [string]$ClientId,
        [string]$ClientType
    )
    
    Write-Host "🔧 Criando client '$ClientId'..." -ForegroundColor Blue
    
    if ($ClientType -eq "frontend") {
        $redirectUris = @("http://localhost:3000/*", "http://localhost:5173/*", "http://localhost:4200/*")
        $webOrigins = @("http://localhost:3000", "http://localhost:5173", "http://localhost:4200")
        $publicClient = $true
        $standardFlowEnabled = $true
        $implicitFlowEnabled = $false
        $directAccessGrantsEnabled = $true
        $serviceAccountsEnabled = $false
    }
    else {
        $redirectUris = @()
        $webOrigins = @()
        $publicClient = $false
        $standardFlowEnabled = $false
        $implicitFlowEnabled = $false
        $directAccessGrantsEnabled = $true
        $serviceAccountsEnabled = $true
    }
    
    $clientConfig = @{
        clientId = $ClientId
        name = $ClientId
        description = "B-Commerce $ClientType client"
        enabled = $true
        publicClient = $publicClient
        standardFlowEnabled = $standardFlowEnabled
        implicitFlowEnabled = $implicitFlowEnabled
        directAccessGrantsEnabled = $directAccessGrantsEnabled
        serviceAccountsEnabled = $serviceAccountsEnabled
        redirectUris = $redirectUris
        webOrigins = $webOrigins
        protocol = "openid-connect"
        attributes = @{
            "access.token.lifespan" = "300"
            "client.session.idle.timeout" = "1800"
            "client.session.max.lifespan" = "36000"
        }
    }
    
    try {
        $headers = @{
            "Authorization" = "Bearer $Token"
            "Content-Type" = "application/json"
        }
        
        $jsonBody = $clientConfig | ConvertTo-Json -Depth 10
        $response = Invoke-RestMethod -Uri "$KEYCLOAK_URL/admin/realms/$REALM_NAME/clients" -Method Post -Headers $headers -Body $jsonBody
        
        Write-Host "✅ Client '$ClientId' criado com sucesso!" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Erro ao criar client '$ClientId'. Erro: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Função para criar usuário
function New-User {
    param(
        [string]$Token,
        [string]$Username,
        [string]$Email,
        [string]$Password,
        [string]$FirstName,
        [string]$LastName,
        [bool]$IsAdmin
    )
    
    Write-Host "👤 Criando usuário '$Username'..." -ForegroundColor Blue
    
    $userConfig = @{
        username = $Username
        email = $Email
        firstName = $FirstName
        lastName = $LastName
        enabled = $true
        emailVerified = $true
        credentials = @(
            @{
                type = "password"
                value = $Password
                temporary = $false
            }
        )
    }
    
    try {
        $headers = @{
            "Authorization" = "Bearer $Token"
            "Content-Type" = "application/json"
        }
        
        $jsonBody = $userConfig | ConvertTo-Json -Depth 10
        $response = Invoke-RestMethod -Uri "$KEYCLOAK_URL/admin/realms/$REALM_NAME/users" -Method Post -Headers $headers -Body $jsonBody
        
        Write-Host "✅ Usuário '$Username' criado com sucesso!" -ForegroundColor Green
        
        if ($IsAdmin) {
            Write-Host "🔐 Adicionando permissões de administrador..." -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "❌ Erro ao criar usuário '$Username'. Erro: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Verificar se o Keycloak está rodando
try {
    $dockerPs = docker ps | Select-String "b-commerce-keycloak"
    if (-not $dockerPs) {
        Write-Host "❌ Container do Keycloak não está rodando." -ForegroundColor Red
        Write-Host "   Execute primeiro: docker-compose -f infra/docker/docker-compose.yml up -d keycloak" -ForegroundColor Yellow
        exit 1
    }
}
catch {
    Write-Host "❌ Erro ao verificar containers Docker. Certifique-se de que o Docker está instalado e rodando." -ForegroundColor Red
    exit 1
}

# Aguardar Keycloak ficar disponível
Wait-ForKeycloak

# Obter token de acesso
Write-Host "🔑 Obtendo token de acesso..." -ForegroundColor Yellow
$ACCESS_TOKEN = Get-AccessToken

if (-not $ACCESS_TOKEN) {
    Write-Host "❌ Não foi possível obter token de acesso" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Token obtido com sucesso!" -ForegroundColor Green

# Verificar se o realm já existe
if (Test-RealmExists -Token $ACCESS_TOKEN) {
    Write-Host "⚠️  Realm '$REALM_NAME' já existe. Pulando criação..." -ForegroundColor Yellow
}
else {
    New-Realm -Token $ACCESS_TOKEN
}

# Criar clients
New-Client -Token $ACCESS_TOKEN -ClientId $BACKEND_CLIENT_ID -ClientType "backend"
New-Client -Token $ACCESS_TOKEN -ClientId $FRONTEND_CLIENT_ID -ClientType "frontend"

# Criar usuários
New-User -Token $ACCESS_TOKEN -Username "bruno.admin" -Email "bruno@admin.com" -Password "Admin123!" -FirstName "Bruno" -LastName "Admin" -IsAdmin $true
New-User -Token $ACCESS_TOKEN -Username "bruno.user" -Email "bruno@teste.com" -Password "User123!" -FirstName "Bruno" -LastName "User" -IsAdmin $false

Write-Host "`n🎉 Configuração do Keycloak concluída com sucesso!" -ForegroundColor Green
Write-Host "`n📋 Resumo da configuração:" -ForegroundColor Cyan
Write-Host "🌐 Keycloak URL: $KEYCLOAK_URL" -ForegroundColor White
Write-Host "🏛️  Realm: $REALM_NAME" -ForegroundColor White
Write-Host "🔧 Backend Client: $BACKEND_CLIENT_ID" -ForegroundColor White
Write-Host "🖥️  Frontend Client: $FRONTEND_CLIENT_ID" -ForegroundColor White
Write-Host "`n👥 Usuários criados:" -ForegroundColor Cyan
Write-Host "🔐 Admin: bruno@admin.com / Admin123!" -ForegroundColor White
Write-Host "👤 User: bruno@teste.com / User123!" -ForegroundColor White
Write-Host "`n💡 Para acessar o console administrativo:" -ForegroundColor Cyan
Write-Host "   URL: $KEYCLOAK_URL/admin" -ForegroundColor White
Write-Host "   Login: $ADMIN_USER / $ADMIN_PASS" -ForegroundColor White
Write-Host "`n🔗 URLs importantes:" -ForegroundColor Cyan
Write-Host "   Realm: $KEYCLOAK_URL/realms/$REALM_NAME" -ForegroundColor White
Write-Host "   Auth: $KEYCLOAK_URL/realms/$REALM_NAME/protocol/openid-connect/auth" -ForegroundColor White
Write-Host "   Token: $KEYCLOAK_URL/realms/$REALM_NAME/protocol/openid-connect/token" -ForegroundColor White