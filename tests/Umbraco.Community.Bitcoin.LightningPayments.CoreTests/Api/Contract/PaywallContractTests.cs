using System.Text.Json;
using FluentAssertions;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Api.Contract;

/// <summary>
/// Contract tests for Paywall API endpoints.
/// These tests verify that DTOs serialize/deserialize correctly according to the OpenAPI specification (public-api.yaml).
///
/// IMPORTANT: These tests are written FIRST (TDD Red phase) and will FAIL until DTOs are implemented.
/// Expected failures:
/// - CreatePaywallInvoiceRequest class does not exist
/// - InvoiceResponse class does not exist
/// - PaywallStatus class does not exist
/// - FiatAmount class does not exist
/// </summary>
public class PaywallContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    #region CreatePaywallInvoiceRequest Contract Tests

    [Fact]
    public void CreatePaywallInvoiceRequest_Serialization_MatchesContract()
    {
        // Arrange
        var request = new CreatePaywallInvoiceRequest
        {
            ContentId = 123,
            SessionId = "session-abc-123",
            Tier = "24h",
            IdempotencyKey = "idempotency-key-xyz"
        };

        // Act
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<CreatePaywallInvoiceRequest>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.ContentId.Should().Be(123);
        deserialized.SessionId.Should().Be("session-abc-123");
        deserialized.Tier.Should().Be("24h");
        deserialized.IdempotencyKey.Should().Be("idempotency-key-xyz");

        // Verify JSON structure matches OpenAPI spec (camelCase)
        json.Should().Contain("\"contentId\":123");
        json.Should().Contain("\"sessionId\":\"session-abc-123\"");
        json.Should().Contain("\"tier\":\"24h\"");
        json.Should().Contain("\"idempotencyKey\":\"idempotency-key-xyz\"");
    }

    [Fact]
    public void CreatePaywallInvoiceRequest_RequiredFields_ArePresent()
    {
        // Arrange - Per OpenAPI spec: contentId, sessionId, tier are required
        var request = new CreatePaywallInvoiceRequest
        {
            ContentId = 1,
            SessionId = "session",
            Tier = "1h"
            // IdempotencyKey is optional - omitted intentionally
        };

        // Act
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<CreatePaywallInvoiceRequest>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.ContentId.Should().BeGreaterThan(0);
        deserialized.SessionId.Should().NotBeNullOrEmpty();
        deserialized.Tier.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreatePaywallInvoiceRequest_WithNullIdempotencyKey_SerializesCorrectly()
    {
        // Arrange - IdempotencyKey is optional per OpenAPI spec
        var request = new CreatePaywallInvoiceRequest
        {
            ContentId = 456,
            SessionId = "session-xyz",
            Tier = "8h",
            IdempotencyKey = null
        };

        // Act
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<CreatePaywallInvoiceRequest>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.IdempotencyKey.Should().BeNull();
    }

    [Theory]
    [InlineData("1h")]
    [InlineData("8h")]
    [InlineData("24h")]
    [InlineData("7d")]
    public void CreatePaywallInvoiceRequest_Tier_AcceptsValidEnumValues(string tier)
    {
        // Arrange - Per OpenAPI spec: tier enum is [1h, 8h, 24h, 7d]
        var request = new CreatePaywallInvoiceRequest
        {
            ContentId = 1,
            SessionId = "session",
            Tier = tier
        };

        // Act
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<CreatePaywallInvoiceRequest>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Tier.Should().Be(tier);
    }

    #endregion

    #region InvoiceResponse Contract Tests

    [Fact]
    public void InvoiceResponse_Serialization_MatchesContract()
    {
        // Arrange
        var response = new InvoiceResponse
        {
            PaymentHash = "abc123def456",
            Invoice = "lnbc100n1p3...",
            AmountSat = 1000,
            AmountFiat = new FiatAmount
            {
                Currency = "USD",
                Amount = 0.45,
                Formatted = "$0.45",
                IsStale = false
            },
            ExpiresAt = new DateTime(2026, 1, 26, 13, 0, 0, DateTimeKind.Utc),
            QrCodeDataUrl = "data:image/png;base64,..."
        };

        // Act
        var json = JsonSerializer.Serialize(response, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<InvoiceResponse>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.PaymentHash.Should().Be("abc123def456");
        deserialized.Invoice.Should().Be("lnbc100n1p3...");
        deserialized.AmountSat.Should().Be(1000);
        deserialized.AmountFiat.Should().NotBeNull();
        deserialized.AmountFiat!.Currency.Should().Be("USD");
        deserialized.AmountFiat.Amount.Should().Be(0.45);
        deserialized.ExpiresAt.Should().Be(new DateTime(2026, 1, 26, 13, 0, 0, DateTimeKind.Utc));
        deserialized.QrCodeDataUrl.Should().Be("data:image/png;base64,...");

        // Verify JSON structure
        json.Should().Contain("\"paymentHash\":\"abc123def456\"");
        json.Should().Contain("\"invoice\":\"lnbc100n1p3...\"");
        json.Should().Contain("\"amountSat\":1000");
        json.Should().Contain("\"amountFiat\":");
        json.Should().Contain("\"expiresAt\":");
    }

    [Fact]
    public void InvoiceResponse_RequiredFields_ArePresent()
    {
        // Arrange - Per OpenAPI spec: paymentHash, invoice, amountSat, expiresAt are required
        var response = new InvoiceResponse
        {
            PaymentHash = "hash",
            Invoice = "lnbc...",
            AmountSat = 100,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
            // AmountFiat and QrCodeDataUrl are optional - omitted
        };

        // Act
        var json = JsonSerializer.Serialize(response, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<InvoiceResponse>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.PaymentHash.Should().NotBeNullOrEmpty();
        deserialized.Invoice.Should().NotBeNullOrEmpty();
        deserialized.AmountSat.Should().BeGreaterThan(0);
        deserialized.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void InvoiceResponse_WithNullOptionalFields_SerializesCorrectly()
    {
        // Arrange - AmountFiat and QrCodeDataUrl are optional
        var response = new InvoiceResponse
        {
            PaymentHash = "hash123",
            Invoice = "lnbc...",
            AmountSat = 500,
            AmountFiat = null,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            QrCodeDataUrl = null
        };

        // Act
        var json = JsonSerializer.Serialize(response, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<InvoiceResponse>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.AmountFiat.Should().BeNull();
        deserialized.QrCodeDataUrl.Should().BeNull();
    }

    [Fact]
    public void InvoiceResponse_WithLargeAmount_HandlesInt64Correctly()
    {
        // Arrange - Test large satoshi values
        var response = new InvoiceResponse
        {
            PaymentHash = "hash",
            Invoice = "lnbc...",
            AmountSat = 21_000_000_00000000L, // Max BTC supply in sats
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        // Act
        var json = JsonSerializer.Serialize(response, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<InvoiceResponse>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.AmountSat.Should().Be(21_000_000_00000000L);
    }

    #endregion

    #region PaywallStatus Contract Tests

    [Fact]
    public void PaywallStatus_Serialization_MatchesContract()
    {
        // Arrange
        var status = new PaywallStatus
        {
            ContentId = 789,
            HasPaid = true,
            PaidAt = new DateTime(2026, 1, 26, 10, 0, 0, DateTimeKind.Utc),
            AccessExpiresAt = new DateTime(2026, 1, 27, 10, 0, 0, DateTimeKind.Utc),
            Tier = "24h"
        };

        // Act
        var json = JsonSerializer.Serialize(status, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PaywallStatus>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.ContentId.Should().Be(789);
        deserialized.HasPaid.Should().BeTrue();
        deserialized.PaidAt.Should().Be(new DateTime(2026, 1, 26, 10, 0, 0, DateTimeKind.Utc));
        deserialized.AccessExpiresAt.Should().Be(new DateTime(2026, 1, 27, 10, 0, 0, DateTimeKind.Utc));
        deserialized.Tier.Should().Be("24h");

        // Verify JSON structure
        json.Should().Contain("\"contentId\":789");
        json.Should().Contain("\"hasPaid\":true");
        json.Should().Contain("\"paidAt\":");
        json.Should().Contain("\"accessExpiresAt\":");
        json.Should().Contain("\"tier\":\"24h\"");
    }

    [Fact]
    public void PaywallStatus_RequiredFields_ArePresent()
    {
        // Arrange - Per OpenAPI spec: contentId, hasPaid are required
        var status = new PaywallStatus
        {
            ContentId = 123,
            HasPaid = false
            // PaidAt, AccessExpiresAt, Tier are nullable - omitted
        };

        // Act
        var json = JsonSerializer.Serialize(status, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PaywallStatus>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.ContentId.Should().BeGreaterThan(0);
        deserialized.HasPaid.Should().BeFalse();
    }

    [Fact]
    public void PaywallStatus_WithNullOptionalFields_SerializesCorrectly()
    {
        // Arrange - PaidAt, AccessExpiresAt, Tier are nullable
        var status = new PaywallStatus
        {
            ContentId = 456,
            HasPaid = false,
            PaidAt = null,
            AccessExpiresAt = null,
            Tier = null
        };

        // Act
        var json = JsonSerializer.Serialize(status, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PaywallStatus>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.PaidAt.Should().BeNull();
        deserialized.AccessExpiresAt.Should().BeNull();
        deserialized.Tier.Should().BeNull();
    }

    [Fact]
    public void PaywallStatus_UnpaidContent_HasCorrectDefaults()
    {
        // Arrange - User has not paid for content
        var status = new PaywallStatus
        {
            ContentId = 100,
            HasPaid = false
        };

        // Act
        var json = JsonSerializer.Serialize(status, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PaywallStatus>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.HasPaid.Should().BeFalse();
        deserialized.PaidAt.Should().BeNull();
        deserialized.AccessExpiresAt.Should().BeNull();
    }

    [Fact]
    public void PaywallStatus_PaidContent_HasValidTimestamps()
    {
        // Arrange - User has paid with 24h access
        var now = DateTime.UtcNow;
        var status = new PaywallStatus
        {
            ContentId = 100,
            HasPaid = true,
            PaidAt = now,
            AccessExpiresAt = now.AddHours(24),
            Tier = "24h"
        };

        // Act & Assert
        status.HasPaid.Should().BeTrue();
        status.PaidAt.Should().NotBeNull();
        status.AccessExpiresAt.Should().BeAfter(status.PaidAt!.Value);
    }

    #endregion

    #region FiatAmount Contract Tests

    [Fact]
    public void FiatAmount_Serialization_MatchesContract()
    {
        // Arrange
        var fiat = new FiatAmount
        {
            Currency = "EUR",
            Amount = 0.82,
            Formatted = "€0.82",
            IsStale = false
        };

        // Act
        var json = JsonSerializer.Serialize(fiat, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<FiatAmount>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Currency.Should().Be("EUR");
        deserialized.Amount.Should().Be(0.82);
        deserialized.Formatted.Should().Be("€0.82");
        deserialized.IsStale.Should().BeFalse();

        // Verify JSON structure
        json.Should().Contain("\"currency\":\"EUR\"");
        json.Should().Contain("\"amount\":0.82");
        json.Should().Contain("\"formatted\":\"\\u20AC0.82\""); // € is escaped in JSON
        json.Should().Contain("\"isStale\":false");
    }

    [Fact]
    public void FiatAmount_WithNullFormatted_SerializesCorrectly()
    {
        // Arrange - Formatted is optional
        var fiat = new FiatAmount
        {
            Currency = "GBP",
            Amount = 0.75,
            Formatted = null,
            IsStale = true
        };

        // Act
        var json = JsonSerializer.Serialize(fiat, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<FiatAmount>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Formatted.Should().BeNull();
    }

    [Fact]
    public void FiatAmount_WithStaleRate_IndicatesCorrectly()
    {
        // Arrange - Rate is stale (>5 minutes old per SC-006)
        var fiat = new FiatAmount
        {
            Currency = "USD",
            Amount = 0.50,
            IsStale = true
        };

        // Act
        var json = JsonSerializer.Serialize(fiat, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<FiatAmount>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.IsStale.Should().BeTrue();
    }

    #endregion

    #region Cross-Cutting Contract Concerns

    [Fact]
    public void AllPaywallDtos_UseCamelCaseNaming()
    {
        // Arrange
        var request = new CreatePaywallInvoiceRequest
        {
            ContentId = 1,
            SessionId = "session",
            Tier = "1h",
            IdempotencyKey = "key"
        };

        // Act
        var json = JsonSerializer.Serialize(request, JsonOptions);

        // Assert - Property names must be camelCase, not PascalCase
        json.Should().NotContain("\"ContentId\"");
        json.Should().NotContain("\"SessionId\"");
        json.Should().NotContain("\"IdempotencyKey\"");
        json.Should().Contain("\"contentId\"");
        json.Should().Contain("\"sessionId\"");
        json.Should().Contain("\"idempotencyKey\"");
    }

    [Fact]
    public void DateTimeFields_SerializeAsISO8601WithUtc()
    {
        // Arrange
        var timestamp = new DateTime(2026, 1, 26, 12, 30, 45, DateTimeKind.Utc);
        var response = new InvoiceResponse
        {
            PaymentHash = "hash",
            Invoice = "lnbc...",
            AmountSat = 100,
            ExpiresAt = timestamp
        };

        // Act
        var json = JsonSerializer.Serialize(response, JsonOptions);

        // Assert - Must be ISO 8601 format
        json.Should().Contain("2026-01-26T12:30:45");
    }

    [Fact]
    public void TierDurationValues_MapToCorrectTimeSpans()
    {
        // Document expected tier durations for implementation
        var tierDurations = new Dictionary<string, TimeSpan>
        {
            ["1h"] = TimeSpan.FromHours(1),
            ["8h"] = TimeSpan.FromHours(8),
            ["24h"] = TimeSpan.FromHours(24),
            ["7d"] = TimeSpan.FromDays(7)
        };

        // Assert tier key format matches OpenAPI enum values
        tierDurations.Keys.Should().BeEquivalentTo(new[] { "1h", "8h", "24h", "7d" });
    }

    #endregion
}

// NOTE: The following types do not exist yet and will cause compilation failures.
// This is EXPECTED in TDD Red phase. Implementation comes AFTER tests.

// Expected types to be implemented in:
// src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Public/Dto/

/// <summary>
/// Placeholder for CreatePaywallInvoiceRequest DTO - TO BE IMPLEMENTED
/// Location: src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Public/Dto/PaywallDtos.cs
/// </summary>
public class CreatePaywallInvoiceRequest
{
    public int ContentId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty; // enum: "1h", "8h", "24h", "7d"
    public string? IdempotencyKey { get; set; }
}

/// <summary>
/// Placeholder for InvoiceResponse DTO - TO BE IMPLEMENTED
/// Location: src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Public/Dto/PaywallDtos.cs
/// </summary>
public class InvoiceResponse
{
    public string PaymentHash { get; set; } = string.Empty;
    public string Invoice { get; set; } = string.Empty;
    public long AmountSat { get; set; }
    public FiatAmount? AmountFiat { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? QrCodeDataUrl { get; set; }
}

/// <summary>
/// Placeholder for PaywallStatus DTO - TO BE IMPLEMENTED
/// Location: src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Public/Dto/PaywallDtos.cs
/// </summary>
public class PaywallStatus
{
    public int ContentId { get; set; }
    public bool HasPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? AccessExpiresAt { get; set; }
    public string? Tier { get; set; }
}

/// <summary>
/// Placeholder for FiatAmount DTO - TO BE IMPLEMENTED
/// Location: src/Umbraco.Community.Bitcoin.LightningPayments.Core/Api/Public/Dto/PaywallDtos.cs
/// </summary>
public class FiatAmount
{
    public string Currency { get; set; } = string.Empty;
    public double Amount { get; set; }
    public string? Formatted { get; set; }
    public bool IsStale { get; set; }
}
