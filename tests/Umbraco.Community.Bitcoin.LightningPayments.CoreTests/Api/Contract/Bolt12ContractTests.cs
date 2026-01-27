using System.Text.Json;
using FluentAssertions;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Management.Dto;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Api.Contract;

/// <summary>
/// Contract tests for Bolt12 /offers CRUD API endpoints.
/// These tests verify that DTOs serialize/deserialize correctly according to the OpenAPI specification
/// (management-api.yaml).
///
/// IMPORTANT: These tests are written FIRST (TDD Red phase) and will FAIL until DTOs are implemented.
/// Expected failures:
/// - Bolt12OfferResponse record does not exist
/// - Bolt12OfferDetailsResponse record does not exist
/// - CreateOfferRequest record does not exist
/// </summary>
public class Bolt12ContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    #region Bolt12OfferResponse Contract Tests

    [Fact]
    public void Bolt12OfferResponse_Serialization_MatchesContract()
    {
        // Arrange
        var offerId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        var createdAt = new DateTimeOffset(2026, 1, 26, 12, 0, 0, TimeSpan.Zero);

        var offer = new Bolt12OfferResponse
        {
            OfferId = offerId,
            OfferString = "lno1qgsqvjlwvejwxzrfq0example",
            Description = "Monthly subscription",
            AmountSat = 50_000,
            IsActive = true,
            CreatedAt = createdAt,
            DeactivatedAt = null,
            ContentId = 42
        };

        // Act
        var json = JsonSerializer.Serialize(offer, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<Bolt12OfferResponse>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.OfferId.Should().Be(offerId);
        deserialized.OfferString.Should().Be("lno1qgsqvjlwvejwxzrfq0example");
        deserialized.Description.Should().Be("Monthly subscription");
        deserialized.AmountSat.Should().Be(50_000UL);
        deserialized.IsActive.Should().BeTrue();
        deserialized.CreatedAt.Should().Be(createdAt);
        deserialized.DeactivatedAt.Should().BeNull();
        deserialized.ContentId.Should().Be(42);

        // Verify JSON structure matches OpenAPI spec (camelCase)
        json.Should().Contain("\"offerId\":");
        json.Should().Contain("\"offerString\":\"lno1qgsqvjlwvejwxzrfq0example\"");
        json.Should().Contain("\"description\":\"Monthly subscription\"");
        json.Should().Contain("\"amountSat\":50000");
        json.Should().Contain("\"isActive\":true");
        json.Should().Contain("\"contentId\":42");
    }

    [Fact]
    public void Bolt12OfferResponse_RequiredFields_ArePresent()
    {
        // Arrange - Per OpenAPI spec: offerId, offerString, description, isActive, createdAt are required
        var offer = new Bolt12OfferResponse
        {
            OfferId = Guid.NewGuid(),
            OfferString = "lno1qgsqvjl_required",
            Description = "Minimal offer",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
            // amountSat, deactivatedAt, contentId are optional
        };

        // Act
        var json = JsonSerializer.Serialize(offer, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<Bolt12OfferResponse>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.OfferId.Should().NotBe(Guid.Empty);
        deserialized.OfferString.Should().NotBeNullOrEmpty();
        deserialized.Description.Should().NotBeNullOrEmpty();
        deserialized.AmountSat.Should().BeNull();
        deserialized.DeactivatedAt.Should().BeNull();
        deserialized.ContentId.Should().BeNull();
    }

    [Fact]
    public void Bolt12OfferResponse_NullableFields_SerializeCorrectly()
    {
        // Arrange
        var offer = new Bolt12OfferResponse
        {
            OfferId = Guid.NewGuid(),
            OfferString = "lno1_nullable_test",
            Description = "Nullable fields",
            AmountSat = null,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            DeactivatedAt = null,
            ContentId = null
        };

        // Act
        var json = JsonSerializer.Serialize(offer, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<Bolt12OfferResponse>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.AmountSat.Should().BeNull();
        deserialized.DeactivatedAt.Should().BeNull();
        deserialized.ContentId.Should().BeNull();
    }

    [Fact]
    public void Bolt12OfferResponse_OfferString_PreservesLno1Prefix()
    {
        // Arrange - BOLT12 offers always start with "lno1"
        var offer = new Bolt12OfferResponse
        {
            OfferId = Guid.NewGuid(),
            OfferString = "lno1qgsqvjlwvejwxzrfq09gcemjeu4pedc",
            Description = "BOLT12 format test",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var json = JsonSerializer.Serialize(offer, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<Bolt12OfferResponse>(json, JsonOptions);

        // Assert
        deserialized!.OfferString.Should().StartWith("lno1");
    }

    [Fact]
    public void Bolt12OfferResponse_JsonPropertyNames_AreCamelCase()
    {
        // Arrange
        var offer = new Bolt12OfferResponse
        {
            OfferId = Guid.NewGuid(),
            OfferString = "lno1_camelcase_test",
            Description = "CamelCase test",
            AmountSat = 100,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            ContentId = 1
        };

        // Act
        var json = JsonSerializer.Serialize(offer, JsonOptions);

        // Assert - All property names must be camelCase
        json.Should().Contain("\"offerId\":");
        json.Should().Contain("\"offerString\":");
        json.Should().Contain("\"description\":");
        json.Should().Contain("\"amountSat\":");
        json.Should().Contain("\"isActive\":");
        json.Should().Contain("\"createdAt\":");
        json.Should().Contain("\"contentId\":");

        // Must NOT contain PascalCase
        json.Should().NotContain("\"OfferId\":");
        json.Should().NotContain("\"OfferString\":");
        json.Should().NotContain("\"Description\":");
        json.Should().NotContain("\"AmountSat\":");
        json.Should().NotContain("\"IsActive\":");
    }

    [Fact]
    public void Bolt12OfferResponse_LargeAmountSat_SerializesCorrectly()
    {
        // Arrange - ulong can hold large amounts (21M BTC = 2_100_000_000_000_000 sats)
        var largeAmount = 2_100_000_000_000_000UL;
        var offer = new Bolt12OfferResponse
        {
            OfferId = Guid.NewGuid(),
            OfferString = "lno1_large_amount",
            Description = "Large amount test",
            AmountSat = largeAmount,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var json = JsonSerializer.Serialize(offer, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<Bolt12OfferResponse>(json, JsonOptions);

        // Assert
        deserialized!.AmountSat.Should().Be(largeAmount);
    }

    #endregion

    #region Bolt12OfferDetailsResponse Contract Tests

    [Fact]
    public void Bolt12OfferDetailsResponse_Serialization_MatchesContract()
    {
        // Arrange
        var details = new Bolt12OfferDetailsResponse
        {
            OfferId = Guid.NewGuid(),
            OfferString = "lno1qgsqvjl_details",
            Description = "Offer with payment details",
            AmountSat = 10_000,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            PaymentCount = 15,
            TotalReceivedSat = 150_000,
            ConfirmedCount = 12
        };

        // Act
        var json = JsonSerializer.Serialize(details, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<Bolt12OfferDetailsResponse>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.PaymentCount.Should().Be(15);
        deserialized.TotalReceivedSat.Should().Be(150_000);
        deserialized.ConfirmedCount.Should().Be(12);

        // Verify JSON structure
        json.Should().Contain("\"paymentCount\":15");
        json.Should().Contain("\"totalReceivedSat\":150000");
        json.Should().Contain("\"confirmedCount\":12");
    }

    [Fact]
    public void Bolt12OfferDetailsResponse_InheritsBaseFields_Correctly()
    {
        // Arrange
        var offerId = Guid.NewGuid();
        var details = new Bolt12OfferDetailsResponse
        {
            OfferId = offerId,
            OfferString = "lno1qgsqvjl_inherited",
            Description = "Inherited fields test",
            AmountSat = 5_000,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            ContentId = 99,
            PaymentCount = 3,
            TotalReceivedSat = 15_000,
            ConfirmedCount = 2
        };

        // Act
        var json = JsonSerializer.Serialize(details, JsonOptions);

        // Assert - Base class fields must be present
        json.Should().Contain("\"offerId\":");
        json.Should().Contain("\"offerString\":");
        json.Should().Contain("\"description\":");
        json.Should().Contain("\"isActive\":");
        json.Should().Contain("\"createdAt\":");
        json.Should().Contain("\"contentId\":99");

        // Derived class fields must be present
        json.Should().Contain("\"paymentCount\":3");
        json.Should().Contain("\"totalReceivedSat\":15000");
        json.Should().Contain("\"confirmedCount\":2");
    }

    [Fact]
    public void Bolt12OfferDetailsResponse_WithZeroPayments_SerializesCorrectly()
    {
        // Arrange
        var details = new Bolt12OfferDetailsResponse
        {
            OfferId = Guid.NewGuid(),
            OfferString = "lno1_zero_payments",
            Description = "No payments yet",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            PaymentCount = 0,
            TotalReceivedSat = 0,
            ConfirmedCount = 0
        };

        // Act
        var json = JsonSerializer.Serialize(details, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<Bolt12OfferDetailsResponse>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.PaymentCount.Should().Be(0);
        deserialized.TotalReceivedSat.Should().Be(0);
        deserialized.ConfirmedCount.Should().Be(0);
    }

    #endregion

    #region CreateOfferRequest Contract Tests

    [Fact]
    public void CreateOfferRequest_Serialization_MatchesContract()
    {
        // Arrange
        var request = new CreateOfferRequest
        {
            Description = "Monthly subscription offer",
            AmountSat = 50_000,
            ContentId = 42
        };

        // Act
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<CreateOfferRequest>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Description.Should().Be("Monthly subscription offer");
        deserialized.AmountSat.Should().Be(50_000UL);
        deserialized.ContentId.Should().Be(42);

        // Verify JSON structure
        json.Should().Contain("\"description\":\"Monthly subscription offer\"");
        json.Should().Contain("\"amountSat\":50000");
        json.Should().Contain("\"contentId\":42");
    }

    [Fact]
    public void CreateOfferRequest_RequiredFields_ArePresent()
    {
        // Arrange - Per OpenAPI spec: only description is required
        var request = new CreateOfferRequest
        {
            Description = "Minimal offer request"
            // amountSat and contentId are optional
        };

        // Act
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<CreateOfferRequest>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Description.Should().Be("Minimal offer request");
        deserialized.AmountSat.Should().BeNull();
        deserialized.ContentId.Should().BeNull();
    }

    [Fact]
    public void CreateOfferRequest_OptionalFields_OmittedWhenNull()
    {
        // Arrange
        var request = new CreateOfferRequest
        {
            Description = "Variable amount offer",
            AmountSat = null,
            ContentId = null
        };

        // Act
        var json = JsonSerializer.Serialize(request, JsonOptions);

        // Assert - Nullable fields should serialize as null (or be omitted)
        var deserialized = JsonSerializer.Deserialize<CreateOfferRequest>(json, JsonOptions);
        deserialized!.AmountSat.Should().BeNull();
        deserialized.ContentId.Should().BeNull();
    }

    [Fact]
    public void CreateOfferRequest_Deserialization_FromApiJsonPayload()
    {
        // Arrange - Simulate a realistic API JSON payload
        var apiJson = """
        {
            "description": "Premium content access",
            "amountSat": 25000,
            "contentId": 7
        }
        """;

        // Act
        var deserialized = JsonSerializer.Deserialize<CreateOfferRequest>(apiJson, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Description.Should().Be("Premium content access");
        deserialized.AmountSat.Should().Be(25_000UL);
        deserialized.ContentId.Should().Be(7);
    }

    [Fact]
    public void CreateOfferRequest_Deserialization_MinimalApiPayload()
    {
        // Arrange - Only required field
        var apiJson = """
        {
            "description": "Donation jar"
        }
        """;

        // Act
        var deserialized = JsonSerializer.Deserialize<CreateOfferRequest>(apiJson, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Description.Should().Be("Donation jar");
        deserialized.AmountSat.Should().BeNull();
        deserialized.ContentId.Should().BeNull();
    }

    #endregion
}
