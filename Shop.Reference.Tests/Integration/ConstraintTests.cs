using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shop.Reference.Data;
using Shop.Reference.Domain;

namespace Shop.Reference.Tests.Integration;

/// <summary>
/// То, что проверяет база — модуль 08, раздел 8.4. Ни одна из этих проверок
/// не прошла бы на провайдере в памяти: ограничений схемы — уникальности,
/// внешних ключей, CHECK — там нет.
///
/// Каждый тест проверяет не просто «упало», а КАКОЕ ограничение сработало —
/// по коду ошибки PostgreSQL. Иначе тест на CHECK прошёл бы и от нарушения
/// NOT NULL, и от чего угодно ещё: зелёный по неверной причине.
///
/// Удаление проверяется из отдельного контекста, в котором зависимые строки
/// не загружены. Если они загружены, ограничение применяет сам EF — раньше
/// базы и с другим исключением; это показано отдельным тестом.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class ConstraintTests(DatabaseFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetAsync();
    public async ValueTask DisposeAsync() => await Task.CompletedTask;

    [Fact]
    public async Task Order_number_is_unique()
    {
        await using var ctx = fixture.CreateContext();
        var customer = await SeedCustomerAsync(ctx);

        ctx.Orders.Add(new Order
        {
            Number = "ORD-DUP",
            PlacedAt = DateTime.UtcNow,
            CustomerId = customer.Id
        });
        await ctx.SaveChangesAsync();

        ctx.Orders.Add(new Order
        {
            Number = "ORD-DUP",
            PlacedAt = DateTime.UtcNow,
            CustomerId = customer.Id
        });

        await AssertViolatesAsync(ctx, PostgresErrorCodes.UniqueViolation);
    }

    [Fact]
    public async Task Deleting_customer_with_orders_is_rejected_by_database()
    {
        // Restrict из модуля 02: заказ без покупателя — исторический факт,
        // стирать его нельзя. Здесь его защищает внешний ключ в PostgreSQL.
        var customerId = await SeedCustomerWithOrderAsync("ORD-RESTRICT");

        // Новый контекст: заказы не загружены, EF о них не знает и честно
        // отправляет DELETE. Решение принимает база.
        await using var ctx = fixture.CreateContext();
        var customer = await ctx.Customers.FirstAsync(c => c.Id == customerId);
        ctx.Customers.Remove(customer);

        // PostgreSQL 18 сообщает об отказе RESTRICT собственным кодом 23001
        // (restrict_violation). До 17-й включительно было 23503
        // (foreign_key_violation) — общий код любого нарушения внешнего ключа.
        // 23503 по-прежнему приходит для NO ACTION и при вставке ссылки
        // на несуществующую запись.
        await AssertViolatesAsync(ctx, PostgresErrorCodes.RestrictViolation);
    }

    [Fact]
    public async Task Deleting_customer_with_tracked_orders_is_rejected_by_ef()
    {
        // Второй уровень той же защиты. Заказ отслеживается этим контекстом,
        // и EF сам видит, что обязательная связь рвётся: исключение летит
        // уже из Remove, до всякого обращения к базе, — и это не
        // DbUpdateException, а InvalidOperationException.
        await using var ctx = fixture.CreateContext();
        var customer = await SeedCustomerAsync(ctx);

        ctx.Orders.Add(new Order
        {
            Number = "ORD-TRACKED",
            PlacedAt = DateTime.UtcNow,
            CustomerId = customer.Id
        });
        await ctx.SaveChangesAsync();

        Assert.Throws<InvalidOperationException>(() => ctx.Customers.Remove(customer));
    }

    [Fact]
    public async Task Deleting_order_cascades_to_lines_in_database()
    {
        // Cascade внутри агрегата: строки без заказа не существуют.
        //
        // Строки не загружены в контекст удаления, поэтому удалить их может
        // только ON DELETE CASCADE в схеме. Если бы они отслеживались, EF
        // удалил бы их сам, и тест проверял бы EF, а не базу.
        Guid orderId;
        await using (var seed = fixture.CreateContext())
        {
            var customer = await SeedCustomerAsync(seed);
            var product = await SeedProductAsync(seed);

            var order = new Order
            {
                Number = "ORD-CASCADE",
                PlacedAt = DateTime.UtcNow,
                CustomerId = customer.Id
            };
            order.AddLine(product.Id, quantity: 1, unitPrice: 10m);
            seed.Orders.Add(order);
            await seed.SaveChangesAsync();
            orderId = order.Id;
        }

        await using (var ctx = fixture.CreateContext())
        {
            var order = await ctx.Orders.FirstAsync(o => o.Id == orderId);
            ctx.Orders.Remove(order);
            await ctx.SaveChangesAsync();
        }

        await using var check = fixture.CreateContext();
        Assert.Equal(0, await check.OrderLines.CountAsync(l => l.OrderId == orderId));
    }

    [Fact]
    public async Task Card_payment_without_last4_violates_check_constraint()
    {
        // TPH из модуля 02 теряет NOT NULL на полях потомков — гарантию
        // возвращают CHECK-ограничения в PaymentConfiguration.
        await using var ctx = fixture.CreateContext();
        var customer = await SeedCustomerAsync(ctx);

        var order = new Order
        {
            Number = "ORD-CHECK",
            PlacedAt = DateTime.UtcNow,
            CustomerId = customer.Id
        };
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        ctx.Payments.Add(new CardPayment
        {
            OrderId = order.Id,
            Amount = 10m,
            PaidAt = DateTime.UtcNow,
            Last4 = null!
        });

        await AssertViolatesAsync(ctx, PostgresErrorCodes.CheckViolation);
    }

    /// <summary>
    /// Сохранение должно упасть именно на указанном ограничении.
    /// Коды — стандартные SQLSTATE: 23505, 23001, 23514.
    /// </summary>
    private static async Task AssertViolatesAsync(ShopContext ctx, string sqlState)
    {
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
        var pg = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(sqlState, pg.SqlState);
    }

    private async Task<Guid> SeedCustomerWithOrderAsync(string number)
    {
        await using var seed = fixture.CreateContext();
        var customer = await SeedCustomerAsync(seed);

        seed.Orders.Add(new Order
        {
            Number = number,
            PlacedAt = DateTime.UtcNow,
            CustomerId = customer.Id
        });
        await seed.SaveChangesAsync();
        return customer.Id;
    }

    internal static async Task<Customer> SeedCustomerAsync(ShopContext ctx)
    {
        var customer = new Customer
        {
            Name = "Тестовый покупатель",
            Email = $"{Guid.NewGuid():N}@example.com",
            // Address — структура: без явного значения это default с тремя
            // null-строками, а колонки адреса обязательные.
            ShippingAddress = new Address("Москва", "Тверская, 1", "125009")
        };
        ctx.Customers.Add(customer);
        await ctx.SaveChangesAsync();
        return customer;
    }

    internal static async Task<Product> SeedProductAsync(ShopContext ctx, int stock = 100)
    {
        var category = new Category { Name = "Тестовая категория" };
        ctx.Categories.Add(category);

        var product = new Product
        {
            Sku = Guid.NewGuid().ToString("N")[..12],
            Name = "Тестовый товар",
            Description = "",
            Price = 10m,
            Stock = stock,
            Category = category
        };
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();
        return product;
    }
}