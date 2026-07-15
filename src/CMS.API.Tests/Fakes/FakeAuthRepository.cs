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
}
