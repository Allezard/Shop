namespace Shop.Reference.Domain;                                          // ← 02

public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;

    // Переименовано из Phone в модуле 06 — четырьмя выкладками, без простоя.
    public string? PhoneNumber { get; set; }                    // ← 06

    public DateTime CreatedAt { get; set; }
    public Address ShippingAddress { get; set; }

    public List<Order> Orders { get; } = [];
}