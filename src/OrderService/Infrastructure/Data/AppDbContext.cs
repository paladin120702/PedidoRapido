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
