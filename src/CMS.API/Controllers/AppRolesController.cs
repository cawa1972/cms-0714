using CMS.API.Models;
using CMS.API.Repositories;
using CMS.API.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

/// <summary>
/// Role management — <b>Admin only, controller-wide</b>: role membership grants privileges (a role
/// update syncs AppUserRole links), so every action requires the Admin role claim. Non-Admins get
/// 403; the role *options* for pickers come from the unrestricted LookupsController instead.
/// </summary>
[ApiController]
[Route("api/app-roles")]
[Authorize(Roles = AppRoles.Admin)]
public class AppRolesController : ControllerBase
{
    private readonly IAppRoleRepository _repository;

    public AppRolesController(IAppRoleRepository repository) => _repository = repository;

    /// <summary>List all roles.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppRole>>> GetAll()
        => Ok(await _repository.GetAllAsync());

    /// <summary>Filtered search.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IEnumerable<AppRole>>> Query([FromBody] AppRoleQuery query)
        => Ok(await _repository.QueryAsync(query));

    /// <summary>Get a single role (with assigned user ids) by RoleId.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AppRole>> GetById(string id)
    {
        var role = await _repository.GetByIdAsync(id);
        return role is null ? NotFound() : Ok(role);
    }

    /// <summary>Create a role. Returns 409 if the RoleId already exists.</summary>
    [HttpPost]
    public async Task<ActionResult<AppRole>> Create([FromBody] AppRoleRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (await _repository.ExistsAsync(request.RoleId))
            return Conflict(new { message = $"角色代碼「{request.RoleId}」已存在。" });

        await _repository.CreateAsync(request);
        var created = await _repository.GetByIdAsync(request.RoleId);
        return CreatedAtAction(nameof(GetById), new { id = request.RoleId }, created);
    }

    /// <summary>Update a role (RoleId taken from body). Returns 404 if not found.</summary>
    [HttpPut]
    public async Task<ActionResult<AppRole>> Update([FromBody] AppRoleRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var updated = await _repository.UpdateAsync(request);
        if (!updated)
            return NotFound();

        return Ok(await _repository.GetByIdAsync(request.RoleId));
    }

    /// <summary>Delete a role by RoleId.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _repository.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
