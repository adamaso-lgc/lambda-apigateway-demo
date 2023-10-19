#!/bin/sh

set -e

framework="net6.0"

echo "Publishing lambda..."
dotnet publish ./../src/Demo.Lambda/ -f $framework 
echo "Publishing lambda done!"

echo "Packaging lambda function..."
7z a -tzip ./../.docker/tmp/demo_function.zip ./../src/Demo.Lambda/bin/Debug/$framework/publish/* &> /dev/null
echo "Packaging lambda function done!"

echo "Updating Lambda function..."

aws --endpoint-url=http://localhost:4566 lambda update-function-code \
    --function-name LambdaDemo \
    --zip-file fileb://../.docker/tmp/demo_function.zip #&> /dev/null

echo "Lambda function updated!"