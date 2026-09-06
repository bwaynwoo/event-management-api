using UserService.Domain.Models;

namespace UserService.Application.Services;

public interface ITokenGenerator
{
    string GenerateToken(User user);
}