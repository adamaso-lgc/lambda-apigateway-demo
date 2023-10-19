#!/bin/bash

# Create the Lambda function
aws --endpoint-url=http://localhost:4566 lambda create-function \
  --function-name LambdaDemo \
  --runtime dotnet6 \
  --handler LambdaDemo::LambdaDemo.Function::FunctionHandler \
  --role arn:aws:iam::123456:role/execution_role \
  --zip-file fileb:///tmp/demo_function.zip \

# Create the API Gateway
aws --endpoint-url=http://localhost:4566 apigateway create-rest-api \
  --name HealthDataAPI \
  --description "API for wearable sensor data"

  
# Retrieve the API Gateway ID
api_id=$(aws --endpoint-url=http://localhost:4566 apigateway get-rest-apis \
  --query 'items[?name==`HealthDataAPI`].id' \
  --output text)

# Create a root resource (e.g., '/')
root_resource_id=$(aws --endpoint-url=http://localhost:4566 apigateway create-resource \
  --rest-api-id $api_id \
  --parent-id $(aws --endpoint-url=http://localhost:4566 apigateway get-resources --rest-api-id $api_id \
  --query 'items[?path==`/`].id' --output text) \
  --path-part "data" \
  --query 'id' \
  --output text)

# Create the POST method on the '/data' resource
aws --endpoint-url=http://localhost:4566 apigateway put-method \
  --rest-api-id $api_id \
  --resource-id $root_resource_id \
  --http-method POST \
  --authorization-type "NONE"

# Integrate the POST method with the Lambda function
aws --endpoint-url=http://localhost:4566 apigateway put-integration \
  --rest-api-id $api_id \
  --resource-id $root_resource_id \
  --http-method POST \
  --type AWS_PROXY \
  --integration-http-method POST \
  --uri arn:aws:apigateway:us-east-1:lambda:path/2015-03-31/functions/arn:aws:lambda:us-east-1:000000000000:function:LambdaDemo/invocations

# Deploy the API
aws --endpoint-url=http://localhost:4566 apigateway create-deployment \
  --rest-api-id $api_id \
  --stage-name test
