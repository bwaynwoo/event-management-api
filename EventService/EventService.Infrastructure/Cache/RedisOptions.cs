namespace EventService.Infrastructure.Cache;

public sealed class RedisOptions
{
    public string ConnectionString { get; init; } = "localhost:6379";

    public TimeSpan DefaultTtl { get; init; } = TimeSpan.FromMinutes(5); 
}
