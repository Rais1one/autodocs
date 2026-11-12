namespace DemoApi.Models;

public record OrderDto(int Id, int UserId, decimal Total, string Status);

public record CreateOrderRequest(int UserId, decimal Total);