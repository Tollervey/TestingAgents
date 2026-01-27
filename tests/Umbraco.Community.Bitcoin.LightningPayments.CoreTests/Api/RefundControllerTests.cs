using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Management;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Management.Dto;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Exceptions;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Refund;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Api;

/// <summary>
/// Unit tests for RefundController.
/// Tests follow TDD RED-GREEN-REFACTOR pattern.
/// These tests are written FIRST and will FAIL until the implementation is created.
/// </summary>
public class RefundControllerTests
{
    private readonly Mock<IRefundService> _refundServiceMock;
    private readonly RefundController _sut;

    public RefundControllerTests()
    {
        _refundServiceMock = new Mock<IRefundService>();
        _sut = new RefundController(_refundServiceMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRefundService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new RefundController(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("refundService");
    }

    #endregion

    #region ListRefunds Tests

    [Fact]
    public async Task ListRefunds_WithNoStatusFilter_ReturnsOkWithAllRefunds()
    {
        // Arrange
        var refunds = new List<RefundTransaction>
        {
            CreateTestRefund("hash1", RefundStatus.Pending),
            CreateTestRefund("hash2", RefundStatus.Succeeded),
            CreateTestRefund("hash3", RefundStatus.Failed)
        };

        _refundServiceMock
            .Setup(s => s.GetRefundsAsync(null, 0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((refunds, 3));

        // Act
        var result = await _sut.ListRefunds(null, 0, 20, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as RefundListResponse;
        response.Should().NotBeNull();
        response!.Items.Should().HaveCount(3);
        response.Total.Should().Be(3);
    }

    [Fact]
    public async Task ListRefunds_WithStatusFilter_ReturnsOkWithFilteredRefunds()
    {
        // Arrange
        var refunds = new List<RefundTransaction>
        {
            CreateTestRefund("hash1", RefundStatus.Pending),
            CreateTestRefund("hash2", RefundStatus.Pending)
        };

        _refundServiceMock
            .Setup(s => s.GetRefundsAsync(RefundStatus.Pending, 0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((refunds, 2));

        // Act
        var result = await _sut.ListRefunds(RefundStatus.Pending, 0, 20, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as RefundListResponse;
        response.Should().NotBeNull();
        response!.Items.Should().HaveCount(2);
        response.Items.Should().OnlyContain(r => r.Status == "pending");
        response.Total.Should().Be(2);
    }

    [Fact]
    public async Task ListRefunds_WithPagination_ReturnsOkWithPaginatedResults()
    {
        // Arrange
        var refunds = new List<RefundTransaction>
        {
            CreateTestRefund("hash1", RefundStatus.Succeeded),
            CreateTestRefund("hash2", RefundStatus.Succeeded)
        };

        _refundServiceMock
            .Setup(s => s.GetRefundsAsync(null, 10, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((refunds, 25));

        // Act
        var result = await _sut.ListRefunds(null, skip: 10, take: 5, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as RefundListResponse;
        response.Should().NotBeNull();
        response!.Items.Should().HaveCount(2);
        response.Total.Should().Be(25);
    }

    [Fact]
    public async Task ListRefunds_WithNoRefunds_ReturnsOkWithEmptyList()
    {
        // Arrange
        _refundServiceMock
            .Setup(s => s.GetRefundsAsync(It.IsAny<RefundStatus?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<RefundTransaction>(), 0));

        // Act
        var result = await _sut.ListRefunds(null, 0, 20, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as RefundListResponse;
        response.Should().NotBeNull();
        response!.Items.Should().BeEmpty();
        response.Total.Should().Be(0);
    }

    #endregion

    #region InitiateRefund Tests

    [Fact]
    public async Task InitiateRefund_WithValidRequest_ReturnsCreatedWithRefund()
    {
        // Arrange
        var request = new InitiateRefundRequest
        {
            OriginalPaymentHash = "abc123",
            DestinationInvoice = "lnbc1000n1...",
            Reason = "Customer request"
        };

        var createdRefund = new RefundTransaction
        {
            RefundId = Guid.NewGuid(),
            OriginalPaymentHash = request.OriginalPaymentHash,
            AmountSat = 50_000,
            DestinationInvoice = request.DestinationInvoice,
            Status = RefundStatus.Pending,
            Reason = request.Reason,
            InitiatedByUserId = "admin-user-1",
            InitiatedAt = DateTimeOffset.UtcNow
        };

        _refundServiceMock
            .Setup(s => s.InitiateRefundAsync(
                request.OriginalPaymentHash,
                request.DestinationInvoice,
                It.IsAny<string>(),
                request.Reason,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdRefund);

        // Act
        var result = await _sut.InitiateRefund(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult!.StatusCode.Should().Be(201);

        var response = createdResult.Value as RefundTransactionResponse;
        response.Should().NotBeNull();
        response!.RefundId.Should().Be(createdRefund.RefundId);
        response.OriginalPaymentHash.Should().Be(request.OriginalPaymentHash);
        response.AmountSat.Should().Be(50_000);
        response.DestinationInvoice.Should().Be(request.DestinationInvoice);
        response.Status.Should().Be("pending");
        response.Reason.Should().Be(request.Reason);
    }

    [Fact]
    public async Task InitiateRefund_WhenPaymentNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new InitiateRefundRequest
        {
            OriginalPaymentHash = "nonexistent",
            DestinationInvoice = "lnbc1000n1..."
        };

        _refundServiceMock
            .Setup(s => s.InitiateRefundAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PaymentNotFoundException("nonexistent"));

        // Act
        var result = await _sut.InitiateRefund(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task InitiateRefund_WhenRefundExceedsOriginal_ReturnsBadRequest()
    {
        // Arrange
        var request = new InitiateRefundRequest
        {
            OriginalPaymentHash = "abc123",
            DestinationInvoice = "lnbc5000000n1..."
        };

        _refundServiceMock
            .Setup(s => s.InitiateRefundAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RefundExceedsOriginalException("Refund amount exceeds original payment"));

        // Act
        var result = await _sut.InitiateRefund(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeOfType<ProblemDetails>();
    }

    [Fact]
    public async Task InitiateRefund_WhenInsufficientBalance_ReturnsBadRequest()
    {
        // Arrange
        var request = new InitiateRefundRequest
        {
            OriginalPaymentHash = "abc123",
            DestinationInvoice = "lnbc1000n1..."
        };

        _refundServiceMock
            .Setup(s => s.InitiateRefundAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InsufficientBalanceException("Insufficient balance"));

        // Act
        var result = await _sut.InitiateRefund(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeOfType<ProblemDetails>();
    }

    [Fact]
    public async Task InitiateRefund_WhenInvalidInvoice_ReturnsBadRequest()
    {
        // Arrange
        var request = new InitiateRefundRequest
        {
            OriginalPaymentHash = "abc123",
            DestinationInvoice = "invalid-invoice"
        };

        _refundServiceMock
            .Setup(s => s.InitiateRefundAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidInvoiceException("Invalid invoice format"));

        // Act
        var result = await _sut.InitiateRefund(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeOfType<ProblemDetails>();
    }

    #endregion

    #region GetRefund Tests

    [Fact]
    public async Task GetRefund_WithExistingRefund_ReturnsOkWithRefundDetails()
    {
        // Arrange
        var refundId = Guid.NewGuid();
        var refund = new RefundTransaction
        {
            RefundId = refundId,
            OriginalPaymentHash = "abc123",
            AmountSat = 25_000,
            DestinationInvoice = "lnbc1000n1...",
            Status = RefundStatus.Succeeded,
            Reason = "Customer request",
            InitiatedByUserId = "admin-1",
            InitiatedAt = DateTimeOffset.UtcNow.AddHours(-2),
            CompletedAt = DateTimeOffset.UtcNow.AddHours(-1),
            RefundPaymentHash = "refund-hash-123"
        };

        _refundServiceMock
            .Setup(s => s.GetRefundByIdAsync(refundId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refund);

        // Act
        var result = await _sut.GetRefund(refundId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as RefundTransactionResponse;
        response.Should().NotBeNull();
        response!.RefundId.Should().Be(refundId);
        response.OriginalPaymentHash.Should().Be("abc123");
        response.AmountSat.Should().Be(25_000);
        response.Status.Should().Be("succeeded");
        response.RefundPaymentHash.Should().Be("refund-hash-123");
    }

    [Fact]
    public async Task GetRefund_WithNonExistentRefund_ReturnsNotFound()
    {
        // Arrange
        var refundId = Guid.NewGuid();

        _refundServiceMock
            .Setup(s => s.GetRefundByIdAsync(refundId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefundTransaction?)null);

        // Act
        var result = await _sut.GetRefund(refundId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetRefund_WithPendingRefund_ReturnsRefundWithoutCompletionData()
    {
        // Arrange
        var refundId = Guid.NewGuid();
        var refund = new RefundTransaction
        {
            RefundId = refundId,
            OriginalPaymentHash = "abc123",
            AmountSat = 10_000,
            DestinationInvoice = "lnbc1000n1...",
            Status = RefundStatus.Pending,
            InitiatedByUserId = "admin-1",
            InitiatedAt = DateTimeOffset.UtcNow,
            CompletedAt = null,
            RefundPaymentHash = null
        };

        _refundServiceMock
            .Setup(s => s.GetRefundByIdAsync(refundId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refund);

        // Act
        var result = await _sut.GetRefund(refundId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as RefundTransactionResponse;
        response.Should().NotBeNull();
        response!.Status.Should().Be("pending");
        response.CompletedAt.Should().BeNull();
        response.RefundPaymentHash.Should().BeNull();
    }

    [Fact]
    public async Task GetRefund_WithFailedRefund_ReturnsRefundWithErrorMessage()
    {
        // Arrange
        var refundId = Guid.NewGuid();
        var refund = new RefundTransaction
        {
            RefundId = refundId,
            OriginalPaymentHash = "abc123",
            AmountSat = 10_000,
            DestinationInvoice = "lnbc1000n1...",
            Status = RefundStatus.Failed,
            ErrorMessage = "Payment timeout",
            InitiatedByUserId = "admin-1",
            InitiatedAt = DateTimeOffset.UtcNow.AddHours(-1),
            CompletedAt = DateTimeOffset.UtcNow
        };

        _refundServiceMock
            .Setup(s => s.GetRefundByIdAsync(refundId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refund);

        // Act
        var result = await _sut.GetRefund(refundId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as RefundTransactionResponse;
        response.Should().NotBeNull();
        response!.Status.Should().Be("failed");
        response.ErrorMessage.Should().Be("Payment timeout");
    }

    #endregion

    #region PrepareRefund Tests

    [Fact]
    public async Task PrepareRefund_WithValidRequest_ReturnsOkWithPrepareResult()
    {
        // Arrange
        var request = new PrepareRefundRequest
        {
            OriginalPaymentHash = "abc123",
            DestinationInvoice = "lnbc1000n1..."
        };

        var prepareResult = new PrepareRefundResult
        {
            OriginalAmountSat = 50_000,
            RefundAmountSat = 48_500,
            FeeSat = 1_500,
            WalletBalanceSat = 100_000,
            CanProceed = true,
            ValidationError = null
        };

        _refundServiceMock
            .Setup(s => s.PrepareRefundAsync(
                request.OriginalPaymentHash,
                request.DestinationInvoice,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(prepareResult);

        // Act
        var result = await _sut.PrepareRefund(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as PrepareRefundResponse;
        response.Should().NotBeNull();
        response!.OriginalAmountSat.Should().Be(50_000);
        response.RefundAmountSat.Should().Be(48_500);
        response.FeeSat.Should().Be(1_500);
        response.WalletBalanceSat.Should().Be(100_000);
        response.CanProceed.Should().BeTrue();
        response.ValidationError.Should().BeNull();
    }

    [Fact]
    public async Task PrepareRefund_WhenPaymentNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new PrepareRefundRequest
        {
            OriginalPaymentHash = "nonexistent",
            DestinationInvoice = "lnbc1000n1..."
        };

        _refundServiceMock
            .Setup(s => s.PrepareRefundAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PaymentNotFoundException("nonexistent"));

        // Act
        var result = await _sut.PrepareRefund(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task PrepareRefund_WhenCannotProceed_ReturnsOkWithValidationError()
    {
        // Arrange
        var request = new PrepareRefundRequest
        {
            OriginalPaymentHash = "abc123",
            DestinationInvoice = "lnbc1000n1..."
        };

        var prepareResult = new PrepareRefundResult
        {
            OriginalAmountSat = 50_000,
            RefundAmountSat = 50_000,
            FeeSat = 2_000,
            WalletBalanceSat = 1_000,
            CanProceed = false,
            ValidationError = "Insufficient wallet balance"
        };

        _refundServiceMock
            .Setup(s => s.PrepareRefundAsync(
                request.OriginalPaymentHash,
                request.DestinationInvoice,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(prepareResult);

        // Act
        var result = await _sut.PrepareRefund(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as PrepareRefundResponse;
        response.Should().NotBeNull();
        response!.CanProceed.Should().BeFalse();
        response.ValidationError.Should().Be("Insufficient wallet balance");
    }

    [Fact]
    public async Task PrepareRefund_WhenInvalidInvoice_ReturnsBadRequest()
    {
        // Arrange
        var request = new PrepareRefundRequest
        {
            OriginalPaymentHash = "abc123",
            DestinationInvoice = "invalid-invoice"
        };

        _refundServiceMock
            .Setup(s => s.PrepareRefundAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidInvoiceException("Invalid invoice format"));

        // Act
        var result = await _sut.PrepareRefund(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeOfType<ProblemDetails>();
    }

    [Fact]
    public async Task PrepareRefund_WhenRefundExceedsOriginal_ReturnsBadRequest()
    {
        // Arrange
        var request = new PrepareRefundRequest
        {
            OriginalPaymentHash = "abc123",
            DestinationInvoice = "lnbc5000000n1..."
        };

        _refundServiceMock
            .Setup(s => s.PrepareRefundAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RefundExceedsOriginalException("Refund exceeds original"));

        // Act
        var result = await _sut.PrepareRefund(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeOfType<ProblemDetails>();
    }

    #endregion

    #region Helpers

    private static RefundTransaction CreateTestRefund(string originalPaymentHash, RefundStatus status)
    {
        var refund = new RefundTransaction
        {
            RefundId = Guid.NewGuid(),
            OriginalPaymentHash = originalPaymentHash,
            AmountSat = 10_000,
            DestinationInvoice = $"lnbc10000n1test{Guid.NewGuid():N}",
            Status = status,
            InitiatedByUserId = "admin-user-1",
            InitiatedAt = DateTimeOffset.UtcNow.AddHours(-2)
        };

        if (status == RefundStatus.Succeeded)
        {
            refund.CompletedAt = DateTimeOffset.UtcNow.AddHours(-1);
            refund.RefundPaymentHash = $"refund-{Guid.NewGuid():N}";
        }
        else if (status == RefundStatus.Failed)
        {
            refund.CompletedAt = DateTimeOffset.UtcNow.AddHours(-1);
            refund.ErrorMessage = "Payment failed";
        }

        return refund;
    }

    #endregion
}
