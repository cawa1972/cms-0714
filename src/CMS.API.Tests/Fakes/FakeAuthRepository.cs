using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IAuthRepository"/>. Seeded with credential rows (which, unlike the client
/// DTO, carry the stored PasswordHash) so the login endpoint can be exercised without a live
/// SQL Server.
/// </summary>
public sealed class FakeAuthRepository : IAuthRepository
{
    private readonly List<AppUserCredential> _credentials;

    /// <summary>The UserId passed to the most recent <see cref="UpdateUserNameAsync"/> call (or null).</summary>
    public string? UpdatedUserId { get; private set; }

    /// <summary>The UserName passed to the most recent <see cref="UpdateUserNameAsync"/> call (or null).</summary>
    public string? UpdatedUserName { get; private set; }

    public FakeAuthRepository(params AppUserCredential[] seed) => _credentials = seed.ToList();

    public Task<AppUserCredential?> GetCredentialAsync(string userId)
        => Task.FromResult(_credentials.FirstOrDefault(c => c.UserId == userId));

    public Task<bool> UpdateUserNameAsync(string userId, string userName)
    {
        UpdatedUserId = userId;
        UpdatedUserName = userName;

        var credential = _credentials.FirstOrDefault(c => c.UserId == userId);
        if (credential is null)
            return Task.FromResult(false);

        credential.UserName = userName;
        return Task.FromResult(true);
    }

    /// <summary>The UserId passed to the most recent <see cref="UpdatePasswordAsync"/> call (or null).</summary>
    public string? PasswordUpdatedUserId { get; private set; }

    /// <summary>The hash passed to the most recent <see cref="UpdatePasswordAsync"/> call (or null).</summary>
    public string? UpdatedPasswordHash { get; private set; }

    /// <summary>When the last successful password update happened (mirrors SQL's GETDATE() stamp).</summary>
    public DateTime? PasswordUpdatedTime { get; private set; }

    public Task<bool> UpdatePasswordAsync(string userId, string passwordHash)
    {
        PasswordUpdatedUserId = userId;
        UpdatedPasswordHash = passwordHash;

        var credential = _credentials.FirstOrDefault(c => c.UserId == userId);
        if (credential is null)
            return Task.FromResult(false);

        credential.PasswordHash = passwordHash;
        PasswordUpdatedTime = DateTime.UtcNow;
        return Task.FromResult(true);
    }

    /// <summary>How many times <see cref="UpgradePasswordHashAsync"/> has been called.</summary>
    public int UpgradeCount { get; private set; }

    /// <summary>The hash passed to the most recent <see cref="UpgradePasswordHashAsync"/> call (or null).</summary>
    public string? UpgradedPasswordHash { get; private set; }

    public Task<bool> UpgradePasswordHashAsync(string userId, string passwordHash)
    {
        UpgradeCount++;
        UpgradedPasswordHash = passwordHash;

        var credential = _credentials.FirstOrDefault(c => c.UserId == userId);
        if (credential is null)
            return Task.FromResult(false);

        // Deliberately does NOT touch PasswordUpdatedTime — the real repository leaves that column
        // alone on a re-hash, and a test asserting that would be worthless against a fake that lies.
        credential.PasswordHash = passwordHash;
        return Task.FromResult(true);
    }
}
