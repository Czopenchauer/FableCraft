#!/bin/bash

# FableCraft Docker Startup Script
# Sets FABLECRAFT_PROJECT_PATH and runs docker compose

set -e

if ! docker info > /dev/null 2>&1; then
    echo "Error: Docker is not running. Please start Docker and try again."
    exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

export FABLECRAFT_PROJECT_PATH="$SCRIPT_DIR"

echo "Starting FableCraft..."
echo "Project path: $FABLECRAFT_PROJECT_PATH"

if [ -z "$(docker images -q fablecraft-fablecraft-server 2>/dev/null)" ] || [ -z "$(docker images -q fablecraft-fablecraft-client 2>/dev/null)" ]; then
    echo "Building Docker images (this may take a few minutes on first run)..."
    docker compose build
fi

if [ -z "$(docker images -q fablecraft-graph-rag-api 2>/dev/null)" ]; then
    echo "Building graph-rag-api image..."
    docker compose build graph-rag-api
fi

docker compose up

echo ""
echo "FableCraft is starting. Services will be available at:"
echo "  Frontend:         http://localhost:4200"
echo "  Backend API:      http://localhost:5000"
echo "  GraphRag API:     http://localhost:8111"
echo "  Aspire Dashboard: http://localhost:18888"
