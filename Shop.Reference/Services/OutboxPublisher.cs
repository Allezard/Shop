using Microsoft.EntityFrameworkCore;
using Shop.Reference.Data;

namespace Shop.Reference.Services;                                        // ← 07

public interface IMessageBus
{
    Task PublishAsync(string type, string payload, Guid idempotencyKey, CancellationToken ct = default);
}

public sealed class OutboxPublisher(ShopContext context, IMessageBus bus)
{
    public async Task<int> PublishPendingAsync(int batchSize = 100, CancellationToken ct = default)
    {
        // OrderBy(m => m.Id): UUID v7 упорядочен по времени,
        // поэтому порядок публикации совпадает с порядком создания.
        var batch = await context.Outbox
            .Where(m => m.SentAt == null)
            .OrderBy(m => m.Id)
            .Take(batchSize)
            .ToListAsync(ct);

        foreach (var m in batch)
        {
            try
            {
                // Между публикацией и отметкой возможен сбой — тогда сообщение
                // уйдёт повторно. Id служит потребителю ключом для распознавания.
                await bus.PublishAsync(m.Type, m.Payload, m.Id, ct);
                m.SentAt = DateTime.UtcNow;
            }
            catch
            {
                m.Attempts++;
            }
        }

        await context.SaveChangesAsync(ct);
        return batch.Count(m => m.SentAt is not null);
    }

    /// <summary>Основная метрика здоровья: возраст самого старого неотправленного.</summary>
    public async Task<TimeSpan?> GetLagAsync(CancellationToken ct = default)
    {
        var oldest = await context.Outbox
            .Where(m => m.SentAt == null)
            .MinAsync(m => (DateTime?)m.CreatedAt, ct);

        return oldest is null ? null : DateTime.UtcNow - oldest.Value;
    }

    /// <summary>
    /// Чистка архива. Без неё таблица растёт вечно, и однажды придётся
    /// удалять на большой таблице в спешке.
    /// </summary>
    public Task<int> PurgeSentAsync(TimeSpan keepFor, CancellationToken ct = default) =>
        context.Outbox
            .Where(m => m.SentAt != null && m.SentAt < DateTime.UtcNow - keepFor)
            .ExecuteDeleteAsync(ct);
}