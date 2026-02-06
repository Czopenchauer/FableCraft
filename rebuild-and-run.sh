#!/bin/bash

# Rebuild client and server images, then start with force recreate

set -e

if ! docker info > /dev/null 2>&1; then
    echo "Error: Docker is not running. Please start Docker and try again."
    exit 1
fi

if [ -z "$(docker images -q fablecraft-graph-rag-api 2>/dev/null)" ]; then
    echo "Building graph-rag-api image..."
    docker-compose build graph-rag-api
fi

echo "Rebuilding fablecraft-server and fablecraft-client images..."

docker-compose build --no-cache fablecraft-server fablecraft-client

echo "Starting docker-compose with force recreate..."

docker-compose up --force-recreate

echo "Services started."
echo "Frontend available at: http://localhost:4200"
