using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Breez.Sdk.Liquid.Extensions.Sqlite.Data;

/// <summary>
/// SQLite database context for payment state persistence.
/// </summary>
public class SqlitePaymentDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlitePaymentDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to configure the context.</param>
    public SqlitePaymentDbContext(DbContextOptions<SqlitePaymentDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the PaymentStates DbSet.
    /// </summary>
    public DbSet<PaymentState> PaymentStates => Set<PaymentState>();

    /// <summary>
    /// Configures the model using Fluent API.
    /// </summary>
    /// <param name="modelBuilder">The model builder instance.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply PaymentState configuration
        modelBuilder.ApplyConfiguration(new PaymentStateConfiguration());
    }
}
