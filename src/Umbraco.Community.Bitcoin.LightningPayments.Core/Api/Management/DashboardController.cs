using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Base;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Features.Dashboard;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Management;

/// <summary>
/// Dashboard API controller providing payment statistics and wallet information.
/// </summary>
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Lightning Payments Dashboard")]
public class DashboardController : OurUmbracoBitcoinLightningPaymentsApiControllerBase
{
    private readonly IDashboardStatsService _dashboardStatsService;

    public DashboardController(IDashboardStatsService dashboardStatsService)
    {
        _dashboardStatsService = dashboardStatsService;
    }

    /// <summary>
    /// Get aggregated dashboard statistics.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dashboard statistics including payment totals and SDK status.</returns>
    [HttpGet("dashboard/stats")]
    [ProducesResponseType<DashboardStatsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DashboardStatsResponse>> GetDashboardStats(CancellationToken ct)
    {
        var stats = await _dashboardStatsService.GetDashboardStatsAsync(ct);

        return Ok(new DashboardStatsResponse
        {
            TotalReceivedSat = stats.TotalReceivedSat,
            PendingCount = stats.PendingCount,
            DailyVolumeSat = stats.DailyVolumeSat,
            WalletBalanceSat = stats.WalletBalanceSat,
            SdkConnected = stats.SdkConnected,
            LastPaymentAt = stats.LastPaymentAt
        });
    }

    /// <summary>
    /// Get payment volume chart data.
    /// </summary>
    /// <param name="period">Time period (day, week, month). Defaults to week.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Chart data points for the specified period.</returns>
    [HttpGet("dashboard/chart")]
    [ProducesResponseType<ChartDataResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChartDataResponse>> GetPaymentChart(
        [FromQuery] string period = "week",
        CancellationToken ct = default)
    {
        if (!TryParseChartPeriod(period, out var chartPeriod))
        {
            return BadRequest(new { error = "Invalid period. Must be 'day', 'week', or 'month'." });
        }

        var data = await _dashboardStatsService.GetPaymentChartDataAsync(chartPeriod, ct);

        return Ok(new ChartDataResponse
        {
            Period = period,
            DataPoints = data.DataPoints.Select(dp => new ChartDataPointResponse
            {
                Timestamp = dp.Timestamp,
                AmountSat = dp.AmountSat,
                Count = dp.Count
            }).ToList()
        });
    }

    /// <summary>
    /// Get current wallet balance.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Wallet balance information.</returns>
    [HttpGet("wallet/balance")]
    [ProducesResponseType<WalletBalanceResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<WalletBalanceResponse>> GetWalletBalance(CancellationToken ct)
    {
        var balance = await _dashboardStatsService.GetWalletBalanceAsync(ct);

        return Ok(new WalletBalanceResponse
        {
            BalanceSat = balance.BalanceSat,
            PendingReceiveSat = balance.PendingReceiveSat,
            PendingSendSat = balance.PendingSendSat
        });
    }

    /// <summary>
    /// Get wallet receive and send limits.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Wallet limits.</returns>
    [HttpGet("wallet/limits")]
    [ProducesResponseType<WalletLimitsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<WalletLimitsResponse>> GetWalletLimits(CancellationToken ct)
    {
        var limits = await _dashboardStatsService.GetWalletLimitsAsync(ct);

        return Ok(new WalletLimitsResponse
        {
            Receive = new LimitRangeResponse
            {
                MinSat = limits.Receive.MinSat,
                MaxSat = limits.Receive.MaxSat
            },
            Send = new LimitRangeResponse
            {
                MinSat = limits.Send.MinSat,
                MaxSat = limits.Send.MaxSat
            }
        });
    }

    private static bool TryParseChartPeriod(string period, out ChartPeriod chartPeriod)
    {
        chartPeriod = period.ToLowerInvariant() switch
        {
            "day" => ChartPeriod.Day,
            "week" => ChartPeriod.Week,
            "month" => ChartPeriod.Month,
            _ => ChartPeriod.Week
        };

        return period.ToLowerInvariant() is "day" or "week" or "month";
    }
}

/// <summary>
/// Dashboard statistics response.
/// </summary>
public record DashboardStatsResponse
{
    public long TotalReceivedSat { get; init; }
    public int PendingCount { get; init; }
    public long DailyVolumeSat { get; init; }
    public long WalletBalanceSat { get; init; }
    public bool SdkConnected { get; init; }
    public DateTimeOffset? LastPaymentAt { get; init; }
}

/// <summary>
/// Chart data response.
/// </summary>
public record ChartDataResponse
{
    public string Period { get; init; } = string.Empty;
    public IReadOnlyList<ChartDataPointResponse> DataPoints { get; init; } = Array.Empty<ChartDataPointResponse>();
}

/// <summary>
/// Chart data point response.
/// </summary>
public record ChartDataPointResponse
{
    public DateTimeOffset Timestamp { get; init; }
    public long AmountSat { get; init; }
    public int Count { get; init; }
}

/// <summary>
/// Wallet balance response.
/// </summary>
public record WalletBalanceResponse
{
    public long BalanceSat { get; init; }
    public long PendingReceiveSat { get; init; }
    public long PendingSendSat { get; init; }
}

/// <summary>
/// Wallet limits response.
/// </summary>
public record WalletLimitsResponse
{
    public LimitRangeResponse Receive { get; init; } = new();
    public LimitRangeResponse Send { get; init; } = new();
}

/// <summary>
/// Limit range response.
/// </summary>
public record LimitRangeResponse
{
    public long MinSat { get; init; }
    public long MaxSat { get; init; }
}
