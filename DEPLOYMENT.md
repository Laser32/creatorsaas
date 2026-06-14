# CreatorSaaS Production Deployment Guide

## Pre-Deployment Checklist

- [ ] All tests passing locally
- [ ] Environment variables configured
- [ ] Database migrations tested
- [ ] SSL certificates ready
- [ ] Domain DNS records configured
- [ ] Backups configured
- [ ] Monitoring & alerting setup
- [ ] CI/CD pipeline tested

## Deployment Options

### Option 1: Docker Compose (Single Server)

**Recommended for**: Small to medium deployments (< 1000 users)

#### Prerequisites
- Ubuntu 20.04 LTS or similar
- Docker Engine 20.10+
- Docker Compose 2.0+
- 4GB+ RAM, 50GB+ storage

#### Steps

```bash
# 1. SSH into server
ssh user@production-server

# 2. Clone repository
git clone https://github.com/your-org/creatorsaas.git
cd creatorsaas

# 3. Create production env file
cp .env.example .env
# Edit .env with production values
nano .env

# Required values:
# - ASPNETCORE_ENVIRONMENT=Production
# - JWT_SECRET (new strong key)
# - All API keys (OpenAI, ElevenLabs, Stripe, etc.)
# - Database password (strong)
# - Redis password (if exposed)

# 4. Build and start
docker-compose -f docker-compose.yml up -d

# 5. Verify services
docker-compose ps
docker-compose logs -f api

# 6. Backup setup
docker-compose exec postgres pg_dump -U creatorsaas creatorsaas > backup_$(date +%Y%m%d).sql

# 7. SSL/TLS (Let's Encrypt with Nginx)
# Install certbot and Nginx as reverse proxy
sudo apt-get install certbot python3-certbot-nginx nginx
sudo certbot certonly --nginx -d api.creatorsaas.io -d creatorsaas.io
```

#### Nginx Reverse Proxy Configuration

```nginx
# /etc/nginx/sites-available/creatorsaas

upstream api_backend {
    server 127.0.0.1:5000;
}

upstream frontend_backend {
    server 127.0.0.1:3000;
}

# Redirect HTTP to HTTPS
server {
    listen 80;
    server_name creatorsaas.io api.creatorsaas.io;
    return 301 https://$server_name$request_uri;
}

# HTTPS API
server {
    listen 443 ssl http2;
    server_name api.creatorsaas.io;

    ssl_certificate /etc/letsencrypt/live/api.creatorsaas.io/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/api.creatorsaas.io/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;

    location / {
        proxy_pass http://api_backend;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # Timeouts for long-running requests
        proxy_connect_timeout 60s;
        proxy_send_timeout 300s;
        proxy_read_timeout 300s;
    }
}

# HTTPS Frontend
server {
    listen 443 ssl http2;
    server_name creatorsaas.io;

    ssl_certificate /etc/letsencrypt/live/creatorsaas.io/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/creatorsaas.io/privkey.pem;

    location / {
        proxy_pass http://frontend_backend;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

### Option 2: Kubernetes (Scalable)

**Recommended for**: Large deployments (> 5000 users), requiring scaling

#### Prerequisites
- Kubernetes cluster (1.24+)
- kubectl configured
- Helm 3+ (optional, for package management)
- Container registry access

#### Steps

```bash
# 1. Create namespace
kubectl apply -f k8s/base/configmap.yaml

# 2. Create secrets
kubectl create secret generic creatorsaas-secrets \
  --from-literal=DB_CONNECTION_STRING="..." \
  --from-literal=JWT_SECRET="..." \
  -n creatorsaas

# 3. Create persistent volumes (adjust for your cluster)
kubectl apply -f k8s/base/database.yaml

# 4. Wait for database to be ready
kubectl wait --for=condition=Ready pod \
  -l app=postgres -n creatorsaas --timeout=300s

# 5. Run database migrations
kubectl run migration --rm -i --restart=Never \
  --image=creatorsaas/api:latest \
  -- dotnet ef database update -p CreatorSaaS.Infrastructure \
  -n creatorsaas

# 6. Deploy API
kubectl apply -f k8s/base/api-deployment.yaml

# 7. Deploy Frontend & Ingress
kubectl apply -f k8s/base/frontend-ingress.yaml

# 8. Verify
kubectl get all -n creatorsaas
kubectl logs -n creatorsaas -f deployment/creatorsaas-api

# 9. Setup cert-manager for Let's Encrypt
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.0/cert-manager.yaml

# 10. Create ClusterIssuer for Let's Encrypt
cat <<EOF | kubectl apply -f -
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-prod
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: admin@creatorsaas.io
    privateKeySecretRef:
      name: letsencrypt-prod
    solvers:
    - http01:
        ingress:
          class: nginx
EOF

# 11. Update ingress with cert-manager
# (Already configured in k8s/base/frontend-ingress.yaml)
```

#### Kubernetes Monitoring

```bash
# Pod status
kubectl get pods -n creatorsaas -w

# Service status
kubectl get svc -n creatorsaas

# Resource usage
kubectl top pods -n creatorsaas
kubectl top nodes

# View logs
kubectl logs -n creatorsaas deployment/creatorsaas-api --tail=100 -f

# Describe pod for debugging
kubectl describe pod <pod-name> -n creatorsaas

# Execute command in pod
kubectl exec -it <pod-name> -n creatorsaas -- /bin/bash

# Port forward for debugging
kubectl port-forward svc/creatorsaas-api-svc 5000:5000 -n creatorsaas
```

### Option 3: Cloud Platforms

#### AWS ECS

```bash
# 1. Create ECR repository
aws ecr create-repository --repository-name creatorsaas-api

# 2. Build and push image
docker build -t creatorsaas-api:latest -f docker/Dockerfile.api .
aws ecr get-login-password --region us-east-1 | \
  docker login --username AWS --password-stdin <account-id>.dkr.ecr.us-east-1.amazonaws.com
docker tag creatorsaas-api:latest <account-id>.dkr.ecr.us-east-1.amazonaws.com/creatorsaas-api:latest
docker push <account-id>.dkr.ecr.us-east-1.amazonaws.com/creatorsaas-api:latest

# 3. Create RDS PostgreSQL instance
# Use AWS Console or Terraform

# 4. Create ElastiCache Redis
# Use AWS Console or Terraform

# 5. Create ECS task definition
# (Manually or with Terraform)

# 6. Create ECS service with ALB
# (Manually or with Terraform)
```

#### Heroku (Easiest for small projects)

```bash
# 1. Create app
heroku create creatorsaas-api

# 2. Add buildpacks
heroku buildpacks:add --index=1 heroku/dotnet
heroku buildpacks:add --index=2 heroku/nodejs

# 3. Configure env variables
heroku config:set ASPNETCORE_ENVIRONMENT=Production
heroku config:set JWT_SECRET="your-secret-key"
# ... all other variables

# 4. Add PostgreSQL
heroku addons:create heroku-postgresql:standard-0

# 5. Add Redis
heroku addons:create heroku-redis:premium-0

# 6. Deploy
git push heroku main

# 7. Run migrations
heroku run "cd CreatorSaaS.Infrastructure && dotnet ef database update -s ../CreatorSaaS.API"

# 8. View logs
heroku logs -t
```

## Post-Deployment

### 1. Health Checks

```bash
# API health
curl https://api.creatorsaas.io/health

# Database connectivity
curl https://api.creatorsaas.io/health/db

# Redis connectivity
curl https://api.creatorsaas.io/health/cache
```

### 2. SSL Certificate Renewal

```bash
# Automatic renewal (should be setup)
sudo systemctl enable certbot-renewal.timer

# Manual renewal if needed
sudo certbot renew --dry-run
sudo certbot renew

# For Kubernetes with cert-manager, automatic
```

### 3. Backup Strategy

```bash
# Daily PostgreSQL backup
0 2 * * * /usr/local/bin/backup-postgres.sh

# Backup script example
#!/bin/bash
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
docker-compose exec -T postgres pg_dump -U creatorsaas creatorsaas | gzip > /backups/creatorsaas_$TIMESTAMP.sql.gz

# Keep last 30 days
find /backups -name "creatorsaas_*.sql.gz" -mtime +30 -delete
```

### 4. Monitoring & Alerting

```bash
# Setup Prometheus + Grafana
docker-compose -f docker-compose.monitoring.yml up -d

# Key metrics to monitor:
# - API response time (p95 < 200ms)
# - Error rate (< 0.5%)
# - Database query time (p95 < 100ms)
# - Redis memory usage (< 80% of max)
# - Hangfire job queue length (< 100)
# - Worker pod CPU (< 70%)
```

### 5. Log Aggregation

```bash
# Setup ELK Stack
# - Elasticsearch (logs storage)
# - Logstash (log processing)
# - Kibana (visualization)

# Configure app to send logs to ELK
# Update Serilog configuration in Program.cs
```

## Disaster Recovery

### Database Restore

```bash
# From SQL backup
psql -h localhost -U creatorsaas creatorsaas < backup.sql

# From Docker
docker exec creatorsaas_postgres psql -U creatorsaas creatorsaas < backup.sql
```

### Rollback

```bash
# Docker Compose
git checkout <previous-tag>
docker-compose down
docker-compose build
docker-compose up -d

# Kubernetes
kubectl rollout undo deployment/creatorsaas-api -n creatorsaas
kubectl rollout undo deployment/creatorsaas-frontend -n creatorsaas
```

## Performance Tuning

### Database Optimization

```sql
-- Create indexes for frequent queries
CREATE INDEX idx_videos_tenant_status ON video_jobs(tenant_id, status);
CREATE INDEX idx_scenes_job_order ON video_scenes(video_job_id, "order");

-- VACUUM analysis
VACUUM ANALYZE;

-- Connection pooling (EF Core default: min 10, max 30)
```

### Redis Optimization

```bash
# Monitor memory
redis-cli INFO memory

# Configure maxmemory policy
redis-cli CONFIG SET maxmemory-policy allkeys-lru

# Enable persistence
appendonly yes
```

### API Performance

```csharp
// Add response caching
app.UseResponseCaching();

// Add compression
app.UseResponseCompression();

// Configure Kestrel
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 100;
    options.Limits.MaxConcurrentUpgradedConnections = 100;
});
```

## Maintenance

### Regular Tasks

- **Daily**: Monitor error logs, check disk space
- **Weekly**: Review slow query logs, check backup integrity
- **Monthly**: Review performance metrics, update dependencies
- **Quarterly**: Security audit, penetration testing

### Security Hardening

- [ ] Enable WAF (Web Application Firewall)
- [ ] Configure DDoS protection
- [ ] Implement rate limiting
- [ ] Setup HSTS headers
- [ ] Configure CORS properly
- [ ] Use security headers (CSP, X-Frame-Options, etc.)
- [ ] Regular dependency updates
- [ ] Secrets rotation

## Troubleshooting

### API not responding

```bash
# Check container status
docker-compose ps

# View logs
docker-compose logs api | tail -50

# Check port binding
netstat -tuln | grep 5000

# Test connection
curl -v http://localhost:5000/health
```

### Database connection issues

```bash
# Check PostgreSQL
docker-compose exec postgres psql -U creatorsaas -c "SELECT 1"

# Check password
echo "creatorsaas:password@postgres:5432" | nc -zv postgres 5432

# Connection string format
Host=postgres;Port=5432;Database=creatorsaas;Username=creatorsaas;Password=***
```

### Redis issues

```bash
# Check Redis
docker-compose exec redis redis-cli ping

# Monitor commands
redis-cli MONITOR

# Check memory
redis-cli INFO memory
```

### Job queue backlog

```bash
# View Hangfire dashboard
# http://localhost:5000/hangfire

# Kill stuck job
redis-cli DEL "hangfire:job:{jobId}"
```

## Support & Escalation

- **Critical Issues**: PagerDuty alert → on-call engineer
- **Production Outages**: War room call + incident response
- **Security Issues**: CERT team notification
- **Data Loss**: Restore from backup + incident report

---

**Deployment Checklist Last Updated**: 2024  
**Supported Environments**: Linux, Kubernetes, AWS, Heroku
