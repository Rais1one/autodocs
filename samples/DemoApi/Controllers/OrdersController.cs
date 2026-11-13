using DemoApi.Data;
using DemoApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace DemoApi.Controllers;

[Route("api/[controller]")]
public class OrdersController : BaseApiController
{
    /// <summary>
    /// Получить заказы клиента.
    /// </summary>
    /// <param name="clientId">Идентификатор клиентского приложения из заголовка.</param>
    /// <param name="userId">Фильтр по пользователю; если не задан, возвращаются все заказы.</param>
    /// <param name="cancellationToken">Токен отмены запроса.</param>
    /// <returns>Список заказов.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders(
        [FromHeader(Name = "X-Client-Id")] string clientId,
        int? userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var orders = userId is null
            ? FakeDatabase.Orders
            : FakeDatabase.Orders.Where(o => o.UserId == userId).ToList();

        return Ok(await Task.FromResult(orders));
    }

    /// <summary>
    /// Получить заказ по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор заказа.</param>
    /// <param name="cancellationToken">Токен отмены запроса.</param>
    /// <returns>Найденный заказ.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrder(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var order = await Task.FromResult(FakeDatabase.Orders.FirstOrDefault(o => o.Id == id));

        if (order is null)
        {
            return NotFound();
        }

        return order;
    }

    /// <summary>
    /// Создать заказ.
    /// </summary>
    /// <param name="request">Данные заказа.</param>
    /// <returns>Созданный заказ.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<OrderDto> CreateOrder(CreateOrderRequest request)
    {
        var newId = FakeDatabase.Orders.Count == 0 ? 1 : FakeDatabase.Orders.Max(o => o.Id) + 1;
        var order = new OrderDto(newId, request.UserId, request.Total, "Новый");
        FakeDatabase.Orders.Add(order);

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }
}