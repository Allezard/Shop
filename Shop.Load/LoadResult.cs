using System.Globalization;
using System.Text.Json;

namespace Shop.Load;

internal sealed record LoadResult(
    string Scenario,
    int Ok,
    int Fail,
    double AvgMs,
    double P50Ms,
    double P95Ms,
    double P99Ms,
    double Rps,
    int? TargetRps,
    double ElapsedSeconds,
    bool Interrupted)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static LoadResult From(string scenario, IReadOnlyList<WorkerStats> stats, TimeSpan elapsed, int? targetRps, bool interrupted)
    {
        var latencies = stats.SelectMany(s => s.LatenciesMs).Order().ToArray();
        var ok = stats.Sum(s => s.Ok);
        var fail = stats.Sum(s => s.Fail);
        var total = ok + fail;
        var seconds = elapsed.TotalSeconds;

        return new LoadResult(
            scenario, 
            ok, 
            fail,
            AvgMs: total > 0 
                ? Math.Round(latencies.Average()) 
                : 0,
            P50Ms: Percentile(latencies, 50),
            P95Ms: Percentile(latencies, 95),
            P99Ms: Percentile(latencies, 99),
            // Делим на фактическое время, а не на заданную длительность:
            // хвост из дорабатывающих запросов иначе завышал бы темп.
            Rps: seconds > 0 
                ? Math.Round(total / seconds, 1) 
                : 0,
            targetRps,
            ElapsedSeconds: Math.Round(seconds, 1),
            interrupted);
    }

    /// <summary>Перцентиль методом ближайшего ранга по отсортированному массиву.</summary>
    private static double Percentile(double[] sorted, int p)
    {
        if (sorted.Length == 0) return 0;
        var rank = (int)Math.Ceiling(p / 100.0 * sorted.Length);
        return Math.Round(sorted[Math.Clamp(rank - 1, 0, sorted.Length - 1)]);
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public void WriteTable(TextWriter output)
    {
        var rows = new List<(string, string)>
        {
            ("Сценарий", Scenario),
            ("Успешных", Ok.ToString(CultureInfo.InvariantCulture)),
            ("Ошибок", Fail.ToString(CultureInfo.InvariantCulture)),
            ("Средняя, мс", Format(AvgMs)),
            ("p50 / p95 / p99, мс", $"{Format(P50Ms)} / {Format(P95Ms)} / {Format(P99Ms)}"),
            ("Запросов/с", Format(Rps)),
        };

        if (TargetRps is int target)
            rows.Add(("Цель, запросов/с", target.ToString(CultureInfo.InvariantCulture)));

        if (Interrupted)
            rows.Add(("Прервано", $"да, через {Format(ElapsedSeconds)} с"));

        var width = rows.Max(r => r.Item1.Length);

        output.WriteLine();

        foreach (var (label, value) in rows)
            output.WriteLine($"{label.PadRight(width)} : {value}");

        output.WriteLine();
    }

    private static string Format(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);
}