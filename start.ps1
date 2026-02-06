# FableCraft Docker Startup Script
# Sets FABLECRAFT_PROJECT_PATH and runs docker compose

docker info 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Docker is not running. Please start Docker and try again." -ForegroundColor Red
    exit 1
}

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$env:FABLECRAFT_PROJECT_PATH = $ScriptDir -replace '\\', '/'

Write-Host "Starting FableCraft..."
Write-Host "Project path: $env:FABLECRAFT_PROJECT_PATH"

$serverImage = docker images -q fablecraft-fablecraft-server 2>$null
$clientImage = docker images -q fablecraft-fablecraft-client 2>$null

if (-not $serverImage -or -not $clientImage) {
    Write-Host "Building Docker images (this may take a few minutes on first run)..."
    docker compose build
}

$graphRagImage = docker images -q fablecraft-graph-rag-api 2>$null
if (-not $graphRagImage) {
    Write-Host "Building graph-rag-api image..."
    docker compose build graph-rag-api
}

docker compose up

Write-Host ""
Write-Host "FableCraft is starting. Services will be available at:"
Write-Host "  Frontend:         http://localhost:4200"
Write-Host "  Backend API:      http://localhost:5000"
Write-Host "  GraphRag API:     http://localhost:8111"
Write-Host "  Aspire Dashboard: http://localhost:18888"
