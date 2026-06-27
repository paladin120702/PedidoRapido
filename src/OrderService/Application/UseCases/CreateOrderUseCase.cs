using Microsoft.Extensions.Logging;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using PedidoRapido.Contracts.Events;

namespace OrderService.Application.UseCases;

/// <summary>
/// SRP: Orchestrates order creation — validation, persistence, and event publishing only.
/// DIP: Depends on IOrderRepository and IMessagePublisher abstractions injected via constructor.
/// OCP: Additional post-creation steps require new use cases, not modifications to this class.
/// </summary>
public sealed class CreateOrderUseCase
{
    private readonly IOrderRepository _repository;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<CreateOrderUseCase> _logger;

    /// <summary>Initializes the use case with required abstractions.</summary>
    public CreateOrderUseCase(
        IOrderRepository repository,
        IMessagePublisher publisher,
        ILogger<CreateOrderUseCase> logger)
    {
        _repository = repository;
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>
    /// Creates an order, persists it, publishes an OrderCreatedEvent, and returns a response DTO.
    /// </summary>
    /// <param name="request">The order creation payload.</param>
    /// <param name="ct">Cancellation token for the async pipeline.</param>
    /// <returns>Response DTO with the created order's details.</returns>
    public async Task<OrderResponse> ExecuteAsync(CreateOrderRequest request, CancellationToken ct)
    {
        _logger.LogInformation(
            "Creating order for customer {CustomerName}, product {ProductName}",
            request.CustomerName, request.ProductName);

        var order = Order.Create(
            request.CustomerName,
            request.ProductName,
            request.Quantity,
            request.UnitPrice);

        await _repository.AddAsync(order, ct);
        await _repository.SaveChangesAsync(ct);

        await _publisher.PublishAsync(new OrderCreatedEvent
        {
            OrderId = order.Id,
            CustomerName = order.CustomerName,
            ProductName = order.ProductName,
            TotalPrice = order.TotalPrice,
            CreatedAt = order.CreatedAt
        }, ct);

        _logger.LogInformation("Order {OrderId} created and event published.", order.Id);

        return new OrderResponse(
            order.Id,
            order.CustomerName,
            order.ProductName,
            order.TotalPrice,
            order.Status.ToString(),
            order.CreatedAt);
    }
}
