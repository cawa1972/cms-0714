using System.Security.Cryptography;
using System.Text;

namespace CMS.API.Security;

/// <summary>
/// Single source of truth for how passwords are hashed in this app: SHA-256 of the UTF-8 bytes
/// of the plain text, rendered as lowercase hex. Stored (and compared) against
/// <c>AppUser.PasswordHash</c>.
/// </summary>
public static class PasswordHasher
{
    public static string Hash(string plainText)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainText));
        return Convert.ToHexStringLower(bytes);
    }
}
