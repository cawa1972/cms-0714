using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="ICourseGroupRepository"/> mirroring the real repository's contract
/// (keyword filtering, IDENTITY-style auto-incrementing pkid, not-found semantics) so controller
/// endpoints can be exercised without a live SQL Server.
/// </summary>
public sealed class FakeCourseGroupRepository : ICourseGroupRepository
{
    private readonly List<CourseGroup> _groups = [];
    private short _nextPkid;

    public FakeCourseGroupRepository(params CourseGroup[] seed)
    {
        _groups.AddRange(seed);
        _nextPkid = (short)(_groups.Count == 0 ? 1 : _groups.Max(g => g.Pkid) + 1);
    }

    public Task<IEnumerable<CourseGroup>> GetAllAsync()
        => Task.FromResult(_groups.OrderBy(g => g.Pkid).AsEnumerable());

    public Task<IEnumerable<CourseGroup>> QueryAsync(CourseGroupQuery query)
    {
        IEnumerable<CourseGroup> result = _groups;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            result = result.Where(g => g.Description.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(result.OrderBy(g => g.Pkid).AsEnumerable());
    }

    public Task<CourseGroup?> GetByIdAsync(short pkid)
        => Task.FromResult(_groups.FirstOrDefault(g => g.Pkid == pkid));

    public Task<short> CreateAsync(CourseGroupRequest request)
    {
        var pkid = _nextPkid++;
        _groups.Add(new CourseGroup { Pkid = pkid, Description = request.Description });
        return Task.FromResult(pkid);
    }

    public Task<bool> UpdateAsync(CourseGroupRequest request)
    {
        var group = _groups.FirstOrDefault(g => g.Pkid == request.Pkid);
        if (group is null)
            return Task.FromResult(false);

        group.Description = request.Description;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(short pkid)
    {
        var removed = _groups.RemoveAll(g => g.Pkid == pkid);
        return Task.FromResult(removed > 0);
    }
}
