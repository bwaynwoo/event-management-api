using System.Text.Json;
using Confluent.Kafka;
using Contracts;
using EventService.Application;
using EventService.Application.Repositories;
using EventService.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventService.Infrastructure.Kafka;

public sealed class BookingConfirmedConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> options,
    ILogger<BookingConfirmedConsumer> logger)
    : BackgroundService
{
    private readonly KafkaOptions _options = options.Value;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private async Task ConsumeLoop(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(KafkaTopics.BookingConfirmed);

        logger.LogInformation(
            "Kafka consumer started, subscribed to {Topic} at {Servers}",
            KafkaTopics.BookingConfirmed, _options.BootstrapServers);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result;
                try
                {
                    result = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Error while consuming from Kafka");
                    continue;
                }

                if (result?.Message?.Value is null)
                    continue;

                await HandleMessageAsync(result.Message.Value, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task HandleMessageAsync(string payload, CancellationToken stoppingToken)
    {
        BookingConfirmed? message;
        try
        {
            message = JsonSerializer.Deserialize<BookingConfirmed>(payload);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize BookingConfirmed: {Payload}", payload);
            return;
        }

        if (message is null)
            return;

        using var scope = scopeFactory.CreateScope();
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var @event = await eventRepository.GetByIdAsync(message.EventId, stoppingToken);
        if (@event is null)
        {
            logger.LogWarning(
                "BookingConfirmed for unknown event {EventId} ignored (booking {BookingId})",
                message.EventId, message.BookingId);
            return;
        }

        if (!@event.TryReserveSeats(message.Seats))
        {
            logger.LogWarning(
                "No available seats for event {EventId} on BookingConfirmed {BookingId}",
                message.EventId, message.BookingId);
            return;
        }

        await eventRepository.UpdateAsync(@event, stoppingToken);
        await cacheService.RemoveAsync(CacheKeys.Event(message.EventId), stoppingToken);
        await cacheService.RemoveAsync(CacheKeys.TopEvents, stoppingToken);

        logger.LogInformation(
            "Reserved {Seats} seat(s) for event {EventId} (booking {BookingId}); {Available} left",
            message.Seats, message.EventId, message.BookingId, @event.AvailableSeats);
    }
}
