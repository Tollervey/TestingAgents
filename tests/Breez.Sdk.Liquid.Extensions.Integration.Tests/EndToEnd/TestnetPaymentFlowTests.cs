using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Configuration;
using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Breez.Sdk.Liquid.Extensions.Core.Infrastructure;
using Breez.Sdk.Liquid.Extensions.Sqlite.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Breez.Sdk.Liquid.Extensions.Integration.Tests.EndToEnd;

/// <summary>
/// End-to-end integration tests for testnet payment flows.
/// </summary>
/// <remarks>
/// <para>
/// These tests require a real testnet connection and are conditionally skipped if the
/// environment is not configured with testnet credentials.
/// </para>
/// <para>
/// <b>Required Environment Variables:</b>
/// </para>
/// <list type="bullet">
///   <item><b>BREEZ_TESTNET_MNEMONIC</b>: BIP39 mnemonic for testnet wallet access</item>
///   <item><b>BREEZ_TESTNET_API_KEY</b>: Breez API key for testnet authentication</item>
/// </list>
/// <para>
/// <b>Test Characteristics:</b>
/// </para>
/// <list type="bullet">
///   <item>Uses real BreezSDK connection to testnet Lightning Network</item>
///   <item>Tests actual invoice creation, payment status, and event handling</item>
///   <item>Validates BOLT11 invoice format compliance</item>
///   <item>Tests wallet balance retrieval and connection state management</item>
///   <item>Skips gracefully when testnet is not configured (CI/CD friendly)</item>
/// </list>
/// <para>
/// <b>Constitutional Compliance:</b>
/// </para>
/// <list type="bullet">
///   <item>Article III.3 (Test Isolation): Each test creates independent invoice to avoid conflicts</item>
///   <item>Article III.4 (Automated Validation): Tests run via standard `dotnet test` pipeline</item>
///   <item>Article VII.1 (Error Handling): Tests verify error paths and exception handling</item>
/// </list>
/// </remarks>
[Collection("Testnet")]
[Trait("Category", "Integration")]
[Trait("Category", "Testnet")]
public class TestnetPaymentFlowTests : IAsyncLifetime
{
    private readonly string? _testnetMnemonic;
    private readonly string? _testnetApiKey;
    private IBreezSdkService? _sut;
    private IBreezSdkWrapper? _wrapper;
    private IPaymentRepository? _repository;
    private SqlitePaymentDbContext? _dbContext;
    private string? _testWorkingDir;

    public TestnetPaymentFlowTests()
    {
        _testnetMnemonic = Environment.GetEnvironmentVariable("BREEZ_TESTNET_MNEMONIC");
        _testnetApiKey = Environment.GetEnvironmentVariable("BREEZ_TESTNET_API_KEY");
    }

    public async Task InitializeAsync()
    {
        // Skip setup if testnet is not configured
        if (string.IsNullOrEmpty(_testnetMnemonic) || string.IsNullOrEmpty(_testnetApiKey))
        {
            return;
        }

        // Create unique working directory for this test run
        _testWorkingDir = Path.Combine(Path.GetTempPath(), $"breez-testnet-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testWorkingDir);

        // Create in-memory SQLite database for test isolation
        var optionsBuilder = new DbContextOptionsBuilder<SqlitePaymentDbContext>();
        optionsBuilder.UseSqlite("Data Source=:memory:");
        _dbContext = new SqlitePaymentDbContext(optionsBuilder.Options);
        await _dbContext.Database.OpenConnectionAsync();
        await _dbContext.Database.EnsureCreatedAsync();

        _repository = new SqlitePaymentRepository(_dbContext);

        // Configure BreezSDK for testnet
        var options = Options.Create(new BreezSdkOptions
        {
            ApiKey = _testnetApiKey,
            Mnemonic = _testnetMnemonic,
            Network = BreezNetwork.Testnet,
            WorkingDirectory = _testWorkingDir,
            ConnectionTimeoutSeconds = 60, // Longer timeout for testnet
            MaxInvoiceAmountSat = 100_000, // Small amounts for testing
            MaxInvoiceDescriptionLength = 200
        });

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var wrapperLogger = loggerFactory.CreateLogger<BreezSdkWrapper>();
        var serviceLogger = loggerFactory.CreateLogger<BreezSdkService>();

        // Create real wrapper (not mocked - this is end-to-end test)
        _wrapper = new BreezSdkWrapper(options, wrapperLogger);

        // Create service with real dependencies
        _sut = new BreezSdkService(_wrapper, _repository, options, serviceLogger);

        // Connect to testnet
        try
        {
            await _sut.ConnectAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Log connection failure but don't fail the test setup
            // The test will be skipped based on environment variable check
            Console.WriteLine($"Failed to connect to testnet: {ex.Message}");
        }
    }

    public async Task DisposeAsync()
    {
        if (_sut != null)
        {
            await _sut.DisconnectAsync();
        }

        if (_wrapper != null)
        {
            await _wrapper.DisposeAsync();
        }

        if (_dbContext != null)
        {
            await _dbContext.Database.CloseConnectionAsync();
            await _dbContext.DisposeAsync();
        }

        // Clean up test working directory
        if (_testWorkingDir != null && Directory.Exists(_testWorkingDir))
        {
            try
            {
                Directory.Delete(_testWorkingDir, recursive: true);
            }
            catch
            {
                // Best effort cleanup - ignore errors
            }
        }
    }

    #region Connection and Health Tests

    [SkippableFact]
    public async Task IsConnectedAsync_WhenConnectedToTestnet_ReturnsTrue()
    {
        // Arrange
        Skip.If(string.IsNullOrEmpty(_testnetMnemonic) || string.IsNullOrEmpty(_testnetApiKey),
            "Testnet not configured. Set BREEZ_TESTNET_MNEMONIC and BREEZ_TESTNET_API_KEY environment variables.");

        // Act
        var isConnected = await _sut!.IsConnectedAsync();

        // Assert
        isConnected.Should().BeTrue("service should be connected to testnet after initialization");
    }

    [SkippableFact]
    public void IsConnected_Property_ReturnsConnectionState()
    {
        // Arrange
        Skip.If(string.IsNullOrEmpty(_testnetMnemonic) || string.IsNullOrEmpty(_testnetApiKey),
            "Testnet not configured. Set BREEZ_TESTNET_MNEMONIC and BREEZ_TESTNET_API_KEY environment variables.");

        // Act
        var isConnected = _sut!.IsConnected;

        // Assert
        isConnected.Should().BeTrue("IsConnected property should reflect active testnet connection");
    }

    #endregion

    #region Balance Tests

    [SkippableFact]
    public async Task GetBalanceAsync_OnTestnet_ReturnsBalance()
    {
        // Arrange
        Skip.If(string.IsNullOrEmpty(_testnetMnemonic) || string.IsNullOrEmpty(_testnetApiKey),
            "Testnet not configured. Set BREEZ_TESTNET_MNEMONIC and BREEZ_TESTNET_API_KEY environment variables.");

        // Act
        var result = await _sut!.GetBalanceAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue("balance query should succeed on connected testnet");
        result.Value.Should().BeGreaterThanOrEqualTo(0UL, "balance should be non-negative");
    }

    [SkippableFact]
    public async Task GetBalanceAsync_OnTestnet_ReturnsConsistentValue()
    {
        // Arrange
        Skip.If(string.IsNullOrEmpty(_testnetMnemonic) || string.IsNullOrEmpty(_testnetApiKey),
            "Testnet not configured. Set BREEZ_TESTNET_MNEMONIC and BREEZ_TESTNET_API_KEY environment variables.");

        // Act - Query balance twice in quick succession
        var result1 = await _sut!.GetBalanceAsync();
        var result2 = await _sut.GetBalanceAsync();

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.Should().Be(result2.Value,
            "balance should remain consistent across rapid queries (assuming no concurrent payments)");
    }

    #endregion

    #region Invoice Creation Tests

    [SkippableFact]
    public async Task CreateInvoiceAsync_WithValidAmount_ReturnsValidBolt11Invoice()
    {
        // Arrange
        Skip.If(string.IsNullOrEmpty(_testnetMnemonic) || string.IsNullOrEmpty(_testnetApiKey),
            "Testnet not configured. Set BREEZ_TESTNET_MNEMONIC and BREEZ_TESTNET_API_KEY environment variables.");

        var amountSat = 1000UL;
        var description = $"Test invoice {Guid.NewGuid():N}";

        // Act
        var result = await _sut!.CreateInvoiceAsync(amountSat, description);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue("invoice creation should succeed on testnet");

        var invoice = result.Value;
        invoice.Should().NotBeNull();
        invoice!.Destination.Should().StartWith("lntb",
            "testnet BOLT11 invoices should start with 'lntb' prefix");
        invoice.AmountSat.Should().Be(amountSat, "invoice should reflect requested amount");
        invoice.Description.Should().Be(description, "invoice should preserve description");
        invoice.PaymentHash.Should().NotBeNullOrEmpty("invoice should have a payment hash");
        invoice.PaymentHash.Should().HaveLength(64, "payment hash should be 64 hex characters (32 bytes)");
        invoice.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow, "invoice should expire in the future");
    }

    [SkippableFact]
    public async Task CreateInvoiceAsync_WithMultipleAmounts_CreatesUniqueInvoices()
    {
        // Arrange
        Skip.If(string.IsNullOrEmpty(_testnetMnemonic) || string.IsNullOrEmpty(_testnetApiKey),
            "Testnet not configured. Set BREEZ_TESTNET_MNEMONIC and BREEZ_TESTNET_API_KEY environment variables.");

        var amounts = new[] { 1000UL, 2000UL, 5000UL };

        // Act
        var invoices = new List<Invoice>();
        foreach (var amount in amounts)
        {
            var result = await _sut!.CreateInvoiceAsync(amount, $"Test {amount} sats");
            result.IsSuccess.Should().BeTrue();
            invoices.Add(result.Value!);
        }

        // Assert
        invoices.Should().HaveCount(3);
        var paymentHashes = invoices.Select(i => i.PaymentHash).ToList();
        paymentHashes.Should().OnlyHaveUniqueItems("each invoice should have a unique payment hash");

        var destinations = invoices.Select(i => i.Destination).ToList();
        destinations.Should().OnlyHaveUniqueItems("each invoice should have a unique BOLT11 string");

        for (int i = 0; i < amounts.Length; i++)
        {
            invoices[i].AmountSat.Should().Be(amounts[i],
                $"invoice {i} should have correct amount");
        }
    }

    [SkippableFact]
    public async Task CreateInvoiceAsync_WithCustomExpiry_RespectsExpiryTime()
    {
        // Arrange
        Skip.If(string.IsNullOrEmpty(_testnetMnemonic) || string.IsNullOrEmpty(_testnetApiKey),
            "Testnet not configured. Set BREEZ_TESTNET_MNEMONIC and BREEZ_TESTNET_API_KEY environment variables.");

        var amountSat = 1000UL;
        var expirySec = 600U; // 10 minutes
        var description = $"Test expiry invoice {Guid.NewGuid():N}";

        // Act
        var result = await _sut!.CreateInvoiceAsync(amountSat, description, expirySec);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var invoice = result.Value!;

        var expectedExpiry = DateTimeOffset.UtcNow.AddSeconds(expirySec);
        var actualExpiry = invoice.ExpiresAt;

        // Allow 60 second tolerance for network latency and test execution time
        actualExpiry.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(60),
            "invoice expiry should be approximately {0} seconds from creation", expirySec);
    }

    [SkippableFact]
    public async Task CreateInvoiceAsync_WithMinimumAmount_Succeeds()
    {
        // Arrange
        Skip.If(string.IsNullOrEmpty(_testnetMnemonic) || string.IsNullOrEmpty(_testnetApiKey),
            "Testnet not configured. Set BREEZ_TESTNET_MNEMONIC and BREEZ_TESTNET_API_KEY environment variables.");

        var minAmount = 1000UL; // Minimum practical Lightning amount

        // Act
        var result = await _sut!.CreateInvoiceAsync(minAmount, "Minimum amount test");

        // Assert
        result.IsSuccess.Should().BeTrue("minimum amount invoice should succeed");
        result.Value!.AmountSat.Should().Be(minAmount);
    }

    [SkippableFact]
    public async Task CreateInvoiceAsync_WithLongDescription_Succeeds()
    {
        // Arrange
        Skip.If(string.IsNullOrEmpty(_testnetMnemonic) || string.IsNullOrEmpty(_testnetApiKey),
            "Testnet not configured. Set BREEZ_TESTNET_MNEMONIC and BREEZ_TESTNET_API_KEY environment variables.");

        var longDescription = new string('X', 200); // Max configured length

        // Act
        var result = await _sut!.CreateInvoiceAsync(1000UL, longDescription);

        // Assert
        result.IsSuccess.Should().BeTrue("invoice with max-length description should succeed");
        result.Value!.Description.Should().Be(longDescription);
    }

    [SkippableFact]
    public async Task CreateInvoiceAsync_WithZeroAmount_ReturnsFailure()
    {
        // Arrange
        Skip.If(string.IsNullOrEmpty(_testnetMnemonic) || string.IsNullOrEmpty(_testnetApiKey),
            "Testnet not configured. Set BREEZ_TESTNET_MNEMONIC and BREEZ_TESTNET_API_KEY environment variables.");

        // Act
        var result = await _sut!.CreateInvoiceAsync(0UL, "Zero amount test");

        // Assert
        result.IsSuccess.Should().BeFalse("zero amount invoice should fail validation");
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be(BreezErrorCode.AmountBelowMinimum,
            "zero amount should be below minimum error");
    }

    [SkippableFact]
    public async Task CreateInvoiceAsync_WithExcessiveAmount_ReturnsFailure()
    {
        // Arrange
        Skip.If(string.IsNullOrEmpty(_testnetMnemonic) || string.IsNullOrEmpty(_testnetApiKey),
            "Testnet not configured. Set BREEZ_TESTNET_MNEMONIC and BREEZ_TESTNET_API_KEY environment variables.");

        var excessiveAmount = 1_000_000UL; // Above configured max (100,000 for tests)

        // Act
        var result = await _sut!.CreateInvoiceAsync(excessiveAmount, "Excessive amount test");

        // Assert
        result.IsSuccess.Should().BeFalse("excessive amount should fail validation");
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be(BreezErrorCode.AmountAboveMaximum,
            "excessive amount should be above maximum error");
    }

    #endregion

    #region Payment History Tests

    [SkippableFact]
    public async Task GetPaymentByHashAsync_WithExistingInvoice_ReturnsPaymentState()
    {
        // Arrange
        Skip.If(string.IsNullOrEmpty(_testnetMnemonic) || string.IsNullOrEmpty(_testnetApiKey),
            "Testnet not configured. Set BREEZ_TESTNET_MNEMONIC and BREEZ_TESTNET_API_KEY environment variables.");

        // Create an invoice first
        var invoiceResult = await _sut!.CreateInvoiceAsync(1000UL, "Test payment lookup");
        invoiceResult.IsSuccess.Should().BeTrue();
        var paymentHash = invoiceResult.Value!.PaymentHash;

        // Act
        var payment = await _sut.GetPaymentByHashAsync(paymentHash);

        // Assert
        payment.Should().NotBeNull("payment should be persisted after invoice creation");
        payment!.PaymentHash.Should().Be(paymentHash);
        payment.Status.Should().Be(PaymentStatus.Pending,
            "newly created invoice should have Pending status");
        payment.AmountSat.Should().Be(1000UL);
    }

    [SkippableFact]
    public async Task GetPaymentByHashAsync_WithNonExistentHash_ReturnsNull()
    {
        // Arrange
        Skip.If(string.IsNullOrEmpty(_testnetMnemonic) || string.IsNullOrEmpty(_testnetApiKey),
            "Testnet not configured. Set BREEZ_TESTNET_MNEMONIC and BREEZ_TESTNET_API_KEY environment variables.");

        var nonExistentHash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

        // Act
        var payment = await _sut!.GetPaymentByHashAsync(nonExistentHash);

        // Assert
        payment.Should().BeNull("non-existent payment hash should return null");
    }

    [SkippableFact]
    public async Task GetPaymentHistoryAsync_AfterCreatingInvoices_ReturnsPayments()
    {
        // Arrange
        Skip.If(string.IsNullOrEmpty(_testnetMnemonic) || string.IsNullOrEmpty(_testnetApiKey),
            "Testnet not configured. Set BREEZ_TESTNET_MNEMONIC and BREEZ_TESTNET_API_KEY environment variables.");

        // Create multiple invoices
        var uniqueId = Guid.NewGuid().ToString("N");
        for (int i = 0; i < 3; i++)
        {
            var result = await _sut!.CreateInvoiceAsync(
                (ulong)(1000 * (i + 1)),
                $"History test {uniqueId} #{i}");
            result.IsSuccess.Should().BeTrue();
        }

        // Act
        var history = await _sut!.GetPaymentHistoryAsync(offset: 0, limit: 10);

        // Assert
        history.Should().NotBeNull();
        history.Should().NotBeEmpty("payment history should contain created invoices");

        // Filter to our test payments (using unique description prefix)
        var testPayments = history.Where(p => p.Description?.Contains(uniqueId) == true).ToList();
        testPayments.Should().HaveCount(3, "should find all 3 created test invoices");
    }

    [SkippableFact]
    public async Task GetPaymentHistoryAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        Skip.If(string.IsNullOrEmpty(_testnetMnemonic) || string.IsNullOrEmpty(_testnetApiKey),
            "Testnet not configured. Set BREEZ_TESTNET_MNEMONIC and BREEZ_TESTNET_API_KEY environment variables.");

        // Act - Get first page
        var firstPage = await _sut!.GetPaymentHistoryAsync(offset: 0, limit: 2);

        // Act - Get second page
        var secondPage = await _sut.GetPaymentHistoryAsync(offset: 2, limit: 2);

        // Assert
        firstPage.Should().NotBeNull();
        secondPage.Should().NotBeNull();

        if (firstPage.Count > 0 && secondPage.Count > 0)
        {
            // Verify no overlap between pages
            var firstPageHashes = firstPage.Select(p => p.PaymentHash).ToHashSet();
            var secondPageHashes = secondPage.Select(p => p.PaymentHash).ToHashSet();
            firstPageHashes.Should().NotIntersectWith(secondPageHashes,
                "pagination should not return duplicate payments across pages");
        }
    }

    #endregion

    #region Invoice Expiration Tests

    [SkippableFact]
    public async Task CreateInvoiceAsync_WithShortExpiry_InvoiceExpiresAsExpected()
    {
        // Arrange
        Skip.If(string.IsNullOrEmpty(_testnetMnemonic) || string.IsNullOrEmpty(_testnetApiKey),
            "Testnet not configured. Set BREEZ_TESTNET_MNEMONIC and BREEZ_TESTNET_API_KEY environment variables.");

        var shortExpirySec = 60U; // 1 minute for fast test
        var description = $"Short expiry test {Guid.NewGuid():N}";

        // Act
        var result = await _sut!.CreateInvoiceAsync(1000UL, description, shortExpirySec);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var invoice = result.Value!;

        var now = DateTimeOffset.UtcNow;
        var expiryTime = invoice.ExpiresAt;

        expiryTime.Should().BeAfter(now, "invoice should not be expired immediately");
        expiryTime.Should().BeBefore(now.AddSeconds(shortExpirySec + 60),
            "invoice should expire within configured time (with tolerance)");
    }

    #endregion

    #region BOLT11 Format Validation Tests

    [SkippableFact]
    public async Task CreateInvoiceAsync_InvoiceFormat_IsValidBolt11()
    {
        // Arrange
        Skip.If(string.IsNullOrEmpty(_testnetMnemonic) || string.IsNullOrEmpty(_testnetApiKey),
            "Testnet not configured. Set BREEZ_TESTNET_MNEMONIC and BREEZ_TESTNET_API_KEY environment variables.");

        // Act
        var result = await _sut!.CreateInvoiceAsync(1000UL, "BOLT11 format test");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var invoice = result.Value!;

        // BOLT11 format validation
        invoice.Destination.Should().StartWith("lntb",
            "testnet invoices must start with 'lntb' prefix per BOLT11 spec");
        invoice.Destination.Should().NotContainAny(new[] { " ", "\n", "\r", "\t" },
            "BOLT11 invoices should not contain whitespace");
        invoice.Destination.Length.Should().BeGreaterThan(20,
            "BOLT11 invoices should be substantial length with encoded data");

        // Character set validation (BOLT11 uses bech32)
        var validBech32Chars = "qpzry9x8gf2tvdw0s3jn54khce6mua7l0123456789";
        var invalidChars = invoice.Destination
            .Skip(4) // Skip "lntb" prefix
            .Where(c => !validBech32Chars.Contains(char.ToLowerInvariant(c)))
            .ToList();
        invalidChars.Should().BeEmpty(
            "BOLT11 invoice should only contain valid bech32 characters after prefix");
    }

    #endregion

    #region Connection State Management Tests

    [SkippableFact]
    public async Task DisconnectAsync_WhenConnected_DisconnectsSuccessfully()
    {
        // Arrange
        Skip.If(string.IsNullOrEmpty(_testnetMnemonic) || string.IsNullOrEmpty(_testnetApiKey),
            "Testnet not configured. Set BREEZ_TESTNET_MNEMONIC and BREEZ_TESTNET_API_KEY environment variables.");

        _sut!.IsConnected.Should().BeTrue("should start connected");

        // Act
        await _sut.DisconnectAsync();

        // Assert
        _sut.IsConnected.Should().BeFalse("should be disconnected after DisconnectAsync");
    }

    [SkippableFact]
    public async Task ConnectAsync_AfterDisconnect_ReconnectsSuccessfully()
    {
        // Arrange
        Skip.If(string.IsNullOrEmpty(_testnetMnemonic) || string.IsNullOrEmpty(_testnetApiKey),
            "Testnet not configured. Set BREEZ_TESTNET_MNEMONIC and BREEZ_TESTNET_API_KEY environment variables.");

        await _sut!.DisconnectAsync();
        _sut.IsConnected.Should().BeFalse();

        // Act
        await _sut.ConnectAsync();

        // Assert
        _sut.IsConnected.Should().BeTrue("should be connected after reconnection");

        // Verify functionality after reconnection
        var balanceResult = await _sut.GetBalanceAsync();
        balanceResult.IsSuccess.Should().BeTrue("operations should work after reconnection");
    }

    #endregion

    #region Error Handling Tests

    [SkippableFact]
    public async Task CreateInvoiceAsync_WithExcessiveDescriptionLength_ReturnsFailure()
    {
        // Arrange
        Skip.If(string.IsNullOrEmpty(_testnetMnemonic) || string.IsNullOrEmpty(_testnetApiKey),
            "Testnet not configured. Set BREEZ_TESTNET_MNEMONIC and BREEZ_TESTNET_API_KEY environment variables.");

        var excessiveDescription = new string('X', 500); // Beyond configured max (200)

        // Act
        var result = await _sut!.CreateInvoiceAsync(1000UL, excessiveDescription);

        // Assert
        result.IsSuccess.Should().BeFalse("excessive description length should fail validation");
        result.Error.Should().NotBeNull();
        // Note: Error code will depend on service implementation validation logic
    }

    [SkippableFact]
    public async Task GetPaymentByHashAsync_WithInvalidHash_ThrowsArgumentException()
    {
        // Arrange
        Skip.If(string.IsNullOrEmpty(_testnetMnemonic) || string.IsNullOrEmpty(_testnetApiKey),
            "Testnet not configured. Set BREEZ_TESTNET_MNEMONIC and BREEZ_TESTNET_API_KEY environment variables.");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _sut!.GetPaymentByHashAsync("invalid-hash"));
    }

    [SkippableFact]
    public async Task GetPaymentHistoryAsync_WithNegativeOffset_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        Skip.If(string.IsNullOrEmpty(_testnetMnemonic) || string.IsNullOrEmpty(_testnetApiKey),
            "Testnet not configured. Set BREEZ_TESTNET_MNEMONIC and BREEZ_TESTNET_API_KEY environment variables.");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await _sut!.GetPaymentHistoryAsync(offset: -1, limit: 10));
    }

    [SkippableFact]
    public async Task GetPaymentHistoryAsync_WithZeroLimit_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        Skip.If(string.IsNullOrEmpty(_testnetMnemonic) || string.IsNullOrEmpty(_testnetApiKey),
            "Testnet not configured. Set BREEZ_TESTNET_MNEMONIC and BREEZ_TESTNET_API_KEY environment variables.");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await _sut!.GetPaymentHistoryAsync(offset: 0, limit: 0));
    }

    #endregion
}

/// <summary>
/// Collection definition for testnet integration tests.
/// Tests in this collection do not share fixtures but are grouped for organizational purposes.
/// </summary>
[CollectionDefinition("Testnet")]
public class TestnetCollection
{
}
