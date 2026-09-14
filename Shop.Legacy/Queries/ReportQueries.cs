using Shop.Legacy.Data;
using Shop.Legacy.Domain;

namespace Shop.Legacy.Queries;

public sealed record OrderReportRow(string Number, decimal Total, OrderStatus Status);

public sealed class ReportQueries(ShopContext context)
{
    /// <summary>
    /// Отчёт по заказам за период. Только чтение, ничего не меняет.
    /// </summary>
    public IReadOnlyList<OrderReportRow> Build(DateTime from, DateTime to)
    {
        var orders = context.Orders
            .Where(o => o.PlacedAt >= from && o.PlacedAt < to)
            .ToList();

        return orders
            .Select(o => new OrderReportRow(o.Number, o.Total, o.Status))
            .ToList();
    }
}