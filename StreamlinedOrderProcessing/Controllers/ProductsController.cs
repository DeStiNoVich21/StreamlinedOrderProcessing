using Microsoft.AspNetCore.Mvc;
using StreamlinedOrderProcessing.Models;
using StreamlinedOrderProcessing.Repositories;

namespace StreamlinedOrderProcessing.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(
    IGenericRepository<Product> repository,
    IWebHostEnvironment env) : ControllerBase
{
    // 1. Получить все товары - доступно всем авторизованным
    [HttpGet]
    [AuthorizeRoles(UserRole.Admin, UserRole.Manager, UserRole.Employee)]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
    {
        var products = await repository.GetAllAsync();
        return Ok(products);
    }

    // 2. Получить товар по ID - доступно всем авторизованным
    [HttpGet("{id:int}")]
    [AuthorizeRoles(UserRole.Admin, UserRole.Manager, UserRole.Employee)]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        var product = await repository.GetByIdAsync(id);
        if (product == null) return NotFound($"Товар с ID {id} не найден.");

        return Ok(product);
    }

    // 3. Создать новый товар - только админ и менеджер
    [HttpPost]
    [AuthorizeRoles(UserRole.Admin, UserRole.Manager)]
    public async Task<IActionResult> Create([FromForm] ProductCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var product = new Product(dto.Title, dto.Price)
        {
            Description = dto.Description,
            StockQuantity = dto.StockQuantity
        };

        if (dto.Image != null && dto.Image.Length > 0)
        {
            product.ImageUrl = await SaveImage(dto.Image);
        }

        await repository.AddAsync(product);

        return CreatedAtAction(nameof(GetById), new { id = product.ProductId }, product);
    }

    // 4. Обновить товар - только админ и менеджер
    [HttpPut("{id:int}")]
    [AuthorizeRoles(UserRole.Admin, UserRole.Manager)]
    public async Task<IActionResult> Update(int id, [FromForm] ProductUpdateDto dto)
    {
        var product = await repository.GetByIdAsync(id);
        if (product == null) return NotFound();

        product.Title = dto.Title;
        product.Price = dto.Price;
        product.Description = dto.Description;
        product.StockQuantity = dto.StockQuantity;

        if (dto.Image != null)
        {
            product.ImageUrl = await SaveImage(dto.Image);
        }

        repository.Update(product);
        return NoContent();
    }

    // 5. Удалить товар - только админ
    [HttpDelete("{id:int}")]
    [AuthorizeRoles(UserRole.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await repository.GetByIdAsync(id);
        if (product == null) return NotFound();

        repository.Delete(product);
        return NoContent();
    }

    // Вспомогательный метод для сохранения картинки
    private async Task<string> SaveImage(IFormFile image)
    {
        var contentPath = env.WebRootPath;
        var path = Path.Combine(contentPath, "images", "products");

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
        var fullPath = Path.Combine(path, fileName);

        using var stream = new FileStream(fullPath, FileMode.Create);
        await image.CopyToAsync(stream);

        return $"/images/products/{fileName}";
    }
}

#region DTOs
public record ProductCreateDto(
    string Title,
    decimal Price,
    string? Description,
    int StockQuantity,
    IFormFile? Image
);

public record ProductUpdateDto(
    string Title,
    decimal Price,
    string? Description,
    int StockQuantity,
    IFormFile? Image
);
#endregion