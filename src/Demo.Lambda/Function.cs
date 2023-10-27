using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Demo.Lambda;

public class Function
{
    private readonly IServiceProvider _serviceProvider;
    private static string? _topic;

    public Function()
    {
        var configuration = BuildConfiguration();
        _serviceProvider = ConfigureServices(configuration).BuildServiceProvider();
    }

    public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var inputData = JsonSerializer.Deserialize<HealthData>(request.Body);
        
        SendMessageToKafka(inputData, context);

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
    
    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Local"}.json", optional: true)
            .Build();
    }

    private static IServiceCollection ConfigureServices(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddSingleton(configuration);

        _topic = configuration["Kafka:Topic"];
        // Add Kafka producer
        var producerConfig = new ProducerConfig 
        { 
            BootstrapServers = configuration["Kafka:BootstrapServers"]
        };
        services.AddSingleton<IProducer<Null, string>>(
            new ProducerBuilder<Null, string>(producerConfig).Build());

        return services;
    }
    
    private void SendMessageToKafka(HealthData? data, ILambdaContext context)
    {
        context.Logger.LogLine($"Topic: {_topic}");
        
        var kafkaProducer = _serviceProvider.GetRequiredService<IProducer<Null, string>>();
        
        kafkaProducer.Produce(_topic, new Message<Null, string> { Value = JsonSerializer.Serialize(data) });
        kafkaProducer.Flush();
    }
}

public record HealthData(string PatientName, int HeartRate, float Temperature);

