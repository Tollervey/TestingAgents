using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Breez;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Exceptions;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Refund;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Services;

/// <summary>
/// Unit tests for RefundService.
/// Tests verify refund lifecycle management per management-api.yaml contract.
///
/// IMPORTANT: These tests are written FIRST (TDD Red phase) and will FAIL until
/// IRefundService and RefundService are implemented.
/// </summary>
public class RefundServiceTests : IDisposable
{
    private readonly PaymentDbContext _context;
    private readonly Mock<IBreezSdkService> _breezSdkServiceMock;
    private readonly Mock<ILogger<RefundService>> _loggerMock;
    private readonly IRefundService _sut;

    public RefundServiceTests()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase($"RefundServiceTest_{Guid.NewGuid()}")
            .Options;
        _context = new PaymentDbContext(options);
        _context.Database.EnsureCreated();

        _breezSdkServiceMock = new Mock<IBreezSdkService>();
        _loggerMock = new Mock<ILogger<RefundService>>();

        _sut = new RefundService(
            _context,
            _breezSdkServiceMock.Object,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region PrepareRefundAsync Tests

    [Fact]
    public async Task PrepareRefundAsync_WithValidPaymentAndInvoice_ReturnsValidPrepareResult()
    {
        // Arrange
        var originalPaymentHash = "valid_payment_hash";
        var destinationInvoice = "lnbc1000n1test_invoice";
        await SeedPayment(originalPaymentHash, PaymentStatus.Paid, 10_000);

        var mockLnInvoice = CreateMockLnInvoice(destinationInvoice, 5_000);
        var mockPrepareResponse = CreateMockPrepareSendResponse(5_000, 100);

        _breezSdkServiceMock
            .Setup(s => s.ParseInvoiceAsync(destinationInvoice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockLnInvoice);

        _breezSdkServiceMock
            .Setup(s => s.PrepareSendPaymentAsync(destinationInvoice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPrepareResponse);

        _breezSdkServiceMock
            .Setup(s => s.GetWalletBalanceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((10_000UL, 0UL, 0UL));

        // Act
        var result = await _sut.PrepareRefundAsync(originalPaymentHash, destinationInvoice);

        // Assert
        result.Should().NotBeNull();
        result.OriginalAmountSat.Should().Be(10_000);
        result.RefundAmountSat.Should().Be(5_000);
        result.FeeSat.Should().Be(100);
        result.WalletBalanceSat.Should().Be(10_000);
        result.CanProceed.Should().BeTrue();
        result.ValidationError.Should().BeNull();
    }

    [Fact]
    public async Task PrepareRefundAsync_WithNonExistentPayment_ThrowsPaymentNotFoundException()
    {
        // Arrange
        var nonExistentHash = "nonexistent_hash";
        var invoice = "lnbc1000n1test_invoice";

        // Act
        var act = async () => await _sut.PrepareRefundAsync(nonExistentHash, invoice);

        // Assert
        await act.Should().ThrowAsync<PaymentNotFoundException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task PrepareRefundAsync_WithRefundAmountExceedingOriginal_ThrowsRefundExceedsOriginalException()
    {
        // Arrange
        var originalPaymentHash = "small_payment_hash";
        var destinationInvoice = "lnbc10000n1large_invoice";
        await SeedPayment(originalPaymentHash, PaymentStatus.Paid, 5_000); // Original: 5k sats

        var mockLnInvoice = CreateMockLnInvoice(destinationInvoice, 10_000); // Refund: 10k sats

        _breezSdkServiceMock
            .Setup(s => s.ParseInvoiceAsync(destinationInvoice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockLnInvoice);

        // Act
        var act = async () => await _sut.PrepareRefundAsync(originalPaymentHash, destinationInvoice);

        // Assert
        await act.Should().ThrowAsync<RefundExceedsOriginalException>()
            .WithMessage("*exceeds*original*");
    }

    [Fact]
    public async Task PrepareRefundAsync_WithInsufficientWalletBalance_ThrowsInsufficientBalanceException()
    {
        // Arrange
        var originalPaymentHash = "payment_hash";
        var destinationInvoice = "lnbc5000n1invoice";
        await SeedPayment(originalPaymentHash, PaymentStatus.Paid, 10_000);

        var mockLnInvoice = CreateMockLnInvoice(destinationInvoice, 5_000);
        var mockPrepareResponse = CreateMockPrepareSendResponse(5_000, 100); // Total: 5,100

        _breezSdkServiceMock
            .Setup(s => s.ParseInvoiceAsync(destinationInvoice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockLnInvoice);

        _breezSdkServiceMock
            .Setup(s => s.PrepareSendPaymentAsync(destinationInvoice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPrepareResponse);

        _breezSdkServiceMock
            .Setup(s => s.GetWalletBalanceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((3_000UL, 0UL, 0UL)); // Insufficient balance

        // Act
        var act = async () => await _sut.PrepareRefundAsync(originalPaymentHash, destinationInvoice);

        // Assert
        await act.Should().ThrowAsync<InsufficientBalanceException>()
            .WithMessage("*insufficient*balance*");
    }

    [Fact]
    public async Task PrepareRefundAsync_WithInvalidInvoice_ThrowsInvalidInvoiceException()
    {
        // Arrange
        var originalPaymentHash = "payment_hash";
        var invalidInvoice = "invalid_invoice_format";
        await SeedPayment(originalPaymentHash, PaymentStatus.Paid, 10_000);

        _breezSdkServiceMock
            .Setup(s => s.ParseInvoiceAsync(invalidInvoice, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidInvoiceException("Failed to parse invoice"));

        // Act
        var act = async () => await _sut.PrepareRefundAsync(originalPaymentHash, invalidInvoice);

        // Assert
        await act.Should().ThrowAsync<InvalidInvoiceException>()
            .WithMessage("*parse invoice*");
    }

    [Fact]
    public async Task PrepareRefundAsync_WithExpiredPayment_ThrowsPaymentNotFoundException()
    {
        // Arrange
        var expiredPaymentHash = "expired_payment_hash";
        var invoice = "lnbc1000n1test_invoice";
        await SeedPayment(expiredPaymentHash, PaymentStatus.Expired, 10_000);

        // Act
        var act = async () => await _sut.PrepareRefundAsync(expiredPaymentHash, invoice);

        // Assert
        await act.Should().ThrowAsync<PaymentNotFoundException>()
            .WithMessage("*not found*eligible*");
    }

    #endregion

    #region InitiateRefundAsync Tests

    [Fact]
    public async Task InitiateRefundAsync_WithValidPrepareResponse_CreatesRefundRecordAndSendsPayment()
    {
        // Arrange
        var originalPaymentHash = "payment_hash";
        var destinationInvoice = "lnbc5000n1invoice";
        var initiatedByUserId = "admin-user-123";
        var reason = "Customer requested refund";
        await SeedPayment(originalPaymentHash, PaymentStatus.Paid, 10_000);

        var mockLnInvoice = CreateMockLnInvoice(destinationInvoice, 5_000);
        var mockPrepareResponse = CreateMockPrepareSendResponse(5_000, 100);
        var mockSendResponse = CreateMockSendPaymentResponse("refund_payment_hash_xyz");

        _breezSdkServiceMock
            .Setup(s => s.ParseInvoiceAsync(destinationInvoice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockLnInvoice);

        _breezSdkServiceMock
            .Setup(s => s.PrepareSendPaymentAsync(destinationInvoice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPrepareResponse);

        _breezSdkServiceMock
            .Setup(s => s.GetWalletBalanceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((10_000UL, 0UL, 0UL));

        _breezSdkServiceMock
            .Setup(s => s.SendPaymentAsync(It.Is<global::Breez.Sdk.Liquid.PrepareSendResponse>(o => o == mockPrepareResponse), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSendResponse);

        // Act - Note: Parameter order is originalPaymentHash, destinationInvoice, initiatedByUserId, reason
        var result = await _sut.InitiateRefundAsync(
            originalPaymentHash,
            destinationInvoice,
            initiatedByUserId,
            reason);

        // Assert
        result.Should().NotBeNull();
        result.OriginalPaymentHash.Should().Be(originalPaymentHash);
        result.AmountSat.Should().Be(5_000);
        result.DestinationInvoice.Should().Be(destinationInvoice);
        result.Status.Should().Be(RefundStatus.Succeeded);
        result.Reason.Should().Be(reason);
        result.InitiatedByUserId.Should().Be(initiatedByUserId);
        result.RefundPaymentHash.Should().Be("refund_payment_hash_xyz");
        result.InitiatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        result.CompletedAt.Should().NotBeNull();
        result.CompletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        result.ErrorMessage.Should().BeNull();

        // Verify persisted to database
        var dbRefund = await _context.RefundTransactions.FindAsync(result.RefundId);
        dbRefund.Should().NotBeNull();
        dbRefund!.Status.Should().Be(RefundStatus.Succeeded);
        dbRefund.RefundPaymentHash.Should().Be("refund_payment_hash_xyz");

        // Verify original payment status updated
        var originalPayment = await _context.PaymentStates.FindAsync(originalPaymentHash);
        originalPayment.Should().NotBeNull();
        originalPayment!.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public async Task InitiateRefundAsync_WhenSdkSendPaymentFails_CreatesFailedRefundRecord()
    {
        // Arrange
        var originalPaymentHash = "payment_hash";
        var destinationInvoice = "lnbc5000n1invoice";
        var initiatedByUserId = "admin-123";
        var reason = "Test refund";
        await SeedPayment(originalPaymentHash, PaymentStatus.Paid, 10_000);

        var mockLnInvoice = CreateMockLnInvoice(destinationInvoice, 5_000);
        var mockPrepareResponse = CreateMockPrepareSendResponse(5_000, 100);

        _breezSdkServiceMock
            .Setup(s => s.ParseInvoiceAsync(destinationInvoice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockLnInvoice);

        _breezSdkServiceMock
            .Setup(s => s.PrepareSendPaymentAsync(destinationInvoice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPrepareResponse);

        _breezSdkServiceMock
            .Setup(s => s.GetWalletBalanceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((10_000UL, 0UL, 0UL));

        _breezSdkServiceMock
            .Setup(s => s.SendPaymentAsync(It.IsAny<global::Breez.Sdk.Liquid.PrepareSendResponse>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BreezSdkConnectionException("Payment routing failed"));

        // Act
        var result = await _sut.InitiateRefundAsync(
            originalPaymentHash,
            destinationInvoice,
            initiatedByUserId,
            reason);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(RefundStatus.Failed);
        result.ErrorMessage.Should().Contain("Payment routing failed");
        result.CompletedAt.Should().NotBeNull();
        result.RefundPaymentHash.Should().BeNull();

        // Verify original payment status remains Paid (not Refunded)
        var originalPayment = await _context.PaymentStates.FindAsync(originalPaymentHash);
        originalPayment.Should().NotBeNull();
        originalPayment!.Status.Should().Be(PaymentStatus.Paid);
    }

    [Fact]
    public async Task InitiateRefundAsync_WithNonExistentPayment_ThrowsPaymentNotFoundException()
    {
        // Arrange
        var nonExistentHash = "nonexistent_hash";
        var invoice = "lnbc1000n1invoice";
        var userId = "admin-123";
        var reason = "Test";

        // Act
        var act = async () => await _sut.InitiateRefundAsync(nonExistentHash, invoice, userId, reason);

        // Assert
        await act.Should().ThrowAsync<PaymentNotFoundException>();
    }

    [Fact]
    public async Task InitiateRefundAsync_WithNullReason_CreatesRefundWithNullReason()
    {
        // Arrange
        var originalPaymentHash = "payment_hash";
        var destinationInvoice = "lnbc5000n1invoice";
        var initiatedByUserId = "admin-123";
        await SeedPayment(originalPaymentHash, PaymentStatus.Paid, 10_000);

        var mockLnInvoice = CreateMockLnInvoice(destinationInvoice, 5_000);
        var mockPrepareResponse = CreateMockPrepareSendResponse(5_000, 100);
        var mockSendResponse = CreateMockSendPaymentResponse("refund_hash");

        _breezSdkServiceMock
            .Setup(s => s.ParseInvoiceAsync(destinationInvoice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockLnInvoice);

        _breezSdkServiceMock
            .Setup(s => s.PrepareSendPaymentAsync(destinationInvoice, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPrepareResponse);

        _breezSdkServiceMock
            .Setup(s => s.GetWalletBalanceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((10_000UL, 0UL, 0UL));

        _breezSdkServiceMock
            .Setup(s => s.SendPaymentAsync(It.IsAny<global::Breez.Sdk.Liquid.PrepareSendResponse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSendResponse);

        // Act
        var result = await _sut.InitiateRefundAsync(
            originalPaymentHash,
            destinationInvoice,
            initiatedByUserId,
            reason: null);

        // Assert
        result.Should().NotBeNull();
        result.Reason.Should().BeNull();
    }

    #endregion

    #region GetRefundByIdAsync Tests

    [Fact]
    public async Task GetRefundByIdAsync_WithExistingRefund_ReturnsRefund()
    {
        // Arrange
        var refund = await SeedRefund(
            "original_hash",
            "lnbc1000n1invoice",
            5_000,
            RefundStatus.Succeeded,
            "admin-123");

        // Act
        var result = await _sut.GetRefundByIdAsync(refund.RefundId);

        // Assert
        result.Should().NotBeNull();
        result!.RefundId.Should().Be(refund.RefundId);
        result.OriginalPaymentHash.Should().Be("original_hash");
        result.AmountSat.Should().Be(5_000UL);
        result.Status.Should().Be(RefundStatus.Succeeded);
    }

    [Fact]
    public async Task GetRefundByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _sut.GetRefundByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRefundByIdAsync_IncludesOriginalPaymentNavigation()
    {
        // Arrange
        var originalPaymentHash = "payment_with_refund";
        await SeedPayment(originalPaymentHash, PaymentStatus.Refunded, 10_000);
        var refund = await SeedRefund(
            originalPaymentHash,
            "lnbc5000n1invoice",
            5_000,
            RefundStatus.Succeeded,
            "admin-123");

        // Act
        var result = await _sut.GetRefundByIdAsync(refund.RefundId);

        // Assert
        result.Should().NotBeNull();
        result!.OriginalPayment.Should().NotBeNull();
        result.OriginalPayment!.PaymentHash.Should().Be(originalPaymentHash);
        result.OriginalPayment.AmountSat.Should().Be(10_000UL);
    }

    #endregion

    #region GetRefundsAsync Tests

    [Fact]
    public async Task GetRefundsAsync_WithNoFilters_ReturnsAllRefundsPaginated()
    {
        // Arrange
        await SeedRefund("hash1", "invoice1", 1000, RefundStatus.Succeeded, "admin-1");
        await SeedRefund("hash2", "invoice2", 2000, RefundStatus.Pending, "admin-2");
        await SeedRefund("hash3", "invoice3", 3000, RefundStatus.Failed, "admin-3");

        // Act
        var (items, total) = await _sut.GetRefundsAsync(skip: 0, take: 10);

        // Assert
        items.Should().HaveCount(3);
        total.Should().Be(3);
    }

    [Fact]
    public async Task GetRefundsAsync_WithStatusFilter_ReturnsOnlyMatchingStatus()
    {
        // Arrange
        await SeedRefund("hash1", "invoice1", 1000, RefundStatus.Succeeded, "admin-1");
        await SeedRefund("hash2", "invoice2", 2000, RefundStatus.Succeeded, "admin-2");
        await SeedRefund("hash3", "invoice3", 3000, RefundStatus.Failed, "admin-3");
        await SeedRefund("hash4", "invoice4", 4000, RefundStatus.Pending, "admin-4");

        // Act
        var (items, total) = await _sut.GetRefundsAsync(status: RefundStatus.Succeeded, skip: 0, take: 10);

        // Assert
        items.Should().HaveCount(2);
        total.Should().Be(2);
        items.Should().AllSatisfy(r => r.Status.Should().Be(RefundStatus.Succeeded));
    }

    [Fact]
    public async Task GetRefundsAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 15; i++)
        {
            await SeedRefund($"hash{i}", $"invoice{i}", (ulong)(i * 1000), RefundStatus.Succeeded, "admin-1");
        }

        // Act - Get page 2 (skip 5, take 5)
        var (items, total) = await _sut.GetRefundsAsync(skip: 5, take: 5);

        // Assert
        items.Should().HaveCount(5);
        total.Should().Be(15);
    }

    [Fact]
    public async Task GetRefundsAsync_OrdersByInitiatedAtDescending()
    {
        // Arrange
        var refund1 = await SeedRefund("hash1", "invoice1", 1000, RefundStatus.Succeeded, "admin-1");
        await Task.Delay(100); // Ensure different timestamps
        var refund2 = await SeedRefund("hash2", "invoice2", 2000, RefundStatus.Succeeded, "admin-2");
        await Task.Delay(100);
        var refund3 = await SeedRefund("hash3", "invoice3", 3000, RefundStatus.Succeeded, "admin-3");

        // Act
        var (items, total) = await _sut.GetRefundsAsync(skip: 0, take: 10);

        // Assert
        items.Should().HaveCount(3);
        total.Should().Be(3);
        items[0].RefundId.Should().Be(refund3.RefundId); // Most recent first
        items[1].RefundId.Should().Be(refund2.RefundId);
        items[2].RefundId.Should().Be(refund1.RefundId);
    }

    [Fact]
    public async Task GetRefundsAsync_WithNoRefunds_ReturnsEmptyList()
    {
        // Act
        var (items, total) = await _sut.GetRefundsAsync(skip: 0, take: 10);

        // Assert
        items.Should().NotBeNull();
        items.Should().BeEmpty();
        total.Should().Be(0);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new RefundService(
            null!,
            _breezSdkServiceMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Constructor_WithNullBreezSdkService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new RefundService(
            _context,
            null!,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("breezSdkService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new RefundService(
            _context,
            _breezSdkServiceMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Helpers

    private async Task<PaymentState> SeedPayment(string paymentHash, PaymentStatus status, ulong amountSat)
    {
        var payment = new PaymentState
        {
            PaymentHash = paymentHash,
            Status = status,
            AmountSat = amountSat,
            ContentId = 42,
            UserSessionId = $"session-{Guid.NewGuid()}",
            Kind = PaymentKind.Paywall
        };

        _context.PaymentStates.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    private async Task<RefundTransaction> SeedRefund(
        string originalPaymentHash,
        string destinationInvoice,
        ulong amountSat,
        RefundStatus status,
        string initiatedByUserId)
    {
        // Ensure original payment exists
        var existingPayment = await _context.PaymentStates.FindAsync(originalPaymentHash);
        if (existingPayment == null)
        {
            await SeedPayment(originalPaymentHash, PaymentStatus.Paid, amountSat);
        }

        var refund = new RefundTransaction
        {
            RefundId = Guid.NewGuid(),
            OriginalPaymentHash = originalPaymentHash,
            AmountSat = amountSat,
            DestinationInvoice = destinationInvoice,
            Status = status,
            Reason = $"Test refund for {originalPaymentHash}",
            InitiatedByUserId = initiatedByUserId,
            InitiatedAt = DateTimeOffset.UtcNow.AddMinutes(-10), // Backdated for ordering tests
            CompletedAt = status != RefundStatus.Pending ? DateTimeOffset.UtcNow.AddMinutes(-5) : null,
            RefundPaymentHash = status == RefundStatus.Succeeded ? $"refund_{Guid.NewGuid():N}" : null,
            ErrorMessage = status == RefundStatus.Failed ? "Test failure reason" : null
        };

        _context.RefundTransactions.Add(refund);
        await _context.SaveChangesAsync();
        return refund;
    }

    /// <summary>
    /// Creates a mock LnInvoice object (Breez.Sdk.Liquid type) using reflection.
    /// Uses reflection-based construction since SDK types have fragile constructors.
    /// </summary>
    private global::Breez.Sdk.Liquid.LnInvoice CreateMockLnInvoice(string invoice, ulong amountSat)
    {
        var invoiceType = typeof(global::Breez.Sdk.Liquid.LnInvoice);

        foreach (var ctor in invoiceType.GetConstructors())
        {
            try
            {
                var pars = ctor.GetParameters();
                var args = new object?[pars.Length];
                for (int i = 0; i < pars.Length; i++)
                {
                    var pt = pars[i].ParameterType;
                    var paramName = pars[i].Name;

                    // Set specific parameters we care about for tests
                    if (string.Equals(paramName, "bolt11", StringComparison.OrdinalIgnoreCase))
                    {
                        args[i] = invoice;
                    }
                    else if (string.Equals(paramName, "amountMsat", StringComparison.OrdinalIgnoreCase))
                    {
                        if (pt == typeof(ulong?))
                        {
                            args[i] = (ulong?)(amountSat * 1000);
                        }
                        else
                        {
                            args[i] = amountSat * 1000;
                        }
                    }
                    else if (string.Equals(paramName, "paymentHash", StringComparison.OrdinalIgnoreCase))
                    {
                        args[i] = "mock_payment_hash";
                    }
                    else if (string.Equals(paramName, "description", StringComparison.OrdinalIgnoreCase))
                    {
                        args[i] = "Test invoice";
                    }
                    else if (string.Equals(paramName, "timestamp", StringComparison.OrdinalIgnoreCase))
                    {
                        if (pt == typeof(ulong))
                        {
                            args[i] = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        }
                        else
                        {
                            args[i] = Convert.ChangeType(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), pt);
                        }
                    }
                    else if (string.Equals(paramName, "expiry", StringComparison.OrdinalIgnoreCase))
                    {
                        if (pt == typeof(ulong))
                        {
                            args[i] = (ulong)3600;
                        }
                        else
                        {
                            args[i] = Convert.ChangeType(3600, pt);
                        }
                    }
                    else if (pt == typeof(string))
                    {
                        args[i] = "";
                    }
                    else if (pt.IsValueType)
                    {
                        args[i] = Activator.CreateInstance(pt);
                    }
                    else
                    {
                        args[i] = null;
                    }
                }
                return (global::Breez.Sdk.Liquid.LnInvoice)ctor.Invoke(args);
            }
            catch
            {
                // Try next constructor
            }
        }

        throw new InvalidOperationException("Unable to construct LnInvoice for test.");
    }

    /// <summary>
    /// Creates a mock PrepareSendResponse object (Breez.Sdk.Liquid type) using reflection.
    /// </summary>
    private global::Breez.Sdk.Liquid.PrepareSendResponse CreateMockPrepareSendResponse(ulong senderAmountSat, ulong feesSat)
    {
        var responseType = typeof(global::Breez.Sdk.Liquid.PrepareSendResponse);

        foreach (var ctor in responseType.GetConstructors())
        {
            try
            {
                var pars = ctor.GetParameters();
                var args = new object?[pars.Length];
                for (int i = 0; i < pars.Length; i++)
                {
                    var pt = pars[i].ParameterType;
                    var paramName = pars[i].Name;

                    // Set feesSat if the parameter name matches
                    if (string.Equals(paramName, "feesSat", StringComparison.OrdinalIgnoreCase))
                    {
                        if (pt == typeof(ulong))
                        {
                            args[i] = feesSat;
                        }
                        else
                        {
                            args[i] = Convert.ChangeType(feesSat, pt);
                        }
                    }
                    else if (pt == typeof(string))
                    {
                        args[i] = "";
                    }
                    else if (pt.IsValueType)
                    {
                        args[i] = Activator.CreateInstance(pt);
                    }
                    else
                    {
                        args[i] = null;
                    }
                }
                return (global::Breez.Sdk.Liquid.PrepareSendResponse)ctor.Invoke(args);
            }
            catch
            {
                // Try next constructor
            }
        }

        throw new InvalidOperationException("Unable to construct PrepareSendResponse for test.");
    }

    /// <summary>
    /// Creates a mock SendPaymentResponse object (Breez.Sdk.Liquid type) using reflection.
    /// </summary>
    private global::Breez.Sdk.Liquid.SendPaymentResponse CreateMockSendPaymentResponse(string paymentHash)
    {
        // First, create a mock Payment object using reflection
        var mockPayment = CreateMockPayment(paymentHash);

        // Then create SendPaymentResponse with the Payment
        var responseType = typeof(global::Breez.Sdk.Liquid.SendPaymentResponse);

        foreach (var ctor in responseType.GetConstructors())
        {
            try
            {
                var pars = ctor.GetParameters();
                var args = new object?[pars.Length];
                for (int i = 0; i < pars.Length; i++)
                {
                    var pt = pars[i].ParameterType;
                    var paramName = pars[i].Name;

                    // If parameter is a Payment, use our mock
                    if (pt == typeof(global::Breez.Sdk.Liquid.Payment))
                    {
                        args[i] = mockPayment;
                    }
                    else if (pt == typeof(string))
                    {
                        args[i] = "";
                    }
                    else if (pt.IsValueType)
                    {
                        args[i] = Activator.CreateInstance(pt);
                    }
                    else
                    {
                        args[i] = null;
                    }
                }
                return (global::Breez.Sdk.Liquid.SendPaymentResponse)ctor.Invoke(args);
            }
            catch
            {
                // Try next constructor
            }
        }

        throw new InvalidOperationException("Unable to construct SendPaymentResponse for test.");
    }

    /// <summary>
    /// Creates a mock Payment object using reflection.
    /// </summary>
    private global::Breez.Sdk.Liquid.Payment CreateMockPayment(string paymentHash)
    {
        var paymentType = typeof(global::Breez.Sdk.Liquid.Payment);

        foreach (var ctor in paymentType.GetConstructors())
        {
            try
            {
                var pars = ctor.GetParameters();
                var args = new object?[pars.Length];
                for (int i = 0; i < pars.Length; i++)
                {
                    var pt = pars[i].ParameterType;
                    var paramName = pars[i].Name;

                    // Set specific parameters we care about
                    if (string.Equals(paramName, "txId", StringComparison.OrdinalIgnoreCase))
                    {
                        args[i] = paymentHash;
                    }
                    else if (string.Equals(paramName, "destination", StringComparison.OrdinalIgnoreCase))
                    {
                        args[i] = "mock_destination";
                    }
                    else if (string.Equals(paramName, "timestamp", StringComparison.OrdinalIgnoreCase))
                    {
                        if (pt == typeof(uint))
                        {
                            args[i] = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        }
                        else if (pt == typeof(ulong))
                        {
                            args[i] = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        }
                        else
                        {
                            args[i] = Convert.ChangeType(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), pt);
                        }
                    }
                    else if (string.Equals(paramName, "amountSat", StringComparison.OrdinalIgnoreCase))
                    {
                        if (pt == typeof(ulong))
                        {
                            args[i] = 5000UL;
                        }
                        else
                        {
                            args[i] = Convert.ChangeType(5000, pt);
                        }
                    }
                    else if (string.Equals(paramName, "feesSat", StringComparison.OrdinalIgnoreCase))
                    {
                        if (pt == typeof(ulong))
                        {
                            args[i] = 100UL;
                        }
                        else
                        {
                            args[i] = Convert.ChangeType(100, pt);
                        }
                    }
                    else if (pt == typeof(string))
                    {
                        args[i] = "";
                    }
                    else if (pt.IsValueType)
                    {
                        args[i] = Activator.CreateInstance(pt);
                    }
                    else
                    {
                        args[i] = null;
                    }
                }
                return (global::Breez.Sdk.Liquid.Payment)ctor.Invoke(args);
            }
            catch
            {
                // Try next constructor
            }
        }

        throw new InvalidOperationException("Unable to construct Payment for test.");
    }

    #endregion
}
