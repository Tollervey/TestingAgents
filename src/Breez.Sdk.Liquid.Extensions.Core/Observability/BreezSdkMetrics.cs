using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Breez.Sdk.Liquid.Extensions.Core.Observability;

/// <summary>
/// Provides OpenTelemetry metrics instrumentation for BreezSDK operations.
/// Tracks invoices, payments, operation durations, and connection states.
/// </summary>
/// <remarks>
/// This class exposes metrics for:
/// - Invoice creation counters (breez.invoice.created)
/// - Payment received counters (breez.payment.received)
/// - Payment failure counters (breez.payment.failed)
/// - Operation duration histograms (breez.operation.duration)
/// - Connection state gauges (breez.sdk.connected)
///
/// All methods are thread-safe and designed for concurrent access.
/// </remarks>
public static class BreezSdkMetrics
{
    /// <summary>
    /// The meter name used for all BreezSDK metrics.
    /// </summary>
    public const string MeterName = "Breez.Sdk.Liquid.Extensions";

    /// <summary>
    /// The meter instance used to create all metrics instruments.
    /// </summary>
    public static readonly Meter Meter = new(MeterName);

    // Thread-safe dictionary to track connection states per network
    private static readonly ConcurrentDictionary<string, int> _connectionStates = new();

    // Metric instruments
    private static readonly Counter<long> _invoiceCreatedCounter = Meter.CreateCounter<long>(
        "breez.invoice.created",
        description: "Number of invoices created");

    private static readonly Counter<long> _paymentReceivedCounter = Meter.CreateCounter<long>(
        "breez.payment.received",
        description: "Number of payments received");

    private static readonly Counter<long> _paymentFailedCounter = Meter.CreateCounter<long>(
        "breez.payment.failed",
        description: "Number of failed payments");

    private static readonly Histogram<double> _operationDurationHistogram = Meter.CreateHistogram<double>(
        "breez.operation.duration",
        unit: "ms",
        description: "Duration of SDK operations in milliseconds");

    private static readonly ObservableGauge<int> _connectionStateGauge = Meter.CreateObservableGauge(
        "breez.sdk.connected",
        observeValues: ObserveConnectionStates,
        description: "Connection state (1=connected, 0=disconnected)");

    /// <summary>
    /// Records that an invoice was created.
    /// </summary>
    /// <param name="network">The network the invoice was created on (e.g., "mainnet", "testnet").</param>
    /// <param name="status">The status of the invoice creation (e.g., "success", "failure").</param>
    public static void RecordInvoiceCreated(string network, string status)
    {
        var tags = new TagList
        {
            { "network", network ?? string.Empty },
            { "status", status ?? string.Empty }
        };
        _invoiceCreatedCounter.Add(1, tags);
    }

    /// <summary>
    /// Records that a payment was received.
    /// </summary>
    /// <param name="network">The network the payment was received on (e.g., "mainnet", "testnet").</param>
    /// <param name="status">The status of the payment (e.g., "success", "failure").</param>
    public static void RecordPaymentReceived(string network, string status)
    {
        var tags = new TagList
        {
            { "network", network ?? string.Empty },
            { "status", status ?? string.Empty }
        };
        _paymentReceivedCounter.Add(1, tags);
    }

    /// <summary>
    /// Records that a payment failed.
    /// </summary>
    /// <param name="network">The network the payment was attempted on (e.g., "mainnet", "testnet").</param>
    /// <param name="errorType">The type of error that caused the failure (e.g., "insufficient_funds", "timeout").</param>
    public static void RecordPaymentFailed(string network, string errorType)
    {
        var tags = new TagList
        {
            { "network", network ?? string.Empty },
            { "error_type", errorType ?? string.Empty }
        };
        _paymentFailedCounter.Add(1, tags);
    }

    /// <summary>
    /// Records the duration of an SDK operation.
    /// </summary>
    /// <param name="operationType">The type of operation (e.g., "connect", "prepare_receive", "send_payment").</param>
    /// <param name="network">The network the operation was performed on (e.g., "mainnet", "testnet").</param>
    /// <param name="durationMs">The duration of the operation in milliseconds.</param>
    public static void RecordOperationDuration(string operationType, string network, double durationMs)
    {
        var tags = new TagList
        {
            { "operation_type", operationType ?? string.Empty },
            { "network", network ?? string.Empty }
        };
        _operationDurationHistogram.Record(durationMs, tags);
    }

    /// <summary>
    /// Sets the connection state for a specific network.
    /// </summary>
    /// <param name="isConnected">True if connected, false if disconnected.</param>
    /// <param name="network">The network identifier (e.g., "mainnet", "testnet").</param>
    public static void SetConnectionState(bool isConnected, string network)
    {
        var networkKey = network ?? string.Empty;
        var stateValue = isConnected ? 1 : 0;
        _connectionStates.AddOrUpdate(networkKey, stateValue, (_, _) => stateValue);
    }

    /// <summary>
    /// Observable callback that reports current connection states for all networks.
    /// </summary>
    private static IEnumerable<Measurement<int>> ObserveConnectionStates()
    {
        foreach (var kvp in _connectionStates)
        {
            var tags = new TagList
            {
                { "network", kvp.Key }
            };
            yield return new Measurement<int>(kvp.Value, tags);
        }
    }
}
