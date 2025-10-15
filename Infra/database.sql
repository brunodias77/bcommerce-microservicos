-- =====================================================================
-- REFATORAÇÃO PARA ARQUITETURA DE MICROSSERVIÇOS - E-COMMERCE
-- Versão: 3.0 - Microsserviços
-- Data: 12 de Setembro de 2025
-- Descrição: Divisão do monólito em microsserviços por domínio
-- =====================================================================

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
    phone VARCHAR(20),
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
    token_value VARCHAR(256) NOT NULL UNIQUE,
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
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

-- =====================================================================
-- MICROSSERVIÇO 2: CATALOG SERVICE
-- Responsabilidades: Produtos, categorias, marcas, inventário
-- =====================================================================

-- Tabela de categorias
CREATE TABLE categories (
    category_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL UNIQUE,
    slug VARCHAR(150) NOT NULL UNIQUE,
    description TEXT,
    parent_category_id UUID REFERENCES categories(category_id) ON DELETE SET NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    sort_order INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1
);
CREATE INDEX idx_categories_parent_category_id ON categories (parent_category_id);
CREATE INDEX idx_categories_is_active ON categories (is_active) WHERE deleted_at IS NULL;
CREATE TRIGGER set_timestamp_categories BEFORE UPDATE ON categories FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- Tabela de marcas
CREATE TABLE brands (
    brand_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL UNIQUE,
    slug VARCHAR(150) NOT NULL UNIQUE,
    description TEXT,
    logo_url VARCHAR(255),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1
);
CREATE INDEX idx_brands_is_active ON brands (is_active) WHERE deleted_at IS NULL;
CREATE TRIGGER set_timestamp_brands BEFORE UPDATE ON brands FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- Função para atualizar vetor de busca
CREATE OR REPLACE FUNCTION trigger_update_products_search_vector()
RETURNS TRIGGER AS $$
BEGIN
    NEW.search_vector =
        setweight(to_tsvector('portuguese', COALESCE(NEW.name, '')), 'A') ||
        setweight(to_tsvector('portuguese', COALESCE(NEW.base_sku, '')), 'A') ||
        setweight(to_tsvector('portuguese', COALESCE(NEW.description, '')), 'B');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Tabela de produtos
CREATE TABLE products (
    product_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    base_sku VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(150) NOT NULL,
    slug VARCHAR(200) NOT NULL UNIQUE,
    description TEXT,
    category_id UUID NOT NULL REFERENCES categories(category_id) ON DELETE RESTRICT,
    brand_id UUID REFERENCES brands(brand_id) ON DELETE SET NULL,
    base_price NUMERIC(10,2) NOT NULL CHECK (base_price >= 0),
    sale_price NUMERIC(10,2) CHECK (sale_price IS NULL OR sale_price >= 0),
    sale_price_start_date TIMESTAMPTZ,
    sale_price_end_date TIMESTAMPTZ,
    stock_quantity INTEGER NOT NULL DEFAULT 0 CHECK (stock_quantity >= 0),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    weight_kg NUMERIC(6,3) CHECK (weight_kg IS NULL OR weight_kg > 0),
    height_cm INTEGER CHECK (height_cm IS NULL OR height_cm > 0),
    width_cm INTEGER CHECK (width_cm IS NULL OR width_cm > 0),
    depth_cm INTEGER CHECK (depth_cm IS NULL OR depth_cm > 0),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1,
    search_vector TSVECTOR,
    CONSTRAINT chk_sale_price CHECK (sale_price IS NULL OR sale_price < base_price),
    CONSTRAINT chk_sale_dates CHECK ((sale_price IS NULL) OR (sale_price IS NOT NULL AND sale_price_start_date IS NOT NULL AND sale_price_end_date IS NOT NULL))
);
CREATE INDEX idx_products_category_id ON products (category_id);
CREATE INDEX idx_products_brand_id ON products (brand_id);
CREATE INDEX idx_products_is_active ON products (is_active) WHERE deleted_at IS NULL;
CREATE INDEX idx_products_search_vector ON products USING GIN (search_vector);
CREATE TRIGGER set_timestamp_products BEFORE UPDATE ON products FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();
CREATE TRIGGER update_search_vector_trigger BEFORE INSERT OR UPDATE ON products FOR EACH ROW EXECUTE FUNCTION trigger_update_products_search_vector();

-- Imagens dos produtos
CREATE TABLE product_images (
    product_image_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL REFERENCES products(product_id) ON DELETE CASCADE,
    image_url VARCHAR(255) NOT NULL,
    alt_text VARCHAR(255),
    is_cover BOOLEAN NOT NULL DEFAULT FALSE,
    sort_order INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1
);
CREATE INDEX idx_product_images_product_id ON product_images (product_id);
CREATE UNIQUE INDEX uq_product_images_cover_per_product ON product_images (product_id) WHERE is_cover = TRUE AND deleted_at IS NULL;
CREATE TRIGGER set_timestamp_product_images BEFORE UPDATE ON product_images FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- Cores e tamanhos para variações
CREATE TABLE product_colors (
    color_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(50) NOT NULL UNIQUE,
    hex_code CHAR(7) UNIQUE CHECK (hex_code ~ '^#[0-9a-fA-F]{6}$'),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1
);
CREATE TRIGGER set_timestamp_product_colors BEFORE UPDATE ON product_colors FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

CREATE TABLE product_sizes (
    size_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(50) NOT NULL UNIQUE,
    size_code VARCHAR(20) UNIQUE,
    sort_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1
);
CREATE TRIGGER set_timestamp_product_sizes BEFORE UPDATE ON product_sizes FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- Variações de produtos
CREATE TABLE product_variants (
    product_variant_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL REFERENCES products(product_id) ON DELETE CASCADE,
    sku VARCHAR(50) NOT NULL UNIQUE,
    color_id UUID REFERENCES product_colors(color_id) ON DELETE RESTRICT,
    size_id UUID REFERENCES product_sizes(size_id) ON DELETE RESTRICT,
    stock_quantity INTEGER NOT NULL DEFAULT 0 CHECK (stock_quantity >= 0),
    additional_price NUMERIC(10,2) NOT NULL DEFAULT 0.00,
    image_url VARCHAR(255),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1,
    CONSTRAINT uq_product_variant_attributes UNIQUE (product_id, color_id, size_id)
);
CREATE INDEX idx_product_variants_product_id ON product_variants (product_id);
CREATE INDEX idx_product_variants_color_id ON product_variants (color_id);
CREATE INDEX idx_product_variants_size_id ON product_variants (size_id);
CREATE TRIGGER set_timestamp_product_variants BEFORE UPDATE ON product_variants FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- =====================================================================
-- MICROSSERVIÇO 3: PROMOTION SERVICE
-- Responsabilidades: Cupons, promoções, descontos
-- =====================================================================

CREATE TYPE coupon_type AS ENUM ('general', 'user_specific');

CREATE TABLE coupons (
    coupon_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) NOT NULL UNIQUE,
    description TEXT,
    discount_percentage NUMERIC(5,2),
    discount_amount NUMERIC(10,2),
    valid_from TIMESTAMPTZ NOT NULL,
    valid_until TIMESTAMPTZ NOT NULL,
    max_uses INTEGER,
    times_used INTEGER NOT NULL DEFAULT 0,
    min_purchase_amount NUMERIC(10,2),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    type coupon_type NOT NULL DEFAULT 'general',
    target_user_id UUID, -- Referência externa para User Service
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1,
    CONSTRAINT chk_discount_type CHECK ( (discount_percentage IS NOT NULL AND discount_amount IS NULL) OR (discount_percentage IS NULL AND discount_amount IS NOT NULL) ),
    CONSTRAINT chk_valid_until CHECK (valid_until > valid_from)
);
CREATE INDEX idx_coupons_target_user_id ON coupons (target_user_id) WHERE type = 'user_specific';
CREATE INDEX idx_coupons_is_active_and_valid ON coupons (is_active, valid_until) WHERE deleted_at IS NULL;
CREATE TRIGGER set_timestamp_coupons BEFORE UPDATE ON coupons FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- =====================================================================
-- MICROSSERVIÇO 4: CART SERVICE
-- Responsabilidades: Carrinho de compras, sessões de compra
-- =====================================================================

CREATE TABLE shopping_carts (
    cart_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID UNIQUE, -- Referência externa para User Service
    session_id VARCHAR(128), -- Para usuários não autenticados
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMPTZ,
    CONSTRAINT chk_cart_owner CHECK (user_id IS NOT NULL OR session_id IS NOT NULL)
);
CREATE INDEX idx_shopping_carts_user_id ON shopping_carts (user_id);
CREATE INDEX idx_shopping_carts_session_id ON shopping_carts (session_id);
CREATE TRIGGER set_timestamp_shopping_carts BEFORE UPDATE ON shopping_carts FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

CREATE TABLE cart_items (
    cart_item_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    cart_id UUID NOT NULL REFERENCES shopping_carts(cart_id) ON DELETE CASCADE,
    product_variant_id UUID NOT NULL, -- Referência externa para Catalog Service
    quantity INTEGER NOT NULL CHECK (quantity > 0),
    unit_price NUMERIC(10,2) NOT NULL,
    currency CHAR(3) NOT NULL DEFAULT 'BRL',
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_cart_item_variant UNIQUE (cart_id, product_variant_id)
);
CREATE INDEX idx_cart_items_cart_id ON cart_items (cart_id);
CREATE INDEX idx_cart_items_product_variant_id ON cart_items (product_variant_id);
CREATE TRIGGER set_timestamp_cart_items BEFORE UPDATE ON cart_items FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- =====================================================================
-- MICROSSERVIÇO 5: ORDER SERVICE
-- Responsabilidades: Pedidos, processos de checkout
-- =====================================================================

CREATE TYPE order_status_enum AS ENUM ('pending', 'processing', 'shipped', 'delivered', 'canceled', 'returned');

-- Função para gerar códigos de pedido
CREATE OR REPLACE FUNCTION generate_order_code()
RETURNS VARCHAR AS $$
BEGIN
  RETURN 'ORD-' || TO_CHAR(CURRENT_DATE, 'YYYY-') || UPPER(SUBSTRING(REPLACE(gen_random_uuid()::text, '-', ''), 1, 8));
END;
$$ LANGUAGE plpgsql VOLATILE;

CREATE TABLE orders (
    order_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    reference_code VARCHAR(20) UNIQUE NOT NULL DEFAULT generate_order_code(),
    user_id UUID NOT NULL, -- Referência externa para User Service
    coupon_id UUID, -- Referência externa para Promotion Service
    status order_status_enum NOT NULL DEFAULT 'pending',
    items_total_amount NUMERIC(10,2) NOT NULL CHECK (items_total_amount >= 0),
    discount_amount NUMERIC(10,2) NOT NULL DEFAULT 0.00,
    shipping_amount NUMERIC(10,2) NOT NULL DEFAULT 0.00,
    grand_total_amount NUMERIC(12,2) GENERATED ALWAYS AS (items_total_amount - discount_amount + shipping_amount) STORED,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1
);
CREATE INDEX idx_orders_user_id ON orders (user_id);
CREATE INDEX idx_orders_coupon_id ON orders (coupon_id);
CREATE INDEX idx_orders_status ON orders (status);
CREATE INDEX idx_orders_created_at ON orders (created_at DESC);
CREATE TRIGGER set_timestamp_orders BEFORE UPDATE ON orders FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

CREATE TABLE order_items (
    order_item_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id UUID NOT NULL REFERENCES orders(order_id) ON DELETE CASCADE,
    product_variant_id UUID, -- Referência externa para Catalog Service (pode ser NULL se produto foi deletado)
    item_sku VARCHAR(100) NOT NULL,
    item_name VARCHAR(255) NOT NULL,
    quantity INTEGER NOT NULL CHECK (quantity > 0),
    unit_price NUMERIC(10,2) NOT NULL,
    line_item_total_amount NUMERIC(12,2) GENERATED ALWAYS AS (unit_price * quantity) STORED,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    version INTEGER NOT NULL DEFAULT 1
);
CREATE INDEX idx_order_items_order_id ON order_items (order_id);
CREATE INDEX idx_order_items_product_variant_id ON order_items (product_variant_id);
CREATE TRIGGER set_timestamp_order_items BEFORE UPDATE ON order_items FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- Snapshot dos endereços no momento da compra
CREATE TABLE order_addresses (
    order_address_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id UUID NOT NULL REFERENCES orders(order_id) ON DELETE CASCADE,
    address_type address_type_enum NOT NULL,
    recipient_name VARCHAR(255) NOT NULL,
    postal_code CHAR(8) NOT NULL,
    street VARCHAR(150) NOT NULL,
    street_number VARCHAR(20) NOT NULL,
    complement VARCHAR(100),
    neighborhood VARCHAR(100) NOT NULL,
    city VARCHAR(100) NOT NULL,
    state_code CHAR(2) NOT NULL,
    country_code CHAR(2) NOT NULL DEFAULT 'BR',
    phone VARCHAR(20),
    original_address_id UUID, -- Referência externa para User Service
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX idx_order_addresses_order_id ON order_addresses (order_id);

-- =====================================================================
-- MICROSSERVIÇO 6: PAYMENT SERVICE
-- Responsabilidades: Pagamentos, transações financeiras
-- =====================================================================

CREATE TYPE payment_method_enum AS ENUM ('credit_card', 'debit_card', 'pix', 'bank_slip');
CREATE TYPE payment_status_enum AS ENUM ('pending', 'approved', 'declined', 'refunded', 'partially_refunded', 'chargeback', 'error');

CREATE TABLE payments (
    payment_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id UUID NOT NULL, -- Referência externa para Order Service
    user_id UUID NOT NULL, -- Referência externa para User Service
    method payment_method_enum NOT NULL,
    status payment_status_enum NOT NULL DEFAULT 'pending',
    amount NUMERIC(10,2) NOT NULL,
    transaction_id VARCHAR(100) UNIQUE,
    gateway_response JSONB, -- Resposta completa do gateway
    method_details JSONB, -- Detalhes específicos do método de pagamento
    processed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    version INTEGER NOT NULL DEFAULT 1
);
CREATE INDEX idx_payments_order_id ON payments (order_id);
CREATE INDEX idx_payments_user_id ON payments (user_id);
CREATE INDEX idx_payments_status ON payments (status);
CREATE INDEX idx_payments_method_details_gin ON payments USING GIN (method_details);
CREATE TRIGGER set_timestamp_payments BEFORE UPDATE ON payments FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- =====================================================================
-- MICROSSERVIÇO 7: REVIEW SERVICE
-- Responsabilidades: Avaliações, comentários de produtos
-- =====================================================================

CREATE TABLE product_reviews (
    review_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID, -- Referência externa para User Service (pode ser NULL se usuário foi deletado)
    product_id UUID NOT NULL, -- Referência externa para Catalog Service
    order_id UUID, -- Referência externa para Order Service (compra verificada)
    rating SMALLINT NOT NULL CHECK (rating BETWEEN 1 AND 5),
    comment TEXT,
    is_approved BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1
);
CREATE INDEX idx_product_reviews_user_id ON product_reviews (user_id);
CREATE INDEX idx_product_reviews_product_id ON product_reviews (product_id);
CREATE INDEX idx_product_reviews_is_approved ON product_reviews (is_approved);
CREATE TRIGGER set_timestamp_product_reviews BEFORE UPDATE ON product_reviews FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- =====================================================================
-- MICROSSERVIÇO 8: AUDIT SERVICE
-- Responsabilidades: Logs de auditoria, compliance, LGPD
-- =====================================================================

CREATE TYPE audit_operation_type_enum AS ENUM ('INSERT', 'UPDATE', 'DELETE', 'LOGIN_SUCCESS', 'LOGIN_FAILURE', 'PASSWORD_RESET_REQUEST', 'PASSWORD_RESET_SUCCESS', 'ORDER_STATUS_CHANGE', 'PAYMENT_STATUS_CHANGE', 'SYSTEM_ACTION');

CREATE TABLE audit_log (
    audit_log_id BIGSERIAL PRIMARY KEY,
    service_name VARCHAR(100) NOT NULL, -- Nome do microsserviço
    table_name VARCHAR(63) NOT NULL,
    record_id TEXT,
    operation_type audit_operation_type_enum NOT NULL,
    previous_data JSONB,
    new_data JSONB,
    change_description TEXT,
    user_id UUID, -- Referência externa para User Service
    user_ip_address INET,
    user_agent TEXT,
    request_id VARCHAR(128), -- Para rastreamento de requests
    logged_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX idx_audit_log_service_table ON audit_log (service_name, table_name);
CREATE INDEX idx_audit_log_record_id ON audit_log (record_id);
CREATE INDEX idx_audit_log_logged_at ON audit_log (logged_at DESC);
CREATE INDEX idx_audit_log_user_id ON audit_log (user_id);
CREATE INDEX idx_audit_log_request_id ON audit_log (request_id);

-- =====================================================================
-- TABELAS DE COMUNICAÇÃO ENTRE MICROSSERVIÇOS
-- =====================================================================

-- Eventos para comunicação assíncrona entre serviços
CREATE TABLE domain_events (
    event_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_type VARCHAR(100) NOT NULL,
    aggregate_type VARCHAR(100) NOT NULL, -- users, products, orders, etc.
    aggregate_id UUID NOT NULL,
    event_data JSONB NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    processed_at TIMESTAMPTZ,
    version INTEGER NOT NULL DEFAULT 1
);
CREATE INDEX idx_domain_events_type ON domain_events (event_type);
CREATE INDEX idx_domain_events_aggregate ON domain_events (aggregate_type, aggregate_id);
CREATE INDEX idx_domain_events_occurred_at ON domain_events (occurred_at);
CREATE INDEX idx_domain_events_processed ON domain_events (processed_at) WHERE processed_at IS NULL;

-- =====================================================================
-- VIEWS PARA AGREGAÇÃO DE DADOS ENTRE SERVIÇOS
-- =====================================================================

-- View para estatísticas de produto (pode ser materializada)
CREATE VIEW product_statistics AS
SELECT 
    p.product_id,
    p.name,
    p.base_sku,
    COALESCE(AVG(pr.rating), 0) as average_rating,
    COUNT(pr.review_id) as review_count,
    SUM(CASE WHEN o.status IN ('delivered', 'shipped') THEN oi.quantity ELSE 0 END) as total_sold
FROM products p
LEFT JOIN product_reviews pr ON p.product_id = pr.product_id AND pr.is_approved = true
LEFT JOIN order_items oi ON EXISTS (
    SELECT 1 FROM product_variants pv 
    WHERE pv.product_id = p.product_id 
    AND pv.product_variant_id = oi.product_variant_id
)
LEFT JOIN orders o ON oi.order_id = o.order_id
WHERE p.deleted_at IS NULL
GROUP BY p.product_id, p.name, p.base_sku;

-- =====================================================================
-- CONFIGURAÇÕES DE COMUNICAÇÃO ENTRE MICROSSERVIÇOS
-- =====================================================================

-- Tabela para configuração de endpoints dos microsserviços
CREATE TABLE service_registry (
    service_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    service_name VARCHAR(100) NOT NULL UNIQUE,
    service_url VARCHAR(255) NOT NULL,
    health_check_endpoint VARCHAR(100) NOT NULL DEFAULT '/health',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE TRIGGER set_timestamp_service_registry BEFORE UPDATE ON service_registry FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();

-- Inserir configurações iniciais dos microsserviços
INSERT INTO service_registry (service_name, service_url) VALUES
    ('user-management', 'http://user-service:3001'),
    ('catalog', 'http://catalog-service:3002'),
    ('promotion', 'http://promotion-service:3003'),
    ('cart', 'http://cart-service:3004'),
    ('order', 'http://order-service:3005'),
    ('payment', 'http://payment-service:3006'),
    ('review', 'http://review-service:3007'),
    ('audit', 'http://audit-service:3008');

-- =====================================================================
-- ÍNDICES E CONSTRAINTS ADICIONAIS PARA PERFORMANCE
-- =====================================================================

-- Índices compostos para queries comuns
CREATE INDEX idx_orders_user_status_date ON orders (user_id, status, created_at DESC);
CREATE INDEX idx_payments_order_status ON payments (order_id, status);
CREATE INDEX idx_product_variants_product_active ON product_variants (product_id, is_active) WHERE deleted_at IS NULL;
CREATE INDEX idx_cart_items_cart_updated ON cart_items (cart_id, updated_at DESC);

-- =====================================================================
-- FUNÇÕES PARA COMUNICAÇÃO ENTRE MICROSSERVIÇOS
-- =====================================================================

-- Função para publicar eventos de domínio
CREATE OR REPLACE FUNCTION publish_domain_event(
    p_event_type VARCHAR(100),
    p_aggregate_type VARCHAR(100),
    p_aggregate_id UUID,
    p_event_data JSONB
)
RETURNS UUID AS $
DECLARE
    event_id UUID;
BEGIN
    INSERT INTO domain_events (event_type, aggregate_type, aggregate_id, event_data)
    VALUES (p_event_type, p_aggregate_type, p_aggregate_id, p_event_data)
    RETURNING domain_events.event_id INTO event_id;
    
    -- Aqui poderia haver uma notificação para o message broker
    -- NOTIFY domain_events, event_id::text;
    
    RETURN event_id;
END;
$ LANGUAGE plpgsql;

-- Função para marcar evento como processado
CREATE OR REPLACE FUNCTION mark_event_processed(p_event_id UUID)
RETURNS BOOLEAN AS $
BEGIN
    UPDATE domain_events 
    SET processed_at = CURRENT_TIMESTAMP 
    WHERE event_id = p_event_id AND processed_at IS NULL;
    
    RETURN FOUND;
END;
$ LANGUAGE plpgsql;

-- =====================================================================
-- TRIGGERS PARA EVENTOS DE DOMÍNIO
-- =====================================================================

-- Trigger para publicar eventos quando usuário é criado
CREATE OR REPLACE FUNCTION trigger_user_events()
RETURNS TRIGGER AS $
BEGIN
    IF TG_OP = 'INSERT' THEN
        PERFORM publish_domain_event(
            'user.created',
            'users',
            NEW.user_id,
            jsonb_build_object(
                'user_id', NEW.user_id,
                'email', NEW.email,
                'first_name', NEW.first_name,
                'last_name', NEW.last_name,
                'role', NEW.role
            )
        );
    ELSIF TG_OP = 'UPDATE' THEN
        IF OLD.email != NEW.email OR OLD.status != NEW.status THEN
            PERFORM publish_domain_event(
                'user.updated',
                'users',
                NEW.user_id,
                jsonb_build_object(
                    'user_id', NEW.user_id,
                    'old_email', OLD.email,
                    'new_email', NEW.email,
                    'old_status', OLD.status,
                    'new_status', NEW.status
                )
            );
        END IF;
    END IF;
    
    RETURN COALESCE(NEW, OLD);
END;
$ LANGUAGE plpgsql;

CREATE TRIGGER user_domain_events_trigger
    AFTER INSERT OR UPDATE ON users
    FOR EACH ROW
    EXECUTE FUNCTION trigger_user_events();

-- Trigger para eventos de pedidos
CREATE OR REPLACE FUNCTION trigger_order_events()
RETURNS TRIGGER AS $
BEGIN
    IF TG_OP = 'INSERT' THEN
        PERFORM publish_domain_event(
            'order.created',
            'orders',
            NEW.order_id,
            jsonb_build_object(
                'order_id', NEW.order_id,
                'reference_code', NEW.reference_code,
                'user_id', NEW.user_id,
                'total_amount', NEW.grand_total_amount,
                'status', NEW.status
            )
        );
    ELSIF TG_OP = 'UPDATE' AND OLD.status != NEW.status THEN
        PERFORM publish_domain_event(
            'order.status_changed',
            'orders',
            NEW.order_id,
            jsonb_build_object(
                'order_id', NEW.order_id,
                'reference_code', NEW.reference_code,
                'user_id', NEW.user_id,
                'old_status', OLD.status,
                'new_status', NEW.status,
                'total_amount', NEW.grand_total_amount
            )
        );
    END IF;
    
    RETURN COALESCE(NEW, OLD);
END;
$ LANGUAGE plpgsql;

CREATE TRIGGER order_domain_events_trigger
    AFTER INSERT OR UPDATE ON orders
    FOR EACH ROW
    EXECUTE FUNCTION trigger_order_events();

-- Trigger para eventos de pagamento
CREATE OR REPLACE FUNCTION trigger_payment_events()
RETURNS TRIGGER AS $
BEGIN
    IF TG_OP = 'INSERT' THEN
        PERFORM publish_domain_event(
            'payment.initiated',
            'payments',
            NEW.payment_id,
            jsonb_build_object(
                'payment_id', NEW.payment_id,
                'order_id', NEW.order_id,
                'user_id', NEW.user_id,
                'amount', NEW.amount,
                'method', NEW.method,
                'status', NEW.status
            )
        );
    ELSIF TG_OP = 'UPDATE' AND OLD.status != NEW.status THEN
        PERFORM publish_domain_event(
            'payment.status_changed',
            'payments',
            NEW.payment_id,
            jsonb_build_object(
                'payment_id', NEW.payment_id,
                'order_id', NEW.order_id,
                'user_id', NEW.user_id,
                'amount', NEW.amount,
                'method', NEW.method,
                'old_status', OLD.status,
                'new_status', NEW.status,
                'transaction_id', NEW.transaction_id
            )
        );
    END IF;
    
    RETURN COALESCE(NEW, OLD);
END;
$ LANGUAGE plpgsql;

CREATE TRIGGER payment_domain_events_trigger
    AFTER INSERT OR UPDATE ON payments
    FOR EACH ROW
    EXECUTE FUNCTION trigger_payment_events();

-- =====================================================================
-- PROCEDURES PARA OPERAÇÕES CROSS-SERVICE
-- =====================================================================

-- Procedure para finalizar pedido (coordena múltiplos serviços)
CREATE OR REPLACE FUNCTION complete_order_checkout(
    p_user_id UUID,
    p_cart_id UUID,
    p_shipping_address_data JSONB,
    p_billing_address_data JSONB,
    p_payment_method payment_method_enum,
    p_payment_details JSONB,
    p_coupon_code VARCHAR(50) DEFAULT NULL
)
RETURNS JSONB AS $
DECLARE
    v_order_id UUID;
    v_payment_id UUID;
    v_total_amount NUMERIC(12,2);
    v_result JSONB;
BEGIN
    -- Esta seria uma transação distribuída em uma implementação real
    -- Aqui é uma simplificação para demonstrar a estrutura
    
    -- 1. Criar o pedido
    INSERT INTO orders (user_id, items_total_amount, shipping_amount)
    SELECT p_user_id, SUM(ci.quantity * ci.unit_price), 30.00
    FROM cart_items ci 
    WHERE ci.cart_id = p_cart_id
    RETURNING order_id, grand_total_amount INTO v_order_id, v_total_amount;
    
    -- 2. Copiar itens do carrinho para o pedido
    INSERT INTO order_items (order_id, product_variant_id, item_sku, item_name, quantity, unit_price)
    SELECT v_order_id, ci.product_variant_id, 'TEMP_SKU', 'TEMP_NAME', ci.quantity, ci.unit_price
    FROM cart_items ci 
    WHERE ci.cart_id = p_cart_id;
    
    -- 3. Criar endereços do pedido
    INSERT INTO order_addresses (order_id, address_type, recipient_name, postal_code, street, street_number, neighborhood, city, state_code)
    VALUES 
        (v_order_id, 'shipping', 
         p_shipping_address_data->>'recipient_name',
         p_shipping_address_data->>'postal_code',
         p_shipping_address_data->>'street',
         p_shipping_address_data->>'street_number',
         p_shipping_address_data->>'neighborhood',
         p_shipping_address_data->>'city',
         p_shipping_address_data->>'state_code'),
        (v_order_id, 'billing', 
         p_billing_address_data->>'recipient_name',
         p_billing_address_data->>'postal_code',
         p_billing_address_data->>'street',
         p_billing_address_data->>'street_number',
         p_billing_address_data->>'neighborhood',
         p_billing_address_data->>'city',
         p_billing_address_data->>'state_code');
    
    -- 4. Iniciar pagamento
    INSERT INTO payments (order_id, user_id, method, amount, method_details)
    VALUES (v_order_id, p_user_id, p_payment_method, v_total_amount, p_payment_details)
    RETURNING payment_id INTO v_payment_id;
    
    -- 5. Limpar carrinho
    DELETE FROM cart_items WHERE cart_id = p_cart_id;
    
    -- 6. Retornar resultado
    v_result = jsonb_build_object(
        'success', true,
        'order_id', v_order_id,
        'payment_id', v_payment_id,
        'total_amount', v_total_amount
    );
    
    RETURN v_result;
    
EXCEPTION
    WHEN OTHERS THEN
        RETURN jsonb_build_object(
            'success', false,
            'error', SQLERRM
        );
END;
$ LANGUAGE plpgsql;

-- =====================================================================
-- POLÍTICAS DE RETENÇÃO E LIMPEZA DE DADOS
-- =====================================================================

-- Procedure para limpeza de eventos processados antigos
CREATE OR REPLACE FUNCTION cleanup_old_domain_events(days_old INTEGER DEFAULT 90)
RETURNS INTEGER AS $
DECLARE
    deleted_count INTEGER;
BEGIN
    DELETE FROM domain_events 
    WHERE processed_at IS NOT NULL 
    AND processed_at < CURRENT_TIMESTAMP - INTERVAL '1 day' * days_old;
    
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    
    INSERT INTO audit_log (service_name, table_name, operation_type, change_description)
    VALUES ('audit', 'domain_events', 'DELETE', 
            FORMAT('Cleaned up %s old processed events older than %s days', deleted_count, days_old));
    
    RETURN deleted_count;
END;
$ LANGUAGE plpgsql;

-- Procedure para limpeza de carrinhos expirados
CREATE OR REPLACE FUNCTION cleanup_expired_carts()
RETURNS INTEGER AS $
DECLARE
    deleted_count INTEGER;
BEGIN
    DELETE FROM shopping_carts 
    WHERE expires_at IS NOT NULL 
    AND expires_at < CURRENT_TIMESTAMP;
    
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    
    RETURN deleted_count;
END;
$ LANGUAGE plpgsql;

-- =====================================================================
-- DOCUMENTAÇÃO DOS MICROSSERVIÇOS
-- =====================================================================

/*
RESUMO DA ARQUITETURA DE MICROSSERVIÇOS:

1. USER MANAGEMENT SERVICE (Porta 3001)
   - Tabelas: users, user_addresses, user_saved_cards, user_tokens, user_consents, revoked_jwt_tokens
   - Responsabilidades: Autenticação, perfis, endereços, cartões salvos, LGPD

2. CATALOG SERVICE (Porta 3002)
   - Tabelas: categories, brands, products, product_images, product_colors, product_sizes, product_variants
   - Responsabilidades: Catálogo de produtos, categorias, marcas, estoque

3. PROMOTION SERVICE (Porta 3003)
   - Tabelas: coupons
   - Responsabilidades: Cupons de desconto, promoções

4. CART SERVICE (Porta 3004)
   - Tabelas: shopping_carts, cart_items
   - Responsabilidades: Carrinho de compras, sessões

5. ORDER SERVICE (Porta 3005)
   - Tabelas: orders, order_items, order_addresses
   - Responsabilidades: Pedidos, checkout, gestão de status

6. PAYMENT SERVICE (Porta 3006)
   - Tabelas: payments
   - Responsabilidades: Processamento de pagamentos, transações

7. REVIEW SERVICE (Porta 3007)
   - Tabelas: product_reviews
   - Responsabilidades: Avaliações e comentários de produtos

8. AUDIT SERVICE (Porta 3008)
   - Tabelas: audit_log, domain_events, service_registry
   - Responsabilidades: Logs de auditoria, eventos, compliance

COMUNICAÇÃO ENTRE SERVIÇOS:
- Eventos de domínio assíncronos via tabela domain_events
- APIs REST síncronas via service_registry
- Referências por UUID entre serviços (sem foreign keys físicas)

VANTAGENS DESTA ARQUITETURA:
- Separação clara de responsabilidades
- Escalabilidade independente por serviço
- Deployment independente
- Tecnologias diferentes por serviço se necessário
- Isolamento de falhas
- Conformidade com LGPD por serviço

DESAFIOS CONSIDERADOS:
- Consistência eventual entre serviços
- Transações distribuídas (saga pattern)
- Duplicação controlada de dados para performance
- Monitoramento e observabilidade
- Versionamento de APIs entre serviços
*/