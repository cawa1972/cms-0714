using CMS.API.Models;

namespace CMS.API.Repositories;

public interface ILookupRepository
{
    /// <summary>AppUser options: value = UserId, label = "UserName (UserId)". Ordered by UserName.</summary>
    Task<IEnumerable<LookupItem>> GetAppUsersAsync();

    /// <summary>PublishStatus options: value = pkid (as string), label = Description. Ordered by pkid.</summary>
    Task<IEnumerable<LookupItem>> GetPublishStatusesAsync();

    /// <summary>CourseGroup options: value = pkid (as string), label = Description. Ordered by pkid.</summary>
    Task<IEnumerable<LookupItem>> GetCourseGroupsAsync();
}
