using Microsoft.EntityFrameworkCore;
using Shop.Legacy.Data;

namespace Shop.Legacy.Queries;

public sealed record OrderDto(Guid Id, string Number, decimal Total, string CustomerName);
public sealed record OrderSummary(Guid Id, decimal Total);
public sealed record LineDto(string Product, int Quantity, decimal UnitPrice);
public sealed record PaymentDto(decimal Amount, DateTime PaidAt);
public sealed record OrderDetailsDto(string Number, DateTime PlacedAt, IReadOnlyList<LineDto> Lines, IReadOnlyList<PaymentDto> Payments);

public sealed class OrderQueries(ShopContext context)
{
    /// <summary>
    /// Последние заказы для главного экрана.
    /// </summary>
    public IReadOnlyList<OrderDto> GetRecent(int take = 50)
    {
        var orders = context.Orders
            .OrderByDescending(o => o.PlacedAt)
            .Take(take)
            .ToList();

        // Сборка ответа по загруженному списку.
        return orders
            .Select(o => new OrderDto(o.Id, o.Number, o.Total, o.Customer.Name))
            .ToList();
    }

    /// <summary>
    /// Карточка заказа: позиции и платежи.
    /// </summary>
    public OrderDetailsDto? GetDetails(Guid id)
    {
        var order = context.Orders
            .Include(o => o.Lines).ThenInclude(l => l.Product)
            .Include(o => o.Payments)
            .FirstOrDefault(o => o.Id == id);

        if (order is null) 
            return null;

        return new OrderDetailsDto(
            order.Number,
            order.PlacedAt,
            order.Lines.Select(l => new LineDto(l.Product.Name, l.Quantity, l.UnitPrice)).ToList(),
            order.Payments.Select(p => new PaymentDto(p.Amount, p.PaidAt)).ToList());
    }

    /// <summary>
    /// Сводка по заказам за период: идентификатор и сумма.
    /// </summary>
    public IReadOnlyList<OrderSummary> GetSummaries(DateTime from, DateTime to)
    {
        return context.Orders
            .Where(o => o.PlacedAt >= from && o.PlacedAt < to)
            .ToList()
            .Select(o => new OrderSummary(o.Id, o.Total))
            .ToList();
    }
}