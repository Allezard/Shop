namespace Shop.Legacy.Domain;

// Навигации объявлены virtual — этого требуют прокси, подключённые в Program.cs.

public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }

    public Address ShippingAddress { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = [];
}

/// <summary>Адрес доставки. Хранится колонками в таблице покупателя.</summary>
public readonly record struct Address(string City, string Street, string PostalCode);

public class Category
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;

    public long? ParentId { get; set; }
    public virtual Category? Parent { get; set; }

    public virtual ICollection<Product> Products { get; set; } = [];
}

public class Tag
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = [];
}

public class Product
{
    public long Id { get; set; }
    public string Sku { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Price { get; set; }
    public int Stock { get; set; }

    /// <summary>Снят с продажи. Строка сохраняется ради истории заказов.</summary>
    public bool IsDeleted { get; set; }

    public long CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<Tag> Tags { get; set; } = [];
}

public enum OrderStatus
{
    Draft = 0,
    Pending = 1,
    AwaitingPayment = 2,
    Paid = 3,
    PaymentFailed = 4,
    Completed = 5,
    Cancelled = 6
}

public class Order
{
    public Guid Id { get; set; }
    public string Number { get; set; } = null!;
    public DateTime PlacedAt { get; set; }
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }

    /// <summary>Свободный комментарий оператора. Заполняется редко, бывает длинным.</summary>
    public string? Notes { get; set; }

    public Guid CustomerId { get; set; }
    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<OrderLine> Lines { get; set; } = [];
    public virtual ICollection<Payment> Payments { get; set; } = [];
}

public class OrderLine
{
    public Guid Id { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public Guid OrderId { get; set; }
    public virtual Order Order { get; set; } = null!;

    public long ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;
}

// ─── Способы оплаты ─────────────────────────────────────────────────────────

public abstract class Payment
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }

    public Guid OrderId { get; set; }
    public virtual Order Order { get; set; } = null!;
}

public class CardPayment : Payment
{
    public string Last4 { get; set; } = null!;
}

public class BankTransferPayment : Payment
{
    public string Bic { get; set; } = null!;
}

public class CryptoPayment : Payment
{
    public string Wallet { get; set; } = null!;
}

public class Review
{
    public Guid Id { get; set; }
    public int Rating { get; set; }
    public string Text { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public long ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;

    public Guid CustomerId { get; set; }
}