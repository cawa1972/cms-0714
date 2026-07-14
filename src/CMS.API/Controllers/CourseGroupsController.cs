using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/course-groups")]
public class CourseGroupsController : ControllerBase
{
    private readonly ICourseGroupRepository _repository;

    public CourseGroupsController(ICourseGroupRepository repository) => _repository = repository;

    /// <summary>List all course groups.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CourseGroup>>> GetAll()
        => Ok(await _repository.GetAllAsync());

    /// <summary>Filtered search.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IEnumerable<CourseGroup>>> Query([FromBody] CourseGroupQuery query)
        => Ok(await _repository.QueryAsync(query));

    /// <summary>Get a single course group by pkid.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CourseGroup>> GetById(short id)
    {
        var group = await _repository.GetByIdAsync(id);
        return group is null ? NotFound() : Ok(group);
    }

    /// <summary>Create a course group. The pkid is assigned by the database (IDENTITY).</summary>
    [HttpPost]
    public async Task<ActionResult<CourseGroup>> Create([FromBody] CourseGroupRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var pkid = await _repository.CreateAsync(request);
        var created = await _repository.GetByIdAsync(pkid);
        return CreatedAtAction(nameof(GetById), new { id = pkid }, created);
    }

    /// <summary>Update a course group (pkid taken from body). Returns 404 if not found.</summary>
    [HttpPut]
    public async Task<ActionResult<CourseGroup>> Update([FromBody] CourseGroupRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var updated = await _repository.UpdateAsync(request);
        if (!updated)
            return NotFound();

        return Ok(await _repository.GetByIdAsync(request.Pkid));
    }

    /// <summary>Delete a course group by pkid.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(short id)
    {
        var deleted = await _repository.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
