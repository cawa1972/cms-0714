using CMS.API.Models;

namespace CMS.API.Repositories;

public interface ICourseGroupRepository
{
    Task<IEnumerable<CourseGroup>> GetAllAsync();
    Task<IEnumerable<CourseGroup>> QueryAsync(CourseGroupQuery query);
    Task<CourseGroup?> GetByIdAsync(short pkid);

    /// <summary>Inserts a course group and returns the database-assigned pkid.</summary>
    Task<short> CreateAsync(CourseGroupRequest request);

    /// <summary>Updates a course group (keyed by pkid). Returns false if not found.</summary>
    Task<bool> UpdateAsync(CourseGroupRequest request);

    Task<bool> DeleteAsync(short pkid);
}
