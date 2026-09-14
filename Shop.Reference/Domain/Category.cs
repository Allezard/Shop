namespace Shop.Reference.Domain;                                          // ← 02

public class Category
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;

    public long? ParentId { get; set; }
    public Category? Parent { get; set; }
}