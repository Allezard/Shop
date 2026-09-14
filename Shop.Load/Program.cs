using Shop.Load;
using System.Text;

// кириллица в старых консолях Windows
Console.OutputEncoding = Encoding.UTF8;

LoadOptions? options;
try
{
    options = LoadOptions.Parse(args);
}
catch (OptionsException e)
{
    Console.Error.WriteLine(e.Message);
    Console.Error.WriteLine("Справка: dotnet run -- --help");
    return 2;
}

if (options is null)
{
    Help.Print(Console.Out);
    return 0;
}

// В режиме --json в stdout идёт только JSON, всё остальное — в stderr.
var info = options.Json 
    ? Console.Error 
    : Console.Out;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true; // не убивать процесс: дать воркерам остановиться и вывести сводку
    cts.Cancel();
};

using var http = new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(2) })
{
    BaseAddress = options.BaseUrl,
    Timeout = TimeSpan.FromSeconds(60),
};

var stand = new Stand(http);
var health = await stand.ProbeAsync(cts.Token);
if (health is null)
{
    Console.Error.WriteLine($"Shop.Legacy недоступен на {options.BaseUrl}. Запустите стенд и повторите.");
    return 1;
}

if (health.OrderCount == 0)
    Console.Error.WriteLine("ВНИМАНИЕ: база пуста. Заполните её перед нагрузкой.");

if (options.CheckOnly)
{
    info.WriteLine($"Стенд отвечает на {options.BaseUrl}" +
                   (health.OrderCount is long n ? $", заказов в базе: {n}" : ""));
    return 0;
}

string? orderId = null;
if (Scenarios.NeedsOrderId(options.Scenario))
{
    orderId = await stand.GetAnyOrderIdAsync(cts.Token);
    if (orderId is null)
    {
        Console.Error.WriteLine("В базе нет ни одного заказа — сценарию details нечего запрашивать. Заполните базу и повторите.");
        return 1;
    }
}

var until = DateTime.Now.AddSeconds(options.DurationSeconds);
var pace = options.Rps is int rps ? $" | темп: {rps} запр/с" : "";
info.WriteLine($"Сценарий: {options.Scenario} | потоков: {options.Concurrency}{pace} | до {until:HH:mm:ss}");

var result = await new LoadRunner(http, options, orderId).RunAsync(cts.Token);

if (options.Json)
    Console.Out.WriteLine(result.ToJson());
else
    result.WriteTable(Console.Out);

if (result.TargetRps is int target && !result.Interrupted && result.Rps < target * 0.95)
    Console.Error.WriteLine($"ВНИМАНИЕ: заданный темп {target} запр/с не выдержан ({result.Rps:0.#}). " +
                            "Стенд не успевает при текущем --concurrency — увеличьте его.");

return result.Interrupted ? 130 : 0;