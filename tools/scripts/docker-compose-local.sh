#!/bin/bash
# docker-compose-local.sh
# Helper to run the solution locally with Docker Compose

echo "Starting Bcommerce solution locally..."
docker-compose -f ../../docker-compose.yml up -d --build
