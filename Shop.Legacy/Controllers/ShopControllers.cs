using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Legacy.Data;
using Shop.Legacy.Diagnostics;
using Shop.Legacy.Queries;
using Shop.Legacy.Services;

namespace Shop.Legacy.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController(ShopContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var connected = await context.Database.CanConnectAsync();
        var orders = connected ? await context.Orders.CountAsync() : 0;

        return Ok(new
        {
            status = connected ? "ok" : "down",
            database = connected ? "connected" : "unavailable",
            orders
        });
    }
}

/// <summary>
/// Служебная ручка самопроверки.
///
/// Назначение — СВЕРЯТЬ гипотезу, построенную инструментами, а не заменять их.
/// В реальном приложении такой ручки не будет: пользуйтесь ей после того,
/// как сделали вывод по логу и плану, а не вместо этого.
/// </summary>
[ApiController]
[Route("api/diag")]
public sealed class DiagController(
    CountingInterceptor commands,
    SaveCountingInterceptor saves,
    ShopContext context) : ControllerBase
{
    [HttpGet("queries")]
    public IActionResult Queries() => Ok(new
    {
        queries = commands.Readers,
        writes = commands.NonQueries,
        scalars = commands.Scalars,
        saveChanges = saves.Count,
        totalMs = commands.TotalMs,
        trackedEntities = context.ChangeTracker.Entries().Count(),
        sinceReset = commands.Since.ToString(@"hh\:mm\:ss")
    });

    [HttpGet("last-sql")]
    public IActionResult LastSql() => Ok(new { sql = commands.LastSql });

    [HttpPost("reset")]
    public IActionResult Reset()
    {
        commands.Reset();
        saves.Reset();
        context.ChangeTracker.Clear();
        return Ok(new { reset = true });
    }
}

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(OrderQueries queries) : ControllerBase
{
    [HttpGet("recent")]
    public IActionResult Recent([FromQuery] int take = 50) => Ok(queries.GetRecent(take));

    [HttpGet("{id:guid}/details")]
    public IActionResult Details(Guid id)
        => queries.GetDetails(id) is { } d ? Ok(d) : NotFound();

    [HttpGet("summaries")]
    public IActionResult Summaries([FromQuery] int days = 30)
    {
        var to = DateTime.UtcNow;
        return Ok(queries.GetSummaries(to.AddDays(-days), to));
    }
}

[ApiController]
[Route("api/catalog")]
public sealed class CatalogController(ProductQueries queries) : ControllerBase
{
    [HttpGet]
    public IActionResult Page([FromQuery] int page = 0, [FromQuery] int size = 20)
        => Ok(queries.GetCatalogPage(page, size));
}

[ApiController]
[Route("api/products")]
public sealed class ProductsController(ProductQueries queries) : ControllerBase
{
    [HttpGet("search")]
    public IActionResult Search([FromQuery] string q) => Ok(queries.Search(q));
}

[ApiController]
[Route("api/reviews")]
public sealed class ReviewsController(ReviewQueries queries) : ControllerBase
{
    [HttpGet]
    public IActionResult Page([FromQuery] int page = 0, [FromQuery] int size = 20)
        => Ok(queries.GetPage(page, size));
}

[ApiController]
[Route("api/reports")]
public sealed class ReportsController(ReportQueries queries) : ControllerBase
{
    [HttpGet("orders")]
    public IActionResult Orders([FromQuery] int days = 30)
    {
        var to = DateTime.UtcNow;
        return Ok(queries.Build(to.AddDays(-days), to));
    }
}

[ApiController]
[Route("api/import")]
public sealed class ImportController(ImportService service) : ControllerBase
{
    [HttpPost("products")]
    public IActionResult Products([FromQuery] int count = 500)
    {
        var stamp = DateTime.UtcNow.Ticks;

        var items = Enumerable.Range(0, count).Select(i => new ProductImportDto(
            Sku: $"IMP-{stamp}-{i}"[..Math.Min(20, $"IMP-{stamp}-{i}".Length)],
            Name: $"Импорт позиция {i}",
            Description: new string('x', 200),
            Price: 100m + i % 900,
            Stock: 50,
            CategoryId: 1));

        return Ok(new { imported = service.Import(items) });
    }
}

[ApiController]
[Route("api/checkout")]
public sealed class CheckoutController(CheckoutService service, ShopContext context) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Place(
        [FromQuery] long productId, [FromQuery] int qty = 1, [FromQuery] Guid? customerId = null)
    {
        var buyer = customerId ?? await context.Customers.Select(c => c.Id).FirstAsync();
        var result = await service.PlaceAsync(productId, qty, buyer);

        return result.Ok 
            ? Ok(result) 
            : BadRequest(result);
    }
}