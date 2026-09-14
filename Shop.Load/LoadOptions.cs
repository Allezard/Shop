using System.Globalization;

namespace Shop.Load;

/// <summary>Параметры запуска.</summary>
internal sealed record LoadOptions(
    string Scenario,
    int DurationSeconds,
    int Concurrency,
    Uri BaseUrl,
    int DelayMs,
    int? Rps,
    bool Json)
{
    public bool CheckOnly => Scenario == "check";

    /// <summary>Возвращает null, если запрошена справка.</summary>
    public static LoadOptions? Parse(string[] args)
    {
        string? scenario = null;
        var duration = 60;
        var concurrency = 8;
        var delayMs = 0;
        int? rps = null;
        var baseUrl = "http://localhost:5080";
        var json = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string? inline = null;
            if (arg.StartsWith("--", StringComparison.Ordinal) && arg.IndexOf('=') is var eq and > 0)
            {
                inline = arg[(eq + 1)..];
                arg = arg[..eq];
            }

            string Value() => inline ?? (++i < args.Length ? args[i] : throw new OptionsException($"Для {arg} не указано значение."));

            switch (arg)
            {
                case "-h" or "--help" or "-?":
                    return null;
                case "--scenario":
                    scenario = Value();
                    break;
                case "--duration":
                    duration = ParseInt(arg, Value(), 5, 3600);
                    break;
                case "--concurrency":
                    concurrency = ParseInt(arg, Value(), 1, 128);
                    break;
                case "--delay-ms":
                    delayMs = ParseInt(arg, Value(), 0, 5000);
                    break;
                case "--rps":
                    rps = ParseInt(arg, Value(), 1, 10_000);
                    break;
                case "--base-url":
                    baseUrl = Value();
                    break;
                case "--json":
                    json = true;
                    break;
                default:
                    if (arg.StartsWith('-') || scenario is not null)
                        throw new OptionsException($"Неизвестный параметр: {args[i]}");
                    scenario = arg;
                    break;
            }
        }

        scenario = (scenario ?? "mixed").ToLowerInvariant();
        if (scenario != "check" && scenario != "mixed" && !Scenarios.All.Contains(scenario))
            throw new OptionsException(
                $"Неизвестный сценарий: {scenario}. Допустимые: {string.Join(", ", Scenarios.All)}, mixed, check.");

        if (rps is not null && delayMs > 0)
            throw new OptionsException("--rps и --delay-ms взаимоисключающие: задайте что-то одно.");

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new OptionsException($"--base-url должен быть абсолютным http(s)-адресом, получено: {baseUrl}");

        // Пути сценариев относительные ("api/..."), поэтому базовый адрес обязан кончаться на '/'.
        if (!uri.AbsoluteUri.EndsWith('/'))
            uri = new Uri(uri.AbsoluteUri + "/");

        return new LoadOptions(scenario, duration, concurrency, uri, delayMs, rps, json);
    }

    private static int ParseInt(string name, string raw, int min, int max) =>
        int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value >= min && value <= max
            ? value
            : throw new OptionsException($"{name} должен быть целым числом от {min} до {max}, получено: {raw}");
}

internal sealed class OptionsException(string message) : Exception(message);