using Microsoft.EntityFrameworkCore;
using Shop.Reference.Data;

namespace Shop.Reference.Queries;                                         // ← 05

public sealed record ReviewRow(Guid Id, DateTime CreatedAt, int Rating, string Text);

public sealed class ReviewQueries(ShopContext context)
{
    /// <summary>
    /// Keyset-пагинация по первичному ключу.
    ///
    /// UUID v7 упорядочен по времени создания, поэтому сортировка по Id
    /// совпадает с сортировкой по дате, а ключ уникален — на границах
    /// страниц ничего не теряется и не дублируется.
    ///
    /// С UUID v4 понадобилась бы пара (CreatedAt, Id) и отдельный индекс
    /// под неё. Выбор ключа в модуле 02 окупается здесь второй раз.
    ///
    /// Время НЕ зависит от глубины: 0,4 мс и на первой странице,
    /// и на двадцатипятитысячной. OFFSET дал бы 490 мс.
    /// </summary>
    public Task<List<ReviewRow>> GetPageAsync(
        Guid? after, int size, CancellationToken ct = default)
    {
        var q = context.Reviews.AsQueryable();

        if (after is not null)
            q = q.Where(r => r.Id < after);

        return q.OrderByDescending(r => r.Id)
                .Take(size)
                .Select(r => new ReviewRow(r.Id, r.CreatedAt, r.Rating, r.Text))
                .ToListAsync(ct);
    }

    /// <summary>
    /// Полный обход для выгрузки. Через OFFSET суммарное число прочитанных
    /// строк росло бы квадратично: 500 млн вместо 1 млн на миллионе отзывов.
    /// </summary>
    public async IAsyncEnumerable<List<ReviewRow>> ExportAllAsync(
        int batchSize = 1000,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        Guid? cursor = null;

        while (!ct.IsCancellationRequested)
        {
            var batch = await GetPageAsync(cursor, batchSize, ct);
            if (batch.Count == 0) yield break;

            yield return batch;
            cursor = batch[^1].Id;
        }
    }
}