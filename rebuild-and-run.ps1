# Rebuild client and server images, then start with force recreate

Write-Host "Rebuilding fablecraft-server and fablecraft-client images..." -ForegroundColor Cyan

docker-compose build --no-cache fablecraft-server fablecraft-client

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Starting docker-compose with force recreate..." -ForegroundColor Cyan

docker-compose up --force-recreate

if ($LASTEXITCODE -eq 0) {
    Write-Host "Services started." -ForegroundColor Green
    Write-Host "Frontend available at: http://localhost:4200" -ForegroundColor Yellow
} else {
    Write-Host "Failed to start services." -ForegroundColor Red
    exit 1
}
