namespace OrderService.Application.DTOs;

/// <summary>
/// SRP: Immutable DTO for incoming order creation requests only.
/// Validated by FluentValidation before reaching the use case.
/// </summary>
public record CreateOrderRequest(
    string CustomerName,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
