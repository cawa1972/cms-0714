using System.Security.Cryptography;
using System.Text;

namespace CMS.API.Security;

/// <summary>
/// Single source of truth for how passwords are hashed in this app: PBKDF2-HMAC-SHA512 over a
/// per-password 128-bit random salt, encoded as
/// <c>pbkdf2$sha512${iterations}${base64 salt}${base64 hash}</c> and stored in
/// <c>AppUser.PasswordHash</c>.
/// <para>
/// <see cref="Hash"/> is deliberately <b>non-deterministic</b> — the same plain text hashes to a
/// different string every call, because each call draws a fresh salt. Never compare two hashes with
/// <c>==</c>; the only way to check a password is <see cref="Verify"/>.
/// </para>
/// <para>
/// <b>Legacy migration.</b> Passwords used to be stored as unsalted <c>SHA256(plain)</c> lowercase
/// hex. <see cref="Verify"/> still accepts that shape so existing logins keep working, and
/// <see cref="NeedsRehash"/> reports true for it so callers can transparently upgrade the row on the
/// next successful sign-in (see <c>AuthController.Login</c>). Once no stored hash is legacy, the
/// <see cref="VerifyLegacySha256"/> branch can be deleted.
/// </para>
/// </summary>
public static class PasswordHasher
{
    private const string Prefix = "pbkdf2";
    private const string Algorithm = "sha512";

    /// <summary>
    /// PBKDF2-HMAC-SHA512 iteration count, per the OWASP Password Storage Cheat Sheet. Raising this
    /// is safe and backward-compatible: each stored hash carries the iteration count it was built
    /// with, <see cref="Verify"/> honours that stored value, and <see cref="NeedsRehash"/> then flags
    /// the row for upgrade on the owner's next sign-in.
    /// </summary>
    public const int Iterations = 210_000;

    private const int SaltBytes = 16;
    private const int HashBytes = 32;

    /// <summary>Length of the old unsalted SHA-256 lowercase-hex hash (32 bytes = 64 hex chars).</summary>
    private const int LegacyHexLength = 64;

    /// <summary>
    /// Hashes <paramref name="plainText"/> with a fresh random salt. Returns a self-describing string
    /// that carries its own algorithm, iteration count, and salt, so stored hashes stay verifiable
    /// after those parameters are tuned.
    /// </summary>
    public static string Hash(string plainText)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Derive(plainText, salt, Iterations, HashBytes);

        return string.Join('$',
            Prefix,
            Algorithm,
            Iterations.ToString(),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    /// <summary>
    /// Constant-time check of <paramref name="plainText"/> against <paramref name="storedHash"/>.
    /// Accepts both the current PBKDF2 encoding and the legacy unsalted SHA-256 hex. Returns false
    /// (never throws) for a null, empty, or unparseable stored hash.
    /// </summary>
    public static bool Verify(string plainText, string? storedHash)
    {
        if (string.IsNullOrEmpty(storedHash))
            return false;

        if (IsLegacySha256(storedHash))
            return VerifyLegacySha256(plainText, storedHash);

        // Base64 never contains '$', so splitting on it cannot corrupt the salt or hash segments.
        var parts = storedHash.Split('$');
        if (parts.Length != 5 || parts[0] != Prefix || parts[1] != Algorithm)
            return false;

        if (!int.TryParse(parts[2], out var iterations) || iterations < 1)
            return false;

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[3]);
            expected = Convert.FromBase64String(parts[4]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (salt.Length == 0 || expected.Length == 0)
            return false;

        // Derive to the stored hash's own length so an older/shorter hash still verifies.
        var actual = Derive(plainText, salt, iterations, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    /// <summary>
    /// True when <paramref name="storedHash"/> should be re-hashed with the current parameters — it is
    /// either a legacy SHA-256 value or was built with a weaker iteration count. Callers should only
    /// act on this <b>after</b> <see cref="Verify"/> has returned true, since re-hashing needs the
    /// plain text. Returns false for anything unparseable: a hash that cannot be read cannot be
    /// verified either, so there is no successful-login path on which to upgrade it.
    /// </summary>
    public static bool NeedsRehash(string? storedHash)
    {
        if (string.IsNullOrEmpty(storedHash))
            return false;

        if (IsLegacySha256(storedHash))
            return true;

        var parts = storedHash.Split('$');
        if (parts.Length != 5 || parts[0] != Prefix || parts[1] != Algorithm)
            return false;

        return int.TryParse(parts[2], out var iterations) && iterations < Iterations;
    }

    private static byte[] Derive(string plainText, byte[] salt, int iterations, int outputBytes) =>
        Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(plainText),
            salt,
            iterations,
            HashAlgorithmName.SHA512,
            outputBytes);

    private static bool IsLegacySha256(string storedHash) =>
        storedHash.Length == LegacyHexLength && storedHash.All(Uri.IsHexDigit);

    private static bool VerifyLegacySha256(string plainText, string storedHash)
    {
        var actual = SHA256.HashData(Encoding.UTF8.GetBytes(plainText));
        byte[] expected;
        try
        {
            expected = Convert.FromHexString(storedHash);
        }
        catch (FormatException)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
