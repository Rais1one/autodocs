namespace DemoApi.Models;

public record UserDto(int Id, string Name, string Email, DateTime CreatedAt);

public record CreateUserRequest(string Name, string Email);

public record UpdateUserRequest(string Name, string Email);