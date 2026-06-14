#!/bin/bash

# CreatorSaaS Database Migration Script

set -e

echo "🔧 CreatorSaaS Database Migration"
echo "=================================="

CONNECTION_STRING="${1:-$CONNECTION_STRING}"

if [ -z "$CONNECTION_STRING" ]; then
    echo "❌ Connection string not provided"
    echo "Usage: ./scripts/migrate-db.sh \"connection_string\""
    exit 1
fi

cd src/CreatorSaaS.Infrastructure

echo "📦 Installing dotnet-ef tool..."
dotnet tool install --global dotnet-ef --version 8.0.0 2>/dev/null || true

echo "🗄️  Creating migration..."
dotnet ef migrations add InitialCreate \
    --project . \
    --startup-project ../CreatorSaaS.API \
    --context AppDbContext \
    --configuration Release

echo "🚀 Applying migrations..."
dotnet ef database update \
    --project . \
    --startup-project ../CreatorSaaS.API \
    --context AppDbContext \
    --configuration Release

echo "✅ Database migration completed!"
