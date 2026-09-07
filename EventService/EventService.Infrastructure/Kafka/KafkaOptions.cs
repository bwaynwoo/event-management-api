namespace EventService.Infrastructure.Kafka;

public sealed class KafkaOptions
{
    public string BootstrapServers { get; init; } = "localhost:9092";

    public string ConsumerGroup { get; init; } = "events-service";

    public int TopicPartitions { get; init; } = 1;

    public short TopicReplicationFactor { get; init; } = 1;
}
