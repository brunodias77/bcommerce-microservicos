-- ========================================
-- COUPON SERVICE DATABASE
-- Responsável por: Cupons, Regras, Uso de cupons
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

-- Função para validar uso do cupom
CREATE OR REPLACE FUNCTION validate_coupon_usage(
    p_coupon_id UUID,
    p_user_id UUID,
    p_cart_subtotal DECIMAL
)
RETURNS TABLE (
    is_valid BOOLEAN,
    error_code VARCHAR(50),
    error_message TEXT,
    discount_amount DECIMAL(10, 2)
) AS $$
DECLARE
    v_coupon RECORD;
    v_user_usage_count INT;
BEGIN
    -- Busca o cupom
    SELECT * INTO v_coupon FROM coupons WHERE id = p_coupon_id;
    
    -- Validações
    IF v_coupon IS NULL THEN
        RETURN QUERY SELECT FALSE, 'COUPON_NOT_FOUND'::VARCHAR(50), 'Cupom não encontrado'::TEXT, 0::DECIMAL(10,2);
        RETURN;
    END IF;
    
    IF v_coupon.status != 'ACTIVE' THEN
        RETURN QUERY SELECT FALSE, 'COUPON_INACTIVE'::VARCHAR(50), 'Cupom inativo'::TEXT, 0::DECIMAL(10,2);
        RETURN;
    END IF;
    
    IF NOW() < v_coupon.valid_from THEN
        RETURN QUERY SELECT FALSE, 'COUPON_NOT_STARTED'::VARCHAR(50), 'Cupom ainda não está válido'::TEXT, 0::DECIMAL(10,2);
        RETURN;
    END IF;
    
    IF NOW() > v_coupon.valid_until THEN
        RETURN QUERY SELECT FALSE, 'COUPON_EXPIRED'::VARCHAR(50), 'Cupom expirado'::TEXT, 0::DECIMAL(10,2);
        RETURN;
    END IF;
    
    IF v_coupon.max_uses IS NOT NULL AND v_coupon.current_uses >= v_coupon.max_uses THEN
        RETURN QUERY SELECT FALSE, 'COUPON_LIMIT_REACHED'::VARCHAR(50), 'Limite de uso do cupom atingido'::TEXT, 0::DECIMAL(10,2);
        RETURN;
    END IF;
    
    IF p_cart_subtotal < v_coupon.min_purchase_amount THEN
        RETURN QUERY SELECT FALSE, 'MIN_PURCHASE_NOT_MET'::VARCHAR(50), 
            FORMAT('Compra mínima de R$ %s não atingida', v_coupon.min_purchase_amount)::TEXT, 0::DECIMAL(10,2);
        RETURN;
    END IF;
    
    -- Verifica uso por usuário
    IF v_coupon.max_uses_per_user IS NOT NULL AND p_user_id IS NOT NULL THEN
        SELECT COUNT(*) INTO v_user_usage_count 
        FROM coupon_usages 
        WHERE coupon_id = p_coupon_id AND user_id = p_user_id;
        
        IF v_user_usage_count >= v_coupon.max_uses_per_user THEN
            RETURN QUERY SELECT FALSE, 'USER_LIMIT_REACHED'::VARCHAR(50), 
                'Você já utilizou este cupom o máximo de vezes permitido'::TEXT, 0::DECIMAL(10,2);
            RETURN;
        END IF;
    END IF;
    
    -- Calcula desconto
    IF v_coupon.discount_type = 'PERCENTAGE' THEN
        RETURN QUERY SELECT TRUE, NULL::VARCHAR(50), NULL::TEXT, 
            LEAST(p_cart_subtotal * v_coupon.discount_value / 100, COALESCE(v_coupon.max_discount_amount, p_cart_subtotal))::DECIMAL(10,2);
    ELSE
        RETURN QUERY SELECT TRUE, NULL::VARCHAR(50), NULL::TEXT, 
            LEAST(v_coupon.discount_value, p_cart_subtotal)::DECIMAL(10,2);
    END IF;
END;
$$ LANGUAGE plpgsql;

-- ========================================
-- 3. ENUMS
-- ========================================
CREATE TYPE coupon_type_enum AS ENUM (
    'PERCENTAGE',      -- Desconto percentual
    'FIXED_AMOUNT',    -- Valor fixo
    'FREE_SHIPPING',   -- Frete grátis
    'BUY_X_GET_Y'      -- Compre X leve Y
);

CREATE TYPE coupon_status_enum AS ENUM (
    'DRAFT',           -- Rascunho
    'SCHEDULED',       -- Agendado
    'ACTIVE',          -- Ativo
    'PAUSED',          -- Pausado
    'EXPIRED',         -- Expirado
    'DEPLETED'         -- Esgotado (limite de uso atingido)
);

CREATE TYPE coupon_scope_enum AS ENUM (
    'ALL',             -- Todos os produtos
    'CATEGORIES',      -- Categorias específicas
    'PRODUCTS',        -- Produtos específicos
    'FIRST_PURCHASE',  -- Primeira compra
    'SPECIFIC_USERS'   -- Usuários específicos
);

-- ========================================
-- 4. TABLES
-- ========================================

-- Tabela principal de cupons
CREATE TABLE coupons (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Identificação
    code CITEXT UNIQUE NOT NULL,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    
    -- Tipo e valor do desconto
    discount_type coupon_type_enum NOT NULL,
    discount_value DECIMAL(10, 2) NOT NULL,
    max_discount_amount DECIMAL(10, 2), -- Teto máximo para descontos percentuais
    
    -- Escopo de aplicação
    scope coupon_scope_enum NOT NULL DEFAULT 'ALL',
    
    -- Requisitos
    min_purchase_amount DECIMAL(10, 2) DEFAULT 0,
    min_items_quantity INT DEFAULT 1,
    
    -- Buy X Get Y específico
    buy_quantity INT, -- Compre X
    get_quantity INT, -- Leve Y
    
    -- Validade
    valid_from TIMESTAMPTZ NOT NULL,
    valid_until TIMESTAMPTZ NOT NULL,
    
    -- Limites de uso
    max_uses INT, -- Total de usos permitidos (NULL = ilimitado)
    current_uses INT NOT NULL DEFAULT 0,
    max_uses_per_user INT DEFAULT 1, -- Usos por usuário (NULL = ilimitado)
    
    -- Stackability (pode ser combinado com outros cupons)
    is_stackable BOOLEAN DEFAULT FALSE,
    
    -- Status
    status coupon_status_enum NOT NULL DEFAULT 'DRAFT',
    
    -- Metadados
    created_by UUID, -- Admin que criou
    metadata JSONB DEFAULT '{}',
    
    -- Controle de versão
    version INT NOT NULL DEFAULT 1,
    
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ,
    
    -- Constraints
    CONSTRAINT chk_coupon_code_format CHECK (char_length(code) >= 3 AND char_length(code) <= 50),
    CONSTRAINT chk_coupon_discount_value CHECK (discount_value > 0),
    CONSTRAINT chk_coupon_percentage CHECK (discount_type != 'PERCENTAGE' OR discount_value <= 100),
    CONSTRAINT chk_coupon_validity CHECK (valid_until > valid_from),
    CONSTRAINT chk_coupon_max_uses CHECK (max_uses IS NULL OR max_uses > 0),
    CONSTRAINT chk_coupon_buy_get CHECK (
        discount_type != 'BUY_X_GET_Y' 
        OR (buy_quantity IS NOT NULL AND get_quantity IS NOT NULL AND buy_quantity > 0 AND get_quantity > 0)
    )
);

-- Tabela de categorias elegíveis (quando scope = 'CATEGORIES')
CREATE TABLE coupon_eligible_categories (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    coupon_id UUID NOT NULL REFERENCES coupons(id) ON DELETE CASCADE,
    category_id UUID NOT NULL, -- Referência ao Catalog Service (sem FK)
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT uq_coupon_category UNIQUE (coupon_id, category_id)
);

-- Tabela de produtos elegíveis (quando scope = 'PRODUCTS')
CREATE TABLE coupon_eligible_products (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    coupon_id UUID NOT NULL REFERENCES coupons(id) ON DELETE CASCADE,
    product_id UUID NOT NULL, -- Referência ao Catalog Service (sem FK)
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT uq_coupon_product UNIQUE (coupon_id, product_id)
);

-- Tabela de usuários elegíveis (quando scope = 'SPECIFIC_USERS')
CREATE TABLE coupon_eligible_users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    coupon_id UUID NOT NULL REFERENCES coupons(id) ON DELETE CASCADE,
    user_id UUID NOT NULL, -- Referência ao User Service (sem FK)
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    CONSTRAINT uq_coupon_user UNIQUE (coupon_id, user_id)
);

-- Tabela de uso de cupons
CREATE TABLE coupon_usages (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    coupon_id UUID NOT NULL REFERENCES coupons(id) ON DELETE CASCADE,
    
    -- Referências externas (cross-service)
    user_id UUID, -- NULL para compras de convidados
    order_id UUID NOT NULL,
    
    -- Valores
    discount_amount DECIMAL(10, 2) NOT NULL,
    order_subtotal DECIMAL(10, 2) NOT NULL,
    
    -- Timestamps
    used_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Constraints
    CONSTRAINT chk_coupon_usage_discount CHECK (discount_amount >= 0)
);

-- Tabela de reservas de cupom (durante checkout)
CREATE TABLE coupon_reservations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    coupon_id UUID NOT NULL REFERENCES coupons(id) ON DELETE CASCADE,
    
    -- Referências
    cart_id UUID NOT NULL, -- Referência ao Cart Service (sem FK)
    user_id UUID,
    
    -- Valor calculado
    discount_amount DECIMAL(10, 2) NOT NULL,
    
    -- Expiração
    expires_at TIMESTAMPTZ NOT NULL,
    released_at TIMESTAMPTZ,
    converted_at TIMESTAMPTZ, -- Quando virou uso efetivo
    
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Constraints
    CONSTRAINT uq_coupon_reservation UNIQUE (coupon_id, cart_id)
);

-- Outbox para eventos do Coupon Service
CREATE TABLE coupon_outbox_events (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    aggregate_type VARCHAR(100) NOT NULL,
    aggregate_id UUID NOT NULL,
    event_type VARCHAR(100) NOT NULL, -- COUPON_CREATED, COUPON_USED, COUPON_EXPIRED, etc.
    payload JSONB NOT NULL,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    processed_at TIMESTAMPTZ,
    error_message TEXT,
    retry_count INT DEFAULT 0
);

-- Inbox para idempotência
CREATE TABLE coupon_inbox_messages (
    id UUID PRIMARY KEY,
    message_type VARCHAR(100) NOT NULL,
    processed_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Audit Log
CREATE TABLE coupon_audit_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    entity_type VARCHAR(100) NOT NULL,
    entity_id UUID NOT NULL,
    action VARCHAR(50) NOT NULL,
    
    old_values JSONB,
    new_values JSONB,
    
    user_id UUID,
    ip_address VARCHAR(45),
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ========================================
-- 5. TRIGGERS
-- ========================================
CREATE TRIGGER trg_coupons_updated_at
    BEFORE UPDATE ON coupons
    FOR EACH ROW
    EXECUTE FUNCTION trigger_set_timestamp();

CREATE TRIGGER trg_coupons_version
    BEFORE UPDATE ON coupons
    FOR EACH ROW
    EXECUTE FUNCTION trigger_increment_version();

-- Trigger para incrementar current_uses
CREATE OR REPLACE FUNCTION trigger_increment_coupon_usage()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE coupons 
    SET current_uses = current_uses + 1 
    WHERE id = NEW.coupon_id;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_coupon_usage_increment
    AFTER INSERT ON coupon_usages
    FOR EACH ROW
    EXECUTE FUNCTION trigger_increment_coupon_usage();

-- Trigger para atualizar status quando limite é atingido
CREATE OR REPLACE FUNCTION trigger_check_coupon_depleted()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.max_uses IS NOT NULL AND NEW.current_uses >= NEW.max_uses THEN
        NEW.status := 'DEPLETED';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_coupon_check_depleted
    BEFORE UPDATE ON coupons
    FOR EACH ROW
    EXECUTE FUNCTION trigger_check_coupon_depleted();

-- ========================================
-- 6. INDEXES
-- ========================================

-- Coupons
CREATE INDEX idx_coupons_code ON coupons(code) WHERE deleted_at IS NULL;
CREATE INDEX idx_coupons_status ON coupons(status) WHERE deleted_at IS NULL;
CREATE INDEX idx_coupons_valid_period ON coupons(valid_from, valid_until) WHERE deleted_at IS NULL;
CREATE INDEX idx_coupons_active ON coupons(status) 
    WHERE status = 'ACTIVE' AND deleted_at IS NULL;
CREATE INDEX idx_coupons_expiring ON coupons(valid_until) 
    WHERE status = 'ACTIVE' AND deleted_at IS NULL;

-- Eligible Categories
CREATE INDEX idx_coupon_categories_coupon_id ON coupon_eligible_categories(coupon_id);
CREATE INDEX idx_coupon_categories_category_id ON coupon_eligible_categories(category_id);

-- Eligible Products
CREATE INDEX idx_coupon_products_coupon_id ON coupon_eligible_products(coupon_id);
CREATE INDEX idx_coupon_products_product_id ON coupon_eligible_products(product_id);

-- Eligible Users
CREATE INDEX idx_coupon_users_coupon_id ON coupon_eligible_users(coupon_id);
CREATE INDEX idx_coupon_users_user_id ON coupon_eligible_users(user_id);

-- Usages
CREATE INDEX idx_coupon_usages_coupon_id ON coupon_usages(coupon_id);
CREATE INDEX idx_coupon_usages_user_id ON coupon_usages(user_id) WHERE user_id IS NOT NULL;
CREATE INDEX idx_coupon_usages_order_id ON coupon_usages(order_id);
CREATE INDEX idx_coupon_usages_used_at ON coupon_usages(used_at);

-- Reservations
CREATE INDEX idx_coupon_reservations_coupon_id ON coupon_reservations(coupon_id) 
    WHERE released_at IS NULL AND converted_at IS NULL;
CREATE INDEX idx_coupon_reservations_cart_id ON coupon_reservations(cart_id);
CREATE INDEX idx_coupon_reservations_expires ON coupon_reservations(expires_at) 
    WHERE released_at IS NULL AND converted_at IS NULL;

-- Outbox
CREATE INDEX idx_coupon_outbox_unprocessed ON coupon_outbox_events(created_at) 
    WHERE processed_at IS NULL;
CREATE INDEX idx_coupon_outbox_aggregate ON coupon_outbox_events(aggregate_type, aggregate_id);

-- Audit
CREATE INDEX idx_coupon_audit_entity ON coupon_audit_logs(entity_type, entity_id);
CREATE INDEX idx_coupon_audit_created ON coupon_audit_logs(created_at);

-- ========================================
-- 7. VIEWS
-- ========================================

-- View de cupons ativos e válidos
CREATE VIEW v_active_coupons AS
SELECT 
    c.*,
    c.max_uses - c.current_uses AS remaining_uses,
    CASE 
        WHEN c.max_uses IS NOT NULL THEN 
            ROUND((c.current_uses::DECIMAL / c.max_uses) * 100, 2)
        ELSE NULL
    END AS usage_percentage
FROM coupons c
WHERE c.status = 'ACTIVE'
AND c.deleted_at IS NULL
AND NOW() BETWEEN c.valid_from AND c.valid_until
AND (c.max_uses IS NULL OR c.current_uses < c.max_uses);

-- View de métricas de cupons
CREATE VIEW v_coupon_metrics AS
SELECT 
    c.id,
    c.code,
    c.name,
    c.discount_type,
    c.discount_value,
    c.status,
    c.current_uses,
    c.max_uses,
    COALESCE(SUM(cu.discount_amount), 0) AS total_discount_given,
    COALESCE(SUM(cu.order_subtotal), 0) AS total_order_value,
    COUNT(DISTINCT cu.user_id) AS unique_users,
    AVG(cu.discount_amount) AS avg_discount_per_use,
    MIN(cu.used_at) AS first_used_at,
    MAX(cu.used_at) AS last_used_at
FROM coupons c
LEFT JOIN coupon_usages cu ON cu.coupon_id = c.id
WHERE c.deleted_at IS NULL
GROUP BY c.id, c.code, c.name, c.discount_type, c.discount_value, c.status, c.current_uses, c.max_uses;

-- View de cupons próximos de expirar
CREATE VIEW v_expiring_coupons AS
SELECT 
    c.*,
    c.valid_until - NOW() AS time_until_expiration
FROM coupons c
WHERE c.status = 'ACTIVE'
AND c.deleted_at IS NULL
AND c.valid_until BETWEEN NOW() AND NOW() + INTERVAL '7 days';

-- ========================================
-- 8. SCHEDULED JOBS (pseudo-código para reference)
-- ========================================
-- Job 1: Atualizar status de cupons expirados
-- UPDATE coupons SET status = 'EXPIRED' 
-- WHERE status = 'ACTIVE' AND valid_until < NOW();

-- Job 2: Ativar cupons agendados
-- UPDATE coupons SET status = 'ACTIVE' 
-- WHERE status = 'SCHEDULED' AND valid_from <= NOW() AND valid_until > NOW();

-- Job 3: Liberar reservas expiradas
-- UPDATE coupon_reservations SET released_at = NOW() 
-- WHERE expires_at < NOW() AND released_at IS NULL AND converted_at IS NULL;

-- ========================================
-- 9. COMMENTS
-- ========================================
COMMENT ON TABLE coupons IS 'Cupons de desconto';
COMMENT ON TABLE coupon_eligible_categories IS 'Categorias onde o cupom pode ser aplicado';
COMMENT ON TABLE coupon_eligible_products IS 'Produtos específicos onde o cupom pode ser aplicado';
COMMENT ON TABLE coupon_eligible_users IS 'Usuários que podem usar o cupom (cupons exclusivos)';
COMMENT ON TABLE coupon_usages IS 'Registro de uso dos cupons';
COMMENT ON TABLE coupon_reservations IS 'Reservas temporárias durante o checkout';
COMMENT ON COLUMN coupons.is_stackable IS 'Indica se pode ser combinado com outros cupons';
COMMENT ON COLUMN coupons.scope IS 'Define onde o cupom pode ser aplicado';
COMMENT ON FUNCTION validate_coupon_usage IS 'Valida se um cupom pode ser usado e retorna o desconto calculado';
