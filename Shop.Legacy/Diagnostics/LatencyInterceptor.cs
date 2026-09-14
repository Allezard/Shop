using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace Shop.Legacy.Diagnostics;

/// <summary>
/// Добавляет задержку к каждой команде, имитируя сеть до удалённой базы.
/// Включается переменной SHOP_DB_LATENCY_MS. Разбирается в разделе 1.8:
/// локальный контейнер отвечает за доли миллисекунды, боевая база — за единицы.
/// </summary>
public sealed class LatencyInterceptor(int delayMs) : DbCommandInterceptor
{
    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData e, InterceptionResult<DbDataReader> result,
        CancellationToken ct = default)
    {
        await Task.Delay(delayMs, ct);
        return result;
    }

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData e, InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        await Task.Delay(delayMs, ct);
        return result;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData e, InterceptionResult<DbDataReader> result)
    {
        Thread.Sleep(delayMs);
        return result;
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command, CommandEventData e, InterceptionResult<int> result)
    {
        Thread.Sleep(delayMs);
        return result;
    }
}