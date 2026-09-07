using System.Text.Json;
using BookingService.Application.Services;
using Confluent.Kafka;
using Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookingService.Infrastructure.Kafka;

public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(IOptions<KafkaOptions> options, ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;
        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            AllowAutoCreateTopics = true
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishBookingConfirmedAsync(BookingConfirmed message,
        CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(message);

        var result = await _producer.ProduceAsync(
            KafkaTopics.BookingConfirmed,
            new Message<string, string>
            {
                Key = message.EventId.ToString(),
                Value = payload
            },
            cancellationToken);

        _logger.LogInformation(
            "Published BookingConfirmed for booking {BookingId} to {TopicPartitionOffset}",
            message.BookingId, result.TopicPartitionOffset);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}