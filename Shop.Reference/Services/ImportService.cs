using Microsoft.EntityFrameworkCore;
using Shop.Reference.Data;
using Shop.Reference.Domain;

namespace Shop.Reference.Services;                                        // ← 04

public sealed record ProductImportDto(
    string Sku, 
    string Name, 
    string Description, 
    decimal Price, 
    int Stock, 
    long CategoryId);

public sealed record ImportResult(int Imported, int Failed);

public sealed class ImportService(ShopContext context)
{
    private const int BatchSize = 200;

    /// <summary>
    /// Импорт пакетами.
    ///
    /// Исходное требование — «одна плохая строка не должна откатить весь
    /// пакет» — сохранено, но платим за него тремя транзакциями вместо
    /// пятисот, а не пятьюстами.
    ///
    /// ChangeTracker.Clear() между пакетами обязателен: без него сущности
    /// накапливаются, и обход трекера в SaveChanges даёт квадратичный рост.
    /// </summary>
    public async Task<ImportResult> ImportAsync(
        IReadOnlyList<ProductImportDto> items, CancellationToken ct = default)
    {
        int imported = 0, failed = 0;

        foreach (var chunk in items.Chunk(BatchSize))
        {
            try
            {
                context.Products.AddRange(chunk.Select(Map));
                await context.SaveChangesAsync(ct);
                imported += chunk.Length;
            }
            catch (DbUpdateException)
            {
                context.ChangeTracker.Clear();
                var (ok, bad) = await RetryOneByOneAsync(chunk, ct);
                imported += ok; failed += bad;
            }

            context.ChangeTracker.Clear();
        }

        return new ImportResult(imported, failed);
    }

    private async Task<(int Ok, int Failed)> RetryOneByOneAsync(
        ProductImportDto[] chunk, CancellationToken ct)
    {
        int ok = 0, failed = 0;

        foreach (var dto in chunk)
        {
            try
            {
                context.Products.Add(Map(dto));
                await context.SaveChangesAsync(ct);
                ok++;
            }
            catch (DbUpdateException)
            {
                failed++;
            }
            finally
            {
                context.ChangeTracker.Clear();
            }
        }

        return (ok, failed);
    }

    private static Product Map(ProductImportDto d) => new()
    {
        Sku = d.Sku, Name = d.Name, Description = d.Description,
        Price = d.Price, Stock = d.Stock, CategoryId = d.CategoryId
    };
}