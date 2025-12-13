-- ========================================
-- SHARED INFRASTRUCTURE
-- Tabelas e funções compartilhadas entre serviços
-- ========================================

-- ========================================
-- 1. EXTENSIONS
-- ========================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "citext";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- ========================================
-- 2. FUNÇÕES COMPARTILHADAS
-- ========================================

-- Função para atualizar updated_at automaticamente
CREATE OR REPLACE FUNCTION trigger_set_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Função para incrementar version (optimistic locking)
CREATE OR REPLACE FUNCTION trigger_increment_version()
RETURNS TRIGGER AS $$
BEGIN
    NEW.version = OLD.version + 1;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ========================================
-- 3. OUTBOX PATTERN (para cada serviço)
-- ========================================
-- Template: Cada serviço terá sua própria tabela outbox
-- CREATE TABLE {service}_outbox_events (
--     id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
--     aggregate_type VARCHAR(100) NOT NULL,
--     aggregate_id UUID NOT NULL,
--     event_type VARCHAR(100) NOT NULL,
--     payload JSONB NOT NULL,
--     created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
--     processed_at TIMESTAMPTZ,
--     error_message TEXT,
--     retry_count INT DEFAULT 0
-- );

-- ========================================
-- 4. INBOX PATTERN (para idempotência)
-- ========================================
-- Template: Cada serviço terá sua própria tabela inbox
-- CREATE TABLE {service}_inbox_messages (
--     id UUID PRIMARY KEY,
--     message_type VARCHAR(100) NOT NULL,
--     processed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
--     CONSTRAINT uq_{service}_inbox_id UNIQUE (id)
-- );
