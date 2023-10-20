#!/bin/sh

set -e

echo "Cleaning up Localstack ..."

docker-compose \
    -f ./../.docker/docker-compose.yml \
    stop

docker-compose \
    -f ./../.docker/docker-compose.yml \
    down -v
