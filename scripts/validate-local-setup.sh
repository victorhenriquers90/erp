#!/bin/bash

# STEP 1: Local Testing Validation Script
# Usage: ./validate-local-setup.sh

set -e

echo "═══════════════════════════════════════════════════════════════════════════════"
echo "STEP 1: LOCAL TESTING WITH DOCKER COMPOSE - VALIDATION"
echo "═══════════════════════════════════════════════════════════════════════════════"
echo

# Check prerequisites
echo "1️⃣ Checking Prerequisites..."
echo

# Check Docker
if ! command -v docker &> /dev/null; then
    echo "❌ Docker not found. Please install Docker first."
    echo "   https://docs.docker.com/get-docker/"
    exit 1
fi
echo "✅ Docker is installed: $(docker --version)"

# Check Docker Compose
if ! command -v docker-compose &> /dev/null; then
    echo "❌ Docker Compose not found. Please install Docker Compose."
    echo "   https://docs.docker.com/compose/install/"
    exit 1
fi
echo "✅ Docker Compose is installed: $(docker-compose --version)"

# Check .env file
echo
echo "2️⃣ Checking Environment Configuration..."
echo

if [ ! -f .env ]; then
    echo "⚠️  .env file not found. Creating from template..."
    if [ ! -f .env.example ]; then
        echo "❌ .env.example template not found"
        exit 1
    fi
    cp .env.example .env
    echo "✅ Created .env from template"
    echo "⚠️  Please update .env with your actual values (passwords, keys, etc.)"
else
    echo "✅ .env file exists"
fi

# Validate docker-compose.yml
echo
echo "3️⃣ Validating docker-compose.yml..."
echo

if ! docker-compose config --quiet; then
    echo "❌ docker-compose.yml has errors"
    exit 1
fi
echo "✅ docker-compose.yml is valid"

# Check required services in docker-compose.yml
echo
echo "4️⃣ Checking Docker Compose Services..."
echo

required_services=("api" "sqlserver" "redis")
for service in "${required_services[@]}"; do
    if grep -q "^  $service:" docker-compose.yml; then
        echo "✅ Service '$service' is configured"
    else
        echo "❌ Service '$service' is missing"
        exit 1
    fi
done

# Check image availability
echo
echo "5️⃣ Checking Docker Images..."
echo

if docker images | grep -q "aspnet:8.0"; then
    echo "✅ ASP.NET Core 8.0 image is available"
else
    echo "⚠️  ASP.NET Core 8.0 image will be pulled on first run"
fi

if docker images | grep -q "mssql/server:2022"; then
    echo "✅ SQL Server 2022 image is available"
else
    echo "⚠️  SQL Server 2022 image will be pulled on first run"
fi

if docker images | grep -q "redis:7"; then
    echo "✅ Redis 7 image is available"
else
    echo "⚠️  Redis 7 image will be pulled on first run"
fi

# Check disk space
echo
echo "6️⃣ Checking Disk Space..."
echo

available_space=$(df . | awk 'NR==2 {print $4}')
required_space=$((3000000))  # ~3GB in KB

if [ "$available_space" -gt "$required_space" ]; then
    space_gb=$((available_space / 1024 / 1024))
    echo "✅ Sufficient disk space available (~${space_gb}GB)"
else
    echo "❌ Insufficient disk space. At least 3GB required."
    exit 1
fi

# Summary
echo
echo "═══════════════════════════════════════════════════════════════════════════════"
echo "✅ ALL VALIDATIONS PASSED!"
echo "═══════════════════════════════════════════════════════════════════════════════"
echo

echo "NEXT STEPS:"
echo "1. Update .env file with production values (if needed)"
echo "2. Run: docker-compose up"
echo "3. Wait for all services to start (2-3 minutes)"
echo "4. Test API: curl http://localhost:5000/health"
echo "5. View Swagger: http://localhost:5000/swagger"
echo
echo "TO START DEVELOPMENT:"
echo "  docker-compose up -d"
echo
echo "TO VIEW LOGS:"
echo "  docker-compose logs -f api"
echo
echo "TO RUN TESTS:"
echo "  docker-compose run tests"
echo
echo "TO STOP ALL SERVICES:"
echo "  docker-compose down"
echo

exit 0
