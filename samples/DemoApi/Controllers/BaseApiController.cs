using Microsoft.AspNetCore.Mvc;

namespace DemoApi.Controllers;

/// <summary>
/// Общий базовый класс контроллеров. Нужен, чтобы проверить,
/// что AutoDocs находит контроллеры через цепочку наследования.
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
}