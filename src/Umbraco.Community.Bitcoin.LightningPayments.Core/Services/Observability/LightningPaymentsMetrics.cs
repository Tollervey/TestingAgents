using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Observability;

/// <summary>
/// Provides OpenTelemetry metrics for Umbraco Lightning Payments operations.
/// </summary>
public static class LightningPaymentsMetrics
{
    /// <summary>
    /// The meter name used for all Lightning Payments metrics.
    /// </summary>
    public const string MeterName = "Umbraco.Community.LightningPayments";

    /// <summary>
    /// The meter instance used to create all metrics instruments.
    /// </summary>
    public static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> _notificationSentCounter = Meter.CreateCounter<long>(
        "lightning.notification.sent",
        description: "Number of notifications sent");

    private static readonly Counter<long> _refundInitiatedCounter = Meter.CreateCounter<long>(
        "lightning.refund.initiated",
        description: "Number of refunds initiated");

    private static readonly Counter<long> _refundCompletedCounter = Meter.CreateCounter<long>(
        "lightning.refund.completed",
        description: "Number of refunds completed");

    private static readonly Counter<long> _exchangeRateFetchedCounter = Meter.CreateCounter<long>(
        "lightning.exchange_rate.fetched",
        description: "Number of exchange rate fetches");

    private static readonly Counter<long> _dashboardAccessedCounter = Meter.CreateCounter<long>(
        "lightning.dashboard.accessed",
        description: "Number of dashboard accesses");

    private static readonly Histogram<double> _apiRequestDurationHistogram = Meter.CreateHistogram<double>(
        "lightning.api.request.duration",
        unit: "ms",
        description: "Duration of API requests in milliseconds");

    /// <summary>
    /// Records that a notification was sent.
    /// </summary>
    public static void RecordNotificationSent(string type, string status)
    {
        var tags = new TagList
        {
            { "type", type },
            { "status", status }
        };
        _notificationSentCounter.Add(1, tags);
    }

    /// <summary>
    /// Records that a refund was initiated.
    /// </summary>
    public static void RecordRefundInitiated()
    {
        _refundInitiatedCounter.Add(1);
    }

    /// <summary>
    /// Records that a refund was completed.
    /// </summary>
    public static void RecordRefundCompleted(string status)
    {
        var tags = new TagList { { "status", status } };
        _refundCompletedCounter.Add(1, tags);
    }

    /// <summary>
    /// Records that an exchange rate was fetched.
    /// </summary>
    public static void RecordExchangeRateFetched(string currency, string source)
    {
        var tags = new TagList
        {
            { "currency", currency },
            { "source", source }
        };
        _exchangeRateFetchedCounter.Add(1, tags);
    }

    /// <summary>
    /// Records that the dashboard was accessed.
    /// </summary>
    public static void RecordDashboardAccessed()
    {
        _dashboardAccessedCounter.Add(1);
    }

    /// <summary>
    /// Records the duration of an API request.
    /// </summary>
    public static void RecordApiRequestDuration(string controller, string action, double durationMs)
    {
        var tags = new TagList
        {
            { "controller", controller },
            { "action", action }
        };
        _apiRequestDurationHistogram.Record(durationMs, tags);
    }
}
