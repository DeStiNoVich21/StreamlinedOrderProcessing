using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace StreamlinedOrderProcessing.Models;

[Table("AppUser")]
public class User
{
    [Key, Column("user_id")]
    public int UserId { get; set; }

    [Column("username")]
    public string Username { get; set; } = null!;

    [Column("password_hash")]
    [JsonIgnore] // Чтобы хеш пароля никогда не улетал на фронтенд
    public string PasswordHash { get; set; } = null!;

    [Column("role")]
    public string Role { get; set; } = "Employee"; // Admin, Manager, Employee

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}