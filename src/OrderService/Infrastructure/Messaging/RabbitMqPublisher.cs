using MassTransit;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;

namespace OrderService.Infrastructure.Messaging;

/// <summary>
/// SRP: Adapts MassTransit's IPublishEndpoint to the application's IMessagePublisher contract.
/// OCP: The application layer never knows this uses MassTransit — broker can be swapped freely.
/// LSP: Fully satisfies IMessagePublisher for any class T.
/// </summary>
public sealed class RabbitMqPublisher : IMessagePublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<RabbitMqPublisher> _logger;

    /// <summary>Initializes the publisher with a MassTransit publish endpoint.</summary>
    public RabbitMqPublisher(IPublishEndpoint publishEndpoint, ILogger<RabbitMqPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class
    {
        await _publishEndpoint.Publish(message, ct);
        _logger.LogDebug("Published {MessageType} to broker.", typeof(T).Name);
    }
}
