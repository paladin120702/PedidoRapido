using Microsoft.AspNetCore.Mvc;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Application.UseCases;

namespace OrderService.Api.Controllers;

/// <summary>
/// SRP: Handles HTTP concerns only — routing, model binding, and HTTP status codes.
///      All business logic is delegated to use cases and repositories.
/// DIP: Depends on abstractions (CreateOrderUseCase, IOrderRepository), not concrete types.
/// </summary>
[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public sealed class OrdersController : ControllerBase
{
    private readonly CreateOrderUseCase _createOrderUseCase;
    private readonly IOrderRepository _repository;

    /// <summary>Initializes the controller with required dependencies.</summary>
    public OrdersController(CreateOrderUseCase createOrderUseCase, IOrderRepository repository)
    {
        _createOrderUseCase = createOrderUseCase;
        _repository = repository;
    }

    /// <summary>Creates a new order and publishes an event to RabbitMQ.</summary>
    /// <param name="request">Order creation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>201 Created with the new order, or 400 Bad Request on validation failure.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var result = await _createOrderUseCase.ExecuteAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Gets an order by its unique identifier.</summary>
    /// <param name="id">The order GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK with the order, or 404 Not Found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(id, ct);
        if (order is null) return NotFound();

        return Ok(new OrderResponse(
            order.Id, order.CustomerName, order.ProductName,
            order.TotalPrice, order.Status.ToString(), order.CreatedAt));
    }

    /// <summary>Lists all orders.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK with the full list of orders.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var orders = await _repository.GetAllAsync(ct);
        return Ok(orders.Select(o => new OrderResponse(
            o.Id, o.CustomerName, o.ProductName,
            o.TotalPrice, o.Status.ToString(), o.CreatedAt)));
    }
}
