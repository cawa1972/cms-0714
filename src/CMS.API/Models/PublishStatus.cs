namespace CMS.API.Models;

/// <summary>
/// Response model for the <c>PublishStatus</c> table (發布狀態).
/// The primary key <see cref="Pkid"/> is a <c>tinyint</c> that is <b>user-assigned</b> (not an
/// IDENTITY column), so it is supplied on create and immutable on update. The three flags are
/// independent bits describing the content lifecycle.
/// </summary>
public class PublishStatus
{
    public byte Pkid { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsDraft { get; set; }
    public bool IsPublished { get; set; }
    public bool IsDiscontinued { get; set; }
}
