using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Shop.Legacy.Diagnostics;

/// <summary>
/// Считает обращения к базе. Разбирается в модуле 01.
///
/// Три семейства методов считаются ОТДЕЛЬНО и это принципиально:
/// Reader* — чтения, NonQuery* — записи. Задание 1.6 построено на том,
/// что импорт даёт queries = 0 при пятистах командах записи.
/// </summary>
public sealed class CountingInterceptor : DbCommandInterceptor
{
    private int _readers;
    private int _nonQueries;
    private int _scalars;
    private long _totalMs;
    private DateTime _since = DateTime.UtcNow;
    private string? _lastSql;

    public int Readers => Volatile.Read(ref _readers);
    public int NonQueries => Volatile.Read(ref _nonQueries);
    public int Scalars => Volatile.Read(ref _scalars);
    public long TotalMs => Interlocked.Read(ref _totalMs);
    public TimeSpan Since => DateTime.UtcNow - _since;
    public string? LastSql => _lastSql;

    public void Reset()
    {
        Interlocked.Exchange(ref _readers, 0);
        Interlocked.Exchange(ref _nonQueries, 0);
        Interlocked.Exchange(ref _scalars, 0);
        Interlocked.Exchange(ref _totalMs, 0);
        _lastSql = null;
        _since = DateTime.UtcNow;
    }

    private void Record(DbCommand command, CommandExecutedEventData e, ref int counter)
    {
        Interlocked.Increment(ref counter);
        Interlocked.Add(ref _totalMs, (long)e.Duration.TotalMilliseconds);
        _lastSql = command.CommandText;
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command, CommandExecutedEventData e, DbDataReader result)
    {
        Record(command, e, ref _readers);
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData e, DbDataReader result,
        CancellationToken ct = default)
    {
        Record(command, e, ref _readers);
        return ValueTask.FromResult(result);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData e, int result)
    {
        Record(command, e, ref _nonQueries);
        return result;
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command, CommandExecutedEventData e, int result, CancellationToken ct = default)
    {
        Record(command, e, ref _nonQueries);
        return ValueTask.FromResult(result);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData e, object? result)
    {
        Record(command, e, ref _scalars);
        return result;
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command, CommandExecutedEventData e, object? result, CancellationToken ct = default)
    {
        Record(command, e, ref _scalars);
        return ValueTask.FromResult(result);
    }
}