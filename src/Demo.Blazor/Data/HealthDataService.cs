using System.Text.Json;
using Confluent.Kafka;
using Demo.Lambda;

namespace Demo.Blazor.Data;

public interface IHealthDataService
{
    event Action<HealthData> OnNewHealthDataReceived;
    Task StartConsuming(CancellationToken cancellationToken);
    void StopConsuming();
    bool IsConsuming { get; } 
}

public class HealthDataService : IHealthDataService
{
    private readonly ConsumerConfig _consumerConfig;
    private IConsumer<Ignore, string>? _kafkaConsumer;
    private const string TopicName = "HealthDataTopic";
    public bool IsConsuming { get; private set; } 

    public HealthDataService(IConfiguration configuration)
    {
        _consumerConfig = new ConsumerConfig
        {
            GroupId = "health-data-group",
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            AutoOffsetReset = AutoOffsetReset.Latest
        };
    }

    
    public event Action<HealthData>? OnNewHealthDataReceived;
    
    public async Task StartConsuming(CancellationToken cancellationToken)
    {
        if (IsConsuming) return;
        
        IsConsuming = true;
        _kafkaConsumer = new ConsumerBuilder<Ignore, string>(_consumerConfig).Build();
        await Task.Run(() => StartConsumerLoop(cancellationToken), cancellationToken);
    }

    private Task StartConsumerLoop(CancellationToken cancellationToken)
    {
        _kafkaConsumer!.Subscribe(TopicName);
        try
        {
            while (!cancellationToken.IsCancellationRequested)  
            {  
                var result = _kafkaConsumer.Consume(cancellationToken);  
                var healthData = JsonSerializer.Deserialize<HealthData>(result.Message.Value);
                if (healthData != null)
                {
                    OnNewHealthDataReceived?.Invoke(healthData);
                }
            }  
        }
        catch (OperationCanceledException)  
        {  
            // Handle cancellation
        }
        finally
        {
            IsConsuming = false;  // Reset this when you stop consuming
            StopConsuming(); // Call StopConsuming to unsubscribe, close and dispose the consumer.
        }

        return Task.CompletedTask;
    }

    public void StopConsuming()
    {
        IsConsuming = false; 
        _kafkaConsumer?.Unsubscribe(); // Unsubscribe first to stop consuming
        _kafkaConsumer?.Close();
        _kafkaConsumer?.Dispose();
        _kafkaConsumer = null; 
    }

}