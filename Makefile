.PHONY: help build run test clean docker-up docker-down db-migrate logs

help:
	@echo "CreatorSaaS Development Commands"
	@echo "=================================="
	@echo ""
	@echo "Development:"
	@echo "  make dev-setup          - Setup dev environment"
	@echo "  make run-api            - Run API (dotnet watch)"
	@echo "  make run-workers        - Run background workers"
	@echo "  make run-frontend       - Run frontend dev server"
	@echo ""
	@echo "Database:"
	@echo "  make db-migrate         - Run EF Core migrations"
	@echo "  make db-rollback        - Rollback last migration"
	@echo "  make db-seed            - Seed test data"
	@echo ""
	@echo "Testing:"
	@echo "  make test               - Run all tests"
	@echo "  make test-unit          - Run unit tests only"
	@echo "  make test-integration   - Run integration tests only"
	@echo "  make test-coverage      - Run with coverage"
	@echo ""
	@echo "Docker:"
	@echo "  make docker-build       - Build Docker images"
	@echo "  make docker-up          - Start Docker services"
	@echo "  make docker-down        - Stop Docker services"
	@echo "  make docker-logs        - View Docker logs"
	@echo ""
	@echo "Utilities:"
	@echo "  make clean              - Clean build artifacts"
	@echo "  make format             - Format code"
	@echo "  make lint               - Run linters"

# Development Setup
dev-setup:
	@echo "Setting up development environment..."
	@chmod +x scripts/*.sh
	@cp .env.example .env
	@echo "✅ Created .env file - please configure it"
	@echo "Running: docker-compose up -d"
	@docker-compose up -d
	@echo "✅ Docker services started"

# API
run-api:
	cd src/CreatorSaaS.API && dotnet watch run

run-workers:
	cd src/CreatorSaaS.Workers && dotnet run

run-frontend:
	cd src/CreatorSaaS.Frontend && npm install && npm run dev

# Database
db-migrate:
	cd src/CreatorSaaS.Infrastructure && \
	dotnet ef database update --project . --startup-project ../CreatorSaaS.API --context AppDbContext

db-rollback:
	cd src/CreatorSaaS.Infrastructure && \
	dotnet ef migrations remove --project . --startup-project ../CreatorSaaS.API --context AppDbContext

db-seed:
	cd src/CreatorSaaS.API && \
	dotnet run -- seed

# Testing
test:
	@echo "Running all tests..."
	dotnet test

test-unit:
	@echo "Running unit tests..."
	dotnet test tests/CreatorSaaS.UnitTests

test-integration:
	@echo "Running integration tests..."
	dotnet test tests/CreatorSaaS.IntegrationTests

test-coverage:
	@echo "Running tests with coverage..."
	dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover

# Docker
docker-build:
	docker-compose build --no-cache

docker-up:
	docker-compose up -d

docker-down:
	docker-compose down

docker-logs:
	docker-compose logs -f

# Code Quality
clean:
	@echo "Cleaning build artifacts..."
	find . -type d -name "bin" -exec rm -rf {} +
	find . -type d -name "obj" -exec rm -rf {} +
	find . -type d -name "dist" -exec rm -rf {} +
	find . -type d -name "node_modules" -exec rm -rf {} +

format:
	@echo "Formatting C# code..."
	dotnet format
	@echo "Formatting frontend code..."
	cd src/CreatorSaaS.Frontend && npm run format 2>/dev/null || echo "Skipping frontend format"

lint:
	@echo "Linting C# code..."
	dotnet format --verify-no-changes --verbosity diagnostic
	@echo "Linting frontend code..."
	cd src/CreatorSaaS.Frontend && npm run lint 2>/dev/null || echo "Skipping frontend lint"

# Utilities
install-tools:
	dotnet tool install --global dotnet-ef --version 8.0.0
	dotnet tool install --global dotnet-format

restore:
	dotnet restore

build:
	dotnet build --configuration Release

publish:
	dotnet publish -c Release -o ./publish

swagger:
	@echo "Open Swagger at http://localhost:5000/swagger"

hangfire:
	@echo "Open Hangfire Dashboard at http://localhost:5000/hangfire"

# Local development convenience
dev: dev-setup
	@echo ""
	@echo "✅ Development environment ready!"
	@echo ""
	@echo "Next steps:"
	@echo "1. Terminal 1: make run-api"
	@echo "2. Terminal 2: make run-frontend"
	@echo "3. Frontend: http://localhost:3000"
	@echo "4. API: http://localhost:5000"
	@echo "5. Swagger: http://localhost:5000/swagger"

all: clean restore build test
	@echo "✅ Full build and test complete!"

.DEFAULT_GOAL := help
