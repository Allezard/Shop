using Shop.Reference.Domain;
using Shop.Reference.Queries;
using Shop.Reference.Tests.Integration;

namespace Shop.Reference.Tests.QueryShape;

/// <summary>
/// Только для критичных путей — модуль 08, раздел 8.4. Каждый такой тест
/// фиксирует форму запроса и упадёт при любой её порче: вернувшейся
/// ленивой загрузке, забытой проекции, лишнем Include.
///
/// Проверяется ЧИСЛО ЗАПРОСОВ, а не время — оно одинаково в любой среде.
/// Время мигало бы в CI и тест бы со временем отключили. См. модуль 01,
/// раздел 1.8, и модуль 08, раздел 8.4.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class OrderQueryShapeTests(DatabaseFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetAsync();
    public async ValueTask DisposeAsync() => await Task.CompletedTask;

    [Fact]
    public async Task Recent_orders_execute_single_query()
    {
        await SeedOrdersAsync(count: 20);

        var counter = new QueryCounter();
        await using var ctx = fixture.CreateContext(counter);

        var result = await new OrderQueries(ctx).GetRecentAsync(take: 20);

        Assert.Equal(20, result.Count);
        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public async Task Order_details_avoid_cartesian_explosion()
    {
        var orderId = await SeedOrderWithLinesAndPaymentsAsync(lines: 5, payments: 2);

        var counter = new QueryCounter();
        await using var ctx = fixture.CreateContext(counter);

        var details = await new OrderQueries(ctx).GetDetailsAsync(orderId);

        Assert.NotNull(details);
        Assert.Equal(5, details!.Lines.Count);
        Assert.Equal(2, details.Payments.Count);

        // Два запроса (заказ со строками одной проекцией + платежи
        // подзапросом), а не JOIN, дающий 5 × 2 = 10 строк.
        Assert.True(counter.Count <= 2, $"Ожидалось не больше 2 запросов, получено {counter.Count}");
    }

    [Fact]
    public async Task Catalog_page_executes_single_query_without_split()
    {
        await SeedProductsAsync(count: 5, tagsPerProduct: 2);

        var counter = new QueryCounter();
        await using var ctx = fixture.CreateContext(counter);

        await new ProductQueries(ctx).GetCatalogPageAsync(page: 0, size: 5);

        // Один запрос — не два, как дал бы AsSplitQuery на единственной
        // коллекции. См. модуль 03, задание 3.6.
        Assert.Equal(1, counter.Count);
    }

    private async Task SeedOrdersAsync(int count)
    {
        await using var ctx = fixture.CreateContext();

        for (var i = 0; i < count; i++)
        {
            var customer = await ConstraintTests.SeedCustomerAsync(ctx);
            ctx.Orders.Add(new Order
            {
                Number = $"ORD-{i:D4}",
                PlacedAt = DateTime.UtcNow.AddMinutes(-i),
                CustomerId = customer.Id
            });
        }

        await ctx.SaveChangesAsync();
    }

    private async Task<Guid> SeedOrderWithLinesAndPaymentsAsync(int lines, int payments)
    {
        await using var ctx = fixture.CreateContext();
        var customer = await ConstraintTests.SeedCustomerAsync(ctx);
        var product = await ConstraintTests.SeedProductAsync(ctx);

        var order = new Order
        {
            Number = "ORD-DETAILS", PlacedAt = DateTime.UtcNow, CustomerId = customer.Id
        };
        for (var i = 0; i < lines; i++)
            order.AddLine(product.Id, quantity: 1, unitPrice: 10m);

        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        for (var i = 0; i < payments; i++)
            ctx.Payments.Add(new CardPayment
            {
                OrderId = order.Id, Amount = 5m, PaidAt = DateTime.UtcNow, Last4 = "4242"
            });

        await ctx.SaveChangesAsync();
        return order.Id;
    }

    private async Task SeedProductsAsync(int count, int tagsPerProduct)
    {
        await using var ctx = fixture.CreateContext();
        var category = new Category { Name = "Тестовая категория" };
        ctx.Categories.Add(category);

        var tags = Enumerable.Range(0, tagsPerProduct)
            .Select(i => new Tag { Name = $"тег-{Guid.NewGuid():N}"[..10] })
            .ToList();
        ctx.Tags.AddRange(tags);

        for (var i = 0; i < count; i++)
        {
            var product = new Product
            {
                Sku = Guid.NewGuid().ToString("N")[..12],
                Name = $"Товар {i}", Description = "", Price = 10m, Category = category
            };
            product.Tags.AddRange(tags);
            ctx.Products.Add(product);
        }

        await ctx.SaveChangesAsync();
    }
}