using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Moq;

namespace Breez.Sdk.Liquid.Extensions.TestUtilities.Builders;

/// <summary>
/// Example usage of MockBreezSdkBuilder for testing.
/// </summary>
/// <remarks>
/// This is a demonstration file showing common usage patterns.
/// In real tests, these patterns would be in actual test methods.
/// </remarks>
public static class MockBreezSdkBuilderExample
{
    /// <summary>
    /// Example: Create a connected SDK mock with default balance.
    /// </summary>
    public static void Example_ConnectedSdkWithDefaultBalance()
    {
        var mockSdk = new MockBreezSdkBuilder()
            .WithConnectedSdk()
            .Build();

        // Use mockSdk in your tests
        var isConnected = mockSdk.IsConnected; // true
    }

    /// <summary>
    /// Example: Create a mock with custom balance.
    /// </summary>
    public static void Example_CustomBalance()
    {
        var mockSdk = new MockBreezSdkBuilder()
            .WithBalance(50_000)
            .Build();

        // Balance will be 50,000 sats
    }

    /// <summary>
    /// Example: Create a mock that returns a successful invoice.
    /// </summary>
    public static void Example_SuccessfulInvoiceCreation()
    {
        var mockSdk = new MockBreezSdkBuilder()
            .WithInvoiceSuccessFlow(
                paymentHash: "abc123",
                invoice: "lnbc50000n1...")
            .Build();

        // CreateInvoiceAsync will return success with the specified hash and invoice
    }

    /// <summary>
    /// Example: Create a mock that simulates invoice creation failure.
    /// </summary>
    public static void Example_FailedInvoiceCreation()
    {
        var mockSdk = new MockBreezSdkBuilder()
            .WithInvoiceFailure(
                errorCode: BreezErrorCode.InsufficientFunds,
                message: "Not enough balance to receive payment")
            .Build();

        // CreateInvoiceAsync will return failure with the specified error
    }

    /// <summary>
    /// Example: Create a mock with payment history.
    /// </summary>
    public static void Example_PaymentHistory()
    {
        var payment1 = new PaymentState
        {
            PaymentHash = "hash1",
            AmountSat = 5000,
            Status = PaymentStatus.Succeeded
        };

        var payment2 = new PaymentState
        {
            PaymentHash = "hash2",
            AmountSat = 3000,
            Status = PaymentStatus.Pending
        };

        var mockSdk = new MockBreezSdkBuilder()
            .WithPaymentHistory(payment1, payment2)
            .Build();

        // GetPaymentHistoryAsync will return these two payments
    }

    /// <summary>
    /// Example: Create a mock that can retrieve a specific payment by hash.
    /// </summary>
    public static void Example_GetPaymentByHash()
    {
        var payment = new PaymentState
        {
            PaymentHash = "specific_hash",
            AmountSat = 10_000,
            Status = PaymentStatus.Succeeded
        };

        var mockSdk = new MockBreezSdkBuilder()
            .WithPayment(payment)
            .Build();

        // GetPaymentByHashAsync("specific_hash") will return this payment
    }

    /// <summary>
    /// Example: Create a disconnected SDK mock.
    /// </summary>
    public static void Example_DisconnectedSdk()
    {
        var mockSdk = new MockBreezSdkBuilder()
            .WithDisconnectedSdk()
            .Build();

        // IsConnected will be false
    }

    /// <summary>
    /// Example: Chain multiple configurations.
    /// </summary>
    public static void Example_ChainedConfiguration()
    {
        var mockSdk = new MockBreezSdkBuilder()
            .WithConnectedSdk()
            .WithBalance(75_000)
            .WithInvoiceSuccessFlow("payment_hash_123", "lnbc75000n1...")
            .Build();

        // All configurations are applied
    }

    /// <summary>
    /// Example: Access the underlying Mock for advanced scenarios.
    /// </summary>
    public static void Example_AdvancedMockConfiguration()
    {
        var builder = new MockBreezSdkBuilder();

        // Access the underlying Moq Mock object for advanced setup
        builder.Mock.Setup(x => x.IsConnectedAsync(default))
            .ReturnsAsync(true);

        var mockSdk = builder.Build();

        // Now you have both the builder's defaults and custom setup
    }
}
