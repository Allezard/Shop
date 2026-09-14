namespace Shop.Legacy.Services;

public sealed record ChargeResult(bool Success, string? Reference);

/// <summary>
/// Имитация внешнего платёжного шлюза: отвечает примерно за 800 мс.
/// Задержка настраивается переменной SHOP_GATEWAY_MS.
/// </summary>
public sealed class PaymentGateway
{
    private readonly int _delayMs =
        int.TryParse(Environment.GetEnvironmentVariable("SHOP_GATEWAY_MS"), out var ms) ? ms : 800;

    public async Task<ChargeResult> ChargeAsync(Guid idempotencyKey, decimal amount)
    {
        await Task.Delay(_delayMs);
        return new ChargeResult(Success: true, Reference: idempotencyKey.ToString("N")[..12]);
    }

    public Task<ChargeResult> GetStatusAsync(Guid idempotencyKey) =>
        Task.FromResult(new ChargeResult(true, idempotencyKey.ToString("N")[..12]));
}