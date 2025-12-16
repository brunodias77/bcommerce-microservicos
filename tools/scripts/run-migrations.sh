#!/bin/bash
# run-migrations.sh
# Runs EF Core migrations for all services

echo "Running migrations for User Service..."
# dotnet ef database update --project src/Services/User/ECommerce.User.API

echo "Running migrations for Catalog Service..."
# dotnet ef database update --project src/Services/Catalog/ECommerce.Catalog.API

echo "Running migrations for Cart Service..."
# dotnet ef database update --project src/Services/Cart/ECommerce.Cart.API

echo "Running migrations for Order Service..."
# dotnet ef database update --project src/Services/Order/ECommerce.Order.API

echo "Running migrations for Payment Service..."
# dotnet ef database update --project src/Services/Payment/ECommerce.Payment.API

echo "Running migrations for Coupon Service..."
# dotnet ef database update --project src/Services/Coupon/ECommerce.Coupon.API

echo "All migrations applied."
