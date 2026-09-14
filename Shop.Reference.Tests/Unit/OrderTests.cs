using Shop.Reference.Domain;

namespace Shop.Reference.Tests.Unit;

/// <summary>
/// Без базы и без EF — см. модуль 08, раздел 8.4. Если бы этой проверке
/// нужна была база, она стояла бы не в том месте.
/// </summary>
public class OrderTests
{
    [Fact]
    public void Order_total_equals_sum_of_lines()
    {
        var order = new Order { Number = "ORD-1", CustomerId = Guid.NewGuid() };

        order.AddLine(productId: 1, quantity: 2, unitPrice: 100m);
        order.AddLine(productId: 2, quantity: 1, unitPrice: 50m);

        Assert.Equal(250m, order.Total);
        Assert.Equal(2, order.Lines.Count);
    }

    [Fact]
    public void New_order_has_zero_total()
    {
        var order = new Order { Number = "ORD-2", CustomerId = Guid.NewGuid() };

        Assert.Equal(0m, order.Total);
        Assert.Empty(order.Lines);
    }
}