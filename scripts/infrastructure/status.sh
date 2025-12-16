#!/usr/bin/env bash
set -eo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
compose() { docker compose -f "$REPO_ROOT/docker/docker-compose.yml" "$@"; }

containers=(
  bcommerce-postgres-user
  bcommerce-postgres-catalog
  bcommerce-postgres-cart
  bcommerce-postgres-order
  bcommerce-postgres-payment
  bcommerce-postgres-coupon
  bcommerce-rabbitmq
  bcommerce-redis
  bcommerce-seq
)

fail=0

compose ps

for c in "${containers[@]}"; do
  status=$(docker inspect -f '{{.State.Status}}' "$c" 2>/dev/null || echo "unknown")
  health=$(docker inspect -f '{{if .State.Health}}{{.State.Health.Status}}{{else}}n/a{{end}}' "$c" 2>/dev/null || echo "n/a")
  echo "[status] $c -> status=$status health=$health"
  if [[ "$status" != "running" ]]; then fail=1; fi
done

for c in bcommerce-postgres-user bcommerce-postgres-catalog bcommerce-postgres-cart bcommerce-postgres-order bcommerce-postgres-payment bcommerce-postgres-coupon; do
  case "$c" in
    bcommerce-postgres-user) db="bcommerce_user";;
    bcommerce-postgres-catalog) db="bcommerce_catalog";;
    bcommerce-postgres-cart) db="bcommerce_cart";;
    bcommerce-postgres-order) db="bcommerce_order";;
    bcommerce-postgres-payment) db="bcommerce_payment";;
    bcommerce-postgres-coupon) db="bcommerce_coupon";;
    *) db="postgres";;
  esac
  if ! docker exec "$c" pg_isready -U bcommerce -d "$db" >/dev/null 2>&1; then
    echo "[check] $c:$db -> pg_isready FAILED"
    fail=1
  else
    echo "[check] $c:$db -> pg_isready OK"
  fi
done

if ! docker exec bcommerce-rabbitmq rabbitmq-diagnostics -q ping >/dev/null 2>&1; then
  echo "[check] rabbitmq -> ping FAILED"; fail=1
else
  echo "[check] rabbitmq -> ping OK"
fi

if ! docker exec bcommerce-redis redis-cli -a bcommerce123 ping | grep -q PONG; then
  echo "[check] redis -> PING FAILED"; fail=1
else
  echo "[check] redis -> PING OK"
fi

seq_http=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:8081/ 2>/dev/null || echo "000")
if [[ "$seq_http" != "200" && "$seq_http" != "302" ]]; then
  echo "[check] seq -> HTTP $seq_http FAILED"; fail=1
else
  echo "[check] seq -> HTTP $seq_http OK"
fi

if [[ "$fail" -ne 0 ]]; then
  echo "Infra: problemas detectados"; exit 1
else
  echo "Infra: OK"; exit 0
fi
