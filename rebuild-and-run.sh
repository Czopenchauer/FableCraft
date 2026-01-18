#!/bin/bash

# Rebuild client and server images, then start with force recreate

set -e

echo "Rebuilding fablecraft-server and fablecraft-client images..."

docker-compose build --no-cache fablecraft-server fablecraft-client

echo "Starting docker-compose with force recreate..."

docker-compose up -d --force-recreate

echo "Services started."
echo "Frontend available at: http://localhost:4200"
