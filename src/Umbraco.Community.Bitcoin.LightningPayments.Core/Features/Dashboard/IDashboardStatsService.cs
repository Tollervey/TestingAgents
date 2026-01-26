namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Features.Dashboard;

/// <summary>
/// Service for retrieving dashboard statistics and wallet information.
/// </summary>
public interface IDashboardStatsService
{
    /// <summary>
    /// Gets aggregated dashboard statistics including payment totals and SDK status.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dashboard statistics.</returns>
    Task<DashboardStats> GetDashboardStatsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets payment chart data for the specified time period.
    /// </summary>
    /// <param name="period">The chart period (day, week, month).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Chart data with data points.</returns>
    Task<ChartData> GetPaymentChartDataAsync(ChartPeriod period, CancellationToken ct = default);

    /// <summary>
    /// Gets the current wallet balance.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Wallet balance information.</returns>
    Task<WalletBalance> GetWalletBalanceAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the wallet receive and send limits.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Wallet limits.</returns>
    Task<WalletLimits> GetWalletLimitsAsync(CancellationToken ct = default);
}
