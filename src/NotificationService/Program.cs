using Microsoft.Extensions.Hosting;
using NotificationService.Infrastructure.Messaging;

var builder = Host.CreateApplicationBuilder(args);

// OCP: adding new consumers only requires changes in RabbitMqConsumerExtensions
builder.Services.AddRabbitMqConsumer(builder.Configuration);

var host = builder.Build();
await host.RunAsync();
