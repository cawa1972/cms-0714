using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>
/// Write DTO for creating/updating a <c>Partner</c>.
/// <see cref="Pkid"/> is a <c>smallint IDENTITY</c>: ignored on create (the DB assigns it) and used as
/// the key on update.
/// </summary>
public class PartnerRequest
{
    public short Pkid { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string AppKey { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string NameOnPartnerMenu { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string NameOnCourseDetailPage { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    [StringLength(50)]
    public string? ImageFilename { get; set; }
}
