using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Configuration;
using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Breez.Sdk.Liquid.Extensions.Core.Infrastructure;
using Breez.Sdk.Liquid.Extensions.TestUtilities.Builders;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Breez.Sdk.Liquid.Extensions.Core.Tests.Services;

/// <summary>
/// Unit tests for BreezSdkService payment query methods (GetPaymentByHashAsync and GetPaymentHistoryAsync).
/// These tests verify that the service correctly delegates to IPaymentRepository.
/// </summary>
/// <remarks>
/// <para>
/// TDD RED PHASE: These tests are written BEFORE implementing IPaymentRepository integration.
/// They define the expected behavior and will fail until BreezSdkService is updated to:
/// 1. Accept IPaymentRepository in constructor
/// 2. Delegate GetPaymentByHashAsync to repository.GetByHashAsync
/// 3. Delegate GetPaymentHistoryAsync to repository.GetAllAsync
/// 4. Validate parameters and propagate cancellation tokens
/// </para>
/// <para>
/// Test Strategy:
/// - Use Moq to mock IPaymentRepository
/// - Verify service validates parameters before calling repository
/// - Verify service propagates cancellation tokens
/// - Test both success and error scenarios
/// - Use PaymentStateBuilder for test data creation
/// </para>
/// </remarks>
public class BreezSdkServiceQueryTests
{
    private readonly Mock<IBreezSdkWrapper> _wrapperMock;
    private readonly Mock<IPaymentRepository> _repositoryMock;
    private readonly Mock<ILogger<BreezSdkService>> _loggerMock;
    private readonly BreezSdkOptions _options;
    private readonly BreezSdkService _sut;

    public BreezSdkServiceQueryTests()
    {
        _wrapperMock = new Mock<IBreezSdkWrapper>();
        _repositoryMock = new Mock<IPaymentRepository>();
        _loggerMock = new Mock<ILogger<BreezSdkService>>();
        _options = CreateValidOptions();

        // NOTE: BreezSdkService constructor currently doesn't accept IPaymentRepository
        // This will fail compilation until constructor is updated
        // Expected constructor signature: BreezSdkService(IBreezSdkWrapper, IPaymentRepository, IOptions<BreezSdkOptions>, ILogger)
        _sut = new BreezSdkService(
            _wrapperMock.Object,
            _repositoryMock.Object,
            Options.Create(_options),
            _loggerMock.Object);
    }

    #region GetPaymentByHashAsync Tests

    [Fact]
    public async Task GetPaymentByHashAsync_WhenPaymentExists_ReturnsPayment()
    {
        // Arrange
        const string paymentHash = "abc123def456abc123def456abc123def456abc123def456abc123def456abc1";
        var expectedPayment = PaymentStateBuilder.Succeeded(5000)
            .WithPaymentHash(paymentHash)
            .WithDescription("Test payment")
            .Build();

        _repositoryMock
            .Setup(r => r.GetByHashAsync(paymentHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPayment);

        // Act
        var result = await _sut.GetPaymentByHashAsync(paymentHash);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(expectedPayment);
        result!.PaymentHash.Should().Be(paymentHash);
        result.AmountSat.Should().Be(5000);
        result.Status.Should().Be(PaymentStatus.Succeeded);

        _repositoryMock.Verify(
            r => r.GetByHashAsync(paymentHash, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPaymentByHashAsync_WhenPaymentNotFound_ReturnsNull()
    {
        // Arrange
        const string paymentHash = "nonexistent123456789012345678901234567890123456789012345678901234";

        _repositoryMock
            .Setup(r => r.GetByHashAsync(paymentHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentState?)null);

        // Act
        var result = await _sut.GetPaymentByHashAsync(paymentHash);

        // Assert
        result.Should().BeNull();

        _repositoryMock.Verify(
            r => r.GetByHashAsync(paymentHash, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetPaymentByHashAsync_WhenPaymentHashEmpty_ThrowsArgumentException(string invalidHash)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _sut.GetPaymentByHashAsync(invalidHash!));

        // Verify repository was not called
        _repositoryMock.Verify(
            r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetPaymentByHashAsync_WhenPaymentHashNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _sut.GetPaymentByHashAsync(null!));

        // Verify repository was not called
        _repositoryMock.Verify(
            r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetPaymentByHashAsync_PropagatesCancellationToken()
    {
        // Arrange
        const string paymentHash = "test123456789012345678901234567890123456789012345678901234567890";
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _repositoryMock
            .Setup(r => r.GetByHashAsync(paymentHash, cancellationToken))
            .ReturnsAsync((PaymentState?)null);

        // Act
        await _sut.GetPaymentByHashAsync(paymentHash, cancellationToken);

        // Assert
        _repositoryMock.Verify(
            r => r.GetByHashAsync(paymentHash, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task GetPaymentByHashAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        const string paymentHash = "test123456789012345678901234567890123456789012345678901234567890";
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel(); // Cancel immediately

        _repositoryMock
            .Setup(r => r.GetByHashAsync(paymentHash, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _sut.GetPaymentByHashAsync(paymentHash, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task GetPaymentByHashAsync_WithDifferentStatuses_ReturnsCorrectly()
    {
        // Arrange - Test with different payment statuses
        var testCases = new[]
        {
            PaymentStatus.Pending,
            PaymentStatus.Succeeded,
            PaymentStatus.Failed
        };

        foreach (var status in testCases)
        {
            var paymentHash = $"hash{status}1234567890123456789012345678901234567890123456789012345";
            var payment = PaymentStateBuilder.Create()
                .WithPaymentHash(paymentHash)
                .WithStatus(status)
                .WithAmount(1000)
                .Build();

            _repositoryMock
                .Setup(r => r.GetByHashAsync(paymentHash, It.IsAny<CancellationToken>()))
                .ReturnsAsync(payment);

            // Act
            var result = await _sut.GetPaymentByHashAsync(paymentHash);

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be(status);
        }
    }

    #endregion

    #region GetPaymentHistoryAsync Tests

    [Fact]
    public async Task GetPaymentHistoryAsync_WithDefaultParameters_ReturnsPaginatedList()
    {
        // Arrange
        var expectedPayments = new List<PaymentState>
        {
            PaymentStateBuilder.Succeeded(5000).WithCreatedAt(DateTimeOffset.UtcNow.AddHours(-1)).Build(),
            PaymentStateBuilder.Pending(3000).WithCreatedAt(DateTimeOffset.UtcNow.AddHours(-2)).Build(),
            PaymentStateBuilder.Failed(1000).WithCreatedAt(DateTimeOffset.UtcNow.AddHours(-3)).Build()
        };

        _repositoryMock
            .Setup(r => r.GetAllAsync(0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPayments);

        // Act
        var result = await _sut.GetPaymentHistoryAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(expectedPayments);

        _repositoryMock.Verify(
            r => r.GetAllAsync(0, 50, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPaymentHistoryAsync_WithCustomOffsetAndLimit_UseProvidedValues()
    {
        // Arrange
        const int offset = 10;
        const int limit = 20;
        var expectedPayments = new List<PaymentState>
        {
            PaymentStateBuilder.Succeeded(5000).Build()
        };

        _repositoryMock
            .Setup(r => r.GetAllAsync(offset, limit, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPayments);

        // Act
        var result = await _sut.GetPaymentHistoryAsync(offset, limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);

        _repositoryMock.Verify(
            r => r.GetAllAsync(offset, limit, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPaymentHistoryAsync_WhenRepositoryEmpty_ReturnsEmptyList()
    {
        // Arrange
        var emptyList = new List<PaymentState>();

        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyList);

        // Act
        var result = await _sut.GetPaymentHistoryAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(-1, 50)]
    [InlineData(-10, 50)]
    [InlineData(int.MinValue, 50)]
    public async Task GetPaymentHistoryAsync_WhenOffsetNegative_ThrowsArgumentOutOfRangeException(int invalidOffset, int limit)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await _sut.GetPaymentHistoryAsync(invalidOffset, limit));

        // Verify repository was not called
        _repositoryMock.Verify(
            r => r.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, -1)]
    [InlineData(0, -10)]
    public async Task GetPaymentHistoryAsync_WhenLimitZeroOrNegative_ThrowsArgumentOutOfRangeException(int offset, int invalidLimit)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await _sut.GetPaymentHistoryAsync(offset, invalidLimit));

        // Verify repository was not called
        _repositoryMock.Verify(
            r => r.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(1001)]
    [InlineData(2000)]
    [InlineData(int.MaxValue)]
    public async Task GetPaymentHistoryAsync_WhenLimitExceedsMaximum_ThrowsArgumentOutOfRangeException(int invalidLimit)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await _sut.GetPaymentHistoryAsync(0, invalidLimit));

        // Verify repository was not called
        _repositoryMock.Verify(
            r => r.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(0, 50)]
    [InlineData(0, 100)]
    [InlineData(0, 500)]
    [InlineData(0, 1000)]
    public async Task GetPaymentHistoryAsync_WithValidLimitRange_Succeeds(int offset, int validLimit)
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetAllAsync(offset, validLimit, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentState>());

        // Act
        var result = await _sut.GetPaymentHistoryAsync(offset, validLimit);

        // Assert
        result.Should().NotBeNull();

        _repositoryMock.Verify(
            r => r.GetAllAsync(offset, validLimit, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPaymentHistoryAsync_PropagatesCancellationToken()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _repositoryMock
            .Setup(r => r.GetAllAsync(0, 50, cancellationToken))
            .ReturnsAsync(new List<PaymentState>());

        // Act
        await _sut.GetPaymentHistoryAsync(0, 50, cancellationToken);

        // Assert
        _repositoryMock.Verify(
            r => r.GetAllAsync(0, 50, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task GetPaymentHistoryAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel(); // Cancel immediately

        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _sut.GetPaymentHistoryAsync(0, 50, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task GetPaymentHistoryAsync_ReturnsPaymentsInDescendingOrder()
    {
        // Arrange - Repository should return payments newest first
        var now = DateTimeOffset.UtcNow;
        var expectedPayments = new List<PaymentState>
        {
            PaymentStateBuilder.Succeeded(1000).WithCreatedAt(now).Build(),                 // Newest
            PaymentStateBuilder.Succeeded(2000).WithCreatedAt(now.AddHours(-1)).Build(),    // Middle
            PaymentStateBuilder.Succeeded(3000).WithCreatedAt(now.AddHours(-2)).Build()     // Oldest
        };

        _repositoryMock
            .Setup(r => r.GetAllAsync(0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPayments);

        // Act
        var result = await _sut.GetPaymentHistoryAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().BeInDescendingOrder(p => p.CreatedAt);
        result[0].CreatedAt.Should().Be(now);
        result[1].CreatedAt.Should().Be(now.AddHours(-1));
        result[2].CreatedAt.Should().Be(now.AddHours(-2));
    }

    [Fact]
    public async Task GetPaymentHistoryAsync_WithLargeOffset_ReturnsCorrectPage()
    {
        // Arrange - Simulate pagination with large offset
        const int offset = 100;
        const int limit = 10;
        var expectedPayments = new List<PaymentState>
        {
            PaymentStateBuilder.Succeeded(1000).Build()
        };

        _repositoryMock
            .Setup(r => r.GetAllAsync(offset, limit, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPayments);

        // Act
        var result = await _sut.GetPaymentHistoryAsync(offset, limit);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);

        _repositoryMock.Verify(
            r => r.GetAllAsync(offset, limit, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPaymentHistoryAsync_WithMaximumLimit_Succeeds()
    {
        // Arrange - Test upper boundary of limit
        const int maxLimit = 1000;
        var payments = Enumerable.Range(0, maxLimit)
            .Select(i => PaymentStateBuilder.Succeeded((ulong)(i + 1) * 100).Build())
            .ToList();

        _repositoryMock
            .Setup(r => r.GetAllAsync(0, maxLimit, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payments);

        // Act
        var result = await _sut.GetPaymentHistoryAsync(0, maxLimit);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(maxLimit);
    }

    [Fact]
    public async Task GetPaymentHistoryAsync_ReturnsReadOnlyList()
    {
        // Arrange
        var payments = new List<PaymentState>
        {
            PaymentStateBuilder.Succeeded(5000).Build()
        };

        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payments);

        // Act
        var result = await _sut.GetPaymentHistoryAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IReadOnlyList<PaymentState>>();
    }

    #endregion

    #region Helper Methods

    private static BreezSdkOptions CreateValidOptions() => new()
    {
        ApiKey = "test-api-key",
        Mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about",
        MaxInvoiceAmountSat = 10_000_000,
        MaxInvoiceDescriptionLength = 200
    };

    #endregion
}
