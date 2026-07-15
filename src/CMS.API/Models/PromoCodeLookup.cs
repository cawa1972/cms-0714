namespace CMS.API.Models;

/// <summary>
/// Result of resolving a <c>Promotion2.PromoCode</c> to its pkid, used by the
/// FeaturedPromoItem edit form (the user types a PromoCode; Topic / Description
/// default from the promotion).
/// </summary>
public class PromoCodeLookup
{
    public int PromotionPkid { get; set; }
    public string PromoCode { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
