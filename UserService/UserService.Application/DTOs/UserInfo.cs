namespace Application.DTOs;

public sealed record UserInfo
{
    public Guid Id { get; init; }
    public string Login { get; init; } = string.Empty;
}