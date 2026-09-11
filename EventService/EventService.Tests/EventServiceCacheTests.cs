using EventService.Application;
using EventService.Application.DTOs;
using EventService.Application.Repositories;
using EventService.Application.Services;
using EventService.Domain.Models;
using Microsoft.Extensions.Options;
using Moq;

namespace EventService.Tests;

public sealed class EventServiceCacheTests
{
    private readonly Mock<IEventRepository> _repositoryMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Application.Services.EventService _eventService;

    public EventServiceCacheTests()
    {
        _repositoryMock = new Mock<IEventRepository>();
        _cacheMock = new Mock<ICacheService>();

        var options = Options.Create(new EventCacheOptions
        {
            EventTtl = TimeSpan.FromMinutes(10),
            TopEventsTtl = TimeSpan.FromMinutes(5)
        });

        _eventService = new Application.Services.EventService(_repositoryMock.Object, _cacheMock.Object, options);
    }

    [Fact]
    public async Task GetEventByIdAsync_WhenCacheHit_ReturnsFromCacheWithoutDatabaseCall()
    {
        var eventId = Guid.NewGuid();
        var cached = BuildEventInfo(eventId);

        _cacheMock
            .Setup(c => c.GetAsync<EventInfo>(CacheKeys.Event(eventId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var result = await _eventService.GetEventByIdAsync(eventId);

        Assert.Equal(cached.Id, result.Id);

        _repositoryMock.Verify(
            r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetEventByIdAsync_WhenCacheMiss_FetchesFromDatabaseAndPopulatesCache()
    {
        var @event = BuildEvent();
        var eventId = @event.Id;

        _cacheMock
            .Setup(c => c.GetAsync<EventInfo>(CacheKeys.Event(eventId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventInfo?)null);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var result = await _eventService.GetEventByIdAsync(eventId);

        Assert.Equal(eventId, result.Id);

        _cacheMock.Verify(
            c => c.SetAsync(
                CacheKeys.Event(eventId),
                It.IsAny<EventInfo>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateEventAsync_InvalidatesCacheForEvent()
    {
        var @event = BuildEvent();
        var eventId = @event.Id;

        _repositoryMock
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _repositoryMock
            .Setup(r => r.UpdateAsync(@event, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = new UpdateEvent
        {
            Title = "Updated Title",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2)
        };

        await _eventService.UpdateEventAsync(eventId, request);

        _cacheMock.Verify(
            c => c.RemoveAsync(CacheKeys.Event(eventId), It.IsAny<CancellationToken>()),
            Times.Once);

        _cacheMock.Verify(
            c => c.RemoveAsync(CacheKeys.TopEvents, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteEventAsync_InvalidatesCacheForEventAndList()
    {
        var @event = BuildEvent();
        var eventId = @event.Id;

        _repositoryMock
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _repositoryMock
            .Setup(r => r.DeleteAsync(@event, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _eventService.DeleteEventAsync(eventId);

        _cacheMock.Verify(
            c => c.RemoveAsync(CacheKeys.Event(eventId), It.IsAny<CancellationToken>()),
            Times.Once);

        _cacheMock.Verify(
            c => c.RemoveAsync(CacheKeys.TopEvents, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateEventAsync_InvalidatesTopEventsCache()
    {
        var request = new CreateEvent
        {
            Title = "New Event",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 100
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _eventService.CreateEventAsync(request);

        _cacheMock.Verify(
            c => c.RemoveAsync(CacheKeys.TopEvents, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTopEventsAsync_WhenCacheHit_ReturnsFromCacheWithoutDatabaseCall()
    {
        var cached = new[] { BuildEventInfo(Guid.NewGuid()) };

        _cacheMock
            .Setup(c => c.GetAsync<EventInfo[]>(CacheKeys.TopEvents, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var result = await _eventService.GetTopEventsAsync();

        Assert.Single(result);

        _repositoryMock.Verify(
            r => r.GetTopAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetTopEventsAsync_WhenCacheMiss_FetchesFromDatabaseAndPopulatesCache()
    {
        var events = new[] { BuildEvent(), BuildEvent() };

        _cacheMock
            .Setup(c => c.GetAsync<EventInfo[]>(CacheKeys.TopEvents, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventInfo[]?)null);

        _repositoryMock
            .Setup(r => r.GetTopAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        var result = await _eventService.GetTopEventsAsync();

        Assert.Equal(2, result.Count);

        _cacheMock.Verify(
            c => c.SetAsync(
                CacheKeys.TopEvents,
                It.IsAny<EventInfo[]>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Event BuildEvent()
    {
        return Event.Create("Test Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 50);
    }

    private static EventInfo BuildEventInfo(Guid id)
    {
        return new EventInfo
        {
            Id = id,
            Title = "Cached Event",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = 50,
            AvailableSeats = 50
        };
    }
}