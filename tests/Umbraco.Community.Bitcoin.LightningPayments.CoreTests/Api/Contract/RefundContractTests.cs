using System.Text.Json;
using FluentAssertions;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Management.Dto;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Api.Contract;

/// <summary>
/// Contract tests for Refund API endpoints (/refunds).
/// These tests verify that DTOs serialize/deserialize correctly according to the OpenAPI specification
/// (management-api.yaml).
///
/// IMPORTANT: These tests are written FIRST (TDD Red phase) and will FAIL until DTOs are implemented.
/// Expected failures:
/// - RefundTransactionResponse record does not exist
/// - RefundListResponse record does not exist
/// - InitiateRefundRequest record does not exist
/// - PrepareRefundRequest record does not exist
/// - PrepareRefundResponse record does not exist
/// </summary>
public class RefundContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    #region RefundTransactionResponse Contract Tests

    [Fact]
    public void RefundTransactionResponse_Serialization_MatchesContract()
    {
        // Arrange
        var refundId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        var initiatedAt = new DateTimeOffset(2026, 1, 27, 10, 0, 0, TimeSpan.Zero);
        var completedAt = new DateTimeOffset(2026, 1, 27, 10, 5, 0, TimeSpan.Zero);

        var refund = new RefundTransactionResponse
        {
            RefundId = refundId,
            OriginalPaymentHash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            AmountSat = 50_000,
            DestinationInvoice = "lnbc500u1pjdxyz...",
            Status = "succeeded",
            ErrorMessage = null,
            Reason = "Customer requested refund",
            InitiatedByUserId = "user-123",
            InitiatedAt = initiatedAt,
            CompletedAt = completedAt,
            RefundPaymentHash = "fedcba9876543210fedcba9876543210fedcba9876543210fedcba9876543210"
        };

        // Act
        var json = JsonSerializer.Serialize(refund, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<RefundTransactionResponse>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.RefundId.Should().Be(refundId);
        deserialized.OriginalPaymentHash.Should().Be("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef");
        deserialized.AmountSat.Should().Be(50_000);
        deserialized.DestinationInvoice.Should().Be("lnbc500u1pjdxyz...");
        deserialized.Status.Should().Be("succeeded");
        deserialized.ErrorMessage.Should().BeNull();
        deserialized.Reason.Should().Be("Customer requested refund");
        deserialized.InitiatedByUserId.Should().Be("user-123");
        deserialized.InitiatedAt.Should().Be(initiatedAt);
        deserialized.CompletedAt.Should().Be(completedAt);
        deserialized.RefundPaymentHash.Should().Be("fedcba9876543210fedcba9876543210fedcba9876543210fedcba9876543210");

        // Verify JSON structure matches OpenAPI spec (camelCase)
        json.Should().Contain("\"refundId\":");
        json.Should().Contain("\"originalPaymentHash\":\"0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef\"");
        json.Should().Contain("\"amountSat\":50000");
        json.Should().Contain("\"destinationInvoice\":\"lnbc500u1pjdxyz...\"");
        json.Should().Contain("\"status\":\"succeeded\""); // Status as lowercase string
        json.Should().Contain("\"reason\":\"Customer requested refund\"");
        json.Should().Contain("\"initiatedByUserId\":\"user-123\"");
    }

    [Fact]
    public void RefundTransactionResponse_RequiredFields_ArePresent()
    {
        // Arrange - Per OpenAPI spec: refundId, originalPaymentHash, amountSat, status, initiatedAt, initiatedByUserId are required
        var refund = new RefundTransactionResponse
        {
            RefundId = Guid.NewGuid(),
            OriginalPaymentHash = "abcd1234",
            AmountSat = 1000,
            DestinationInvoice = "lnbc10u1pjdxyz...",
            Status = "pending",
            InitiatedByUserId = "user-456",
            InitiatedAt = DateTimeOffset.UtcNow
            // errorMessage, reason, completedAt, refundPaymentHash are optional
        };

        // Act
        var json = JsonSerializer.Serialize(refund, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<RefundTransactionResponse>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.RefundId.Should().NotBe(Guid.Empty);
        deserialized.OriginalPaymentHash.Should().NotBeNullOrEmpty();
        deserialized.AmountSat.Should().BePositive();
        deserialized.Status.Should().NotBeNullOrEmpty();
        deserialized.InitiatedByUserId.Should().NotBeNullOrEmpty();
        deserialized.InitiatedAt.Should().NotBe(default);
        deserialized.ErrorMessage.Should().BeNull();
        deserialized.Reason.Should().BeNull();
        deserialized.CompletedAt.Should().BeNull();
        deserialized.RefundPaymentHash.Should().BeNull();
    }

    [Fact]
    public void RefundTransactionResponse_StatusString_SerializesAsLowercase()
    {
        // Arrange - Test all status values
        var testCases = new[]
        {
            "pending",
            "succeeded",
            "failed"
        };

        foreach (var statusValue in testCases)
        {
            var refund = new RefundTransactionResponse
            {
                RefundId = Guid.NewGuid(),
                OriginalPaymentHash = "test-hash",
                AmountSat = 1000,
                DestinationInvoice = "lnbc...",
                Status = statusValue,
                InitiatedByUserId = "user-1",
                InitiatedAt = DateTimeOffset.UtcNow
            };

            // Act
            var json = JsonSerializer.Serialize(refund, JsonOptions);

            // Assert
            json.Should().Contain($"\"status\":\"{statusValue}\"");
        }
    }

    [Fact]
    public void RefundTransactionResponse_NullableFields_SerializeCorrectly()
    {
        // Arrange - All nullable fields set to null
        var refund = new RefundTransactionResponse
        {
            RefundId = Guid.NewGuid(),
            OriginalPaymentHash = "hash123",
            AmountSat = 5000,
            DestinationInvoice = "lnbc50u1pjdxyz...",
            Status = "pending",
            ErrorMessage = null,
            Reason = null,
            InitiatedByUserId = "user-789",
            InitiatedAt = DateTimeOffset.UtcNow,
            CompletedAt = null,
            RefundPaymentHash = null
        };

        // Act
        var json = JsonSerializer.Serialize(refund, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<RefundTransactionResponse>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.ErrorMessage.Should().BeNull();
        deserialized.Reason.Should().BeNull();
        deserialized.CompletedAt.Should().BeNull();
        deserialized.RefundPaymentHash.Should().BeNull();
    }

    [Fact]
    public void RefundTransactionResponse_FailedStatus_WithErrorMessage()
    {
        // Arrange
        var refund = new RefundTransactionResponse
        {
            RefundId = Guid.NewGuid(),
            OriginalPaymentHash = "failed-hash",
            AmountSat = 10_000,
            DestinationInvoice = "lnbc100u1pjdxyz...",
            Status = "failed",
            ErrorMessage = "Insufficient balance to complete refund",
            Reason = "Product defect",
            InitiatedByUserId = "admin-1",
            InitiatedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow,
            RefundPaymentHash = null
        };

        // Act
        var json = JsonSerializer.Serialize(refund, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<RefundTransactionResponse>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Status.Should().Be("failed");
        deserialized.ErrorMessage.Should().Be("Insufficient balance to complete refund");
        deserialized.RefundPaymentHash.Should().BeNull();
    }

    [Fact]
    public void RefundTransactionResponse_JsonPropertyNames_AreCamelCase()
    {
        // Arrange
        var refund = new RefundTransactionResponse
        {
            RefundId = Guid.NewGuid(),
            OriginalPaymentHash = "camelcase-test",
            AmountSat = 1000,
            DestinationInvoice = "lnbc10u1pjdxyz...",
            Status = "pending",
            InitiatedByUserId = "user-1",
            InitiatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var json = JsonSerializer.Serialize(refund, JsonOptions);

        // Assert - All property names must be camelCase
        json.Should().Contain("\"refundId\":");
        json.Should().Contain("\"originalPaymentHash\":");
        json.Should().Contain("\"amountSat\":");
        json.Should().Contain("\"destinationInvoice\":");
        json.Should().Contain("\"status\":");
        json.Should().Contain("\"initiatedByUserId\":");
        json.Should().Contain("\"initiatedAt\":");

        // Must NOT contain PascalCase
        json.Should().NotContain("\"RefundId\":");
        json.Should().NotContain("\"OriginalPaymentHash\":");
        json.Should().NotContain("\"AmountSat\":");
        json.Should().NotContain("\"DestinationInvoice\":");
    }

    #endregion

    #region RefundListResponse Contract Tests

    [Fact]
    public void RefundListResponse_Serialization_MatchesContract()
    {
        // Arrange
        var refund1 = new RefundTransactionResponse
        {
            RefundId = Guid.NewGuid(),
            OriginalPaymentHash = "hash-1",
            AmountSat = 5000,
            DestinationInvoice = "lnbc50u1...",
            Status = "succeeded",
            InitiatedByUserId = "user-1",
            InitiatedAt = DateTimeOffset.UtcNow
        };

        var refund2 = new RefundTransactionResponse
        {
            RefundId = Guid.NewGuid(),
            OriginalPaymentHash = "hash-2",
            AmountSat = 10_000,
            DestinationInvoice = "lnbc100u1...",
            Status = "pending",
            InitiatedByUserId = "user-2",
            InitiatedAt = DateTimeOffset.UtcNow
        };

        var response = new RefundListResponse
        {
            Items = new[] { refund1, refund2 },
            Total = 2
        };

        // Act
        var json = JsonSerializer.Serialize(response, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<RefundListResponse>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Items.Should().HaveCount(2);
        deserialized.Total.Should().Be(2);

        // Verify JSON structure
        json.Should().Contain("\"items\":");
        json.Should().Contain("\"total\":2");
    }

    [Fact]
    public void RefundListResponse_EmptyList_SerializesCorrectly()
    {
        // Arrange
        var response = new RefundListResponse
        {
            Items = Array.Empty<RefundTransactionResponse>(),
            Total = 0
        };

        // Act
        var json = JsonSerializer.Serialize(response, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<RefundListResponse>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Items.Should().BeEmpty();
        deserialized.Total.Should().Be(0);
    }

    [Fact]
    public void RefundListResponse_RequiredFields_ArePresent()
    {
        // Arrange - Per OpenAPI spec: items and total are required
        var response = new RefundListResponse
        {
            Items = Array.Empty<RefundTransactionResponse>(),
            Total = 0
        };

        // Act
        var json = JsonSerializer.Serialize(response, JsonOptions);

        // Assert
        json.Should().Contain("\"items\":");
        json.Should().Contain("\"total\":");
    }

    #endregion

    #region InitiateRefundRequest Contract Tests

    [Fact]
    public void InitiateRefundRequest_Serialization_MatchesContract()
    {
        // Arrange
        var request = new InitiateRefundRequest
        {
            OriginalPaymentHash = "payment-hash-abc123",
            DestinationInvoice = "lnbc500u1pjdxyz...",
            Reason = "Customer dissatisfied with service"
        };

        // Act
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<InitiateRefundRequest>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.OriginalPaymentHash.Should().Be("payment-hash-abc123");
        deserialized.DestinationInvoice.Should().Be("lnbc500u1pjdxyz...");
        deserialized.Reason.Should().Be("Customer dissatisfied with service");

        // Verify JSON structure
        json.Should().Contain("\"originalPaymentHash\":\"payment-hash-abc123\"");
        json.Should().Contain("\"destinationInvoice\":\"lnbc500u1pjdxyz...\"");
        json.Should().Contain("\"reason\":\"Customer dissatisfied with service\"");
    }

    [Fact]
    public void InitiateRefundRequest_RequiredFields_ArePresent()
    {
        // Arrange - Per OpenAPI spec: originalPaymentHash and destinationInvoice are required; reason is optional
        var request = new InitiateRefundRequest
        {
            OriginalPaymentHash = "required-hash",
            DestinationInvoice = "lnbc10u1pjdxyz..."
            // reason is optional
        };

        // Act
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<InitiateRefundRequest>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.OriginalPaymentHash.Should().NotBeNullOrEmpty();
        deserialized.DestinationInvoice.Should().NotBeNullOrEmpty();
        deserialized.Reason.Should().BeNull();
    }

    [Fact]
    public void InitiateRefundRequest_OptionalReason_NullWhenOmitted()
    {
        // Arrange
        var request = new InitiateRefundRequest
        {
            OriginalPaymentHash = "test-hash",
            DestinationInvoice = "lnbc...",
            Reason = null
        };

        // Act
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<InitiateRefundRequest>(json, JsonOptions);

        // Assert
        deserialized!.Reason.Should().BeNull();
    }

    [Fact]
    public void InitiateRefundRequest_ReasonMaxLength_RespectedInContract()
    {
        // Arrange - Per OpenAPI spec: reason maxLength is 500
        var longReason = new string('x', 500);
        var request = new InitiateRefundRequest
        {
            OriginalPaymentHash = "hash-123",
            DestinationInvoice = "lnbc...",
            Reason = longReason
        };

        // Act
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<InitiateRefundRequest>(json, JsonOptions);

        // Assert
        deserialized!.Reason.Should().HaveLength(500);
    }

    [Fact]
    public void InitiateRefundRequest_Deserialization_FromApiJsonPayload()
    {
        // Arrange - Simulate a realistic API JSON payload
        var apiJson = """
        {
            "originalPaymentHash": "fedcba9876543210fedcba9876543210fedcba9876543210fedcba9876543210",
            "destinationInvoice": "lnbc1500u1pjk9xyzpp5qqqsyqcyq5rqwzqfqqqsyqcyq5rqwzqfqqqsyqcyq5rqwzqfqypq",
            "reason": "Defective product"
        }
        """;

        // Act
        var deserialized = JsonSerializer.Deserialize<InitiateRefundRequest>(apiJson, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.OriginalPaymentHash.Should().Be("fedcba9876543210fedcba9876543210fedcba9876543210fedcba9876543210");
        deserialized.DestinationInvoice.Should().Be("lnbc1500u1pjk9xyzpp5qqqsyqcyq5rqwzqfqqqsyqcyq5rqwzqfqqqsyqcyq5rqwzqfqypq");
        deserialized.Reason.Should().Be("Defective product");
    }

    [Fact]
    public void InitiateRefundRequest_Deserialization_MinimalApiPayload()
    {
        // Arrange - Only required fields
        var apiJson = """
        {
            "originalPaymentHash": "minimal-hash",
            "destinationInvoice": "lnbc..."
        }
        """;

        // Act
        var deserialized = JsonSerializer.Deserialize<InitiateRefundRequest>(apiJson, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.OriginalPaymentHash.Should().Be("minimal-hash");
        deserialized.DestinationInvoice.Should().Be("lnbc...");
        deserialized.Reason.Should().BeNull();
    }

    #endregion

    #region PrepareRefundRequest Contract Tests

    [Fact]
    public void PrepareRefundRequest_Serialization_MatchesContract()
    {
        // Arrange
        var request = new PrepareRefundRequest
        {
            OriginalPaymentHash = "prepare-hash-123",
            DestinationInvoice = "lnbc250u1pjdxyz..."
        };

        // Act
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PrepareRefundRequest>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.OriginalPaymentHash.Should().Be("prepare-hash-123");
        deserialized.DestinationInvoice.Should().Be("lnbc250u1pjdxyz...");

        // Verify JSON structure
        json.Should().Contain("\"originalPaymentHash\":\"prepare-hash-123\"");
        json.Should().Contain("\"destinationInvoice\":\"lnbc250u1pjdxyz...\"");
    }

    [Fact]
    public void PrepareRefundRequest_RequiredFields_ArePresent()
    {
        // Arrange - Per OpenAPI spec: originalPaymentHash and destinationInvoice are required
        var request = new PrepareRefundRequest
        {
            OriginalPaymentHash = "required-prepare-hash",
            DestinationInvoice = "lnbc..."
        };

        // Act
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PrepareRefundRequest>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.OriginalPaymentHash.Should().NotBeNullOrEmpty();
        deserialized.DestinationInvoice.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void PrepareRefundRequest_Deserialization_FromApiJsonPayload()
    {
        // Arrange
        var apiJson = """
        {
            "originalPaymentHash": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            "destinationInvoice": "lnbc750u1pjk9xyzpp5qqqsyqcyq5rqwzqfqqqsyqcyq5rqwzqfqqqsyqcyq5rqwzqfqypq"
        }
        """;

        // Act
        var deserialized = JsonSerializer.Deserialize<PrepareRefundRequest>(apiJson, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.OriginalPaymentHash.Should().Be("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef");
        deserialized.DestinationInvoice.Should().Be("lnbc750u1pjk9xyzpp5qqqsyqcyq5rqwzqfqqqsyqcyq5rqwzqfqqqsyqcyq5rqwzqfqypq");
    }

    #endregion

    #region PrepareRefundResponse Contract Tests

    [Fact]
    public void PrepareRefundResponse_Serialization_MatchesContract()
    {
        // Arrange
        var response = new PrepareRefundResponse
        {
            OriginalAmountSat = 100_000,
            RefundAmountSat = 95_000,
            FeeSat = 5_000,
            WalletBalanceSat = 500_000,
            CanProceed = true,
            ValidationError = null
        };

        // Act
        var json = JsonSerializer.Serialize(response, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PrepareRefundResponse>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.OriginalAmountSat.Should().Be(100_000);
        deserialized.RefundAmountSat.Should().Be(95_000);
        deserialized.FeeSat.Should().Be(5_000);
        deserialized.WalletBalanceSat.Should().Be(500_000);
        deserialized.CanProceed.Should().BeTrue();
        deserialized.ValidationError.Should().BeNull();

        // Verify JSON structure
        json.Should().Contain("\"originalAmountSat\":100000");
        json.Should().Contain("\"refundAmountSat\":95000");
        json.Should().Contain("\"feeSat\":5000");
        json.Should().Contain("\"walletBalanceSat\":500000");
        json.Should().Contain("\"canProceed\":true");
    }

    [Fact]
    public void PrepareRefundResponse_RequiredFields_ArePresent()
    {
        // Arrange - Per OpenAPI spec: all fields except validationError are required
        var response = new PrepareRefundResponse
        {
            OriginalAmountSat = 50_000,
            RefundAmountSat = 48_000,
            FeeSat = 2_000,
            WalletBalanceSat = 100_000,
            CanProceed = true
            // validationError is optional
        };

        // Act
        var json = JsonSerializer.Serialize(response, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PrepareRefundResponse>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.OriginalAmountSat.Should().BePositive();
        deserialized.RefundAmountSat.Should().BePositive();
        deserialized.FeeSat.Should().BeGreaterThanOrEqualTo(0);
        deserialized.WalletBalanceSat.Should().BeGreaterThanOrEqualTo(0);
        deserialized.CanProceed.Should().Be(deserialized.CanProceed); // CanProceed is always bool type
        deserialized.ValidationError.Should().BeNull();
    }

    [Fact]
    public void PrepareRefundResponse_CannotProceed_WithValidationError()
    {
        // Arrange
        var response = new PrepareRefundResponse
        {
            OriginalAmountSat = 100_000,
            RefundAmountSat = 0,
            FeeSat = 0,
            WalletBalanceSat = 10_000,
            CanProceed = false,
            ValidationError = "Insufficient wallet balance to complete refund"
        };

        // Act
        var json = JsonSerializer.Serialize(response, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PrepareRefundResponse>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.CanProceed.Should().BeFalse();
        deserialized.ValidationError.Should().Be("Insufficient wallet balance to complete refund");
    }

    [Fact]
    public void PrepareRefundResponse_JsonPropertyNames_AreCamelCase()
    {
        // Arrange
        var response = new PrepareRefundResponse
        {
            OriginalAmountSat = 1000,
            RefundAmountSat = 950,
            FeeSat = 50,
            WalletBalanceSat = 5000,
            CanProceed = true
        };

        // Act
        var json = JsonSerializer.Serialize(response, JsonOptions);

        // Assert - All property names must be camelCase
        json.Should().Contain("\"originalAmountSat\":");
        json.Should().Contain("\"refundAmountSat\":");
        json.Should().Contain("\"feeSat\":");
        json.Should().Contain("\"walletBalanceSat\":");
        json.Should().Contain("\"canProceed\":");

        // Must NOT contain PascalCase
        json.Should().NotContain("\"OriginalAmountSat\":");
        json.Should().NotContain("\"RefundAmountSat\":");
        json.Should().NotContain("\"CanProceed\":");
    }

    [Fact]
    public void PrepareRefundResponse_Deserialization_FromApiJsonPayload()
    {
        // Arrange - Simulate a realistic API JSON response
        var apiJson = """
        {
            "originalAmountSat": 200000,
            "refundAmountSat": 192000,
            "feeSat": 8000,
            "walletBalanceSat": 1000000,
            "canProceed": true,
            "validationError": null
        }
        """;

        // Act
        var deserialized = JsonSerializer.Deserialize<PrepareRefundResponse>(apiJson, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.OriginalAmountSat.Should().Be(200_000);
        deserialized.RefundAmountSat.Should().Be(192_000);
        deserialized.FeeSat.Should().Be(8_000);
        deserialized.WalletBalanceSat.Should().Be(1_000_000);
        deserialized.CanProceed.Should().BeTrue();
        deserialized.ValidationError.Should().BeNull();
    }

    [Fact]
    public void PrepareRefundResponse_Deserialization_WithValidationError()
    {
        // Arrange - Response indicating refund cannot proceed
        var apiJson = """
        {
            "originalAmountSat": 500000,
            "refundAmountSat": 0,
            "feeSat": 0,
            "walletBalanceSat": 50000,
            "canProceed": false,
            "validationError": "Wallet balance insufficient (need 500000, have 50000)"
        }
        """;

        // Act
        var deserialized = JsonSerializer.Deserialize<PrepareRefundResponse>(apiJson, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.CanProceed.Should().BeFalse();
        deserialized.ValidationError.Should().Be("Wallet balance insufficient (need 500000, have 50000)");
    }

    #endregion
}
