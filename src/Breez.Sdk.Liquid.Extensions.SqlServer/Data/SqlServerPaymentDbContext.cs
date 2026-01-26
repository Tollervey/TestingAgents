using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Breez.Sdk.Liquid.Extensions.SqlServer.Data;

/// <summary>
/// SQL Server database context for Breez SDK payment persistence.
/// Manages <see cref="PaymentState"/> entities with SQL Server-specific configurations.
/// </summary>
public class SqlServerPaymentDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerPaymentDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    public SqlServerPaymentDbContext(DbContextOptions<SqlServerPaymentDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the DbSet of payment states.
    /// </summary>
    public DbSet<PaymentState> PaymentStates => Set<PaymentState>();

    /// <summary>
    /// Configures the model using Fluent API.
    /// Applies <see cref="PaymentStateConfiguration"/> for the <see cref="PaymentState"/> entity.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure the context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configuration
        modelBuilder.ApplyConfiguration(new PaymentStateConfiguration());
    }
}
