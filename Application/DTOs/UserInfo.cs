using Domain.Enums;

namespace Application.DTOs;

public class UserInfo
{
    public Guid Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public Role Role { get; set; }
}