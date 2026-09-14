using Microsoft.EntityFrameworkCore;
using Shop.Reference.Services;

namespace Shop.Reference.Tests.Integration;

/// <summary>
/// Сценарий из модуля 04, задание 4.7: маркер конкурентности для остатка
/// был бы неверным ответом, потому что конкурентные изменения тут —
/// норма. Правильный ответ — атомарная команда, и вот её проверка.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class ConcurrencyTests(DatabaseFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetAsync();
    public async ValueTask DisposeAsync() => await Task.CompletedTask;

    [Fact]
    public async Task Reservation_never_goes_negative()
    {
        long productId;

        await using (var seed = fixture.CreateContext())
        {
            var product = await ConstraintTests.SeedProductAsync(seed, stock: 10);
            productId = product.Id;
        }

        // Каждая попытка — свой контекст: DbContext не потокобезопасен,
        // см. модуль 00, раздел 0.4.
        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(async _ =>
        {
            await using var ctx = fixture.CreateContext();
            return await new ReservationService(ctx).TryReserveAsync(productId, qty: 1);
        }));

        Assert.Equal(10, results.Count(r => r.Ok));
        Assert.Equal(10, results.Count(r => !r.Ok && r.Error == "out_of_stock"));

        await using var check = fixture.CreateContext();
        var stock = await check.Products
            .Where(p => p.Id == productId)
            .Select(p => p.Stock)
            .FirstAsync();

        Assert.Equal(0, stock);
    }
}