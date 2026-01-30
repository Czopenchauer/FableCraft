#!/bin/bash

# FableCraft Docker Startup Script
# Sets FABLECRAFT_PROJECT_PATH and runs docker compose

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

export FABLECRAFT_PROJECT_PATH="$SCRIPT_DIR"

echo "Starting FableCraft..."
echo "Project path: $FABLECRAFT_PROJECT_PATH"

docker compose up

echo ""
echo "FableCraft is starting. Services will be available at:"
echo "  Frontend:         http://localhost:4200"
echo "  Backend API:      http://localhost:5000"
echo "  GraphRag API:     http://localhost:8111"
echo "  Aspire Dashboard: http://localhost:18888"
