using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Breez.Sdk.Liquid.Extensions.PostgreSql.Data;

/// <summary>
/// Entity Framework Core DbContext for PostgreSQL-backed payment state persistence.
/// </summary>
public class PostgreSqlPaymentDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlPaymentDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to configure the context.</param>
    public PostgreSqlPaymentDbContext(DbContextOptions<PostgreSqlPaymentDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the payment states DbSet.
    /// </summary>
    public DbSet<PaymentState> PaymentStates => Set<PaymentState>();

    /// <summary>
    /// Configures the entity model using PostgreSQL-specific settings.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new PaymentStateConfiguration());
    }
}
