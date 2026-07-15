namespace CMS.API.Models;

/// <summary>
/// Search DTO for <c>POST /api/featured-promo-items/query</c>. All fields are optional; a null
/// field imposes no constraint. The board UI sends <see cref="ScheduleOnFrom"/> /
/// <see cref="ScheduleOnTo"/> as the Monday–Sunday bounds of the selected week plus the active
/// <see cref="TrainingCenterPkid"/> tab.
/// </summary>
public class FeaturedPromoItemQuery
{
    public DateOnly? ScheduleOnFrom { get; set; }
    public DateOnly? ScheduleOnTo { get; set; }
    public short? TrainingCenterPkid { get; set; }
    public byte? Slot { get; set; }
}
