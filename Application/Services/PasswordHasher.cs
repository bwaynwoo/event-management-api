using System.Security.Cryptography;
using System.Text;

namespace Application.Services;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public bool Verify(string password, string hash)
    {
        var hashedPassword = Hash(password);
        return string.Equals(hashedPassword, hash, StringComparison.OrdinalIgnoreCase);
    }
}