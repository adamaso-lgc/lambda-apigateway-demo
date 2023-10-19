using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Demo.Lambda;

public class Function
{
    public APIGatewayProxyResponse FunctionHandler(HealthData input, ILambdaContext context)
    {
        return new APIGatewayProxyResponse(200, "Success", input);
    }
}

public record HealthData(string PatientName, int HeartRate, float Temperature);

public record APIGatewayProxyResponse(int StatusCode, string Message, HealthData Data);
