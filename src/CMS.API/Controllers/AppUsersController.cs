using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/app-users")]
public class AppUsersController : ControllerBase
{
    private readonly IAppUserRepository _repository;

    public AppUsersController(IAppUserRepository repository) => _repository = repository;

    /// <summary>List all users.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppUser>>> GetAll()
        => Ok(await _repository.GetAllAsync());

    /// <summary>Filtered search.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IEnumerable<AppUser>>> Query([FromBody] AppUserQuery query)
        => Ok(await _repository.QueryAsync(query));

    /// <summary>Get a single user (with assigned role ids) by UserId.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AppUser>> GetById(string id)
    {
        var user = await _repository.GetByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Create a user. The password is set server-side from the SysConfig default. Returns 409 if the
    /// UserId already exists.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AppUser>> Create([FromBody] AppUserRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (await _repository.ExistsAsync(request.UserId))
            return Conflict(new { message = $"使用者代碼「{request.UserId}」已存在。" });

        await _repository.CreateAsync(request);
        var created = await _repository.GetByIdAsync(request.UserId);
        return CreatedAtAction(nameof(GetById), new { id = request.UserId }, created);
    }

    /// <summary>Update a user (UserId taken from body). Does not change the password. 404 if not found.</summary>
    [HttpPut]
    public async Task<ActionResult<AppUser>> Update([FromBody] AppUserRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var updated = await _repository.UpdateAsync(request);
        if (!updated)
            return NotFound();

        return Ok(await _repository.GetByIdAsync(request.UserId));
    }

    /// <summary>Delete a user by UserId.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _repository.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>
    /// Reset the user's password back to the SysConfig default (re-hashed). 204 on success,
    /// 404 if the user does not exist.
    /// </summary>
    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var reset = await _repository.ResetPasswordAsync(id);
        return reset ? NoContent() : NotFound();
    }
}
