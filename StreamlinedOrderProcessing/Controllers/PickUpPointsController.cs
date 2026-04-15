using Microsoft.AspNetCore.Mvc;
using StreamlinedOrderProcessing.Models;
using StreamlinedOrderProcessing.Repositories;

namespace StreamlinedOrderProcessing.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PickupPointsController(IGenericRepository<PickupPoint> repository) : ControllerBase
{
    // 1. Получить все пункты выдачи - доступно всем авторизованным
    [HttpGet]
    [AuthorizeRoles(UserRole.Admin, UserRole.Manager, UserRole.Employee)]
    public async Task<ActionResult<IEnumerable<PickupPoint>>> GetAll()
    {
        var points = await repository.GetAllAsync();
        return Ok(points);
    }

    // 2. Получить конкретный пункт по ID - доступно всем авторизованным
    [HttpGet("{id:int}")]
    [AuthorizeRoles(UserRole.Admin, UserRole.Manager, UserRole.Employee)]
    public async Task<ActionResult<PickupPoint>> GetById(int id)
    {
        var point = await repository.GetByIdAsync(id);
        if (point == null) return NotFound($"Пункт выдачи с ID {id} не найден.");

        return Ok(point);
    }

    // 3. Добавить новый пункт выдачи - только админ
    [HttpPost]
    [AuthorizeRoles(UserRole.Admin)]
    public async Task<ActionResult<PickupPoint>> Create([FromBody] PickupPointDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Address))
            return BadRequest("Адрес пункта выдачи обязателен.");

        var point = new PickupPoint(dto.Address);

        await repository.AddAsync(point);

        return CreatedAtAction(nameof(GetById), new { id = point.PickupPointId }, point);
    }

    // 4. Обновить данные пункта выдачи - только админ
    [HttpPut("{id:int}")]
    [AuthorizeRoles(UserRole.Admin)]
    public async Task<IActionResult> Update(int id, [FromBody] PickupPointDto dto)
    {
        var point = await repository.GetByIdAsync(id);
        if (point == null) return NotFound();

        point.Address = dto.Address;

        repository.Update(point);
        return NoContent();
    }

    // 5. Удалить пункт выдачи - только админ
    [HttpDelete("{id:int}")]
    [AuthorizeRoles(UserRole.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var point = await repository.GetByIdAsync(id);
        if (point == null) return NotFound();

        repository.Delete(point);
        return NoContent();
    }
}

#region DTOs
public record PickupPointDto(string Address);
#endregion