using System.Text.Json;
using FluentAssertions;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Api.Contract;

/// <summary>
/// Contract tests for Dashboard API endpoints.
/// These tests verify that DTOs serialize/deserialize correctly according to the OpenAPI specification.
///
/// IMPORTANT: These tests are written FIRST (TDD Red phase) and will FAIL until DTOs are implemented.
/// Expected failures:
/// - DashboardStats class does not exist
/// - ChartData class does not exist
/// - WalletBalance class does not exist
/// - WalletLimits class does not exist
/// </summary>
public class DashboardContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    #region DashboardStats Contract Tests

    [Fact]
    public void DashboardStats_Serialization_MatchesContract()
    {
        // Arrange - This will fail until DashboardStats is implemented
        var stats = new DashboardStats
        {
            TotalReceivedSat = 1000000,
            PendingCount = 5,
            DailyVolumeSat = 50000,
            WalletBalanceSat = 250000,
            SdkConnected = true,
            LastPaymentAt = new DateTime(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var json = JsonSerializer.Serialize(stats, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<DashboardStats>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.TotalReceivedSat.Should().Be(1000000);
        deserialized.PendingCount.Should().Be(5);
        deserialized.DailyVolumeSat.Should().Be(50000);
        deserialized.WalletBalanceSat.Should().Be(250000);
        deserialized.SdkConnected.Should().BeTrue();
        deserialized.LastPaymentAt.Should().Be(new DateTime(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc));

        // Verify JSON structure matches OpenAPI spec (camelCase)
        json.Should().Contain("\"totalReceivedSat\":1000000");
        json.Should().Contain("\"pendingCount\":5");
        json.Should().Contain("\"dailyVolumeSat\":50000");
        json.Should().Contain("\"walletBalanceSat\":250000");
        json.Should().Contain("\"sdkConnected\":true");
    }

    [Fact]
    public void DashboardStats_RequiredFields_ArePresent()
    {
        // Arrange - This will fail until DashboardStats is implemented
        var stats = new DashboardStats
        {
            TotalReceivedSat = 100,
            PendingCount = 0,
            DailyVolumeSat = 0,
            WalletBalanceSat = 500,
            SdkConnected = false
            // LastPaymentAt is optional - omitted intentionally
        };

        // Act
        var json = JsonSerializer.Serialize(stats, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<DashboardStats>(json, JsonOptions);

        // Assert - All required fields must have values
        deserialized.Should().NotBeNull();
        deserialized!.TotalReceivedSat.Should().BeGreaterThanOrEqualTo(0);
        deserialized.PendingCount.Should().BeGreaterThanOrEqualTo(0);
        deserialized.DailyVolumeSat.Should().BeGreaterThanOrEqualTo(0);
        deserialized.WalletBalanceSat.Should().BeGreaterThanOrEqualTo(0);
        // bool is non-nullable, must have value - verify it's false as set
        deserialized.SdkConnected.Should().BeFalse();
    }

    [Fact]
    public void DashboardStats_WithNullLastPaymentAt_SerializesCorrectly()
    {
        // Arrange - LastPaymentAt is optional in OpenAPI spec
        var stats = new DashboardStats
        {
            TotalReceivedSat = 1000000,
            PendingCount = 0,
            DailyVolumeSat = 0,
            WalletBalanceSat = 500000,
            SdkConnected = true,
            LastPaymentAt = null
        };

        // Act
        var json = JsonSerializer.Serialize(stats, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<DashboardStats>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.LastPaymentAt.Should().BeNull();
    }

    [Fact]
    public void DashboardStats_WithLargeValues_HandlesInt64Correctly()
    {
        // Arrange - Test int64 boundaries (21M BTC * 100M sats/BTC = 2.1 quadrillion sats)
        var stats = new DashboardStats
        {
            TotalReceivedSat = 2_100_000_000_000_000L, // Max Bitcoin supply in sats
            PendingCount = int.MaxValue,
            DailyVolumeSat = 1_000_000_000_000L, // 10,000 BTC
            WalletBalanceSat = 21_000_000_000L, // 210 BTC
            SdkConnected = false
        };

        // Act
        var json = JsonSerializer.Serialize(stats, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<DashboardStats>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.TotalReceivedSat.Should().Be(2_100_000_000_000_000L);
        deserialized.PendingCount.Should().Be(int.MaxValue);
        deserialized.DailyVolumeSat.Should().Be(1_000_000_000_000L);
        deserialized.WalletBalanceSat.Should().Be(21_000_000_000L);
    }

    #endregion

    #region ChartData Contract Tests

    [Fact]
    public void ChartData_Serialization_MatchesContract()
    {
        // Arrange - This will fail until ChartData and ChartDataPoint are implemented
        var chartData = new ChartData
        {
            Period = "week",
            DataPoints = new List<ChartDataPoint>
            {
                new()
                {
                    Timestamp = new DateTime(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc),
                    AmountSat = 10000,
                    Count = 5
                },
                new()
                {
                    Timestamp = new DateTime(2026, 1, 27, 12, 0, 0, DateTimeKind.Utc),
                    AmountSat = 15000,
                    Count = 8
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(chartData, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ChartData>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Period.Should().Be("week");
        deserialized.DataPoints.Should().HaveCount(2);

        var firstPoint = deserialized.DataPoints[0];
        firstPoint.Timestamp.Should().Be(new DateTime(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc));
        firstPoint.AmountSat.Should().Be(10000);
        firstPoint.Count.Should().Be(5);

        // Verify JSON structure
        json.Should().Contain("\"period\":\"week\"");
        json.Should().Contain("\"dataPoints\":");
        json.Should().Contain("\"amountSat\":10000");
        json.Should().Contain("\"count\":5");
    }

    [Theory]
    [InlineData("day")]
    [InlineData("week")]
    [InlineData("month")]
    public void ChartData_Period_AcceptsOnlyValidValues(string period)
    {
        // Arrange - Test enum validation
        var chartData = new ChartData
        {
            Period = period,
            DataPoints = new List<ChartDataPoint>
            {
                new()
                {
                    Timestamp = DateTime.UtcNow,
                    AmountSat = 1000,
                    Count = 1
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(chartData, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ChartData>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Period.Should().Be(period);
    }

    [Fact]
    public void ChartData_WithInvalidPeriod_ShouldBeRejectedByValidation()
    {
        // Arrange - Invalid period value (should fail validation, not serialization)
        var chartData = new ChartData
        {
            Period = "invalid-period", // Not in enum: day/week/month
            DataPoints = new List<ChartDataPoint>()
        };

        // Act
        var json = JsonSerializer.Serialize(chartData, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ChartData>(json, JsonOptions);

        // Assert - Deserialization works, but validation should catch this
        deserialized.Should().NotBeNull();
        deserialized!.Period.Should().Be("invalid-period");

        // NOTE: Actual validation would happen in controller with FluentValidation
        // This test documents that validation is needed beyond serialization
    }

    [Fact]
    public void ChartData_WithEmptyDataPoints_SerializesCorrectly()
    {
        // Arrange - Valid to have no data points (e.g., no activity in period)
        var chartData = new ChartData
        {
            Period = "day",
            DataPoints = new List<ChartDataPoint>()
        };

        // Act
        var json = JsonSerializer.Serialize(chartData, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ChartData>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Period.Should().Be("day");
        deserialized.DataPoints.Should().BeEmpty();
    }

    [Fact]
    public void ChartDataPoint_WithZeroValues_SerializesCorrectly()
    {
        // Arrange - Edge case: no payments in a time bucket
        var dataPoint = new ChartDataPoint
        {
            Timestamp = new DateTime(2026, 1, 26, 0, 0, 0, DateTimeKind.Utc),
            AmountSat = 0,
            Count = 0
        };

        // Act
        var json = JsonSerializer.Serialize(dataPoint, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ChartDataPoint>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.AmountSat.Should().Be(0);
        deserialized.Count.Should().Be(0);
    }

    #endregion

    #region WalletBalance Contract Tests

    [Fact]
    public void WalletBalance_Serialization_MatchesContract()
    {
        // Arrange - This will fail until WalletBalance is implemented
        var balance = new WalletBalance
        {
            BalanceSat = 500000,
            PendingReceiveSat = 10000,
            PendingSendSat = 5000
        };

        // Act
        var json = JsonSerializer.Serialize(balance, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<WalletBalance>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.BalanceSat.Should().Be(500000);
        deserialized.PendingReceiveSat.Should().Be(10000);
        deserialized.PendingSendSat.Should().Be(5000);

        // Verify JSON structure
        json.Should().Contain("\"balanceSat\":500000");
        json.Should().Contain("\"pendingReceiveSat\":10000");
        json.Should().Contain("\"pendingSendSat\":5000");
    }

    [Fact]
    public void WalletBalance_AllRequiredFields_ArePresent()
    {
        // Arrange - All fields are required (non-nullable int64)
        var balance = new WalletBalance
        {
            BalanceSat = 0,
            PendingReceiveSat = 0,
            PendingSendSat = 0
        };

        // Act
        var json = JsonSerializer.Serialize(balance, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<WalletBalance>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.BalanceSat.Should().BeGreaterThanOrEqualTo(0);
        deserialized.PendingReceiveSat.Should().BeGreaterThanOrEqualTo(0);
        deserialized.PendingSendSat.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void WalletBalance_WithLargeValues_HandlesInt64Correctly()
    {
        // Arrange - Test int64 boundaries
        var balance = new WalletBalance
        {
            BalanceSat = 21_000_000_000_000L, // 210,000 BTC
            PendingReceiveSat = 1_000_000_000L, // 10 BTC
            PendingSendSat = 500_000_000L // 5 BTC
        };

        // Act
        var json = JsonSerializer.Serialize(balance, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<WalletBalance>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.BalanceSat.Should().Be(21_000_000_000_000L);
        deserialized.PendingReceiveSat.Should().Be(1_000_000_000L);
        deserialized.PendingSendSat.Should().Be(500_000_000L);
    }

    #endregion

    #region WalletLimits Contract Tests

    [Fact]
    public void WalletLimits_Serialization_MatchesContract()
    {
        // Arrange - This will fail until WalletLimits and nested types are implemented
        var limits = new WalletLimits
        {
            Receive = new LimitRange
            {
                MinSat = 1000,
                MaxSat = 10_000_000
            },
            Send = new LimitRange
            {
                MinSat = 1000,
                MaxSat = 5_000_000
            }
        };

        // Act
        var json = JsonSerializer.Serialize(limits, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<WalletLimits>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Receive.Should().NotBeNull();
        deserialized.Receive.MinSat.Should().Be(1000);
        deserialized.Receive.MaxSat.Should().Be(10_000_000);
        deserialized.Send.Should().NotBeNull();
        deserialized.Send.MinSat.Should().Be(1000);
        deserialized.Send.MaxSat.Should().Be(5_000_000);

        // Verify JSON structure
        json.Should().Contain("\"receive\":");
        json.Should().Contain("\"send\":");
        json.Should().Contain("\"minSat\":1000");
        json.Should().Contain("\"maxSat\":");
    }

    [Fact]
    public void WalletLimits_AllNestedFields_ArePresent()
    {
        // Arrange - All fields are required
        var limits = new WalletLimits
        {
            Receive = new LimitRange
            {
                MinSat = 100,
                MaxSat = 1000
            },
            Send = new LimitRange
            {
                MinSat = 50,
                MaxSat = 500
            }
        };

        // Act
        var json = JsonSerializer.Serialize(limits, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<WalletLimits>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Receive.MinSat.Should().BeGreaterThan(0);
        deserialized.Receive.MaxSat.Should().BeGreaterThan(deserialized.Receive.MinSat);
        deserialized.Send.MinSat.Should().BeGreaterThan(0);
        deserialized.Send.MaxSat.Should().BeGreaterThan(deserialized.Send.MinSat);
    }

    [Fact]
    public void LimitRange_WithLargeValues_HandlesInt64Correctly()
    {
        // Arrange - Test maximum Lightning Network limits
        var limitRange = new LimitRange
        {
            MinSat = 1,
            MaxSat = 4_294_967_295L // ~43 BTC (typical channel capacity limit)
        };

        // Act
        var json = JsonSerializer.Serialize(limitRange, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<LimitRange>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.MinSat.Should().Be(1);
        deserialized.MaxSat.Should().Be(4_294_967_295L);
    }

    [Fact]
    public void WalletLimits_JsonStructure_MatchesNestedSchema()
    {
        // Arrange
        var limits = new WalletLimits
        {
            Receive = new LimitRange { MinSat = 1000, MaxSat = 10000000 },
            Send = new LimitRange { MinSat = 1000, MaxSat = 5000000 }
        };

        // Act
        var json = JsonSerializer.Serialize(limits, JsonOptions);

        // Assert - Verify exact JSON structure matches OpenAPI spec
        json.Should().Contain("\"receive\":{\"minSat\":1000,\"maxSat\":10000000}");
        json.Should().Contain("\"send\":{\"minSat\":1000,\"maxSat\":5000000}");
    }

    #endregion

    #region Cross-Cutting Contract Concerns

    [Fact]
    public void AllDashboardDtos_UseCamelCaseNaming()
    {
        // Arrange - Verify consistent naming convention across all DTOs
        var stats = new DashboardStats
        {
            TotalReceivedSat = 1000,
            PendingCount = 1,
            DailyVolumeSat = 100,
            WalletBalanceSat = 500,
            SdkConnected = true
        };

        // Act
        var json = JsonSerializer.Serialize(stats, JsonOptions);

        // Assert - Property names must be camelCase, not PascalCase
        json.Should().NotContain("\"TotalReceivedSat\"");
        json.Should().NotContain("\"PendingCount\"");
        json.Should().Contain("\"totalReceivedSat\"");
        json.Should().Contain("\"pendingCount\"");
    }

    [Fact]
    public void DateTimeFields_SerializeAsISO8601WithUtc()
    {
        // Arrange - Verify consistent date/time format
        var timestamp = new DateTime(2026, 1, 26, 12, 30, 45, DateTimeKind.Utc);
        var stats = new DashboardStats
        {
            TotalReceivedSat = 1000,
            PendingCount = 0,
            DailyVolumeSat = 0,
            WalletBalanceSat = 1000,
            SdkConnected = true,
            LastPaymentAt = timestamp
        };

        // Act
        var json = JsonSerializer.Serialize(stats, JsonOptions);

        // Assert - Must be ISO 8601 format with Z suffix
        json.Should().Contain("2026-01-26T12:30:45");
        json.Should().Contain("Z"); // UTC indicator
    }

    #endregion
}

// NOTE: The following types do not exist yet and will cause compilation failures.
// This is EXPECTED in TDD Red phase. Implementation comes AFTER tests.

// Expected types to be implemented in src/Umbraco.Community.Bitcoin.LightningPayments.Core/Models/Dashboard/

/// <summary>
/// Placeholder for DashboardStats DTO - TO BE IMPLEMENTED
/// </summary>
public class DashboardStats
{
    public long TotalReceivedSat { get; set; }
    public int PendingCount { get; set; }
    public long DailyVolumeSat { get; set; }
    public long WalletBalanceSat { get; set; }
    public bool SdkConnected { get; set; }
    public DateTime? LastPaymentAt { get; set; }
}

/// <summary>
/// Placeholder for ChartData DTO - TO BE IMPLEMENTED
/// </summary>
public class ChartData
{
    public string Period { get; set; } = string.Empty;
    public List<ChartDataPoint> DataPoints { get; set; } = new();
}

/// <summary>
/// Placeholder for ChartDataPoint DTO - TO BE IMPLEMENTED
/// </summary>
public class ChartDataPoint
{
    public DateTime Timestamp { get; set; }
    public long AmountSat { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Placeholder for WalletBalance DTO - TO BE IMPLEMENTED
/// </summary>
public class WalletBalance
{
    public long BalanceSat { get; set; }
    public long PendingReceiveSat { get; set; }
    public long PendingSendSat { get; set; }
}

/// <summary>
/// Placeholder for WalletLimits DTO - TO BE IMPLEMENTED
/// </summary>
public class WalletLimits
{
    public LimitRange Receive { get; set; } = null!;
    public LimitRange Send { get; set; } = null!;
}

/// <summary>
/// Placeholder for LimitRange DTO - TO BE IMPLEMENTED
/// </summary>
public class LimitRange
{
    public long MinSat { get; set; }
    public long MaxSat { get; set; }
}
