using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Breez.Sdk.Liquid.Extensions.Core.Persistence;
using Breez.Sdk.Liquid.Extensions.TestUtilities.Builders;
using FluentAssertions;

namespace Breez.Sdk.Liquid.Extensions.Core.Tests.Persistence;

/// <summary>
/// Unit tests for InMemoryPaymentRepository.
/// These tests verify the in-memory repository implementation's CRUD operations,
/// pagination, filtering, and error handling paths.
/// </summary>
/// <remarks>
/// TDD RED PHASE: These tests verify the existing implementation provides comprehensive
/// coverage for all repository operations including edge cases and error conditions.
/// </remarks>
public class InMemoryPaymentRepositoryTests
{
    private readonly InMemoryPaymentRepository _sut;

    public InMemoryPaymentRepositoryTests()
    {
        _sut = new InMemoryPaymentRepository();
    }

    #region GetByHashAsync Tests

    [Fact]
    public async Task GetByHashAsync_WhenPaymentExists_ReturnsPayment()
    {
        // Arrange
        var payment = PaymentStateBuilder.Pending(5000).Build();
        await _sut.AddAsync(payment);

        // Act
        var result = await _sut.GetByHashAsync(payment.PaymentHash);

        // Assert
        result.Should().NotBeNull();
        result!.PaymentHash.Should().Be(payment.PaymentHash);
        result.AmountSat.Should().Be(payment.AmountSat);
        result.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task GetByHashAsync_WhenPaymentDoesNotExist_ReturnsNull()
    {
        // Arrange
        var nonExistentHash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

        // Act
        var result = await _sut.GetByHashAsync(nonExistentHash);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task GetByHashAsync_WhenPaymentHashIsWhitespace_ThrowsArgumentException(string? invalidHash)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _sut.GetByHashAsync(invalidHash!));
    }

    [Fact]
    public async Task GetByHashAsync_WhenPaymentHashIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _sut.GetByHashAsync(null!));
    }

    #endregion

    #region GetByStatusAsync Tests

    [Fact]
    public async Task GetByStatusAsync_WithMatchingStatus_ReturnsPayments()
    {
        // Arrange
        var pending1 = PaymentStateBuilder.Pending(1000).Build();
        var pending2 = PaymentStateBuilder.Pending(2000).Build();
        var succeeded = PaymentStateBuilder.Succeeded(3000).Build();

        await _sut.AddAsync(pending1);
        await _sut.AddAsync(pending2);
        await _sut.AddAsync(succeeded);

        // Act
        var result = await _sut.GetByStatusAsync(PaymentStatus.Pending);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.Status == PaymentStatus.Pending);
        result.Select(p => p.AmountSat).Should().Contain(new[] { 1000UL, 2000UL });
    }

    [Fact]
    public async Task GetByStatusAsync_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        var pending = PaymentStateBuilder.Pending().Build();
        await _sut.AddAsync(pending);

        // Act
        var result = await _sut.GetByStatusAsync(PaymentStatus.Failed);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByStatusAsync_WithLimitSmallerThanResults_ReturnsLimitedPayments()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            var payment = PaymentStateBuilder.Pending((ulong)(1000 * (i + 1))).Build();
            await _sut.AddAsync(payment);
        }

        // Act
        var result = await _sut.GetByStatusAsync(PaymentStatus.Pending, limit: 5);

        // Assert
        result.Should().HaveCount(5);
        result.Should().OnlyContain(p => p.Status == PaymentStatus.Pending);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetByStatusAsync_WhenLimitIsLessThanOne_ThrowsArgumentOutOfRangeException(int invalidLimit)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await _sut.GetByStatusAsync(PaymentStatus.Pending, invalidLimit));
    }

    [Fact]
    public async Task GetByStatusAsync_WithMultipleStatuses_ReturnsOnlyMatchingStatus()
    {
        // Arrange
        await _sut.AddAsync(PaymentStateBuilder.Pending().Build());
        await _sut.AddAsync(PaymentStateBuilder.Succeeded().Build());
        await _sut.AddAsync(PaymentStateBuilder.Failed().Build());
        await _sut.AddAsync(PaymentStateBuilder.Pending().Build());

        // Act
        var result = await _sut.GetByStatusAsync(PaymentStatus.Succeeded);

        // Assert
        result.Should().HaveCount(1);
        result[0].Status.Should().Be(PaymentStatus.Succeeded);
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithValidPayment_AddsSuccessfully()
    {
        // Arrange
        var payment = PaymentStateBuilder.Pending(5000)
            .WithDescription("Test payment")
            .Build();

        // Act
        var result = await _sut.AddAsync(payment);

        // Assert
        result.Should().NotBeNull();
        result.PaymentHash.Should().Be(payment.PaymentHash);

        // Verify it was added
        var retrieved = await _sut.GetByHashAsync(payment.PaymentHash);
        retrieved.Should().NotBeNull();
        retrieved!.PaymentHash.Should().Be(payment.PaymentHash);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateHash_ThrowsInvalidOperationException()
    {
        // Arrange
        var payment1 = PaymentStateBuilder.Pending(5000).Build();
        var payment2 = PaymentStateBuilder.Create()
            .WithPaymentHash(payment1.PaymentHash) // Same hash
            .WithAmount(10000)
            .Build();

        await _sut.AddAsync(payment1);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _sut.AddAsync(payment2));

        exception.Message.Should().Contain(payment1.PaymentHash);
        exception.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task AddAsync_WithNullPayment_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _sut.AddAsync(null!));
    }

    [Fact]
    public async Task AddAsync_WithMultiplePayments_AllAreStored()
    {
        // Arrange
        var payments = new[]
        {
            PaymentStateBuilder.Pending(1000).Build(),
            PaymentStateBuilder.Succeeded(2000).Build(),
            PaymentStateBuilder.Failed(3000).Build()
        };

        // Act
        foreach (var payment in payments)
        {
            await _sut.AddAsync(payment);
        }

        // Assert
        var allPayments = await _sut.GetAllAsync(limit: 100);
        allPayments.Should().HaveCount(3);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WhenPaymentExists_UpdatesSuccessfully()
    {
        // Arrange
        var payment = PaymentStateBuilder.Pending(5000).Build();
        await _sut.AddAsync(payment);

        var updatedPayment = new PaymentState
        {
            Id = payment.Id,
            PaymentHash = payment.PaymentHash,
            AmountSat = payment.AmountSat,
            Status = PaymentStatus.Succeeded,
            ConfirmedAt = DateTimeOffset.UtcNow,
            Preimage = "abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234",
            CreatedAt = payment.CreatedAt
        };

        // Act
        var result = await _sut.UpdateAsync(updatedPayment);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Succeeded);
        result.ConfirmedAt.Should().NotBeNull();
        result.Preimage.Should().Be("abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234");

        // Verify it was updated in repository
        var retrieved = await _sut.GetByHashAsync(payment.PaymentHash);
        retrieved!.Status.Should().Be(PaymentStatus.Succeeded);
    }

    [Fact]
    public async Task UpdateAsync_WhenPaymentDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        var payment = PaymentStateBuilder.Pending(5000).Build();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _sut.UpdateAsync(payment));

        exception.Message.Should().Contain(payment.PaymentHash);
        exception.Message.Should().Contain("does not exist");
    }

    [Fact]
    public async Task UpdateAsync_WithNullPayment_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _sut.UpdateAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_MultipleUpdates_LastUpdateWins()
    {
        // Arrange
        var payment = PaymentStateBuilder.Pending(5000).Build();
        await _sut.AddAsync(payment);

        // Act - Multiple updates
        var update1 = new PaymentState
        {
            Id = payment.Id,
            PaymentHash = payment.PaymentHash,
            AmountSat = payment.AmountSat,
            Status = PaymentStatus.Succeeded,
            CreatedAt = payment.CreatedAt
        };
        await _sut.UpdateAsync(update1);

        var update2 = new PaymentState
        {
            Id = payment.Id,
            PaymentHash = payment.PaymentHash,
            AmountSat = payment.AmountSat,
            Status = PaymentStatus.Refunded,
            CreatedAt = payment.CreatedAt
        };
        await _sut.UpdateAsync(update2);

        // Assert
        var retrieved = await _sut.GetByHashAsync(payment.PaymentHash);
        retrieved!.Status.Should().Be(PaymentStatus.Refunded);
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WhenPaymentExists_ReturnsTrue()
    {
        // Arrange
        var payment = PaymentStateBuilder.Pending().Build();
        await _sut.AddAsync(payment);

        // Act
        var result = await _sut.ExistsAsync(payment.PaymentHash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenPaymentDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var nonExistentHash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

        // Act
        var result = await _sut.ExistsAsync(nonExistentHash);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task ExistsAsync_WhenPaymentHashIsWhitespace_ThrowsArgumentException(string? invalidHash)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _sut.ExistsAsync(invalidHash!));
    }

    [Fact]
    public async Task ExistsAsync_WhenPaymentHashIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _sut.ExistsAsync(null!));
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithNoPayments_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithPayments_ReturnsAllPayments()
    {
        // Arrange
        var payment1 = PaymentStateBuilder.Pending(1000).Build();
        var payment2 = PaymentStateBuilder.Succeeded(2000).Build();
        var payment3 = PaymentStateBuilder.Failed(3000).Build();

        await _sut.AddAsync(payment1);
        await _sut.AddAsync(payment2);
        await _sut.AddAsync(payment3);

        // Act
        var result = await _sut.GetAllAsync(limit: 100);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByCreatedAtDescending()
    {
        // Arrange - Create payments with different timestamps
        var oldest = PaymentStateBuilder.Pending(1000)
            .WithCreatedAt(DateTimeOffset.UtcNow.AddDays(-3))
            .Build();
        var middle = PaymentStateBuilder.Pending(2000)
            .WithCreatedAt(DateTimeOffset.UtcNow.AddDays(-2))
            .Build();
        var newest = PaymentStateBuilder.Pending(3000)
            .WithCreatedAt(DateTimeOffset.UtcNow.AddDays(-1))
            .Build();

        await _sut.AddAsync(middle);
        await _sut.AddAsync(oldest);
        await _sut.AddAsync(newest);

        // Act
        var result = await _sut.GetAllAsync(limit: 100);

        // Assert
        result.Should().HaveCount(3);
        result[0].AmountSat.Should().Be(3000); // Newest first
        result[1].AmountSat.Should().Be(2000);
        result[2].AmountSat.Should().Be(1000); // Oldest last
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange - Create 10 payments
        var payments = new List<PaymentState>();
        for (int i = 0; i < 10; i++)
        {
            var payment = PaymentStateBuilder.Pending((ulong)(1000 * (i + 1)))
                .WithCreatedAt(DateTimeOffset.UtcNow.AddMinutes(-i))
                .Build();
            payments.Add(payment);
            await _sut.AddAsync(payment);
        }

        // Act - Get second page (skip 3, take 4)
        var result = await _sut.GetAllAsync(offset: 3, limit: 4);

        // Assert
        result.Should().HaveCount(4);
        // Should get items 3, 4, 5, 6 (0-indexed)
    }

    [Fact]
    public async Task GetAllAsync_WithOffsetGreaterThanCount_ReturnsEmptyList()
    {
        // Arrange
        await _sut.AddAsync(PaymentStateBuilder.Pending().Build());
        await _sut.AddAsync(PaymentStateBuilder.Pending().Build());

        // Act
        var result = await _sut.GetAllAsync(offset: 10, limit: 50);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    public async Task GetAllAsync_WithNegativeOffset_ThrowsArgumentOutOfRangeException(int invalidOffset)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await _sut.GetAllAsync(offset: invalidOffset));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public async Task GetAllAsync_WithInvalidLimit_ThrowsArgumentOutOfRangeException(int invalidLimit)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await _sut.GetAllAsync(limit: invalidLimit));
    }

    [Fact]
    public async Task GetAllAsync_WithDefaultPagination_ReturnsFirstFifty()
    {
        // Arrange - Add 100 payments
        for (int i = 0; i < 100; i++)
        {
            var payment = PaymentStateBuilder.Pending((ulong)(1000 * (i + 1)))
                .WithCreatedAt(DateTimeOffset.UtcNow.AddSeconds(-i))
                .Build();
            await _sut.AddAsync(payment);
        }

        // Act
        var result = await _sut.GetAllAsync(); // Default limit is 50

        // Assert
        result.Should().HaveCount(50);
    }

    #endregion

    #region GetByDateRangeAsync Tests

    [Fact]
    public async Task GetByDateRangeAsync_WithMatchingRange_ReturnsPayments()
    {
        // Arrange
        var baseTime = DateTimeOffset.UtcNow;
        var payment1 = PaymentStateBuilder.Pending(1000)
            .WithCreatedAt(baseTime.AddDays(-5))
            .Build();
        var payment2 = PaymentStateBuilder.Pending(2000)
            .WithCreatedAt(baseTime.AddDays(-3))
            .Build();
        var payment3 = PaymentStateBuilder.Pending(3000)
            .WithCreatedAt(baseTime.AddDays(-1))
            .Build();

        await _sut.AddAsync(payment1);
        await _sut.AddAsync(payment2);
        await _sut.AddAsync(payment3);

        // Act - Get payments from last 4 days
        var result = await _sut.GetByDateRangeAsync(
            baseTime.AddDays(-4),
            baseTime);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.AmountSat == 2000);
        result.Should().Contain(p => p.AmountSat == 3000);
        result.Should().NotContain(p => p.AmountSat == 1000);
    }

    [Fact]
    public async Task GetByDateRangeAsync_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        var payment = PaymentStateBuilder.Pending()
            .WithCreatedAt(DateTimeOffset.UtcNow.AddDays(-10))
            .Build();
        await _sut.AddAsync(payment);

        // Act - Search in a different time range
        var result = await _sut.GetByDateRangeAsync(
            DateTimeOffset.UtcNow.AddDays(-5),
            DateTimeOffset.UtcNow);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByDateRangeAsync_WithInvalidRange_ThrowsArgumentException()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow;
        var to = from.AddDays(-1); // to is before from

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _sut.GetByDateRangeAsync(from, to));

        exception.Message.Should().Contain("from");
        exception.Message.Should().Contain("to");
        exception.ParamName.Should().Be("from");
    }

    [Fact]
    public async Task GetByDateRangeAsync_OrdersByCreatedAtDescending()
    {
        // Arrange
        var baseTime = DateTimeOffset.UtcNow;
        var payment1 = PaymentStateBuilder.Pending(1000)
            .WithCreatedAt(baseTime.AddDays(-3))
            .Build();
        var payment2 = PaymentStateBuilder.Pending(2000)
            .WithCreatedAt(baseTime.AddDays(-2))
            .Build();
        var payment3 = PaymentStateBuilder.Pending(3000)
            .WithCreatedAt(baseTime.AddDays(-1))
            .Build();

        await _sut.AddAsync(payment1);
        await _sut.AddAsync(payment2);
        await _sut.AddAsync(payment3);

        // Act
        var result = await _sut.GetByDateRangeAsync(
            baseTime.AddDays(-4),
            baseTime);

        // Assert
        result.Should().HaveCount(3);
        result[0].AmountSat.Should().Be(3000); // Newest first
        result[1].AmountSat.Should().Be(2000);
        result[2].AmountSat.Should().Be(1000); // Oldest last
    }

    [Fact]
    public async Task GetByDateRangeAsync_WithInclusiveBoundaries_IncludesExactMatches()
    {
        // Arrange
        var exactTime = DateTimeOffset.UtcNow;
        var payment = PaymentStateBuilder.Pending()
            .WithCreatedAt(exactTime)
            .Build();
        await _sut.AddAsync(payment);

        // Act - Search with exact time as both boundaries
        var result = await _sut.GetByDateRangeAsync(exactTime, exactTime);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByDateRangeAsync_WithSameFromAndTo_ReturnsPaymentsAtExactTime()
    {
        // Arrange
        var exactTime = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var payment1 = PaymentStateBuilder.Pending(1000)
            .WithCreatedAt(exactTime)
            .Build();
        var payment2 = PaymentStateBuilder.Pending(2000)
            .WithCreatedAt(exactTime.AddHours(-1))
            .Build();
        var payment3 = PaymentStateBuilder.Pending(3000)
            .WithCreatedAt(exactTime.AddHours(1))
            .Build();

        await _sut.AddAsync(payment1);
        await _sut.AddAsync(payment2);
        await _sut.AddAsync(payment3);

        // Act
        var result = await _sut.GetByDateRangeAsync(exactTime, exactTime);

        // Assert
        result.Should().HaveCount(1);
        result[0].AmountSat.Should().Be(1000);
    }

    #endregion

    #region Integration Scenarios

    [Fact]
    public async Task CompletePaymentLifecycle_AddUpdateRetrieve_WorksCorrectly()
    {
        // Arrange - Create pending payment
        var payment = PaymentStateBuilder.Pending(5000)
            .WithDescription("Integration test payment")
            .Build();

        // Act - Add payment
        await _sut.AddAsync(payment);
        var exists = await _sut.ExistsAsync(payment.PaymentHash);
        exists.Should().BeTrue();

        // Update to succeeded
        var updatedPayment = new PaymentState
        {
            Id = payment.Id,
            PaymentHash = payment.PaymentHash,
            AmountSat = payment.AmountSat,
            Status = PaymentStatus.Succeeded,
            ConfirmedAt = DateTimeOffset.UtcNow,
            Preimage = "test_preimage_64_chars_0123456789abcdef0123456789abcdef012345",
            CreatedAt = payment.CreatedAt,
            Description = payment.Description
        };
        await _sut.UpdateAsync(updatedPayment);

        // Assert - Verify final state
        var retrieved = await _sut.GetByHashAsync(payment.PaymentHash);
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be(PaymentStatus.Succeeded);
        retrieved.ConfirmedAt.Should().NotBeNull();

        var succeededPayments = await _sut.GetByStatusAsync(PaymentStatus.Succeeded);
        succeededPayments.Should().HaveCount(1);
    }

    [Fact]
    public async Task MultiplePaymentTypes_FilteringAndPagination_WorksCorrectly()
    {
        // Arrange - Create various payment types
        var baseTime = DateTimeOffset.UtcNow;
        for (int i = 0; i < 5; i++)
        {
            await _sut.AddAsync(PaymentStateBuilder.Pending((ulong)(1000 * (i + 1)))
                .WithCreatedAt(baseTime.AddMinutes(-i))
                .Build());
        }
        for (int i = 0; i < 3; i++)
        {
            await _sut.AddAsync(PaymentStateBuilder.Succeeded((ulong)(2000 * (i + 1)))
                .WithCreatedAt(baseTime.AddMinutes(-i - 5))
                .Build());
        }
        for (int i = 0; i < 2; i++)
        {
            await _sut.AddAsync(PaymentStateBuilder.Failed((ulong)(3000 * (i + 1)))
                .WithCreatedAt(baseTime.AddMinutes(-i - 8))
                .Build());
        }

        // Act & Assert - Status filtering
        var pending = await _sut.GetByStatusAsync(PaymentStatus.Pending);
        pending.Should().HaveCount(5);

        var succeeded = await _sut.GetByStatusAsync(PaymentStatus.Succeeded);
        succeeded.Should().HaveCount(3);

        var failed = await _sut.GetByStatusAsync(PaymentStatus.Failed);
        failed.Should().HaveCount(2);

        // Pagination
        var firstPage = await _sut.GetAllAsync(offset: 0, limit: 5);
        firstPage.Should().HaveCount(5);

        var secondPage = await _sut.GetAllAsync(offset: 5, limit: 5);
        secondPage.Should().HaveCount(5);

        // Date range - Get only pending payments (created from baseTime to baseTime-4 minutes)
        var recent = await _sut.GetByDateRangeAsync(baseTime.AddMinutes(-4.5), baseTime);
        recent.Should().HaveCount(5); // Only pending payments (succeeded start at -5 minutes)
    }

    #endregion
}
