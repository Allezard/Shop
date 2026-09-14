using Microsoft.EntityFrameworkCore;
using Shop.Reference.Data;
using Shop.Reference.Domain;
using System.Security.Cryptography;
using System.Text.Json;

namespace Shop.Reference.Services;                                        // ← 07

public sealed record CheckoutResult(bool Ok, Guid? OrderId, string? Error);
public sealed record OrderPlaced(Guid OrderId, decimal Total);

/// <summary>
/// Оформление заказа в три фазы.
///
/// Требование «не должно остаться заказа без платежа» выполняется НЕ
/// транзакцией, а промежуточным статусом плюс обработчиком застрявших:
/// внешний вызов держал бы транзакцию 812 мс при 12 мс полезной работы,
/// блокируя строку остатка и занимая соединение из пула.
///
/// Цена подхода настоящая: появляется состояние «исход неизвестен»,
/// и его надо уметь разбирать. См. RecoverStuckAsync.
/// </summary>
public sealed class CheckoutService(
    ShopContext context,
    ReservationService reservations,
    IPaymentGateway gateway)
{
    public async Task<CheckoutResult> PlaceAsync(
        long productId, int qty, Guid customerId, CancellationToken ct = default)
    {
        // ── ① Транзакция: резерв и заказ в промежуточном статусе ────────────
        var product = await context.Products
            .Where(p => p.Id == productId)
            .Select(p => new { p.Price })
            .FirstOrDefaultAsync(ct);

        if (product is null) return new CheckoutResult(false, null, "not_found");

        var order = new Order
        {
            Number = NewOrderNumber(),
            PlacedAt = DateTime.UtcNow,
            Total = product.Price * qty,
            Status = OrderStatus.AwaitingPayment,
            CustomerId = customerId
        };
        order.Lines.Add(new OrderLine
        {
            ProductId = productId,
            Quantity = qty,
            UnitPrice = product.Price
        });

        var strategy = context.Database.CreateExecutionStrategy();

        var reserved = await strategy.ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();      // при повторе начинаем с чистого листа

            await using var tx = await context.Database.BeginTransactionAsync(ct);

            var result = await reservations.TryReserveAsync(productId, qty, ct);
            if (!result.Ok) { await tx.RollbackAsync(ct); return false; }

            context.Orders.Add(order);
            await context.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
            return true;
        });

        if (!reserved) return new CheckoutResult(false, null, "out_of_stock");

        // ── ② Вне транзакции: внешний вызов ─────────────────────────────────
        // Ключ идемпотентности — идентификатор заказа. Он известен ДО вызова,
        // потому что генерируется приложением (UUID v7), а не базой.
        var charge = await gateway.ChargeAsync(order.Id, order.Total, ct);

        // ── ③ Транзакция: зафиксировать исход ───────────────────────────────
        await FinalizeAsync(order, charge, productId, qty, ct);

        return charge.Success
            ? new CheckoutResult(true, order.Id, null)
            : new CheckoutResult(false, order.Id, "payment_failed");
    }

    private async Task FinalizeAsync(
        Order order, ChargeResult charge, long productId, int qty, CancellationToken ct)
    {
        await using var tx = await context.Database.BeginTransactionAsync(ct);

        if (charge.Success)
        {
            context.Payments.Add(new CardPayment
            {
                OrderId = order.Id,
                Amount = order.Total,
                PaidAt = DateTime.UtcNow,
                Last4 = "4242"
            });

            order.Status = OrderStatus.Paid;

            // Событие пишется в ТОЙ ЖЕ транзакции, что и заказ, —
            // поэтому потеряться не может. Продублироваться может:
            // доставка «хотя бы один раз», потребитель обязан быть идемпотентным.
            context.Outbox.Add(new OutboxMessage
            {
                Type = nameof(OrderPlaced),
                Payload = JsonSerializer.Serialize(new OrderPlaced(order.Id, order.Total)),
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            order.Status = OrderStatus.PaymentFailed;
            await reservations.ReleaseAsync(productId, qty, ct);
        }

        await context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    /// <summary>
    /// Разбор состояния «исход неизвестен»: заказ создан, ответа шлюза нет.
    /// Именно этот обработчик и обеспечивает исходное требование —
    /// не транзакция.
    /// </summary>
    public async Task<int> RecoverStuckAsync(TimeSpan olderThan, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow - olderThan;

        var stuck = await context.Orders
            .Where(o => o.Status == OrderStatus.AwaitingPayment && o.PlacedAt < cutoff)
            .OrderBy(o => o.Id)
            .Take(100)
            .ToListAsync(ct);

        foreach (var order in stuck)
        {
            var actual = await gateway.GetStatusAsync(order.Id, ct);

            var line = await context.OrderLines
                .Where(l => l.OrderId == order.Id)
                .Select(l => new { l.ProductId, l.Quantity })
                .FirstAsync(ct);

            await FinalizeAsync(order, actual, line.ProductId, line.Quantity, ct);
        }

        return stuck.Count;
    }

    /// <summary>
    /// «ORD-» и 12 шестнадцатеричных знаков — ровно 16 символов, по длине
    /// колонки number. 48 случайных бит: при параллельном оформлении номера
    /// не совпадают, а совпадение отсёк бы уникальный индекс.
    /// В промышленной системе чаще берут последовательность в базе —
    /// здесь это лишняя инфраструктура ради учебного примера.
    /// </summary>
    private static string NewOrderNumber() =>
        $"ORD-{Convert.ToHexString(RandomNumberGenerator.GetBytes(6))}";
}