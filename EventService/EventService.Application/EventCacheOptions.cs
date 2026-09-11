namespace EventService.Application;

public sealed class EventCacheOptions
{
    public TimeSpan EventTtl { get; init; } = TimeSpan.FromMinutes(10);

    public TimeSpan TopEventsTtl { get; init; } = TimeSpan.FromMinutes(5);
}
