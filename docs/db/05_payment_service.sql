-- ========================================
-- PAYMENT SERVICE DATABASE
-- Responsável por: Pagamentos, Métodos de pagamento, Transações
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

-- ========================================
-- 3. ENUMS
-- ========================================
CREATE TYPE payment_status_enum AS ENUM (
    'PENDING',           -- Aguardando
    'PROCESSING',        -- Em processamento no gateway
    'AUTHORIZED',        -- Autorizado (pré-captura)
    'CAPTURED',          -- Capturado/Pago
    'FAILED',            -- Falhou
    'CANCELLED',         -- Cancelado
    'REFUNDED',          -- Reembolsado
    'PARTIALLY_REFUNDED',-- Parcialmente reembolsado
    'CHARGEBACK',        -- Estorno (contestação)
    'EXPIRED'            -- Expirado (boleto/pix não pago)
);

CREATE TYPE payment_method_type_enum AS ENUM (
    'CREDIT_CARD',
    'DEBIT_CARD',
    'PIX',
    'BOLETO',
    'WALLET',
    'BANK_TRANSFER'
);

CREATE TYPE card_brand_enum AS ENUM (
    'VISA',
    'MASTERCARD',
    'AMEX',
    'ELO',
    'HIPERCARD',
    'DINERS',
    'DISCOVER',
    'JCB',
    'OTHER'
);

CREATE TYPE transaction_type_enum AS ENUM (
    'AUTHORIZATION',
    'CAPTURE',
    'VOID',
    'REFUND',
    'CHARGEBACK'
);

-- ========================================
-- 4. TABLES
-- ========================================

-- Tabela de métodos de pagamento salvos pelo usuário
CREATE TABLE user_payment_methods (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Referência ao usuário (cross-service)
    user_id UUID NOT NULL,
    
    -- Identificação no gateway
    gateway_customer_id VARCHAR(100), -- ID do cliente no gateway (Stripe, Pagar.me, etc.)
    gateway_payment_method_id VARCHAR(100) NOT NULL,
    gateway_name VARCHAR(50) NOT NULL, -- 'STRIPE', 'PAGARME', 'MERCADOPAGO'
    
    -- Tipo
    method_type payment_method_type_enum NOT NULL,
    
    -- Dados do cartão (tokenizado)
    card_brand card_brand_enum,
    card_last_four VARCHAR(4),
    card_holder_name VARCHAR(150),
    card_expiration_month VARCHAR(2),
    card_expiration_year VARCHAR(4),
    
    -- Dados de carteira digital
    wallet_type VARCHAR(50), -- 'APPLE_PAY', 'GOOGLE_PAY', 'SAMSUNG_PAY'
    wallet_email VARCHAR(255),
    
    -- Controle
    is_default BOOLEAN DEFAULT FALSE,
    is_valid BOOLEAN DEFAULT TRUE,
    
    -- Controle de versão
    version INT NOT NULL DEFAULT 1,
    
    -- Timestamps
    last_used_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ,
    
    -- Constraints
    CONSTRAINT chk_card_data CHECK (
        method_type != 'CREDIT_CARD' AND method_type != 'DEBIT_CARD' 
        OR (card_last_four IS NOT NULL AND card_brand IS NOT NULL)
    )
);

-- Tabela principal de pagamentos
CREATE TABLE payments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Referências externas (cross-service)
    order_id UUID NOT NULL,
    user_id UUID NOT NULL,
    
    -- Idempotência
    idempotency_key VARCHAR(100) UNIQUE NOT NULL,
    
    -- Valores
    amount DECIMAL(10, 2) NOT NULL,
    currency VARCHAR(3) NOT NULL DEFAULT 'BRL',
    
    -- Taxas
    fee_amount DECIMAL(10, 2) DEFAULT 0, -- Taxa do gateway
    net_amount DECIMAL(10, 2), -- Valor líquido (amount - fee)
    
    -- Método de pagamento
    payment_method_type payment_method_type_enum NOT NULL,
    saved_payment_method_id UUID REFERENCES user_payment_methods(id),
    
    -- Dados do pagamento (snapshot)
    payment_method_snapshot JSONB, -- Dados do método usado
    
    -- Parcelamento
    installments INT DEFAULT 1,
    installment_amount DECIMAL(10, 2),
    
    -- Gateway
    gateway_name VARCHAR(50) NOT NULL,
    gateway_transaction_id VARCHAR(100),
    gateway_authorization_code VARCHAR(50),
    gateway_response JSONB, -- Resposta completa do gateway
    
    -- PIX específico
    pix_qr_code TEXT,
    pix_qr_code_url TEXT,
    pix_expiration_at TIMESTAMPTZ,
    
    -- Boleto específico
    boleto_url TEXT,
    boleto_barcode VARCHAR(50),
    boleto_expiration_at TIMESTAMPTZ,
    
    -- Status
    status payment_status_enum NOT NULL DEFAULT 'PENDING',
    
    -- Análise de fraude
    fraud_score DECIMAL(5, 2),
    fraud_analysis JSONB,
    
    -- Erros
    error_code VARCHAR(50),
    error_message TEXT,
    
    -- Controle de versão
    version INT NOT NULL DEFAULT 1,
    
    -- Timestamps de eventos
    authorized_at TIMESTAMPTZ,
    captured_at TIMESTAMPTZ,
    failed_at TIMESTAMPTZ,
    cancelled_at TIMESTAMPTZ,
    refunded_at TIMESTAMPTZ,
    expires_at TIMESTAMPTZ,
    
    -- Timestamps padrão
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Constraints
    CONSTRAINT chk_payments_amount CHECK (amount > 0),
    CONSTRAINT chk_payments_installments CHECK (installments >= 1 AND installments <= 24)
);

-- Tabela de transações (cada operação com o gateway)
CREATE TABLE payment_transactions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    payment_id UUID NOT NULL REFERENCES payments(id) ON DELETE CASCADE,
    
    -- Tipo de transação
    transaction_type transaction_type_enum NOT NULL,
    
    -- Valores
    amount DECIMAL(10, 2) NOT NULL,
    
    -- Gateway
    gateway_transaction_id VARCHAR(100),
    gateway_response JSONB,
    
    -- Status
    success BOOLEAN NOT NULL,
    error_code VARCHAR(50),
    error_message TEXT,
    
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Tabela de reembolsos
CREATE TABLE payment_refunds (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    payment_id UUID NOT NULL REFERENCES payments(id) ON DELETE CASCADE,
    
    -- Idempotência
    idempotency_key VARCHAR(100) UNIQUE NOT NULL,
    
    -- Valores
    amount DECIMAL(10, 2) NOT NULL,
    
    -- Motivo
    reason TEXT NOT NULL,
    
    -- Gateway
    gateway_refund_id VARCHAR(100),
    gateway_response JSONB,
    
    -- Status
    status VARCHAR(50) NOT NULL DEFAULT 'PENDING',
    
    -- Referência ao pedido
    order_refund_id UUID, -- Referência ao Order Service
    
    -- Timestamps
    processed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Constraints
    CONSTRAINT chk_refund_amount CHECK (amount > 0)
);

-- Tabela de chargebacks/contestações
CREATE TABLE payment_chargebacks (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    payment_id UUID NOT NULL REFERENCES payments(id) ON DELETE CASCADE,
    
    -- Dados do chargeback
    gateway_chargeback_id VARCHAR(100),
    reason_code VARCHAR(50),
    reason_description TEXT,
    
    -- Valores
    amount DECIMAL(10, 2) NOT NULL,
    
    -- Documentação para contestar
    evidence_submitted BOOLEAN DEFAULT FALSE,
    evidence_due_at TIMESTAMPTZ,
    
    -- Status
    status VARCHAR(50) NOT NULL DEFAULT 'OPEN',
    result VARCHAR(50), -- WON, LOST, PENDING
    
    -- Timestamps
    opened_at TIMESTAMPTZ NOT NULL,
    resolved_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Tabela de webhooks recebidos
CREATE TABLE payment_webhooks (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Origem
    gateway_name VARCHAR(50) NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    
    -- Payload
    payload JSONB NOT NULL,
    headers JSONB,
    
    -- Processamento
    processed BOOLEAN DEFAULT FALSE,
    processed_at TIMESTAMPTZ,
    error_message TEXT,
    
    -- Referência
    payment_id UUID REFERENCES payments(id),
    
    -- Timestamps
    received_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Outbox para eventos do Payment Service
CREATE TABLE payment_outbox_events (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    aggregate_type VARCHAR(100) NOT NULL,
    aggregate_id UUID NOT NULL,
    event_type VARCHAR(100) NOT NULL, -- PAYMENT_CREATED, PAYMENT_CAPTURED, PAYMENT_FAILED, etc.
    payload JSONB NOT NULL,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    processed_at TIMESTAMPTZ,
    error_message TEXT,
    retry_count INT DEFAULT 0
);

-- Inbox para idempotência
CREATE TABLE payment_inbox_messages (
    id UUID PRIMARY KEY,
    message_type VARCHAR(100) NOT NULL,
    processed_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Audit Log
CREATE TABLE payment_audit_logs (
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
CREATE TRIGGER trg_user_payment_methods_updated_at
    BEFORE UPDATE ON user_payment_methods
    FOR EACH ROW
    EXECUTE FUNCTION trigger_set_timestamp();

CREATE TRIGGER trg_user_payment_methods_version
    BEFORE UPDATE ON user_payment_methods
    FOR EACH ROW
    EXECUTE FUNCTION trigger_increment_version();

CREATE TRIGGER trg_payments_updated_at
    BEFORE UPDATE ON payments
    FOR EACH ROW
    EXECUTE FUNCTION trigger_set_timestamp();

CREATE TRIGGER trg_payments_version
    BEFORE UPDATE ON payments
    FOR EACH ROW
    EXECUTE FUNCTION trigger_increment_version();

-- ========================================
-- 6. INDEXES
-- ========================================

-- User Payment Methods
CREATE INDEX idx_user_payment_methods_user_id ON user_payment_methods(user_id) 
    WHERE deleted_at IS NULL;
CREATE UNIQUE INDEX uq_user_payment_methods_default ON user_payment_methods(user_id) 
    WHERE is_default = TRUE AND deleted_at IS NULL;
CREATE INDEX idx_user_payment_methods_gateway ON user_payment_methods(gateway_name, gateway_payment_method_id);

-- Payments
CREATE INDEX idx_payments_order_id ON payments(order_id);
CREATE INDEX idx_payments_user_id ON payments(user_id);
CREATE INDEX idx_payments_status ON payments(status);
CREATE INDEX idx_payments_created_at ON payments(created_at DESC);
CREATE INDEX idx_payments_gateway ON payments(gateway_name, gateway_transaction_id);
CREATE INDEX idx_payments_idempotency ON payments(idempotency_key);
CREATE INDEX idx_payments_pending ON payments(created_at) WHERE status = 'PENDING';
CREATE INDEX idx_payments_pix_expiring ON payments(pix_expiration_at) 
    WHERE payment_method_type = 'PIX' AND status = 'PENDING';
CREATE INDEX idx_payments_boleto_expiring ON payments(boleto_expiration_at) 
    WHERE payment_method_type = 'BOLETO' AND status = 'PENDING';

-- Payment Transactions
CREATE INDEX idx_payment_transactions_payment_id ON payment_transactions(payment_id);
CREATE INDEX idx_payment_transactions_created ON payment_transactions(created_at);

-- Payment Refunds
CREATE INDEX idx_payment_refunds_payment_id ON payment_refunds(payment_id);
CREATE INDEX idx_payment_refunds_status ON payment_refunds(status);
CREATE INDEX idx_payment_refunds_idempotency ON payment_refunds(idempotency_key);

-- Chargebacks
CREATE INDEX idx_payment_chargebacks_payment_id ON payment_chargebacks(payment_id);
CREATE INDEX idx_payment_chargebacks_status ON payment_chargebacks(status);
CREATE INDEX idx_payment_chargebacks_evidence_due ON payment_chargebacks(evidence_due_at) 
    WHERE status = 'OPEN';

-- Webhooks
CREATE INDEX idx_payment_webhooks_gateway ON payment_webhooks(gateway_name, event_type);
CREATE INDEX idx_payment_webhooks_unprocessed ON payment_webhooks(received_at) 
    WHERE processed = FALSE;
CREATE INDEX idx_payment_webhooks_payment_id ON payment_webhooks(payment_id) 
    WHERE payment_id IS NOT NULL;

-- Outbox
CREATE INDEX idx_payment_outbox_unprocessed ON payment_outbox_events(created_at) 
    WHERE processed_at IS NULL;
CREATE INDEX idx_payment_outbox_aggregate ON payment_outbox_events(aggregate_type, aggregate_id);

-- Audit
CREATE INDEX idx_payment_audit_entity ON payment_audit_logs(entity_type, entity_id);
CREATE INDEX idx_payment_audit_created ON payment_audit_logs(created_at);

-- ========================================
-- 7. VIEWS
-- ========================================

-- View de pagamentos pendentes de expiração
CREATE VIEW v_expiring_payments AS
SELECT 
    p.*,
    CASE 
        WHEN p.payment_method_type = 'PIX' THEN p.pix_expiration_at
        WHEN p.payment_method_type = 'BOLETO' THEN p.boleto_expiration_at
        ELSE p.expires_at
    END AS expiration_date,
    CASE 
        WHEN p.payment_method_type = 'PIX' THEN p.pix_expiration_at - NOW()
        WHEN p.payment_method_type = 'BOLETO' THEN p.boleto_expiration_at - NOW()
        ELSE p.expires_at - NOW()
    END AS time_until_expiration
FROM payments p
WHERE p.status = 'PENDING'
AND (
    (p.payment_method_type = 'PIX' AND p.pix_expiration_at IS NOT NULL)
    OR (p.payment_method_type = 'BOLETO' AND p.boleto_expiration_at IS NOT NULL)
    OR p.expires_at IS NOT NULL
);

-- View de métricas de pagamento
CREATE VIEW v_payment_metrics AS
SELECT 
    DATE_TRUNC('day', created_at) AS date,
    COUNT(*) AS total_payments,
    COUNT(*) FILTER (WHERE status = 'CAPTURED') AS successful_payments,
    COUNT(*) FILTER (WHERE status = 'FAILED') AS failed_payments,
    SUM(amount) FILTER (WHERE status = 'CAPTURED') AS total_captured,
    AVG(amount) FILTER (WHERE status = 'CAPTURED') AS avg_payment_amount,
    payment_method_type,
    gateway_name
FROM payments
GROUP BY DATE_TRUNC('day', created_at), payment_method_type, gateway_name;

-- ========================================
-- 8. COMMENTS
-- ========================================
COMMENT ON TABLE user_payment_methods IS 'Métodos de pagamento salvos pelos usuários (tokenizados)';
COMMENT ON TABLE payments IS 'Pagamentos processados';
COMMENT ON TABLE payment_transactions IS 'Cada operação individual com o gateway';
COMMENT ON TABLE payment_refunds IS 'Reembolsos processados';
COMMENT ON TABLE payment_chargebacks IS 'Contestações/estornos de cartão';
COMMENT ON TABLE payment_webhooks IS 'Webhooks recebidos dos gateways para auditoria';
COMMENT ON COLUMN payments.idempotency_key IS 'Chave única para garantir idempotência das requisições';
COMMENT ON COLUMN payments.fraud_score IS 'Score de fraude retornado pelo gateway ou sistema antifraude';
