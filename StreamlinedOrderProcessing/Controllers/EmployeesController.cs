using Microsoft.AspNetCore.Mvc;
using StreamlinedOrderProcessing.Models;
using StreamlinedOrderProcessing.Repositories;

namespace StreamlinedOrderProcessing.Controllers;

[ApiController]
[Route("api/[controller]")]
// Используем первичный конструктор для внедрения репозитория
public class EmployeesController(IGenericRepository<Employee> repository) : ControllerBase
{
    // 1. Получить список всех сотрудников
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Employee>>> GetAll()
    {
        var employees = await repository.GetAllAsync();
        return Ok(employees);
    }

    // 2. Получить сотрудника по ID
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Employee>> GetById(int id)
    {
        var employee = await repository.GetByIdAsync(id);
        if (employee == null)
            return NotFound(new { message = $"Сотрудник с ID {id} не найден" });

        return Ok(employee);
    }

    // 3. Добавить нового сотрудника
    [HttpPost]
    public async Task<ActionResult<Employee>> Create([FromBody] EmployeeCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var employee = new Employee(dto.FullName, dto.Position);

        await repository.AddAsync(employee);
        // Примечание: Не забудьте вызвать SaveChanges в UnitOfWork или репозитории

        return CreatedAtAction(nameof(GetById), new { id = employee.EmployeeId }, employee);
    }

    // 4. Обновить данные сотрудника
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] EmployeeUpdateDto dto)
    {
        var employee = await repository.GetByIdAsync(id);
        if (employee == null) return NotFound();

        employee.FullName = dto.FullName;
        employee.Position = dto.Position;

        repository.Update(employee);
        return NoContent();
    }

    // 5. Удалить сотрудника
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var employee = await repository.GetByIdAsync(id);
        if (employee == null) return NotFound();

        repository.Delete(employee);
        return NoContent();
    }

    // 6. Специальный метод: Поиск сотрудников по должности
    // Полезно для фильтрации в React (например, только "Менеджеры")
    [HttpGet("position/{position}")]
    public async Task<ActionResult<IEnumerable<Employee>>> GetByPosition(string position)
    {
        var employees = await repository.FindAsync(e => e.Position.ToLower() == position.ToLower());
        return Ok(employees);
    }
}

#region DTOs

public record EmployeeCreateDto(string FullName, string Position);
public record EmployeeUpdateDto(string FullName, string Position);

#endregion