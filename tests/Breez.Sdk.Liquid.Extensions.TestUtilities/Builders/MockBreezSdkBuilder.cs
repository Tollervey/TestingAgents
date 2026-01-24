using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Moq;

namespace Breez.Sdk.Liquid.Extensions.TestUtilities.Builders;

/// <summary>
/// Fluent builder for creating mock IBreezSdkService instances for testing.
/// </summary>
public class MockBreezSdkBuilder
{
    private readonly Mock<IBreezSdkService> _mock;
    private bool _isConnected = true;
    private ulong _balance = 100_000;

    /// <summary>
    /// Initializes a new instance of the builder.
    /// </summary>
    public MockBreezSdkBuilder()
    {
        _mock = new Mock<IBreezSdkService>();
        SetupDefaults();
    }

    private void SetupDefaults()
    {
        _mock.Setup(x => x.IsConnected).Returns(() => _isConnected);
        _mock.Setup(x => x.IsConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _isConnected);
        _mock.Setup(x => x.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mock.Setup(x => x.DisconnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mock.Setup(x => x.GetBalanceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => OperationResult<ulong>.Success(_balance));
    }

    /// <summary>
    /// Configures the mock to be in connected state.
    /// </summary>
    public MockBreezSdkBuilder WithConnectedSdk()
    {
        _isConnected = true;
        return this;
    }

    /// <summary>
    /// Configures the mock to be in disconnected state.
    /// </summary>
    public MockBreezSdkBuilder WithDisconnectedSdk()
    {
        _isConnected = false;
        return this;
    }

    /// <summary>
    /// Configures the mock wallet balance.
    /// </summary>
    /// <param name="balanceSat">Balance in satoshis.</param>
    public MockBreezSdkBuilder WithBalance(ulong balanceSat)
    {
        _balance = balanceSat;
        return this;
    }

    /// <summary>
    /// Configures invoice creation to succeed with the specified values.
    /// </summary>
    /// <param name="paymentHash">The payment hash to return.</param>
    /// <param name="invoice">The invoice string to return.</param>
    public MockBreezSdkBuilder WithInvoiceSuccessFlow(string paymentHash, string invoice)
    {
        _mock.Setup(x => x.CreateInvoiceAsync(
                It.IsAny<ulong>(),
                It.IsAny<string?>(),
                It.IsAny<uint?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ulong amount, string? desc, uint? expiry, CancellationToken _) =>
                OperationResult<Invoice>.Success(new Invoice
                {
                    PaymentHash = paymentHash,
                    Destination = invoice,
                    AmountSat = amount,
                    Description = desc,
                    ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
                }));
        return this;
    }

    /// <summary>
    /// Configures invoice creation to fail with the specified error.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="message">The error message.</param>
    public MockBreezSdkBuilder WithInvoiceFailure(BreezErrorCode errorCode, string message)
    {
        _mock.Setup(x => x.CreateInvoiceAsync(
                It.IsAny<ulong>(),
                It.IsAny<string?>(),
                It.IsAny<uint?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Invoice>.Failure(new OperationError
            {
                Code = errorCode,
                Message = message,
                IsRetryable = false
            }));
        return this;
    }

    /// <summary>
    /// Configures a payment to be returned when queried by hash.
    /// </summary>
    /// <param name="payment">The payment to return.</param>
    public MockBreezSdkBuilder WithPayment(PaymentState payment)
    {
        _mock.Setup(x => x.GetPaymentByHashAsync(payment.PaymentHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        return this;
    }

    /// <summary>
    /// Configures payment history to return the specified payments.
    /// </summary>
    /// <param name="payments">The payments to return.</param>
    public MockBreezSdkBuilder WithPaymentHistory(params PaymentState[] payments)
    {
        _mock.Setup(x => x.GetPaymentHistoryAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payments);
        return this;
    }

    /// <summary>
    /// Gets the underlying mock for additional configuration.
    /// </summary>
    public Mock<IBreezSdkService> Mock => _mock;

    /// <summary>
    /// Builds the mock service instance.
    /// </summary>
    public IBreezSdkService Build() => _mock.Object;
}
