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
    // 1. Получить список всех заказов (кратко для таблицы на фронте)
    [HttpGet]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders = await orderRepository.GetAllAsync();
        return Ok(orders);
    }

    // 2. Получить детальную информацию о заказе (включая товары и клиента)
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderDetails(int id)
    {
        var order = await orderRepository.GetWithIncludesAsync(
            o => o.OrderId == id,
            o => o.Customer,     // Загружаем данные клиента
            o => o.OrderItems,   // Загружаем список позиций
            o => o.Employee      // Загружаем ответственного сотрудника
        );

        if (order == null) return NotFound();
        return Ok(order);
    }

    // 3. Создать новый заказ
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        if (dto.Items == null || !dto.Items.Any())
            return BadRequest("Заказ не может быть пустым.");

        // Создаем объект заказа
        var newOrder = new Order
        {
            CustomerId = dto.CustomerId,
            EmployeeId = dto.EmployeeId,
            PickupPointId = dto.PickupPointId,
            OrderDate = DateTime.UtcNow,
            Status = "Processing",
            PaymentStatus = dto.PaymentStatus,
            PriceTotal = 0 // Рассчитаем ниже
        };

        decimal totalSum = 0;

        // Сначала сохраняем сам заказ, чтобы получить OrderId
        await orderRepository.AddAsync(newOrder);
        // ВАЖНО: В реальном проекте здесь вызывается UnitOfWork.SaveChangesAsync()

        foreach (var item in dto.Items)
        {
            var product = await productRepository.GetByIdAsync(item.ProductId);
            if (product == null || product.StockQuantity < item.Quantity)
                return BadRequest($"Товар с ID {item.ProductId} недоступен или недостаточно на складе.");

            // Уменьшаем остаток на складе
            product.StockQuantity -= item.Quantity;
            productRepository.Update(product);

            // Добавляем позицию в заказ
            var orderItem = new OrderItem
            {
                OrderId = newOrder.OrderId,
                ProductId = item.ProductId,
                Quantity = item.Quantity
            };

            totalSum += product.Price * item.Quantity;
            await orderItemRepository.AddAsync(orderItem);
        }

        newOrder.PriceTotal = totalSum;
        orderRepository.Update(newOrder);

        // Опять же, здесь нужен общий SaveChanges через UnitOfWork

        return CreatedAtAction(nameof(GetOrderDetails), new { id = newOrder.OrderId }, newOrder);
    }

    // 4. Обновить статус заказа (например, из React админки)
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
    {
        var order = await orderRepository.GetByIdAsync(id);
        if (order == null) return NotFound();

        order.Status = status;
        orderRepository.Update(order);

        return Ok(new { Message = $"Статус заказа {id} обновлен на {status}" });
    }

    // 5. Удалить/Отменить заказ
    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var order = await orderRepository.GetByIdAsync(id);
        if (order == null) return NotFound();

        orderRepository.Delete(order);
        return NoContent();
    }
}

#region DTOs (Объекты передачи данных)

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