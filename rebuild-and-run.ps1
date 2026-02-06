# Rebuild client and server images, then start with force recreate

docker info 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Docker is not running. Please start Docker and try again." -ForegroundColor Red
    exit 1
}

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$env:FABLECRAFT_PROJECT_PATH = $ScriptDir -replace '\\', '/'

Write-Host "Project path: $env:FABLECRAFT_PROJECT_PATH"

$graphRagImage = docker images -q fablecraft-graph-rag-api 2>$null
if (-not $graphRagImage) {
    Write-Host "Building graph-rag-api image..." -ForegroundColor Cyan
    docker-compose build graph-rag-api
}

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
