using DemoApi.Data;
using DemoApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace DemoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    /// <summary>
    ///  список пользователей с постраничным выводом.
    /// </summary>
    /// <param name="page">Номер страницы, начиная с 1.</param>
    /// <param name="pageSize">Количество записей на странице.</param>
    /// <returns>Список пользователей.</returns>
    [HttpGet]
    public ActionResult<IEnumerable<UserDto>> GetUsers(int page = 1, int pageSize = 20)
    {
        var result = FakeDatabase.Users
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        return Ok(result);
    }

    /// <summary>
    /// Получить пользователя по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор пользователя.</param>
    /// <returns>Найденный пользователь.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await Task.FromResult(FakeDatabase.Users.FirstOrDefault(u => u.Id == id));

        if (user is null)
        {
            return NotFound();
        }

        return user;
    }

    /// <summary>
    /// Создать нового пользователя.
    /// </summary>
    /// <param name="request">Данные нового пользователя.</param>
    /// <returns>Созданный пользователь.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<UserDto> CreateUser([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("Имя и почта обязательны.");
        }

        var newId = FakeDatabase.Users.Count == 0 ? 2 : FakeDatabase.Users.Max(u => u.Id) + 1;
        var user = new UserDto(newId, request.Name, request.Email, DateTime.UtcNow);
        FakeDatabase.Users.Add(user);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    /// <summary>
    /// Обновить данные пользователя. [f[f[f
    /// </summary>
    /// <param name="id">Идентификатор пользователя.</param>
    /// <param name="request">Новые данные пользователя.</param>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult UpdateUser(int id, UpdateUserRequest request)
    {
        var index = FakeDatabase.Users.FindIndex(u => u.Id == id);

        if (index < 0)
        {
            return NotFound();
        }

        var old = FakeDatabase.Users[index];
        FakeDatabase.Users[index] = old with { Name = request.Name, Email = request.Email };

        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteUser(int id)
    {
        var removed = FakeDatabase.Users.RemoveAll(u => u.Id == id);

        return removed == 0 ? NotFound() : NoContent();
    }


}