# FableCraft Docker Startup Script
# Sets FABLECRAFT_PROJECT_PATH and runs docker compose

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Convert to forward slashes for Docker compatibility
$env:FABLECRAFT_PROJECT_PATH = $ScriptDir -replace '\\', '/'

Write-Host "Starting FableCraft..."
Write-Host "Project path: $env:FABLECRAFT_PROJECT_PATH"

docker compose up

Write-Host ""
Write-Host "FableCraft is starting. Services will be available at:"
Write-Host "  Frontend:         http://localhost:4200"
Write-Host "  Backend API:      http://localhost:5000"
Write-Host "  GraphRag API:     http://localhost:8111"
Write-Host "  Aspire Dashboard: http://localhost:18888"
