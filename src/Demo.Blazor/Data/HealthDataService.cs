using System.Text.Json;
using Confluent.Kafka;
using Demo.Lambda;

namespace Demo.Blazor.Data;

public interface IHealthDataService
{
    event Action<HealthData> OnNewHealthDataReceived;
    Task StartConsuming(CancellationToken cancellationToken);
    void StopConsuming();
}

public class HealthDataService : IHealthDataService
{
    private readonly ConsumerConfig _consumerConfig;
    private IConsumer<Ignore, string>? _kafkaConsumer;
    private const string TopicName = "HealthDataTopic"; 
    private readonly ILogger<HealthDataService> _logger;

    public HealthDataService(IConfiguration configuration, ILogger<HealthDataService> logger)
    {
        _logger = logger;
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
        if (_kafkaConsumer != null) return; 
        _kafkaConsumer = new ConsumerBuilder<Ignore, string>(_consumerConfig).Build();
        _kafkaConsumer.Subscribe(TopicName);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var consumeResult = await Task.Run(() => _kafkaConsumer.Consume(cancellationToken), cancellationToken);
                var healthData = JsonSerializer.Deserialize<HealthData>(consumeResult.Message.Value);
                OnNewHealthDataReceived?.Invoke(healthData);
            }
        }
        catch (OperationCanceledException)
        {
            _kafkaConsumer.Close();
        }
    }

    public void StopConsuming()
    {
        _kafkaConsumer?.Close();
        _kafkaConsumer?.Dispose();  // Dispose the consumer
        _kafkaConsumer = null;
    }

}