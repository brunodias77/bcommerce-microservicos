#!/usr/bin/env bash
set -eo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
DB_DIR="$REPO_ROOT/docs/db"

if [[ ! -d "$DB_DIR" ]]; then
  echo "Diretório de schemas não encontrado: $DB_DIR" >&2
  exit 1
fi

SHARED_SQL="$DB_DIR/00_shared_infrastructure.sql"
if [[ ! -f "$SHARED_SQL" ]]; then
  echo "Arquivo compartilhado ausente: $SHARED_SQL" >&2
  exit 1
fi

apply_for() {
  local container="$1" db="$2" specific_sql="$3"
  if [[ ! -f "$specific_sql" ]]; then
    echo "[SKIP] Arquivo específico não encontrado: $specific_sql" >&2
    return 0
  fi
  echo "[APPLY] $container -> $db"
  echo "  - $SHARED_SQL"
  cat "$SHARED_SQL" | docker exec -i "$container" psql -v ON_ERROR_STOP=1 -U bcommerce -d "$db" || { echo "[ERRO] Falha ao aplicar compartilhado em $container"; exit 1; }
  echo "  - $specific_sql"
  cat "$specific_sql" | docker exec -i "$container" psql -v ON_ERROR_STOP=1 -U bcommerce -d "$db" || { echo "[ERRO] Falha ao aplicar específico em $container"; exit 1; }
  echo "[OK] $container -> $db"
}

apply_for bcommerce-postgres-user     bcommerce_user     "$DB_DIR/01_user_service.sql"
apply_for bcommerce-postgres-catalog  bcommerce_catalog  "$DB_DIR/02_catalog_service.sql"
apply_for bcommerce-postgres-cart     bcommerce_cart     "$DB_DIR/03_cart_service.sql"
apply_for bcommerce-postgres-order    bcommerce_order    "$DB_DIR/04_order_service.sql"
apply_for bcommerce-postgres-payment  bcommerce_payment  "$DB_DIR/05_payment_service.sql"
apply_for bcommerce-postgres-coupon   bcommerce_coupon   "$DB_DIR/06_coupon_service.sql"

echo "Schemas aplicados com sucesso."

