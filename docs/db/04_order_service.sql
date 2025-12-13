-- ========================================
-- ORDER SERVICE DATABASE
-- Responsável por: Pedidos, Itens do pedido, Histórico de status
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

-- Função para gerar número do pedido
CREATE OR REPLACE FUNCTION generate_order_number()
RETURNS TEXT AS $$
DECLARE
    v_year TEXT;
    v_sequence INT;
    v_number TEXT;
BEGIN
    v_year := TO_CHAR(NOW(), 'YY');
    
    -- Pega próximo valor da sequence
    SELECT nextval('order_number_seq') INTO v_sequence;
    
    -- Formato: YY-NNNNNN (ex: 25-000001)
    v_number := v_year || '-' || LPAD(v_sequence::TEXT, 6, '0');
    
    RETURN v_number;
END;
$$ LANGUAGE plpgsql;

-- Sequence para número do pedido
CREATE SEQUENCE IF NOT EXISTS order_number_seq START 1;

-- ========================================
-- 3. ENUMS
-- ========================================
CREATE TYPE order_status_enum AS ENUM (
    'PENDING',           -- Aguardando pagamento
    'PAYMENT_PROCESSING',-- Pagamento em processamento
    'PAID',              -- Pago
    'PREPARING',         -- Em preparação
    'SHIPPED',           -- Enviado
    'OUT_FOR_DELIVERY',  -- Saiu para entrega
    'DELIVERED',         -- Entregue
    'CANCELLED',         -- Cancelado
    'REFUNDED',          -- Reembolsado
    'FAILED'             -- Falhou
);

CREATE TYPE payment_method_enum AS ENUM (
    'CREDIT_CARD',
    'DEBIT_CARD',
    'PIX',
    'BOLETO',
    'WALLET'
);

CREATE TYPE shipping_method_enum AS ENUM (
    'STANDARD',
    'EXPRESS',
    'SAME_DAY',
    'PICKUP'
);

CREATE TYPE cancellation_reason_enum AS ENUM (
    'CUSTOMER_REQUEST',
    'PAYMENT_FAILED',
    'OUT_OF_STOCK',
    'FRAUD_SUSPECTED',
    'SHIPPING_ISSUE',
    'OTHER'
);

-- ========================================
-- 4. TABLES
-- ========================================

-- Tabela principal de pedidos
CREATE TABLE orders (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Identificação
    order_number VARCHAR(20) UNIQUE NOT NULL DEFAULT generate_order_number(),
    
    -- Referências externas (cross-service)
    user_id UUID NOT NULL,
    cart_id UUID, -- Referência ao carrinho que originou o pedido
    coupon_id UUID,
    
    -- Valores
    subtotal DECIMAL(10, 2) NOT NULL,
    discount_amount DECIMAL(10, 2) NOT NULL DEFAULT 0,
    shipping_amount DECIMAL(10, 2) NOT NULL DEFAULT 0,
    tax_amount DECIMAL(10, 2) NOT NULL DEFAULT 0,
    total DECIMAL(10, 2) NOT NULL,
    
    -- Informações do cupom (snapshot)
    coupon_snapshot JSONB, -- { "code": "SAVE10", "type": "PERCENTAGE", "value": 10 }
    
    -- Status
    status order_status_enum NOT NULL DEFAULT 'PENDING',
    
    -- Endereço de entrega (snapshot)
    shipping_address JSONB NOT NULL,
    billing_address JSONB,
    
    -- Entrega
    shipping_method shipping_method_enum NOT NULL DEFAULT 'STANDARD',
    shipping_carrier VARCHAR(100),
    tracking_code VARCHAR(100),
    tracking_url TEXT,
    estimated_delivery_at TIMESTAMPTZ,
    
    -- Pagamento
    payment_method payment_method_enum NOT NULL,
    
    -- Cancelamento
    cancellation_reason cancellation_reason_enum,
    cancellation_notes TEXT,
    cancelled_by UUID, -- User ID que cancelou
    
    -- Notas
    customer_notes TEXT,
    internal_notes TEXT,
    
    -- Controle de versão
    version INT NOT NULL DEFAULT 1,
    
    -- Timestamps de eventos
    paid_at TIMESTAMPTZ,
    shipped_at TIMESTAMPTZ,
    delivered_at TIMESTAMPTZ,
    cancelled_at TIMESTAMPTZ,
    refunded_at TIMESTAMPTZ,
    
    -- Timestamps padrão
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Constraints
    CONSTRAINT chk_orders_total CHECK (total >= 0),
    CONSTRAINT chk_orders_subtotal CHECK (subtotal >= 0),
    CONSTRAINT chk_orders_discount CHECK (discount_amount >= 0 AND discount_amount <= subtotal)
);

-- Tabela de itens do pedido
CREATE TABLE order_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    
    -- Referência ao produto (cross-service)
    product_id UUID NOT NULL,
    
    -- Snapshot completo do produto
    product_snapshot JSONB NOT NULL, -- { "name", "sku", "image_url", "category", "attributes" }
    
    -- Valores
    unit_price DECIMAL(10, 2) NOT NULL,
    quantity INT NOT NULL CHECK (quantity > 0),
    discount_amount DECIMAL(10, 2) NOT NULL DEFAULT 0,
    subtotal DECIMAL(10, 2) NOT NULL,
    
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Constraints
    CONSTRAINT chk_order_items_price CHECK (unit_price >= 0),
    CONSTRAINT chk_order_items_subtotal CHECK (subtotal >= 0)
);

-- Tabela de histórico de status do pedido
CREATE TABLE order_status_history (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    
    -- Status
    from_status order_status_enum,
    to_status order_status_enum NOT NULL,
    
    -- Metadados
    reason TEXT,
    changed_by UUID, -- User ID que fez a mudança (NULL para sistema)
    metadata JSONB,
    
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Tabela de eventos de rastreamento
CREATE TABLE order_tracking_events (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    
    -- Evento
    event_code VARCHAR(50) NOT NULL,
    event_description TEXT NOT NULL,
    
    -- Localização
    location VARCHAR(200),
    city VARCHAR(100),
    state VARCHAR(2),
    
    -- Timestamps
    occurred_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Tabela de notas fiscais
CREATE TABLE order_invoices (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    
    -- Dados da NF
    invoice_number VARCHAR(50) NOT NULL,
    invoice_key VARCHAR(50), -- Chave da NF-e
    invoice_series VARCHAR(10),
    
    -- URLs
    pdf_url TEXT,
    xml_url TEXT,
    
    -- Timestamps
    issued_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Tabela de reembolsos
CREATE TABLE order_refunds (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    
    -- Valores
    amount DECIMAL(10, 2) NOT NULL,
    
    -- Detalhes
    reason TEXT NOT NULL,
    refund_method VARCHAR(50), -- Mesmo método de pagamento, crédito em loja, etc.
    
    -- Referência ao pagamento
    payment_id UUID, -- Referência ao Payment Service
    gateway_refund_id VARCHAR(100),
    
    -- Status
    status VARCHAR(50) NOT NULL DEFAULT 'PENDING',
    
    -- Timestamps
    processed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Constraints
    CONSTRAINT chk_refund_amount CHECK (amount > 0)
);

-- Outbox para eventos do Order Service
CREATE TABLE order_outbox_events (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    aggregate_type VARCHAR(100) NOT NULL,
    aggregate_id UUID NOT NULL,
    event_type VARCHAR(100) NOT NULL, -- ORDER_CREATED, ORDER_PAID, ORDER_SHIPPED, etc.
    payload JSONB NOT NULL,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    processed_at TIMESTAMPTZ,
    error_message TEXT,
    retry_count INT DEFAULT 0
);

-- Inbox para idempotência
CREATE TABLE order_inbox_messages (
    id UUID PRIMARY KEY,
    message_type VARCHAR(100) NOT NULL,
    processed_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Audit Log
CREATE TABLE order_audit_logs (
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
CREATE TRIGGER trg_orders_updated_at
    BEFORE UPDATE ON orders
    FOR EACH ROW
    EXECUTE FUNCTION trigger_set_timestamp();

CREATE TRIGGER trg_orders_version
    BEFORE UPDATE ON orders
    FOR EACH ROW
    EXECUTE FUNCTION trigger_increment_version();

-- Trigger para criar histórico de status automaticamente
CREATE OR REPLACE FUNCTION trigger_order_status_history()
RETURNS TRIGGER AS $$
BEGIN
    IF OLD.status IS DISTINCT FROM NEW.status THEN
        INSERT INTO order_status_history (order_id, from_status, to_status)
        VALUES (NEW.id, OLD.status, NEW.status);
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_orders_status_history
    AFTER UPDATE ON orders
    FOR EACH ROW
    EXECUTE FUNCTION trigger_order_status_history();

-- ========================================
-- 6. INDEXES
-- ========================================

-- Orders
CREATE INDEX idx_orders_user_id ON orders(user_id);
CREATE INDEX idx_orders_status ON orders(status);
CREATE INDEX idx_orders_created_at ON orders(created_at DESC);
CREATE INDEX idx_orders_order_number ON orders(order_number);
CREATE INDEX idx_orders_tracking_code ON orders(tracking_code) WHERE tracking_code IS NOT NULL;
CREATE INDEX idx_orders_coupon_id ON orders(coupon_id) WHERE coupon_id IS NOT NULL;
CREATE INDEX idx_orders_paid_at ON orders(paid_at) WHERE paid_at IS NOT NULL;
CREATE INDEX idx_orders_user_status ON orders(user_id, status);
CREATE INDEX idx_orders_pending ON orders(created_at) WHERE status = 'PENDING';

-- Order Items
CREATE INDEX idx_order_items_order_id ON order_items(order_id);
CREATE INDEX idx_order_items_product_id ON order_items(product_id);

-- Status History
CREATE INDEX idx_order_status_history_order_id ON order_status_history(order_id);
CREATE INDEX idx_order_status_history_created ON order_status_history(created_at);

-- Tracking Events
CREATE INDEX idx_tracking_events_order_id ON order_tracking_events(order_id);
CREATE INDEX idx_tracking_events_occurred ON order_tracking_events(occurred_at);

-- Invoices
CREATE INDEX idx_order_invoices_order_id ON order_invoices(order_id);
CREATE UNIQUE INDEX uq_order_invoices_number ON order_invoices(invoice_number);

-- Refunds
CREATE INDEX idx_order_refunds_order_id ON order_refunds(order_id);
CREATE INDEX idx_order_refunds_status ON order_refunds(status);

-- Outbox
CREATE INDEX idx_order_outbox_unprocessed ON order_outbox_events(created_at) 
    WHERE processed_at IS NULL;
CREATE INDEX idx_order_outbox_aggregate ON order_outbox_events(aggregate_type, aggregate_id);

-- Audit
CREATE INDEX idx_order_audit_entity ON order_audit_logs(entity_type, entity_id);
CREATE INDEX idx_order_audit_created ON order_audit_logs(created_at);

-- ========================================
-- 7. VIEWS
-- ========================================

-- View de resumo de pedidos por usuário
CREATE VIEW v_user_order_summary AS
SELECT 
    user_id,
    COUNT(*) AS total_orders,
    COUNT(*) FILTER (WHERE status = 'DELIVERED') AS completed_orders,
    COUNT(*) FILTER (WHERE status = 'CANCELLED') AS cancelled_orders,
    SUM(total) AS total_spent,
    AVG(total) AS avg_order_value,
    MAX(created_at) AS last_order_at
FROM orders
GROUP BY user_id;

-- View de pedidos pendentes de ação
CREATE VIEW v_orders_pending_action AS
SELECT 
    o.*,
    CASE 
        WHEN o.status = 'PENDING' AND o.created_at < NOW() - INTERVAL '30 minutes' 
            THEN 'PAYMENT_TIMEOUT_RISK'
        WHEN o.status = 'PAID' AND o.created_at < NOW() - INTERVAL '2 hours' 
            THEN 'NEEDS_PREPARATION'
        WHEN o.status = 'SHIPPED' AND o.shipped_at < NOW() - INTERVAL '7 days' 
            THEN 'DELIVERY_DELAYED'
        ELSE 'OK'
    END AS alert_status
FROM orders o
WHERE o.status NOT IN ('DELIVERED', 'CANCELLED', 'REFUNDED');

-- ========================================
-- 8. COMMENTS
-- ========================================
COMMENT ON TABLE orders IS 'Pedidos dos clientes';
COMMENT ON TABLE order_items IS 'Itens do pedido com snapshot do produto';
COMMENT ON TABLE order_status_history IS 'Histórico completo de mudanças de status';
COMMENT ON TABLE order_tracking_events IS 'Eventos de rastreamento da transportadora';
COMMENT ON TABLE order_invoices IS 'Notas fiscais emitidas';
COMMENT ON TABLE order_refunds IS 'Reembolsos processados';
COMMENT ON COLUMN orders.shipping_address IS 'Snapshot do endereço no momento do pedido';
COMMENT ON COLUMN orders.coupon_snapshot IS 'Snapshot do cupom aplicado para auditoria';
COMMENT ON VIEW v_orders_pending_action IS 'View para dashboard operacional de pedidos que precisam de atenção';
