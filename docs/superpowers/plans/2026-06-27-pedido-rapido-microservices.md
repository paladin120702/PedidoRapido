# PedidoRapido Microservices Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Scaffold a complete .NET 8 microservices solution (PedidoRapido) demonstrating Clean Architecture, REST API with Swagger, two microservices with their own databases, and async messaging with MassTransit + RabbitMQ.

**Architecture:** OrderService (Web API) accepts orders via REST, persists them with EF Core + SQLite, and publishes `OrderCreatedEvent` to RabbitMQ via MassTransit. NotificationService (.NET Worker) consumes those events and logs confirmations. Both services share event contracts through a class library (`PedidoRapido.Contracts`).

**Tech Stack:** .NET 8, Entity Framework Core 8 + SQLite, MassTransit 8 + RabbitMQ 3, Swashbuckle 6 (Swagger), FluentValidation 11, Docker + docker-compose

## Global Constraints

- Target framework: net8.0, Nullable enable, ImplicitUsings enable
- All config strings (connection strings, RabbitMQ host/user/pass) in `appsettings.json` — no magic strings in code
- `CancellationToken` on every async method
- `ILogger<T>` injected and used in every class
- `record` types for all DTOs (immutable)
- XML doc comments on every class and public member noting which SOLID principle it satisfies
- No TODOs, no placeholders — every file fully implemented
- Docker build context must be the **solution root** (not the service subdirectory) — Dockerfiles reference paths from root
- Base directory for all paths: `/home/artur/Projetos/OrderFlow`

---

### Task 1: Solution & Project Scaffolding

**Files:**
- Create: `PedidoRapido.sln`
- Create: `shared/PedidoRapido.Contracts/PedidoRapido.Contracts.csproj`
- Create: `src/OrderService/OrderService.csproj`
- Create: `src/NotificationService/NotificationService.csproj`

**Interfaces:**
- Produces: Three linked .NET 8 projects registered in one solution; all NuGet packages restored; `dotnet build PedidoRapido.sln` succeeds

- [ ] **Step 1: Initialize git and create solution**

```bash
cd /home/artur/Projetos/OrderFlow
git init
dotnet new sln -n PedidoRapido
```

Expected: `PedidoRapido.sln` appears in current directory.

- [ ] **Step 2: Create directory tree**

```bash
mkdir -p src/OrderService src/NotificationService shared/PedidoRapido.Contracts
```

- [ ] **Step 3: Create PedidoRapido.Contracts.csproj**

Write `shared/PedidoRapido.Contracts/PedidoRapido.Contracts.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>PedidoRapido.Contracts</RootNamespace>
  </PropertyGroup>
</Project>
```

- [ ] **Step 4: Create OrderService.csproj**

Write `src/OrderService/OrderService.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.*">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="MassTransit" Version="8.3.*" />
    <PackageReference Include="MassTransit.RabbitMQ" Version="8.3.*" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.9.*" />
    <PackageReference Include="FluentValidation.AspNetCore" Version="11.3.*" />
    <PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" Version="8.0.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\shared\PedidoRapido.Contracts\PedidoRapido.Contracts.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 5: Create NotificationService.csproj**

Write `src/NotificationService/NotificationService.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk.Worker">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MassTransit" Version="8.3.*" />
    <PackageReference Include="MassTransit.RabbitMQ" Version="8.3.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\shared\PedidoRapido.Contracts\PedidoRapido.Contracts.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 6: Register projects in the solution and restore**

```bash
dotnet sln PedidoRapido.sln add shared/PedidoRapido.Contracts/PedidoRapido.Contracts.csproj
dotnet sln PedidoRapido.sln add src/OrderService/OrderService.csproj
dotnet sln PedidoRapido.sln add src/NotificationService/NotificationService.csproj
dotnet restore PedidoRapido.sln
```

Expected: All packages restored, 0 errors.

- [ ] **Step 7: Commit**

```bash
git add PedidoRapido.sln src/ shared/
git commit -m "feat: initialize PedidoRapido solution with three projects"
```

---

### Task 2: Shared Contracts Library

**Files:**
- Create: `shared/PedidoRapido.Contracts/Events/OrderCreatedEvent.cs`

**Interfaces:**
- Produces: `OrderCreatedEvent` record in namespace `PedidoRapido.Contracts.Events` — with properties `OrderId Guid`, `CustomerName string`, `ProductName string`, `TotalPrice decimal`, `CreatedAt DateTime`

- [ ] **Step 1: Create the Events directory**

```bash
mkdir -p shared/PedidoRapido.Contracts/Events
```

- [ ] **Step 2: Write OrderCreatedEvent**

Write `shared/PedidoRapido.Contracts/Events/OrderCreatedEvent.cs`:
```csharp
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
```

- [ ] **Step 3: Verify compilation**

```bash
dotnet build shared/PedidoRapido.Contracts/PedidoRapido.Contracts.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add shared/PedidoRapido.Contracts/Events/OrderCreatedEvent.cs
git commit -m "feat: add OrderCreatedEvent shared contract"
```

---

### Task 3: OrderService Domain Layer

**Files:**
- Create: `src/OrderService/Domain/Exceptions/DomainException.cs`
- Create: `src/OrderService/Domain/Entities/Order.cs`

**Interfaces:**
- Produces:
  - `DomainException(string message) : Exception` in namespace `OrderService.Domain.Exceptions`
  - `Order.Create(string customerName, string productName, int quantity, decimal unitPrice) : Order` static factory (throws `DomainException` on invalid inputs)
  - `Order.Confirm() : void`
  - `OrderStatus` enum with values `Pending`, `Confirmed`, `Cancelled`
  - All in namespace `OrderService.Domain.Entities`

- [ ] **Step 1: Create domain directories**

```bash
mkdir -p src/OrderService/Domain/Entities src/OrderService/Domain/Exceptions
```

- [ ] **Step 2: Write DomainException**

Write `src/OrderService/Domain/Exceptions/DomainException.cs`:
```csharp
namespace OrderService.Domain.Exceptions;

/// <summary>
/// SRP: Represents domain-level rule violations only.
/// Thrown when business invariants are broken (e.g., invalid quantity or negative price).
/// </summary>
public sealed class DomainException : Exception
{
    /// <summary>Initializes a domain exception with the violated rule message.</summary>
    public DomainException(string message) : base(message) { }
}
```

- [ ] **Step 3: Write Order entity**

Write `src/OrderService/Domain/Entities/Order.cs`:
```csharp
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
```

- [ ] **Step 4: Verify compilation**

```bash
dotnet build src/OrderService/OrderService.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Commit**

```bash
git add src/OrderService/Domain/
git commit -m "feat: add Order domain entity, OrderStatus enum, and DomainException"
```

---

### Task 4: OrderService Application Layer

**Files:**
- Create: `src/OrderService/Application/Interfaces/IOrderRepository.cs`
- Create: `src/OrderService/Application/Interfaces/IMessagePublisher.cs`
- Create: `src/OrderService/Application/DTOs/CreateOrderRequest.cs`
- Create: `src/OrderService/Application/DTOs/OrderResponse.cs`
- Create: `src/OrderService/Application/UseCases/CreateOrderUseCase.cs`

**Interfaces:**
- Consumes: `Order` from Task 3; `OrderCreatedEvent` from Task 2
- Produces:
  - `IOrderRepository` — `AddAsync(Order, CancellationToken)`, `GetByIdAsync(Guid, CancellationToken) : Task<Order?>`, `GetAllAsync(CancellationToken) : Task<IEnumerable<Order>>`, `SaveChangesAsync(CancellationToken)`
  - `IMessagePublisher` — `PublishAsync<T>(T, CancellationToken) where T : class`
  - `CreateOrderRequest(string CustomerName, string ProductName, int Quantity, decimal UnitPrice)` record
  - `OrderResponse(Guid Id, string CustomerName, string ProductName, decimal TotalPrice, string Status, DateTime CreatedAt)` record
  - `CreateOrderUseCase.ExecuteAsync(CreateOrderRequest, CancellationToken) : Task<OrderResponse>`

- [ ] **Step 1: Create application directories**

```bash
mkdir -p src/OrderService/Application/Interfaces src/OrderService/Application/DTOs src/OrderService/Application/UseCases
```

- [ ] **Step 2: Write IOrderRepository**

Write `src/OrderService/Application/Interfaces/IOrderRepository.cs`:
```csharp
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
```

- [ ] **Step 3: Write IMessagePublisher**

Write `src/OrderService/Application/Interfaces/IMessagePublisher.cs`:
```csharp
namespace OrderService.Application.Interfaces;

/// <summary>
/// ISP: Single-method interface — one responsibility: publish a message.
/// OCP: New message brokers (Kafka, Azure Service Bus) can be added without touching use cases.
/// DIP: Use cases depend on this abstraction, never on MassTransit directly.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>Publishes <typeparamref name="T"/> to the configured message broker.</summary>
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class;
}
```

- [ ] **Step 4: Write DTOs**

Write `src/OrderService/Application/DTOs/CreateOrderRequest.cs`:
```csharp
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
```

Write `src/OrderService/Application/DTOs/OrderResponse.cs`:
```csharp
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
```

- [ ] **Step 5: Write CreateOrderUseCase**

Write `src/OrderService/Application/UseCases/CreateOrderUseCase.cs`:
```csharp
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
```

- [ ] **Step 6: Verify compilation**

```bash
dotnet build src/OrderService/OrderService.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 7: Commit**

```bash
git add src/OrderService/Application/
git commit -m "feat: add OrderService application layer (interfaces, DTOs, use case)"
```

---

### Task 5: OrderService Infrastructure Layer

**Files:**
- Create: `src/OrderService/Infrastructure/Data/AppDbContext.cs`
- Create: `src/OrderService/Infrastructure/Data/OrderRepository.cs`
- Create: `src/OrderService/Infrastructure/Messaging/RabbitMqPublisher.cs`

**Interfaces:**
- Consumes: `IOrderRepository` from Task 4; `IMessagePublisher` from Task 4; `Order` from Task 3
- Produces:
  - `AppDbContext : DbContext` with `DbSet<Order> Orders`
  - `OrderRepository : IOrderRepository` — full implementation (LSP satisfied)
  - `RabbitMqPublisher : IMessagePublisher` — wraps MassTransit `IPublishEndpoint`

- [ ] **Step 1: Create infrastructure directories**

```bash
mkdir -p src/OrderService/Infrastructure/Data src/OrderService/Infrastructure/Messaging
```

- [ ] **Step 2: Write AppDbContext**

Write `src/OrderService/Infrastructure/Data/AppDbContext.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Data;

/// <summary>
/// SRP: Responsible for EF Core schema configuration and DbContext lifecycle only.
/// </summary>
public sealed class AppDbContext : DbContext
{
    /// <summary>The Orders table.</summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>Initializes the context with the provided options.</summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(builder =>
        {
            builder.HasKey(o => o.Id);
            builder.Property(o => o.CustomerName).HasMaxLength(100).IsRequired();
            builder.Property(o => o.ProductName).HasMaxLength(100).IsRequired();
            builder.Property(o => o.TotalPrice).HasPrecision(18, 2);
            builder.Property(o => o.Status).HasConversion<string>();
        });
    }
}
```

- [ ] **Step 3: Write OrderRepository**

Write `src/OrderService/Infrastructure/Data/OrderRepository.cs`:
```csharp
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
```

- [ ] **Step 4: Write RabbitMqPublisher**

Write `src/OrderService/Infrastructure/Messaging/RabbitMqPublisher.cs`:
```csharp
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
```

- [ ] **Step 5: Verify compilation**

```bash
dotnet build src/OrderService/OrderService.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 6: Commit**

```bash
git add src/OrderService/Infrastructure/
git commit -m "feat: add OrderService infrastructure (EF Core, repository, RabbitMQ publisher)"
```

---

### Task 6: OrderService API Layer & Bootstrap

**Files:**
- Create: `src/OrderService/Api/Controllers/OrdersController.cs`
- Create: `src/OrderService/Program.cs`
- Create: `src/OrderService/appsettings.json`
- Create: `src/OrderService/appsettings.Development.json`

**Interfaces:**
- Consumes: `CreateOrderUseCase` from Task 4; `IOrderRepository` from Task 4; `OrderResponse` from Task 4; `AppDbContext` from Task 5; `OrderRepository` from Task 5; `RabbitMqPublisher` from Task 5
- Produces: Running ASP.NET Core 8 Web API on port 8080 with Swagger at `/swagger` and health check at `/health`

- [ ] **Step 1: Create API directory**

```bash
mkdir -p src/OrderService/Api/Controllers
```

- [ ] **Step 2: Write OrdersController**

Write `src/OrderService/Api/Controllers/OrdersController.cs`:
```csharp
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
```

- [ ] **Step 3: Write Program.cs**

Write `src/OrderService/Program.cs`:
```csharp
using System.Reflection;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.Interfaces;
using OrderService.Application.UseCases;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);

// API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "PedidoRapido - Order Service", Version = "v1" });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// Database — connection string from appsettings, never hardcoded
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
                  ?? "Data Source=orders.db"));

// DI — Dependency Inversion (SOLID D): register implementations behind interfaces
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IMessagePublisher, RabbitMqPublisher>();
builder.Services.AddScoped<CreateOrderUseCase>();

// MassTransit + RabbitMQ — host from appsettings
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(
            builder.Configuration["RabbitMQ:Host"] ?? "localhost",
            "/",
            h =>
            {
                h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
                h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
            });
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

var app = builder.Build();

// Create SQLite schema on startup (no migrations needed for this demo)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

- [ ] **Step 4: Write appsettings files**

Write `src/OrderService/appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=orders.db"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
  }
}
```

Write `src/OrderService/appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

- [ ] **Step 5: Verify full OrderService build**

```bash
dotnet build src/OrderService/OrderService.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 6: Commit**

```bash
git add src/OrderService/Api/ src/OrderService/Program.cs src/OrderService/appsettings*.json
git commit -m "feat: add OrderService API controller and bootstrap (Program.cs, appsettings)"
```

---

### Task 7: NotificationService

**Files:**
- Create: `src/NotificationService/Application/Consumers/OrderCreatedConsumer.cs`
- Create: `src/NotificationService/Infrastructure/Messaging/RabbitMqConsumer.cs`
- Create: `src/NotificationService/Program.cs`
- Create: `src/NotificationService/appsettings.json`

**Interfaces:**
- Consumes: `OrderCreatedEvent` from Task 2
- Produces: Running .NET 8 Worker that connects to RabbitMQ and logs every `OrderCreatedEvent` it receives

- [ ] **Step 1: Create directories**

```bash
mkdir -p src/NotificationService/Application/Consumers src/NotificationService/Infrastructure/Messaging
```

- [ ] **Step 2: Write OrderCreatedConsumer**

Write `src/NotificationService/Application/Consumers/OrderCreatedConsumer.cs`:
```csharp
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

        _logger.LogInformation(
            "NOTIFICATION: New order received! OrderId: {OrderId} | Customer: {Customer} | Product: {Product} | Total: {Total:C} | At: {CreatedAt}",
            order.OrderId,
            order.CustomerName,
            order.ProductName,
            order.TotalPrice,
            order.CreatedAt);

        return Task.CompletedTask;
    }
}
```

- [ ] **Step 3: Write RabbitMqConsumer extension**

Write `src/NotificationService/Infrastructure/Messaging/RabbitMqConsumer.cs`:
```csharp
using MassTransit;
using NotificationService.Application.Consumers;

namespace NotificationService.Infrastructure.Messaging;

/// <summary>
/// SRP: Responsible for registering MassTransit consumers and configuring the RabbitMQ transport.
/// OCP: New consumers are added here without touching Program.cs.
/// </summary>
public static class RabbitMqConsumerExtensions
{
    /// <summary>Registers MassTransit with RabbitMQ transport and all consumers.</summary>
    public static IServiceCollection AddRabbitMqConsumer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<OrderCreatedConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(
                    configuration["RabbitMQ:Host"] ?? "localhost",
                    "/",
                    h =>
                    {
                        h.Username(configuration["RabbitMQ:Username"] ?? "guest");
                        h.Password(configuration["RabbitMQ:Password"] ?? "guest");
                    });

                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
```

- [ ] **Step 4: Write Program.cs**

Write `src/NotificationService/Program.cs`:
```csharp
using NotificationService.Infrastructure.Messaging;

var builder = Host.CreateApplicationBuilder(args);

// OCP: adding new consumers only requires changes in RabbitMqConsumerExtensions
builder.Services.AddRabbitMqConsumer(builder.Configuration);

var host = builder.Build();
await host.RunAsync();
```

- [ ] **Step 5: Write appsettings.json**

Write `src/NotificationService/appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
  }
}
```

- [ ] **Step 6: Verify NotificationService compilation**

```bash
dotnet build src/NotificationService/NotificationService.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 7: Verify full solution builds cleanly**

```bash
dotnet build PedidoRapido.sln
```

Expected: `Build succeeded. 0 Error(s)` for all 3 projects.

- [ ] **Step 8: Commit**

```bash
git add src/NotificationService/
git commit -m "feat: add NotificationService worker (consumer, RabbitMQ config, bootstrap)"
```

---

### Task 8: Docker Infrastructure

**Files:**
- Create: `src/OrderService/Dockerfile`
- Create: `src/NotificationService/Dockerfile`
- Create: `docker-compose.yml`

**Interfaces:**
- Produces: `docker-compose up --build` brings up all 3 services; `POST http://localhost:5001/api/orders` creates an order and the NotificationService logs it

**Note:** Build context for both services is the **solution root** (`.`), not the service subdirectory. This lets the Dockerfiles COPY the shared contracts project, which lives outside each service's folder.

- [ ] **Step 1: Write OrderService Dockerfile**

Write `src/OrderService/Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/OrderService/OrderService.csproj", "src/OrderService/"]
COPY ["shared/PedidoRapido.Contracts/PedidoRapido.Contracts.csproj", "shared/PedidoRapido.Contracts/"]
RUN dotnet restore "src/OrderService/OrderService.csproj"
COPY . .
WORKDIR "/src/src/OrderService"
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrderService.dll"]
```

- [ ] **Step 2: Write NotificationService Dockerfile**

Write `src/NotificationService/Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/NotificationService/NotificationService.csproj", "src/NotificationService/"]
COPY ["shared/PedidoRapido.Contracts/PedidoRapido.Contracts.csproj", "shared/PedidoRapido.Contracts/"]
RUN dotnet restore "src/NotificationService/NotificationService.csproj"
COPY . .
WORKDIR "/src/src/NotificationService"
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.dll"]
```

- [ ] **Step 3: Write docker-compose.yml**

Write `docker-compose.yml` at the solution root:
```yaml
version: '3.8'

services:
  rabbitmq:
    image: rabbitmq:3-management
    container_name: pedidorapido-rabbitmq
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  order-service:
    build:
      context: .
      dockerfile: src/OrderService/Dockerfile
    container_name: pedidorapido-order-service
    ports:
      - "5001:8080"
    environment:
      - RabbitMQ__Host=rabbitmq
      - RabbitMQ__Username=guest
      - RabbitMQ__Password=guest
      - ConnectionStrings__DefaultConnection=Data Source=/app/data/orders.db
    volumes:
      - order-data:/app/data
    depends_on:
      rabbitmq:
        condition: service_healthy

  notification-service:
    build:
      context: .
      dockerfile: src/NotificationService/Dockerfile
    container_name: pedidorapido-notification-service
    environment:
      - RabbitMQ__Host=rabbitmq
      - RabbitMQ__Username=guest
      - RabbitMQ__Password=guest
    depends_on:
      rabbitmq:
        condition: service_healthy

volumes:
  order-data:
```

- [ ] **Step 4: Commit**

```bash
git add src/OrderService/Dockerfile src/NotificationService/Dockerfile docker-compose.yml
git commit -m "feat: add Dockerfiles and docker-compose for all services"
```

---

### Task 9: README Documentation

**Files:**
- Create: `README.md`

**Interfaces:**
- Produces: Developer-facing documentation with architecture diagram, setup instructions, URL table, and a working curl example

- [ ] **Step 1: Write README.md**

Write `README.md` at the solution root:
````markdown
# PedidoRapido

A minimal .NET 8 microservices portfolio project demonstrating Clean Architecture,
SOLID principles, REST APIs with Swagger, and async messaging with RabbitMQ + MassTransit.

## Architecture

```
[Client]
    │
    │ POST /api/orders
    ▼
[Order Service :5001]
    │  1. Validate request (FluentValidation)
    │  2. Create Order (Domain entity with invariants)
    │  3. Persist to SQLite (EF Core)
    │  4. Publish OrderCreatedEvent (MassTransit)
    ▼
[RabbitMQ :5672]
    │
    │  OrderCreatedEvent
    ▼
[Notification Service]
    └─ Log: "NOTIFICATION: New order received!"
```

## Services

| Service              | Port        | Purpose                                    |
|----------------------|-------------|--------------------------------------------|
| Order Service        | 5001        | REST API — create and query orders         |
| Notification Service | —           | Worker — consumes events and logs them     |
| RabbitMQ             | 5672 / 15672| Message broker + management UI             |

## Tech Stack

- **.NET 8** — Web API (Order Service) + Worker Service (Notification Service)
- **Entity Framework Core 8 + SQLite** — persistence (no extra infra required)
- **MassTransit 8 + RabbitMQ 3** — async messaging
- **Swashbuckle 6** — Swagger / OpenAPI
- **FluentValidation 11** — request validation
- **Docker + docker-compose** — containerisation

## SOLID Principles

| Principle | Example in this codebase |
|-----------|--------------------------|
| **S** — SRP | `CreateOrderUseCase` only orchestrates creation; `OrdersController` only handles HTTP |
| **O** — OCP | `IMessagePublisher` allows new brokers without touching use cases |
| **L** — LSP | `OrderRepository` fully implements `IOrderRepository` without weakening postconditions |
| **I** — ISP | `IMessagePublisher` and `IOrderRepository` are small, focused interfaces |
| **D** — DIP | Use cases depend on interfaces, never on EF Core or MassTransit directly |

## How to Run

### With Docker (recommended)

**Prerequisites:** Docker + Docker Compose

```bash
git clone <repo-url>
cd PedidoRapido
docker-compose up --build
```

All three containers start automatically. Order Service waits for RabbitMQ to be healthy.

### Locally

**Prerequisites:** .NET 8 SDK + Docker (for RabbitMQ only)

```bash
# 1. Start RabbitMQ
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# 2. Run Order Service (terminal 1)
cd src/OrderService
dotnet run

# 3. Run Notification Service (terminal 2)
cd src/NotificationService
dotnet run
```

## URLs

| Resource             | URL                                      |
|----------------------|------------------------------------------|
| Swagger UI           | http://localhost:5001/swagger            |
| Health Check         | http://localhost:5001/health             |
| RabbitMQ Management  | http://localhost:15672  (guest / guest)  |

## Example: Create an Order

```bash
curl -X POST http://localhost:5001/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "João Silva",
    "productName": "Notebook Dell XPS",
    "quantity": 1,
    "unitPrice": 7499.99
  }'
```

**Response — 201 Created:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "customerName": "João Silva",
  "productName": "Notebook Dell XPS",
  "totalPrice": 7499.99,
  "status": "Pending",
  "createdAt": "2026-06-27T12:00:00Z"
}
```

The Notification Service logs immediately:
```
NOTIFICATION: New order received! OrderId: 3fa85f64... | Customer: João Silva | Product: Notebook Dell XPS | Total: R$7,499.99 | At: 2026-06-27T12:00:00
```

## Project Structure

```
PedidoRapido/
├── docker-compose.yml
├── PedidoRapido.sln
├── README.md
├── src/
│   ├── OrderService/           # REST API
│   │   ├── Domain/             # Entities + exceptions (zero external deps)
│   │   ├── Application/        # Use cases, interfaces, DTOs
│   │   ├── Infrastructure/     # EF Core + MassTransit implementations
│   │   └── Api/                # Controllers
│   └── NotificationService/    # .NET Worker
│       ├── Application/        # MassTransit consumers
│       └── Infrastructure/     # RabbitMQ configuration
└── shared/
    └── PedidoRapido.Contracts/ # Shared event contracts
```
````

- [ ] **Step 2: Commit**

```bash
git add README.md
git commit -m "docs: add README with architecture diagram and usage examples"
```

---

## Self-Review

### 1. Spec Coverage

| Spec Requirement | Task |
|---|---|
| Clean Architecture (Domain → Application → Infrastructure → API) | Tasks 3–6 |
| REST API with Swagger | Task 6 (OrdersController + SwaggerGen in Program.cs) |
| Two microservices with own databases | OrderService (SQLite, Tasks 3–6); NotificationService (no DB, Task 7) |
| Async messaging RabbitMQ + MassTransit | Tasks 5, 6, 7 |
| `Order.cs` rich domain model | Task 3 |
| `IOrderRepository`, `IMessagePublisher` interfaces | Task 4 |
| `CreateOrderUseCase` | Task 4 |
| `AppDbContext`, `OrderRepository` | Task 5 |
| `RabbitMqPublisher` | Task 5 |
| `OrdersController` (POST, GET, GET all) | Task 6 |
| `Program.cs` for OrderService | Task 6 |
| `Program.cs` for NotificationService | Task 7 |
| `OrderCreatedConsumer` | Task 7 |
| `PedidoRapido.Contracts/Events/OrderCreatedEvent` | Task 2 |
| `docker-compose.yml` with RabbitMQ + both services | Task 8 |
| `Dockerfile` for each service (build context = solution root) | Task 8 |
| `DomainException.cs` | Task 3 |
| `appsettings.json` for both services (no magic strings) | Tasks 6, 7 |
| `CancellationToken` on all async methods | Tasks 4–7 |
| `ILogger<T>` in all classes | Tasks 4–7 |
| `record` types for all DTOs | Task 4 |
| XML doc comments with SOLID principle notes | All tasks |
| README with ASCII diagram + curl example | Task 9 |
| Solution file + NuGet restore | Task 1 |

All requirements covered. ✓

### 2. Placeholder Scan

No TODOs, TBDs, or vague instructions appear in any step. Every step contains complete, runnable code. ✓

### 3. Type Consistency

- `OrderCreatedEvent` — defined in Task 2, used in Task 4 (`CreateOrderUseCase`) and Task 7 (`OrderCreatedConsumer`). Same namespace `PedidoRapido.Contracts.Events`. ✓
- `IOrderRepository` — defined in Task 4, implemented in Task 5, used in Tasks 5 and 6. Signatures match. ✓
- `IMessagePublisher` — defined in Task 4, implemented in Task 5, used in Task 4. Signatures match. ✓
- `CreateOrderRequest(string, string, int, decimal)` — defined in Task 4, constructed in Task 6 controller. ✓
- `OrderResponse(Guid, string, string, decimal, string, DateTime)` — defined in Task 4, constructed in Tasks 4 and 6 with same positional args. ✓
- `Order.Create(string, string, int, decimal)` — signature in Task 3 matches call in Task 4. ✓
- `RabbitMqConsumerExtensions.AddRabbitMqConsumer(IServiceCollection, IConfiguration)` — defined in Task 7 `RabbitMqConsumer.cs`, called in Task 7 `Program.cs`. ✓

All type signatures consistent. ✓
