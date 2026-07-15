using CMS.API.Models;
using CMS.API.Repositories;

namespace CMS.API.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IFeaturedPromoItemRepository"/> mirroring the real repository's contract
/// (week/date-range + training-center filtering, IDENTITY-style pkid, unique
/// (ScheduleOn, TrainingCenter, Slot) semantics, slot move/swap, PromoCode resolution) so
/// controller endpoints can be exercised without a live SQL Server. Promotions are seeded
/// separately; PromoCode labels on items are resolved from that seed like the real JOIN.
/// </summary>
public sealed class FakeFeaturedPromoItemRepository : IFeaturedPromoItemRepository
{
    private readonly List<FeaturedPromoItem> _items = [];
    private readonly List<PromoCodeLookup> _promotions = [];
    private int _nextPkid;

    public FakeFeaturedPromoItemRepository(
        IEnumerable<FeaturedPromoItem>? items = null,
        IEnumerable<PromoCodeLookup>? promotions = null)
    {
        _items.AddRange(items ?? []);
        _promotions.AddRange(promotions ?? []);
        _nextPkid = _items.Count == 0 ? 1 : _items.Max(i => i.Pkid) + 1;
    }

    public Task<IEnumerable<FeaturedPromoItem>> GetAllAsync()
        => Task.FromResult(Ordered(_items));

    public Task<IEnumerable<FeaturedPromoItem>> QueryAsync(FeaturedPromoItemQuery query)
    {
        IEnumerable<FeaturedPromoItem> result = _items;

        if (query.ScheduleOnFrom.HasValue)
            result = result.Where(i => i.ScheduleOn >= query.ScheduleOnFrom.Value);

        if (query.ScheduleOnTo.HasValue)
            result = result.Where(i => i.ScheduleOn <= query.ScheduleOnTo.Value);

        if (query.TrainingCenterPkid.HasValue)
            result = result.Where(i => i.TrainingCenterPkid == query.TrainingCenterPkid.Value);

        if (query.Slot.HasValue)
            result = result.Where(i => i.Slot == query.Slot.Value);

        return Task.FromResult(Ordered(result));
    }

    public Task<FeaturedPromoItem?> GetByIdAsync(int pkid)
        => Task.FromResult(_items.FirstOrDefault(i => i.Pkid == pkid));

    public Task<int> CreateAsync(FeaturedPromoItemRequest request)
    {
        var pkid = _nextPkid++;
        _items.Add(Map(request, pkid));
        return Task.FromResult(pkid);
    }

    public Task<bool> UpdateAsync(FeaturedPromoItemRequest request)
    {
        var index = _items.FindIndex(i => i.Pkid == request.Pkid);
        if (index < 0)
            return Task.FromResult(false);

        _items[index] = Map(request, request.Pkid);
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int pkid)
    {
        var removed = _items.RemoveAll(i => i.Pkid == pkid);
        return Task.FromResult(removed > 0);
    }

    public Task<bool> SlotTakenAsync(DateOnly scheduleOn, short trainingCenterPkid, byte slot, int? excludePkid = null)
        => Task.FromResult(_items.Any(i =>
            i.ScheduleOn == scheduleOn
            && i.TrainingCenterPkid == trainingCenterPkid
            && i.Slot == slot
            && i.Pkid != excludePkid));

    public Task<MoveSlotResult> MoveSlotAsync(int pkid, bool moveDown)
    {
        var item = _items.FirstOrDefault(i => i.Pkid == pkid);
        if (item is null)
            return Task.FromResult(MoveSlotResult.NotFound);

        var target = item.Slot + (moveDown ? 1 : -1);
        if (target is < 1 or > 3)
            return Task.FromResult(MoveSlotResult.OutOfRange);

        var occupant = _items.FirstOrDefault(i =>
            i.ScheduleOn == item.ScheduleOn
            && i.TrainingCenterPkid == item.TrainingCenterPkid
            && i.Slot == target);

        if (occupant is not null)
            occupant.Slot = item.Slot;
        item.Slot = (byte)target;

        return Task.FromResult(MoveSlotResult.Moved);
    }

    public Task<PromoCodeLookup?> GetPromoByCodeAsync(string promoCode)
        => Task.FromResult(_promotions.FirstOrDefault(p => p.PromoCode == promoCode.Trim()));

    private static IEnumerable<FeaturedPromoItem> Ordered(IEnumerable<FeaturedPromoItem> items)
        => items
            .OrderBy(i => i.ScheduleOn)
            .ThenBy(i => i.TrainingCenterPkid)
            .ThenBy(i => i.Slot)
            .ToList();

    private FeaturedPromoItem Map(FeaturedPromoItemRequest r, int pkid) => new()
    {
        Pkid = pkid,
        ScheduleOn = r.ScheduleOn,
        TrainingCenterPkid = r.TrainingCenterPkid,
        Slot = r.Slot,
        PromotionPkid = r.PromotionPkid,
        Topic = r.Topic,
        Description = r.Description,
        PromoCode = _promotions.FirstOrDefault(p => p.PromotionPkid == r.PromotionPkid)?.PromoCode,
        TrainingCenterName = null,
    };
}
