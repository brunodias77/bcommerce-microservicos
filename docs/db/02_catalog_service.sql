-- ========================================
-- CATALOG SERVICE DATABASE
-- Responsável por: Categorias, Produtos, Imagens, Estoque
-- ========================================

-- ========================================
-- 1. EXTENSIONS
-- ========================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm"; -- Para buscas full-text

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

-- Função para gerar slug a partir do nome
CREATE OR REPLACE FUNCTION generate_slug(input_text TEXT)
RETURNS TEXT AS $$
BEGIN
    RETURN LOWER(
        REGEXP_REPLACE(
            REGEXP_REPLACE(
                TRANSLATE(
                    input_text,
                    'áàâãäéèêëíìîïóòôõöúùûüçñÁÀÂÃÄÉÈÊËÍÌÎÏÓÒÔÕÖÚÙÛÜÇÑ',
                    'aaaaaeeeeiiiiooooouuuucnAAAAAEEEEIIIIOOOOOUUUUCN'
                ),
                '[^a-zA-Z0-9\s-]', '', 'g'
            ),
            '\s+', '-', 'g'
        )
    );
END;
$$ LANGUAGE plpgsql;

-- ========================================
-- 3. ENUMS
-- ========================================
CREATE TYPE product_status_enum AS ENUM ('DRAFT', 'ACTIVE', 'INACTIVE', 'OUT_OF_STOCK', 'DISCONTINUED');
CREATE TYPE stock_movement_type_enum AS ENUM ('IN', 'OUT', 'ADJUSTMENT', 'RESERVE', 'RELEASE');

-- ========================================
-- 4. TABLES
-- ========================================

-- Tabela de categorias (hierárquica)
CREATE TABLE categories (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Hierarquia
    parent_id UUID REFERENCES categories(id) ON DELETE SET NULL,
    path TEXT, -- Materialized path: "root/electronics/phones"
    depth INT NOT NULL DEFAULT 0,
    
    -- Dados da categoria
    name VARCHAR(100) NOT NULL,
    slug VARCHAR(120) NOT NULL,
    description TEXT,
    image_url TEXT,
    
    -- SEO
    meta_title VARCHAR(70),
    meta_description VARCHAR(160),
    
    -- Controle
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    sort_order INT DEFAULT 0,
    
    -- Controle de versão
    version INT NOT NULL DEFAULT 1,
    
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ,
    
    -- Constraints
    CONSTRAINT chk_categories_name_not_blank CHECK (char_length(trim(name)) > 0),
    CONSTRAINT chk_categories_parent_not_self CHECK (parent_id IS NULL OR parent_id <> id),
    CONSTRAINT chk_categories_depth CHECK (depth >= 0 AND depth <= 5)
);

-- Tabela de produtos
CREATE TABLE products (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Relacionamentos
    category_id UUID REFERENCES categories(id) ON DELETE SET NULL,
    brand_id UUID, -- Referência futura para tabela de marcas
    
    -- Identificação
    sku VARCHAR(100) UNIQUE NOT NULL,
    slug VARCHAR(200) NOT NULL,
    barcode VARCHAR(50),
    
    -- Dados do produto
    name VARCHAR(150) NOT NULL,
    short_description VARCHAR(500),
    description TEXT,
    
    -- Preços
    price DECIMAL(10, 2) NOT NULL,
    compare_at_price DECIMAL(10, 2), -- Preço "de" para mostrar desconto
    cost_price DECIMAL(10, 2), -- Custo para cálculo de margem
    
    -- Estoque
    stock INT NOT NULL DEFAULT 0,
    reserved_stock INT NOT NULL DEFAULT 0, -- Reservado em carrinhos/pedidos
    low_stock_threshold INT DEFAULT 10,
    
    -- Dimensões e peso (para cálculo de frete)
    weight_grams INT,
    height_cm DECIMAL(6, 2),
    width_cm DECIMAL(6, 2),
    length_cm DECIMAL(6, 2),
    
    -- SEO
    meta_title VARCHAR(70),
    meta_description VARCHAR(160),
    
    -- Controle
    status product_status_enum NOT NULL DEFAULT 'DRAFT',
    is_featured BOOLEAN DEFAULT FALSE,
    is_digital BOOLEAN DEFAULT FALSE,
    requires_shipping BOOLEAN DEFAULT TRUE,
    
    -- Atributos extras (flexível)
    attributes JSONB DEFAULT '{}', -- { "color": "red", "size": "M", "material": "cotton" }
    tags TEXT[], -- Array de tags para busca
    
    -- Controle de versão
    version INT NOT NULL DEFAULT 1,
    
    -- Timestamps
    published_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ,
    
    -- Constraints
    CONSTRAINT chk_products_name_not_blank CHECK (char_length(trim(name)) > 0),
    CONSTRAINT chk_products_price_positive CHECK (price >= 0),
    CONSTRAINT chk_products_stock_non_negative CHECK (stock >= 0),
    CONSTRAINT chk_products_reserved_stock CHECK (reserved_stock >= 0 AND reserved_stock <= stock),
    CONSTRAINT chk_products_compare_price CHECK (compare_at_price IS NULL OR compare_at_price > price)
);

-- Tabela de imagens de produtos
CREATE TABLE product_images (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    
    -- Dados da imagem
    url TEXT NOT NULL,
    alt_text VARCHAR(255),
    
    -- Variantes de tamanho (para CDN/responsive)
    url_thumbnail TEXT,
    url_medium TEXT,
    url_large TEXT,
    
    -- Controle
    is_primary BOOLEAN DEFAULT FALSE,
    sort_order INT DEFAULT 0,
    
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Tabela de movimentação de estoque
CREATE TABLE stock_movements (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    
    -- Dados da movimentação
    movement_type stock_movement_type_enum NOT NULL,
    quantity INT NOT NULL, -- Positivo para entrada, negativo para saída
    
    -- Referência externa (pedido, ajuste manual, etc.)
    reference_type VARCHAR(50), -- 'ORDER', 'MANUAL_ADJUSTMENT', 'INVENTORY_COUNT'
    reference_id UUID,
    
    -- Estoque resultante
    stock_before INT NOT NULL,
    stock_after INT NOT NULL,
    
    -- Metadata
    reason TEXT,
    performed_by UUID, -- User ID que fez a operação
    
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Tabela de reservas de estoque (para carrinhos e pedidos pendentes)
CREATE TABLE stock_reservations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    
    -- Referência
    reference_type VARCHAR(50) NOT NULL, -- 'CART', 'ORDER'
    reference_id UUID NOT NULL,
    
    -- Quantidade reservada
    quantity INT NOT NULL CHECK (quantity > 0),
    
    -- Controle de expiração
    expires_at TIMESTAMPTZ NOT NULL,
    released_at TIMESTAMPTZ,
    
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Unique constraint
    CONSTRAINT uq_stock_reservation UNIQUE (product_id, reference_type, reference_id)
);

-- Tabela de avaliações de produtos
CREATE TABLE product_reviews (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    user_id UUID NOT NULL, -- Referência ao User Service (sem FK)
    order_id UUID, -- Referência ao Order Service (sem FK) - para validar compra
    
    -- Avaliação
    rating INT NOT NULL CHECK (rating >= 1 AND rating <= 5),
    title VARCHAR(200),
    comment TEXT,
    
    -- Controle
    is_verified_purchase BOOLEAN DEFAULT FALSE,
    is_approved BOOLEAN DEFAULT FALSE,
    
    -- Resposta do vendedor
    seller_response TEXT,
    seller_responded_at TIMESTAMPTZ,
    
    -- Timestamps
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ,
    
    -- Um usuário só pode avaliar um produto uma vez
    CONSTRAINT uq_product_review_user UNIQUE (product_id, user_id)
);

-- Outbox para eventos do Catalog Service
CREATE TABLE catalog_outbox_events (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    aggregate_type VARCHAR(100) NOT NULL,
    aggregate_id UUID NOT NULL,
    event_type VARCHAR(100) NOT NULL, -- PRODUCT_CREATED, STOCK_UPDATED, PRICE_CHANGED, etc.
    payload JSONB NOT NULL,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    processed_at TIMESTAMPTZ,
    error_message TEXT,
    retry_count INT DEFAULT 0
);

-- Inbox para idempotência
CREATE TABLE catalog_inbox_messages (
    id UUID PRIMARY KEY,
    message_type VARCHAR(100) NOT NULL,
    processed_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Audit Log
CREATE TABLE catalog_audit_logs (
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
CREATE TRIGGER trg_categories_updated_at
    BEFORE UPDATE ON categories
    FOR EACH ROW
    EXECUTE FUNCTION trigger_set_timestamp();

CREATE TRIGGER trg_categories_version
    BEFORE UPDATE ON categories
    FOR EACH ROW
    EXECUTE FUNCTION trigger_increment_version();

CREATE TRIGGER trg_products_updated_at
    BEFORE UPDATE ON products
    FOR EACH ROW
    EXECUTE FUNCTION trigger_set_timestamp();

CREATE TRIGGER trg_products_version
    BEFORE UPDATE ON products
    FOR EACH ROW
    EXECUTE FUNCTION trigger_increment_version();

CREATE TRIGGER trg_reviews_updated_at
    BEFORE UPDATE ON product_reviews
    FOR EACH ROW
    EXECUTE FUNCTION trigger_set_timestamp();

-- ========================================
-- 6. INDEXES
-- ========================================

-- Categories
CREATE UNIQUE INDEX uq_categories_slug ON categories(slug) WHERE deleted_at IS NULL;
CREATE INDEX idx_categories_parent_id ON categories(parent_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_categories_path ON categories(path) WHERE deleted_at IS NULL;
CREATE INDEX idx_categories_active ON categories(is_active) WHERE deleted_at IS NULL;
CREATE INDEX idx_categories_name_trgm ON categories USING GIN (name gin_trgm_ops);
CREATE UNIQUE INDEX uq_categories_parent_name ON categories(parent_id, LOWER(name)) WHERE deleted_at IS NULL;

-- Products
CREATE UNIQUE INDEX uq_products_slug ON products(slug) WHERE deleted_at IS NULL;
CREATE INDEX idx_products_category_id ON products(category_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_products_status ON products(status) WHERE deleted_at IS NULL;
CREATE INDEX idx_products_active ON products(category_id, status) WHERE status = 'ACTIVE' AND deleted_at IS NULL;
CREATE INDEX idx_products_featured ON products(is_featured) WHERE is_featured = TRUE AND deleted_at IS NULL;
CREATE INDEX idx_products_price ON products(price) WHERE deleted_at IS NULL;
CREATE INDEX idx_products_created_at ON products(created_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX idx_products_name_trgm ON products USING GIN (name gin_trgm_ops);
CREATE INDEX idx_products_tags ON products USING GIN (tags);
CREATE INDEX idx_products_attributes ON products USING GIN (attributes);
CREATE INDEX idx_products_low_stock ON products(stock) WHERE stock <= low_stock_threshold AND status = 'ACTIVE';

-- Product Images
CREATE INDEX idx_product_images_product_id ON product_images(product_id);
CREATE INDEX idx_product_images_sort ON product_images(product_id, sort_order);
CREATE UNIQUE INDEX uq_product_images_primary ON product_images(product_id) WHERE is_primary = TRUE;

-- Stock Movements
CREATE INDEX idx_stock_movements_product_id ON stock_movements(product_id);
CREATE INDEX idx_stock_movements_created_at ON stock_movements(created_at DESC);
CREATE INDEX idx_stock_movements_reference ON stock_movements(reference_type, reference_id);

-- Stock Reservations
CREATE INDEX idx_stock_reservations_product_id ON stock_reservations(product_id) WHERE released_at IS NULL;
CREATE INDEX idx_stock_reservations_expires ON stock_reservations(expires_at) WHERE released_at IS NULL;
CREATE INDEX idx_stock_reservations_reference ON stock_reservations(reference_type, reference_id);

-- Product Reviews
CREATE INDEX idx_product_reviews_product_id ON product_reviews(product_id) WHERE deleted_at IS NULL;
CREATE INDEX idx_product_reviews_user_id ON product_reviews(user_id);
CREATE INDEX idx_product_reviews_rating ON product_reviews(product_id, rating) WHERE is_approved = TRUE AND deleted_at IS NULL;
CREATE INDEX idx_product_reviews_approved ON product_reviews(is_approved) WHERE deleted_at IS NULL;

-- Outbox
CREATE INDEX idx_catalog_outbox_unprocessed ON catalog_outbox_events(created_at) WHERE processed_at IS NULL;
CREATE INDEX idx_catalog_outbox_aggregate ON catalog_outbox_events(aggregate_type, aggregate_id);

-- Audit
CREATE INDEX idx_catalog_audit_entity ON catalog_audit_logs(entity_type, entity_id);
CREATE INDEX idx_catalog_audit_created ON catalog_audit_logs(created_at);

-- ========================================
-- 7. MATERIALIZED VIEWS (para performance)
-- ========================================

-- View materializada para estatísticas de produtos
CREATE MATERIALIZED VIEW mv_product_stats AS
SELECT 
    p.id AS product_id,
    p.name,
    p.price,
    p.stock,
    COALESCE(AVG(r.rating), 0) AS avg_rating,
    COUNT(r.id) AS review_count,
    COUNT(r.id) FILTER (WHERE r.rating = 5) AS five_star_count,
    COUNT(r.id) FILTER (WHERE r.rating = 4) AS four_star_count,
    COUNT(r.id) FILTER (WHERE r.rating = 3) AS three_star_count,
    COUNT(r.id) FILTER (WHERE r.rating = 2) AS two_star_count,
    COUNT(r.id) FILTER (WHERE r.rating = 1) AS one_star_count
FROM products p
LEFT JOIN product_reviews r ON r.product_id = p.id AND r.is_approved = TRUE AND r.deleted_at IS NULL
WHERE p.deleted_at IS NULL
GROUP BY p.id, p.name, p.price, p.stock;

CREATE UNIQUE INDEX idx_mv_product_stats_id ON mv_product_stats(product_id);

-- ========================================
-- 8. COMMENTS
-- ========================================
COMMENT ON TABLE categories IS 'Categorias hierárquicas de produtos';
COMMENT ON TABLE products IS 'Catálogo de produtos';
COMMENT ON TABLE product_images IS 'Imagens dos produtos';
COMMENT ON TABLE stock_movements IS 'Histórico de movimentações de estoque';
COMMENT ON TABLE stock_reservations IS 'Reservas temporárias de estoque';
COMMENT ON TABLE product_reviews IS 'Avaliações de produtos pelos usuários';
COMMENT ON COLUMN products.attributes IS 'Atributos dinâmicos em formato JSON';
COMMENT ON COLUMN products.reserved_stock IS 'Quantidade reservada em carrinhos e pedidos pendentes';
