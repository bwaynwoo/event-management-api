using EventManagementApi.DTOs;
using EventManagementApi.Exceptions;
using EventManagementApi.Models;
using EventManagementApi.Services;
using FluentAssertions;

namespace EventManagementApi.Tests.Services;

public class EventServiceTests : IDisposable
{
    private readonly EventService _eventService;

    public EventServiceTests()
    {
        _eventService = new EventService();
    }

    public void Dispose()
    {
        _eventService.Clear();
    }

    [Fact(DisplayName = "Создание события")]
    public void AddEvent_ShouldAddEvent_WhenValid()
    {
        var newEvent = new Event
        {
            Title = "Test Event",
            Description = "Test Description",
            StartAt = new DateTime(2025, 1, 1),
            EndAt = new DateTime(2025, 1, 2)
        };

        _eventService.AddEvent(newEvent);

        var events = _eventService.GetEvents(new GetEventsRequestDto());
        events.Items.Count.Should().Be(1);
    }

    [Fact(DisplayName = "Получение всех событий")]
    public void GetEvents_ShouldReturnAllEvents()
    {
        var event1 = new Event
            { Title = "Event 1", Description = "Desc 1", StartAt = DateTime.Now, EndAt = DateTime.Now.AddDays(1) };
        var event2 = new Event
            { Title = "Event 2", Description = "Desc 2", StartAt = DateTime.Now, EndAt = DateTime.Now.AddDays(1) };
        _eventService.AddEvent(event1);
        _eventService.AddEvent(event2);

        var result = _eventService.GetEvents(new GetEventsRequestDto());

        result.Items.Should().HaveCount(2);
    }

    [Fact(DisplayName = "Получение события по ID")]
    public void GetEvent_ShouldReturnEvent_WhenExists()
    {
        var newEvent = new Event
            { Title = "Find Me", Description = "Desc", StartAt = DateTime.Now, EndAt = DateTime.Now.AddDays(1) };
        _eventService.AddEvent(newEvent);

        var result = _eventService.GetEvent(newEvent.Id);

        result.Should().NotBeNull();
        result.Id.Should().Be(newEvent.Id);
        result.Title.Should().Be("Find Me");
        result.Description.Should().Be("Desc");
        result.StartAt.Should().Be(newEvent.StartAt);
        result.EndAt.Should().Be(newEvent.EndAt);
    }

    [Fact(DisplayName = "Обновление существующего события")]
    public void UpdateEvent_ShouldUpdateExistingEvent()
    {
        var existingEvent = new Event
            { Title = "Old Title", Description = "Old Desc", StartAt = DateTime.Now, EndAt = DateTime.Now.AddDays(1) };
        _eventService.AddEvent(existingEvent);

        var updatedEvent = new Event
        {
            Title = "New Title", Description = "New Desc", StartAt = DateTime.Now.AddDays(2),
            EndAt = DateTime.Now.AddDays(3)
        };
        _eventService.UpdateEvent(existingEvent.Id, updatedEvent);

        var result = _eventService.GetEvent(existingEvent.Id);
        result.Title.Should().Be("New Title");
        result.Description.Should().Be("New Desc");
    }

    [Fact(DisplayName = "Удаление существующего события")]
    public void RemoveEvent_ShouldRemoveExistingEvent()
    {
        var existingEvent = new Event
            { Title = "To Delete", Description = "Desc", StartAt = DateTime.Now, EndAt = DateTime.Now.AddDays(1) };
        _eventService.AddEvent(existingEvent);

        _eventService.RemoveEvent(existingEvent.Id);

        var events = _eventService.GetEvents(new GetEventsRequestDto());
        events.Items.Should().NotContain(e => e.Id == existingEvent.Id);
    }

    [Fact(DisplayName = "Фильтрация по названию")]
    public void GetEvents_ShouldFilterByTitle()
    {
        _eventService.AddEvent(new Event
        {
            Title = "Conference 2025", Description = "Desc", StartAt = DateTime.Now, EndAt = DateTime.Now.AddDays(1)
        });
        _eventService.AddEvent(new Event
            { Title = "Workshop 2025", Description = "Desc", StartAt = DateTime.Now, EndAt = DateTime.Now.AddDays(1) });
        _eventService.AddEvent(new Event
            { Title = "Meeting", Description = "Desc", StartAt = DateTime.Now, EndAt = DateTime.Now.AddDays(1) });

        var request = new GetEventsRequestDto { Title = "2025" };
        var result = _eventService.GetEvents(request);

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(e => e.Title.Should().Contain("2025"));
    }

    [Fact(DisplayName = "Фильтрация по датам (startDate, endDate)")]
    public void GetEvents_ShouldFilterByFromDate()
    {
        _eventService.AddEvent(new Event
            { Title = "Past", StartAt = new DateTime(2024, 1, 1), EndAt = new DateTime(2024, 1, 2) });
        _eventService.AddEvent(new Event
            { Title = "Future", StartAt = new DateTime(2026, 1, 1), EndAt = new DateTime(2026, 1, 2) });

        var request = new GetEventsRequestDto { From = new DateTime(2025, 1, 1) };
        var result = _eventService.GetEvents(request);

        result.Items.Should().NotContain(e => e.Title == "Past");
        result.Items.Should().Contain(e => e.Title == "Future");
    }

    [Fact(DisplayName = "Фильтрация по датам (startDate, endDate)")]
    public void GetEvents_ShouldFilterByToDate()
    {
        _eventService.AddEvent(new Event
            { Title = "Early", StartAt = new DateTime(2024, 1, 1), EndAt = new DateTime(2024, 1, 2) });
        _eventService.AddEvent(new Event
            { Title = "Late", StartAt = new DateTime(2026, 1, 1), EndAt = new DateTime(2026, 1, 2) });

        var request = new GetEventsRequestDto { To = new DateTime(2025, 1, 1) };
        var result = _eventService.GetEvents(request);

        result.Items.Should().Contain(e => e.Title == "Early");
        result.Items.Should().NotContain(e => e.Title == "Late");
    }

    [Fact(DisplayName = "Пагинация событий")]
    public void GetEvents_ShouldPaginateResults()
    {
        for (int i = 1; i <= 25; i++)
        {
            _eventService.AddEvent(new Event
                { Title = $"Event {i}", StartAt = DateTime.Now, EndAt = DateTime.Now.AddDays(1) });
        }

        var page1 = new GetEventsRequestDto { Page = 1, PageSize = 10 };
        var result1 = _eventService.GetEvents(page1);

        var page2 = new GetEventsRequestDto { Page = 2, PageSize = 10 };
        var result2 = _eventService.GetEvents(page2);

        result1.Items.Should().HaveCount(10);
        result2.Items.Should().HaveCount(10);
        result1.Items.Select(e => e.Id).Should().NotIntersectWith(result2.Items.Select(e => e.Id));
    }

    [Fact(DisplayName = "Комбинированная фильтрация")]
    public void GetEvents_ShouldCombineFilters()
    {
        _eventService.AddEvent(new Event
            { Title = "Summer Conference", StartAt = new DateTime(2025, 6, 1), EndAt = new DateTime(2025, 6, 5) });
        _eventService.AddEvent(new Event
            { Title = "Summer Workshop", StartAt = new DateTime(2025, 7, 1), EndAt = new DateTime(2025, 7, 5) });
        _eventService.AddEvent(new Event
            { Title = "Winter Conference", StartAt = new DateTime(2025, 12, 1), EndAt = new DateTime(2025, 12, 5) });

        var request = new GetEventsRequestDto
        {
            Title = "Summer",
            From = new DateTime(2025, 5, 1),
            To = new DateTime(2025, 8, 1)
        };
        var result = _eventService.GetEvents(request);

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(e => e.Title.Should().Contain("Summer"));
    }

    [Fact(DisplayName = "Попытка получить событие с несуществующим ID")]
    public void GetEvent_ShouldThrowNotFoundException_WhenIdDoesNotExist()
    {
        var action = () => _eventService.GetEvent(77);

        action.Should().Throw<NotFoundException>().WithMessage("Event with id 77 was not found");
    }

    [Fact(DisplayName = "Попытка обновить событие с несуществующим ID")]
    public void UpdateEvent_ShouldThrowNotFoundException_WhenIdDoesNotExist()
    {
        var updateEvent = new Event { Title = "Test", StartAt = DateTime.Now, EndAt = DateTime.Now.AddDays(1) };

        var action = () => _eventService.UpdateEvent(99999, updateEvent);

        action.Should().Throw<NotFoundException>().WithMessage("Event with id 99999 was not found");
    }

    [Fact(DisplayName = "Попытка удалить событие с несуществующим ID")]
    public void RemoveEvent_ShouldThrowNotFoundException_WhenIdDoesNotExist()
    {
        var action = () => _eventService.RemoveEvent(99999);

        action.Should().Throw<NotFoundException>().WithMessage("Event with id 99999 was not found");
    }

    [Fact(DisplayName = "Title - пустая строка (граничное значение)")]
    public void AddEvent_TitleIsEmptyString_ShouldWork()
    {
        var newEvent = new Event
        {
            Title = "",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddDays(1)
        };

        _eventService.AddEvent(newEvent);

        var result = _eventService.GetEvent(newEvent.Id);
        result.Title.Should().BeEmpty();
    }

    [Fact(DisplayName = "Title - длинная строка (1000 символов)")]
    public void AddEvent_TitleIsVeryLong_ShouldWork()
    {
        var longTitle = new string('a', 1000);
        var newEvent = new Event
        {
            Title = longTitle,
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddDays(1)
        };

        _eventService.AddEvent(newEvent);

        var result = _eventService.GetEvent(newEvent.Id);
        result.Title.Should().Be(longTitle);
        result.Title.Length.Should().Be(1000);
    }
}