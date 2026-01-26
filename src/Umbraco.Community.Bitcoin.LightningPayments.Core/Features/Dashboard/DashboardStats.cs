namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Features.Dashboard;

/// <summary>
/// Aggregated statistics for the Lightning Payments dashboard.
/// </summary>
public record DashboardStats
{
    /// <summary>
    /// Total satoshis received all time (from confirmed payments).
    /// </summary>
    public long TotalReceivedSat { get; init; }

    /// <summary>
    /// Number of pending payments awaiting confirmation.
    /// </summary>
    public int PendingCount { get; init; }

    /// <summary>
    /// Satoshis received in the last 24 hours.
    /// </summary>
    public long DailyVolumeSat { get; init; }

    /// <summary>
    /// Current wallet balance in satoshis.
    /// </summary>
    public long WalletBalanceSat { get; init; }

    /// <summary>
    /// Whether the BreezSDK is connected and operational.
    /// </summary>
    public bool SdkConnected { get; init; }

    /// <summary>
    /// Timestamp of the most recent payment (null if no payments).
    /// </summary>
    public DateTimeOffset? LastPaymentAt { get; init; }
}
