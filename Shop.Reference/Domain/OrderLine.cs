namespace Shop.Reference.Domain;                                          // ← 02

public class OrderLine
{
    public Guid Id { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;
}