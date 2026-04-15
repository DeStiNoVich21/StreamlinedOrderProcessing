using Microsoft.EntityFrameworkCore;
using StreamlinedOrderProcessing.Models;

namespace StreamlinedOrderProcessing.DataContext;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<PickupPoint> PickupPoints { get; set; }

    // ВОТ ЭТОЙ СТРОКИ НЕ ХВАТАЛО:
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Настройка составного ключа для Order_Items
        modelBuilder.Entity<OrderItem>()
            .HasKey(oi => new { oi.OrderId, oi.ProductId });

        // Уникальный индекс для Email
        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Email)
            .IsUnique();
        // Это гарантирует, что EF не будет искать "Customers"
        modelBuilder.Entity<Customer>().ToTable("Customer");
        modelBuilder.Entity<Employee>().ToTable("Employee");
        modelBuilder.Entity<Order>().ToTable("Order");
        modelBuilder.Entity<Product>().ToTable("Product");
        modelBuilder.Entity<OrderItem>().ToTable("Order_Items");
        modelBuilder.Entity<PickupPoint>().ToTable("Pickup_Point");
        modelBuilder.Entity<User>().ToTable("AppUser");
   

        // Seed Data: Пользователи по умолчанию
        // ВНИМАНИЕ: Если ты уже сделал "пустую" миграцию, эти данные могут не добавиться 
        // автоматически через Update-Database. Но для кода они теперь видны.
        modelBuilder.Entity<User>().HasData(
            new User
            {
                UserId = 1,
                Username = "admin",
                Role = "Admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                CreatedAt = DateTime.SpecifyKind(new DateTime(2026, 1, 1), DateTimeKind.Utc)
            },
            new User
            {
                UserId = 2,
                Username = "worker1",
                Role = "Employee",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("worker123"),
                CreatedAt = DateTime.SpecifyKind(new DateTime(2026, 1, 1), DateTimeKind.Utc)
            }
        );
    }
}