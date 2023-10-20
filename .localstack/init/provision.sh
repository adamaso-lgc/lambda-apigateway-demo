#!/bin/sh

#
#   INFO:
#       This scrip will be executed *inside* the localstack container once all its services are up and running
#

set -e

export AWS_DEFAULT_REGION=eu-west-1
export AWS_ACCESS_KEY_ID=test
export AWS_SECRET_ACCESS_KEY=test

endpoint="--endpoint-url=http://localhost:4566"

 echo "Creating Lambda execution role..."
 aws $endpoint iam create-role --role-name LambdaDemoRole \
     --path "/service-role/" \
     --assume-role-policy-document file:///docker-entrypoint-initaws.d/trust-relationship.json #&> /dev/null
 echo "Execution role created."

# Create the Lambda function
aws $endpoint lambda create-function \
  --function-name LambdaDemo \
  --runtime dotnet6 \
  --timeout 10 \
  --role LambdaDemoRole \
  --region eu-west-1 \
  --environment Variables="{ASPNETCORE_ENVIRONMENT=Local}" \
  --handler Demo.Lambda::Demo.Lambda.Function::FunctionHandler \
  --zip-file fileb:///tmp/demo_function.zip \
  --role local-role #&> /dev/null
  
echo "Done creating Lambda function"

echo "Creating Lambda role policy ..."
aws $endpoint iam put-role-policy --role-name LambdaDemoRole \
   --policy-name LambdaDemoRolePolicy \
   --policy-document file:///docker-entrypoint-initaws.d/role-policy.json #&> /dev/null
echo "Lambda role policy created."

# Create the API Gateway
aws $endpoint apigateway create-rest-api \
  --name HealthDataAPI \
  --region eu-west-1 \
  --description "API for wearable sensor data"
  
# Retrieve the API Gateway ID
api_id=$(aws $endpoint apigateway get-rest-apis \
  --query 'items[?name==`HealthDataAPI`].id' \
  --region eu-west-1 \
  --output text)

# Create a root resource (e.g., '/')
root_resource_id=$(aws $endpoint apigateway create-resource \
  --rest-api-id $api_id \
  --parent-id $(aws $endpoint apigateway get-resources --rest-api-id $api_id \
  --query 'items[?path==`/`].id' --output text) \
  --path-part "data" \
  --region eu-west-1 \
  --query 'id' \
  --output text)

# Create the POST method on the '/data' resource
aws $endpoint apigateway put-method \
  --rest-api-id $api_id \
  --resource-id $root_resource_id \
  --http-method POST \
  --region eu-west-1 \
  --authorization-type "NONE"

# Integrate the POST method with the Lambda function
aws $endpoint apigateway put-integration \
  --rest-api-id $api_id \
  --resource-id $root_resource_id \
  --http-method POST \
  --type AWS_PROXY \
  --integration-http-method POST \
  --region eu-west-1 \
  --uri arn:aws:apigateway:eu-west-1:lambda:path/2015-03-31/functions/arn:aws:lambda:eu-west-1:000000000000:function:LambdaDemo/invocations

# Deploy the API
aws $endpoint apigateway create-deployment \
  --rest-api-id $api_id \
  --region eu-west-1 \
  --stage-name test
