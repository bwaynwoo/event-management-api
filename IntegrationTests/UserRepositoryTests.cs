using Domain.Enums;
using Domain.Models;
using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace IntegrationTests;

public sealed class UserRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();
    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        
        var context = new AppDbContext(options);
        context.Database.Migrate();
        return context;
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();
        context.Users.RemoveRange(context.Users);
        context.Bookings.RemoveRange(context.Bookings);
        context.Events.RemoveRange(context.Events);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task AddAsync_SavesUserToDatabase()
    {
        await ResetDatabaseAsync();
        
        await using var context = CreateContext();
        var user = User.Create("testuser", "testhash");
        
        var repository = new UserRepository(context);
        await repository.AddAsync(user, CancellationToken.None);
        
        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Login == "testuser");
        Assert.NotNull(savedUser);
        Assert.Equal(user.Id, savedUser.Id);
        Assert.Equal("testuser", savedUser.Login);
        Assert.Equal("testhash", savedUser.PasswordHash);
    }

    [Fact]
    public async Task GetByLoginAsync_ExistingUser_ReturnsUser()
    {
        await ResetDatabaseAsync();
        
        await using var context = CreateContext();
        var user = User.Create("existinguser", "testhash");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        var repository = new UserRepository(context);
        var result = await repository.GetByLoginAsync("existinguser", CancellationToken.None);
        
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal("existinguser", result.Login);
    }

    [Fact]
    public async Task GetByLoginAsync_NonExistentUser_ReturnsNull()
    {
        await ResetDatabaseAsync();
        
        await using var context = CreateContext();
        
        var repository = new UserRepository(context);
        var result = await repository.GetByLoginAsync("nonexistent", CancellationToken.None);
        
        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateLogin_ThrowsException()
    {
        await ResetDatabaseAsync();
        
        await using var context = CreateContext();
        var user1 = User.Create("duplicate", "hash1");
        context.Users.Add(user1);
        await context.SaveChangesAsync();
        
        var user2 = User.Create("duplicate", "hash2");
        
        var repository = new UserRepository(context);
        await Assert.ThrowsAsync<DbUpdateException>(
            () => repository.AddAsync(user2, CancellationToken.None));
    }

    [Fact]
    public async Task AddAsync_MultipleUsers_AllSaved()
    {
        await ResetDatabaseAsync();
        
        await using var context = CreateContext();
        var user1 = User.Create("user1", "hash1");
        var user2 = User.Create("user2", "hash2");
        
        var repository = new UserRepository(context);
        await repository.AddAsync(user1, CancellationToken.None);
        await repository.AddAsync(user2, CancellationToken.None);
        
        var allUsers = await context.Users.ToListAsync();
        Assert.Equal(2, allUsers.Count);
        Assert.Contains(allUsers, u => u.Login == "user1");
        Assert.Contains(allUsers, u => u.Login == "user2");
    }

    [Fact]
    public async Task AddAsync_PreservesRole()
    {
        await ResetDatabaseAsync();
        
        await using var context = CreateContext();
        var user = User.Create("adminuser", "testhash", Role.Admin);
        
        var repository = new UserRepository(context);
        await repository.AddAsync(user, CancellationToken.None);
        
        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Login == "adminuser");
        Assert.NotNull(savedUser);
        Assert.Equal(Role.Admin, savedUser.Role);
    }
}