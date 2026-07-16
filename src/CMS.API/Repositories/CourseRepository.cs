using System.Data;
using CMS.API.Audit;
using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class CourseRepository : ICourseRepository
{
    private const string TableName = "Course";

    private readonly ISqlConnectionFactory _factory;
    private readonly IRowAuditWriter _audit;

    public CourseRepository(ISqlConnectionFactory factory, IRowAuditWriter audit)
    {
        _factory = factory;
        _audit = audit;
    }

    // Shared SELECT: FK columns aliased to the C# property names; label columns resolved via JOINs.
    // Partner and PublishStatus are NOT NULL (INNER JOIN); CourseGroup is nullable (LEFT JOIN).
    private const string SelectColumns = """
        SELECT c.pkid AS Pkid,
               c.Title,
               c.OfficialTitle,
               c.CourseId,
               c.ProdCourseId,
               c.FriendlyUrl,
               c.DisplayOrder,
               c.Partner_pkid       AS PartnerPkid,
               c.CourseGroup_pkid   AS CourseGroupPkid,
               c.PublishStatus_pkid AS PublishStatusPkid,
               c.ScheduleOn,
               c.ScheduleOff,
               c.Hour,
               c.ListPrice,
               c.LearningCredit,
               c.Material,
               c.Objective,
               c.Target,
               c.Prerequisites,
               c.Outline,
               c.TowardCertOrExam,
               c.Note,
               c.OtherInfo,
               c.CanRepeat,
               p.Name         AS PartnerName,
               cg.Description AS CourseGroupDescription,
               ps.Description AS PublishStatusDescription,
               ps.IsPublished AS PublishStatusIsPublished
        FROM Course c
             INNER JOIN Partner p        ON p.pkid  = c.Partner_pkid
             LEFT  JOIN CourseGroup cg   ON cg.pkid = c.CourseGroup_pkid
             INNER JOIN PublishStatus ps ON ps.pkid = c.PublishStatus_pkid
        """;

    private const string OrderBy = " ORDER BY c.DisplayOrder ASC, c.pkid DESC";

    public async Task<IEnumerable<Course>> GetAllAsync()
    {
        using var db = _factory.Create();
        return await db.QueryAsync<Course>($"{SelectColumns}{OrderBy}");
    }

    public async Task<IEnumerable<Course>> QueryAsync(CourseQuery query)
    {
        var where = new List<string>();
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            where.Add("""
                (c.Title LIKE @kw OR c.OfficialTitle LIKE @kw OR c.CourseId LIKE @kw
                 OR c.ProdCourseId LIKE @kw OR c.FriendlyUrl LIKE @kw)
                """);
            p.Add("kw", $"%{query.Keyword.Trim()}%");
        }

        if (query.PartnerPkid.HasValue)
        {
            where.Add("c.Partner_pkid = @PartnerPkid");
            p.Add("PartnerPkid", query.PartnerPkid.Value);
        }

        if (query.CourseGroupPkid.HasValue)
        {
            where.Add("c.CourseGroup_pkid = @CourseGroupPkid");
            p.Add("CourseGroupPkid", query.CourseGroupPkid.Value);
        }

        if (query.PublishStatusPkid.HasValue)
        {
            where.Add("c.PublishStatus_pkid = @PublishStatusPkid");
            p.Add("PublishStatusPkid", query.PublishStatusPkid.Value);
        }

        if (query.ScheduleOnFrom.HasValue)
        {
            where.Add("c.ScheduleOn >= @ScheduleOnFrom");
            p.Add("ScheduleOnFrom", query.ScheduleOnFrom.Value);
        }

        if (query.ScheduleOnTo.HasValue)
        {
            where.Add("c.ScheduleOn <= @ScheduleOnTo");
            p.Add("ScheduleOnTo", query.ScheduleOnTo.Value);
        }

        if (query.ScheduleOffFrom.HasValue)
        {
            where.Add("c.ScheduleOff >= @ScheduleOffFrom");
            p.Add("ScheduleOffFrom", query.ScheduleOffFrom.Value);
        }

        if (query.ScheduleOffTo.HasValue)
        {
            where.Add("c.ScheduleOff <= @ScheduleOffTo");
            p.Add("ScheduleOffTo", query.ScheduleOffTo.Value);
        }

        if (query.CanRepeat.HasValue)
        {
            where.Add("c.CanRepeat = @CanRepeat");
            p.Add("CanRepeat", query.CanRepeat.Value);
        }

        var sql = SelectColumns;
        if (where.Count > 0)
            sql += " WHERE " + string.Join(" AND ", where);
        sql += OrderBy;

        using var db = _factory.Create();
        return await db.QueryAsync<Course>(sql, p);
    }

    public async Task<Course?> GetByIdAsync(int pkid)
    {
        using var db = _factory.Create();
        return await db.QuerySingleOrDefaultAsync<Course>(
            $"{SelectColumns} WHERE c.pkid = @pkid", new { pkid });
    }

    // Audit snapshot: raw table columns only — no JOIN labels — so the update audit's
    // changed-column list contains real Course columns, never derived display fields.
    private const string AuditSelect = """
        SELECT c.pkid AS Pkid,
               c.Title,
               c.OfficialTitle,
               c.CourseId,
               c.ProdCourseId,
               c.FriendlyUrl,
               c.DisplayOrder,
               c.Partner_pkid       AS PartnerPkid,
               c.CourseGroup_pkid   AS CourseGroupPkid,
               c.PublishStatus_pkid AS PublishStatusPkid,
               c.ScheduleOn,
               c.ScheduleOff,
               c.Hour,
               c.ListPrice,
               c.LearningCredit,
               c.Material,
               c.Objective,
               c.Target,
               c.Prerequisites,
               c.Outline,
               c.TowardCertOrExam,
               c.Note,
               c.OtherInfo,
               c.CanRepeat
        FROM Course c
        """;

    public async Task<int> CreateAsync(CourseRequest request)
    {
        using var db = _factory.Create();
        db.Open();
        using var tx = db.BeginTransaction();

        // pkid is int IDENTITY: excluded from the column list, returned via SCOPE_IDENTITY().
        var pkid = await db.ExecuteScalarAsync<int>("""
            INSERT INTO Course
                (Title, OfficialTitle, CourseId, ProdCourseId, FriendlyUrl, DisplayOrder,
                 Partner_pkid, CourseGroup_pkid, PublishStatus_pkid, ScheduleOn, ScheduleOff,
                 Hour, ListPrice, LearningCredit, Material, Objective, Target, Prerequisites,
                 Outline, TowardCertOrExam, Note, OtherInfo, CanRepeat)
            VALUES
                (@Title, @OfficialTitle, @CourseId, @ProdCourseId, @FriendlyUrl, @DisplayOrder,
                 @PartnerPkid, @CourseGroupPkid, @PublishStatusPkid, @ScheduleOn, @ScheduleOff,
                 @Hour, @ListPrice, @LearningCredit, @Material, @Objective, @Target, @Prerequisites,
                 @Outline, @TowardCertOrExam, @Note, @OtherInfo, @CanRepeat);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """, request, tx);

        var created = await GetSnapshotAsync(db, tx, pkid);
        if (created is not null)
            await _audit.LogInsertAsync(TableName, created, db, tx);

        tx.Commit();
        return pkid;
    }

    public async Task<bool> UpdateAsync(CourseRequest request)
    {
        using var db = _factory.Create();
        db.Open();
        using var tx = db.BeginTransaction();

        // Load "before" first so the audit row can list exactly the changed columns.
        var before = await GetSnapshotAsync(db, tx, request.Pkid);
        if (before is null)
            return false;

        var affected = await db.ExecuteAsync("""
            UPDATE Course
               SET Title = @Title,
                   OfficialTitle = @OfficialTitle,
                   CourseId = @CourseId,
                   ProdCourseId = @ProdCourseId,
                   FriendlyUrl = @FriendlyUrl,
                   DisplayOrder = @DisplayOrder,
                   Partner_pkid = @PartnerPkid,
                   CourseGroup_pkid = @CourseGroupPkid,
                   PublishStatus_pkid = @PublishStatusPkid,
                   ScheduleOn = @ScheduleOn,
                   ScheduleOff = @ScheduleOff,
                   Hour = @Hour,
                   ListPrice = @ListPrice,
                   LearningCredit = @LearningCredit,
                   Material = @Material,
                   Objective = @Objective,
                   Target = @Target,
                   Prerequisites = @Prerequisites,
                   Outline = @Outline,
                   TowardCertOrExam = @TowardCertOrExam,
                   Note = @Note,
                   OtherInfo = @OtherInfo,
                   CanRepeat = @CanRepeat
             WHERE pkid = @Pkid;
            """, request, tx);

        if (affected == 0)
            return false;

        var after = await GetSnapshotAsync(db, tx, request.Pkid);
        await _audit.LogUpdateAsync(TableName, before, after!, db, tx);

        tx.Commit();
        return true;
    }

    public async Task<bool> DeleteAsync(int pkid)
    {
        using var db = _factory.Create();
        db.Open();
        using var tx = db.BeginTransaction();

        // Load the row first so its Title is still available for the audit description.
        var row = await GetSnapshotAsync(db, tx, pkid);
        if (row is null)
            return false;

        await db.ExecuteAsync("DELETE FROM Course WHERE pkid = @pkid", new { pkid }, tx);
        await _audit.LogDeleteAsync(TableName, row, db, tx);

        tx.Commit();
        return true;
    }

    /// <summary>Audit snapshot (raw columns, no labels), read inside the caller's transaction.</summary>
    private static Task<Course?> GetSnapshotAsync(IDbConnection db, IDbTransaction tx, int pkid) =>
        db.QuerySingleOrDefaultAsync<Course?>(
            $"{AuditSelect} WHERE c.pkid = @pkid", new { pkid }, tx);
}
