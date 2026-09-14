namespace Shop.Reference.Domain;                                          // ← 02

public class Product
{
    // bigint, а не Guid: товары заводятся только нами, склеивать
    // каталоги из разных систем не требуется. Ключ уже 8 байт вместо 16.
    public long Id { get; set; }

    public string Sku { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsDeleted { get; set; }

    public long CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public List<Tag> Tags { get; } = [];
}