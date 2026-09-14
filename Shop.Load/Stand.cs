using System.Net.Http.Json;
using System.Text.Json;

namespace Shop.Load;

/// <summary>Проверки стенда перед нагрузкой.</summary>
internal sealed class Stand(HttpClient http)
{
    /// <summary>Число заказов из /api/health; null в OrderCount — поля нет в ответе.</summary>
    public sealed record Health(long? OrderCount);

    /// <summary>Возвращает null, если стенд не ответил за 3 секунды или ответил ошибкой.</summary>
    public async Task<Health?> ProbeAsync(CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        try
        {
            using var doc = await http.GetFromJsonAsync<JsonDocument>("api/health", timeout.Token);
            var orders = doc is not null
                && doc.RootElement.ValueKind == JsonValueKind.Object
                && TryGet(doc.RootElement, "orders", out var value)
                && value.TryGetInt64(out var count)
                    ? count
                    : (long?)null;
            return new Health(orders);
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException or JsonException)
        {
            if (ct.IsCancellationRequested) throw;
            return null;
        }
    }

    /// <summary>Идентификатор любого существующего заказа — нужен сценарию details.</summary>
    public async Task<string?> GetAnyOrderIdAsync(CancellationToken ct)
    {
        using var doc = await http.GetFromJsonAsync<JsonDocument>("api/orders/recent?take=1", ct);
        if (doc is null
            || doc.RootElement.ValueKind != JsonValueKind.Array
            || doc.RootElement.GetArrayLength() == 0
            || !TryGet(doc.RootElement[0], "id", out var id))
            return null;

        return id.ValueKind == JsonValueKind.String 
            ? id.GetString() 
            : id.GetRawText();
    }

    // Имена свойств сравниваются без учёта регистра: стенд может отдавать и camelCase, и PascalCase.
    private static bool TryGet(JsonElement obj, string name, out JsonElement value)
    {
        if (obj.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in obj.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }
}