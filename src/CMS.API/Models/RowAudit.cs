namespace CMS.API.Models;

/// <summary>
/// One row of the <c>RowAudit</c> table — a cross-cutting change-log entry describing a single
/// Insert / Update / Delete against any business table. <see cref="Pkid"/> is an <c>int IDENTITY</c>
/// assigned by the database and is never supplied on insert.
/// </summary>
public class RowAudit
{
    public int Pkid { get; set; }

    /// <summary>Business table the change happened on (e.g. "Course"). varchar(50).</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>Signed-in user's UserName from the JWT, or "system" when unauthenticated. nvarchar(100).</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>The changed row's pkid, as a string. nvarchar(100).</summary>
    public string PrimaryKeyValues { get; set; } = string.Empty;

    /// <summary>"Insert" | "Update" | "Delete". varchar(20).</summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Insert/Delete: the entity's first string-property value. Update: comma-separated names of the
    /// properties that changed. varchar(1000).
    /// </summary>
    public string? ActionDesc { get; set; }

    /// <summary>When the change happened.</summary>
    public DateTime DateTime { get; set; }
}
