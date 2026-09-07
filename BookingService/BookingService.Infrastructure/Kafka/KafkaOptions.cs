namespace BookingService.Infrastructure.Kafka;

public sealed class KafkaOptions
{
    public string BootstrapServers { get; init; } = "localhost:9092";
}