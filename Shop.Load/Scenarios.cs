namespace Shop.Load;

/// <summary>
/// Сценарии нагрузки. Единственное место, где описаны эндпоинты:
/// его используют и ученик локально, и воркер автопроверки.
/// Описания намеренно нейтральны — справка не должна подсказывать, где искать.
/// </summary>
internal static class Scenarios
{
    public static readonly string[] ReadOnly = ["recent", "details", "summaries", "catalog", "search", "reviews"];

    public static readonly string[] All = [.. ReadOnly, "report", "import", "checkout"];

    public static readonly (string Name, string Description)[] Descriptions =
    [
        ("recent",    "список последних заказов"),
        ("details",   "карточка заказа"),
        ("summaries", "сводка за период"),
        ("catalog",   "страница каталога"),
        ("search",    "поиск по каталогу"),
        ("reviews",   "лента отзывов"),
        ("report",    "отчёт за период"),
        ("import",    "импорт товаров (запись)"),
        ("checkout",  "оформление заказа (запись)"),
        ("mixed",     "случайно из читающих на каждом запросе"),
    ];

    private static readonly string SearchQuery = Uri.EscapeDataString("ноутбук");

    public static bool NeedsOrderId(string scenario) => scenario is "details" or "mixed";

    /// <summary>Для mixed выбирает случайный читающий сценарий, иначе возвращает заданный.</summary>
    public static string Pick(string scenario) => scenario == "mixed" 
        ? ReadOnly[Random.Shared.Next(ReadOnly.Length)] 
        : scenario;

    public static HttpRequestMessage Build(string name, string? orderId) => name switch
    {
        "recent" => Get("api/orders/recent?take=50"),
        "details" => Get($"api/orders/{orderId}/details"),
        "summaries" => Get("api/orders/summaries?days=7"),
        "catalog" => Get($"api/catalog?page={Random.Shared.Next(50)}&size=20"),
        "search" => Get($"api/products/search?q={SearchQuery}"),
        "reviews" => Get($"api/reviews?page={Random.Shared.Next(2000)}&size=20"),
        "report" => Get("api/reports/orders?days=30"),
        "import" => Post("api/import/products?count=50"),
        "checkout" => Post("api/checkout?productId=1&qty=1"),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Неизвестный сценарий"),
    };

    private static HttpRequestMessage Get(string path) => new(HttpMethod.Get, path);

    private static HttpRequestMessage Post(string path) => new(HttpMethod.Post, path);
}