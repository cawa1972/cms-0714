using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

/// <summary>
/// Read-only view of the RowAudit change log. Protected by the global authorization filter like
/// every other controller; audit rows are written by the repositories, never through the API.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RowAuditController : ControllerBase
{
    private readonly IRowAuditRepository _repository;

    public RowAuditController(IRowAuditRepository repository) => _repository = repository;

    /// <summary>
    /// History of one record, newest first: <c>GET /api/rowaudit?tableName=Course&amp;pkid=123</c>.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RowAuditEntry>>> GetHistory(
        [FromQuery] string? tableName, [FromQuery] string? pkid)
    {
        if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(pkid))
            return BadRequest(new { message = "tableName and pkid are required." });

        var rows = await _repository.GetHistoryAsync(tableName.Trim(), pkid.Trim());
        return Ok(rows);
    }
}
