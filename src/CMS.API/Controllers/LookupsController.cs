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

    /// <summary>Partner options (value = pkid, label = Name). FK target for Course/Certification.</summary>
    [HttpGet("partners")]
    public async Task<ActionResult<IEnumerable<LookupItem>>> GetPartners()
        => Ok(await _repository.GetPartnersAsync());

    /// <summary>CourseGroup options (value = pkid, label = Description). FK target for Course / PartnerCourseGroup.</summary>
    [HttpGet("course-groups")]
    public async Task<ActionResult<IEnumerable<LookupItem>>> GetCourseGroups()
        => Ok(await _repository.GetCourseGroupsAsync());
}
