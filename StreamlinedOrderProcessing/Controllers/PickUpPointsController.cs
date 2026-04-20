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

    [HttpPost]
    [AuthorizeRoles(UserRole.Admin)]
    public async Task<ActionResult<PickupPoint>> Create([FromBody] PickupPointDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Address))
            return BadRequest("Адрес пункта выдачи обязателен.");

        // Создаем объект и заполняем все поля из DDL
        var point = new PickupPoint
        {
            Address = dto.Address,
            ManagerName = dto.ManagerName,
            OpeningHours = dto.OpeningHours
        };

        await repository.AddAsync(point);
        await repository.SaveChangesAsync();
        // Не забудь вызвать SaveChanges в репозитории, если он не автоматический!

        return CreatedAtAction(nameof(GetById), new { id = point.PointId }, point);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PickupPoint>> GetById(int id)
    {
        var point = await repository.GetByIdAsync(id);
        return point == null ? NotFound() : Ok(point);
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
        await repository.SaveChangesAsync();
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
        await repository.SaveChangesAsync();
        return NoContent();
    }
}

#region DTOs
public record PickupPointDto(
    string Address,
    string? ManagerName,
    string? OpeningHours
);
#endregion