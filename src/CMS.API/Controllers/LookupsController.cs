using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/lookups")]
public class LookupsController : ControllerBase
{
    private readonly ILookupRepository _repository;

    public LookupsController(ILookupRepository repository) => _repository = repository;

    /// <summary>AppUser options for the role's user multiselect.</summary>
    [HttpGet("app-users")]
    public async Task<ActionResult<IEnumerable<LookupItem>>> GetAppUsers()
        => Ok(await _repository.GetAppUsersAsync());

    /// <summary>PublishStatus options (value = pkid, label = Description). FK target for Course.</summary>
    [HttpGet("publish-statuses")]
    public async Task<ActionResult<IEnumerable<LookupItem>>> GetPublishStatuses()
        => Ok(await _repository.GetPublishStatusesAsync());
}
