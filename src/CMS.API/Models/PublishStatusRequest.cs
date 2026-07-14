using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>
/// Write DTO for creating/updating a <c>PublishStatus</c>.
/// <see cref="Pkid"/> is the natural (user-assigned tinyint) key: required on create, immutable on update.
/// </summary>
public class PublishStatusRequest
{
    [Range(0, 255)]
    public byte Pkid { get; set; }

    [Required]
    [StringLength(50)]
    public string Description { get; set; } = string.Empty;

    public bool IsDraft { get; set; }
    public bool IsPublished { get; set; }
    public bool IsDiscontinued { get; set; }
}
