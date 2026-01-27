namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Features.Dashboard;

/// <summary>
/// Chart data for payment volume visualization.
/// </summary>
public record ChartData
{
    /// <summary>
    /// The time period for the chart data.
    /// </summary>
    public ChartPeriod Period { get; init; }

    /// <summary>
    /// Data points for the chart.
    /// </summary>
    public IReadOnlyList<ChartDataPoint> DataPoints { get; init; } = Array.Empty<ChartDataPoint>();
}

/// <summary>
/// A single data point in the payment chart.
/// </summary>
public record ChartDataPoint
{
    /// <summary>
    /// The timestamp for this data point.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Total amount in satoshis for this time period.
    /// </summary>
    public long AmountSat { get; init; }

    /// <summary>
    /// Number of payments in this time period.
    /// </summary>
    public int Count { get; init; }
}

/// <summary>
/// Time period options for chart data.
/// </summary>
public enum ChartPeriod
{
    /// <summary>
    /// Last 24 hours with hourly granularity.
    /// </summary>
    Day,

    /// <summary>
    /// Last 7 days with daily granularity.
    /// </summary>
    Week,

    /// <summary>
    /// Last 30 days with daily granularity.
    /// </summary>
    Month
}
