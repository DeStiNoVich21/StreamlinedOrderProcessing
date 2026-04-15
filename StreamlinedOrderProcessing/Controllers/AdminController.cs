using Microsoft.AspNetCore.Mvc;
using StreamlinedOrderProcessing.Models;
using StreamlinedOrderProcessing.Repositories;
using BCrypt.Net; // Не забудь установить пакет: dotnet add package BCrypt.Net-Next
namespace StreamlinedOrderProcessing.Controllers;

[ApiController]
[Route("api/[controller]")]
// В будущем добавь [Authorize(Roles = "Admin")]
public class AdminController(IGenericRepository<User> userRepository) : ControllerBase
{
    // --- БАЗОВЫЕ CRUD МЕТОДЫ ---

    // 1. READ ALL: Получить всех пользователей
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetAll()
    {
        var users = await userRepository.GetAllAsync();
        return Ok(users);
    }

    // 2. READ ONE: Получить пользователя по ID
    [HttpGet("{id:int}")]
    public async Task<ActionResult<User>> GetById(int id)
    {
        var user = await userRepository.GetByIdAsync(id);
        if (user == null) return NotFound("Пользователь не найден.");
        return Ok(user);
    }

    // 3. CREATE (REGISTER): Регистрация нового админа или менеджера
    [HttpPost("register")]
    public async Task<IActionResult> Create([FromBody] UserRegisterDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Проверка уникальности логина
        var existing = await userRepository.FindAsync(u => u.Username == dto.Username);
        if (existing.Any()) return BadRequest("Логин уже занят.");

        var newUser = new User
        {
            Username = dto.Username,
            Role = dto.Role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password) // Хешируем!
        };

        await userRepository.AddAsync(newUser);
        // Тут должен быть вызов UnitOfWork.Save() или аналогичный

        return CreatedAtAction(nameof(GetById), new { id = newUser.UserId }, newUser);
    }

    // 4. UPDATE: Обновить роль или имя пользователя
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UserUpdateDto dto)
    {
        var user = await userRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        user.Username = dto.Username;
        user.Role = dto.Role;

        // Если прислали новый пароль — обновляем его хеш
        if (!string.IsNullOrEmpty(dto.NewPassword))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        }

        userRepository.Update(user);
        return NoContent();
    }

    // 5. DELETE: Удалить пользователя из системы
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await userRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        userRepository.Delete(user);
        return NoContent();
    }

    // --- СПЕЦИАЛЬНЫЕ МЕТОДЫ АДМИНА ---

    // Поиск пользователей по роли (например, только менеджеры)
    [HttpGet("role/{roleName}")]
    public async Task<IActionResult> GetByRole(string roleName)
    {
        var users = await userRepository.FindAsync(u => u.Role.ToLower() == roleName.ToLower());
        return Ok(users);
    }
}

#region DTOs (Data Transfer Objects)
// Используем для чистоты API и безопасности паролей

public record UserRegisterDto(
    string Username,
    string Password,
    string Role
);

public record UserUpdateDto(
    string Username,
    string Role,
    string? NewPassword // Пароль необязателен при обновлении профиля
);

#endregion