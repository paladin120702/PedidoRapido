using OrderService.Domain.Entities;

namespace OrderService.Application.Interfaces;

/// <summary>
/// ISP: Small, focused interface — only order-specific data operations.
/// DIP: Application layer depends on this abstraction; not on EF Core directly.
/// </summary>
public interface IOrderRepository
{
    /// <summary>Adds a new order to the data store (not yet committed).</summary>
    Task AddAsync(Order order, CancellationToken ct = default);

    /// <summary>Returns the order with the given id, or null if not found.</summary>
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns all orders in the data store.</summary>
    Task<IEnumerable<Order>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Commits all pending changes to the data store.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
