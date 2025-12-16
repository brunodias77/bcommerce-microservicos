#!/usr/bin/env bash
set -eo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
docker compose -f "$REPO_ROOT/docker/docker-compose.yml" down -v --remove-orphans || true
for v in postgres_user_data postgres_catalog_data postgres_cart_data postgres_order_data postgres_payment_data postgres_coupon_data rabbitmq_data redis_data seq_data; do
  docker volume rm -f "$v" || true
done
for n in $(docker network ls --format '{{.Name}}' | grep -E 'bcommerce-network'); do
  docker network rm "$n" || true
done
