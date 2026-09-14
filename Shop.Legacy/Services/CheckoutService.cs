using Microsoft.EntityFrameworkCore;
using Shop.Legacy.Data;
using Shop.Legacy.Domain;
using System.Security.Cryptography;

namespace Shop.Legacy.Services;

public sealed record CheckoutResult(bool Ok, Guid? OrderId, string? Error);

public sealed class CheckoutService(ShopContext context, PaymentGateway gateway)
{
    /// <summary>
    /// Оформление заказа: резерв остатка, создание заказа, оплата.
    /// </summary>
    public async Task<CheckoutResult> PlaceAsync(long productId, int qty, Guid customerId)
    {
        // Всё в одной транзакции, чтобы не осталось заказа без платежа
        // и не списался остаток при неудачной оплате.
        await using var tx = await context.Database.BeginTransactionAsync();

        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null)
            return new CheckoutResult(false, null, "not_found");

        var reserved = await context.Products
            .Where(p => p.Id == productId && p.Stock >= qty)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - qty));

        if (reserved == 0)
            return new CheckoutResult(false, null, "out_of_stock");

        var order = new Order
        {
            Number = NewOrderNumber(),
            PlacedAt = DateTime.UtcNow,
            Total = product.Price * qty,
            Status = OrderStatus.Pending,
            CustomerId = customerId
        };
        order.Lines.Add(new OrderLine { ProductId = productId, Quantity = qty, UnitPrice = product.Price });

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var charge = await gateway.ChargeAsync(order.Id, order.Total);

        if (!charge.Success)
        {
            await tx.RollbackAsync();
            return new CheckoutResult(false, null, "payment_failed");
        }

        context.Payments.Add(new CardPayment
        {
            OrderId = order.Id,
            Amount = order.Total,
            PaidAt = DateTime.UtcNow,
            Last4 = "4242"
        });
        order.Status = OrderStatus.Paid;
        await context.SaveChangesAsync();

        await tx.CommitAsync();
        return new CheckoutResult(true, order.Id, null);
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