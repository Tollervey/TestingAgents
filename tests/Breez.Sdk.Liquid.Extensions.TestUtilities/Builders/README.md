# MockBreezSdkBuilder

A fluent builder for creating mock `IBreezSdkService` instances for testing.

## Purpose

The `MockBreezSdkBuilder` simplifies the creation of test doubles for the BreezSDK service, allowing you to easily configure different scenarios without writing repetitive Moq setup code.

## Features

- Fluent API for readable test setup
- Pre-configured defaults for common scenarios
- Support for both success and failure paths
- Access to underlying Moq.Mock for advanced scenarios
- Comprehensive examples included

## Basic Usage

```csharp
// Create a connected SDK with default balance (100,000 sats)
var mockSdk = new MockBreezSdkBuilder()
    .Build();

// Use in your test
var service = new PaymentService(mockSdk);
```

## Configuration Methods

### Connection State

```csharp
// Connected SDK (default)
var mockSdk = new MockBreezSdkBuilder()
    .WithConnectedSdk()
    .Build();

// Disconnected SDK
var mockSdk = new MockBreezSdkBuilder()
    .WithDisconnectedSdk()
    .Build();
```

### Balance Configuration

```csharp
// Custom balance
var mockSdk = new MockBreezSdkBuilder()
    .WithBalance(50_000) // 50,000 sats
    .Build();
```

### Invoice Creation

```csharp
// Successful invoice creation
var mockSdk = new MockBreezSdkBuilder()
    .WithInvoiceSuccessFlow(
        paymentHash: "abc123",
        invoice: "lnbc50000n1...")
    .Build();

// Failed invoice creation
var mockSdk = new MockBreezSdkBuilder()
    .WithInvoiceFailure(
        errorCode: BreezErrorCode.InsufficientFunds,
        message: "Not enough balance")
    .Build();
```

### Payment Queries

```csharp
// Configure payment retrieval by hash
var payment = new PaymentState
{
    PaymentHash = "specific_hash",
    AmountSat = 10_000,
    Status = PaymentStatus.Succeeded
};

var mockSdk = new MockBreezSdkBuilder()
    .WithPayment(payment)
    .Build();

// Configure payment history
var mockSdk = new MockBreezSdkBuilder()
    .WithPaymentHistory(payment1, payment2, payment3)
    .Build();
```

## Complete Test Example

```csharp
[Fact]
public async Task CreateInvoice_WithValidAmount_ReturnsInvoice()
{
    // Arrange
    var mockSdk = new MockBreezSdkBuilder()
        .WithConnectedSdk()
        .WithBalance(100_000)
        .WithInvoiceSuccessFlow(
            paymentHash: "test_hash_123",
            invoice: "lnbc50000n1...")
        .Build();

    var service = new PaymentService(mockSdk);

    // Act
    var result = await service.CreateInvoiceAsync(5000, "Test payment");

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.PaymentHash.Should().Be("test_hash_123");
}
```

## Advanced Usage

For scenarios not covered by the builder methods, you can access the underlying Moq Mock:

```csharp
var builder = new MockBreezSdkBuilder();

// Access the underlying Mock for custom setup
builder.Mock
    .Setup(x => x.CreateInvoiceAsync(
        It.Is<ulong>(amt => amt > 1000000),
        It.IsAny<string?>(),
        It.IsAny<uint?>(),
        It.IsAny<CancellationToken>()))
    .ReturnsAsync(OperationResult<Invoice>.Failure(
        new OperationError
        {
            Code = BreezErrorCode.AmountAboveMaximum,
            Message = "Amount exceeds maximum",
            IsRetryable = false
        }));

var mockSdk = builder.Build();
```

## Default Behavior

Without any configuration, the builder creates a mock with:
- Connected SDK (`IsConnected = true`)
- Balance of 100,000 sats
- No-op Connect/Disconnect methods
- Default GetBalanceAsync returns success with configured balance

## Testing Patterns

### Test Isolation

Each test should create its own mock instance:

```csharp
public class PaymentServiceTests
{
    [Fact]
    public async Task Test1()
    {
        var mockSdk = new MockBreezSdkBuilder()
            .WithBalance(50_000)
            .Build();
        // Test implementation
    }

    [Fact]
    public async Task Test2()
    {
        var mockSdk = new MockBreezSdkBuilder()
            .WithBalance(75_000)
            .Build();
        // Test implementation
    }
}
```

### Error Scenarios

Test both success and failure paths:

```csharp
[Fact]
public async Task CreateInvoice_InsufficientFunds_ReturnsFailure()
{
    // Arrange
    var mockSdk = new MockBreezSdkBuilder()
        .WithInvoiceFailure(
            BreezErrorCode.InsufficientFunds,
            "Insufficient balance")
        .Build();

    var service = new PaymentService(mockSdk);

    // Act
    var result = await service.CreateInvoiceAsync(1_000_000);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be(BreezErrorCode.InsufficientFunds);
}
```

## See Also

- `MockBreezSdkBuilderExample.cs` - Complete usage examples
- `PaymentStateBuilder.cs` - Builder for PaymentState test data
- `FakeBreezSdkWrapper.cs` - Alternative fake implementation
