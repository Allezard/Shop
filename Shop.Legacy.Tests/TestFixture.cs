using Microsoft.EntityFrameworkCore;
using Shop.Legacy.Data;

namespace Shop.Legacy.Tests;

/// <summary>
/// Быстрые тесты без внешних зависимостей: не нужен ни Docker, ни база.
/// </summary>
public static class TestFixture
{
    public static ShopContext CreateContext() =>
        new(new DbContextOptionsBuilder<ShopContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}