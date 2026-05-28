# Start Docker Compose and test STEP 1
cd C:\Users\victo\Documents\projeto

echo "Starting Docker Compose..."
docker-compose up -d

echo "Waiting 30 seconds for services to start..."
Start-Sleep -Seconds 30

echo "=== Service Status ==="
docker-compose ps

echo ""
echo "=== Testing API Health Check ==="
$maxAttempts = 5
$attempt = 0
while ($attempt -lt $maxAttempts) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000/health" -UseBasicParsing -ErrorAction Stop
        Write-Host "✅ API Health Check: SUCCESS" -ForegroundColor Green
        Write-Host $response.Content
        break
    } catch {
        $attempt++
        Write-Host "Attempt $attempt/$maxAttempts - Waiting for API to be ready..." -ForegroundColor Yellow
        Start-Sleep -Seconds 5
    }
}

echo ""
echo "=== STEP 1 VERIFICATION COMPLETE ==="
echo "If health check passed, your Docker Compose setup is working correctly!"
