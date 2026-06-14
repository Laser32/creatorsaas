#!/bin/bash
# PostgreSQL initialization script for Docker

set -e

echo "Initializing PostgreSQL database for CreatorSaaS..."

# Create the application database and user
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres <<-EOSQL
    CREATE DATABASE $POSTGRES_DB
        ENCODING = 'UTF8'
        LC_COLLATE = 'en_US.UTF-8'
        LC_CTYPE = 'en_US.UTF-8';

    -- Create user if it doesn't exist
    DO \$\$ 
    BEGIN 
        CREATE USER creatorsaas WITH PASSWORD '$POSTGRES_PASSWORD';
    EXCEPTION WHEN DUPLICATE_OBJECT THEN
        ALTER USER creatorsaas WITH PASSWORD '$POSTGRES_PASSWORD';
    END
    \$\$;

    -- Grant privileges
    GRANT ALL PRIVILEGES ON DATABASE $POSTGRES_DB TO creatorsaas;
    ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO creatorsaas;
    ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO creatorsaas;
    ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON FUNCTIONS TO creatorsaas;
EOSQL

echo "PostgreSQL initialization complete!"
