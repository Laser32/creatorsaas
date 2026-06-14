# CreatorSaaS – AI Video Automation Platform

A production-ready, multi-tenant SaaS platform that automates YouTube video creation using AI.

## Architecture Overview

```
Topic → Script (OpenAI/Anthropic) → Scenes → Voice (ElevenLabs) → B-Roll (Pexels) → Render → Upload (YouTube)
```

### Tech Stack

| Layer | Technology |
|-------|------------|
| Backend API | .NET 8 ASP.NET Core |
| Database | PostgreSQL + EF Core |
| Cache / Queues | Redis |
| Background Jobs | Hangfire |
| Frontend | React 18 + Tailwind CSS |
| Auth | JWT + Refresh Tokens |
| Billing | Stripe |
| AI Script | OpenAI GPT-4 / Anthropic Claude |
| Voice | ElevenLabs TTS |
| B-Roll | Pexels Video API |
| Upload | YouTube Data API v3 |
| Container | Docker + Docker Compose |
| Orchestration | Kubernetes-ready (Helm charts) |

## Quick Start

### Prerequisites
- Docker & Docker Compose
- .NET 8 SDK (for local dev)
- Node.js 20+ (for frontend)

### 1. Clone & Configure

```bash
git clone https://github.com/your-org/creatorsaas.git
cd creatorsaas
cp .env.example .env
# Fill in your API keys in .env
```

### 2. Start with Docker Compose

```bash
docker-compose up -d
```

Services start at:
- **API**: http://localhost:5000
- **Frontend**: http://localhost:3000
- **Hangfire Dashboard**: http://localhost:5000/hangfire
- **PostgreSQL**: localhost:5432
- **Redis**: localhost:6379

### 3. Local Development

```bash
# Backend
cd src/CreatorSaaS.API
dotnet run

# Frontend
cd src/CreatorSaaS.Frontend
npm install && npm run dev

# Workers
cd src/CreatorSaaS.Workers
dotnet run
```

## Project Structure

```
CreatorSaaS/
├── src/
│   ├── CreatorSaaS.Core/          # Domain entities, interfaces, enums
│   ├── CreatorSaaS.Application/   # CQRS commands/queries, DTOs, services
│   ├── CreatorSaaS.Infrastructure/# EF Core, Redis, external APIs
│   ├── CreatorSaaS.API/           # ASP.NET Core Web API
│   ├── CreatorSaaS.Workers/       # Hangfire background workers
│   └── CreatorSaaS.Frontend/      # React + Tailwind dashboard
├── tests/
│   ├── CreatorSaaS.UnitTests/
│   └── CreatorSaaS.IntegrationTests/
├── docker/
│   ├── Dockerfile.api
│   ├── Dockerfile.workers
│   └── Dockerfile.frontend
├── k8s/                           # Kubernetes manifests
├── scripts/                       # Dev & deployment scripts
└── .github/workflows/             # CI/CD pipelines
```

## Video Pipeline

```
CreateVideoJob
    ├── [1] GenerateScript      → OpenAI/Anthropic → script.json
    ├── [2] GenerateScenes      → Parses script into scene objects
    ├── [3] SynthesizeVoice     → ElevenLabs → audio/*.mp3
    ├── [4] FetchBRoll          → Pexels API → clips/*.mp4
    ├── [5] RenderVideo         → FFmpeg → output.mp4
    ├── [6] GenerateThumbnail   → AI + overlay → thumbnail.jpg
    └── [7] UploadToYouTube     → YouTube Data API v3 → video_id
```

## Subscription Plans

| Plan | Videos/month | AI Quality | Price |
|------|-------------|------------|-------|
| Starter | 5 | Standard | $19/mo |
| Pro | 25 | Advanced | $49/mo |
| Agency | Unlimited | Premium | $149/mo |

## Environment Variables

See `.env.example` for all required configuration.

## API Documentation

Swagger UI available at: http://localhost:5000/swagger

## License

MIT
