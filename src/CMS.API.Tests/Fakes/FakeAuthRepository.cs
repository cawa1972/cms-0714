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

    public FakeAuthRepository(params AppUserCredential[] seed) => _credentials = seed.ToList();

    public Task<AppUserCredential?> GetCredentialAsync(string userId)
        => Task.FromResult(_credentials.FirstOrDefault(c => c.UserId == userId));
}
