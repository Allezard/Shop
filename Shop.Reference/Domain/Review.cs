namespace Shop.Reference.Domain;                                          // ← 02

public class Review
{
    // UUID v7: упорядочен по времени создания, поэтому keyset-пагинация
    // в ReviewQueries обходится первичным ключом без отдельного индекса.
    public Guid Id { get; set; }

    public int Rating { get; set; }
    public string Text { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public long ProductId { get; set; }
    public Guid CustomerId { get; set; }
}