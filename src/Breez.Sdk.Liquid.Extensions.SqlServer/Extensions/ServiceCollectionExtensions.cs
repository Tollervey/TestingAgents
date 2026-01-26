using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.SqlServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Breez.Sdk.Liquid.Extensions.SqlServer.Extensions;

/// <summary>
/// Extension methods for configuring Breez SDK SQL Server persistence services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Breez SDK SQL Server persistence services to the specified <see cref="IServiceCollection"/>.
    /// Registers <see cref="SqlServerPaymentDbContext"/> and <see cref="IPaymentRepository"/> with scoped lifetime.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <returns>The same service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="connectionString"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="connectionString"/> is empty or whitespace.
    /// </exception>
    /// <example>
    /// <code>
    /// services.AddBreezSdkSqlServer("Server=localhost;Database=BreezPayments;Trusted_Connection=True;");
    /// </code>
    /// </example>
    public static IServiceCollection AddBreezSdkSqlServer(
        this IServiceCollection services,
        string connectionString)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or whitespace.", nameof(connectionString));
        }

        return services.AddBreezSdkSqlServer(options =>
            options.UseSqlServer(connectionString));
    }

    /// <summary>
    /// Adds Breez SDK SQL Server persistence services to the specified <see cref="IServiceCollection"/>.
    /// Registers <see cref="SqlServerPaymentDbContext"/> and <see cref="IPaymentRepository"/> with scoped lifetime.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="optionsAction">
    /// A builder action to configure the <see cref="DbContextOptionsBuilder"/> for <see cref="SqlServerPaymentDbContext"/>.
    /// </param>
    /// <returns>The same service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="optionsAction"/> is null.
    /// </exception>
    /// <example>
    /// <code>
    /// services.AddBreezSdkSqlServer(options =>
    /// {
    ///     options.UseSqlServer(connectionString);
    ///     options.EnableSensitiveDataLogging();
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddBreezSdkSqlServer(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (optionsAction == null)
        {
            throw new ArgumentNullException(nameof(optionsAction));
        }

        // Register DbContext with scoped lifetime
        services.AddDbContext<SqlServerPaymentDbContext>(optionsAction, ServiceLifetime.Scoped);

        // Register repository with scoped lifetime
        services.AddScoped<IPaymentRepository, SqlServerPaymentRepository>();

        return services;
    }
}
