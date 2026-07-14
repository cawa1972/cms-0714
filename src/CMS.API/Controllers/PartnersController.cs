using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/partners")]
public class PartnersController : ControllerBase
{
    private readonly IPartnerRepository _repository;

    public PartnersController(IPartnerRepository repository) => _repository = repository;

    /// <summary>List all partners.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Partner>>> GetAll()
        => Ok(await _repository.GetAllAsync());

    /// <summary>Filtered search.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IEnumerable<Partner>>> Query([FromBody] PartnerQuery query)
        => Ok(await _repository.QueryAsync(query));

    /// <summary>Get a single partner by pkid.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Partner>> GetById(short id)
    {
        var partner = await _repository.GetByIdAsync(id);
        return partner is null ? NotFound() : Ok(partner);
    }

    /// <summary>Create a partner. The pkid is assigned by the database (IDENTITY).</summary>
    [HttpPost]
    public async Task<ActionResult<Partner>> Create([FromBody] PartnerRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var pkid = await _repository.CreateAsync(request);
        var created = await _repository.GetByIdAsync(pkid);
        return CreatedAtAction(nameof(GetById), new { id = pkid }, created);
    }

    /// <summary>Update a partner (pkid taken from body). Returns 404 if not found.</summary>
    [HttpPut]
    public async Task<ActionResult<Partner>> Update([FromBody] PartnerRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var updated = await _repository.UpdateAsync(request);
        if (!updated)
            return NotFound();

        return Ok(await _repository.GetByIdAsync(request.Pkid));
    }

    /// <summary>Delete a partner by pkid.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(short id)
    {
        var deleted = await _repository.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
