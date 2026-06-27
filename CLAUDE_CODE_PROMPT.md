# 🚀 Claude Code Prompt — PedidoRapido (Portfólio .NET)

> **Model:** claude-opus-4-8  
> **Usar no:** Visual Studio Code com extensão Claude Code  
> **Cole esse prompt inteiro no chat do Claude Code**

---

## PROMPT

```
You are an expert .NET 8 software architect. Your task is to scaffold a complete,
production-quality (but minimal) microservices solution called "PedidoRapido"
for a developer portfolio.

## 🎯 GOAL
Build the SIMPLEST possible microservices system that clearly demonstrates:
- Clean Architecture + SOLID principles
- REST API with Swagger
- Microservices (2 services with their own databases)
- Async messaging with RabbitMQ + MassTransit

## 📦 PRODUCT: PedidoRapido
A customer places an order → RabbitMQ notifies the Notification Service → 
Notification Service logs a confirmation.
That's it. Simple domain, rich architecture.

---

## 🏗️ SOLUTION STRUCTURE

Create this exact folder/file structure:

PedidoRapido/
├── docker-compose.yml
├── README.md
│
├── src/
│   ├── OrderService/
│   │   ├── OrderService.csproj
│   │   ├── Program.cs
│   │   ├── Domain/
│   │   │   ├── Entities/
│   │   │   │   └── Order.cs
│   │   │   └── Events/
│   │   │       └── OrderCreatedEvent.cs
│   │   ├── Application/
│   │   │   ├── DTOs/
│   │   │   │   ├── CreateOrderRequest.cs
│   │   │   │   └── OrderResponse.cs
│   │   │   ├── Interfaces/
│   │   │   │   └── IOrderRepository.cs
│   │   │   └── UseCases/
│   │   │       └── CreateOrderUseCase.cs
│   │   ├── Infrastructure/
│   │   │   ├── Data/
│   │   │   │   ├── AppDbContext.cs
│   │   │   │   └── OrderRepository.cs
│   │   │   └── Messaging/
│   │   │       └── RabbitMqPublisher.cs
│   │   └── Api/
│   │       └── Controllers/
│   │           └── OrdersController.cs
│   │
│   └── NotificationService/
│       ├── NotificationService.csproj
│       ├── Program.cs
│       ├── Application/
│       │   └── Consumers/
│       │       └── OrderCreatedConsumer.cs
│       └── Infrastructure/
│           └── Messaging/
│               └── RabbitMqConsumer.cs
│
└── shared/
    └── PedidoRapido.Contracts/
        ├── PedidoRapido.Contracts.csproj
        └── Events/
            └── OrderCreatedEvent.cs

---

## 🛠️ TECH STACK

### OrderService
- .NET 8 Web API (minimal hosting, Controllers)
- Entity Framework Core 8 with SQLite (simple, no extra infra)
- MassTransit 8 + RabbitMQ transport
- Swashbuckle (Swagger/OpenAPI)
- FluentValidation

### NotificationService
- .NET 8 Worker Service (BackgroundService)
- MassTransit 8 + RabbitMQ transport
- No database needed (just logs to console)

### Shared
- PedidoRapido.Contracts — shared event contracts (class library)

### Infrastructure (docker-compose)
- RabbitMQ 3 (with management UI on port 15672)

---

## 📋 IMPLEMENTATION REQUIREMENTS

### 1. DOMAIN LAYER (OrderService)

```csharp
// Order.cs — rich domain model, not anemic
public class Order
{
    public Guid Id { get; private set; }
    public string CustomerName { get; private set; }
    public string ProductName { get; private set; }
    public int Quantity { get; private set; }
    public decimal TotalPrice { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Order() { } // EF Core

    public static Order Create(string customerName, string productName,
                               int quantity, decimal unitPrice)
    {
        // Validate here — domain rules
        if (quantity <= 0) throw new DomainException("Quantity must be positive.");
        if (unitPrice <= 0) throw new DomainException("Unit price must be positive.");

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

    public void Confirm() => Status = OrderStatus.Confirmed;
}

public enum OrderStatus { Pending, Confirmed, Cancelled }
```

### 2. APPLICATION LAYER (OrderService)

```csharp
// IOrderRepository.cs
public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct = default);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Order>> GetAllAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

// CreateOrderUseCase.cs — single responsibility, depends on abstractions
public class CreateOrderUseCase
{
    private readonly IOrderRepository _repository;
    private readonly IMessagePublisher _publisher;

    public CreateOrderUseCase(IOrderRepository repository, IMessagePublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task<OrderResponse> ExecuteAsync(CreateOrderRequest request, CancellationToken ct)
    {
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

        return new OrderResponse(order.Id, order.CustomerName,
                                 order.ProductName, order.TotalPrice,
                                 order.Status.ToString(), order.CreatedAt);
    }
}
```

### 3. API LAYER (OrderService)

```csharp
// OrdersController.cs — thin controller, just delegates
[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly CreateOrderUseCase _createOrderUseCase;
    private readonly IOrderRepository _repository;

    public OrdersController(CreateOrderUseCase createOrderUseCase,
                             IOrderRepository repository)
    {
        _createOrderUseCase = createOrderUseCase;
        _repository = repository;
    }

    /// <summary>Creates a new order and publishes an event to RabbitMQ.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request,
                                            CancellationToken ct)
    {
        var result = await _createOrderUseCase.ExecuteAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Gets an order by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(id, ct);
        if (order is null) return NotFound();
        return Ok(new OrderResponse(order.Id, order.CustomerName,
                                    order.ProductName, order.TotalPrice,
                                    order.Status.ToString(), order.CreatedAt));
    }

    /// <summary>Lists all orders.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var orders = await _repository.GetAllAsync(ct);
        return Ok(orders.Select(o => new OrderResponse(o.Id, o.CustomerName,
                                                        o.ProductName, o.TotalPrice,
                                                        o.Status.ToString(), o.CreatedAt)));
    }
}
```

### 4. INFRASTRUCTURE LAYER (OrderService)

```csharp
// AppDbContext.cs
public class AppDbContext : DbContext
{
    public DbSet<Order> Orders => Set<Order>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

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

// RabbitMqPublisher.cs — implements IMessagePublisher
public class RabbitMqPublisher : IMessagePublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    public RabbitMqPublisher(IPublishEndpoint publishEndpoint)
        => _publishEndpoint = publishEndpoint;

    public async Task PublishAsync<T>(T message, CancellationToken ct) where T : class
        => await _publishEndpoint.Publish(message, ct);
}
```

### 5. NOTIFICATION SERVICE (Consumer)

```csharp
// OrderCreatedConsumer.cs
public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
        => _logger = logger;

    public Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var order = context.Message;
        _logger.LogInformation(
            "📬 NOTIFICATION: New order received! " +
            "OrderId: {OrderId} | Customer: {Customer} | " +
            "Product: {Product} | Total: {Total:C} | At: {CreatedAt}",
            order.OrderId, order.CustomerName,
            order.ProductName, order.TotalPrice, order.CreatedAt);

        return Task.CompletedTask;
    }
}
```

### 6. PROGRAM.CS (OrderService)

```csharp
var builder = WebApplication.CreateBuilder(args);

// API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "PedidoRapido - Order Service", Version = "v1" });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
});

// Database
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=orders.db"));

// DI — Dependency Inversion (SOLID)
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IMessagePublisher, RabbitMqPublisher>();
builder.Services.AddScoped<CreateOrderUseCase>();

// MassTransit + RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
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

// Migrate DB on startup
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

### 7. PROGRAM.CS (NotificationService)

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        cfg.ConfigureEndpoints(ctx);
    });
});

var host = builder.Build();
await host.RunAsync();
```

### 8. SHARED CONTRACTS

```csharp
// PedidoRapido.Contracts/Events/OrderCreatedEvent.cs
namespace PedidoRapido.Contracts.Events;

public record OrderCreatedEvent
{
    public Guid OrderId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public decimal TotalPrice { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

### 9. DOCKER COMPOSE

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
      context: ./src/OrderService
    container_name: pedidorapido-order-service
    ports:
      - "5001:8080"
    environment:
      - RabbitMQ__Host=rabbitmq
      - RabbitMQ__Username=guest
      - RabbitMQ__Password=guest
    depends_on:
      rabbitmq:
        condition: service_healthy

  notification-service:
    build:
      context: ./src/NotificationService
    container_name: pedidorapido-notification-service
    environment:
      - RabbitMQ__Host=rabbitmq
      - RabbitMQ__Username=guest
      - RabbitMQ__Password=guest
    depends_on:
      rabbitmq:
        condition: service_healthy
```

### 10. DOCKERFILES

Create a Dockerfile for each service:

```dockerfile
# OrderService/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OrderService.csproj", "."]
COPY ["../../shared/PedidoRapido.Contracts/PedidoRapido.Contracts.csproj",
      "../../shared/PedidoRapido.Contracts/"]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrderService.dll"]
```

---

## 📦 NUGET PACKAGES

### OrderService.csproj
- Microsoft.EntityFrameworkCore.Sqlite (8.x)
- Microsoft.EntityFrameworkCore.Design (8.x)
- MassTransit (8.x)
- MassTransit.RabbitMQ (8.x)
- Swashbuckle.AspNetCore (6.x)
- FluentValidation.AspNetCore (11.x)
- Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore (8.x)

### NotificationService.csproj
- MassTransit (8.x)
- MassTransit.RabbitMQ (8.x)

---

## ✅ SOLID CHECKLIST — add XML comments noting each principle

- **S** (SRP): Each class has ONE responsibility. Note it on each file.
- **O** (OCP): IMessagePublisher allows new publishers without changing UseCase.
- **L** (LSP): OrderRepository fully implements IOrderRepository.
- **I** (ISP): IMessagePublisher and IOrderRepository are small, focused interfaces.
- **D** (DIP): UseCase depends on abstractions (interfaces), not concrete classes.

---

## 📝 README.md

Generate a README.md with:
1. Architecture diagram (ASCII art showing the two services + RabbitMQ)
2. How to run with Docker (`docker-compose up`)
3. How to run locally (pre-requisites: .NET 8 SDK + Docker for RabbitMQ)
4. Swagger URL: http://localhost:5001/swagger
5. RabbitMQ Management UI: http://localhost:15672 (guest/guest)
6. Example curl to POST an order

---

## 🔥 FINAL INSTRUCTIONS

1. Generate ALL files completely — no placeholders, no "// TODO"
2. Every class must have XML doc comments
3. Use `record` types for DTOs (immutable by default)
4. Use `CancellationToken` on all async methods
5. Use `ILogger<T>` for logging in all classes
6. All strings go to `appsettings.json` — no magic strings in code
7. Add a `DomainException.cs` class for domain-level errors
8. Make the project compile and run with just `docker-compose up`

Start by creating the solution file, then scaffold all projects and files.
```

---

## ⚡ COMO USAR NO VS CODE

1. Abra o VS Code em uma pasta vazia
2. Abra o painel do **Claude Code** (`Ctrl+Shift+P` → "Claude Code")
3. Certifique-se de estar usando o modelo **claude-opus-4-8**
4. Cole o bloco inteiro acima (de `You are an expert...` até `...all projects and files.`)
5. Aguarde — o Claude Code vai criar todos os arquivos automaticamente
6. Ao final, rode: `docker-compose up`
7. Acesse: `http://localhost:5001/swagger`

---

## 🗺️ FLUXO DO SISTEMA

```
[Cliente]
    │
    │ POST /api/orders
    ▼
[Order Service :5001]
    │  1. Valida request
    │  2. Cria Order (Domain)
    │  3. Salva no SQLite
    │  4. Publica OrderCreatedEvent
    ▼
[RabbitMQ :5672]
    │
    │ OrderCreatedEvent
    ▼
[Notification Service]
    │  Consome o evento
    └─ Loga: "📬 NOTIFICATION: New order received!"
```
