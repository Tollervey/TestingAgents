using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Breez.Sdk.Liquid.Extensions.PostgreSql.Data;
using Breez.Sdk.Liquid.Extensions.TestUtilities.Builders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Breez.Sdk.Liquid.Extensions.Integration.Tests.PostgreSql;

/// <summary>
/// Integration tests for PostgreSQL payment repository.
/// These tests use Testcontainers to spin up a real PostgreSQL instance.
/// </summary>
/// <remarks>
/// TDD RED PHASE: These tests are written BEFORE the implementation exists.
/// They define the expected behavior of the PostgreSQL repository and will fail
/// until the implementation (PostgreSqlPaymentRepository and PostgreSqlPaymentDbContext) is created.
/// </remarks>
[Collection(nameof(PostgreSqlCollection))]
public class PostgreSqlPaymentRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private IPaymentRepository _sut = null!;
    private PostgreSqlPaymentDbContext _dbContext = null!;

    public PostgreSqlPaymentRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Create a new DbContext for this test instance
        _dbContext = _fixture.CreateDbContext();

        // Create the repository
        _sut = new PostgreSqlPaymentRepository(_dbContext);

        // Ensure clean state for each test
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        // Clean up test data after each test
        if (_dbContext.Database.CanConnect())
        {
            await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE payment_states RESTART IDENTITY CASCADE");
        }

        await _dbContext.DisposeAsync();
    }

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithValidPayment_AddsSuccessfully()
    {
        // Arrange
        var payment = PaymentStateBuilder.Pending(5000)
            .WithDescription("PostgreSQL integration test payment")
            .WithKind(PaymentKind.Purchase)
            .WithMetadata("orderId", "ORDER-12345")
            .WithMetadata("source", "integration-test")
            .Build();

        // Act
        var result = await _sut.AddAsync(payment);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.PaymentHash.Should().Be(payment.PaymentHash);
        result.AmountSat.Should().Be(5000);
        result.Status.Should().Be(PaymentStatus.Pending);
        result.Description.Should().Be("PostgreSQL integration test payment");
        result.Kind.Should().Be(PaymentKind.Purchase);
        result.Metadata.Should().ContainKey("orderId").WhoseValue.Should().Be("ORDER-12345");
        result.Metadata.Should().ContainKey("source").WhoseValue.Should().Be("integration-test");

        // Verify it was persisted to database
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
    public async Task AddAsync_WithMetadata_PersistsMetadataCorrectly()
    {
        // Arrange
        var payment = PaymentStateBuilder.Pending(3000)
            .WithMetadata("userId", "usr_abc123")
            .WithMetadata("sessionId", "sess_xyz789")
            .WithMetadata("environment", "test")
            .Build();

        // Act
        await _sut.AddAsync(payment);

        // Assert
        var retrieved = await _sut.GetByHashAsync(payment.PaymentHash);
        retrieved.Should().NotBeNull();
        retrieved!.Metadata.Should().HaveCount(3);
        retrieved.Metadata["userId"].Should().Be("usr_abc123");
        retrieved.Metadata["sessionId"].Should().Be("sess_xyz789");
        retrieved.Metadata["environment"].Should().Be("test");
    }

    [Fact]
    public async Task AddAsync_WithNullableFields_PersistsCorrectly()
    {
        // Arrange
        var payment = PaymentStateBuilder.Pending(2000)
            .WithDescription(null)
            .WithInvoice(null)
            .WithCorrelationId(null)
            .Build();

        // Act
        var result = await _sut.AddAsync(payment);

        // Assert
        result.Description.Should().BeNull();
        result.Invoice.Should().BeNull();
        result.CorrelationId.Should().BeNull();
        result.ConfirmedAt.Should().BeNull();
        result.ExpiresAt.Should().BeNull();
        result.Preimage.Should().BeNull();
        result.FeeSat.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_WithAllPaymentKinds_PersistsCorrectly()
    {
        // Arrange & Act
        var customPayment = await _sut.AddAsync(PaymentStateBuilder.Pending(1000).WithKind(PaymentKind.Custom).Build());
        var paywallPayment = await _sut.AddAsync(PaymentStateBuilder.Pending(2000).WithKind(PaymentKind.Paywall).Build());
        var tipJarPayment = await _sut.AddAsync(PaymentStateBuilder.Pending(3000).WithKind(PaymentKind.TipJar).Build());
        var purchasePayment = await _sut.AddAsync(PaymentStateBuilder.Pending(4000).WithKind(PaymentKind.Purchase).Build());
        var subscriptionPayment = await _sut.AddAsync(PaymentStateBuilder.Pending(5000).WithKind(PaymentKind.Subscription).Build());

        // Assert
        customPayment.Kind.Should().Be(PaymentKind.Custom);
        paywallPayment.Kind.Should().Be(PaymentKind.Paywall);
        tipJarPayment.Kind.Should().Be(PaymentKind.TipJar);
        purchasePayment.Kind.Should().Be(PaymentKind.Purchase);
        subscriptionPayment.Kind.Should().Be(PaymentKind.Subscription);
    }

    #endregion

    #region GetByHashAsync Tests

    [Fact]
    public async Task GetByHashAsync_WhenPaymentExists_ReturnsPayment()
    {
        // Arrange
        var payment = PaymentStateBuilder.Pending(5000)
            .WithDescription("Test payment")
            .Build();
        await _sut.AddAsync(payment);

        // Act
        var result = await _sut.GetByHashAsync(payment.PaymentHash);

        // Assert
        result.Should().NotBeNull();
        result!.PaymentHash.Should().Be(payment.PaymentHash);
        result.AmountSat.Should().Be(5000);
        result.Status.Should().Be(PaymentStatus.Pending);
        result.Description.Should().Be("Test payment");
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

    [Fact]
    public async Task GetByHashAsync_WithMetadata_RetrievesMetadataCorrectly()
    {
        // Arrange
        var payment = PaymentStateBuilder.Pending(1000)
            .WithMetadata("key1", "value1")
            .WithMetadata("key2", "value2")
            .Build();
        await _sut.AddAsync(payment);

        // Act
        var result = await _sut.GetByHashAsync(payment.PaymentHash);

        // Assert
        result.Should().NotBeNull();
        result!.Metadata.Should().HaveCount(2);
        result.Metadata["key1"].Should().Be("value1");
        result.Metadata["key2"].Should().Be("value2");
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
            FeeSat = 100,
            CreatedAt = payment.CreatedAt
        };

        // Act
        var result = await _sut.UpdateAsync(updatedPayment);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Succeeded);
        result.ConfirmedAt.Should().NotBeNull();
        result.Preimage.Should().Be("abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234");
        result.FeeSat.Should().Be(100);

        // Verify it was updated in database
        var retrieved = await _sut.GetByHashAsync(payment.PaymentHash);
        retrieved!.Status.Should().Be(PaymentStatus.Succeeded);
        retrieved.ConfirmedAt.Should().NotBeNull();
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
    public async Task UpdateAsync_TransitionsThroughStatuses_PersistsCorrectly()
    {
        // Arrange
        var payment = PaymentStateBuilder.Pending(3000).Build();
        await _sut.AddAsync(payment);

        // Act - Transition to Succeeded
        var succeededPayment = new PaymentState
        {
            Id = payment.Id,
            PaymentHash = payment.PaymentHash,
            AmountSat = payment.AmountSat,
            Status = PaymentStatus.Succeeded,
            ConfirmedAt = DateTimeOffset.UtcNow,
            CreatedAt = payment.CreatedAt
        };
        await _sut.UpdateAsync(succeededPayment);

        // Transition to Refunded
        var refundedPayment = new PaymentState
        {
            Id = payment.Id,
            PaymentHash = payment.PaymentHash,
            AmountSat = payment.AmountSat,
            Status = PaymentStatus.Refunded,
            ConfirmedAt = succeededPayment.ConfirmedAt,
            CreatedAt = payment.CreatedAt
        };
        await _sut.UpdateAsync(refundedPayment);

        // Assert
        var final = await _sut.GetByHashAsync(payment.PaymentHash);
        final!.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public async Task UpdateAsync_WithMetadataChanges_PersistsCorrectly()
    {
        // Arrange
        var payment = PaymentStateBuilder.Pending(2000)
            .WithMetadata("status", "processing")
            .Build();
        await _sut.AddAsync(payment);

        // Act - Update with new metadata
        var updatedMetadata = new Dictionary<string, string>
        {
            { "status", "completed" },
            { "completedBy", "system" }
        };
        var updatedPayment = new PaymentState
        {
            Id = payment.Id,
            PaymentHash = payment.PaymentHash,
            AmountSat = payment.AmountSat,
            Status = PaymentStatus.Succeeded,
            CreatedAt = payment.CreatedAt,
            Metadata = updatedMetadata
        };
        await _sut.UpdateAsync(updatedPayment);

        // Assert
        var retrieved = await _sut.GetByHashAsync(payment.PaymentHash);
        retrieved!.Metadata.Should().HaveCount(2);
        retrieved.Metadata["status"].Should().Be("completed");
        retrieved.Metadata["completedBy"].Should().Be("system");
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

    [Fact]
    public async Task GetByStatusAsync_AllStatuses_ReturnCorrectPayments()
    {
        // Arrange
        await _sut.AddAsync(PaymentStateBuilder.Pending().Build());
        await _sut.AddAsync(PaymentStateBuilder.Succeeded().Build());
        await _sut.AddAsync(PaymentStateBuilder.Failed().Build());
        await _sut.AddAsync(PaymentStateBuilder.Create().WithStatus(PaymentStatus.Expired).WithAmount(4000).Build());
        await _sut.AddAsync(PaymentStateBuilder.Create().WithStatus(PaymentStatus.Refunded).WithAmount(5000).Build());

        // Act & Assert
        var pending = await _sut.GetByStatusAsync(PaymentStatus.Pending);
        pending.Should().HaveCount(1);

        var succeeded = await _sut.GetByStatusAsync(PaymentStatus.Succeeded);
        succeeded.Should().HaveCount(1);

        var failed = await _sut.GetByStatusAsync(PaymentStatus.Failed);
        failed.Should().HaveCount(1);

        var expired = await _sut.GetByStatusAsync(PaymentStatus.Expired);
        expired.Should().HaveCount(1);

        var refunded = await _sut.GetByStatusAsync(PaymentStatus.Refunded);
        refunded.Should().HaveCount(1);
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
        for (int i = 0; i < 10; i++)
        {
            var payment = PaymentStateBuilder.Pending((ulong)(1000 * (i + 1)))
                .WithCreatedAt(DateTimeOffset.UtcNow.AddMinutes(-i))
                .Build();
            await _sut.AddAsync(payment);
        }

        // Act - Get second page (skip 3, take 4)
        var result = await _sut.GetAllAsync(offset: 3, limit: 4);

        // Assert
        result.Should().HaveCount(4);
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
        var exactTime = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero);
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

    [Fact]
    public async Task GetByDateRangeAsync_WithTimezoneAwareDates_HandlesCorrectly()
    {
        // Arrange
        var utcTime = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var estTime = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.FromHours(-5));

        var payment1 = PaymentStateBuilder.Pending(1000)
            .WithCreatedAt(utcTime)
            .Build();
        var payment2 = PaymentStateBuilder.Pending(2000)
            .WithCreatedAt(estTime)
            .Build();

        await _sut.AddAsync(payment1);
        await _sut.AddAsync(payment2);

        // Act - Query using UTC range
        var result = await _sut.GetByDateRangeAsync(
            utcTime.AddHours(-1),
            utcTime.AddHours(1));

        // Assert - Should include payment1 (UTC) but not payment2 (EST is 5 hours ahead in absolute time)
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
            .WithKind(PaymentKind.Purchase)
            .WithMetadata("orderId", "ORDER-123")
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
            FeeSat = 50,
            CreatedAt = payment.CreatedAt,
            Description = payment.Description,
            Kind = payment.Kind,
            Metadata = payment.Metadata
        };
        await _sut.UpdateAsync(updatedPayment);

        // Assert - Verify final state
        var retrieved = await _sut.GetByHashAsync(payment.PaymentHash);
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be(PaymentStatus.Succeeded);
        retrieved.ConfirmedAt.Should().NotBeNull();
        retrieved.Preimage.Should().NotBeNullOrEmpty();
        retrieved.FeeSat.Should().Be(50);

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

        // Date range
        var recent = await _sut.GetByDateRangeAsync(baseTime.AddMinutes(-4.5), baseTime);
        recent.Should().HaveCount(5);
    }

    [Fact]
    public async Task ConcurrentAccess_MultipleOperations_HandlesCorrectly()
    {
        // Arrange
        var payments = Enumerable.Range(1, 10)
            .Select(i => PaymentStateBuilder.Pending((ulong)(1000 * i)).Build())
            .ToList();

        // Act - Add payments concurrently (simulating multiple users/threads)
        var addTasks = payments.Select(p => _sut.AddAsync(p));
        await Task.WhenAll(addTasks);

        // Assert
        var allPayments = await _sut.GetAllAsync(limit: 100);
        allPayments.Should().HaveCount(10);

        // Verify each payment exists
        foreach (var payment in payments)
        {
            var exists = await _sut.ExistsAsync(payment.PaymentHash);
            exists.Should().BeTrue();
        }
    }

    [Fact]
    public async Task LargeMetadata_PersistsAndRetrieves_Correctly()
    {
        // Arrange - Create payment with large metadata
        var payment = PaymentStateBuilder.Pending(5000).Build();

        // Add multiple metadata entries
        for (int i = 0; i < 50; i++)
        {
            payment.Metadata[$"key_{i}"] = $"value_{i}_with_some_longer_content_to_test_storage";
        }

        // Act
        await _sut.AddAsync(payment);
        var retrieved = await _sut.GetByHashAsync(payment.PaymentHash);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Metadata.Should().HaveCount(50);
        for (int i = 0; i < 50; i++)
        {
            retrieved.Metadata[$"key_{i}"].Should().Be($"value_{i}_with_some_longer_content_to_test_storage");
        }
    }

    #endregion

    #region Edge Cases and Database-Specific Tests

    [Fact]
    public async Task PaymentHash_IsCaseSensitive()
    {
        // Arrange
        var lowerCaseHash = "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789";
        var upperCaseHash = "ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789";

        var payment1 = PaymentStateBuilder.Pending(1000)
            .WithPaymentHash(lowerCaseHash)
            .Build();
        var payment2 = PaymentStateBuilder.Pending(2000)
            .WithPaymentHash(upperCaseHash)
            .Build();

        // Act
        await _sut.AddAsync(payment1);
        await _sut.AddAsync(payment2);

        // Assert - Both should exist independently
        var retrieved1 = await _sut.GetByHashAsync(lowerCaseHash);
        var retrieved2 = await _sut.GetByHashAsync(upperCaseHash);

        retrieved1.Should().NotBeNull();
        retrieved2.Should().NotBeNull();
        retrieved1!.AmountSat.Should().Be(1000);
        retrieved2!.AmountSat.Should().Be(2000);

        var allPayments = await _sut.GetAllAsync(limit: 100);
        allPayments.Should().HaveCount(2);
    }

    [Fact]
    public async Task EmptyMetadata_PersistsAsEmptyDictionary()
    {
        // Arrange
        var payment = PaymentStateBuilder.Pending(1000).Build();
        // Ensure metadata is empty
        payment.Metadata.Clear();

        // Act
        await _sut.AddAsync(payment);
        var retrieved = await _sut.GetByHashAsync(payment.PaymentHash);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Metadata.Should().NotBeNull();
        retrieved.Metadata.Should().BeEmpty();
    }

    [Fact]
    public async Task LargeAmounts_UlongMaxValue_HandlesCorrectly()
    {
        // Arrange - Test with very large amounts (near ulong.MaxValue)
        var maxAmount = ulong.MaxValue - 1000; // Close to max
        var payment = PaymentStateBuilder.Pending(maxAmount).Build();

        // Act
        await _sut.AddAsync(payment);
        var retrieved = await _sut.GetByHashAsync(payment.PaymentHash);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.AmountSat.Should().Be(maxAmount);
    }

    [Fact]
    public async Task MinimumAmount_ZeroSatoshis_HandlesCorrectly()
    {
        // Arrange - Test with zero amount (edge case)
        var payment = PaymentStateBuilder.Pending(0).Build();

        // Act
        await _sut.AddAsync(payment);
        var retrieved = await _sut.GetByHashAsync(payment.PaymentHash);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.AmountSat.Should().Be(0);
    }

    [Fact]
    public async Task LongDescription_MaxLength_HandlesCorrectly()
    {
        // Arrange - Test with 500 character description (max length from config)
        var longDescription = new string('X', 500);
        var payment = PaymentStateBuilder.Pending(1000)
            .WithDescription(longDescription)
            .Build();

        // Act
        await _sut.AddAsync(payment);
        var retrieved = await _sut.GetByHashAsync(payment.PaymentHash);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Description.Should().Be(longDescription);
        retrieved.Description!.Length.Should().Be(500);
    }

    [Fact]
    public async Task UniqueConstraint_OnPaymentHash_IsEnforced()
    {
        // Arrange
        var paymentHash = "unique0123456789abcdef0123456789abcdef0123456789abcdef0123456789";
        var payment1 = PaymentStateBuilder.Pending(1000)
            .WithPaymentHash(paymentHash)
            .Build();

        await _sut.AddAsync(payment1);

        // Act & Assert - Attempting to add another payment with same hash should fail
        var payment2 = PaymentStateBuilder.Pending(2000)
            .WithPaymentHash(paymentHash)
            .Build();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _sut.AddAsync(payment2));

        exception.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task DateTimeOffset_PreservesTimezoneInformation()
    {
        // Arrange - Create payment with specific timezone
        var pstTime = new DateTimeOffset(2024, 1, 15, 9, 30, 0, TimeSpan.FromHours(-8));
        var payment = PaymentStateBuilder.Pending(1000)
            .WithCreatedAt(pstTime)
            .Build();

        // Act
        await _sut.AddAsync(payment);
        var retrieved = await _sut.GetByHashAsync(payment.PaymentHash);

        // Assert - Should maintain the same absolute time
        retrieved.Should().NotBeNull();
        retrieved!.CreatedAt.ToUniversalTime().Should().Be(pstTime.ToUniversalTime());
    }

    [Fact]
    public async Task GetAllAsync_WithExactLimit_ReturnsExactCount()
    {
        // Arrange - Add exactly 25 payments
        for (int i = 0; i < 25; i++)
        {
            await _sut.AddAsync(PaymentStateBuilder.Pending((ulong)(1000 * (i + 1))).Build());
        }

        // Act
        var result = await _sut.GetAllAsync(offset: 0, limit: 25);

        // Assert
        result.Should().HaveCount(25);
    }

    [Fact]
    public async Task GetByStatusAsync_WithLimit100_ReturnsDefaultMax()
    {
        // Arrange - Add 150 payments with same status
        for (int i = 0; i < 150; i++)
        {
            await _sut.AddAsync(PaymentStateBuilder.Pending((ulong)(100 * (i + 1))).Build());
        }

        // Act - Default limit is 100
        var result = await _sut.GetByStatusAsync(PaymentStatus.Pending);

        // Assert
        result.Should().HaveCount(100);
    }

    [Fact]
    public async Task SpecialCharacters_InDescription_HandlesCorrectly()
    {
        // Arrange - Test with special characters
        var specialDescription = "Payment for: <script>alert('test')</script> & \"quotes\" 'apostrophes' 日本語";
        var payment = PaymentStateBuilder.Pending(1000)
            .WithDescription(specialDescription)
            .Build();

        // Act
        await _sut.AddAsync(payment);
        var retrieved = await _sut.GetByHashAsync(payment.PaymentHash);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Description.Should().Be(specialDescription);
    }

    [Fact]
    public async Task Invoice_WithBolt11String_PersistsCorrectly()
    {
        // Arrange - Test with realistic BOLT11 invoice
        var bolt11 = "lnbc1500n1p3s9xm9pp5qqqsyqcyq5rqwzqfqqqsyqcyq5rqwzqfqqqsyqcyq5rqwzqfqypqdpl2pkx2ctnv5sxxmmwwd5kgetjypeh2ursdae8g6twvus8g6rfwvs8qun0dfjkxaq8rkx3yf5tcsyz3d73gafnh3cax9rn449d9p5uxz9ezhhypd0elx87sjle52x86fux2ypatgddc6k63n7erqz25le42c4u4ecky03ylcqca784w";
        var payment = PaymentStateBuilder.Pending(1000)
            .WithInvoice(bolt11)
            .Build();

        // Act
        await _sut.AddAsync(payment);
        var retrieved = await _sut.GetByHashAsync(payment.PaymentHash);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Invoice.Should().Be(bolt11);
    }

    [Fact]
    public async Task CorrelationId_WithGuid_PersistsCorrectly()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var payment = PaymentStateBuilder.Pending(1000)
            .WithCorrelationId(correlationId)
            .Build();

        // Act
        await _sut.AddAsync(payment);
        var retrieved = await _sut.GetByHashAsync(payment.PaymentHash);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.CorrelationId.Should().Be(correlationId);
    }

    #endregion
}

/// <summary>
/// xUnit collection fixture for PostgreSQL integration tests.
/// This ensures the PostgreSQL container is shared across all test classes in the collection.
/// </summary>
[CollectionDefinition(nameof(PostgreSqlCollection))]
public class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
}

/// <summary>
/// Fixture for managing PostgreSQL container lifecycle for integration tests.
/// </summary>
public class PostgreSqlFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private string? _connectionString;

    /// <summary>
    /// Gets the connection string for the test PostgreSQL instance.
    /// </summary>
    public string ConnectionString => _connectionString ?? throw new InvalidOperationException("Container not initialized");

    public async Task InitializeAsync()
    {
        // Create and start PostgreSQL container
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("breez_test")
            .WithUsername("testuser")
            .WithPassword("testpass123")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();

        // Create a temporary DbContext to apply migrations
        using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates a new DbContext instance for testing.
    /// </summary>
    public PostgreSqlPaymentDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<PostgreSqlPaymentDbContext>();
        optionsBuilder.UseNpgsql(ConnectionString);
        return new PostgreSqlPaymentDbContext(optionsBuilder.Options);
    }
}
