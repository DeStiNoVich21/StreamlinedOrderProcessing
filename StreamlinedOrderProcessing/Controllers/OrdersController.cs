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

        var newOrder = new Order
        {
            CustomerId = dto.CustomerId,
            EmployeeId = dto.EmployeeId,
            PickupPointId = dto.PickupPointId,
            OrderDate = DateTime.UtcNow,
            Status = "Processing",
            PaymentStatus = dto.PaymentStatus,
            PriceTotal = 0
        };

        decimal totalSum = 0;

        await orderRepository.AddAsync(newOrder);

        foreach (var item in dto.Items)
        {
            var product = await productRepository.GetByIdAsync(item.ProductId);
            if (product == null || product.StockQuantity < item.Quantity)
                return BadRequest($"Товар с ID {item.ProductId} недоступен или недостаточно на складе.");

            product.StockQuantity -= item.Quantity;
            productRepository.Update(product);

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