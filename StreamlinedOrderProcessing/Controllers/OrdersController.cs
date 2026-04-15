using Microsoft.AspNetCore.Mvc;
using StreamlinedOrderProcessing.Models;
using StreamlinedOrderProcessing.Repositories;

namespace StreamlinedOrderProcessing.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(
    IGenericRepository<Order> orderRepository,
    IGenericRepository<Product> productRepository,
    IGenericRepository<OrderItem> orderItemRepository) : ControllerBase
{
    // 1. Получить список всех заказов - доступно всем авторизованным
    [HttpGet]
    [AuthorizeRoles(UserRole.Admin, UserRole.Manager, UserRole.Employee)]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders = await orderRepository.GetAllAsync();
        return Ok(orders);
    }

    // 2. Получить детальную информацию о заказе - доступно всем авторизованным
    [HttpGet("{id}")]
    [AuthorizeRoles(UserRole.Admin, UserRole.Manager, UserRole.Employee)]
    public async Task<IActionResult> GetOrderDetails(int id)
    {
        var order = await orderRepository.GetWithIncludesAsync(
            o => o.OrderId == id,
            o => o.Customer,
            o => o.OrderItems,
            o => o.Employee
        );

        if (order == null) return NotFound();
        return Ok(order);
    }

    // 3. Создать новый заказ - админ и менеджер
    [HttpPost]
    [AuthorizeRoles(UserRole.Admin, UserRole.Manager)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        if (dto.Items == null || !dto.Items.Any())
            return BadRequest("Заказ не может быть пустым.");

        // Создаем объект заказа. 
        // ВАЖНО: Используем TotalAmount, так как в DDL поле называется именно так.
        var newOrder = new Order
        {
            CustomerId = dto.CustomerId,
            EmployeeId = dto.EmployeeId,
            PickupPointId = dto.PickupPointId ?? 1, // Если null, ставим дефолтный ID из DDL
            Status = "Processing",
            TotalAmount = 0 // Рассчитаем ниже
        };

        decimal runningTotal = 0;

        foreach (var itemRequest in dto.Items)
        {
            var product = await productRepository.GetByIdAsync(itemRequest.ProductId);

            if (product == null)
                return BadRequest($"Товар {itemRequest.ProductId} не найден.");

            if (product.StockQuantity < itemRequest.Quantity)
                return BadRequest($"Недостаточно товара '{product.Title}' на складе.");

            // Уменьшаем остаток на складе
            product.StockQuantity -= itemRequest.Quantity;
            productRepository.Update(product);

            // Создаем позицию заказа
            var orderItem = new OrderItem
            {
                // Не передаем OrderId вручную, EF сделает это сам через навигацию
                Product = product,
                ProductId = product.ProductId,
                Quantity = itemRequest.Quantity,
                // ВАЖНО: В DDL есть поле price_at_purchase (фиксируем цену на момент покупки)
                PriceAtPurchase = product.Price
            };

            runningTotal += product.Price * itemRequest.Quantity;

            // Добавляем в коллекцию заказа
            newOrder.OrderItems.Add(orderItem);
        }

        newOrder.TotalAmount = runningTotal;

        // Сохраняем всё одним махом
        await orderRepository.AddAsync(newOrder);

        // ВНИМАНИЕ: Здесь должен быть вызов context.SaveChangesAsync() через UnitOfWork/Repository
        // Чтобы получить сгенерированный ID для CreatedAtAction

        return CreatedAtAction(nameof(GetOrderDetails), new { id = newOrder.OrderId }, newOrder);
    }

    // 4. Обновить статус заказа - менеджер и админ
    [HttpPatch("{id}/status")]
    [AuthorizeRoles(UserRole.Admin, UserRole.Manager)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
    {
        var order = await orderRepository.GetByIdAsync(id);
        if (order == null) return NotFound();

        order.Status = status;
        orderRepository.Update(order);

        return Ok(new { Message = $"Статус заказа {id} обновлен на {status}" });
    }

    // 5. Удалить/Отменить заказ - только админ
    [HttpDelete("{id}")]
    [AuthorizeRoles(UserRole.Admin)]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var order = await orderRepository.GetByIdAsync(id);
        if (order == null) return NotFound();

        orderRepository.Delete(order);
        return NoContent();
    }
}

#region DTOs
public record CreateOrderDto(
    int CustomerId,
    int EmployeeId,
    int? PickupPointId,
    string PaymentStatus,
    List<OrderItemRequest> Items
);

public record OrderItemRequest(
    int ProductId,
    int Quantity
);
#endregion