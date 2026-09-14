namespace Shop.Reference.Domain;                                          // ← 02

public class Tag
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;

    public List<Product> Products { get; } = [];
}