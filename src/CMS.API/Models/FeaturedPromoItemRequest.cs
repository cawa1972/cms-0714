using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>
/// Write DTO for creating/updating a <c>FeaturedPromoItem</c>.
/// <see cref="Pkid"/> is ignored on create (IDENTITY); on update it identifies the row.
/// <see cref="PromotionPkid"/> is resolved client-side from a PromoCode lookup.
/// </summary>
public class FeaturedPromoItemRequest
{
    public int Pkid { get; set; }

    public DateOnly ScheduleOn { get; set; }

    public short TrainingCenterPkid { get; set; }

    [Range(1, 3)]
    public byte Slot { get; set; }

    public int PromotionPkid { get; set; }

    [Required]
    [StringLength(100)]
    public string Topic { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string Description { get; set; } = string.Empty;
}
