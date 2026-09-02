using Application.DTOs;

namespace UserService.Application.Services;

public interface IUserService
{
    Task<UserInfo> RegisterAsync(string login, string password, CancellationToken cancellationToken = default);
    Task<UserInfo> RegisterAdminAsync(string login, string password, CancellationToken cancellationToken = default);
    Task<string> LoginAsync(string login, string password, CancellationToken cancellationToken = default);
}