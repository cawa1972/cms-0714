using CMS.API.Models;

namespace CMS.API.Repositories;

/// <summary>Outcome of a slot move (+ / − on the weekly board).</summary>
public enum MoveSlotResult
{
    /// <summary>No FeaturedPromoItem with the given pkid.</summary>
    NotFound,

    /// <summary>The move would leave the valid slot range (1–3).</summary>
    OutOfRange,

    /// <summary>Slot changed; if the target slot was occupied the two rows were swapped.</summary>
    Moved,
}

public interface IFeaturedPromoItemRepository
{
    Task<IEnumerable<FeaturedPromoItem>> GetAllAsync();
    Task<IEnumerable<FeaturedPromoItem>> QueryAsync(FeaturedPromoItemQuery query);
    Task<FeaturedPromoItem?> GetByIdAsync(int pkid);
    Task<int> CreateAsync(FeaturedPromoItemRequest request);
    Task<bool> UpdateAsync(FeaturedPromoItemRequest request);
    Task<bool> DeleteAsync(int pkid);

    /// <summary>
    /// True if a row other than <paramref name="excludePkid"/> already occupies
    /// (ScheduleOn, TrainingCenter, Slot) — the table's unique key.
    /// </summary>
    Task<bool> SlotTakenAsync(DateOnly scheduleOn, short trainingCenterPkid, byte slot, int? excludePkid = null);

    /// <summary>
    /// Move the item one slot down (<paramref name="moveDown"/> = true, e.g. 1→2) or up
    /// (false, e.g. 2→1) within the same day/training center. Swaps with the occupant of the
    /// target slot when there is one; transactional so the unique key is never violated.
    /// </summary>
    Task<MoveSlotResult> MoveSlotAsync(int pkid, bool moveDown);

    /// <summary>Resolve a Promotion2 PromoCode (exact match) to its pkid + default texts.</summary>
    Task<PromoCodeLookup?> GetPromoByCodeAsync(string promoCode);
}
