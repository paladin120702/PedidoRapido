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
