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

    // 3. Добавить нового сотрудника
    [HttpPost]
    [AuthorizeRoles(UserRole.Admin)]
    public async Task<ActionResult<Employee>> Create([FromBody] EmployeeCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Используем объектный инициализатор вместо конструктора
        var employee = new Employee
        {
            FullName = dto.FullName,
            JobTitle = dto.JobTitle, // исправлено с Position
            Phone = dto.Phone
        };

        await repository.AddAsync(employee);
        return CreatedAtAction(nameof(GetById), new { id = employee.EmployeeId }, employee);
    }

    // 4. Обновить данные
    [HttpPut("{id:int}")]
    [AuthorizeRoles(UserRole.Admin)]
    public async Task<IActionResult> Update(int id, [FromBody] EmployeeUpdateDto dto)
    {
        var employee = await repository.GetByIdAsync(id);
        if (employee == null) return NotFound();

        employee.FullName = dto.FullName;
        employee.JobTitle = dto.JobTitle; // исправлено с Position
        employee.Phone = dto.Phone;

        repository.Update(employee);
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

    // Не забудь обновить DTO
    public record EmployeeCreateDto(string FullName, string? JobTitle, string? Phone);
    public record EmployeeUpdateDto(string FullName, string? JobTitle, string? Phone);
}

