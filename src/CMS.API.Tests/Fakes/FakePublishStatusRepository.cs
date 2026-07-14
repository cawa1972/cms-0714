using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IPublishStatusRepository"/> mirroring the real repository's contract
/// (keyword + tri-state bit filtering, user-assigned pkid, exists/not-found semantics) so controller
/// endpoints can be exercised without a live SQL Server.
/// </summary>
public sealed class FakePublishStatusRepository : IPublishStatusRepository
{
    private readonly List<PublishStatus> _statuses = [];

    public FakePublishStatusRepository(params PublishStatus[] seed) => _statuses.AddRange(seed);

    public Task<IEnumerable<PublishStatus>> GetAllAsync()
        => Task.FromResult(_statuses.OrderBy(s => s.Pkid).AsEnumerable());

    public Task<IEnumerable<PublishStatus>> QueryAsync(PublishStatusQuery query)
    {
        IEnumerable<PublishStatus> result = _statuses;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            result = result.Where(s => s.Description.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        if (query.IsDraft is not null)
            result = result.Where(s => s.IsDraft == query.IsDraft);

        if (query.IsPublished is not null)
            result = result.Where(s => s.IsPublished == query.IsPublished);

        if (query.IsDiscontinued is not null)
            result = result.Where(s => s.IsDiscontinued == query.IsDiscontinued);

        return Task.FromResult(result.OrderBy(s => s.Pkid).AsEnumerable());
    }

    public Task<PublishStatus?> GetByIdAsync(byte pkid)
        => Task.FromResult(_statuses.FirstOrDefault(s => s.Pkid == pkid));

    public Task<bool> ExistsAsync(byte pkid)
        => Task.FromResult(_statuses.Any(s => s.Pkid == pkid));

    public Task CreateAsync(PublishStatusRequest request)
    {
        _statuses.Add(new PublishStatus
        {
            Pkid = request.Pkid,
            Description = request.Description,
            IsDraft = request.IsDraft,
            IsPublished = request.IsPublished,
            IsDiscontinued = request.IsDiscontinued,
        });
        return Task.CompletedTask;
    }

    public Task<bool> UpdateAsync(PublishStatusRequest request)
    {
        var status = _statuses.FirstOrDefault(s => s.Pkid == request.Pkid);
        if (status is null)
            return Task.FromResult(false);

        status.Description = request.Description;
        status.IsDraft = request.IsDraft;
        status.IsPublished = request.IsPublished;
        status.IsDiscontinued = request.IsDiscontinued;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(byte pkid)
    {
        var removed = _statuses.RemoveAll(s => s.Pkid == pkid);
        return Task.FromResult(removed > 0);
    }
}
