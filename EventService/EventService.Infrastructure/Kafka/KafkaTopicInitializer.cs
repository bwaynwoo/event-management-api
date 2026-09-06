using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventService.Infrastructure.Kafka;

public sealed class KafkaTopicInitializer(IOptions<KafkaOptions> options, ILogger<KafkaTopicInitializer> logger)
    : IHostedService
{
    private readonly KafkaOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var config = new AdminClientConfig { BootstrapServers = _options.BootstrapServers };
        using var adminClient = new AdminClientBuilder(config).Build();

        var topic = new TopicSpecification
        {
            Name = KafkaTopics.BookingConfirmed,
            NumPartitions = _options.TopicPartitions,
            ReplicationFactor = _options.TopicReplicationFactor
        };

        try
        {
            await adminClient.CreateTopicsAsync(new[] { topic });
            logger.LogInformation("Kafka topic {Topic} created", KafkaTopics.BookingConfirmed);
        }
        catch (CreateTopicsException ex) when (ex.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            logger.LogInformation("Kafka topic {Topic} already exists", KafkaTopics.BookingConfirmed);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create Kafka topic {Topic} at startup", KafkaTopics.BookingConfirmed);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}