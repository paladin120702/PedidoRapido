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
