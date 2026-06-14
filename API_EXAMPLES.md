# CreatorSaaS API Examples

Base URL: `http://localhost:5000/api` (development) or `https://api.creatorsaas.io` (production)

## Authentication

### Register

```http
POST /auth/register
Content-Type: application/json

{
  "tenantName": "My Video Agency",
  "email": "john@example.com",
  "password": "SecurePassword123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Response (200):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "base64_encoded_refresh_token",
  "user": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "email": "john@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "role": "owner",
    "tenantId": "223e4567-e89b-12d3-a456-426614174000",
    "avatarUrl": null
  },
  "tenant": {
    "id": "223e4567-e89b-12d3-a456-426614174000",
    "name": "My Video Agency",
    "slug": "my-video-agency-abc123",
    "plan": "starter",
    "monthlyVideoQuota": 5,
    "videosCreatedThisMonth": 0
  }
}
```

### Login

```http
POST /auth/login
Content-Type: application/json

{
  "email": "john@example.com",
  "password": "SecurePassword123!"
}
```

### Refresh Token

```http
POST /auth/refresh
Content-Type: application/json

{
  "refreshToken": "base64_encoded_refresh_token"
}
```

### Get Current User

```http
GET /auth/me
Authorization: Bearer {accessToken}
```

---

## Videos

### Create Video Job

```http
POST /videos
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "projectId": "123e4567-e89b-12d3-a456-426614174000",
  "channelId": "223e4567-e89b-12d3-a456-426614174000",
  "topic": "10 Tips for Better Video Marketing in 2024",
  "keywords": "video marketing, social media, content creation",
  "language": "en",
  "style": "tutorial",
  "targetDurationSeconds": 300,
  "voiceId": "21m00Tcm4TlvDq8ikWAM"
}
```

**Response (201):**
```json
{
  "id": "323e4567-e89b-12d3-a456-426614174000",
  "projectId": "123e4567-e89b-12d3-a456-426614174000",
  "channelId": "223e4567-e89b-12d3-a456-426614174000",
  "topic": "10 Tips for Better Video Marketing in 2024",
  "keywords": "video marketing, social media, content creation",
  "language": "en",
  "style": "tutorial",
  "targetDurationSeconds": 300,
  "voiceId": "21m00Tcm4TlvDq8ikWAM",
  "status": 1,
  "currentStep": 0,
  "errorMessage": null,
  "retryCount": 0,
  "scriptTitle": null,
  "scriptContent": null,
  "generatedTitle": null,
  "generatedDescription": null,
  "tags": null,
  "youtubeVideoId": null,
  "youtubeUrl": null,
  "views": null,
  "likes": null,
  "comments": null,
  "clickThroughRate": null,
  "createdAt": "2024-01-15T10:30:00Z",
  "completedAt": null,
  "scenes": []
}
```

### Get Video Job

```http
GET /videos/{videoJobId}
Authorization: Bearer {accessToken}
```

### List Video Jobs

```http
GET /videos?projectId={projectId}&status={status}&page=1&pageSize=20&sortBy=created&sortOrder=desc
Authorization: Bearer {accessToken}
```

**Query Parameters:**
- `projectId` (optional): Filter by project
- `channelId` (optional): Filter by channel
- `status` (optional): 0-11 (see VideoJobStatus enum), -1 for all
- `page` (default: 1): Page number
- `pageSize` (default: 20): Items per page
- `sortBy` (default: "created"): "created", "title", "views"
- `sortOrder` (default: "desc"): "asc" or "desc"

**Response:**
```json
{
  "data": [
    {
      "id": "323e4567-e89b-12d3-a456-426614174000",
      "topic": "10 Tips for Better Video Marketing",
      "scriptTitle": "Video Marketing Tips 2024",
      "status": 9,
      "currentStep": 8,
      "createdAt": "2024-01-15T10:30:00Z",
      "completedAt": "2024-01-15T11:45:00Z",
      "youtubeUrl": "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
      "views": 1250
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 42,
  "totalPages": 3,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

### Cancel Video Job

```http
POST /videos/{videoJobId}/cancel
Authorization: Bearer {accessToken}
```

### Create Video Variant (A/B Testing)

```http
POST /videos/{parentVideoJobId}/variants
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "parentVideoJobId": "323e4567-e89b-12d3-a456-426614174000",
  "variantLabel": "B",
  "titleOverride": "Alternative Title Version",
  "descriptionOverride": "Alternative description with different keywords"
}
```

---

## Projects

### Create Project

```http
POST /projects
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "name": "Tech Channel Videos",
  "description": "All videos for my tech YouTube channel",
  "defaultLanguage": "en",
  "defaultVoiceId": "21m00Tcm4TlvDq8ikWAM",
  "defaultStyle": "tutorial"
}
```

### List Projects

```http
GET /projects
Authorization: Bearer {accessToken}
```

### Get Project Analytics

```http
GET /projects/{projectId}/analytics
Authorization: Bearer {accessToken}
```

**Response:**
```json
{
  "totalVideos": 24,
  "totalViews": 45230,
  "totalLikes": 1850,
  "averageClickThroughRate": 3.2,
  "averageViewDuration": 245.5,
  "topVideos": [
    {
      "videoJobId": "323e4567-e89b-12d3-a456-426614174000",
      "views": 5230,
      "likes": 245,
      "comments": 48,
      "clickThroughRate": 4.1,
      "averageViewDurationSeconds": 285.0,
      "fetchedAt": "2024-01-15T12:00:00Z"
    }
  ]
}
```

---

## Channels

### Create Channel

```http
POST /channels
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "projectId": "123e4567-e89b-12d3-a456-426614174000",
  "name": "My YouTube Channel"
}
```

### Connect YouTube Account

```http
POST /channels/{channelId}/youtube/connect
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "authorizationCode": "4/0AY0e...",
  "redirectUri": "http://localhost:3000/channels/connect-youtube"
}
```

### List Channels

```http
GET /channels?projectId={projectId}
Authorization: Bearer {accessToken}
```

---

## Billing

### Get Current Subscription

```http
GET /billing/subscription
Authorization: Bearer {accessToken}
```

**Response:**
```json
{
  "id": "423e4567-e89b-12d3-a456-426614174000",
  "plan": "starter",
  "status": 1,
  "currentPeriodStart": "2024-01-01T00:00:00Z",
  "currentPeriodEnd": "2024-01-31T23:59:59Z",
  "cancelAtPeriodEnd": false,
  "monthlyVideoQuota": 5,
  "priceMonthly": 19.99
}
```

### Create Checkout Session

```http
POST /billing/checkout
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "priceId": "price_1234567890"
}
```

**Response:**
```json
{
  "sessionId": "cs_live_1234...",
  "url": "https://checkout.stripe.com/pay/cs_live_1234..."
}
```

### Get Billing Portal URL

```http
GET /billing/portal
Authorization: Bearer {accessToken}
```

---

## WebHooks

### Stripe Webhook Handler

```http
POST /webhooks/stripe
Content-Type: application/json
Stripe-Signature: t={timestamp},v1={signature}

{
  "id": "evt_123456",
  "type": "customer.subscription.updated",
  "data": {
    "object": {
      "id": "sub_123456",
      "customer": "cus_123456",
      "status": "active"
    }
  }
}
```

---

## Status Codes

| Code | Meaning |
|------|---------|
| 200 | OK |
| 201 | Created |
| 204 | No Content |
| 400 | Bad Request |
| 401 | Unauthorized |
| 403 | Forbidden |
| 404 | Not Found |
| 422 | Validation Error |
| 429 | Too Many Requests |
| 500 | Internal Server Error |

## Error Response Format

```json
{
  "error": "INVALID_CREDENTIALS",
  "message": "Invalid email or password.",
  "details": null,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## VideoJobStatus Enum

| Value | Status | Description |
|-------|--------|-------------|
| 0 | Pending | Waiting to be queued |
| 1 | Queued | In Hangfire queue |
| 2 | GeneratingScript | AI script generation |
| 3 | GeneratingScenes | Parsing scenes |
| 4 | SynthesizingVoice | ElevenLabs voice synthesis |
| 5 | FetchingBRoll | Downloading Pexels videos |
| 6 | RenderingVideo | FFmpeg composition |
| 7 | GeneratingThumbnail | Thumbnail creation |
| 8 | Uploading | YouTube upload |
| 9 | Completed | Successfully completed |
| 10 | Failed | Processing failed |
| 11 | Cancelled | User cancelled |

---

## cURL Examples

```bash
# Register
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "tenantName": "My Agency",
    "email": "user@example.com",
    "password": "SecurePassword123!",
    "firstName": "John",
    "lastName": "Doe"
  }'

# Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePassword123!"
  }'

# Create Video (with token)
curl -X POST http://localhost:5000/api/videos \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "projectId": "123e4567-e89b-12d3-a456-426614174000",
    "topic": "My Video Topic",
    "style": "tutorial"
  }'

# List Videos (with pagination)
curl "http://localhost:5000/api/videos?page=1&pageSize=10&sortBy=created&sortOrder=desc" \
  -H "Authorization: Bearer $TOKEN"
```

---

For Postman Collection import, use the following URL pattern or create a Collection from the examples above.
