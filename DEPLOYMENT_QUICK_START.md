# DEPLOYMENT QUICK START GUIDE

## 1. LOCAL DEVELOPMENT SETUP

### Prerequisites
- Docker & Docker Compose installed
- Git installed
- .NET 8.0 SDK (optional, for local development without Docker)

### Start Development Environment
```bash
# Navigate to project directory
cd projeto

# Copy environment template
cp .env.example .env

# Start all services (API, SQL Server, Redis)
docker-compose up

# In another terminal, run tests
docker-compose run tests
```

### Access Points
- **API:** http://localhost:5000
- **Swagger UI:** http://localhost:5000/swagger
- **Health Check:** http://localhost:5000/health
- **SQL Server:** localhost:1433 (user: sa, password from .env)
- **Redis:** localhost:6379

---

## 2. ENABLE CACHING IN PRODUCTION

### appsettings.Production.json
```json
{
  "Redis": {
    "Connection": "redis-service.default.svc.cluster.local:6379",
    "Database": 0,
    "Enabled": true
  }
}
```

### Docker Environment Variable
```bash
docker run -e "Redis__Enabled=true" \
           -e "Redis__Connection=redis-host:6379" \
           projetovarejo-api
```

### Cache Monitoring
```bash
# Check token cache stats via API
curl http://localhost:5000/api/cache-stats  # Endpoint needed

# View Redis metrics
redis-cli INFO stats

# Monitor cache performance
redis-cli MONITOR
```

---

## 3. MONITORING & LOGGING

### View Logs
```bash
# Docker logs
docker logs -f projetovarejo-api

# Kubernetes logs
kubectl logs -f deployment/projetovarejo-api

# Local file logs
tail -f logs/api-2026-05-26.log
```

### Health Checks
```bash
# Overall health
curl http://localhost:5000/health

# Readiness (can serve requests)
curl http://localhost:5000/health/ready

# Liveness (process is running)
curl http://localhost:5000/health/live

# Response format
{
  "status": "Healthy",
  "timestamp": "2026-05-26T10:30:00Z",
  "checks": {
    "database": {"status": "Healthy"},
    "redis": {"status": "Healthy"}
  }
}
```

### Search Logs
```bash
# Find all login attempts
grep -i "login" logs/api-*.log

# Find errors
grep "ERROR\|Exception" logs/api-*.log

# Find slow queries (>100ms)
grep "Slow Query" logs/api-*.log

# Follow logs in real-time
tail -f logs/api-*.log | grep ERROR
```

---

## 4. DOCKER DEPLOYMENT

### Build Docker Image
```bash
# Build locally
docker build -t projetovarejo:latest .

# Build with version tag
docker build -t projetovarejo:1.0.0 .

# Push to registry
docker tag projetovarejo:latest ghcr.io/your-org/projetovarejo:latest
docker push ghcr.io/your-org/projetovarejo:latest
```

### Run Docker Container
```bash
docker run -d \
  --name projetovarejo-api \
  -p 80:80 \
  -e "ASPNETCORE_ENVIRONMENT=Production" \
  -e "ConnectionStrings__Default=Server=sqlserver;Database=ProjetoVarejo;User Id=sa;Password=YourPassword123!" \
  -e "Jwt__SecretKey=your-secret-key-min-32-chars" \
  -e "Redis__Enabled=true" \
  -e "Redis__Connection=redis:6379" \
  --healthcheck-interval=30s \
  projetovarejo:latest
```

### Docker Compose Production
```bash
# Start with production override
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f api
```

---

## 5. KUBERNETES DEPLOYMENT

### Prerequisites
- Kubernetes cluster (v1.20+)
- kubectl configured
- cert-manager installed (for TLS)

### Deploy to Kubernetes
```bash
# Apply all manifests
kubectl apply -f k8s/

# Verify deployment
kubectl get pods -l app=projetovarejo-api
kubectl get svc projetovarejo-api
kubectl get hpa projetovarejo-api-hpa

# Check pod logs
kubectl logs -f pod/projetovarejo-api-xxxx

# Port forward for local testing
kubectl port-forward svc/projetovarejo-api 8080:80
curl http://localhost:8080/health
```

### Update Deployment
```bash
# Update image tag
kubectl set image deployment/projetovarejo-api \
  api=ghcr.io/your-org/projeto-varejo/api:v2.0.0

# Check rollout status
kubectl rollout status deployment/projetovarejo-api

# Rollback if needed
kubectl rollout undo deployment/projetovarejo-api
```

### Monitor Auto-Scaling
```bash
# Watch HPA
kubectl get hpa -w

# Check resource usage
kubectl top nodes
kubectl top pods -l app=projetovarejo-api

# View HPA events
kubectl describe hpa projetovarejo-api-hpa
```

---

## 6. GITHUB ACTIONS CI/CD

### Push Triggers CI
```bash
# Commit changes
git add .
git commit -m "feat: add new feature"

# Push to main (triggers CI)
git push origin main

# View workflow
# Go to: https://github.com/your-org/projeto/actions
```

### Manual Deployment
```bash
# Create release (triggers deploy workflow)
git tag v1.0.0
git push origin v1.0.0

# Or use GitHub UI
# Go to Releases > Create New Release > v1.0.0

# Manual workflow trigger
# Go to Actions > Deploy > Run workflow
# Select environment (staging or production)
```

### Workflow Status
```bash
# View CI/CD logs
gh run list
gh run view <run-id>
gh run view <run-id> --log

# Check deployment status
gh deployment list
gh deployment status --environment production
```

---

## 7. DATABASE SETUP

### Create Indexes for Performance
```sql
-- Connect to production database
USE ProjetoVarejo;

CREATE INDEX IX_Usuario_Login ON dbo.Usuarios(Login);
CREATE INDEX IX_Venda_DataCriacao ON dbo.Vendas(DataCriacao);
CREATE INDEX IX_Venda_Status ON dbo.Vendas(Status);
CREATE INDEX IX_Produto_CategoriaId ON dbo.Produtos(CategoriaId);
CREATE INDEX IX_ItemVenda_VendaId ON dbo.ItemVendas(VendaId);
CREATE INDEX IX_Lancamento_DataRegistro ON dbo.Lancamentos(DataRegistro);
CREATE INDEX IX_CaixaSessao_DataAbertura ON dbo.CaixaSessoes(DataAbertura);
```

### Run Migrations
```bash
# Using dotnet CLI
dotnet ef database update --project src/ProjetoVarejo.Api

# Using Package Manager Console
Update-Database -Project ProjetoVarejo.Api

# Verify migrations
SELECT * FROM [dbo].[__EFMigrationsHistory];
```

---

## 8. PERFORMANCE OPTIMIZATION

### Enable Caching Statistics
```bash
# Add endpoint to check cache performance
curl http://localhost:5000/api/cache-stats

# Response
{
  "tokenCacheHits": 4523,
  "tokenCacheMisses": 187,
  "hitRatio": 0.96,
  "memory": "2.4MB"
}
```

### Monitor Response Times
```bash
# Using Apache Bench
ab -n 100 -c 10 http://localhost:5000/api/vendas

# Using wrk (better for load testing)
wrk -t4 -c100 -d30s http://localhost:5000/api/vendas

# Using hey
hey -n 1000 -c 50 http://localhost:5000/api/vendas
```

### Database Query Performance
```sql
-- View slowest queries
SELECT TOP 20 
  creation_time,
  last_execution_time,
  execution_count,
  total_elapsed_time / execution_count as avg_time_ms,
  query_hash,
  text
FROM sys.dm_exec_cached_plans
CROSS APPLY sys.dm_exec_sql_text(plan_handle)
WHERE database_id = DB_ID('ProjetoVarejo')
ORDER BY total_elapsed_time DESC;
```

---

## 9. TROUBLESHOOTING

### Health Check Fails
```bash
# Check Redis connection
redis-cli ping

# Check database connection
sqlcmd -S localhost -U sa -P YourPassword123! -Q "SELECT 1"

# Check logs
docker logs projetovarejo-api | grep ERROR
```

### High Memory Usage
```bash
# Check memory metrics
docker stats projetovarejo-api

# Kubernetes memory
kubectl top pods -l app=projetovarejo-api

# Restart if needed
docker restart projetovarejo-api
# or
kubectl rollout restart deployment/projetovarejo-api
```

### Cache Issues
```bash
# Clear Redis cache
redis-cli FLUSHDB

# Check cache contents
redis-cli KEYS "*"

# Disable caching temporarily
# Set Redis__Enabled=false in environment
```

### Slow Queries
```bash
# Check logs for "Slow Query"
grep "Slow Query" logs/api-*.log

# Run query analyzer
DBCC SHOW_STATISTICS

# Check index fragmentation
SELECT * FROM sys.dm_db_index_physical_stats(
  DB_ID('ProjetoVarejo'), NULL, NULL, NULL, 'LIMITED'
)
WHERE avg_fragmentation_in_percent > 10;
```

---

## 10. PRODUCTION CHECKLIST

### Before Production Deployment
- [ ] Environment variables set correctly
- [ ] Database backups configured
- [ ] Certificates installed (TLS/HTTPS)
- [ ] Health checks responding
- [ ] Logging to file/Application Insights
- [ ] Redis cluster configured
- [ ] Database indexes created
- [ ] Load test passed (100 concurrent users)
- [ ] Monitoring/alerting configured
- [ ] Incident response plan documented
- [ ] Rollback procedure tested

### Post-Deployment Verification
- [ ] API responding on public URL
- [ ] Health check: `/health` returns 200
- [ ] Can login and authenticate
- [ ] Data persists across restarts
- [ ] Cache working (check response times)
- [ ] Logs being written
- [ ] Metrics in Application Insights
- [ ] Scaling working (run load test)
- [ ] No errors in recent logs
- [ ] Team notified of deployment

---

## 11. USEFUL COMMANDS REFERENCE

```bash
# Docker
docker build -t projetovarejo:latest .
docker run -d projetovarejo:latest
docker ps
docker logs -f container-id
docker exec -it container-id /bin/bash

# Docker Compose
docker-compose up -d
docker-compose down
docker-compose logs -f api
docker-compose run tests

# Kubernetes
kubectl apply -f k8s/
kubectl get pods
kubectl logs -f pod-name
kubectl port-forward svc/projetovarejo-api 8080:80
kubectl rollout restart deployment/projetovarejo-api

# Git & GitHub
git push origin main  # Triggers CI
git tag v1.0.0 && git push origin v1.0.0  # Triggers deploy
gh run list
gh run view run-id

# Redis
redis-cli ping
redis-cli KEYS "*"
redis-cli FLUSHDB
redis-cli --stat

# Database
dotnet ef database update
sqlcmd -S server -U user -P password
```

---

## Support & Documentation

- **Full Implementation Guide:** `PHASE_10_11_12_IMPLEMENTATION_SUMMARY.md`
- **Health Checks:** http://localhost:5000/health
- **API Documentation:** http://localhost:5000/swagger
- **Logs Directory:** `./logs/`
- **Kubernetes Manifests:** `./k8s/`
- **CI/CD Workflows:** `.github/workflows/`

---

**Version:** 1.0  
**Last Updated:** 2026-05-26  
**Status:** Production Ready ✅
