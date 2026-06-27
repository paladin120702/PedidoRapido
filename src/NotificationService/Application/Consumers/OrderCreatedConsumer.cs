using System.Globalization;
using MassTransit;
using Microsoft.Extensions.Logging;
using PedidoRapido.Contracts.Events;

namespace NotificationService.Application.Consumers;

/// <summary>
/// SRP: Handles notification logic for OrderCreatedEvent only — no persistence, no routing.
/// DIP: Depends on MassTransit's IConsumer abstraction, not on a concrete broker.
/// </summary>
public sealed class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedConsumer> _logger;

    /// <summary>Initializes the consumer with a logger.</summary>
    public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
        => _logger = logger;

    /// <inheritdoc/>
    public Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var order = context.Message;
        var totalFormatted = order.TotalPrice.ToString("C", new CultureInfo("pt-BR"));

        _logger.LogInformation(
            "NOTIFICATION: New order received! OrderId: {OrderId} | Customer: {Customer} | Product: {Product} | Total: {Total} | At: {CreatedAt}",
            order.OrderId,
            order.CustomerName,
            order.ProductName,
            totalFormatted,
            order.CreatedAt);

        return Task.CompletedTask;
    }
}
