using Microsoft.AspNetCore.Mvc;
using StreamlinedOrderProcessing.Models;
using StreamlinedOrderProcessing.Repositories;

namespace StreamlinedOrderProcessing.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController(IGenericRepository<Customer> repository) : ControllerBase
{
    // 1. Получить список всех клиентов - доступно всем авторизованным
    [HttpGet]
    [AuthorizeRoles(UserRole.Admin, UserRole.Manager, UserRole.Employee)]
    public async Task<ActionResult<IEnumerable<Customer>>> GetAll()
    {
        var customers = await repository.GetAllAsync();
        return Ok(customers);
    }

    // 2. Получить данные конкретного клиента по ID
    [HttpGet("{id:int}")]
    [AuthorizeRoles(UserRole.Admin, UserRole.Manager, UserRole.Employee)]
    public async Task<ActionResult<Customer>> GetById(int id)
    {
        var customer = await repository.GetByIdAsync(id);
        if (customer == null) return NotFound("Клиент не найден.");

        return Ok(customer);
    }

    // 3. Получить клиента вместе с его историей заказов
    [HttpGet("{id:int}/orders")]
    [AuthorizeRoles(UserRole.Admin, UserRole.Manager)]
    public async Task<ActionResult<Customer>> GetCustomerWithOrders(int id)
    {
        var customer = await repository.GetWithIncludesAsync(
            c => c.CustomerId == id,
            c => c.Orders
        );

        if (customer == null) return NotFound("Клиент или его заказы не найдены.");

        return Ok(customer);
    }

    // 4. Поиск клиента по Email или Имени
    [HttpGet("search")]
    [AuthorizeRoles(UserRole.Admin, UserRole.Manager, UserRole.Employee)]
    public async Task<ActionResult<IEnumerable<Customer>>> Search([FromQuery] string query)
    {
        var results = await repository.FindAsync(c =>
            c.FullName.Contains(query) || (c.Email != null && c.Email.Contains(query)));

        return Ok(results);
    }

    [HttpPost]
    [AuthorizeRoles(UserRole.Admin, UserRole.Manager)]
    public async Task<ActionResult<Customer>> Create([FromBody] CustomerDto dto)
    {
        // Убираем параметры из скобок (), используем {}
        var customer = new Customer
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Address = dto.Address,
            Phone = dto.Phone
        };

        await repository.AddAsync(customer);
        await repository.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = customer.CustomerId }, customer);
    }

    // 6. Обновить данные клиента - только админ и менеджер
    [HttpPut("{id:int}")]
    [AuthorizeRoles(UserRole.Admin, UserRole.Manager)]
    public async Task<IActionResult> Update(int id, [FromBody] CustomerDto dto)
    {
        var customer = await repository.GetByIdAsync(id);
        if (customer == null) return NotFound();

        customer.FullName = dto.FullName;
        customer.Email = dto.Email;
        customer.Address = dto.Address;
        customer.Phone = dto.Phone;

        repository.Update(customer);
        await repository.SaveChangesAsync();
        return NoContent();
    }

    // 7. Удалить клиента - только админ
    [HttpDelete("{id:int}")]
    [AuthorizeRoles(UserRole.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await repository.GetByIdAsync(id);
        if (customer == null) return NotFound();

        repository.Delete(customer);
        await repository.SaveChangesAsync();

        return NoContent();
    }
}

public record CustomerDto(
    string FullName,
    string Email,
    string? Address,
    string? Phone
);