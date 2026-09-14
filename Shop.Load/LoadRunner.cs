using System.Diagnostics;

namespace Shop.Load;

/// <summary>
/// Параллельные воркеры, каждый шлёт запросы последовательно.
/// Дедлайн проверяется перед запросом: начатый запрос дорабатывает до конца.
/// </summary>
internal sealed class LoadRunner(HttpClient http, LoadOptions options, string? orderId)
{
    public async Task<LoadResult> RunAsync(CancellationToken ct)
    {
        var started = Stopwatch.GetTimestamp();
        var deadline = started + (long)(options.DurationSeconds * (double)Stopwatch.Frequency);
        var pacer = options.Rps is int rps 
            ? new Pacer(rps, started) 
            : null;

        var workers = Enumerable.Range(0, options.Concurrency)
            .Select(_ => Task.Run(() => WorkerAsync(deadline, pacer, ct), CancellationToken.None));

        var stats = await Task.WhenAll(workers);
        var elapsed = Stopwatch.GetElapsedTime(started);

        return LoadResult.From(options.Scenario, stats, elapsed, options.Rps, interrupted: ct.IsCancellationRequested);
    }

    private async Task<WorkerStats> WorkerAsync(long deadline, Pacer? pacer, CancellationToken ct)
    {
        var stats = new WorkerStats();

        try
        {
            while (!ct.IsCancellationRequested && Stopwatch.GetTimestamp() < deadline)
            {
                if (pacer is not null && !await pacer.WaitTurnAsync(deadline, ct))
                    break;

                var name = Scenarios.Pick(options.Scenario);
                using var request = Scenarios.Build(name, orderId);

                var sent = Stopwatch.GetTimestamp();
                try
                {
                    // ResponseContentRead: тело ответа читается целиком,
                    // иначе время не включало бы передачу ответа.
                    using var response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);
                    if (response.IsSuccessStatusCode) 
                        stats.Ok++;
                    else 
                        stats.Fail++;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break; // Ctrl+C: незавершённый запрос в статистику не попадает
                }
                catch (Exception e) when (e is HttpRequestException or OperationCanceledException)
                {
                    stats.Fail++; // сетевая ошибка или таймаут 60 с
                }

                // Время ошибочных запросов тоже учитывается.
                stats.LatenciesMs.Add(Stopwatch.GetElapsedTime(sent).TotalMilliseconds);

                if (options.DelayMs > 0)
                    await Task.Delay(options.DelayMs, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // прервано во время паузы — отдаём то, что успели
        }

        return stats;
    }
}

internal sealed class WorkerStats
{
    public int Ok;
    public int Fail;
    public readonly List<double> LatenciesMs = [];
}

/// <summary>
/// Равномерный темп для --rps: запрос n назначен на момент start + n/rps.
/// Расписание абсолютное, поэтому неточность таймеров (на Windows ~15 мс)
/// не накапливается — средний темп держится.
/// </summary>
internal sealed class Pacer(int rps, long start)
{
    private long _issued = -1;

    public async ValueTask<bool> WaitTurnAsync(long deadline, CancellationToken ct)
    {
        var n = Interlocked.Increment(ref _issued);
        var due = start + (long)(n * (double)Stopwatch.Frequency / rps);
        if (due >= deadline)
            return false;

        var wait = Stopwatch.GetElapsedTime(Stopwatch.GetTimestamp(), due);
        if (wait > TimeSpan.Zero)
            await Task.Delay(wait, ct);

        return true;
    }
}