using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Shop.Reference.Tests;

/// <summary>
/// Минимальный счётчик запросов для тестов формы — раздел 8.4 модуля 08.
/// Полный вариант с разбивкой на чтения/записи живёт в
/// <c>Shop.Legacy/Diagnostics/Interceptors.cs</c>; здесь читающих операций
/// достаточно, и разбивка не нужна.
/// </summary>
public sealed class QueryCounter : DbCommandInterceptor
{
    public int Count { get; private set; }

    public override DbDataReader ReaderExecuted(
        DbCommand command, CommandExecutedEventData e, DbDataReader result)
    {
        Count++;
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData e, DbDataReader result,
        CancellationToken ct = default)
    {
        Count++;
        return ValueTask.FromResult(result);
    }
}