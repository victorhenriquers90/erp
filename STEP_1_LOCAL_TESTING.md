# STEP 1: Local Testing with Docker Compose

**Status:** 🔄 Ready for Execution (Docker Required)  
**Estimated Time:** 5-10 minutes (first run takes longer due to image pulls)  
**Prerequisites:** Docker & Docker Compose installed

---

## Overview

In this step, we will:
1. Validate the local Docker environment
2. Start all services (API, SQL Server, Redis)
3. Verify all services are healthy
4. Test API endpoints
5. Confirm everything is working

---

## Pre-Flight Checks

### 1. Verify Docker Installation
```bash
docker --version
# Expected: Docker version 20.10+

docker-compose --version
# Expected: Docker Compose version 1.29+
```

### 2. Check Available Disk Space
```bash
df -h
# Need: At least 3GB free space
```

### 3. Verify Required Files
```bash
# Check these files exist:
ls -la docker-compose.yml
ls -la .env.example
ls -la Dockerfile
ls -la Dockerfile.tests
```

---

## Step-by-Step Execution

### Step 1.1: Prepare Environment
```bash
cd /path/to/projeto

# Copy environment template
cp .env.example .env

# Update .env with your values (optional for local dev)
# - MSSQL_SA_PASSWORD
# - JWT_SECRET_KEY
# - REDIS_ENABLED=true

cat .env
```

### Step 1.2: Validate Configuration
```bash
# Validate docker-compose.yml syntax
docker-compose config --quiet
# Output: (no error = success)

# Or run the validation script
chmod +x scripts/validate-local-setup.sh
./scripts/validate-local-setup.sh
```

### Step 1.3: Start Services (First Time)
```bash
# Pull images and start all services
docker-compose up

# OR run in background
docker-compose up -d

# Expected output:
# Creating projetovarejo-sqlserver ... done
# Creating projetovarejo-redis    ... done
# Creating projetovarejo-api      ... done
```

### Step 1.4: Wait for Health Checks

The first run takes 2-3 minutes as it pulls base images. Watch for:
```
projetovarejo-api         | info: ProjetoVarejo.Api starting
projetovarejo-sqlserver   | [1] 2026-05-26 10:30:00.12 Server is listening on port 1433
projetovarejo-redis       | * Ready to accept connections
```

---

## Service Status Verification

### Check All Services Running
```bash
docker-compose ps

# Expected output:
# NAME                        COMMAND                  STATE
# projetovarejo-api           "dotnet ProjetoVar..."   Up (healthy)
# projetovarejo-sqlserver     "/opt/mssql/bin/..."     Up (healthy)
# projetovarejo-redis         "redis-server..."        Up (healthy)
```

### Check Service Logs
```bash
# API logs
docker-compose logs -f api

# SQL Server logs
docker-compose logs -f sqlserver

# Redis logs
docker-compose logs -f redis

# All services
docker-compose logs -f
```

---

## API Testing

### Test 1: Health Check
```bash
# Check overall health
curl http://localhost:5000/health

# Expected response (200 OK):
{
  "status": "Healthy",
  "timestamp": "2026-05-26T10:30:00Z",
  "checks": {
    "database": {"status": "Healthy"},
    "redis": {"status": "Healthy"},
    "memory": {"status": "Healthy"}
  }
}
```

### Test 2: Readiness Probe
```bash
curl http://localhost:5000/health/ready

# Expected: 200 OK (ready to handle requests)
```

### Test 3: Liveness Probe
```bash
curl http://localhost:5000/health/live

# Expected: 200 OK (process is alive)
```

### Test 4: Swagger Documentation
```bash
# Open in browser:
http://localhost:5000/swagger

# Expected: Swagger UI with all endpoints documented
```

### Test 5: Authentication Endpoint
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "usuario": "admin",
    "senha": "senha123"
  }'

# Expected response (200 OK):
{
  "success": true,
  "data": {
    "usuarioId": 1,
    "usuarioNome": "Administrador",
    "token": "eyJhbGc...",
    "refreshToken": "eyJhbGc...",
    "expiresIn": 3600
  }
}
```

### Test 6: Protected Endpoint
```bash
# First, get a token
TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"usuario":"admin","senha":"senha123"}' \
  | grep -o '"token":"[^"]*' | cut -d'"' -f4)

# Use token to access protected endpoint
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5000/api/vendas

# Expected: 200 OK with vendas list
```

---

## Database Verification

### Connect to SQL Server
```bash
# Connect using SQL Server Management Studio:
# Server: localhost,1433
# Username: sa
# Password: (from .env MSSQL_SA_PASSWORD)

# Or via command line:
sqlcmd -S localhost -U sa -P YourPassword123! -Q "SELECT @@VERSION"
```

### Verify Database
```sql
-- Check database
USE ProjetoVarejo;

-- List tables
SELECT * FROM INFORMATION_SCHEMA.TABLES;

-- Check users
SELECT * FROM Usuarios;

-- Expected: 4 test users (admin, gerente, caixa, estoquista)
```

---

## Redis Verification

### Check Redis Connection
```bash
redis-cli ping
# Expected: PONG

redis-cli INFO stats
# Shows: used_memory, total_commands_processed

redis-cli KEYS "*"
# Shows: Any cached keys (may be empty initially)
```

---

## Running Integration Tests

### Run Tests via Docker Compose
```bash
# Run full test suite with coverage
docker-compose run tests

# Expected output:
# Test run complete
# Total tests: 160+
# Passed: 160+
# Failed: 0
# Skipped: 0
```

### View Test Results
```bash
# Results are saved to TestResults/
docker-compose logs tests | tail -50
```

---

## Troubleshooting

### Issue: Container won't start
```bash
# Check logs
docker-compose logs api

# Common causes:
# 1. Port 5000 already in use: lsof -i :5000
# 2. Not enough memory: docker system prune
# 3. Permission issues: sudo chown -R $USER:$USER .
```

### Issue: Database connection fails
```bash
# Check SQL Server health
docker-compose ps sqlserver

# Check logs
docker-compose logs sqlserver

# Verify connection
sqlcmd -S localhost -U sa -P password
```

### Issue: Redis connection fails
```bash
# Check Redis health
docker-compose logs redis

# Test connection
redis-cli -h localhost ping
```

### Issue: Low memory/disk space
```bash
# Clean up Docker resources
docker system prune
docker volume prune

# Check available space
df -h
```

---

## Stop Services

### Graceful Shutdown
```bash
# Stop all services (keep volumes)
docker-compose stop

# Start again later
docker-compose start
```

### Full Cleanup
```bash
# Stop and remove containers (keep volumes)
docker-compose down

# Stop, remove containers AND volumes
docker-compose down -v

# Remove images too
docker-compose down -v --rmi all
```

---

## Success Criteria ✅

- [ ] docker-compose.yml validates successfully
- [ ] All three services start without errors
- [ ] `/health` endpoint returns 200 OK
- [ ] `/health/ready` returns 200 OK
- [ ] `/health/live` returns 200 OK
- [ ] Swagger UI loads at `/swagger`
- [ ] Can authenticate with `admin/senha123`
- [ ] Protected endpoints require valid token
- [ ] Database contains test data (4 users)
- [ ] Redis is accessible via redis-cli
- [ ] Tests pass via `docker-compose run tests`

---

## Performance Baseline (STEP 1)

**Metrics to track before optimization:**
```bash
# Response time for GET request
time curl http://localhost:5000/api/vendas

# Typical without caching: 50-200ms
# With caching enabled: 5-20ms

# Database query time
# Check logs for query duration

# Memory usage
docker stats projetovarejo-api
# Typical: 150-300MB

# Disk usage
du -sh .
# Typical: 5-10GB (with images)
```

---

## Next Steps

After STEP 1 is successful:
- ✅ **STEP 1:** Local Testing (CURRENT)
- ⏭️ **STEP 2:** Production Deployment
- ⏭️ **STEP 3:** Monitoring Setup
- ⏭️ **STEP 4:** Performance Tuning

---

## Documentation Reference

- **Complete Setup:** `DEPLOYMENT_QUICK_START.md`
- **Troubleshooting:** `PHASE_10_11_12_IMPLEMENTATION_SUMMARY.md`
- **API Docs:** `http://localhost:5000/swagger`
- **Health Checks:** `http://localhost:5000/health`

---

**Status:** Ready for local testing execution ✅
