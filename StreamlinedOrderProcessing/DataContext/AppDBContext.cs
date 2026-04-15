using Microsoft.EntityFrameworkCore;
using StreamlinedOrderProcessing.Models;
namespace StreamlinedOrderProcessing.DataContext
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<PickupPoint> PickupPoints { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuring Composite Key for Order_Items as per documentation
            modelBuilder.Entity<OrderItem>()
                .HasKey(oi => new { oi.OrderId, oi.ProductId });

            // Если ты хочешь, чтобы EF сам понимал индексы из DDL:
            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.Email)
                .IsUnique();

            // Naming conventions for PostgreSQL (snake_case)
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                entity.SetTableName(entity.GetTableName()?.ToLower());
            }
            // В AppDbContext.cs внутри метода OnModelCreating
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    Username = "admin",
                    Role = "Admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    UserId = 2,
                    Username = "worker1",
                    Role = "Employee",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("worker123"),
                    CreatedAt = DateTime.UtcNow
                }
            );

        }
    }
}
