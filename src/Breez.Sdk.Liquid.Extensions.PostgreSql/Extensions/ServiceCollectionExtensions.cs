using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.PostgreSql.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Breez.Sdk.Liquid.Extensions.PostgreSql.Extensions;

/// <summary>
/// Extension methods for configuring Breez SDK PostgreSQL persistence services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Breez SDK PostgreSQL persistence services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="connectionString"/> is null.</exception>
    /// <remarks>
    /// This method registers:
    /// <list type="bullet">
    ///   <item><see cref="PostgreSqlPaymentDbContext"/> as a scoped service</item>
    ///   <item><see cref="IPaymentRepository"/> implementation as a scoped service</item>
    /// </list>
    /// <para>
    /// Example usage:
    /// </para>
    /// <code>
    /// services.AddBreezSdkPostgreSql("Host=localhost;Database=breez;Username=user;Password=pass");
    /// </code>
    /// </remarks>
    public static IServiceCollection AddBreezSdkPostgreSql(
        this IServiceCollection services,
        string connectionString)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString));
        }

        return AddBreezSdkPostgreSql(services, options =>
        {
            options.UseNpgsql(connectionString);
        });
    }

    /// <summary>
    /// Adds Breez SDK PostgreSQL persistence services to the dependency injection container with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="optionsAction">An action to configure the DbContext options.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="optionsAction"/> is null.</exception>
    /// <remarks>
    /// This overload allows for advanced configuration scenarios such as:
    /// <list type="bullet">
    ///   <item>Connection pooling configuration</item>
    ///   <item>Retry policies</item>
    ///   <item>Command timeouts</item>
    ///   <item>Migration assembly configuration</item>
    /// </list>
    /// <para>
    /// Example usage:
    /// </para>
    /// <code>
    /// services.AddBreezSdkPostgreSql(options =>
    /// {
    ///     options.UseNpgsql(connectionString, npgsqlOptions =>
    ///     {
    ///         npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
    ///         npgsqlOptions.CommandTimeout(30);
    ///     });
    /// });
    /// </code>
    /// </remarks>
    public static IServiceCollection AddBreezSdkPostgreSql(
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
        services.AddDbContext<PostgreSqlPaymentDbContext>(optionsAction, ServiceLifetime.Scoped);

        // Register repository implementation
        services.AddScoped<IPaymentRepository, PostgreSqlPaymentRepository>();

        return services;
    }
}
