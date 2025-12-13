-- ========================================
-- CART SERVICE DATABASE
-- Responsável por: Carrinho de compras, Itens do carrinho
-- ========================================

-- ========================================
-- 1. EXTENSIONS
-- ========================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

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

-- Função para calcular totais do carrinho
CREATE OR REPLACE FUNCTION calculate_cart_totals(p_cart_id UUID)
RETURNS TABLE (
    item_count INT,
    subtotal DECIMAL(10, 2),
    total_items INT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        COUNT(*)::INT AS item_count,
        COALESCE(SUM(ci.unit_price * ci.quantity), 0)::DECIMAL(10, 2) AS subtotal,
        COALESCE(SUM(ci.quantity), 0)::INT AS total_items
    FROM cart_items ci
    WHERE ci.cart_id = p_cart_id
    AND ci.removed_at IS NULL;
END;
$$ LANGUAGE plpgsql;

-- ========================================
-- 3. ENUMS
-- ========================================
CREATE TYPE cart_status_enum AS ENUM ('ACTIVE', 'MERGED', 'CONVERTED', 'ABANDONED', 'EXPIRED');

-- ========================================
-- 4. TABLES
-- ========================================

-- Tabela principal de carrinhos
CREATE TABLE carts (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Identificação do dono
    user_id UUID, -- NULL para carrinhos anônimos
    session_id VARCHAR(100), -- Para carrinhos de usuários não logados
    
    -- Cupom aplicado
    coupon_id UUID, -- Referência ao Coupon Service (sem FK)
    coupon_code VARCHAR(50),
    discount_amount DECIMAL(10, 2) DEFAULT 0,
    
    -- Status
    status cart_status_enum NOT NULL DEFAULT 'ACTIVE',
    
    -- Metadados
    ip_address VARCHAR(45),
    user_agent TEXT,
    
    -- Controle de versão
    version INT NOT NULL DEFAULT 1,
    
    -- Timestamps
    expires_at TIMESTAMPTZ, -- Para limpeza de carrinhos abandonados
    converted_at TIMESTAMPTZ, -- Quando virou pedido
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Constraints
    CONSTRAINT chk_cart_owner CHECK (user_id IS NOT NULL OR session_id IS NOT NULL)
);

-- Tabela de itens do carrinho
CREATE TABLE cart_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    cart_id UUID NOT NULL REFERENCES carts(id) ON DELETE CASCADE,
    
    -- Referência ao produto (cross-service)
    product_id UUID NOT NULL,
    
    -- Snapshot do produto no momento da adição
    product_snapshot JSONB NOT NULL, -- { "name": "...", "sku": "...", "image_url": "..." }
    
    -- Quantidade e preço
    quantity INT NOT NULL CHECK (quantity > 0),
    unit_price DECIMAL(10, 2) NOT NULL, -- Preço no momento da adição
    
    -- Preço atual (para comparação/aviso de mudança)
    current_price DECIMAL(10, 2),
    price_changed_at TIMESTAMPTZ,
    
    -- Reserva de estoque
    stock_reserved BOOLEAN DEFAULT FALSE,
    stock_reservation_id UUID, -- Referência à reserva no Catalog Service
    
    -- Timestamps
    added_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    removed_at TIMESTAMPTZ, -- Soft delete para histórico
    
    -- Constraints
    CONSTRAINT chk_cart_item_price CHECK (unit_price >= 0),
    CONSTRAINT uq_cart_item_product UNIQUE (cart_id, product_id)
);

-- Tabela de histórico de ações do carrinho
CREATE TABLE cart_activity_log (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    cart_id UUID NOT NULL REFERENCES carts(id) ON DELETE CASCADE,
    
    -- Ação realizada
    action VARCHAR(50) NOT NULL, -- ADD_ITEM, REMOVE_ITEM, UPDATE_QUANTITY, APPLY_COUPON, etc.
    
    -- Detalhes da ação
    product_id UUID,
    quantity_before INT,
    quantity_after INT,
    metadata JSONB,
    
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Tabela para carrinhos salvos/wishlist convertida
CREATE TABLE saved_carts (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL,
    
    name VARCHAR(100) NOT NULL DEFAULT 'Meu Carrinho Salvo',
    items JSONB NOT NULL, -- Array de itens salvos
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Outbox para eventos do Cart Service
CREATE TABLE cart_outbox_events (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    aggregate_type VARCHAR(100) NOT NULL,
    aggregate_id UUID NOT NULL,
    event_type VARCHAR(100) NOT NULL, -- CART_CREATED, ITEM_ADDED, CART_ABANDONED, CART_CONVERTED, etc.
    payload JSONB NOT NULL,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    processed_at TIMESTAMPTZ,
    error_message TEXT,
    retry_count INT DEFAULT 0
);

-- Inbox para idempotência
CREATE TABLE cart_inbox_messages (
    id UUID PRIMARY KEY,
    message_type VARCHAR(100) NOT NULL,
    processed_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ========================================
-- 5. TRIGGERS
-- ========================================
CREATE TRIGGER trg_carts_updated_at
    BEFORE UPDATE ON carts
    FOR EACH ROW
    EXECUTE FUNCTION trigger_set_timestamp();

CREATE TRIGGER trg_carts_version
    BEFORE UPDATE ON carts
    FOR EACH ROW
    EXECUTE FUNCTION trigger_increment_version();

CREATE TRIGGER trg_cart_items_updated_at
    BEFORE UPDATE ON cart_items
    FOR EACH ROW
    EXECUTE FUNCTION trigger_set_timestamp();

CREATE TRIGGER trg_saved_carts_updated_at
    BEFORE UPDATE ON saved_carts
    FOR EACH ROW
    EXECUTE FUNCTION trigger_set_timestamp();

-- ========================================
-- 6. INDEXES
-- ========================================

-- Carts
CREATE UNIQUE INDEX uq_carts_user_active ON carts(user_id) 
    WHERE user_id IS NOT NULL AND status = 'ACTIVE';
CREATE UNIQUE INDEX uq_carts_session_active ON carts(session_id) 
    WHERE session_id IS NOT NULL AND status = 'ACTIVE';
CREATE INDEX idx_carts_user_id ON carts(user_id) WHERE user_id IS NOT NULL;
CREATE INDEX idx_carts_session_id ON carts(session_id) WHERE session_id IS NOT NULL;
CREATE INDEX idx_carts_status ON carts(status);
CREATE INDEX idx_carts_expires_at ON carts(expires_at) WHERE status = 'ACTIVE';
CREATE INDEX idx_carts_abandoned ON carts(updated_at) WHERE status = 'ACTIVE';
CREATE INDEX idx_carts_coupon_id ON carts(coupon_id) WHERE coupon_id IS NOT NULL;

-- Cart Items
CREATE INDEX idx_cart_items_cart_id ON cart_items(cart_id) WHERE removed_at IS NULL;
CREATE INDEX idx_cart_items_product_id ON cart_items(product_id);
CREATE INDEX idx_cart_items_added_at ON cart_items(added_at);
CREATE INDEX idx_cart_items_price_changed ON cart_items(cart_id) 
    WHERE price_changed_at IS NOT NULL AND removed_at IS NULL;

-- Cart Activity Log
CREATE INDEX idx_cart_activity_cart_id ON cart_activity_log(cart_id);
CREATE INDEX idx_cart_activity_created_at ON cart_activity_log(created_at);
CREATE INDEX idx_cart_activity_action ON cart_activity_log(action);

-- Saved Carts
CREATE INDEX idx_saved_carts_user_id ON saved_carts(user_id);

-- Outbox
CREATE INDEX idx_cart_outbox_unprocessed ON cart_outbox_events(created_at) 
    WHERE processed_at IS NULL;
CREATE INDEX idx_cart_outbox_aggregate ON cart_outbox_events(aggregate_type, aggregate_id);

-- ========================================
-- 7. VIEWS
-- ========================================

-- View para carrinhos ativos com totais
CREATE VIEW v_active_carts AS
SELECT 
    c.id,
    c.user_id,
    c.session_id,
    c.coupon_code,
    c.discount_amount,
    c.status,
    c.created_at,
    c.updated_at,
    (SELECT COUNT(*) FROM cart_items ci WHERE ci.cart_id = c.id AND ci.removed_at IS NULL) AS item_count,
    (SELECT COALESCE(SUM(ci.quantity), 0) FROM cart_items ci WHERE ci.cart_id = c.id AND ci.removed_at IS NULL) AS total_items,
    (SELECT COALESCE(SUM(ci.unit_price * ci.quantity), 0) FROM cart_items ci WHERE ci.cart_id = c.id AND ci.removed_at IS NULL) AS subtotal
FROM carts c
WHERE c.status = 'ACTIVE';

-- View para detecção de carrinhos abandonados
CREATE VIEW v_abandoned_carts AS
SELECT 
    c.id,
    c.user_id,
    c.session_id,
    c.updated_at,
    NOW() - c.updated_at AS time_since_update,
    (SELECT COUNT(*) FROM cart_items ci WHERE ci.cart_id = c.id AND ci.removed_at IS NULL) AS item_count,
    (SELECT COALESCE(SUM(ci.unit_price * ci.quantity), 0) FROM cart_items ci WHERE ci.cart_id = c.id AND ci.removed_at IS NULL) AS cart_value
FROM carts c
WHERE c.status = 'ACTIVE'
AND c.updated_at < NOW() - INTERVAL '1 hour';

-- ========================================
-- 8. COMMENTS
-- ========================================
COMMENT ON TABLE carts IS 'Carrinhos de compras (logados e anônimos)';
COMMENT ON TABLE cart_items IS 'Itens do carrinho com snapshot do produto';
COMMENT ON TABLE cart_activity_log IS 'Log de atividades para analytics e debugging';
COMMENT ON TABLE saved_carts IS 'Carrinhos salvos pelo usuário para compra futura';
COMMENT ON COLUMN cart_items.product_snapshot IS 'Snapshot do produto no momento da adição para exibição offline';
COMMENT ON COLUMN cart_items.current_price IS 'Preço atual do produto para detectar mudanças';
COMMENT ON VIEW v_abandoned_carts IS 'View para identificar carrinhos abandonados para remarketing';
