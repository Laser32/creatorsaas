# CreatorSaaS Architecture

## System Design

```
┌─────────────────────────────────────────────────────────────┐
│                    Frontend (React 18)                       │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐    │
│  │ Dashboard│  │ New Video│  │ Settings │  │ Billing  │    │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘    │
└──────────────────────────────────────────────────────────────┘
                           ↓ (REST API + WebSocket)
┌──────────────────────────────────────────────────────────────┐
│                    API Gateway (Kong/nginx)                  │
│              (rate limiting, auth, CORS)                     │
└──────────────────────────────────────────────────────────────┘
                           ↓
┌──────────────────────────────────────────────────────────────┐
│              ASP.NET Core Web API (port 5000)                │
│  ┌─────────────────────────────────────────────────────┐    │
│  │           Controllers (CQRS Endpoints)              │    │
│  │  - AuthController, VideosController, etc.          │    │
│  └─────────────────────────────────────────────────────┘    │
│  ┌─────────────────────────────────────────────────────┐    │
│  │        Middleware & Filters                         │    │
│  │  - JWT Authentication, Exception Handling           │    │
│  └─────────────────────────────────────────────────────┘    │
│  ┌─────────────────────────────────────────────────────┐    │
│  │        Application Layer (MediatR)                  │    │
│  │  - Commands, Queries, Handlers, DTOs               │    │
│  └─────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────┘
                           ↓
┌──────────────────────────────────────────────────────────────┐
│          Infrastructure & Domain Layers                      │
│  ┌──────────────────────────────────────────────────┐       │
│  │  Data Access Layer                               │       │
│  │  - AppDbContext (EF Core)                        │       │
│  │  - Repository<T>, UnitOfWork                     │       │
│  │  - PostgreSQL (database)                         │       │
│  └──────────────────────────────────────────────────┘       │
│  ┌──────────────────────────────────────────────────┐       │
│  │  External Services                               │       │
│  │  - AIScriptService (OpenAI/Anthropic)            │       │
│  │  - VoiceSynthesisService (ElevenLabs)            │       │
│  │  - BRollService (Pexels)                         │       │
│  │  - YouTubeUploadService                          │       │
│  │  - ThumbnailService (FFmpeg)                     │       │
│  │  - EmailService (SendGrid)                       │       │
│  └──────────────────────────────────────────────────┘       │
│  ┌──────────────────────────────────────────────────┐       │
│  │  Cross-Cutting Concerns                          │       │
│  │  - Redis Cache (RedisCacheService)               │       │
│  │  - File Storage (LocalStorageService)            │       │
│  │  - Logging & Monitoring                          │       │
│  └──────────────────────────────────────────────────┘       │
└──────────────────────────────────────────────────────────────┘
           ↓                      ↓                    ↓
    ┌────────────┐      ┌─────────────────┐    ┌──────────┐
    │ PostgreSQL │      │     Redis       │    │ Hangfire │
    │ (Database) │      │ (Cache & Queue) │    │ (Jobs)   │
    └────────────┘      └─────────────────┘    └──────────┘
           ↓                                           ↓
    ┌────────────────────────────────────────────────────┐
    │      Background Job Processor (Hangfire)           │
    │  ┌──────────────────────────────────────────┐     │
    │  │  VideoProcessingService                  │     │
    │  │  1. Generate Script (AI)                 │     │
    │  │  2. Create Scenes                        │     │
    │  │  3. Synthesize Voice (ElevenLabs)        │     │
    │  │  4. Fetch B-Roll (Pexels)                │     │
    │  │  5. Render Video (FFmpeg)                │     │
    │  │  6. Generate Thumbnail                   │     │
    │  │  7. Upload to YouTube                    │     │
    │  │  8. Send Email Notification              │     │
    │  └──────────────────────────────────────────┘     │
    └────────────────────────────────────────────────────┘
              ↓              ↓              ↓
      ┌──────────────┐ ┌──────────┐ ┌───────────┐
      │   OpenAI /   │ │ElevenLabs│ │  Pexels   │
      │ Anthropic API│ │   API    │ │    API    │
      └──────────────┘ └──────────┘ └───────────┘
              ↓              ↓
      ┌──────────────┐ ┌──────────────┐
      │   YouTube    │ │   Stripe     │
      │  Data API    │ │   Webhooks   │
      └──────────────┘ └──────────────┘
```

## Clean Architecture Layers

### 1. Core Layer (Domain)
**Responsibility**: Pure domain logic, no dependencies

```
Core/
├── Entities/
│   ├── BaseEntity.cs              (abstract base with soft delete)
│   ├── Tenant.cs                  (multi-tenant root)
│   ├── User.cs                    (authentication & authorization)
│   ├── Project.cs                 (video project grouping)
│   ├── Channel.cs                 (YouTube channel connection)
│   ├── VideoJob.cs                (main video processing entity)
│   ├── VideoScene.cs              (individual scene in video)
│   └── Subscription.cs            (Stripe subscription)
├── Enums/
│   └── Enums.cs                   (VideoJobStatus, SubscriptionStatus)
├── Interfaces/
│   ├── IRepository.cs             (CRUD abstraction)
│   ├── IUnitOfWork.cs             (transaction management)
│   └── IServices.cs               (external service contracts)
├── Exceptions/
│   └── DomainExceptions.cs        (NotFoundException, QuotaExceeded, etc)
└── ValueObjects/                  (Money, Duration - future expansion)
```

### 2. Application Layer (Use Cases)
**Responsibility**: Application business rules, orchestration

```
Application/
├── Commands/
│   ├── Auth/
│   │   └── AuthCommands.cs        (Register, Login, RefreshToken)
│   └── Videos/
│       └── VideoCommands.cs       (CreateVideoJob, Cancel, Variant)
├── Queries/
│   ├── Videos/
│   │   └── VideoQueries.cs        (GetVideoById, ListVideos, Analytics)
│   └── Projects/
│       └── ProjectQueries.cs      (GetProjectAnalytics)
├── DTOs/
│   └── Dtos.cs                    (AuthResponse, VideoJobDetailDto, etc)
├── Services/
│   └── JwtService.cs              (JWT token generation)
├── Validators/                    (FluentValidation rules - future)
└── Mappings/                      (AutoMapper profiles - future)
```

### 3. Infrastructure Layer (Technical)
**Responsibility**: Database, external APIs, caching, persistence

```
Infrastructure/
├── Data/
│   ├── AppDbContext.cs            (EF Core DbContext)
│   └── Migrations/                (EF Core migrations)
├── Repositories/
│   ├── Repository<T>.cs           (generic CRUD)
│   └── UnitOfWork.cs              (transaction management)
├── Services/
│   ├── AI/
│   │   ├── AIScriptService.cs     (OpenAI/Anthropic)
│   │   ├── ElevenLabsVoiceService.cs
│   │   ├── PexelsBRollService.cs
│   │   ├── FFMpegVideoRenderService.cs
│   │   ├── ThumbnailService.cs
│   │   └── YouTubeUploadService.cs
│   └── Storage/
│       └── LocalStorageService.cs (file system)
├── Cache/
│   └── RedisCacheService.cs       (Redis)
└── Services/
    └── EmailService.cs            (SendGrid)
```

### 4. API Layer (Controllers & Middleware)
**Responsibility**: HTTP request handling, authentication, routing

```
API/
├── Controllers/
│   ├── AuthController.cs          (POST /login, /register, /refresh)
│   ├── VideosController.cs        (REST CRUD for videos)
│   ├── ProjectsController.cs      (Project management)
│   ├── ChannelsController.cs      (YouTube channel connection)
│   └── WebhooksController.cs      (Stripe webhooks)
├── Middleware/
│   ├── JwtAuthMiddleware.cs       (token validation)
│   └── ExceptionMiddleware.cs     (error handling)
├── Extensions/
│   └── ServiceCollectionExtensions.cs (DI setup)
└── Program.cs                     (app startup & configuration)
```

### 5. Workers Layer (Background Jobs)
**Responsibility**: Long-running asynchronous processing

```
Workers/
└── VideoProcessing/
    ├── VideoProcessingService.cs  (Hangfire job handler)
    │   ├── Step 1: GenerateScript
    │   ├── Step 2: CreateScenes
    │   ├── Step 3: SynthesizeVoice
    │   ├── Step 4: FetchBRoll
    │   ├── Step 5: RenderVideo
    │   ├── Step 6: GenerateThumbnail
    │   ├── Step 7: UploadToYouTube
    │   └── Step 8: SendEmail
    └── Program.cs                 (Hangfire startup)
```

## Data Flow

### User Registration & Login
```
User Input (Email, Password)
    ↓
RegisterCommand Handler
    ├─ Hash password (BCrypt)
    ├─ Create Tenant + User
    ├─ Generate JWT + RefreshToken
    ├─ Store in DB
    └─ Send verification email
    ↓
Return AuthResponse (tokens + user info)
    ↓
Frontend stores tokens (localStorage)
    ↓
Use accessToken for all subsequent requests
```

### Video Creation & Processing
```
User Input (Topic, Keywords, Style)
    ↓
CreateVideoJobCommand Handler
    ├─ Verify tenant quota
    ├─ Create VideoJob entity
    ├─ Queue Hangfire job
    └─ Return VideoJobDetailDto
    ↓
Hangfire Background Job Starts
    ↓
VideoProcessingService
    ├─ Step 1: OpenAI/Anthropic → Generate Script
    ├─ Step 2: Parse script → Create VideoScenes
    ├─ Step 3: ElevenLabs → Synthesize audio per scene
    ├─ Step 4: Pexels API → Download B-Roll videos
    ├─ Step 5: FFmpeg → Compose video from scenes
    ├─ Step 6: Generate YouTube-optimized thumbnail
    ├─ Step 7: YouTube API → Upload video
    └─ Step 8: SendGrid → Send completion email
    ↓
Update VideoJob.Status = Completed
    ↓
User can see video in dashboard + YouTube link
```

## Multi-Tenancy Design

- **Tenant Isolation**: Every entity linked to `TenantId`
- **Authentication**: JWT contains `tenant_id` claim
- **Authorization**: Requests must match tenant from token
- **Data Filtering**: Global query filters exclude other tenants
- **Subscription Tracking**: Per-tenant monthly video quota

```csharp
// Example: Tenant isolation in queries
var userVideos = _uow.VideoJobs
    .Query()
    .Where(j => j.TenantId == currentUserTenantId)  // ← Key isolation point
    .ToList();
```

## Scalability Patterns

### Horizontal Scaling (API)
- **Stateless API**: Any instance can handle any request
- **Load Balancer**: Distribute requests across API pods
- **Session State**: Stored in Redis (not in-memory)

### Job Processing
- **Hangfire**: Distributed job processing across workers
- **Redis Queue**: Job state stored centrally
- **Retry Logic**: Automatic retry with backoff

### Database
- **Connection Pooling**: EF Core manages pool
- **Query Optimization**: Indexes on frequently queried fields
- **Migrations**: Zero-downtime schema updates

### Caching Strategy
```
User Login Data  → Redis (24h TTL)
Video Status     → Redis (1h TTL)
Analytics Data   → Redis (12h TTL)
```

## Security Architecture

1. **Authentication**
   - Password: BCrypt hashing
   - Tokens: JWT with 1-hour expiry
   - Refresh: Separate refresh token (30-day expiry)

2. **Authorization**
   - Role-based (owner, admin, member)
   - Tenant-based (data isolation)
   - Resource-level checks

3. **CORS & HTTPS**
   - Frontend whitelisted origin
   - HTTPS enforced in production
   - Secure cookie flags

4. **Secret Management**
   - Environment variables (.env)
   - Kubernetes Secrets
   - HashiCorp Vault (enterprise)

5. **API Key Rotation**
   - External APIs (OpenAI, ElevenLabs, etc.)
   - Stripe webhook signature validation
   - YouTube OAuth token refresh

## Monitoring & Observability

```
Application Layer
    ↓ (Structured logging)
    ├─ Serilog → Log files + Console
    └─ Sentry (optional) → Error tracking
    
Background Jobs
    ↓ (Hangfire Dashboard)
    └─ /hangfire → Real-time job monitoring

Database
    ↓ (Query performance)
    └─ slow query log (PostgreSQL)

API
    ↓ (Request metrics)
    └─ Response times, error rates
```

## Cost Optimization

| Component | Cost Driver | Optimization |
|-----------|-----------|--------------|
| OpenAI/Anthropic | Tokens per request | Prompt optimization, caching |
| ElevenLabs | Characters synthesized | Caching voice outputs |
| Pexels | API calls | Rate limit, cache results |
| YouTube | Bandwidth | Efficient codec (H.264) |
| PostgreSQL | Storage & compute | Index optimization, archival |
| Redis | Memory | TTL policies, eviction |
| Hangfire | Job count | Batch processing, cleanup |

---

**Architecture Version**: 1.0  
**Last Updated**: 2024  
**Maintainer**: CreatorSaaS Team
