namespace Shop.Reference.Services;

/// <summary>
/// Заглушка шины сообщений: пишет публикацию в лог.
///
/// Эталон демонстрирует исходящие сообщения, а не интеграцию с конкретным
/// брокером. Реальную реализацию — RabbitMQ, Kafka, Azure Service Bus —
/// подставляют вместо этой, не трогая OutboxPublisher: он зависит только
/// от интерфейса IMessageBus.
///
/// Тот же приём, что FakePaymentGateway для платёжного шлюза.
/// </summary>
public sealed class LoggingMessageBus(ILogger<LoggingMessageBus> logger) : IMessageBus
{
    public Task PublishAsync(
        string type, 
        string payload, 
        Guid idempotencyKey, 
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "Опубликовано {Type} (ключ идемпотентности {Key}): {Payload}",
            type, idempotencyKey, payload);

        return Task.CompletedTask;
    }
}