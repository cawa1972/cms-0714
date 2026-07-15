using CMS.API.Models;

namespace CMS.API.Repositories;

public interface ILookupRepository
{
    /// <summary>AppUser options: value = UserId, label = "UserName (UserId)". Ordered by UserName.</summary>
    Task<IEnumerable<LookupItem>> GetAppUsersAsync();

    /// <summary>AppRole options: value = RoleId, label = "RoleName (RoleId)". Ordered by RoleName.</summary>
    Task<IEnumerable<LookupItem>> GetAppRolesAsync();

    /// <summary>PublishStatus options: value = pkid (as string), label = Description. Ordered by pkid.</summary>
    Task<IEnumerable<LookupItem>> GetPublishStatusesAsync();

    /// <summary>Partner options: value = pkid (as string), label = Name. Ordered by DisplayOrder.</summary>
    Task<IEnumerable<LookupItem>> GetPartnersAsync();

    /// <summary>CourseGroup options: value = pkid (as string), label = Description. Ordered by pkid.</summary>
    Task<IEnumerable<LookupItem>> GetCourseGroupsAsync();

    /// <summary>TrainingCenter options: value = pkid (as string), label = Name. Ordered by DisplayOrder.</summary>
    Task<IEnumerable<LookupItem>> GetTrainingCentersAsync();
}
