namespace OrderService.Application.Interfaces;

/// <summary>
/// ISP: Single-method interface — one responsibility: publish a message.
/// OCP: New message brokers (Kafka, Azure Service Bus) can be added without touching use cases.
/// DIP: Use cases depend on this abstraction, never on MassTransit directly.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>Publishes <typeparamref name="T"/> to the configured message broker.</summary>
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class;
}
