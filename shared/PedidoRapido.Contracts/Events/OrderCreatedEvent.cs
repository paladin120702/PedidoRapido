namespace PedidoRapido.Contracts.Events;

/// <summary>
/// ISP: Small, focused contract — carries only the data needed by event consumers.
/// Immutable record shared between OrderService (publisher) and NotificationService (consumer).
/// </summary>
public record OrderCreatedEvent
{
    /// <summary>Unique identifier of the created order.</summary>
    public Guid OrderId { get; init; }

    /// <summary>Name of the customer who placed the order.</summary>
    public string CustomerName { get; init; } = string.Empty;

    /// <summary>Name of the product ordered.</summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>Total price calculated as quantity × unit price.</summary>
    public decimal TotalPrice { get; init; }

    /// <summary>UTC timestamp when the order was created.</summary>
    public DateTime CreatedAt { get; init; }
}
