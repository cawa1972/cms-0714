using CMS.API.Models;
using CMS.API.Pdf;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController : ControllerBase
{
    private readonly ICourseRepository _repository;
    private readonly ICourseFlyerRenderer _flyerRenderer;

    public CoursesController(ICourseRepository repository, ICourseFlyerRenderer flyerRenderer)
    {
        _repository = repository;
        _flyerRenderer = flyerRenderer;
    }

    /// <summary>List all courses.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Course>>> GetAll()
        => Ok(await _repository.GetAllAsync());

    /// <summary>Filtered search.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IEnumerable<Course>>> Query([FromBody] CourseQuery query)
        => Ok(await _repository.QueryAsync(query));

    /// <summary>Get a single course by pkid.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Course>> GetById(int id)
    {
        var course = await _repository.GetByIdAsync(id);
        return course is null ? NotFound() : Ok(course);
    }

    /// <summary>Create a course. The pkid is assigned by the database (IDENTITY).</summary>
    [HttpPost]
    public async Task<ActionResult<Course>> Create([FromBody] CourseRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var pkid = await _repository.CreateAsync(request);
        var created = await _repository.GetByIdAsync(pkid);
        return CreatedAtAction(nameof(GetById), new { id = pkid }, created);
    }

    /// <summary>Update a course (pkid taken from body). Returns 404 if not found.</summary>
    [HttpPut]
    public async Task<ActionResult<Course>> Update([FromBody] CourseRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var updated = await _repository.UpdateAsync(request);
        if (!updated)
            return NotFound();

        return Ok(await _repository.GetByIdAsync(request.Pkid));
    }

    /// <summary>Delete a course by pkid.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _repository.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Download the course flyer as a one-page A4 PDF. Read-only — no RowAudit.</summary>
    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> GetFlyer(int id)
    {
        var course = await _repository.GetByIdAsync(id);
        if (course is null)
            return NotFound();

        var bytes = _flyerRenderer.Render(course);

        // Both Content-Disposition parameters, explicitly: ASCII `filename` for legacy agents,
        // RFC 5987 UTF-8 `filename*` (the Chinese name browsers actually show). The FileNameStar
        // setter performs the RFC 5987 percent-encoding. CORS exposes this header so the Angular
        // blob download can read the name (fallback: frontend builds it locally).
        var contentDisposition = new ContentDispositionHeaderValue("attachment")
        {
            FileName = CourseFlyerFileName.Ascii(course),
            FileNameStar = CourseFlyerFileName.Utf8(course),
        };
        Response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();

        return File(bytes, "application/pdf");
    }
}
