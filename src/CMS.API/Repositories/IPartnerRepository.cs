using CMS.API.Models;

namespace CMS.API.Repositories;

public interface IPartnerRepository
{
    Task<IEnumerable<Partner>> GetAllAsync();
    Task<IEnumerable<Partner>> QueryAsync(PartnerQuery query);
    Task<Partner?> GetByIdAsync(short pkid);

    /// <summary>Inserts a partner and returns the DB-assigned IDENTITY pkid.</summary>
    Task<short> CreateAsync(PartnerRequest request);

    /// <summary>Updates a partner (keyed by pkid). Returns false if not found.</summary>
    Task<bool> UpdateAsync(PartnerRequest request);

    Task<bool> DeleteAsync(short pkid);
}
