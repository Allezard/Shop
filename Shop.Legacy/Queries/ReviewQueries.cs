using Shop.Legacy.Data;

namespace Shop.Legacy.Queries;

public sealed record ReviewRow(Guid Id, DateTime CreatedAt, int Rating, string Text);

public sealed class ReviewQueries(ShopContext context)
{
    /// <summary>
    /// Лента отзывов, свежие сверху. Постраничный вывод с номерами страниц.
    /// </summary>
    public IReadOnlyList<ReviewRow> GetPage(int page, int size)
    {
        return context.Reviews
            .OrderByDescending(r => r.CreatedAt)
            .Skip(page * size)
            .Take(size)
            .Select(r => new ReviewRow(r.Id, r.CreatedAt, r.Rating, r.Text))
            .ToList();
    }
}