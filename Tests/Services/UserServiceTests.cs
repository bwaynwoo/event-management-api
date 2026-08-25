using Application.Repositories;
using Application.Services;
using Domain.Exceptions;
using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Tests.Services;

public sealed class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<ITokenGenerator> _tokenGeneratorMock;
    private readonly PasswordHasher<object> _passwordHasher;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _tokenGeneratorMock = new Mock<ITokenGenerator>();
        _passwordHasher = new PasswordHasher<object>();
        _userService = new UserService(
            _userRepoMock.Object,
            _tokenGeneratorMock.Object,
            _passwordHasher);
    }

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_WithValidData_CreatesUserWithHashedPassword()
    {
        _userRepoMock
            .Setup(x => x.GetByLoginAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _userService.RegisterAsync("testuser", "password123");

        Assert.NotNull(result);
        Assert.Equal("testuser", result.Login);
        Assert.NotEqual(Guid.Empty, result.Id);

        _userRepoMock.Verify(x => x.AddAsync(
            It.Is<User>(u => u.Login == "testuser" && u.PasswordHash != "password123"),
            It.IsAny<CancellationToken>()), Times.Once);

        _userRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingLogin_ThrowsValidationException()
    {
        var existingUser = User.Create("existinguser", "somehash");
        _userRepoMock
            .Setup(x => x.GetByLoginAsync("existinguser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var exception =
            await Assert.ThrowsAsync<ValidationException>(() =>
                _userService.RegisterAsync("existinguser", "password123"));

        Assert.Contains("Login", exception.Errors.Keys);
    }

    [Fact]
    public async Task RegisterAsync_WithEmptyLogin_ThrowsArgumentException()
    {
        _userRepoMock
            .Setup(x => x.GetByLoginAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<ArgumentException>(() => _userService.RegisterAsync("", "password123"));
    }

    [Fact]
    public async Task RegisterAdminAsync_WithValidData_CreatesAdminUser()
    {
        _userRepoMock
            .Setup(x => x.GetByLoginAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _userService.RegisterAdminAsync("adminuser", "password123");

        Assert.NotNull(result);
        Assert.Equal("adminuser", result.Login);
    }

    [Fact]
    public async Task RegisterAdminAsync_WithExistingLogin_ThrowsValidationException()
    {
        var existingUser = User.Create("existingadmin", "somehash");
        _userRepoMock
            .Setup(x => x.GetByLoginAsync("existingadmin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.RegisterAdminAsync("existingadmin", "password123"));
    }

    [Fact]
    public async Task RegisterAsync_PasswordIsHashed()
    {
        _userRepoMock
            .Setup(x => x.GetByLoginAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        User? savedUser = null;
        _userRepoMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, ct) => savedUser = u);

        await _userService.RegisterAsync("testuser", "password123");

        Assert.NotNull(savedUser);
        Assert.NotEqual("password123", savedUser.PasswordHash);
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        var passwordHash = _passwordHasher.HashPassword(new object(), "password123");
        var user = User.Create("testuser", passwordHash);

        _userRepoMock
            .Setup(x => x.GetByLoginAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _tokenGeneratorMock
            .Setup(x => x.GenerateToken(user))
            .Returns("jwt-token-123");

        var result = await _userService.LoginAsync("testuser", "password123");

        Assert.Equal("jwt-token-123", result);
        _tokenGeneratorMock.Verify(x => x.GenerateToken(user), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorizedException()
    {
        var passwordHash = _passwordHasher.HashPassword(new object(), "correctPassword");
        var user = User.Create("testuser", passwordHash);

        _userRepoMock
            .Setup(x => x.GetByLoginAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await Assert.ThrowsAsync<UnauthorizedException>(() => _userService.LoginAsync("testuser", "wrongPassword"));
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentUser_ThrowsUnauthorizedException()
    {
        _userRepoMock
            .Setup(x => x.GetByLoginAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() => _userService.LoginAsync("nonexistent", "password123"));
    }

    [Fact]
    public async Task LoginAsync_WithEmptyLogin_ThrowsUnauthorizedException()
    {
        _userRepoMock
            .Setup(x => x.GetByLoginAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() => _userService.LoginAsync("", "password123"));
    }

    #endregion
}