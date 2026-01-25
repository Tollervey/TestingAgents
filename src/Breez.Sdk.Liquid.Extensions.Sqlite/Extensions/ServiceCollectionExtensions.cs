using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Sqlite.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Breez.Sdk.Liquid.Extensions.Sqlite.Extensions;

/// <summary>
/// Extension methods for registering SQLite persistence services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers SQLite payment persistence services with the specified connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The SQLite connection string (e.g., "Data Source=payments.db").</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="connectionString"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> is empty or whitespace.</exception>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// </para>
    /// <list type="bullet">
    ///   <item><see cref="SqlitePaymentDbContext"/> - Scoped lifetime</item>
    ///   <item><see cref="IPaymentRepository"/> - Scoped lifetime</item>
    /// </list>
    /// <para>
    /// Example usage:
    /// </para>
    /// <code>
    /// services.AddBreezSdkSqlite("Data Source=payments.db");
    /// </code>
    /// </remarks>
    public static IServiceCollection AddBreezSdkSqlite(
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

        return services.AddBreezSdkSqlite(options =>
        {
            options.UseSqlite(connectionString);
        });
    }

    /// <summary>
    /// Registers SQLite payment persistence services with a custom DbContext configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure the DbContext options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configureOptions"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// </para>
    /// <list type="bullet">
    ///   <item><see cref="SqlitePaymentDbContext"/> - Scoped lifetime</item>
    ///   <item><see cref="IPaymentRepository"/> - Scoped lifetime</item>
    /// </list>
    /// <para>
    /// Example usage with custom options:
    /// </para>
    /// <code>
    /// services.AddBreezSdkSqlite(options =>
    /// {
    ///     options.UseSqlite("Data Source=payments.db");
    ///     options.EnableSensitiveDataLogging(); // Development only
    /// });
    /// </code>
    /// </remarks>
    public static IServiceCollection AddBreezSdkSqlite(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureOptions)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configureOptions == null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        // Register DbContext with scoped lifetime
        services.AddDbContext<SqlitePaymentDbContext>(configureOptions, ServiceLifetime.Scoped);

        // Register repository implementation with scoped lifetime
        services.AddScoped<IPaymentRepository, SqlitePaymentRepository>();

        return services;
    }
}
