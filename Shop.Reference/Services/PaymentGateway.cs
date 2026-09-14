namespace Shop.Reference.Services;                                        // ← 07

public sealed record ChargeResult(bool Success, string? Reference);

public interface IPaymentGateway
{
    Task<ChargeResult> ChargeAsync(Guid idempotencyKey, decimal amount, CancellationToken ct = default);
    Task<ChargeResult> GetStatusAsync(Guid idempotencyKey, CancellationToken ct = default);
}

public sealed class FakePaymentGateway : IPaymentGateway
{
    private readonly int _delayMs =
        int.TryParse(Environment.GetEnvironmentVariable("SHOP_GATEWAY_MS"), out var ms) ? ms : 800;

    public async Task<ChargeResult> ChargeAsync(Guid key, decimal amount, CancellationToken ct = default)
    {
        await Task.Delay(_delayMs, ct);
        return new ChargeResult(true, key.ToString("N")[..12]);
    }

    public Task<ChargeResult> GetStatusAsync(Guid key, CancellationToken ct = default) =>
        Task.FromResult(new ChargeResult(true, key.ToString("N")[..12]));
}