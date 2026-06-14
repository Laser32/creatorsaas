#!/bin/bash

# CreatorSaaS Development Startup Script

echo "🚀 CreatorSaaS Development Environment Startup"
echo "=============================================="

# Check if .env exists
if [ ! -f .env ]; then
    echo "📝 Creating .env file from .env.example..."
    cp .env.example .env
    echo "⚠️  Please edit .env with your configuration!"
fi

# Start Docker Compose services
echo "🐳 Starting Docker Compose services..."
docker-compose up -d

# Wait for databases to be healthy
echo "⏳ Waiting for databases to be healthy..."
sleep 10

# Navigate to API project
echo "📚 Running database migrations..."
cd src/CreatorSaaS.Infrastructure

# Apply migrations
dotnet ef database update \
    --configuration Debug \
    --connection "Host=localhost;Port=5432;Database=creatorsaas;Username=creatorsaas;Password=your_strong_password_here" \
    2>/dev/null || echo "⚠️  Migration may need manual setup"

cd ../..

echo ""
echo "✅ Environment is ready!"
echo ""
echo "📖 Quick Start:"
echo "   1. Open new terminal: cd src/CreatorSaaS.API && dotnet run"
echo "   2. Open another terminal: cd src/CreatorSaaS.Frontend && npm install && npm run dev"
echo ""
echo "🌐 Services:"
echo "   Frontend: http://localhost:3000"
echo "   API: http://localhost:5000"
echo "   Swagger: http://localhost:5000/swagger"
echo "   Hangfire: http://localhost:5000/hangfire"
echo "   PostgreSQL: localhost:5432"
echo "   Redis: localhost:6379"
echo ""
