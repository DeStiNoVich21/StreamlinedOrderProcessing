using Microsoft.AspNetCore.Mvc;
using StreamlinedOrderProcessing.Models;
using StreamlinedOrderProcessing.Repositories;

namespace StreamlinedOrderProcessing.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController(IGenericRepository<Employee> repository) : ControllerBase
{
    // 1. Получить список всех сотрудников - админ и менеджер
    [HttpGet]
    [AuthorizeRoles(UserRole.Admin, UserRole.Manager)]
    public async Task<ActionResult<IEnumerable<Employee>>> GetAll()
    {
        var employees = await repository.GetAllAsync();
        return Ok(employees);
    }

    // 2. Получить сотрудника по ID - админ и менеджер
    [HttpGet("{id:int}")]
    [AuthorizeRoles(UserRole.Admin, UserRole.Manager)]
    public async Task<ActionResult<Employee>> GetById(int id)
    {
        var employee = await repository.GetByIdAsync(id);
        if (employee == null)
            return NotFound(new { message = $"Сотрудник с ID {id} не найден" });

        return Ok(employee);
    }

    [HttpPost]
    [AuthorizeRoles(UserRole.Admin)]
    public async Task<ActionResult<Employee>> Create([FromBody] EmployeeCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var employee = new Employee
        {
            FullName = dto.FullName,
            JobTitle = dto.JobTitle,
            Phone = dto.Phone,
            PointId = dto.PointId // Привязываем точку при создании
        };

        await repository.AddAsync(employee);
        await repository.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = employee.EmployeeId }, employee);
    }

    [HttpPut("{id:int}")]
    [AuthorizeRoles(UserRole.Admin)]
    public async Task<IActionResult> Update(int id, [FromBody] EmployeeUpdateDto dto)
    {
        var employee = await repository.GetByIdAsync(id);
        if (employee == null) return NotFound();

        employee.FullName = dto.FullName;
        employee.JobTitle = dto.JobTitle;
        employee.Phone = dto.Phone;
        employee.PointId = dto.PointId; // Обновляем привязку к точке

        repository.Update(employee);
        await repository.SaveChangesAsync();
        return NoContent();
    }

    // 6. Поиск по должности
    [HttpGet("position/{position}")]
    [AuthorizeRoles(UserRole.Admin, UserRole.Manager)]
    public async Task<ActionResult<IEnumerable<Employee>>> GetByPosition(string position)
    {
        // Ищем по JobTitle
        var employees = await repository.FindAsync(e =>
            e.JobTitle != null && e.JobTitle.ToLower() == position.ToLower());
        return Ok(employees);
    }
    // 5. Удалить сотрудника
    [HttpDelete("{id:int}")]
    [AuthorizeRoles(UserRole.Admin)] // Удаление разрешено только админу
    public async Task<IActionResult> Delete(int id)
    {
        var employee = await repository.GetByIdAsync(id);

        if (employee == null)
            return NotFound(new { message = $"Сотрудник с ID {id} не найден" });

        try
        {
            repository.Delete(employee);
            await repository.SaveChangesAsync();
            return NoContent(); // Успешное удаление без возврата данных
        }
        catch (Exception)
        {
            // Обработка ошибки, если сотрудник привязан к заказам (Foreign Key constraint)
            return BadRequest(new { message = "Не удалось удалить сотрудника. Возможно, он указан в существующих заказах." });
        }
    }
    // Обновленные DTO с поддержкой PointId
    public record EmployeeCreateDto(string FullName, string? JobTitle, string? Phone, int? PointId);
    public record EmployeeUpdateDto(string FullName, string? JobTitle, string? Phone, int? PointId);
}

