namespace Shop.Reference.Domain;                                          // ← 07

/// <summary>
/// Исходящее сообщение. Записывается в ОДНОЙ транзакции с бизнес-данными,
/// поэтому не может потеряться. Продублироваться — может: доставка
/// «хотя бы один раз», потребитель обязан быть идемпотентным.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; }          // v7: и порядок, и ключ идемпотентности
    public string Type { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public int Attempts { get; set; }
}