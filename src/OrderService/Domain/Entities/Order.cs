using OrderService.Domain.Exceptions;

namespace OrderService.Domain.Entities;

/// <summary>
/// SRP: Encapsulates order data and business rules only — no persistence or messaging concerns.
/// Rich domain model: enforces all invariants via the Create factory; no public setters.
/// </summary>
public sealed class Order
{
    /// <summary>Unique identifier of the order.</summary>
    public Guid Id { get; private set; }

    /// <summary>Name of the customer who placed the order.</summary>
    public string CustomerName { get; private set; } = string.Empty;

    /// <summary>Name of the product ordered.</summary>
    public string ProductName { get; private set; } = string.Empty;

    /// <summary>Number of units ordered.</summary>
    public int Quantity { get; private set; }

    /// <summary>Total price: quantity × unit price.</summary>
    public decimal TotalPrice { get; private set; }

    /// <summary>Current lifecycle status of the order.</summary>
    public OrderStatus Status { get; private set; }

    /// <summary>UTC timestamp when the order was created.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Required by EF Core — must not be called externally.</summary>
    private Order() { }

    /// <summary>
    /// Factory method that enforces domain rules before constructing an Order.
    /// LSP: Fully satisfies all invariants the domain requires.
    /// DIP: No dependency on infrastructure — pure domain logic only.
    /// </summary>
    /// <param name="customerName">Customer's name.</param>
    /// <param name="productName">Product name.</param>
    /// <param name="quantity">Units ordered — must be greater than zero.</param>
    /// <param name="unitPrice">Price per unit — must be greater than zero.</param>
    /// <exception cref="DomainException">Thrown when quantity or unit price is invalid.</exception>
    public static Order Create(string customerName, string productName, int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive.");
        if (unitPrice <= 0)
            throw new DomainException("Unit price must be positive.");

        return new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = customerName,
            ProductName = productName,
            Quantity = quantity,
            TotalPrice = quantity * unitPrice,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Transitions the order to Confirmed status.</summary>
    public void Confirm() => Status = OrderStatus.Confirmed;
}

/// <summary>Lifecycle states an order can occupy.</summary>
public enum OrderStatus
{
    /// <summary>Order placed but not yet confirmed.</summary>
    Pending,

    /// <summary>Order confirmed and being processed.</summary>
    Confirmed,

    /// <summary>Order was cancelled before fulfilment.</summary>
    Cancelled
}
