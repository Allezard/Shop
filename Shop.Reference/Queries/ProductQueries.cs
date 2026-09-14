using Microsoft.EntityFrameworkCore;
using Shop.Reference.Data;

namespace Shop.Reference.Queries;                                         // ← 03

public sealed record ProductRow(long Id, string Name, decimal Price);
public sealed record CatalogRow(long Id, string Name, decimal Price, IReadOnlyList<string> Tags);

public sealed class ProductQueries(ShopContext context)
{
    /// <summary>
    /// Поиск по подстроке, регистр не учитывается.
    ///
    /// ILike вместо ToLower().Contains(): колонка остаётся нетронутой,
    /// и триграммный индекс применим. ToLower() сделал бы условие
    /// несаргабельным и дал бы Seq Scan по всему каталогу.
    ///
    /// Поиск по префиксу был бы в семь раз быстрее — и перестал бы
    /// находить «Игровой ноутбук ASUS» по запросу «ноутбук».
    /// </summary>
    public Task<List<ProductRow>> SearchAsync(string query, CancellationToken ct = default) =>
        context.Products
            .Where(p => EF.Functions.ILike(p.Name, $"%{query}%"))
            .OrderBy(p => p.Name)
            .Take(50)
            .Select(p => new ProductRow(p.Id, p.Name, p.Price))
            .ToListAsync(ct);

    /// <summary>
    /// Коллекция одна — AsSplitQuery только добавил бы round-trip
    /// на самом горячем эндпоинте и риск рассогласования при пагинации.
    /// </summary>
    public Task<List<CatalogRow>> GetCatalogPageAsync(
        int page, int size, CancellationToken ct = default) =>
        context.Products
            .OrderBy(p => p.Name).ThenBy(p => p.Id)
            .Skip(page * size)
            .Take(size)
            .Select(p => new CatalogRow(
                p.Id, p.Name, p.Price, p.Tags.Select(t => t.Name).ToList()))
            .ToListAsync(ct);
}