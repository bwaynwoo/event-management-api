using Microsoft.AspNetCore.Identity;
using UserService.Application.DTOs;
using UserService.Application.Repositories;
using UserService.Domain.Enums;
using UserService.Domain.Exceptions;
using UserService.Domain.Models;

namespace UserService.Application.Services;

internal sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordHasher<object> _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;

    public UserService(
        IUserRepository userRepository,
        ITokenGenerator tokenGenerator,
        PasswordHasher<object> passwordHasher)
    {
        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserInfo> RegisterAsync(string login, string password,
        CancellationToken cancellationToken = default)
    {
        return await RegisterUserAsync(login, password, Role.User, cancellationToken);
    }

    public async Task<UserInfo> RegisterAdminAsync(string login, string password,
        CancellationToken cancellationToken = default)
    {
        return await RegisterUserAsync(login, password, Role.Admin, cancellationToken);
    }

    private async Task<UserInfo> RegisterUserAsync(string login, string password, Role role,
        CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository.GetByLoginAsync(login, cancellationToken);
        if (existingUser != null)
            throw new ValidationException("Login", "User with this login already exists.");

        var tmpUser = new object();
        var passwordHash = _passwordHasher.HashPassword(tmpUser, password);

        var user = User.Create(login, passwordHash, role);

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return new UserInfo
        {
            Id = user.Id,
            Login = user.Login,
        };
    }

    public async Task<string> LoginAsync(string login, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByLoginAsync(login, cancellationToken);

        if (user == null)
            throw new UnauthorizedException("Invalid login or password.");

        var tmpUser = new object();
        var result = _passwordHasher.VerifyHashedPassword(tmpUser, user.PasswordHash, password);

        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedException("Invalid login or password.");

        return _tokenGenerator.GenerateToken(user);
    }
}