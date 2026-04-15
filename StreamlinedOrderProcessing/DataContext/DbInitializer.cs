using StreamlinedOrderProcessing.Models;
using StreamlinedOrderProcessing.DataContext;
using BCrypt.Net;

namespace StreamlinedOrderProcessing.DataContext;

public static class DbInitializer
{
    public static async Task SeedUsers(AppDbContext context)
    {
        // Проверяем, есть ли хоть один пользователь
        if (!context.Users.Any())
        {
            var admin = new User
            {
                Username = "admin",
                Role = "Admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                CreatedAt = DateTime.UtcNow
            };

            var worker = new User
            {
                Username = "worker1",
                Role = "Employee",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("worker123"),
                CreatedAt = DateTime.UtcNow
            };

            await context.Users.AddRangeAsync(admin, worker);
            await context.SaveChangesAsync();

            Console.WriteLine("--> База данных успешно инициализирована пользователями.");
        }
    }
}