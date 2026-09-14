using Microsoft.EntityFrameworkCore;
using Shop.Reference.Data;
using Shop.Reference.Domain;

namespace Shop.Reference.Queries;                                         // ← 03

public sealed record OrderRow(Guid Id, string Number, decimal Total, string CustomerName);
public sealed record LineRow(string Product, int Quantity, decimal UnitPrice);
public sealed record PaymentRow(decimal Amount, DateTime PaidAt);
public sealed record OrderDetails(
    string Number, 
    DateTime PlacedAt, 
    IReadOnlyList<LineRow> Lines, 
    IReadOnlyList<PaymentRow> Payments);
public sealed record OrderReportRow(string Number, decimal Total, OrderStatus Status);

public sealed class OrderQueries(ShopContext context)
{
    /// <summary>
    /// Один запрос, четыре колонки, ноль записей в трекере.
    ///
    /// Include здесь не нужен: обращение к o.Customer.Name внутри проекции
    /// само добавляет JOIN. Include понадобился бы, только если бы мы
    /// собирались сущности МЕНЯТЬ.
    /// </summary>
    public Task<List<OrderRow>> GetRecentAsync(int take = 50, CancellationToken ct = default) =>
        context.Orders
            .OrderByDescending(o => o.PlacedAt)
            .Take(take)
            .Select(o => new OrderRow(o.Id, o.Number, o.Total, o.Customer.Name))
            .ToListAsync(ct);

    /// <summary>
    /// Две коллекции — и ни декартова произведения, ни AsSplitQuery.
    ///
    /// EF переводит проекцию с вложенными коллекциями в отдельные запросы
    /// автоматически. Получается то же число строк, что дал бы AsSplitQuery,
    /// но колонок — только нужные, и трекер пуст.
    ///
    /// Платежи читаются подзапросом, а не навигацией: Payment — отдельный
    /// агрегат, навигации на него у Order нет.
    /// </summary>
    public Task<OrderDetails?> GetDetailsAsync(Guid id, CancellationToken ct = default) =>
        context.Orders
            .Where(o => o.Id == id)
            .Select(o => new OrderDetails(
                o.Number,
                o.PlacedAt,
                o.Lines.Select(l => new LineRow(l.Product.Name, l.Quantity, l.UnitPrice)).ToList(),
                context.Payments.Where(p => p.OrderId == o.Id)
                                .Select(p => new PaymentRow(p.Amount, p.PaidAt)).ToList()))
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// Отчёт: проекция снимает вопрос отслеживания целиком —
    /// AsNoTracking здесь не нужен, отслеживать нечего.
    /// </summary>
    public Task<List<OrderReportRow>> BuildReportAsync(
        DateTime from, DateTime to, CancellationToken ct = default) =>
        context.Orders
            .Where(o => o.PlacedAt >= from && o.PlacedAt < to)
            .Select(o => new OrderReportRow(o.Number, o.Total, o.Status))
            .ToListAsync(ct);
}