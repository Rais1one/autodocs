using DemoApi.Models;

namespace DemoApi.Data;

/// <summary>
/// Данные в памяти для демонстрации. Сбрасываются при перезапуске.
/// </summary>
public static class FakeDatabase
{
    public static readonly List<UserDto> Users =
    [
        new(1, "Иван Петров", "ivan@example.com", new DateTime(2026, 1, 15)),
        new(2, "Мария Смирнова", "maria@example.com", new DateTime(2026, 3, 2)),
        new(3, "Алексей Козлов", "alexey@example.com", new DateTime(2026, 5, 20))
    ];

    public static readonly List<OrderDto> Orders =
    [
        new(1, 1, 1500.00m, "Новый"),
        new(2, 1, 320.50m, "Оплачен"),
        new(3, 2, 9999.99m, "Доставлен")
    ];
}