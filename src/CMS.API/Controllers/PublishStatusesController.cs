using CMS.API.Models;
using CMS.API.Repositories;
using CMS.API.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

/// <summary>
/// PublishStatus management — <b>Admin only, controller-wide</b>: these are system-wide lifecycle
/// definitions, so every action requires the Admin role claim. Non-Admins get 403; the dropdown
/// *options* Course pages need come from the unrestricted LookupsController instead.
/// </summary>
[ApiController]
[Route("api/publish-statuses")]
[Authorize(Roles = AppRoles.Admin)]
public class PublishStatusesController : ControllerBase
{
    private readonly IPublishStatusRepository _repository;

    public PublishStatusesController(IPublishStatusRepository repository) => _repository = repository;

    /// <summary>List all publish statuses.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PublishStatus>>> GetAll()
        => Ok(await _repository.GetAllAsync());

    /// <summary>Filtered search.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IEnumerable<PublishStatus>>> Query([FromBody] PublishStatusQuery query)
        => Ok(await _repository.QueryAsync(query));

    /// <summary>Get a single publish status by pkid.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PublishStatus>> GetById(byte id)
    {
        var status = await _repository.GetByIdAsync(id);
        return status is null ? NotFound() : Ok(status);
    }

    /// <summary>Create a publish status. Returns 409 if the pkid already exists.</summary>
    [HttpPost]
    public async Task<ActionResult<PublishStatus>> Create([FromBody] PublishStatusRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (await _repository.ExistsAsync(request.Pkid))
            return Conflict(new { message = $"主代碼「{request.Pkid}」已存在。" });

        await _repository.CreateAsync(request);
        var created = await _repository.GetByIdAsync(request.Pkid);
        return CreatedAtAction(nameof(GetById), new { id = request.Pkid }, created);
    }

    /// <summary>Update a publish status (pkid taken from body). Returns 404 if not found.</summary>
    [HttpPut]
    public async Task<ActionResult<PublishStatus>> Update([FromBody] PublishStatusRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var updated = await _repository.UpdateAsync(request);
        if (!updated)
            return NotFound();

        return Ok(await _repository.GetByIdAsync(request.Pkid));
    }

    /// <summary>Delete a publish status by pkid.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(byte id)
    {
        var deleted = await _repository.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
