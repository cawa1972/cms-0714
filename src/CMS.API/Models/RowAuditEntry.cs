namespace CMS.API.Models;

/// <summary>
/// One line of a record's change history as returned by <c>GET /api/rowaudit</c> — the read-side
/// projection of <see cref="RowAudit"/> (only the columns the history UI shows; table name and
/// pkid are the caller's filter, so they are not echoed back per row).
/// </summary>
public class RowAuditEntry
{
    public DateTime DateTime { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string? ActionDesc { get; set; }
}
