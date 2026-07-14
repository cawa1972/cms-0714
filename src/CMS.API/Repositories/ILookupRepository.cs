using CMS.API.Models;

namespace CMS.API.Repositories;

public interface ILookupRepository
{
    /// <summary>AppUser options: value = UserId, label = "UserName (UserId)". Ordered by UserName.</summary>
    Task<IEnumerable<LookupItem>> GetAppUsersAsync();
}
