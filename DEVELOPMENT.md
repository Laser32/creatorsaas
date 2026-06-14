# CreatorSaaS Development Guide

## 🎯 Project Overview

CreatorSaaS is a multi-tenant SaaS platform that automates YouTube video creation using AI. The platform generates scripts, synthesizes voices, fetches B-roll, renders videos, generates thumbnails, and uploads to YouTube.

## 🏗️ Architecture

```
Clean Architecture Layers:
├── Core               Domain entities, enums, interfaces
├── Application        CQRS commands/queries, DTOs, services
├── Infrastructure     Database, external APIs, caching
├── API               ASP.NET Core Web API
├── Workers           Hangfire background jobs
└── Frontend          React 18 + Tailwind CSS
```

## ⚙️ Tech Stack

**Backend:**
- .NET 8 ASP.NET Core
- PostgreSQL (database)
- Redis (cache & queues)
- Hangfire (job scheduler)
- Entity Framework Core
- MediatR (CQRS)

**External APIs:**
- OpenAI/Anthropic (script generation)
- ElevenLabs (voice synthesis)
- Pexels (B-roll videos)
- YouTube Data API v3 (upload)
- Stripe (billing)
- SendGrid (emails)

**Frontend:**
- React 18
- TypeScript
- Vite
- Tailwind CSS
- Zustand (state management)
- React Query (data fetching)
- Axios (HTTP client)

## 🚀 Quick Start

### Prerequisites
- Docker & Docker Compose
- .NET 8 SDK (for local backend development)
- Node.js 20+ (for frontend development)

### 1. Clone & Setup

```bash
git clone https://github.com/your-org/creatorsaas.git
cd creatorsaas
cp .env.example .env

# Edit .env with your API keys
```

### 2. Start Environment

```bash
chmod +x scripts/dev-startup.sh
./scripts/dev-startup.sh
```

This starts:
- PostgreSQL (port 5432)
- Redis (port 6379)
- Docker services

### 3. Run Locally

**Terminal 1 - Backend API:**
```bash
cd src/CreatorSaaS.API
dotnet run
# API runs on http://localhost:5000
# Swagger: http://localhost:5000/swagger
# Hangfire: http://localhost:5000/hangfire
```

**Terminal 2 - Frontend:**
```bash
cd src/CreatorSaaS.Frontend
npm install
npm run dev
# Frontend runs on http://localhost:3000
```

## 📁 Project Structure

```
src/
├── CreatorSaaS.Core/
│   ├── Entities/           (Domain models)
│   ├── Enums/              (VideoJobStatus, etc.)
│   ├── Interfaces/         (IRepository, IAIScriptService, etc.)
│   ├── Exceptions/         (Domain exceptions)
│   └── ValueObjects/       (Money, Duration, etc.)
│
├── CreatorSaaS.Application/
│   ├── Commands/           (CreateVideoJob, CancelVideoJob)
│   ├── Queries/            (GetVideoById, ListVideoJobs)
│   ├── DTOs/               (Data transfer objects)
│   ├── Validators/         (FluentValidation)
│   ├── Services/           (JwtService, etc.)
│   └── Mappings/           (AutoMapper profiles)
│
├── CreatorSaaS.Infrastructure/
│   ├── Data/               (AppDbContext, migrations)
│   ├── Repositories/       (Repository pattern, UnitOfWork)
│   ├── Services/
│   │   ├── AI/            (OpenAI, ElevenLabs, Pexels, YouTube)
│   │   └── Storage/       (Local file storage)
│   ├── Cache/             (Redis implementation)
│   └── Messaging/         (Email, webhooks)
│
├── CreatorSaaS.API/
│   ├── Controllers/        (REST API endpoints)
│   ├── Middleware/         (Auth, error handling)
│   ├── Extensions/         (DI configuration)
│   ├── Filters/           (Request/response filters)
│   └── Program.cs         (Startup configuration)
│
├── CreatorSaaS.Workers/
│   └── VideoProcessing/   (Hangfire job service)
│
└── CreatorSaaS.Frontend/
    ├── src/
    │   ├── components/     (UI, dashboard, billing)
    │   ├── pages/         (Login, Dashboard, VideoCreate)
    │   ├── hooks/         (Custom React hooks)
    │   ├── store/         (Zustand state)
    │   ├── services/      (API client, utilities)
    │   └── types/         (TypeScript types)
    └── public/
```

## 🎮 Common Commands

### Backend
```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test

# Create database migration
dotnet ef migrations add InitialCreate -p src/CreatorSaaS.Infrastructure -s src/CreatorSaaS.API

# Apply migrations
dotnet ef database update -p src/CreatorSaaS.Infrastructure -s src/CreatorSaaS.API
```

### Frontend
```bash
# Install dependencies
npm install

# Development server
npm run dev

# Build
npm run build

# Type checking
npm run type-check

# Linting
npm run lint
```

### Docker
```bash
# Start services
docker-compose up -d

# Stop services
docker-compose down

# View logs
docker-compose logs -f api

# Rebuild images
docker-compose build --no-cache
```

## 🔄 Video Processing Pipeline

The video processing pipeline runs as a Hangfire background job:

```
1. GeneratingScript       → OpenAI/Anthropic API
2. GeneratingScenes      → Parse script into scenes
3. SynthesizingVoice     → ElevenLabs API (per scene)
4. FetchingBRoll         → Pexels API (per scene)
5. RenderingVideo        → FFmpeg composition
6. GeneratingThumbnail   → Custom image generation
7. Uploading             → YouTube Data API v3
8. Completed             → Send email notification
```

### Adding a Custom Pipeline Step

1. Add `VideoJobStatus` enum value in `Core/Enums/Enums.cs`
2. Implement the processing logic in `VideoProcessingService`
3. Update the handler in `Application/Commands/Videos/VideoCommands.cs`
4. Add database migration if needed

## 🧪 Testing

### Unit Tests
```bash
cd tests/CreatorSaaS.UnitTests
dotnet test
```

### Integration Tests
```bash
cd tests/CreatorSaaS.IntegrationTests
dotnet test
```

### Frontend Tests (optional)
```bash
cd src/CreatorSaaS.Frontend
npm run test
```

## 📝 Database Migrations

### Create a Migration
```bash
cd src/CreatorSaaS.Infrastructure
dotnet ef migrations add [MigrationName] --context AppDbContext -s ../CreatorSaaS.API
```

### Apply Migrations
```bash
cd src/CreatorSaaS.API
dotnet ef database update -p ../CreatorSaaS.Infrastructure --context AppDbContext
```

### Revert Last Migration
```bash
dotnet ef migrations remove --context AppDbContext
```

## 🔐 Authentication Flow

1. **Register**: User creates tenant + account
   - Hash password with BCrypt
   - Generate JWT + Refresh token
   - Send verification email

2. **Login**: User submits credentials
   - Verify password
   - Generate JWT + Refresh token
   - Return tokens + user info

3. **Protected Routes**: Include JWT in `Authorization: Bearer {token}`
   - JWT middleware validates token
   - Extract `tenant_id` from claims
   - Enforce multi-tenant isolation

4. **Token Refresh**: Exchange refresh token for new access token
   - Validate refresh token expiry
   - Generate new JWT + refresh token

## 💳 Stripe Integration

### Payment Flow
1. User upgrades plan on billing page
2. Frontend creates Stripe checkout session
3. User redirects to Stripe payment
4. Stripe webhook notifies us of successful payment
5. Update `Subscription` + `Tenant.Plan`
6. Reset monthly video quota

### Webhook Endpoint
- `POST /api/webhooks/stripe`
- Validates Stripe signature
- Processes: `customer.subscription.updated`, `invoice.paid`, etc.

## 🔑 Environment Variables

Key variables (see `.env.example`):

```
# Database
CONNECTION_STRING=Host=localhost;Port=5432;...

# JWT
JWT_SECRET=your_secret_key_32_chars_min
JWT_ISSUER=CreatorSaaS
JWT_EXPIRY_MINUTES=60

# OpenAI
OPENAI_API_KEY=sk-...
OPENAI_MODEL=gpt-4o

# ElevenLabs
ELEVENLABS_API_KEY=...
ELEVENLABS_VOICE_ID=21m00Tcm4TlvDq8ikWAM

# Stripe
STRIPE_SECRET_KEY=sk_test_...
STRIPE_PUBLISHABLE_KEY=pk_test_...

# Storage
STORAGE_LOCAL_PATH=/app/storage

# FFmpeg
FFMPEG_PATH=/usr/bin/ffmpeg
```

## 🚢 Deployment

### Docker Compose
```bash
docker-compose -f docker-compose.yml up -d
```

### Kubernetes
```bash
# Apply configurations
kubectl apply -f k8s/base/configmap.yaml
kubectl apply -f k8s/base/database.yaml
kubectl apply -f k8s/base/api-deployment.yaml
kubectl apply -f k8s/base/frontend-ingress.yaml

# Check status
kubectl get pods -n creatorsaas
kubectl logs -n creatorsaas -f deployment/creatorsaas-api
```

### CI/CD (GitHub Actions)
Pipeline on each push to `main` or `develop`:
1. Run backend tests
2. Run frontend tests
3. Build Docker images
4. Push to registry
5. Deploy to environment

## 🐛 Troubleshooting

### Database Connection Issues
```bash
# Check connection string in .env
# Verify PostgreSQL is running
docker-compose ps

# Test connection
psql -h localhost -U creatorsaas -d creatorsaas -c "SELECT 1"
```

### Redis Connection Issues
```bash
# Check Redis is running
docker-compose ps

# Test connection
redis-cli ping
```

### API Won't Start
```bash
# Check logs
docker-compose logs api

# Verify migrations ran
dotnet ef migrations list -p src/CreatorSaaS.Infrastructure -s src/CreatorSaaS.API
```

### Frontend build fails
```bash
# Clear node_modules and reinstall
rm -rf src/CreatorSaaS.Frontend/node_modules package-lock.json
npm install
npm run build
```

## 📚 Additional Resources

- [CQRS Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [Repository Pattern](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/)
- [Hangfire Documentation](https://docs.hangfire.io/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [React Best Practices](https://react.dev/)

## 🤝 Contributing

1. Create a feature branch: `git checkout -b feature/my-feature`
2. Write tests for new functionality
3. Ensure all tests pass: `dotnet test`
4. Push and create a Pull Request
5. Wait for CI/CD pipeline to pass

## 📞 Support

- GitHub Issues: https://github.com/your-org/creatorsaas/issues
- Email: support@creatorsaas.io
- Discord: [Community server]

---

**Happy coding!** 🎉
