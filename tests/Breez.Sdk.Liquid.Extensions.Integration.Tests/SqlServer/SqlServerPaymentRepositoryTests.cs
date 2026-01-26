using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Breez.Sdk.Liquid.Extensions.SqlServer.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace Breez.Sdk.Liquid.Extensions.Integration.Tests.SqlServer;

/// <summary>
/// Integration tests for SQL Server payment repository implementation.
/// Uses Testcontainers to spin up a real SQL Server instance for testing.
/// </summary>
[Collection("SqlServer Collection")]
public class SqlServerPaymentRepositoryTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private SqlServerPaymentDbContext _dbContext = null!;
    private IPaymentRepository _sut = null!;

    public SqlServerPaymentRepositoryTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Create a fresh DbContext for each test
        var options = new DbContextOptionsBuilder<SqlServerPaymentDbContext>()
            .UseSqlServer(_fixture.ConnectionString)
            .Options;

        _dbContext = new SqlServerPaymentDbContext(options);

        // Ensure database is created and clean
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.Database.EnsureCreatedAsync();

        _sut = new SqlServerPaymentRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithValidPayment_ShouldAddPaymentToDatabase()
    {
        // Arrange
        var payment = CreateTestPayment("hash123");

        // Act
        var result = await _sut.AddAsync(payment);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(payment.Id);
        result.PaymentHash.Should().Be("hash123");

        var exists = await _sut.ExistsAsync("hash123");
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_WithDuplicateHash_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payment1 = CreateTestPayment("duplicate-hash");
        var payment2 = CreateTestPayment("duplicate-hash");

        await _sut.AddAsync(payment1);

        // Act
        var act = async () => await _sut.AddAsync(payment2);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*payment with the same hash already exists*");
    }

    [Fact]
    public async Task AddAsync_WithNullPayment_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _sut.AddAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("payment");
    }

    [Fact]
    public async Task AddAsync_WithMetadata_ShouldPersistMetadataCorrectly()
    {
        // Arrange
        var payment = CreateTestPayment("hash-with-metadata");
        payment.Metadata["orderId"] = "ORDER-123";
        payment.Metadata["source"] = "mobile_app";
        payment.Metadata["userId"] = "usr_abc123";

        // Act
        await _sut.AddAsync(payment);

        // Assert
        var retrieved = await _sut.GetByHashAsync("hash-with-metadata");
        retrieved.Should().NotBeNull();
        retrieved!.Metadata.Should().HaveCount(3);
        retrieved.Metadata["orderId"].Should().Be("ORDER-123");
        retrieved.Metadata["source"].Should().Be("mobile_app");
        retrieved.Metadata["userId"].Should().Be("usr_abc123");
    }

    #endregion

    #region GetByHashAsync Tests

    [Fact]
    public async Task GetByHashAsync_WithExistingHash_ShouldReturnPayment()
    {
        // Arrange
        var payment = CreateTestPayment("existing-hash", PaymentStatus.Succeeded);
        await _sut.AddAsync(payment);

        // Act
        var result = await _sut.GetByHashAsync("existing-hash");

        // Assert
        result.Should().NotBeNull();
        result!.PaymentHash.Should().Be("existing-hash");
        result.Status.Should().Be(PaymentStatus.Succeeded);
        result.AmountSat.Should().Be(10000);
    }

    [Fact]
    public async Task GetByHashAsync_WithNonExistentHash_ShouldReturnNull()
    {
        // Act
        var result = await _sut.GetByHashAsync("non-existent-hash");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByHashAsync_WithNullHash_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _sut.GetByHashAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("paymentHash");
    }

    [Fact]
    public async Task GetByHashAsync_WithWhitespaceHash_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _sut.GetByHashAsync("   ");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("paymentHash");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingPayment_ShouldUpdateSuccessfully()
    {
        // Arrange
        var payment = CreateTestPayment("update-hash", PaymentStatus.Pending);
        await _sut.AddAsync(payment);

        // Update the payment properties
        payment.Status = PaymentStatus.Succeeded;
        payment.ConfirmedAt = DateTimeOffset.UtcNow;
        payment.Preimage = "preimage-secret-value";

        // Act
        var result = await _sut.UpdateAsync(payment);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Succeeded);
        result.ConfirmedAt.Should().NotBeNull();
        result.Preimage.Should().Be("preimage-secret-value");

        // Verify update persisted
        var retrieved = await _sut.GetByHashAsync("update-hash");
        retrieved!.Status.Should().Be(PaymentStatus.Succeeded);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentPayment_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payment = CreateTestPayment("non-existent-hash");

        // Act
        var act = async () => await _sut.UpdateAsync(payment);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*payment does not exist*");
    }

    [Fact]
    public async Task UpdateAsync_WithNullPayment_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _sut.UpdateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("payment");
    }

    [Fact]
    public async Task UpdateAsync_WithModifiedMetadata_ShouldPersistChanges()
    {
        // Arrange
        var payment = CreateTestPayment("metadata-update-hash");
        payment.Metadata["key1"] = "value1";
        await _sut.AddAsync(payment);

        payment.Metadata["key2"] = "value2";
        payment.Metadata["key1"] = "updated-value1";

        // Act
        await _sut.UpdateAsync(payment);

        // Assert
        var retrieved = await _sut.GetByHashAsync("metadata-update-hash");
        retrieved!.Metadata.Should().HaveCount(2);
        retrieved.Metadata["key1"].Should().Be("updated-value1");
        retrieved.Metadata["key2"].Should().Be("value2");
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithExistingHash_ShouldReturnTrue()
    {
        // Arrange
        var payment = CreateTestPayment("exists-hash");
        await _sut.AddAsync(payment);

        // Act
        var result = await _sut.ExistsAsync("exists-hash");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentHash_ShouldReturnFalse()
    {
        // Act
        var result = await _sut.ExistsAsync("non-existent-hash");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithNullHash_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _sut.ExistsAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("paymentHash");
    }

    [Fact]
    public async Task ExistsAsync_WithWhitespaceHash_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _sut.ExistsAsync("   ");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("paymentHash");
    }

    #endregion

    #region GetByStatusAsync Tests

    [Fact]
    public async Task GetByStatusAsync_WithMatchingPayments_ShouldReturnFiltered()
    {
        // Arrange
        await _sut.AddAsync(CreateTestPayment("pending1", PaymentStatus.Pending));
        await _sut.AddAsync(CreateTestPayment("pending2", PaymentStatus.Pending));
        await _sut.AddAsync(CreateTestPayment("succeeded1", PaymentStatus.Succeeded));
        await _sut.AddAsync(CreateTestPayment("failed1", PaymentStatus.Failed));

        // Act
        var result = await _sut.GetByStatusAsync(PaymentStatus.Pending);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.Status == PaymentStatus.Pending);
        result.Select(p => p.PaymentHash).Should().Contain(new[] { "pending1", "pending2" });
    }

    [Fact]
    public async Task GetByStatusAsync_WithLimit_ShouldRespectLimit()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            await _sut.AddAsync(CreateTestPayment($"pending-{i}", PaymentStatus.Pending));
        }

        // Act
        var result = await _sut.GetByStatusAsync(PaymentStatus.Pending, limit: 3);

        // Assert
        result.Should().HaveCount(3);
        result.Should().OnlyContain(p => p.Status == PaymentStatus.Pending);
    }

    [Fact]
    public async Task GetByStatusAsync_WithNoMatches_ShouldReturnEmptyList()
    {
        // Arrange
        await _sut.AddAsync(CreateTestPayment("succeeded1", PaymentStatus.Succeeded));

        // Act
        var result = await _sut.GetByStatusAsync(PaymentStatus.Refunded);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByStatusAsync_WithInvalidLimit_ShouldThrowArgumentOutOfRangeException()
    {
        // Act
        var act = async () => await _sut.GetByStatusAsync(PaymentStatus.Pending, limit: 0);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("limit");
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldOrderByCreatedAtDescending()
    {
        // Arrange
        await _sut.AddAsync(CreateTestPayment("pending1", PaymentStatus.Pending, createdAt: DateTimeOffset.UtcNow.AddMinutes(-10)));
        await _sut.AddAsync(CreateTestPayment("pending2", PaymentStatus.Pending, createdAt: DateTimeOffset.UtcNow.AddMinutes(-5)));
        await _sut.AddAsync(CreateTestPayment("pending3", PaymentStatus.Pending, createdAt: DateTimeOffset.UtcNow));

        // Act
        var result = await _sut.GetByStatusAsync(PaymentStatus.Pending);

        // Assert
        result.Should().HaveCount(3);
        result[0].PaymentHash.Should().Be("pending3"); // Newest first
        result[1].PaymentHash.Should().Be("pending2");
        result[2].PaymentHash.Should().Be("pending1"); // Oldest last
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithNoParameters_ShouldReturnFirstFiftyOrderedByCreatedAtDesc()
    {
        // Arrange
        for (int i = 0; i < 60; i++)
        {
            await _sut.AddAsync(CreateTestPayment($"payment-{i:D3}", createdAt: DateTimeOffset.UtcNow.AddSeconds(-i)));
        }

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(50);
        result[0].PaymentHash.Should().Be("payment-000"); // Newest first
        result[49].PaymentHash.Should().Be("payment-049");
    }

    [Fact]
    public async Task GetAllAsync_WithOffsetAndLimit_ShouldPaginateCorrectly()
    {
        // Arrange
        for (int i = 0; i < 25; i++)
        {
            await _sut.AddAsync(CreateTestPayment($"payment-{i:D2}", createdAt: DateTimeOffset.UtcNow.AddSeconds(-i)));
        }

        // Act - Get second page of 10 records
        var result = await _sut.GetAllAsync(offset: 10, limit: 10);

        // Assert
        result.Should().HaveCount(10);
        result[0].PaymentHash.Should().Be("payment-10");
        result[9].PaymentHash.Should().Be("payment-19");
    }

    [Fact]
    public async Task GetAllAsync_WithOffsetBeyondTotal_ShouldReturnEmptyList()
    {
        // Arrange
        await _sut.AddAsync(CreateTestPayment("payment-1"));

        // Act
        var result = await _sut.GetAllAsync(offset: 100);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithNegativeOffset_ShouldThrowArgumentOutOfRangeException()
    {
        // Act
        var act = async () => await _sut.GetAllAsync(offset: -1);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("offset");
    }

    [Fact]
    public async Task GetAllAsync_WithInvalidLimit_ShouldThrowArgumentOutOfRangeException()
    {
        // Act
        var act = async () => await _sut.GetAllAsync(limit: 0);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithParameterName("limit");
    }

    #endregion

    #region GetByDateRangeAsync Tests

    [Fact]
    public async Task GetByDateRangeAsync_WithMatchingPayments_ShouldReturnPaymentsInRange()
    {
        // Arrange
        var baseDate = DateTimeOffset.UtcNow.Date;

        await _sut.AddAsync(CreateTestPayment("before", createdAt: baseDate.AddDays(-2)));
        await _sut.AddAsync(CreateTestPayment("in-range-1", createdAt: baseDate.AddDays(-1)));
        await _sut.AddAsync(CreateTestPayment("in-range-2", createdAt: baseDate));
        await _sut.AddAsync(CreateTestPayment("after", createdAt: baseDate.AddDays(2)));

        // Act
        var result = await _sut.GetByDateRangeAsync(
            from: baseDate.AddDays(-1),
            to: baseDate);

        // Assert
        result.Should().HaveCount(2);
        result.Select(p => p.PaymentHash).Should().Contain(new[] { "in-range-1", "in-range-2" });
    }

    [Fact]
    public async Task GetByDateRangeAsync_ShouldOrderByCreatedAtDescending()
    {
        // Arrange
        var baseDate = DateTimeOffset.UtcNow.Date;

        await _sut.AddAsync(CreateTestPayment("oldest", createdAt: baseDate.AddHours(1)));
        await _sut.AddAsync(CreateTestPayment("middle", createdAt: baseDate.AddHours(5)));
        await _sut.AddAsync(CreateTestPayment("newest", createdAt: baseDate.AddHours(10)));

        // Act
        var result = await _sut.GetByDateRangeAsync(
            from: baseDate,
            to: baseDate.AddDays(1));

        // Assert
        result.Should().HaveCount(3);
        result[0].PaymentHash.Should().Be("newest");
        result[1].PaymentHash.Should().Be("middle");
        result[2].PaymentHash.Should().Be("oldest");
    }

    [Fact]
    public async Task GetByDateRangeAsync_WithNoMatches_ShouldReturnEmptyList()
    {
        // Arrange
        var baseDate = DateTimeOffset.UtcNow.Date;
        await _sut.AddAsync(CreateTestPayment("payment", createdAt: baseDate));

        // Act
        var result = await _sut.GetByDateRangeAsync(
            from: baseDate.AddDays(10),
            to: baseDate.AddDays(20));

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByDateRangeAsync_WithFromGreaterThanTo_ShouldThrowArgumentException()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow;
        var to = from.AddDays(-1);

        // Act
        var act = async () => await _sut.GetByDateRangeAsync(from, to);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*from*greater than*to*");
    }

    [Fact]
    public async Task GetByDateRangeAsync_WithInclusiveBoundaries_ShouldIncludeBoundaryDates()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.Date;
        var to = from.AddDays(1);

        await _sut.AddAsync(CreateTestPayment("at-from", createdAt: from));
        await _sut.AddAsync(CreateTestPayment("at-to", createdAt: to));

        // Act
        var result = await _sut.GetByDateRangeAsync(from, to);

        // Assert
        result.Should().HaveCount(2);
        result.Select(p => p.PaymentHash).Should().Contain(new[] { "at-from", "at-to" });
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public async Task Repository_WithMultiplePaymentKinds_ShouldPersistAndRetrieveCorrectly()
    {
        // Arrange
        var kinds = new[]
        {
            PaymentKind.Custom,
            PaymentKind.Paywall,
            PaymentKind.TipJar,
            PaymentKind.Purchase,
            PaymentKind.Subscription
        };

        foreach (var kind in kinds)
        {
            await _sut.AddAsync(CreateTestPayment($"payment-{kind}", kind: kind));
        }

        // Act & Assert
        foreach (var kind in kinds)
        {
            var payment = await _sut.GetByHashAsync($"payment-{kind}");
            payment.Should().NotBeNull();
            payment!.Kind.Should().Be(kind);
        }
    }

    [Fact]
    public async Task Repository_WithNullableFields_ShouldHandleNullsCorrectly()
    {
        // Arrange
        var payment = new PaymentState
        {
            PaymentHash = "nullable-test",
            AmountSat = 5000,
            Description = null,
            Invoice = null,
            ConfirmedAt = null,
            ExpiresAt = null,
            Preimage = null,
            FeeSat = null,
            CorrelationId = null
        };

        // Act
        await _sut.AddAsync(payment);

        // Assert
        var retrieved = await _sut.GetByHashAsync("nullable-test");
        retrieved.Should().NotBeNull();
        retrieved!.Description.Should().BeNull();
        retrieved.Invoice.Should().BeNull();
        retrieved.ConfirmedAt.Should().BeNull();
        retrieved.ExpiresAt.Should().BeNull();
        retrieved.Preimage.Should().BeNull();
        retrieved.FeeSat.Should().BeNull();
        retrieved.CorrelationId.Should().BeNull();
    }

    [Fact]
    public async Task Repository_WithEmptyMetadata_ShouldPersistEmptyDictionary()
    {
        // Arrange
        var payment = CreateTestPayment("empty-metadata");
        payment.Metadata.Clear();

        // Act
        await _sut.AddAsync(payment);

        // Assert
        var retrieved = await _sut.GetByHashAsync("empty-metadata");
        retrieved.Should().NotBeNull();
        retrieved!.Metadata.Should().BeEmpty();
    }

    [Fact]
    public async Task Repository_WithConcurrentOperations_ShouldMaintainConsistency()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act - Add 10 payments concurrently
        for (int i = 0; i < 10; i++)
        {
            var hash = $"concurrent-{i}";
            tasks.Add(Task.Run(async () =>
            {
                await _sut.AddAsync(CreateTestPayment(hash));
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var allPayments = await _sut.GetAllAsync(limit: 100);
        allPayments.Should().HaveCount(10);
        allPayments.Select(p => p.PaymentHash).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Repository_WithLargeAmountValues_ShouldHandleUlongCorrectly()
    {
        // Arrange
        var payment = new PaymentState
        {
            PaymentHash = "large-amount",
            AmountSat = ulong.MaxValue,
            FeeSat = ulong.MaxValue - 1000
        };

        // Act
        await _sut.AddAsync(payment);

        // Assert
        var retrieved = await _sut.GetByHashAsync("large-amount");
        retrieved.Should().NotBeNull();
        retrieved!.AmountSat.Should().Be(ulong.MaxValue);
        retrieved.FeeSat.Should().Be(ulong.MaxValue - 1000);
    }

    #endregion

    #region Helper Methods

    private static PaymentState CreateTestPayment(
        string hash,
        PaymentStatus status = PaymentStatus.Pending,
        ulong amountSat = 10000,
        DateTimeOffset? createdAt = null,
        PaymentKind? kind = null)
    {
        return new PaymentState
        {
            PaymentHash = hash,
            Status = status,
            AmountSat = amountSat,
            Description = $"Test payment for {hash}",
            Invoice = $"lnbc{amountSat}...",
            Kind = kind ?? PaymentKind.Custom,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
            CorrelationId = Guid.NewGuid().ToString()
        };
    }

    #endregion
}

/// <summary>
/// xUnit collection fixture for SQL Server integration tests.
/// Shares a single SQL Server container across all tests in the collection.
/// </summary>
[CollectionDefinition("SqlServer Collection")]
public class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
}

/// <summary>
/// Test fixture that manages the SQL Server Testcontainer lifecycle.
/// </summary>
public class SqlServerFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong!Passw0rd")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}
