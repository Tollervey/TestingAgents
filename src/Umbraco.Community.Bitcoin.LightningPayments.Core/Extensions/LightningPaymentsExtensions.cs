using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Features.Realtime.Services;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Infrastructure;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Breez;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Invoice;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Payment;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.RateLimiting;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Runtime;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Features.Dashboard;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Bolt12;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Notification;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Refund;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.ExchangeRate;
using System.Threading.RateLimiting;
using Umbraco.Cms.Core.DependencyInjection;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to register Lightning Payments services and related features with Umbraco.
/// </summary>
public static class LightningPaymentsExtensions
{
    public static IUmbracoBuilder AddLightningPayments(this IUmbracoBuilder builder)
    {
        // Ensure logging is available when this registration runs (tests may not have added logging).
        builder.Services.AddLogging();

        // STEP 1: This is the first log. If you don't see this, the consuming app is not calling this method.
        var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>().CreateLogger("LightningPayments.Startup");
        logger.LogInformation("--- Step 1: AddLightningPayments() called. Assembly is now loaded. ---");

        // Bind the "LightningPayments" section of appsettings to the settings model
        builder.Services.AddOptions<LightningPaymentsSettings>()
            .Bind(builder.Config.GetSection(LightningPaymentsSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddSingleton<IValidateOptions<LightningPaymentsSettings>, LightningPaymentsSettingsValidator>();

        // Bind notification options
        builder.Services.AddOptions<NotificationOptions>()
            .Bind(builder.Config.GetSection(NotificationOptions.SectionName))
            .ValidateDataAnnotations();

        // Bind exchange rate options
        builder.Services.AddOptions<ExchangeRateOptions>()
            .Bind(builder.Config.GetSection(ExchangeRateOptions.SectionName))
            .ValidateDataAnnotations();

        // Register FluentValidation validators from this assembly
        builder.Services.AddValidatorsFromAssemblyContaining<LightningPaymentsSettings>(ServiceLifetime.Scoped);

        // Bind rate limiting options (optional)
        var rlSection = builder.Config.GetSection($"{LightningPaymentsSettings.SectionName}:RateLimiting");
        var rlOptions = rlSection.Get<RateLimitingOptions>() ?? new RateLimitingOptions();
        builder.Services.Configure<RateLimitingOptions>(rlSection);

        // If consumer wants to use ASP.NET Core RateLimiting middleware, register it according to options
        if (rlOptions.Enabled && rlOptions.UseAspNetRateLimiter)
        {
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = rlOptions.RejectionStatusCode;

                options.AddPolicy("InvoiceGeneration", context =>
                {
                    string partitionKey = rlOptions.PartitionByIp
                        ? (context.Connection.RemoteIpAddress?.ToString() ?? "unknown")
                        : "default";

                    return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rlOptions.PermitLimit,
                        Window = TimeSpan.FromSeconds(Math.Max(1, rlOptions.WindowSeconds)),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = rlOptions.QueueLimit
                    });
                });
            });
        }

        // Default runtime mode marker (online by default)
        builder.Services.AddSingleton<ILightningPaymentsRuntimeMode>(_ => new LightningPaymentsRuntimeMode(isOffline: false));

        // NOTE: Application Insights is intentionally NOT registered here automatically. Consumers should opt-in by calling
        // AddLightningPaymentsApplicationInsights on the IUmbracoBuilder if they want AI wired up for this library.

        // Register services
        builder.Services.AddDbContext<PaymentDbContext>((sp, options) =>
        {
            var settings = sp.GetRequiredService<IOptions<LightningPaymentsSettings>>().Value;
            var env = sp.GetRequiredService<IHostEnvironment>();
            var dbLogger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("LightningPayments");

            // Respect offline/in-memory mode at resolution time
            var runtimeMode = sp.GetRequiredService<ILightningPaymentsRuntimeMode>();
            var offlineOpts = sp.GetService<IOptions<OfflineLightningPaymentsOptions>>()?.Value;

            if (runtimeMode.IsOffline && offlineOpts?.UseInMemoryStateService == true)
            {
                options.UseInMemoryDatabase("LightningPayments_InMemory");
                dbLogger.LogInformation("LightningPayments running in OFFLINE mode (in-memory). Skipping SQLite registration.");
                return;
            }

            // Normalize SQLite connection string
            var resolved = ConnectionStringResolver.Resolve(settings.ConnectionString, env, dbLogger);
            options.UseSqlite(resolved);
        });

        builder.Services.AddScoped<IPaymentStateService, PersistentPaymentStateService>();
        // Email removed by default to simplify setup: no IEmailService registration.

        builder.Services.AddSingleton<IBreezSdkWrapper, BreezSdkWrapper>();
        builder.Services.AddSingleton<IBreezSdkService, BreezSdkService>();
        builder.Services.AddSingleton<IBreezSdkHandleProvider>(sp => (IBreezSdkHandleProvider)sp.GetRequiredService<IBreezSdkService>());
        builder.Services.AddScoped<IBreezPaymentsFacade, BreezPaymentsFacade>();
        builder.Services.AddSingleton<BreezEventProcessor>();
        builder.Services.AddSingleton<IBreezEventProcessor>(sp => sp.GetRequiredService<BreezEventProcessor>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<BreezEventProcessor>());

        // Keep initializer registration, but it will self-skip when running offline with in-memory state.
        builder.Services.AddHostedService<PaymentDbInitializer>();

        builder.Services.AddMemoryCache();
        builder.Services.AddScoped<IRuntimeSettingsService, RuntimeSettingsService>();

        builder.Services.AddSingleton<SseHub>();
        builder.Services.AddSingleton<IRateLimiter, MemoryRateLimiter>();
        builder.Services.AddScoped<IInvoiceHelper, InvoiceHelper>();

        // Dashboard services
        builder.Services.AddScoped<IDashboardStatsService, DashboardStatsService>();

        // Bolt12 offer services
        builder.Services.AddScoped<IBolt12OfferService, Bolt12OfferService>();

        // Notification services
        builder.Services.AddScoped<IEmailService, SmtpEmailService>();
        builder.Services.AddScoped<INotificationService, NotificationService>();
        builder.Services.AddScoped<INotificationHandler, EmailNotificationHandler>();
        builder.Services.AddScoped<INotificationHandler, WebhookNotificationHandler>();
        builder.Services.AddHttpClient<WebhookNotificationHandler>();
        builder.Services.AddHostedService<NotificationRetryBackgroundService>();

        // Refund services
        builder.Services.AddScoped<IRefundService, RefundService>();

        // Exchange rate services (multi-currency display)
        builder.Services.AddHttpClient<CoinGeckoClient>();
        builder.Services.AddScoped<ICoinGeckoClient, CoinGeckoClient>();
        builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();

        builder.Services.AddHealthChecks().AddCheck<BreezSdkHealthCheck>("breez");

        logger.LogInformation("--- Step 2: AddLightningPayments() completed service registration. ---");

        return builder;
    }

    // NOTE: ApplicationInsights methods moved to main package project
    // since Microsoft.ApplicationInsights.AspNetCore is a dependency there

    // Publicly exposed: enable offline mode for development/testing.
    public static IUmbracoBuilder UseLightningPaymentsOffline(this IUmbracoBuilder builder, Action<OfflineLightningPaymentsOptions>? configure = null)
    {
        var options = new OfflineLightningPaymentsOptions();
        configure?.Invoke(options);

        builder.Services.AddSingleton<ILightningPaymentsRuntimeMode>(_ => new LightningPaymentsRuntimeMode(isOffline: true));
        builder.Services.AddSingleton<Microsoft.Extensions.Options.IOptions<OfflineLightningPaymentsOptions>>(
            _ => Microsoft.Extensions.Options.Options.Create(options)
        );

        builder.Services.AddSingleton<IBreezSdkService, OfflineBreezSdkService>();
        builder.Services.AddSingleton<IBreezSdkHandleProvider>(sp => (IBreezSdkHandleProvider)sp.GetRequiredService<IBreezSdkService>());
        builder.Services.AddScoped<IBreezPaymentsFacade, BreezPaymentsFacade>();

        if (options.UseInMemoryStateService)
        {
            builder.Services.AddScoped<IPaymentStateService, InMemoryPaymentStateService>();
        }

        return builder;
    }
}


