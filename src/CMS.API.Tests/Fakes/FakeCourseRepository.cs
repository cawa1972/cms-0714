using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="ICourseRepository"/> mirroring the real repository's contract
/// (keyword + FK + date-range + CanRepeat filtering, IDENTITY-style auto-incrementing pkid,
/// not-found semantics) so controller endpoints can be exercised without a live SQL Server.
/// FK label fields (<c>PartnerName</c> etc.) are left as seeded — controller tests do not assert
/// on them.
/// </summary>
public sealed class FakeCourseRepository : ICourseRepository
{
    private readonly List<Course> _courses = [];
    private int _nextPkid;

    public FakeCourseRepository(params Course[] seed)
    {
        _courses.AddRange(seed);
        _nextPkid = _courses.Count == 0 ? 1 : _courses.Max(c => c.Pkid) + 1;
    }

    public Task<IEnumerable<Course>> GetAllAsync()
        => Task.FromResult(_courses.OrderBy(c => c.DisplayOrder).ThenByDescending(c => c.Pkid).AsEnumerable());

    public Task<IEnumerable<Course>> QueryAsync(CourseQuery query)
    {
        IEnumerable<Course> result = _courses;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            result = result.Where(c =>
                c.Title.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || (c.OfficialTitle?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false)
                || c.CourseId.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || c.ProdCourseId.Contains(kw, StringComparison.OrdinalIgnoreCase)
                || c.FriendlyUrl.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        if (query.PartnerPkid.HasValue)
            result = result.Where(c => c.PartnerPkid == query.PartnerPkid.Value);

        if (query.CourseGroupPkid.HasValue)
            result = result.Where(c => c.CourseGroupPkid == query.CourseGroupPkid.Value);

        if (query.PublishStatusPkid.HasValue)
            result = result.Where(c => c.PublishStatusPkid == query.PublishStatusPkid.Value);

        if (query.ScheduleOnFrom.HasValue)
            result = result.Where(c => c.ScheduleOn >= query.ScheduleOnFrom.Value);

        if (query.ScheduleOnTo.HasValue)
            result = result.Where(c => c.ScheduleOn <= query.ScheduleOnTo.Value);

        if (query.ScheduleOffFrom.HasValue)
            result = result.Where(c => c.ScheduleOff >= query.ScheduleOffFrom.Value);

        if (query.ScheduleOffTo.HasValue)
            result = result.Where(c => c.ScheduleOff <= query.ScheduleOffTo.Value);

        if (query.CanRepeat.HasValue)
            result = result.Where(c => c.CanRepeat == query.CanRepeat.Value);

        return Task.FromResult(result.OrderBy(c => c.DisplayOrder).ThenByDescending(c => c.Pkid).AsEnumerable());
    }

    public Task<Course?> GetByIdAsync(int pkid)
        => Task.FromResult(_courses.FirstOrDefault(c => c.Pkid == pkid));

    public Task<int> CreateAsync(CourseRequest request)
    {
        var pkid = _nextPkid++;
        _courses.Add(Map(request, pkid));
        return Task.FromResult(pkid);
    }

    public Task<bool> UpdateAsync(CourseRequest request)
    {
        var index = _courses.FindIndex(c => c.Pkid == request.Pkid);
        if (index < 0)
            return Task.FromResult(false);

        _courses[index] = Map(request, request.Pkid);
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int pkid)
    {
        var removed = _courses.RemoveAll(c => c.Pkid == pkid);
        return Task.FromResult(removed > 0);
    }

    private static Course Map(CourseRequest r, int pkid) => new()
    {
        Pkid = pkid,
        Title = r.Title,
        OfficialTitle = r.OfficialTitle,
        CourseId = r.CourseId,
        ProdCourseId = r.ProdCourseId,
        FriendlyUrl = r.FriendlyUrl,
        DisplayOrder = r.DisplayOrder,
        PartnerPkid = r.PartnerPkid,
        CourseGroupPkid = r.CourseGroupPkid,
        PublishStatusPkid = r.PublishStatusPkid,
        ScheduleOn = r.ScheduleOn,
        ScheduleOff = r.ScheduleOff,
        Hour = r.Hour,
        ListPrice = r.ListPrice,
        LearningCredit = r.LearningCredit,
        Material = r.Material,
        Objective = r.Objective,
        Target = r.Target,
        Prerequisites = r.Prerequisites,
        Outline = r.Outline,
        TowardCertOrExam = r.TowardCertOrExam,
        Note = r.Note,
        OtherInfo = r.OtherInfo,
        CanRepeat = r.CanRepeat,
    };
}
