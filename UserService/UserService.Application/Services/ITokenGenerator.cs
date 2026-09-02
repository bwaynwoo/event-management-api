using Domain.Models;

namespace Application.Services;

public interface ITokenGenerator
{
    string GenerateToken(User user);
}