using Shop.Legacy.Data;
using Shop.Legacy.Domain;

namespace Shop.Legacy.Services;

public sealed record ProductImportDto(
    string Sku, 
    string Name, 
    string Description, 
    decimal Price, 
    int Stock, 
    long CategoryId);

public sealed class ImportService(ShopContext context)
{
    /// <summary>Загрузка каталога от партнёра. Выполняется по расписанию ночью.</summary>
    public int Import(IEnumerable<ProductImportDto> items)
    {
        var imported = 0;

        // Сохраняем по одной позиции: одна плохая строка не должна
        // откатить весь пакет — партнёр присылает данные неравномерного качества.
        foreach (var dto in items)
        {
            context.Products.Add(new Product
            {
                Sku = dto.Sku,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Stock = dto.Stock,
                CategoryId = dto.CategoryId
            });

            context.SaveChanges();
            imported++;
        }

        return imported;
    }
}