using System.Security.Cryptography;
using System.Text;
using CMS.API.Security;
using Xunit;

namespace CMS.API.Tests;

/// <summary>
/// Locks in the PBKDF2 hashing contract: salted (so equal passwords never share a hash), verifiable,
/// and still able to read the legacy unsalted SHA-256 values so a hasher change never locks existing
/// users out.
/// </summary>
public class PasswordHasherTests
{
    private const string Password = "New-pw-456";

    /// <summary>The old storage format, reproduced here so the migration path has something to read.</summary>
    private static string LegacySha256(string plainText) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(plainText)));

    // ---- Salting -------------------------------------------------------------

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        // The whole point of the salt: two users with the same password must not collide, so one
        // cracked hash cannot unlock a second account.
        Assert.NotEqual(PasswordHasher.Hash(Password), PasswordHasher.Hash(Password));
    }

    [Fact]
    public void Hash_IsNotTheLegacyUnsaltedSha256()
    {
        Assert.NotEqual(LegacySha256(Password), PasswordHasher.Hash(Password));
    }

    // ---- Round trip ----------------------------------------------------------

    [Theory]
    [InlineData("New-pw-456")]
    [InlineData("CMS4fun#")]
    [InlineData("a")]                        // short
    [InlineData("  leading and trailing  ")] // whitespace is significant
    [InlineData("密碼-中文-123!")]            // non-ASCII (UTF-8 round trip)
    public void Verify_CorrectPassword_ReturnsTrue(string plainText)
    {
        Assert.True(PasswordHasher.Verify(plainText, PasswordHasher.Hash(plainText)));
    }

    [Theory]
    [InlineData("wrong")]
    [InlineData("New-pw-45")]   // one char short
    [InlineData("new-pw-456")]  // case differs
    [InlineData("")]
    public void Verify_WrongPassword_ReturnsFalse(string wrong)
    {
        Assert.False(PasswordHasher.Verify(wrong, PasswordHasher.Hash(Password)));
    }

    // ---- Legacy SHA-256 migration --------------------------------------------

    [Fact]
    public void Verify_LegacySha256Hash_CorrectPassword_ReturnsTrue()
    {
        // Existing rows must keep working — a hashing upgrade that locks everyone out is an outage.
        Assert.True(PasswordHasher.Verify(Password, LegacySha256(Password)));
    }

    [Fact]
    public void Verify_LegacySha256Hash_WrongPassword_ReturnsFalse()
    {
        Assert.False(PasswordHasher.Verify("wrong", LegacySha256(Password)));
    }

    [Fact]
    public void NeedsRehash_LegacySha256Hash_ReturnsTrue()
    {
        Assert.True(PasswordHasher.NeedsRehash(LegacySha256(Password)));
    }

    [Fact]
    public void NeedsRehash_CurrentHash_ReturnsFalse()
    {
        Assert.False(PasswordHasher.NeedsRehash(PasswordHasher.Hash(Password)));
    }

    [Fact]
    public void NeedsRehash_WeakerIterationCount_ReturnsTrue()
    {
        // A hash minted before the iteration count was raised must be flagged for upgrade.
        var weak = PasswordHasher.Hash(Password)
            .Replace($"${PasswordHasher.Iterations}$", $"${PasswordHasher.Iterations - 1000}$");

        Assert.True(PasswordHasher.NeedsRehash(weak));
    }

    [Fact]
    public void Verify_HashFromWeakerIterationCount_StillVerifies()
    {
        // Each hash carries the iteration count it was built with, so raising the constant must not
        // invalidate hashes already in the table.
        var salt = RandomNumberGenerator.GetBytes(16);
        var lowIterations = 1000;
        var derived = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(Password), salt, lowIterations, HashAlgorithmName.SHA512, 32);
        var stored = string.Join('$',
            "pbkdf2", "sha512", lowIterations.ToString(),
            Convert.ToBase64String(salt), Convert.ToBase64String(derived));

        Assert.True(PasswordHasher.Verify(Password, stored));
        Assert.True(PasswordHasher.NeedsRehash(stored));
    }

    // ---- Malformed input ------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("pbkdf2$sha512$notanumber$c2FsdA==$aGFzaA==")]
    [InlineData("pbkdf2$sha512$210000$not-base64!$aGFzaA==")]
    [InlineData("pbkdf2$sha256$210000$c2FsdA==$aGFzaA==")]  // wrong algorithm
    [InlineData("pbkdf2$sha512$210000$c2FsdA==")]           // too few segments
    public void Verify_MalformedStoredHash_ReturnsFalseAndDoesNotThrow(string? stored)
    {
        Assert.False(PasswordHasher.Verify(Password, stored));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-hash")]
    public void NeedsRehash_MalformedStoredHash_ReturnsFalse(string? stored)
    {
        // Unparseable hashes never verify, so there is no successful-login path on which to upgrade
        // them; reporting true would be a promise the login flow cannot keep.
        Assert.False(PasswordHasher.NeedsRehash(stored));
    }
}
