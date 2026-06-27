namespace OrderService.Application.DTOs;

/// <summary>
/// SRP: Immutable DTO for outgoing order data — decouples the API contract from the domain model.
/// </summary>
public record OrderResponse(
    Guid Id,
    string CustomerName,
    string ProductName,
    decimal TotalPrice,
    string Status,
    DateTime CreatedAt);
