using EventApi.DataAccess;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace EventApi.IntegrationTests;

public class DatabaseMigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();
    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var context = new AppDbContext(options);
        return context;
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();

        await context.Database.MigrateAsync("0");

        await context.Database.MigrateAsync();
    }

    [Fact]
    public async Task Migrate_ShouldCreateAllTables()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var tables = await context.Database
            .SqlQueryRaw<string>(
                "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'")
            .ToListAsync();

        Assert.Contains(tables, t => t.Equals("events"));
        Assert.Contains(tables, t => t.Equals("bookings"));
        Assert.Contains(tables, t => t.Equals("__EFMigrationsHistory"));
    }

    [Fact]
    public async Task Migrate_ShouldCreateEventsTableWithCorrectColumns()
    {
        await ResetDatabaseAsync();
        await using var context = CreateContext();

        var columns = await context.Database
            .SqlQueryRaw<string>(
                "SELECT column_name FROM information_schema.columns WHERE table_name = 'events'")
            .ToListAsync();

        Assert.Contains(columns, c => c.Equals("id"));
        Assert.Contains(columns, c => c.Equals("title"));
        Assert.Contains(columns, c => c.Equals("description"));
        Assert.Contains(columns, c => c.Equals("start_at"));
        Assert.Contains(columns, c => c.Equals("end_at"));
        Assert.Contains(columns, c => c.Equals("total_seats"));
        Assert.Contains(columns, c => c.Equals("available_seats"));
    }

    [Fact]
    public async Task Migrate_ShouldCreateBookingsTableWithCorrectColumns()
    {
        await ResetDatabaseAsync();
        await using var context = CreateContext();

        var columns = await context.Database
            .SqlQueryRaw<string>(
                "SELECT column_name FROM information_schema.columns WHERE table_name = 'bookings'")
            .ToListAsync();

        Assert.Contains(columns, c => c.Equals("id"));
        Assert.Contains(columns, c => c.Equals("event_id"));
        Assert.Contains(columns, c => c.Equals("status"));
        Assert.Contains(columns, c => c.Equals("created_at"));
        Assert.Contains(columns, c => c.Equals("processed_at"));
    }

    [Fact]
    public async Task Migrate_ShouldHaveForeignKeyFromBookingsToEvents()
    {
        await ResetDatabaseAsync();
        await using var context = CreateContext();

        var foreignKeys = await context.Database
            .SqlQueryRaw<string>(
                @"SELECT tc.constraint_name 
              FROM information_schema.table_constraints tc
              JOIN information_schema.constraint_column_usage ccu ON tc.constraint_name = ccu.constraint_name
              WHERE tc.constraint_type = 'FOREIGN KEY' 
              AND tc.table_name = 'bookings' 
              AND ccu.table_name = 'events'")
            .ToListAsync();

        Assert.NotEmpty(foreignKeys);
    }

    [Fact]
    public async Task Migrate_ShouldCreateMigrationHistoryTable()
    {
        await ResetDatabaseAsync();
        await using var context = CreateContext();

        var migrations = await context.Database
            .SqlQueryRaw<string>("SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\"")
            .ToListAsync();

        Assert.NotEmpty(migrations);
        Assert.Contains(migrations, m => m.Contains("InitialCreate"));
    }
}