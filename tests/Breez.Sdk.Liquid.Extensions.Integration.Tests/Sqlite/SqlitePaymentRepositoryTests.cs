using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Breez.Sdk.Liquid.Extensions.Sqlite.Data;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Breez.Sdk.Liquid.Extensions.Integration.Tests.Sqlite;

/// <summary>
/// Integration tests for SqlitePaymentRepository using in-memory SQLite database.
/// These tests verify repository operations against a real database engine.
/// </summary>
public class SqlitePaymentRepositoryTests : IClassFixture<SqliteTestFixture>
{
    private readonly SqliteTestFixture _fixture;

    public SqlitePaymentRepositoryTests(SqliteTestFixture fixture)
    {
        _fixture = fixture;
    }

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithValidPayment_AddsPaymentSuccessfully()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        var payment = new PaymentState
        {
            PaymentHash = "abc123",
            AmountSat = 5000,
            Description = "Test payment",
            Invoice = "lnbc50000n1...",
            Kind = PaymentKind.Purchase,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            CorrelationId = "corr-001",
            Metadata = new Dictionary<string, string>
            {
                { "orderId", "ORDER-123" },
                { "userId", "usr_abc" }
            }
        };

        // Act
        var result = await repository.AddAsync(payment);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.PaymentHash.Should().Be("abc123");
        result.AmountSat.Should().Be(5000);
        result.Status.Should().Be(PaymentStatus.Pending);
        result.Metadata.Should().ContainKey("orderId");
    }

    [Fact]
    public async Task AddAsync_WithDuplicatePaymentHash_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        var payment1 = new PaymentState
        {
            PaymentHash = "duplicate_hash",
            AmountSat = 1000
        };

        var payment2 = new PaymentState
        {
            PaymentHash = "duplicate_hash",
            AmountSat = 2000
        };

        await repository.AddAsync(payment1);

        // Act
        var act = async () => await repository.AddAsync(payment2);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task AddAsync_WithNullPayment_ThrowsArgumentNullException()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        // Act
        var act = async () => await repository.AddAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("payment");
    }

    [Fact]
    public async Task AddAsync_WithEmptyMetadata_StoresPaymentCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        var payment = new PaymentState
        {
            PaymentHash = "no_metadata",
            AmountSat = 1000,
            Metadata = new Dictionary<string, string>()
        };

        // Act
        var result = await repository.AddAsync(payment);

        // Assert
        result.Metadata.Should().BeEmpty();
    }

    #endregion

    #region GetByHashAsync Tests

    [Fact]
    public async Task GetByHashAsync_WithExistingHash_ReturnsPayment()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        var payment = new PaymentState
        {
            PaymentHash = "find_me",
            AmountSat = 3000,
            Description = "Findable payment",
            Kind = PaymentKind.TipJar
        };

        await repository.AddAsync(payment);

        // Act
        var result = await repository.GetByHashAsync("find_me");

        // Assert
        result.Should().NotBeNull();
        result!.PaymentHash.Should().Be("find_me");
        result.AmountSat.Should().Be(3000);
        result.Description.Should().Be("Findable payment");
        result.Kind.Should().Be(PaymentKind.TipJar);
    }

    [Fact]
    public async Task GetByHashAsync_WithNonExistentHash_ReturnsNull()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        // Act
        var result = await repository.GetByHashAsync("nonexistent_hash");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByHashAsync_WithNullHash_ThrowsArgumentNullException()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        // Act
        var act = async () => await repository.GetByHashAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("paymentHash");
    }

    [Fact]
    public async Task GetByHashAsync_WithWhitespaceHash_ThrowsArgumentNullException()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        // Act
        var act = async () => await repository.GetByHashAsync("   ");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("paymentHash");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingPayment_UpdatesSuccessfully()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        var payment = new PaymentState
        {
            PaymentHash = "update_me",
            AmountSat = 5000,
            Status = PaymentStatus.Pending
        };

        var added = await repository.AddAsync(payment);

        // Update the mutable properties of PaymentState
        added.Status = PaymentStatus.Succeeded;
        added.ConfirmedAt = DateTimeOffset.UtcNow;
        added.Preimage = "preimage_secret";
        added.FeeSat = 50;
        var updated = added;

        // Act
        var result = await repository.UpdateAsync(updated);

        // Assert
        result.Status.Should().Be(PaymentStatus.Succeeded);
        result.ConfirmedAt.Should().NotBeNull();
        result.Preimage.Should().Be("preimage_secret");
        result.FeeSat.Should().Be(50);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentPayment_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        var payment = new PaymentState
        {
            Id = Guid.NewGuid(),
            PaymentHash = "does_not_exist",
            AmountSat = 1000
        };

        // Act
        var act = async () => await repository.UpdateAsync(payment);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not exist*");
    }

    [Fact]
    public async Task UpdateAsync_WithNullPayment_ThrowsArgumentNullException()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        // Act
        var act = async () => await repository.UpdateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("payment");
    }

    [Fact]
    public async Task UpdateAsync_ChangingStatus_PreservesOtherProperties()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        var payment = new PaymentState
        {
            PaymentHash = "status_change",
            AmountSat = 10000,
            Description = "Status test",
            Kind = PaymentKind.Subscription,
            Status = PaymentStatus.Pending,
            Metadata = new Dictionary<string, string> { { "key", "value" } }
        };

        var added = await repository.AddAsync(payment);
        added.Status = PaymentStatus.Expired;
        var updated = added;

        // Act
        var result = await repository.UpdateAsync(updated);

        // Assert
        result.Status.Should().Be(PaymentStatus.Expired);
        result.AmountSat.Should().Be(10000);
        result.Description.Should().Be("Status test");
        result.Kind.Should().Be(PaymentKind.Subscription);
        result.Metadata.Should().ContainKey("key");
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithExistingHash_ReturnsTrue()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        var payment = new PaymentState
        {
            PaymentHash = "exists_hash",
            AmountSat = 1000
        };

        await repository.AddAsync(payment);

        // Act
        var result = await repository.ExistsAsync("exists_hash");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentHash_ReturnsFalse()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        // Act
        var result = await repository.ExistsAsync("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithNullHash_ThrowsArgumentNullException()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        // Act
        var act = async () => await repository.ExistsAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("paymentHash");
    }

    #endregion

    #region GetByStatusAsync Tests

    [Fact]
    public async Task GetByStatusAsync_WithMatchingPayments_ReturnsFilteredList()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        await repository.AddAsync(new PaymentState { PaymentHash = "pending1", AmountSat = 1000, Status = PaymentStatus.Pending });
        await repository.AddAsync(new PaymentState { PaymentHash = "pending2", AmountSat = 2000, Status = PaymentStatus.Pending });
        await repository.AddAsync(new PaymentState { PaymentHash = "succeeded1", AmountSat = 3000, Status = PaymentStatus.Succeeded });
        await repository.AddAsync(new PaymentState { PaymentHash = "failed1", AmountSat = 4000, Status = PaymentStatus.Failed });

        // Act
        var pendingPayments = await repository.GetByStatusAsync(PaymentStatus.Pending);

        // Assert
        pendingPayments.Should().HaveCount(2);
        pendingPayments.Should().OnlyContain(p => p.Status == PaymentStatus.Pending);
        pendingPayments.Select(p => p.PaymentHash).Should().Contain(new[] { "pending1", "pending2" });
    }

    [Fact]
    public async Task GetByStatusAsync_WithLimit_ReturnsUpToLimitRecords()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        for (int i = 0; i < 10; i++)
        {
            await repository.AddAsync(new PaymentState
            {
                PaymentHash = $"succeeded_{i}",
                AmountSat = (ulong)(i * 1000),
                Status = PaymentStatus.Succeeded
            });
        }

        // Act
        var result = await repository.GetByStatusAsync(PaymentStatus.Succeeded, limit: 5);

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetByStatusAsync_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        await repository.AddAsync(new PaymentState { PaymentHash = "pending_only", AmountSat = 1000, Status = PaymentStatus.Pending });

        // Act
        var result = await repository.GetByStatusAsync(PaymentStatus.Refunded);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByStatusAsync_WithInvalidLimit_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        // Act
        var act = async () => await repository.GetByStatusAsync(PaymentStatus.Pending, limit: 0);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("limit");
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithMultiplePayments_ReturnsOrderedByCreatedAtDesc()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        var oldest = new PaymentState
        {
            PaymentHash = "oldest",
            AmountSat = 1000,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-3)
        };

        var middle = new PaymentState
        {
            PaymentHash = "middle",
            AmountSat = 2000,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-2)
        };

        var newest = new PaymentState
        {
            PaymentHash = "newest",
            AmountSat = 3000,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        await repository.AddAsync(oldest);
        await repository.AddAsync(middle);
        await repository.AddAsync(newest);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result[0].PaymentHash.Should().Be("newest");
        result[1].PaymentHash.Should().Be("middle");
        result[2].PaymentHash.Should().Be("oldest");
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        for (int i = 0; i < 15; i++)
        {
            await repository.AddAsync(new PaymentState
            {
                PaymentHash = $"payment_{i:D2}",
                AmountSat = (ulong)(i * 1000),
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-i)
            });
        }

        // Act
        var page1 = await repository.GetAllAsync(offset: 0, limit: 5);
        var page2 = await repository.GetAllAsync(offset: 5, limit: 5);
        var page3 = await repository.GetAllAsync(offset: 10, limit: 5);

        // Assert
        page1.Should().HaveCount(5);
        page2.Should().HaveCount(5);
        page3.Should().HaveCount(5);

        page1[0].PaymentHash.Should().Be("payment_00");
        page2[0].PaymentHash.Should().Be("payment_05");
        page3[0].PaymentHash.Should().Be("payment_10");
    }

    [Fact]
    public async Task GetAllAsync_WithOffsetBeyondTotal_ReturnsEmptyList()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        await repository.AddAsync(new PaymentState { PaymentHash = "only_one", AmountSat = 1000 });

        // Act
        var result = await repository.GetAllAsync(offset: 10);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithNegativeOffset_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        // Act
        var act = async () => await repository.GetAllAsync(offset: -1);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("offset");
    }

    [Fact]
    public async Task GetAllAsync_WithInvalidLimit_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        // Act
        var act = async () => await repository.GetAllAsync(limit: 0);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("limit");
    }

    [Fact]
    public async Task GetAllAsync_WithNoPayments_ReturnsEmptyList()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetByDateRangeAsync Tests

    [Fact]
    public async Task GetByDateRangeAsync_WithPaymentsInRange_ReturnsFilteredList()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        var baseDate = new DateTimeOffset(2025, 1, 15, 12, 0, 0, TimeSpan.Zero);

        await repository.AddAsync(new PaymentState
        {
            PaymentHash = "before_range",
            AmountSat = 1000,
            CreatedAt = baseDate.AddDays(-5)
        });

        await repository.AddAsync(new PaymentState
        {
            PaymentHash = "in_range_1",
            AmountSat = 2000,
            CreatedAt = baseDate.AddDays(-2)
        });

        await repository.AddAsync(new PaymentState
        {
            PaymentHash = "in_range_2",
            AmountSat = 3000,
            CreatedAt = baseDate
        });

        await repository.AddAsync(new PaymentState
        {
            PaymentHash = "after_range",
            AmountSat = 4000,
            CreatedAt = baseDate.AddDays(5)
        });

        // Act
        var result = await repository.GetByDateRangeAsync(
            from: baseDate.AddDays(-3),
            to: baseDate.AddDays(1)
        );

        // Assert
        result.Should().HaveCount(2);
        result.Select(p => p.PaymentHash).Should().Contain(new[] { "in_range_1", "in_range_2" });
    }

    [Fact]
    public async Task GetByDateRangeAsync_WithInclusiveBoundaries_IncludesBoundaryPayments()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        var startDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2025, 1, 31, 23, 59, 59, TimeSpan.Zero);

        await repository.AddAsync(new PaymentState
        {
            PaymentHash = "start_boundary",
            AmountSat = 1000,
            CreatedAt = startDate
        });

        await repository.AddAsync(new PaymentState
        {
            PaymentHash = "end_boundary",
            AmountSat = 2000,
            CreatedAt = endDate
        });

        // Act
        var result = await repository.GetByDateRangeAsync(from: startDate, to: endDate);

        // Assert
        result.Should().HaveCount(2);
        result.Select(p => p.PaymentHash).Should().Contain(new[] { "start_boundary", "end_boundary" });
    }

    [Fact]
    public async Task GetByDateRangeAsync_WithNoPaymentsInRange_ReturnsEmptyList()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        await repository.AddAsync(new PaymentState
        {
            PaymentHash = "old_payment",
            AmountSat = 1000,
            CreatedAt = DateTimeOffset.UtcNow.AddYears(-1)
        });

        // Act
        var result = await repository.GetByDateRangeAsync(
            from: DateTimeOffset.UtcNow.AddMonths(-1),
            to: DateTimeOffset.UtcNow
        );

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByDateRangeAsync_OrderedByCreatedAtDesc_ReturnsNewestFirst()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        var baseDate = DateTimeOffset.UtcNow;

        await repository.AddAsync(new PaymentState
        {
            PaymentHash = "first",
            AmountSat = 1000,
            CreatedAt = baseDate.AddHours(-3)
        });

        await repository.AddAsync(new PaymentState
        {
            PaymentHash = "second",
            AmountSat = 2000,
            CreatedAt = baseDate.AddHours(-2)
        });

        await repository.AddAsync(new PaymentState
        {
            PaymentHash = "third",
            AmountSat = 3000,
            CreatedAt = baseDate.AddHours(-1)
        });

        // Act
        var result = await repository.GetByDateRangeAsync(
            from: baseDate.AddHours(-4),
            to: baseDate
        );

        // Assert
        result.Should().HaveCount(3);
        result[0].PaymentHash.Should().Be("third");
        result[1].PaymentHash.Should().Be("second");
        result[2].PaymentHash.Should().Be("first");
    }

    [Fact]
    public async Task GetByDateRangeAsync_WithFromGreaterThanTo_ThrowsArgumentException()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        var startDate = DateTimeOffset.UtcNow;
        var endDate = startDate.AddDays(-10);

        // Act
        var act = async () => await repository.GetByDateRangeAsync(from: startDate, to: endDate);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*from*greater*to*");
    }

    #endregion

    #region Metadata Tests

    [Fact]
    public async Task AddAsync_WithComplexMetadata_PreservesAllKeyValuePairs()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        var payment = new PaymentState
        {
            PaymentHash = "complex_metadata",
            AmountSat = 10000,
            Metadata = new Dictionary<string, string>
            {
                { "orderId", "ORDER-12345" },
                { "userId", "usr_abc123" },
                { "source", "mobile_app" },
                { "version", "v2.1.0" },
                { "region", "us-west" }
            }
        };

        // Act
        var result = await repository.AddAsync(payment);
        var retrieved = await repository.GetByHashAsync("complex_metadata");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Metadata.Should().HaveCount(5);
        retrieved.Metadata["orderId"].Should().Be("ORDER-12345");
        retrieved.Metadata["userId"].Should().Be("usr_abc123");
        retrieved.Metadata["source"].Should().Be("mobile_app");
        retrieved.Metadata["version"].Should().Be("v2.1.0");
        retrieved.Metadata["region"].Should().Be("us-west");
    }

    [Fact]
    public async Task UpdateAsync_ModifyingMetadata_UpdatesCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        var payment = new PaymentState
        {
            PaymentHash = "metadata_update",
            AmountSat = 5000,
            Metadata = new Dictionary<string, string> { { "status", "initial" } }
        };

        var added = await repository.AddAsync(payment);

        // Metadata is init-only, so we need to create a new payment state
        // For the test, we'll update the existing entry's metadata directly
        added.Metadata.Clear();
        added.Metadata["status"] = "processed";
        added.Metadata["processedAt"] = DateTimeOffset.UtcNow.ToString("O");
        var updated = added;

        // Act
        await repository.UpdateAsync(updated);
        var retrieved = await repository.GetByHashAsync("metadata_update");

        // Assert
        retrieved!.Metadata.Should().HaveCount(2);
        retrieved.Metadata["status"].Should().Be("processed");
        retrieved.Metadata.Should().ContainKey("processedAt");
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task ConcurrentAdds_WithDifferentHashes_AllSucceed()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            var payment = new PaymentState
            {
                PaymentHash = $"concurrent_{i}",
                AmountSat = (ulong)(i * 1000)
            };
            return await repository.AddAsync(payment);
        });

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        results.Select(r => r.PaymentHash).Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task AddAsync_WithVeryLargeAmount_StoresCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        var payment = new PaymentState
        {
            PaymentHash = "large_amount",
            AmountSat = ulong.MaxValue
        };

        // Act
        var result = await repository.AddAsync(payment);

        // Assert
        result.AmountSat.Should().Be(ulong.MaxValue);
    }

    [Fact]
    public async Task AddAsync_WithMinimumAmount_StoresCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        var payment = new PaymentState
        {
            PaymentHash = "min_amount",
            AmountSat = 0
        };

        // Act
        var result = await repository.AddAsync(payment);

        // Assert
        result.AmountSat.Should().Be(0);
    }

    [Fact]
    public async Task AddAsync_WithLongDescription_StoresCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        var longDescription = new string('A', 5000);
        var payment = new PaymentState
        {
            PaymentHash = "long_description",
            AmountSat = 1000,
            Description = longDescription
        };

        // Act
        var result = await repository.AddAsync(payment);

        // Assert
        result.Description.Should().HaveLength(5000);
    }

    [Fact]
    public async Task AddAsync_WithAllPaymentKinds_StoresCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        var kinds = new[] { PaymentKind.Custom, PaymentKind.Paywall, PaymentKind.TipJar, PaymentKind.Purchase, PaymentKind.Subscription };

        // Act & Assert
        foreach (var kind in kinds)
        {
            var payment = new PaymentState
            {
                PaymentHash = $"kind_{kind}",
                AmountSat = 1000,
                Kind = kind
            };

            var result = await repository.AddAsync(payment);
            result.Kind.Should().Be(kind);
        }
    }

    [Fact]
    public async Task AddAsync_WithAllPaymentStatuses_StoresCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var repository = _fixture.CreateRepository(context);

        var statuses = new[] { PaymentStatus.Pending, PaymentStatus.Succeeded, PaymentStatus.Failed, PaymentStatus.Expired, PaymentStatus.Refunded };

        // Act & Assert
        foreach (var status in statuses)
        {
            var payment = new PaymentState
            {
                PaymentHash = $"status_{status}",
                AmountSat = 1000,
                Status = status
            };

            var result = await repository.AddAsync(payment);
            result.Status.Should().Be(status);
        }
    }

    #endregion
}

/// <summary>
/// Test fixture for creating isolated in-memory SQLite database instances.
/// Each test gets its own database to ensure complete isolation.
/// </summary>
public class SqliteTestFixture : IDisposable
{
    private readonly List<SqliteConnection> _connections = new();

    /// <summary>
    /// Creates a new in-memory SQLite database context.
    /// Each call creates a fresh database for test isolation.
    /// </summary>
    public SqlitePaymentDbContext CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        _connections.Add(connection);

        var options = new DbContextOptionsBuilder<SqlitePaymentDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new SqlitePaymentDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    /// <summary>
    /// Creates a repository instance using the provided context.
    /// </summary>
    public IPaymentRepository CreateRepository(SqlitePaymentDbContext context)
    {
        return new SqlitePaymentRepository(context);
    }

    public void Dispose()
    {
        foreach (var connection in _connections)
        {
            connection.Dispose();
        }
        _connections.Clear();
    }
}
