#!/bin/bash

# PostgreSQL Initialization Script for CreatorSaaS
# This script sets up the database with initial data

set -e

echo "🔧 CreatorSaaS Database Initialization"
echo "======================================"

POSTGRES_USER=${POSTGRES_USER:-creatorsaas}
POSTGRES_PASSWORD=${POSTGRES_PASSWORD:-your_strong_password_here}
POSTGRES_DB=${POSTGRES_DB:-creatorsaas}
POSTGRES_HOST=${POSTGRES_HOST:-localhost}
POSTGRES_PORT=${POSTGRES_PORT:-5432}

# Function to run SQL command
run_sql() {
    PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -U "$POSTGRES_USER" -d "$POSTGRES_DB" -c "$1"
}

# Function to run SQL file
run_sql_file() {
    PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -U "$POSTGRES_USER" -d "$POSTGRES_DB" -f "$1"
}

echo "📦 Creating database..."

# Create database if not exists
PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -U "$POSTGRES_USER" -tc "SELECT 1 FROM pg_database WHERE datname = '$POSTGRES_DB'" | grep -q 1 || \
PGPASSWORD="$POSTGRES_PASSWORD" psql -h "$POSTGRES_HOST" -U "$POSTGRES_USER" -c "CREATE DATABASE $POSTGRES_DB ENCODING 'UTF8' LOCALE 'en_US.UTF-8';"

echo "✅ Database created/verified"

echo ""
echo "🗄️  Running migrations..."

# Run EF Core migrations
cd src/CreatorSaaS.Infrastructure
dotnet ef database update \
    --project . \
    --startup-project ../CreatorSaaS.API \
    --context AppDbContext \
    --configuration Debug

cd ../..

echo "✅ Migrations completed"

echo ""
echo "📝 Inserting seed data..."

# Insert test tenant
run_sql "
INSERT INTO \"Tenants\" (\"Id\", \"Name\", \"Slug\", \"Plan\", \"IsActive\", \"MonthlyVideoQuota\", \"VideosCreatedThisMonth\", \"QuotaResetAt\", \"CreatedAt\", \"UpdatedAt\", \"IsDeleted\")
VALUES (
    'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    'Demo Tenant',
    'demo-tenant-test',
    'starter',
    true,
    5,
    0,
    CURRENT_TIMESTAMP + INTERVAL '30 days',
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP,
    false
) ON CONFLICT DO NOTHING;
"

# Insert test user (password: TestPassword123!)
run_sql "
INSERT INTO \"Users\" (\"Id\", \"TenantId\", \"Email\", \"PasswordHash\", \"FirstName\", \"LastName\", \"Role\", \"EmailVerified\", \"CreatedAt\", \"UpdatedAt\", \"IsDeleted\")
VALUES (
    'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    'demo@creatorsaas.io',
    '\$2a\$11\$8ZiXyKxcVi31F.RW3cFCx.WuGVkLLNhUCVhwFfZx9JxPo7aMQr8zy',
    'Demo',
    'User',
    'owner',
    true,
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP,
    false
) ON CONFLICT DO NOTHING;
"

# Insert test project
run_sql "
INSERT INTO \"Projects\" (\"Id\", \"TenantId\", \"Name\", \"Description\", \"DefaultLanguage\", \"DefaultStyle\", \"IsActive\", \"CreatedAt\", \"UpdatedAt\", \"IsDeleted\")
VALUES (
    'cccccccc-cccc-cccc-cccc-cccccccccccc',
    'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    'My First Video Project',
    'Testing video creation',
    'en',
    'tutorial',
    true,
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP,
    false
) ON CONFLICT DO NOTHING;
"

echo "✅ Seed data inserted"

echo ""
echo "📊 Database Statistics"
echo "====================="

echo ""
echo "Tables created:"
run_sql "
SELECT 
    schemaname,
    tablename 
FROM pg_tables 
WHERE schemaname NOT IN ('pg_catalog', 'information_schema') 
ORDER BY tablename;
"

echo ""
echo "Tenants:"
run_sql "SELECT \"Id\", \"Name\", \"Plan\", \"IsActive\" FROM \"Tenants\" WHERE NOT \"IsDeleted\";"

echo ""
echo "Users:"
run_sql "SELECT \"Id\", \"Email\", \"FirstName\", \"Role\" FROM \"Users\" WHERE NOT \"IsDeleted\";"

echo ""
echo "Projects:"
run_sql "SELECT \"Id\", \"Name\", \"DefaultLanguage\" FROM \"Projects\" WHERE NOT \"IsDeleted\";"

echo ""
echo "✅ Database initialization completed!"
echo ""
echo "📚 Test Credentials:"
echo "  Email: demo@creatorsaas.io"
echo "  Password: TestPassword123!"
echo ""
