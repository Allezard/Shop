using Microsoft.EntityFrameworkCore;
using Shop.Reference.Domain;
using Shop.Reference.Services;

namespace Shop.Reference.Tests.Integration;

/// <summary>
/// Путь отказа исполняется реже успешного и потому чаще всего сломан —
/// дословная формулировка из модуля 08, раздел 8.4. Этот файл — как раз
/// тот тест, который там назван «важен особо».
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class CheckoutTests(DatabaseFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetAsync();
    public async ValueTask DisposeAsync() => await Task.CompletedTask;

    [Fact]
    public async Task Successful_payment_reserves_stock_and_writes_outbox_message()
    {
        long productId; 
        Guid customerId;

        await using (var seed = fixture.CreateContext())
        {
            productId = (await ConstraintTests.SeedProductAsync(seed, stock: 5)).Id;
            customerId = (await ConstraintTests.SeedCustomerAsync(seed)).Id;
        }

        await using var ctx = fixture.CreateContext();
        var checkout = new CheckoutService(ctx, new ReservationService(ctx), new SucceedingGateway());

        var result = await checkout.PlaceAsync(productId, qty: 2, customerId);

        Assert.True(result.Ok);

        await using var check = fixture.CreateContext();
        var stock = await check.Products.Where(p => p.Id == productId).Select(p => p.Stock).FirstAsync();
        Assert.Equal(3, stock);

        var order = await check.Orders.FirstAsync(o => o.Id == result.OrderId);
        Assert.Equal(OrderStatus.Paid, order.Status);

        // Событие пишется в той же транзакции, что и заказ — модуль 07.
        var outboxCount = await check.Outbox.CountAsync(m => m.Type == nameof(OrderPlaced));
        Assert.Equal(1, outboxCount);
    }

    [Fact]
    public async Task Gateway_failure_releases_stock_and_marks_order()
    {
        long productId; 
        Guid customerId;

        await using (var seed = fixture.CreateContext())
        {
            productId = (await ConstraintTests.SeedProductAsync(seed, stock: 5)).Id;
            customerId = (await ConstraintTests.SeedCustomerAsync(seed)).Id;
        }

        await using var ctx = fixture.CreateContext();
        var checkout = new CheckoutService(ctx, new ReservationService(ctx), new FailingGateway());

        var result = await checkout.PlaceAsync(productId, qty: 2, customerId);

        Assert.False(result.Ok);
        Assert.Equal("payment_failed", result.Error);

        // Остаток должен вернуться — иначе отказ шлюза тихо теряет товар со склада.
        await using var check = fixture.CreateContext();
        var stock = await check.Products.Where(p => p.Id == productId).Select(p => p.Stock).FirstAsync();
        Assert.Equal(5, stock);

        var order = await check.Orders.FirstAsync(o => o.Id == result.OrderId);
        Assert.Equal(OrderStatus.PaymentFailed, order.Status);

        // Без успешной оплаты событие "заказ оформлен" уходить не должно.
        var outboxCount = await check.Outbox.CountAsync(m => m.Type == nameof(OrderPlaced));
        Assert.Equal(0, outboxCount);
    }

    [Fact]
    public async Task Recover_stuck_finalizes_awaiting_payment_order()
    {
        // Имитация состояния «исход неизвестен» из модуля 07: заказ создан
        // в статусе AwaitingPayment, обработчик приходит позже.

        long productId; 
        Guid customerId; 
        Guid orderId;

        await using (var seed = fixture.CreateContext())
        {
            var product = await ConstraintTests.SeedProductAsync(seed, stock: 5);
            var customer = await ConstraintTests.SeedCustomerAsync(seed);
            productId = product.Id; 
            customerId = customer.Id;

            await new ReservationService(seed).TryReserveAsync(productId, qty: 1);

            var order = new Order
            {
                Number = "ORD-STUCK", 
                PlacedAt = DateTime.UtcNow.AddMinutes(-10),
                Status = OrderStatus.AwaitingPayment, 
                CustomerId = customerId
            };

            order.AddLine(productId, quantity: 1, unitPrice: product.Price);
            seed.Orders.Add(order);
            await seed.SaveChangesAsync();
            orderId = order.Id;
        }

        await using var ctx = fixture.CreateContext();
        var recovered = await new CheckoutService(
            ctx, new ReservationService(ctx), new SucceedingGateway())
            .RecoverStuckAsync(olderThan: TimeSpan.FromMinutes(5));

        Assert.Equal(1, recovered);

        await using var check = fixture.CreateContext();
        var order2 = await check.Orders.FirstAsync(o => o.Id == orderId);
        Assert.Equal(OrderStatus.Paid, order2.Status);
    }

    private sealed class SucceedingGateway : IPaymentGateway
    {
        public Task<ChargeResult> ChargeAsync(Guid key, decimal amount, CancellationToken ct = default) =>
            Task.FromResult(new ChargeResult(true, key.ToString("N")[..12]));

        public Task<ChargeResult> GetStatusAsync(Guid key, CancellationToken ct = default) =>
            Task.FromResult(new ChargeResult(true, key.ToString("N")[..12]));
    }

    private sealed class FailingGateway : IPaymentGateway
    {
        public Task<ChargeResult> ChargeAsync(Guid key, decimal amount, CancellationToken ct = default) =>
            Task.FromResult(new ChargeResult(false, null));

        public Task<ChargeResult> GetStatusAsync(Guid key, CancellationToken ct = default) =>
            Task.FromResult(new ChargeResult(false, null));
    }
}