using CMS.API.Data;
using CMS.API.Models;
using Dapper;

namespace CMS.API.Repositories;

public sealed class FeaturedPromoItemRepository : IFeaturedPromoItemRepository
{
    private readonly ISqlConnectionFactory _factory;

    public FeaturedPromoItemRepository(ISqlConnectionFactory factory) => _factory = factory;

    // Shared SELECT: FK columns aliased to the C# property names; labels resolved via JOINs
    // (both FKs are NOT NULL → INNER JOIN).
    private const string SelectColumns = """
        SELECT f.pkid                AS Pkid,
               f.ScheduleOn,
               f.TrainingCenter_pkid AS TrainingCenterPkid,
               f.Slot,
               f.Promotion_pkid      AS PromotionPkid,
               f.Topic,
               f.Description,
               p.PromoCode,
               tc.Name               AS TrainingCenterName
        FROM FeaturedPromoItem f
             INNER JOIN Promotion2 p      ON p.pkid  = f.Promotion_pkid
             INNER JOIN TrainingCenter tc ON tc.pkid = f.TrainingCenter_pkid
        """;

    private const string OrderBy = " ORDER BY f.ScheduleOn ASC, f.TrainingCenter_pkid ASC, f.Slot ASC";

    public async Task<IEnumerable<FeaturedPromoItem>> GetAllAsync()
    {
        using var db = _factory.Create();
        return await db.QueryAsync<FeaturedPromoItem>($"{SelectColumns}{OrderBy}");
    }

    public async Task<IEnumerable<FeaturedPromoItem>> QueryAsync(FeaturedPromoItemQuery query)
    {
        var where = new List<string>();
        var p = new DynamicParameters();

        if (query.ScheduleOnFrom.HasValue)
        {
            where.Add("f.ScheduleOn >= @ScheduleOnFrom");
            p.Add("ScheduleOnFrom", query.ScheduleOnFrom.Value);
        }

        if (query.ScheduleOnTo.HasValue)
        {
            where.Add("f.ScheduleOn <= @ScheduleOnTo");
            p.Add("ScheduleOnTo", query.ScheduleOnTo.Value);
        }

        if (query.TrainingCenterPkid.HasValue)
        {
            where.Add("f.TrainingCenter_pkid = @TrainingCenterPkid");
            p.Add("TrainingCenterPkid", query.TrainingCenterPkid.Value);
        }

        if (query.Slot.HasValue)
        {
            where.Add("f.Slot = @Slot");
            p.Add("Slot", query.Slot.Value);
        }

        var sql = SelectColumns;
        if (where.Count > 0)
            sql += " WHERE " + string.Join(" AND ", where);
        sql += OrderBy;

        using var db = _factory.Create();
        return await db.QueryAsync<FeaturedPromoItem>(sql, p);
    }

    public async Task<FeaturedPromoItem?> GetByIdAsync(int pkid)
    {
        using var db = _factory.Create();
        return await db.QuerySingleOrDefaultAsync<FeaturedPromoItem>(
            $"{SelectColumns} WHERE f.pkid = @pkid", new { pkid });
    }

    public async Task<int> CreateAsync(FeaturedPromoItemRequest request)
    {
        using var db = _factory.Create();
        // pkid is int IDENTITY: excluded from the column list, returned via SCOPE_IDENTITY().
        return await db.ExecuteScalarAsync<int>("""
            INSERT INTO FeaturedPromoItem
                (ScheduleOn, TrainingCenter_pkid, Slot, Promotion_pkid, Topic, Description)
            VALUES
                (@ScheduleOn, @TrainingCenterPkid, @Slot, @PromotionPkid, @Topic, @Description);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """, request);
    }

    public async Task<bool> UpdateAsync(FeaturedPromoItemRequest request)
    {
        using var db = _factory.Create();
        var affected = await db.ExecuteAsync("""
            UPDATE FeaturedPromoItem
               SET ScheduleOn = @ScheduleOn,
                   TrainingCenter_pkid = @TrainingCenterPkid,
                   Slot = @Slot,
                   Promotion_pkid = @PromotionPkid,
                   Topic = @Topic,
                   Description = @Description
             WHERE pkid = @Pkid;
            """, request);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int pkid)
    {
        using var db = _factory.Create();
        var affected = await db.ExecuteAsync(
            "DELETE FROM FeaturedPromoItem WHERE pkid = @pkid", new { pkid });
        return affected > 0;
    }

    public async Task<bool> SlotTakenAsync(DateOnly scheduleOn, short trainingCenterPkid, byte slot, int? excludePkid = null)
    {
        using var db = _factory.Create();
        return await db.ExecuteScalarAsync<int>("""
            SELECT COUNT(1)
            FROM FeaturedPromoItem
            WHERE ScheduleOn = @scheduleOn
              AND TrainingCenter_pkid = @trainingCenterPkid
              AND Slot = @slot
              AND (@excludePkid IS NULL OR pkid <> @excludePkid)
            """, new { scheduleOn, trainingCenterPkid, slot, excludePkid }) > 0;
    }

    public async Task<MoveSlotResult> MoveSlotAsync(int pkid, bool moveDown)
    {
        using var db = _factory.Create();
        db.Open();
        using var tx = db.BeginTransaction();

        var item = await db.QuerySingleOrDefaultAsync<FeaturedPromoItem>("""
            SELECT pkid                AS Pkid,
                   ScheduleOn,
                   TrainingCenter_pkid AS TrainingCenterPkid,
                   Slot
            FROM FeaturedPromoItem
            WHERE pkid = @pkid
            """, new { pkid }, tx);

        if (item is null)
            return MoveSlotResult.NotFound;

        var target = item.Slot + (moveDown ? 1 : -1);
        if (target is < 1 or > 3)
            return MoveSlotResult.OutOfRange;

        var occupant = await db.QuerySingleOrDefaultAsync<int?>("""
            SELECT pkid
            FROM FeaturedPromoItem
            WHERE ScheduleOn = @ScheduleOn
              AND TrainingCenter_pkid = @TrainingCenterPkid
              AND Slot = @target
            """, new { item.ScheduleOn, item.TrainingCenterPkid, target }, tx);

        if (occupant.HasValue)
        {
            // Swap via a temporary slot 0 so the unique key (ScheduleOn, TC, Slot) never collides.
            await db.ExecuteAsync("UPDATE FeaturedPromoItem SET Slot = 0 WHERE pkid = @pkid",
                new { pkid }, tx);
            await db.ExecuteAsync("UPDATE FeaturedPromoItem SET Slot = @slot WHERE pkid = @pkid",
                new { slot = item.Slot, pkid = occupant.Value }, tx);
        }

        await db.ExecuteAsync("UPDATE FeaturedPromoItem SET Slot = @target WHERE pkid = @pkid",
            new { target, pkid }, tx);

        tx.Commit();
        return MoveSlotResult.Moved;
    }

    public async Task<PromoCodeLookup?> GetPromoByCodeAsync(string promoCode)
    {
        using var db = _factory.Create();
        return await db.QuerySingleOrDefaultAsync<PromoCodeLookup>("""
            SELECT pkid AS PromotionPkid,
                   PromoCode,
                   Topic,
                   Description
            FROM Promotion2
            WHERE PromoCode = @promoCode
            """, new { promoCode = promoCode.Trim() });
    }
}
