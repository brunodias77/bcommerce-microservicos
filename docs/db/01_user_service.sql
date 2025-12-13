-- ========================================
-- USER SERVICE DATABASE
-- Integrado com ASP.NET Core Identity
-- ========================================

-- ========================================
-- 1. EXTENSIONS
-- ========================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "citext";

-- ========================================
-- 2. FUNCTIONS
-- ========================================
CREATE OR REPLACE FUNCTION trigger_set_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION trigger_increment_version()
RETURNS TRIGGER AS $$
BEGIN
    NEW.version = OLD.version + 1;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ========================================
-- 3. ASP.NET CORE IDENTITY TABLES
-- Estas tabelas são criadas automaticamente pelo Identity
-- Mantidas aqui apenas como referência/documentação
-- O EF Core Migration irá gerenciá-las
-- ========================================

/*
-- REFERÊNCIA: Tabelas criadas pelo ASP.NET Core Identity
-- NÃO EXECUTE ESTE BLOCO - O Identity criará automaticamente

CREATE TABLE "AspNetUsers" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "UserName" VARCHAR(256),
    "NormalizedUserName" VARCHAR(256),
    "Email" VARCHAR(256),
    "NormalizedEmail" VARCHAR(256),
    "EmailConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "PasswordHash" TEXT,
    "SecurityStamp" TEXT,
    "ConcurrencyStamp" TEXT,
    "PhoneNumber" VARCHAR(20),
    "PhoneNumberConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "TwoFactorEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "LockoutEnd" TIMESTAMPTZ,
    "LockoutEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "AccessFailedCount" INT NOT NULL DEFAULT 0
);

CREATE TABLE "AspNetRoles" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Name" VARCHAR(256),
    "NormalizedName" VARCHAR(256),
    "ConcurrencyStamp" TEXT
);

CREATE TABLE "AspNetUserRoles" (
    "UserId" UUID NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    "RoleId" UUID NOT NULL REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE,
    PRIMARY KEY ("UserId", "RoleId")
);

CREATE TABLE "AspNetUserClaims" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    "ClaimType" TEXT,
    "ClaimValue" TEXT
);

CREATE TABLE "AspNetRoleClaims" (
    "Id" SERIAL PRIMARY KEY,
    "RoleId" UUID NOT NULL REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE,
    "ClaimType" TEXT,
    "ClaimValue" TEXT
);

CREATE TABLE "AspNetUserLogins" (
    "LoginProvider" VARCHAR(128) NOT NULL,
    "ProviderKey" VARCHAR(128) NOT NULL,
    "ProviderDisplayName" TEXT,
    "UserId" UUID NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    PRIMARY KEY ("LoginProvider", "ProviderKey")
);

CREATE TABLE "AspNetUserTokens" (
    "UserId" UUID NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    "LoginProvider" VARCHAR(128) NOT NULL,
    "Name" VARCHAR(128) NOT NULL,
    "Value" TEXT,
    PRIMARY KEY ("UserId", "LoginProvider", "Name")
);
*/

-- ========================================
-- 4. TABELAS CUSTOMIZADAS (Extensão do Identity)
-- ========================================

-- Tabela de perfil estendido do usuário
-- Relacionada com AspNetUsers via FK
CREATE TABLE user_profiles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL, -- FK para AspNetUsers.Id (criada após migration do Identity)
    
    -- Dados pessoais adicionais
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    display_name VARCHAR(100),
    avatar_url TEXT,
    birth_date DATE,
    gender VARCHAR(20),
    cpf VARCHAR(14), -- CPF formatado (XXX.XXX.XXX-XX)
    
    -- Preferências
    preferred_language VARCHAR(5) DEFAULT 'pt-BR',
    preferred_currency VARCHAR(3) DEFAULT 'BRL',
    newsletter_subscribed BOOLEAN DEFAULT FALSE,
    
    -- Marketing
    accepted_terms_at TIMESTAMPTZ,
    accepted_privacy_at TIMESTAMPTZ,
    
    -- Controle de versão
    version INT NOT NULL DEFAULT 1,
    
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ,
    
    -- Constraints
    CONSTRAINT uq_user_profiles_user_id UNIQUE (user_id),
    CONSTRAINT chk_user_profiles_cpf_format CHECK (
        cpf IS NULL OR cpf ~ '^\d{3}\.\d{3}\.\d{3}-\d{2}$'
    )
);

-- Tabela de endereços
CREATE TABLE addresses (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL, -- FK para AspNetUsers.Id
    
    -- Dados do endereço
    label VARCHAR(50), -- Ex: "Casa", "Trabalho"
    recipient_name VARCHAR(150),
    street VARCHAR(255) NOT NULL,
    number VARCHAR(20),
    complement VARCHAR(100),
    neighborhood VARCHAR(100),
    city VARCHAR(100) NOT NULL,
    state VARCHAR(2) NOT NULL,
    postal_code VARCHAR(9) NOT NULL,
    country VARCHAR(2) NOT NULL DEFAULT 'BR',
    
    -- Coordenadas (opcional, para cálculo de frete)
    latitude DECIMAL(10, 8),
    longitude DECIMAL(11, 8),
    
    -- Referência externa (IBGE)
    ibge_code VARCHAR(7),
    
    -- Controle
    is_default BOOLEAN DEFAULT FALSE,
    is_billing_address BOOLEAN DEFAULT FALSE,
    
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ,
    
    -- Constraints
    CONSTRAINT chk_addresses_postal_code_format CHECK (
        postal_code ~ '^\d{5}-?\d{3}$'
    ),
    CONSTRAINT chk_addresses_state_format CHECK (
        state ~ '^[A-Z]{2}$'
    )
);

-- Tabela de produtos favoritos (referência cross-service)
CREATE TABLE user_favorite_products (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL, -- FK para AspNetUsers.Id
    product_id UUID NOT NULL, -- Referência ao Catalog Service (sem FK)
    
    -- Snapshot do produto no momento da adição (para exibição offline)
    product_snapshot JSONB, -- { "name": "...", "price": 99.90, "image_url": "..." }
    
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Unique constraint
    CONSTRAINT uq_user_favorite_product UNIQUE (user_id, product_id)
);

-- Tabela de histórico de login (complementa AspNetUserLogins)
CREATE TABLE user_login_history (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL, -- FK para AspNetUsers.Id
    
    -- Dados do login
    login_provider VARCHAR(50) NOT NULL DEFAULT 'Local', -- Local, Google, Facebook, etc.
    ip_address VARCHAR(45),
    user_agent TEXT,
    
    -- Geolocalização (opcional)
    country VARCHAR(2),
    city VARCHAR(100),
    
    -- Device info
    device_type VARCHAR(20), -- Desktop, Mobile, Tablet
    device_info JSONB, -- { "browser": "Chrome", "os": "Windows 11", "device": "Desktop" }
    
    -- Status
    success BOOLEAN NOT NULL DEFAULT TRUE,
    failure_reason VARCHAR(100), -- Se success = false
    
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Tabela de sessões ativas (para gerenciamento de dispositivos)
CREATE TABLE user_sessions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL, -- FK para AspNetUsers.Id
    
    -- Identificação da sessão
    refresh_token_hash VARCHAR(512) NOT NULL,
    
    -- Device info
    device_id VARCHAR(100),
    device_name VARCHAR(100),
    device_type VARCHAR(20),
    
    -- Localização
    ip_address VARCHAR(45),
    country VARCHAR(2),
    city VARCHAR(100),
    
    -- Controle
    is_current BOOLEAN DEFAULT FALSE,
    expires_at TIMESTAMPTZ NOT NULL,
    revoked_at TIMESTAMPTZ,
    revoked_reason VARCHAR(100),
    
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_activity_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Tabela de notificações do usuário
CREATE TABLE user_notifications (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL, -- FK para AspNetUsers.Id
    
    -- Conteúdo
    title VARCHAR(200) NOT NULL,
    message TEXT NOT NULL,
    notification_type VARCHAR(50) NOT NULL, -- ORDER_UPDATE, PROMOTION, SYSTEM, etc.
    
    -- Referência opcional
    reference_type VARCHAR(50), -- ORDER, PRODUCT, COUPON
    reference_id UUID,
    
    -- Ação
    action_url TEXT,
    
    -- Controle
    read_at TIMESTAMPTZ,
    
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Tabela de preferências de notificação
CREATE TABLE user_notification_preferences (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL, -- FK para AspNetUsers.Id
    
    -- Canais
    email_enabled BOOLEAN DEFAULT TRUE,
    push_enabled BOOLEAN DEFAULT TRUE,
    sms_enabled BOOLEAN DEFAULT FALSE,
    
    -- Tipos de notificação
    order_updates BOOLEAN DEFAULT TRUE,
    promotions BOOLEAN DEFAULT TRUE,
    price_drops BOOLEAN DEFAULT TRUE, -- Produtos favoritos
    back_in_stock BOOLEAN DEFAULT TRUE, -- Produtos favoritos
    newsletter BOOLEAN DEFAULT FALSE,
    
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Unique
    CONSTRAINT uq_notification_prefs_user UNIQUE (user_id)
);

-- Outbox para eventos do User Service
CREATE TABLE user_outbox_events (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    aggregate_type VARCHAR(100) NOT NULL,
    aggregate_id UUID NOT NULL,
    event_type VARCHAR(100) NOT NULL, -- USER_REGISTERED, PROFILE_UPDATED, ADDRESS_ADDED, etc.
    payload JSONB NOT NULL,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    processed_at TIMESTAMPTZ,
    error_message TEXT,
    retry_count INT DEFAULT 0
);

-- Inbox para idempotência
CREATE TABLE user_inbox_messages (
    id UUID PRIMARY KEY,
    message_type VARCHAR(100) NOT NULL,
    processed_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Audit Log
CREATE TABLE user_audit_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    entity_type VARCHAR(100) NOT NULL,
    entity_id UUID NOT NULL,
    action VARCHAR(50) NOT NULL, -- CREATE, UPDATE, DELETE, LOGIN, LOGOUT, PASSWORD_CHANGE, etc.
    
    old_values JSONB,
    new_values JSONB,
    
    user_id UUID, -- Quem fez a ação (pode ser NULL para ações do sistema)
    ip_address VARCHAR(45),
    user_agent TEXT,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ========================================
-- 5. TRIGGERS
-- ========================================
CREATE TRIGGER trg_user_profiles_updated_at
    BEFORE UPDATE ON user_profiles
    FOR EACH ROW
    EXECUTE FUNCTION trigger_set_timestamp();

CREATE TRIGGER trg_user_profiles_version
    BEFORE UPDATE ON user_profiles
    FOR EACH ROW
    EXECUTE FUNCTION trigger_increment_version();

CREATE TRIGGER trg_addresses_updated_at
    BEFORE UPDATE ON addresses
    FOR EACH ROW
    EXECUTE FUNCTION trigger_set_timestamp();

CREATE TRIGGER trg_notification_prefs_updated_at
    BEFORE UPDATE ON user_notification_preferences
    FOR EACH ROW
    EXECUTE FUNCTION trigger_set_timestamp();

-- ========================================
-- 6. INDEXES
-- ========================================

-- User Profiles
CREATE INDEX idx_user_profiles_user_id ON user_profiles(user_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_user_profiles_cpf ON user_profiles(cpf) WHERE cpf IS NOT NULL AND deleted_at IS NULL;

-- Addresses
CREATE INDEX idx_addresses_user_id ON addresses(user_id) WHERE deleted_at IS NULL;
CREATE UNIQUE INDEX uq_addresses_default_per_user ON addresses(user_id) 
    WHERE is_default = TRUE AND deleted_at IS NULL;
CREATE INDEX idx_addresses_postal_code ON addresses(postal_code) WHERE deleted_at IS NULL;

-- Favorites
CREATE INDEX idx_favorites_user_id ON user_favorite_products(user_id);
CREATE INDEX idx_favorites_product_id ON user_favorite_products(product_id);

-- Login History
CREATE INDEX idx_login_history_user_id ON user_login_history(user_id);
CREATE INDEX idx_login_history_created_at ON user_login_history(created_at DESC);
CREATE INDEX idx_login_history_ip ON user_login_history(ip_address);

-- Sessions
CREATE INDEX idx_sessions_user_id ON user_sessions(user_id) WHERE revoked_at IS NULL;
CREATE INDEX idx_sessions_expires ON user_sessions(expires_at) WHERE revoked_at IS NULL;
CREATE UNIQUE INDEX uq_sessions_refresh_token ON user_sessions(refresh_token_hash);

-- Notifications
CREATE INDEX idx_notifications_user_id ON user_notifications(user_id);
CREATE INDEX idx_notifications_unread ON user_notifications(user_id, created_at DESC) WHERE read_at IS NULL;
CREATE INDEX idx_notifications_type ON user_notifications(notification_type);

-- Outbox
CREATE INDEX idx_user_outbox_unprocessed ON user_outbox_events(created_at) 
    WHERE processed_at IS NULL;
CREATE INDEX idx_user_outbox_aggregate ON user_outbox_events(aggregate_type, aggregate_id);

-- Audit
CREATE INDEX idx_user_audit_entity ON user_audit_logs(entity_type, entity_id);
CREATE INDEX idx_user_audit_user ON user_audit_logs(user_id) WHERE user_id IS NOT NULL;
CREATE INDEX idx_user_audit_created ON user_audit_logs(created_at DESC);
CREATE INDEX idx_user_audit_action ON user_audit_logs(action);

-- ========================================
-- 7. FOREIGN KEYS
-- Executar APÓS as migrations do Identity criarem AspNetUsers
-- ========================================

/*
-- EXECUTE APÓS A MIGRATION DO IDENTITY:

ALTER TABLE user_profiles 
    ADD CONSTRAINT fk_user_profiles_user 
    FOREIGN KEY (user_id) REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE;

ALTER TABLE addresses 
    ADD CONSTRAINT fk_addresses_user 
    FOREIGN KEY (user_id) REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE;

ALTER TABLE user_favorite_products 
    ADD CONSTRAINT fk_favorites_user 
    FOREIGN KEY (user_id) REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE;

ALTER TABLE user_login_history 
    ADD CONSTRAINT fk_login_history_user 
    FOREIGN KEY (user_id) REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE;

ALTER TABLE user_sessions 
    ADD CONSTRAINT fk_sessions_user 
    FOREIGN KEY (user_id) REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE;

ALTER TABLE user_notifications 
    ADD CONSTRAINT fk_notifications_user 
    FOREIGN KEY (user_id) REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE;

ALTER TABLE user_notification_preferences 
    ADD CONSTRAINT fk_notification_prefs_user 
    FOREIGN KEY (user_id) REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE;
*/

-- ========================================
-- 8. SEED DATA - Roles padrão
-- Executar APÓS as migrations do Identity
-- ========================================

/*
-- EXECUTE APÓS A MIGRATION DO IDENTITY:

INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp") VALUES
    (uuid_generate_v4(), 'Customer', 'CUSTOMER', uuid_generate_v4()::TEXT),
    (uuid_generate_v4(), 'Admin', 'ADMIN', uuid_generate_v4()::TEXT),
    (uuid_generate_v4(), 'Manager', 'MANAGER', uuid_generate_v4()::TEXT),
    (uuid_generate_v4(), 'Support', 'SUPPORT', uuid_generate_v4()::TEXT)
ON CONFLICT DO NOTHING;
*/

-- ========================================
-- 9. VIEWS
-- ========================================

-- View de usuários com perfil completo
-- Nota: Requer que AspNetUsers exista
/*
CREATE VIEW v_users_complete AS
SELECT 
    u."Id" AS user_id,
    u."Email",
    u."UserName",
    u."PhoneNumber",
    u."EmailConfirmed",
    u."PhoneNumberConfirmed",
    u."TwoFactorEnabled",
    u."LockoutEnd",
    u."LockoutEnabled",
    p.first_name,
    p.last_name,
    p.display_name,
    p.avatar_url,
    p.cpf,
    p.birth_date,
    p.newsletter_subscribed,
    p.created_at,
    p.updated_at,
    (SELECT COUNT(*) FROM addresses a WHERE a.user_id = u."Id" AND a.deleted_at IS NULL) AS address_count,
    (SELECT COUNT(*) FROM user_favorite_products f WHERE f.user_id = u."Id") AS favorites_count
FROM "AspNetUsers" u
LEFT JOIN user_profiles p ON p.user_id = u."Id" AND p.deleted_at IS NULL;
*/

-- View de sessões ativas por usuário
CREATE VIEW v_active_sessions AS
SELECT 
    user_id,
    COUNT(*) AS active_sessions,
    MAX(last_activity_at) AS last_activity,
    ARRAY_AGG(DISTINCT device_type) FILTER (WHERE device_type IS NOT NULL) AS device_types
FROM user_sessions
WHERE revoked_at IS NULL AND expires_at > NOW()
GROUP BY user_id;

-- View de notificações não lidas
CREATE VIEW v_unread_notifications AS
SELECT 
    user_id,
    COUNT(*) AS unread_count,
    MIN(created_at) AS oldest_unread,
    MAX(created_at) AS newest_unread
FROM user_notifications
WHERE read_at IS NULL
GROUP BY user_id;

-- ========================================
-- 10. COMMENTS
-- ========================================
COMMENT ON TABLE user_profiles IS 'Dados estendidos do perfil do usuário (complementa AspNetUsers)';
COMMENT ON TABLE addresses IS 'Endereços de entrega e cobrança dos usuários';
COMMENT ON TABLE user_favorite_products IS 'Produtos favoritos (referência cross-service ao Catalog)';
COMMENT ON TABLE user_login_history IS 'Histórico de logins para auditoria e segurança';
COMMENT ON TABLE user_sessions IS 'Sessões ativas para gerenciamento de dispositivos';
COMMENT ON TABLE user_notifications IS 'Notificações in-app para os usuários';
COMMENT ON TABLE user_notification_preferences IS 'Preferências de notificação por canal e tipo';
COMMENT ON TABLE user_outbox_events IS 'Outbox pattern para eventos assíncronos';
COMMENT ON TABLE user_audit_logs IS 'Log de auditoria de todas as operações';

-- ========================================
-- 11. EXEMPLO DE ENTIDADE IDENTITY CUSTOMIZADA
-- Para uso no código C#
-- ========================================

/*
-- Modelo C# correspondente:

public class ApplicationUser : IdentityUser<Guid>
{
    // Propriedades do Identity já incluídas:
    // Id, UserName, NormalizedUserName, Email, NormalizedEmail,
    // EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
    // PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled,
    // LockoutEnd, LockoutEnabled, AccessFailedCount

    // Navegação para perfil estendido
    public virtual UserProfile? Profile { get; set; }
    
    // Navegação para endereços
    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
    
    // Navegação para favoritos
    public virtual ICollection<UserFavoriteProduct> FavoriteProducts { get; set; } = new List<UserFavoriteProduct>();
    
    // Navegação para sessões
    public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    
    // Navegação para notificações
    public virtual ICollection<UserNotification> Notifications { get; set; } = new List<UserNotification>();
    
    // Navegação para preferências de notificação
    public virtual UserNotificationPreference? NotificationPreferences { get; set; }
}
*/
