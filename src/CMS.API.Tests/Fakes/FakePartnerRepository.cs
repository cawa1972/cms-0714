using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IPartnerRepository"/> mirroring the real repository's contract (keyword LIKE
/// filtering across the name/key columns, IDENTITY pkid assignment, not-found semantics) so controller
/// endpoints can be exercised without a live SQL Server.
/// </summary>
public sealed class FakePartnerRepository : IPartnerRepository
{
    private readonly List<Partner> _partners = [];
    private short _nextPkid = 1;

    public FakePartnerRepository(params Partner[] seed)
    {
        _partners.AddRange(seed);
        if (_partners.Count > 0)
            _nextPkid = (short)(_partners.Max(p => p.Pkid) + 1);
    }

    public Task<IEnumerable<Partner>> GetAllAsync()
        => Task.FromResult(_partners.OrderBy(p => p.DisplayOrder).AsEnumerable());

    public Task<IEnumerable<Partner>> QueryAsync(PartnerQuery query)
    {
        IEnumerable<Partner> result = _partners;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            result = result.Where(p =>
                p.Name.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || p.AppKey.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || p.NameOnPartnerMenu.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || p.NameOnCourseDetailPage.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(result.OrderBy(p => p.DisplayOrder).AsEnumerable());
    }

    public Task<Partner?> GetByIdAsync(short pkid)
        => Task.FromResult(_partners.FirstOrDefault(p => p.Pkid == pkid));

    public Task<short> CreateAsync(PartnerRequest request)
    {
        var pkid = _nextPkid++;
        _partners.Add(new Partner
        {
            Pkid = pkid,
            Name = request.Name,
            AppKey = request.AppKey,
            NameOnPartnerMenu = request.NameOnPartnerMenu,
            NameOnCourseDetailPage = request.NameOnCourseDetailPage,
            DisplayOrder = request.DisplayOrder,
            ImageFilename = request.ImageFilename,
        });
        return Task.FromResult(pkid);
    }

    public Task<bool> UpdateAsync(PartnerRequest request)
    {
        var partner = _partners.FirstOrDefault(p => p.Pkid == request.Pkid);
        if (partner is null)
            return Task.FromResult(false);

        partner.Name = request.Name;
        partner.AppKey = request.AppKey;
        partner.NameOnPartnerMenu = request.NameOnPartnerMenu;
        partner.NameOnCourseDetailPage = request.NameOnCourseDetailPage;
        partner.DisplayOrder = request.DisplayOrder;
        partner.ImageFilename = request.ImageFilename;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(short pkid)
    {
        var removed = _partners.RemoveAll(p => p.Pkid == pkid);
        return Task.FromResult(removed > 0);
    }
}
