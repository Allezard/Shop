namespace Shop.Reference.Domain;                                          // ← 02

public enum OrderStatus
{
    Draft = 0,
    AwaitingPayment = 1,                                        // ← 07
    Paid = 2,
    PaymentFailed = 3,                                          // ← 07
    Completed = 4,
    Cancelled = 5
}

/// <summary>
/// Корень агрегата. В агрегат входят строки заказа: сумма проверяется
/// только вместе с ними.
///
/// Платежи в агрегат НЕ входят — платёж меняется без изменения заказа
/// (возврат, доплата, ответ шлюза), поэтому навигации на них здесь нет.
/// Связь с покупателем оставлена навигацией для чтения; правило
/// «один SaveChanges — один агрегат» соблюдается дисциплиной.
/// </summary>
public class Order
{
    public Guid Id { get; set; }
    public string Number { get; set; } = null!;
    public DateTime PlacedAt { get; set; }
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }
    public string? Notes { get; set; }

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public List<OrderLine> Lines { get; } = [];

    /// <summary>
    /// Единственное место, где строка добавляется в заказ. Total всегда
    /// согласован с Lines — это ровно тот инвариант, для проверки которого
    /// не нужна база данных. См. Unit/OrderTests.cs в Shop.Reference.Tests.
    /// </summary>
    public void AddLine(long productId, int quantity, decimal unitPrice)
    {
        Lines.Add(new OrderLine
        {
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice
        });

        Total = Lines.Sum(l => l.Quantity * l.UnitPrice);
    }
}