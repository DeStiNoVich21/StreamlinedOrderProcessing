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
    // 5. Полное обновление заказа
    [HttpPut("{id}")]
    [AuthorizeRoles(UserRole.Admin, UserRole.Manager)]
    public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderDto dto)
    {
        // Загружаем заказ со всеми позициями
        var order = await orderRepository.GetWithIncludesAsync(
            o => o.OrderId == id,
            o => o.OrderItems
        );

        if (order == null) return NotFound();

        // 1. Обновляем базовые поля
        order.CustomerId = dto.CustomerId;
        order.EmployeeId = dto.EmployeeId;
        order.Status = dto.Status;

        // 2. Логика синхронизации товаров (Items)
        if (dto.Items != null)
        {
            // Находим товары, которые были в заказе, но отсутствуют в новом списке (удаленные)
            var itemsToRemove = order.OrderItems
                .Where(oldItem => !dto.Items.Any(newItem => newItem.ProductId == oldItem.ProductId))
                .ToList();

            foreach (var item in itemsToRemove)
            {
                // Возвращаем товар на склад
                var product = await productRepository.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity += item.Quantity;
                    productRepository.Update(product);
                }
                // Удаляем позицию
                orderItemRepository.Delete(item);
                order.TotalAmount -= (item.PriceAtPurchase * item.Quantity);
            }

            // Тут также можно добавить логику изменения количества существующих товаров, 
            // но для начала реализуем удаление, как в твоем интерфейсе.
        }

        orderRepository.Update(order);
        await orderRepository.SaveChangesAsync();

        return Ok(order);
    }

    // Добавь DTO в конец файла к остальным
    public record UpdateOrderDto(
        int CustomerId,
        int EmployeeId,
        string Status,
        List<OrderItemRequest> Items
    );
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

            // 1. Уменьшаем остаток
            product.StockQuantity -= itemRequest.Quantity;
            productRepository.Update(product);

            // 2. Создаем позицию заказа
            var orderItem = new OrderItem
            {
                // ВАЖНО: Используйте только ID, не присваивайте объект Product целиком!
                ProductId = product.ProductId,
                Quantity = itemRequest.Quantity,
                PriceAtPurchase = product.Price
            };

            runningTotal += product.Price * itemRequest.Quantity;

            // 3. Добавляем в коллекцию заказа
            newOrder.OrderItems.Add(orderItem);
        }

        newOrder.TotalAmount = runningTotal;

        // 4. Сохраняем ТОЛЬКО заказ (он потянет за собой новые OrderItems автоматически)
        await orderRepository.AddAsync(newOrder);
        await orderRepository.SaveChangesAsync(); // Не забудьте добавить этот метод в репозиторий!
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
        await orderRepository.SaveChangesAsync(); // Не забудьте добавить этот метод в репозиторий!

        return Ok(new { Message = $"Статус заказа {id} обновлен на {status}" });
    }

    [HttpDelete("{id}")]
    [AuthorizeRoles(UserRole.Admin)]
    public async Task<IActionResult> CancelOrder(int id)
    {
        // Загружаем заказ ВМЕСТЕ с его позициями (OrderItems)
        var order = await orderRepository.GetWithIncludesAsync(
            o => o.OrderId == id,
            o => o.OrderItems
        );

        if (order == null) return NotFound();

        // 1. Сначала удаляем все связанные позиции заказа
        if (order.OrderItems != null && order.OrderItems.Any())
        {
            foreach (var item in order.OrderItems.ToList())
            {
                orderItemRepository.Delete(item);
            }
        }

        // 2. Теперь, когда "детей" нет, удаляем самого "родителя"
        orderRepository.Delete(order);

        // 3. Сохраняем всё одной транзакцией
        await orderRepository.SaveChangesAsync();

        return NoContent();
    }

    #region Order Items Methods

    // А. Получить все позиции конкретного заказа
    [HttpGet("{orderId}/items")]
    [AuthorizeRoles(UserRole.Admin, UserRole.Manager, UserRole.Employee)]
    public async Task<IActionResult> GetOrderItems(int orderId)
    {
        var order = await orderRepository.GetByIdAsync(orderId);
        if (order == null) return NotFound($"Заказ #{orderId} не найден.");

        // ТЕПЕРЬ МЫ ИСПОЛЬЗУЕМ МЕТОД ДЛЯ СПИСКА
        var items = await orderItemRepository.FindWithIncludesAsync(
            oi => oi.OrderId == orderId,
            oi => oi.Product
        );

        return Ok(items);
    }

    // Б. Получить конкретную позицию товара в заказе
    [HttpGet("{orderId}/items/{productId}")]
    [AuthorizeRoles(UserRole.Admin, UserRole.Manager, UserRole.Employee)]
    public async Task<IActionResult> GetSingleOrderItem(int orderId, int productId)
    {
        var item = await orderItemRepository.GetWithIncludesAsync(
            oi => oi.OrderId == orderId && oi.ProductId == productId,
            oi => oi.Product
        );

        if (item == null) return NotFound("Позиция товара в данном заказе не найдена.");

        return Ok(item);
    }

    // В. Добавить товар в существующий заказ (или обновить количество)
    [HttpPost("{orderId}/items")]
    [AuthorizeRoles(UserRole.Admin, UserRole.Manager)]
    public async Task<IActionResult> AddItemToOrder(int orderId, [FromBody] OrderItemRequest dto)
    {
        var order = await orderRepository.GetByIdAsync(orderId);
        if (order == null) return NotFound("Заказ не найден.");

        var product = await productRepository.GetByIdAsync(dto.ProductId);
        if (product == null) return BadRequest("Товар не найден.");

        if (product.StockQuantity < dto.Quantity)
            return BadRequest("Недостаточно товара на складе.");

        // Проверяем, нет ли уже такого товара в заказе
        var existingItem = await orderItemRepository.GetWithIncludesAsync(
            oi => oi.OrderId == orderId && oi.ProductId == dto.ProductId
        );

        if (existingItem != null)
        {
            // Если товар уже есть, увеличиваем количество
            existingItem.Quantity += dto.Quantity;
            orderItemRepository.Update(existingItem);
        }
        else
        {
            // Если товара нет, создаем новую позицию
            var newItem = new OrderItem
            {
                OrderId = orderId,
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                PriceAtPurchase = product.Price // Фиксируем цену из прайса
            };
            await orderItemRepository.AddAsync(newItem);
        }

        // Обновляем остатки на складе
        product.StockQuantity -= dto.Quantity;
        productRepository.Update(product);

        // Обновляем общую сумму заказа
        order.TotalAmount += (product.Price * dto.Quantity);
        orderRepository.Update(order);
        await orderRepository.SaveChangesAsync(); // Не забудьте добавить этот метод в репозиторий!

        return Ok(new { Message = "Товар добавлен в заказ", NewTotal = order.TotalAmount });
    }

    // Г. Удалить позицию из заказа
    [HttpDelete("{orderId}/items/{productId}")]
    [AuthorizeRoles(UserRole.Admin)]
    public async Task<IActionResult> RemoveItemFromOrder(int orderId, int productId)
    {
        var item = await orderItemRepository.GetWithIncludesAsync(
            oi => oi.OrderId == orderId && oi.ProductId == productId
        );

        if (item == null) return NotFound();

        var order = await orderRepository.GetByIdAsync(orderId);

        // Возвращаем товар на склад
        var product = await productRepository.GetByIdAsync(productId);
        if (product != null)
        {
            product.StockQuantity += item.Quantity;
            productRepository.Update(product);
        }

        // Вычитаем сумму из заказа
        if (order != null)
        {
            order.TotalAmount -= (item.PriceAtPurchase * item.Quantity);
            orderRepository.Update(order);
        }

        orderItemRepository.Delete(item);
        await orderRepository.SaveChangesAsync(); // Не забудьте добавить этот метод в репозиторий!

        return NoContent();
    }

    #endregion
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