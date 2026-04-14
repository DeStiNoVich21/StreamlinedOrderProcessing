using Microsoft.AspNetCore.Mvc;
using StreamlinedOrderProcessing.Models;
using StreamlinedOrderProcessing.Repositories;

namespace StreamlinedOrderProcessing.Controllers;

[ApiController]
[Route("api/[controller]")]
// Используем первичный конструктор для внедрения репозитория
public class PickupPointsController(IGenericRepository<PickupPoint> repository) : ControllerBase
{
    // 1. Получить все пункты выдачи
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PickupPoint>>> GetAll()
    {
        var points = await repository.GetAllAsync();
        return Ok(points);
    }

    // 2. Получить конкретный пункт по ID
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PickupPoint>> GetById(int id)
    {
        var point = await repository.GetByIdAsync(id);
        if (point == null) return NotFound($"Пункт выдачи с ID {id} не найден.");

        return Ok(point);
    }

    // 3. Добавить новый пункт выдачи
    [HttpPost]
    public async Task<ActionResult<PickupPoint>> Create([FromBody] PickupPointDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Address))
            return BadRequest("Адрес пункта выдачи обязателен.");

        var point = new PickupPoint(dto.Address);

        await repository.AddAsync(point);
        // Примечание: Не забудьте вызвать SaveChanges в UnitOfWork или репозитории

        return CreatedAtAction(nameof(GetById), new { id = point.PickupPointId }, point);
    }

    // 4. Обновить данные пункта выдачи
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PickupPointDto dto)
    {
        var point = await repository.GetByIdAsync(id);
        if (point == null) return NotFound();

        point.Address = dto.Address;

        repository.Update(point);
        return NoContent();
    }

    // 5. Удалить пункт выдачи
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var point = await repository.GetByIdAsync(id);
        if (point == null) return NotFound();

        // Проверка: перед удалением стоит убедиться, что к этому пункту не привязаны активные заказы
        // Но для простоты текущей задачи просто удаляем
        repository.Delete(point);
        return NoContent();
    }
}

#region DTOs
// Используем record для передачи данных
public record PickupPointDto(string Address);
#endregion