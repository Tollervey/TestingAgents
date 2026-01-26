using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Configuration;
using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Breez.Sdk.Liquid.Extensions.Core.Domain.Events;
using Breez.Sdk.Liquid.Extensions.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Breez.Sdk.Liquid.Extensions.Samples.ConsoleApp;

/// <summary>
/// Sample console application demonstrating BreezSDK integration.
/// </summary>
public static class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register BreezSDK services in offline mode
                // Use configuration section for test values
                services.AddBreezSdkOffline(options =>
                {
                    context.Configuration.GetSection(BreezSdkOptions.SectionName).Bind(options);
                });

                // Or use AddBreezSdk() for production with real SDK connection
                // services.AddBreezSdk(options =>
                // {
                //     context.Configuration.GetSection(BreezSdkOptions.SectionName).Bind(options);
                // });

                // Register our payment event handler
                services.AddTransient<IPaymentEventHandler, ConsolePaymentEventHandler>();

                // Register the demo service
                services.AddHostedService<PaymentDemoService>();
            })
            .Build();

        await host.RunAsync();
    }
}

/// <summary>
/// Hosted service that demonstrates payment operations.
/// </summary>
public sealed class PaymentDemoService : BackgroundService
{
    private readonly IBreezSdkService _breezService;
    private readonly ILogger<PaymentDemoService> _logger;

    public PaymentDemoService(
        IBreezSdkService breezService,
        ILogger<PaymentDemoService> logger)
    {
        _breezService = breezService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment Demo starting...");

        // Wait for SDK to connect
        await _breezService.ConnectAsync(stoppingToken);
        _logger.LogInformation("SDK connected: {IsConnected}", _breezService.IsConnected);

        // Create an invoice
        _logger.LogInformation("Creating invoice for 1000 sats...");
        var result = await _breezService.CreateInvoiceAsync(
            amountSat: 1000,
            description: "Sample payment from console app",
            cancellationToken: stoppingToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Invoice created successfully!");
            _logger.LogInformation("Payment Hash: {PaymentHash}", result.Value!.PaymentHash);
            _logger.LogInformation("Invoice: {Invoice}", result.Value.Destination);
            _logger.LogInformation("Expires at: {ExpiresAt}", result.Value.ExpiresAt);

            // Check payment status
            var payment = await _breezService.GetPaymentByHashAsync(
                result.Value.PaymentHash,
                stoppingToken);

            if (payment != null)
            {
                _logger.LogInformation("Payment status: {Status}", payment.Status);
            }

            // Get payment history
            var history = await _breezService.GetPaymentHistoryAsync(
                offset: 0,
                limit: 10,
                cancellationToken: stoppingToken);

            _logger.LogInformation("Payment history: {Count} payments", history.Count);
        }
        else
        {
            _logger.LogError("Failed to create invoice: {Error}", result.Error!.Message);
        }

        // Get wallet balance
        var balanceResult = await _breezService.GetBalanceAsync(stoppingToken);
        if (balanceResult.IsSuccess)
        {
            _logger.LogInformation("Wallet balance: {Balance} sats", balanceResult.Value);
        }

        _logger.LogInformation("Demo complete. Press Ctrl+C to exit.");
    }
}

/// <summary>
/// Event handler that logs payment events to the console.
/// </summary>
public sealed class ConsolePaymentEventHandler : IPaymentEventHandler
{
    private readonly ILogger<ConsolePaymentEventHandler> _logger;

    public ConsolePaymentEventHandler(ILogger<ConsolePaymentEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(PaymentEvent @event, CancellationToken cancellationToken)
    {
        switch (@event)
        {
            case InvoiceCreated created:
                _logger.LogInformation(
                    "[EVENT] Invoice created: {Amount} sats, expires {Expiry}",
                    created.AmountSat,
                    created.ExpiresAt);
                break;

            case PaymentReceived received:
                _logger.LogInformation(
                    "[EVENT] Payment received: {Amount} sats",
                    received.AmountSat);
                break;

            case PaymentConfirmed confirmed:
                _logger.LogInformation(
                    "[EVENT] Payment confirmed: {Amount} sats, preimage: {Preimage}...",
                    confirmed.AmountSat,
                    confirmed.Preimage[..Math.Min(8, confirmed.Preimage.Length)]);
                break;

            case PaymentFailed failed:
                _logger.LogWarning(
                    "[EVENT] Payment failed: {Error} (retryable: {Retry})",
                    failed.Reason,
                    failed.IsRetryable);
                break;

            case InvoiceExpired expired:
                _logger.LogInformation(
                    "[EVENT] Invoice expired: {PaymentHash}",
                    expired.PaymentHash);
                break;
        }

        return Task.CompletedTask;
    }
}
