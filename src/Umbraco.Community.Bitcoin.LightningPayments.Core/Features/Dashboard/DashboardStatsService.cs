using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Breez;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Features.Dashboard;

/// <summary>
/// Service that provides dashboard statistics and wallet information
/// by aggregating data from the payment database and Breez SDK.
/// </summary>
public class DashboardStatsService : IDashboardStatsService
{
    private readonly PaymentDbContext _dbContext;
    private readonly IBreezSdkService _breezSdkService;
    private readonly IBreezSdkHandleProvider _handleProvider;
    private readonly IBreezSdkWrapper _wrapper;
    private readonly ILogger<DashboardStatsService> _logger;

    public DashboardStatsService(
        PaymentDbContext dbContext,
        IBreezSdkService breezSdkService,
        IBreezSdkHandleProvider handleProvider,
        IBreezSdkWrapper wrapper,
        ILogger<DashboardStatsService> logger)
    {
        _dbContext = dbContext;
        _breezSdkService = breezSdkService;
        _handleProvider = handleProvider;
        _wrapper = wrapper;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DashboardStats> GetDashboardStatsAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching dashboard statistics");

        // Get SDK connection status
        var isConnected = await _breezSdkService.IsConnectedAsync(ct);

        // Get wallet balance from SDK if connected
        long walletBalanceSat = 0;
        if (isConnected)
        {
            try
            {
                var balance = await GetWalletBalanceAsync(ct);
                walletBalanceSat = balance.BalanceSat;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch wallet balance for dashboard stats");
            }
        }

        // Calculate 24 hours ago for daily volume
        var oneDayAgo = DateTimeOffset.UtcNow.AddHours(-24);

        // Query payment statistics from database
        var paidStatuses = new[] { PaymentStatus.Paid };
        var pendingStatuses = new[] { PaymentStatus.Pending };

        // Get total received (all time confirmed payments)
        var totalReceivedSat = await _dbContext.PaymentStates
            .Where(p => paidStatuses.Contains(p.Status))
            .SumAsync(p => (long)p.AmountSat, ct);

        // Get pending count
        var pendingCount = await _dbContext.PaymentStates
            .CountAsync(p => pendingStatuses.Contains(p.Status), ct);

        // Get daily volume (payments confirmed in last 24 hours)
        // Note: PaymentState doesn't have a timestamp field, so we'll use a different approach
        // For now, we'll estimate by looking at recent payments from the SDK
        long dailyVolumeSat = 0;
        DateTimeOffset? lastPaymentAt = null;

        if (isConnected)
        {
            try
            {
                var recentPayments = await _breezSdkService.GetPaymentsAsync(ct);
                var oneDayAgoUnix = oneDayAgo.ToUnixTimeSeconds();

                dailyVolumeSat = recentPayments
                    .Where(p => p.timestamp >= (ulong)oneDayAgoUnix &&
                           p.paymentType == global::Breez.Sdk.Liquid.PaymentType.Receive)
                    .Sum(p => (long)p.amountSat);

                var mostRecent = recentPayments
                    .Where(p => p.paymentType == global::Breez.Sdk.Liquid.PaymentType.Receive)
                    .OrderByDescending(p => p.timestamp)
                    .FirstOrDefault();

                if (mostRecent != null && mostRecent.timestamp > 0)
                {
                    lastPaymentAt = DateTimeOffset.FromUnixTimeSeconds((long)mostRecent.timestamp);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch recent payments from SDK for dashboard stats");
            }
        }

        return new DashboardStats
        {
            TotalReceivedSat = totalReceivedSat,
            PendingCount = pendingCount,
            DailyVolumeSat = dailyVolumeSat,
            WalletBalanceSat = walletBalanceSat,
            SdkConnected = isConnected,
            LastPaymentAt = lastPaymentAt
        };
    }

    /// <inheritdoc />
    public async Task<ChartData> GetPaymentChartDataAsync(ChartPeriod period, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching chart data for period: {Period}", period);

        var dataPoints = new List<ChartDataPoint>();
        var now = DateTimeOffset.UtcNow;

        // Determine time range and granularity based on period
        var (startTime, intervalMinutes, pointCount) = period switch
        {
            ChartPeriod.Day => (now.AddHours(-24), 60, 24),      // 24 hourly points
            ChartPeriod.Week => (now.AddDays(-7), 1440, 7),     // 7 daily points
            ChartPeriod.Month => (now.AddDays(-30), 1440, 30),  // 30 daily points
            _ => (now.AddDays(-7), 1440, 7)
        };

        // Get SDK connection status
        var isConnected = await _breezSdkService.IsConnectedAsync(ct);

        // Try to get payments from SDK
        List<global::Breez.Sdk.Liquid.Payment> payments = new();
        if (isConnected)
        {
            try
            {
                payments = await _breezSdkService.GetPaymentsAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch payments from SDK for chart data");
            }
        }

        // Generate data points for each interval
        for (int i = 0; i < pointCount; i++)
        {
            var intervalStart = startTime.AddMinutes(i * intervalMinutes);
            var intervalEnd = intervalStart.AddMinutes(intervalMinutes);
            var intervalStartUnix = intervalStart.ToUnixTimeSeconds();
            var intervalEndUnix = intervalEnd.ToUnixTimeSeconds();

            var intervalPayments = payments
                .Where(p => p.timestamp >= (ulong)intervalStartUnix &&
                           p.timestamp < (ulong)intervalEndUnix &&
                           p.paymentType == global::Breez.Sdk.Liquid.PaymentType.Receive)
                .ToList();

            dataPoints.Add(new ChartDataPoint
            {
                Timestamp = intervalStart,
                AmountSat = intervalPayments.Sum(p => (long)p.amountSat),
                Count = intervalPayments.Count
            });
        }

        return new ChartData
        {
            Period = period,
            DataPoints = dataPoints
        };
    }

    /// <inheritdoc />
    public async Task<WalletBalance> GetWalletBalanceAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching wallet balance");

        var sdk = await _handleProvider.GetSdkAsync(ct);
        if (sdk == null)
        {
            _logger.LogWarning("SDK not connected, returning zero balance");
            return new WalletBalance
            {
                BalanceSat = 0,
                PendingReceiveSat = 0,
                PendingSendSat = 0
            };
        }

        try
        {
            // Get wallet info from SDK via wrapper
            var walletInfo = await _wrapper.GetInfoAsync(sdk, ct);

            // Access record properties using the primary constructor parameter names (lowercase)
            var balance = GetRecordPropertyValue<ulong>(walletInfo, "balanceSat");
            var pendingReceive = GetRecordPropertyValue<ulong>(walletInfo, "pendingReceiveSat");
            var pendingSend = GetRecordPropertyValue<ulong>(walletInfo, "pendingSendSat");

            return new WalletBalance
            {
                BalanceSat = (long)balance,
                PendingReceiveSat = (long)pendingReceive,
                PendingSendSat = (long)pendingSend
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch wallet balance from SDK");
            return new WalletBalance
            {
                BalanceSat = 0,
                PendingReceiveSat = 0,
                PendingSendSat = 0
            };
        }
    }

    /// <summary>
    /// Helper to get property value from SDK record types that may use lowercase naming.
    /// </summary>
    private static T GetRecordPropertyValue<T>(object record, string propertyName)
    {
        var property = record.GetType().GetProperty(propertyName)
            ?? record.GetType().GetProperty(char.ToUpperInvariant(propertyName[0]) + propertyName.Substring(1));
        return property != null ? (T)property.GetValue(record)! : default!;
    }

    /// <inheritdoc />
    public async Task<WalletLimits> GetWalletLimitsAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching wallet limits");

        var sdk = await _handleProvider.GetSdkAsync(ct);
        if (sdk == null)
        {
            _logger.LogWarning("SDK not connected, returning zero limits");
            return new WalletLimits
            {
                Receive = new LimitRange { MinSat = 0, MaxSat = 0 },
                Send = new LimitRange { MinSat = 0, MaxSat = 0 }
            };
        }

        try
        {
            // Get Lightning limits from SDK
            var limits = await _wrapper.FetchLightningLimitsAsync(sdk, ct);

            return new WalletLimits
            {
                Receive = new LimitRange
                {
                    MinSat = (long)limits.receive.minSat,
                    MaxSat = (long)limits.receive.maxSat
                },
                Send = new LimitRange
                {
                    MinSat = (long)limits.send.minSat,
                    MaxSat = (long)limits.send.maxSat
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch wallet limits from SDK");
            return new WalletLimits
            {
                Receive = new LimitRange { MinSat = 0, MaxSat = 0 },
                Send = new LimitRange { MinSat = 0, MaxSat = 0 }
            };
        }
    }
}
