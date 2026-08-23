using Domain.Enums;

namespace Domain.Models;

public class User
{
    public Guid Id { get; private set; }
    public string Login { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public Role Role { get; private set; }

    private User() { }

    private User(Guid id, string login, string passwordHash, Role role)
    {
        Id = id;
        Login = login;
        PasswordHash = passwordHash;
        Role = role;
    }

    public static User Create(string login, string passwordHash, Role role = Role.User)
    {
        if (string.IsNullOrWhiteSpace(login))
            throw new ArgumentException("Login cannot be empty", nameof(login));
            
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

        return new User(Guid.NewGuid(), login, passwordHash, role);
    }
}