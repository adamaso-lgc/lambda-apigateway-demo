#!/bin/sh

set -e

framework="net6.0"   # change to "net6.0" for .NET 6.0 version once supported by Localstack

echo "Publishing lambda..."
dotnet publish ./../src/Demo.Lambda/ -f $framework &> /dev/null
echo "Publishing lambda done!"

echo "Packaging lambda function..."
7z a -tzip ./../.docker/tmp/demo_function.zip ./../src/Demo.Lambda/bin/Debug/$framework/publish/* &> /dev/null
echo "Packaging lambda function done!"

echo "Starting Localstack ..."
docker-compose \
    -f ./../.docker/docker-compose.yml \
    rm -svf
docker-compose \
    -f ./../.docker/docker-compose.yml \
    up --abort-on-container-exit --timeout 20  --remove-orphans