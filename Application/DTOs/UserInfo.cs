namespace Application.DTOs;

public sealed record UserInfo
{
    public Guid Id { get; set; }
    public string Login { get; set; } = string.Empty;
}