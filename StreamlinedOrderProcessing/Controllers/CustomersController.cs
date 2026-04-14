using Microsoft.AspNetCore.Mvc;
using StreamlinedOrderProcessing.Models;
using StreamlinedOrderProcessing.Repositories;

namespace StreamlinedOrderProcessing.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController(IGenericRepository<Customer> repository) : ControllerBase
{
    // 1. Получить список всех клиентов
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Customer>>> GetAll()
    {
        var customers = await repository.GetAllAsync();
        return Ok(customers);
    }

    // 2. Получить данные конкретного клиента по ID
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Customer>> GetById(int id)
    {
        var customer = await repository.GetByIdAsync(id);
        if (customer == null) return NotFound("Клиент не найден.");

        return Ok(customer);
    }

    // 3. Получить клиента вместе с его историей заказов
    // Используем наш метод GetWithIncludesAsync из репозитория
    [HttpGet("{id:int}/orders")]
    public async Task<ActionResult<Customer>> GetCustomerWithOrders(int id)
    {
        var customer = await repository.GetWithIncludesAsync(
            c => c.CustomerId == id,
            c => c.Orders
        );

        if (customer == null) return NotFound("Клиент или его заказы не найдены.");

        return Ok(customer);
    }

    // 4. Поиск клиента по Email или Имени (полезно для админки на React)
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<Customer>>> Search([FromQuery] string query)
    {
        var results = await repository.FindAsync(c =>
            c.FullName.Contains(query) || (c.Email != null && c.Email.Contains(query)));

        return Ok(results);
    }

    // 5. Добавить нового клиента
    [HttpPost]
    public async Task<ActionResult<Customer>> Create([FromBody] CustomerDto dto)
    {
        var customer = new Customer(dto.FullName, dto.Email)
        {
            Address = dto.Address,
            Phone = dto.Phone
        };

        await repository.AddAsync(customer);
        // Здесь предполагается сохранение изменений в БД

        return CreatedAtAction(nameof(GetById), new { id = customer.CustomerId }, customer);
    }

    // 6. Обновить данные клиента
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CustomerDto dto)
    {
        var customer = await repository.GetByIdAsync(id);
        if (customer == null) return NotFound();

        customer.FullName = dto.FullName;
        customer.Email = dto.Email;
        customer.Address = dto.Address;
        customer.Phone = dto.Phone;

        repository.Update(customer);
        return NoContent();
    }

    // 7. Удалить клиента
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await repository.GetByIdAsync(id);
        if (customer == null) return NotFound();

        repository.Delete(customer);
        return NoContent();
    }
}

// DTO для передачи данных клиента
public record CustomerDto(
    string FullName,
    string Email,
    string? Address,
    string? Phone
);