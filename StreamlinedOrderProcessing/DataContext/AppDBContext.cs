using Microsoft.EntityFrameworkCore;

namespace StreamlinedOrderProcessing.DataContext
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        //public DbSet<Customer> Customers { get; set; }
        //public DbSet<Product> Products { get; set; }
        //public DbSet<Order> Orders { get; set; }
        //public DbSet<OrderItem> OrderItems { get; set; }
        //public DbSet<Employee> Employees { get; set; }
        //public DbSet<PickupPoint> PickupPoints { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuring Composite Key for Order_Items as per documentation
            //modelBuilder.Entity<OrderItem>()
            //    .HasKey(oi => new { oi.OrderId, oi.ProductId });

            // Naming conventions for PostgreSQL (snake_case)
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                entity.SetTableName(entity.GetTableName()?.ToLower());
            }
        }
    }
}
