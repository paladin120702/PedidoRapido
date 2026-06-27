using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Data;

/// <summary>
/// SRP: Handles data access for Order entities only.
/// LSP: Fully and correctly implements IOrderRepository — no postcondition is weakened.
/// </summary>
public sealed class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<OrderRepository> _logger;

    /// <summary>Initializes the repository with a scoped DbContext.</summary>
    public OrderRepository(AppDbContext context, ILogger<OrderRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        await _context.Orders.AddAsync(order, ct);
        _logger.LogDebug("Order {OrderId} staged for insertion.", order.Id);
    }

    /// <inheritdoc/>
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Orders.FindAsync(new object[] { id }, ct);

    /// <inheritdoc/>
    public async Task<IEnumerable<Order>> GetAllAsync(CancellationToken ct = default)
        => await _context.Orders.AsNoTracking().ToListAsync(ct);

    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
