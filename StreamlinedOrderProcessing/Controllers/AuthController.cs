using Microsoft.AspNetCore.Mvc;
using StreamlinedOrderProcessing.Models;
using StreamlinedOrderProcessing.Repositories;
using StreamlinedOrderProcessing.Services;

namespace StreamlinedOrderProcessing.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IGenericRepository<User> userRepository,
    JwtService jwtService) : ControllerBase
{
    // POST: api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Найти пользователя по логину
        var users = await userRepository.FindAsync(u => u.Username == dto.Username);
        var user = users.FirstOrDefault();

        if (user == null)
            return Unauthorized(new { message = "Неверный логин или пароль" });

        // Проверить пароль
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

        if (!isPasswordValid)
            return Unauthorized(new { message = "Неверный логин или пароль" });

        // Генерировать JWT токен
        var token = jwtService.GenerateToken(user);

        return Ok(new
        {
            token,
            user = new
            {
                user.UserId,
                user.Username,
                user.Role
            }
        });
    }

    // GET: api/auth/me - Получить информацию о текущем пользователе
    [HttpGet("me")]
    [AuthorizeRoles(UserRole.Admin, UserRole.Manager, UserRole.Employee)]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        return Ok(new
        {
            userId,
            username,
            role
        });
    }
}

public record LoginDto(
    string Username,
    string Password
);