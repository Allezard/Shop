using Shop.Legacy.Domain;
using Shop.Legacy.Queries;

namespace Shop.Legacy.Tests;

public class OrderQueriesTests
{
    [Fact]
    public void GetRecent_returns_orders_with_customer_name()
    {
        using var ctx = TestFixture.CreateContext();

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Иванов",
            Email = "i@example.com",
            ShippingAddress = new Address("Москва", "Тверская, 1", "125009")
        };

        ctx.Customers.Add(customer);

        ctx.Orders.Add(new Order
        {
            Id = Guid.NewGuid(),
            Number = "ORD-1",
            Total = 100m,
            PlacedAt = DateTime.UtcNow,
            CustomerId = customer.Id,
            Customer = customer
        });

        ctx.SaveChanges();

        var result = new OrderQueries(ctx).GetRecent(take: 10);

        _ = Assert.Single(result);
        Assert.Equal("Иванов", result[0].CustomerName);
    }
}