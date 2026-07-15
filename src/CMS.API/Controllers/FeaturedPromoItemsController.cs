using CMS.API.Models;
using CMS.API.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CMS.API.Controllers;

[ApiController]
[Route("api/featured-promo-items")]
public class FeaturedPromoItemsController : ControllerBase
{
    private readonly IFeaturedPromoItemRepository _repository;

    public FeaturedPromoItemsController(IFeaturedPromoItemRepository repository)
        => _repository = repository;

    /// <summary>List all featured promo items.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FeaturedPromoItem>>> GetAll()
        => Ok(await _repository.GetAllAsync());

    /// <summary>Filtered search — the board sends the week's Monday/Sunday + training center.</summary>
    [HttpPost("query")]
    public async Task<ActionResult<IEnumerable<FeaturedPromoItem>>> Query(
        [FromBody] FeaturedPromoItemQuery query)
        => Ok(await _repository.QueryAsync(query));

    /// <summary>Get a single featured promo item by pkid.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<FeaturedPromoItem>> GetById(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>
    /// Create an item. Returns 409 when (ScheduleOn, TrainingCenter, Slot) is already occupied
    /// (the table's unique key).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<FeaturedPromoItem>> Create([FromBody] FeaturedPromoItemRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (await _repository.SlotTakenAsync(request.ScheduleOn, request.TrainingCenterPkid, request.Slot))
            return Conflict($"該時段已有資料：{request.ScheduleOn:yyyy-MM-dd} 版位 {request.Slot}。");

        var pkid = await _repository.CreateAsync(request);
        var created = await _repository.GetByIdAsync(pkid);
        return CreatedAtAction(nameof(GetById), new { id = pkid }, created);
    }

    /// <summary>Update an item (pkid taken from body). 404 if not found, 409 on slot conflict.</summary>
    [HttpPut]
    public async Task<ActionResult<FeaturedPromoItem>> Update([FromBody] FeaturedPromoItemRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (await _repository.SlotTakenAsync(
                request.ScheduleOn, request.TrainingCenterPkid, request.Slot, request.Pkid))
            return Conflict($"該時段已有資料：{request.ScheduleOn:yyyy-MM-dd} 版位 {request.Slot}。");

        var updated = await _repository.UpdateAsync(request);
        if (!updated)
            return NotFound();

        return Ok(await _repository.GetByIdAsync(request.Pkid));
    }

    /// <summary>Delete an item by pkid.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _repository.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>
    /// Move an item one slot up or down within its day/training center (the board's − / +
    /// buttons), swapping with the occupant of the target slot when there is one.
    /// </summary>
    [HttpPost("{id:int}/move")]
    public async Task<IActionResult> Move(int id, [FromQuery] string direction)
    {
        if (direction is not ("up" or "down"))
            return BadRequest("direction 必須是 'up' 或 'down'。");

        var result = await _repository.MoveSlotAsync(id, moveDown: direction == "down");
        return result switch
        {
            MoveSlotResult.NotFound => NotFound(),
            MoveSlotResult.OutOfRange => BadRequest("已在邊界版位，無法再移動。"),
            _ => NoContent(),
        };
    }

    /// <summary>Resolve a Promotion2 PromoCode to its pkid + default Topic/Description.</summary>
    [HttpGet("promo-lookup")]
    public async Task<ActionResult<PromoCodeLookup>> PromoLookup([FromQuery] string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest("code 為必填。");

        var promo = await _repository.GetPromoByCodeAsync(code);
        return promo is null ? NotFound() : Ok(promo);
    }
}
