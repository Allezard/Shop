using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Shop.Legacy.Diagnostics;

/// <summary>
/// Считает вызовы SaveChanges — они не попадают в счётчик команд.
/// </summary>
public sealed class SaveCountingInterceptor : SaveChangesInterceptor
{
    private int _count;

    public int Count => Volatile.Read(ref _count);
    public void Reset() => Interlocked.Exchange(ref _count, 0);

    public override int SavedChanges(SaveChangesCompletedEventData e, int result)
    {
        Interlocked.Increment(ref _count);
        return result;
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData e, int result, CancellationToken ct = default)
    {
        Interlocked.Increment(ref _count);
        return ValueTask.FromResult(result);
    }
}