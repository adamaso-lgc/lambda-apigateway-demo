using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Demo.Lambda;

public class Function
{
    public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        // Deserialize the body from the input
        var inputData = JsonSerializer.Deserialize<HealthData>(request.Body);

        // Create the response object
        var responseBody = new
        {
            Message = "Success",
            Data = inputData
        };

        return new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(responseBody),
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
    }
}

public record HealthData(string PatientName, int HeartRate, float Temperature);

