using Application.DTOs;
using Application.Repositories;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Models;

namespace Application.Services;

internal sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;

    public UserService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<UserInfo> RegisterAsync(string login, string password, Role? role = null,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await _userRepository.GetByLoginAsync(login, cancellationToken);
        if (existingUser != null)
            throw new ValidationException("Login", "User with this login already exists.");

        var passwordHash = _passwordHasher.Hash(password);

        var user = User.Create(login, passwordHash, role ?? Role.User);

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return new UserInfo
        {
            Id = user.Id,
            Login = user.Login,
            Role = user.Role
        };
    }

    public async Task<string> LoginAsync(string login, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByLoginAsync(login, cancellationToken);

        if (user == null)
            throw new UnauthorizedException("Invalid login or password.");

        if (!_passwordHasher.Verify(password, user.PasswordHash))
            throw new UnauthorizedException("Invalid login or password.");

        return _tokenGenerator.GenerateToken(user);
    }
}