using Microsoft.EntityFrameworkCore;
using Shop.Reference.Data;

namespace Shop.Reference.Services;                                        // ← 04

public sealed record ReserveResult(bool Ok, string? Error);

public sealed class ReservationService(ShopContext context)
{
    /// <summary>
    /// Резерв остатка одной атомарной командой.
    ///
    /// Маркер конкурентности здесь был бы НЕВЕРНЫМ ответом: конкурентные
    /// изменения остатка — норма, а не исключительная ситуация, и приложение
    /// всё время ловило бы DbUpdateConcurrencyException и повторяло.
    ///
    /// Проверка и изменение выполняются одной командой: условие Stock >= qty
    /// не даёт уйти в минус, а число изменённых строк отвечает «получилось ли».
    ///
    /// Оговорка: операция атомарна, но НЕ идемпотентна — повтор спишет дважды.
    /// Для повторяемости нужен отдельный документ резерва со своим ключом.
    /// </summary>
    public async Task<ReserveResult> TryReserveAsync(
        long productId, int qty, CancellationToken ct = default)
    {
        var affected = await context.Products
            .Where(p => p.Id == productId && p.Stock >= qty)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - qty), ct);

        return affected == 0
            ? new ReserveResult(false, "out_of_stock")
            : new ReserveResult(true, null);
    }

    public Task ReleaseAsync(long productId, int qty, CancellationToken ct = default) =>
        context.Products
            .Where(p => p.Id == productId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + qty), ct);
}