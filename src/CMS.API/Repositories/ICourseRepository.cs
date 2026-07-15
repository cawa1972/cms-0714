using CMS.API.Models;

namespace CMS.API.Repositories;

public interface ICourseRepository
{
    Task<IEnumerable<Course>> GetAllAsync();
    Task<IEnumerable<Course>> QueryAsync(CourseQuery query);
    Task<Course?> GetByIdAsync(int pkid);

    /// <summary>Inserts a course and returns the database-assigned pkid.</summary>
    Task<int> CreateAsync(CourseRequest request);

    /// <summary>Updates a course (keyed by pkid). Returns false if not found.</summary>
    Task<bool> UpdateAsync(CourseRequest request);

    Task<bool> DeleteAsync(int pkid);
}
