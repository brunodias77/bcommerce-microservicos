- Subir infra: ./scripts/infrastructure/up.sh
- Status: ./scripts/infrastructure/status.sh
- Parar: ./scripts/infrastructure/stop.sh
- Remover containers: ./scripts/infrastructure/down.sh
- Limpar tudo: ./scripts/infrastructure/destroy.sh
chmod +x ./infrastructure/*.sh && ./infrastructure/apply-db-schemas.sh