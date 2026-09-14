using Microsoft.EntityFrameworkCore;
using Shop.Legacy.Data;

namespace Shop.Legacy.Queries;

public sealed record ProductRow(long Id, string Name, decimal Price);
public sealed record CatalogRow(long Id, string Name, decimal Price, IReadOnlyList<string> Tags);

public sealed class ProductQueries(ShopContext context)
{
    /// <summary>
    /// Поиск по каталогу. Регистр не учитывается, искать можно по части названия.
    /// </summary>
    public IReadOnlyList<ProductRow> Search(string query)
    {
        return context.Products
            .Where(p => p.Name.ToLower().Contains(query.ToLower()))
            .Select(p => new ProductRow(p.Id, p.Name, p.Price))
            .Take(50)
            .ToList();
    }

    /// <summary>
    /// Страница каталога с тегами.
    /// </summary>
    public IReadOnlyList<CatalogRow> GetCatalogPage(int page, int size)
    {
        // Разделение запроса — помогло на карточке заказа, применено и здесь.
        var products = context.Products
            .Include(p => p.Tags)
            .OrderBy(p => p.Name)
            .Skip(page * size)
            .Take(size)
            .AsSplitQuery()
            .ToList();

        return products
            .Select(p => 
                new CatalogRow(p.Id, p.Name, p.Price, p.Tags.Select(t => t.Name).ToList()))
            .ToList();
    }
}