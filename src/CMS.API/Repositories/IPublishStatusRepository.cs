using CMS.API.Models;

namespace CMS.API.Repositories;

public interface IPublishStatusRepository
{
    Task<IEnumerable<PublishStatus>> GetAllAsync();
    Task<IEnumerable<PublishStatus>> QueryAsync(PublishStatusQuery query);
    Task<PublishStatus?> GetByIdAsync(byte pkid);
    Task<bool> ExistsAsync(byte pkid);

    /// <summary>Inserts a status with its user-assigned pkid.</summary>
    Task CreateAsync(PublishStatusRequest request);

    /// <summary>Updates a status (keyed by pkid). Returns false if not found.</summary>
    Task<bool> UpdateAsync(PublishStatusRequest request);

    Task<bool> DeleteAsync(byte pkid);
}
