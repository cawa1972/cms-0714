namespace CMS.API.Models;

/// <summary>
/// Response model for the <c>FeaturedPromoItem</c> table (首頁上稿) — one promoted item shown on
/// the home page for a given day (<see cref="ScheduleOn"/>), training center and slot (1–3).
/// The pair (ScheduleOn, TrainingCenterPkid, Slot) is unique. <see cref="PromoCode"/> and
/// <see cref="TrainingCenterName"/> are resolved display labels populated via JOINs.
/// </summary>
public class FeaturedPromoItem
{
    public int Pkid { get; set; }
    public DateOnly ScheduleOn { get; set; }
    public short TrainingCenterPkid { get; set; }
    public byte Slot { get; set; }
    public int PromotionPkid { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Resolved FK display labels (from JOINs; not persisted).
    public string? PromoCode { get; set; }
    public string? TrainingCenterName { get; set; }
}
