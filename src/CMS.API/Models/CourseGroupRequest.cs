using System.ComponentModel.DataAnnotations;

namespace CMS.API.Models;

/// <summary>
/// Write DTO for creating/updating a <c>CourseGroup</c>.
/// <see cref="Pkid"/> is ignored on create (the database assigns it via IDENTITY); on update it
/// identifies the row to modify.
/// </summary>
public class CourseGroupRequest
{
    public short Pkid { get; set; }

    [Required]
    [StringLength(100)]
    public string Description { get; set; } = string.Empty;
}
