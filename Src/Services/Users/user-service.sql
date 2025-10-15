-- =====================================================================
-- MICROSSERVIÇO 1: USER MANAGEMENT SERVICE
-- Responsabilidades: Autenticação, autorização, perfis de usuário
-- =====================================================================

-- Base comum para todos os microsserviços
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Enums específicos do User Management
CREATE TYPE user_role_enum AS ENUM ('customer', 'admin');
CREATE TYPE consent_type_enum AS ENUM ('marketing_email', 'newsletter_subscription', 'terms_of_service', 'privacy_policy', 'cookies_essential', 'cookies_analytics', 'cookies_marketing');
CREATE TYPE card_brand_enum AS ENUM ('visa', 'mastercard', 'amex', 'elo', 'hipercard', 'diners_club', 'discover', 'jcb', 'aura', 'other');
CREATE TYPE address_type_enum AS ENUM ('shipping', 'billing');

-- Função de timestamp
CREATE OR REPLACE FUNCTION trigger_set_timestamp()
RETURNS TRIGGER AS $$
BEGIN
  NEW.updated_at = CURRENT_TIMESTAMP;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Validação CPF
CREATE OR REPLACE FUNCTION is_cpf_valid(cpf TEXT)
RETURNS BOOLEAN AS $$
DECLARE
    cpf_clean TEXT;
    cpf_array INT[];
    sum1 INT := 0;
    sum2 INT := 0;
    i INT;
BEGIN
    cpf_clean := REGEXP_REPLACE(cpf, '[^0-9]', '', 'g');
    IF LENGTH(cpf_clean) != 11 OR cpf_clean ~ '(\d)\1{10}' THEN
        RETURN FALSE;
    END IF;
    cpf_array := STRING_TO_ARRAY(cpf_clean, NULL)::INT[];
    FOR i IN 1..9 LOOP
        sum1 := sum1 + cpf_array[i] * (11 - i);
    END LOOP;
    sum1 := 11 - (sum1 % 11);
    IF sum1 >= 10 THEN sum1 := 0; END IF;
    IF sum1 != cpf_array[10] THEN RETURN FALSE; END IF;

    FOR i IN 1..10 LOOP
        sum2 := sum2 + cpf_array[i] * (12 - i);
    END LOOP;
    sum2 := 11 - (sum2 % 11);
    IF sum2 >= 10 THEN sum2 := 0; END IF;
    IF sum2 != cpf_array[11] THEN RETURN FALSE; END IF;

    RETURN TRUE;
END;
$$ LANGUAGE plpgsql IMMUTABLE;

-- TABELAS DO USER MANAGEMENT SERVICE

-- Tabela principal de usuários
CREATE TABLE users (
    user_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    keycloak_id UUID UNIQUE, -- ID do usuário no Keycloak para SSO
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(155) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    email_verified_at TIMESTAMPTZ,
    phone VARCHAR(20) CHECK (phone ~ '^\+?[1-9]\d{1,14}$'), -- Formato E.164
    password_hash VARCHAR(255), -- Nullable quando usando Keycloak
    cpf CHAR(11) UNIQUE,
    date_of_birth DATE,
    newsletter_opt_in BOOLEAN NOT NULL DEFAULT FALSE,
    status VARCHAR(20) NOT NULL DEFAULT 'ativo' CHECK (status IN ('ativo', 'inativo', 'banido')),
    role user_role_enum NOT NULL DEFAULT 'customer',
    failed_login_attempts SMALLINT NOT NULL DEFAULT 0,
    account_locked_until TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1,
    CONSTRAINT chk_cpf_valid CHECK (cpf IS NULL OR is_cpf_valid(cpf)),
    CONSTRAINT chk_auth_method CHECK (password_hash IS NOT NULL OR keycloak_id IS NOT NULL)
);
CREATE UNIQUE INDEX idx_users_active_email ON users (email) WHERE deleted_at IS NULL;
CREATE INDEX idx_users_status ON users (status) WHERE deleted_at IS NULL;
CREATE INDEX idx_users_role ON users (role);
CREATE TRIGGER set_timestamp_users BEFORE UPDATE ON users FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- Endereços dos usuários
CREATE TABLE user_addresses (
    address_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    type address_type_enum NOT NULL,
    postal_code CHAR(8) NOT NULL,
    street VARCHAR(150) NOT NULL,
    street_number VARCHAR(20) NOT NULL,
    complement VARCHAR(100),
    neighborhood VARCHAR(100) NOT NULL,
    city VARCHAR(100) NOT NULL,
    state_code CHAR(2) NOT NULL,
    country_code CHAR(2) NOT NULL DEFAULT 'BR',
    is_default BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1
);
CREATE INDEX idx_user_addresses_user_id ON user_addresses (user_id);
CREATE UNIQUE INDEX uq_user_addresses_default_per_user_type ON user_addresses (user_id, type) WHERE is_default = TRUE AND deleted_at IS NULL;
CREATE TRIGGER set_timestamp_user_addresses BEFORE UPDATE ON user_addresses FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- Cartões salvos dos usuários
CREATE TABLE user_saved_cards (
    saved_card_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    nickname VARCHAR(50),
    last_four_digits CHAR(4) NOT NULL,
    brand card_brand_enum NOT NULL,
    gateway_token VARCHAR(255) NOT NULL UNIQUE,
    expiry_date DATE NOT NULL,
    is_default BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1
);
CREATE INDEX idx_user_saved_cards_user_id ON user_saved_cards (user_id);
CREATE UNIQUE INDEX uq_user_saved_cards_default_per_user ON user_saved_cards (user_id) WHERE is_default = TRUE AND deleted_at IS NULL;
CREATE TRIGGER set_timestamp_user_saved_cards BEFORE UPDATE ON user_saved_cards FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- Tokens de autenticação
CREATE TABLE user_tokens (
    token_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    token_type VARCHAR(50) NOT NULL, -- 'refresh', 'email_verification', 'password_reset'
    token_value VARCHAR(2048) NOT NULL UNIQUE,
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    revoked_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1
);
CREATE INDEX idx_user_tokens_user_id ON user_tokens (user_id);
CREATE INDEX idx_user_tokens_type ON user_tokens (token_type);
CREATE INDEX idx_user_tokens_expires_at ON user_tokens (expires_at);

-- Consentimentos LGPD
CREATE TABLE user_consents (
    consent_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    type consent_type_enum NOT NULL,
    terms_version VARCHAR(30),
    is_granted BOOLEAN NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1,
    CONSTRAINT uq_user_consent_type UNIQUE (user_id, type)
);
CREATE INDEX idx_user_consents_user_id ON user_consents (user_id);
CREATE TRIGGER set_timestamp_user_consents BEFORE UPDATE ON user_consents FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- Tokens revogados (JWT blacklist)
CREATE TABLE revoked_jwt_tokens (
    jti UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    expires_at TIMESTAMPTZ NOT NULL
);
CREATE INDEX idx_revoked_jwt_tokens_expires_at ON revoked_jwt_tokens (expires_at);