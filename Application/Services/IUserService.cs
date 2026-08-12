using Application.DTOs;
using Domain.Enums;

namespace Application.Services;

public interface IUserService
{
    Task<UserInfo> RegisterAsync(string login, string password, Role? role = null,
        CancellationToken cancellationToken = default);

    Task<string> LoginAsync(string login, string password, CancellationToken cancellationToken = default);
}