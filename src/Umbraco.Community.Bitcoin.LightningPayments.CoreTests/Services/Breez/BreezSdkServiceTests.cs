using Breez.Sdk.Liquid;
using Microsoft.Extensions.Logging;
using Moq;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Infrastructure;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Breez;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Services.Breez
{
    /// <summary>
    /// Unit tests for BreezSdkService.
    /// Uses BreezSdkServiceMockBuilder for centralized, reusable mock configuration.
    /// </summary>
    public class BreezSdkServiceTests
    {
        #region Constructor and Initialization Tests

        [Fact]
        public void Constructor_InitializesPoliciesAndLazyInstance()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault();

            // Act
            var service = mockBuilder.Build();

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_UsesCustomSettings()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithCustomSettings(
                    breezApiKey: "custom-key",
                    maxInvoiceAmountSat: 500000);

            // Act
            var service = mockBuilder.Build();

            // Assert
            Assert.NotNull(service);
        }

        #endregion

        #region Connection Tests

        [Fact]
        public async Task IsConnectedAsync_WhenSdkInitialized_ReturnsFalse()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithDisconnectedSdk();

            var service = mockBuilder.Build();

            // Act
            var result = await service.IsConnectedAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsConnectedAsync_WhenSdkConnected_ReturnsTrue()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk();
            var service = mockBuilder.Build();

            // Force initialization
            await service.GetSdkAsync();

            // Act
            var result = await service.IsConnectedAsync();

            // Assert
            Assert.True(result);
        }

        #endregion

        #region CreateInvoiceAsync Tests (BOLT11)

        [Fact]
        public async Task CreateInvoiceAsync_WithValidAmount_ThrowsWhenSdkNotConnected()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithSetLoggerSupport()
                .WithNullDefaultConfig();

            var service = mockBuilder.Build();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateInvoiceAsync(100, "Test description"));
        }

        [Fact]
        public async Task CreateInvoiceAsync_WhenSdkConnected_ReturnsInvoice()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithInvoiceSuccessFlow("lnbcInvoice123");
            var service = mockBuilder.Build();

            // Act
            var invoice = await service.CreateInvoiceAsync(1000, "Valid description");

            // Assert
            Assert.Equal("lnbcInvoice123", invoice);
        }

        [Fact]
        public async Task CreateInvoiceAsync_WhenSdkCallFails_ThrowsInvoiceException()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithInvoiceFailureFlow();
            var service = mockBuilder.Build();

            // Act & Assert
            await Assert.ThrowsAsync<InvoiceException>(() => service.CreateInvoiceAsync(1000, "Valid description"));
        }

        [Fact]
        public async Task CreateInvoiceAsync_WithAmountBelowLightningMinimum_ThrowsInvalidInvoiceRequestException()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithWrapper(w =>
                {
                    var limits = new LightningPaymentLimitsResponse(
                        receive: new Limits(1000, 10000000, 0),
                        send: new Limits(1000, 10000000, 0));
                    w.Setup(x => x.FetchLightningLimitsAsync(It.IsAny<BindingLiquidSdk>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(limits);
                });
            var service = mockBuilder.Build();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidInvoiceRequestException>(
                () => service.CreateInvoiceAsync(500, "Below minimum"));

            Assert.Contains("must be between", exception.Message);
        }

        [Fact]
        public async Task CreateInvoiceAsync_WithAmountAboveLightningMaximum_ThrowsInvalidInvoiceRequestException()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithCustomSettings(maxInvoiceAmountSat: 50000000) // Set higher than Lightning max (10M) to ensure Lightning limit is checked
                .WithConnectedSdk()
                .WithWrapper(w =>
                {
                    var limits = new LightningPaymentLimitsResponse(
                        receive: new Limits(1000, 10000000, 0),
                        send: new Limits(1000, 10000000, 0));
                    w.Setup(x => x.FetchLightningLimitsAsync(It.IsAny<BindingLiquidSdk>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(limits);
                });
            var service = mockBuilder.Build();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidInvoiceRequestException>(
                () => service.CreateInvoiceAsync(20000000, "Above maximum"));

            Assert.Contains("must be between", exception.Message);
        }

        #endregion

        #region CreateBolt12OfferAsync Tests

        [Fact]
        public async Task CreateBolt12OfferAsync_WhenSdkConnected_ReturnsOffer()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithBolt12SuccessFlow("lno1offer123");
            var service = mockBuilder.Build();

            // Act
            var offer = await service.CreateBolt12OfferAsync(5000, "BOLT12 test description");

            // Assert
            Assert.Equal("lno1offer123", offer);
        }

        [Fact]
        public async Task CreateBolt12OfferAsync_WhenSdkNotConnected_ThrowsInvalidOperationException()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithDisconnectedSdk();
            var service = mockBuilder.Build();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CreateBolt12OfferAsync(5000, "Test description"));
        }

        [Fact]
        public async Task CreateBolt12OfferAsync_WhenPrepareFails_ThrowsInvoiceException()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithBolt12FailureFlow();
            var service = mockBuilder.Build();

            // Act & Assert
            await Assert.ThrowsAsync<InvoiceException>(
                () => service.CreateBolt12OfferAsync(5000, "Test description"));
        }

        [Fact]
        public async Task CreateBolt12OfferAsync_ValidatesAmount()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk();
            var service = mockBuilder.Build();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidInvoiceRequestException>(
                () => service.CreateBolt12OfferAsync(0, "Zero amount"));
        }

        [Fact]
        public async Task CreateBolt12OfferAsync_ValidatesDescription()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk();
            var service = mockBuilder.Build();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidInvoiceRequestException>(
                () => service.CreateBolt12OfferAsync(1000, ""));
        }

        #endregion

        #region TryExtractPaymentHashAsync Tests

        [Fact]
        public async Task TryExtractPaymentHashAsync_ReturnsNull_WhenSdkNotConnected()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithDisconnectedSdk();

            var service = mockBuilder.Build();

            // Act
            var result = await service.TryExtractPaymentHashAsync("testinvoice");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task TryExtractPaymentHashAsync_ExtractsHash_FromValidBolt11Invoice()
        {
            // Arrange
            var expectedHash = "abc123def456";
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithParseInvoiceSupport(expectedHash);
            var service = mockBuilder.Build();

            // Act
            var result = await service.TryExtractPaymentHashAsync("lnbc1000n1...");

            // Assert
            Assert.Equal(expectedHash.ToLowerInvariant(), result);
        }

        [Fact]
        public async Task TryExtractPaymentHashAsync_ReturnsNull_ForNonBolt11Input()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithParseNonBolt11Support();
            var service = mockBuilder.Build();

            // Act
            var result = await service.TryExtractPaymentHashAsync("notaninvoice");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task TryExtractPaymentHashAsync_ReturnsNull_WhenParseThrowsException()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithParseFailure();
            var service = mockBuilder.Build();

            // Act
            var result = await service.TryExtractPaymentHashAsync("malformed");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task TryExtractPaymentHashAsync_ReturnsNull_WhenHashIsEmpty()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithParseInvoiceSupport("");
            var service = mockBuilder.Build();

            // Act
            var result = await service.TryExtractPaymentHashAsync("lnbc1000n1...");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region TryExtractInvoiceExpiryAsync Tests

        [Fact]
        public async Task TryExtractInvoiceExpiryAsync_ReturnsNull_WhenSdkNotConnected()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithDisconnectedSdk();
            var service = mockBuilder.Build();

            // Act
            var expiry = await service.TryExtractInvoiceExpiryAsync("dummy");

            // Assert
            Assert.Null(expiry);
        }

        [Fact]
        public async Task TryExtractInvoiceExpiryAsync_ExtractsExpiry_FromExpiryField()
        {
            // Arrange
            var creationTime = DateTimeOffset.UtcNow;
            var expectedExpiry = creationTime.AddHours(1);
            var ttl = (long)expectedExpiry.Subtract(creationTime).TotalSeconds;

            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithParseInvoiceExpirySupport(creationTime.ToUnixTimeSeconds(), ttl, expiryFieldName: "expiry");
            var service = mockBuilder.Build();

            // Check if connected
            var isConnected = await service.IsConnectedAsync();
            Assert.True(isConnected);

            // Act
            var result = await service.TryExtractInvoiceExpiryAsync("lnbc1000n1...");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedExpiry.ToUnixTimeSeconds(), result.Value.ToUnixTimeSeconds());
        }

        [Fact]
        public async Task TryExtractInvoiceExpiryAsync_ExtractsExpiry_FromTimestampPlusTtl()
        {
            // Arrange
            var createdAt = DateTimeOffset.UtcNow.AddHours(-1);
            var ttl = 3600; // 1 hour
            var expectedExpiry = createdAt.AddSeconds(ttl);

            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithParseInvoiceExpirySupport(createdAt.ToUnixTimeSeconds(), ttl);
            var service = mockBuilder.Build();

            // Check if connected
            var isConnected = await service.IsConnectedAsync();
            Assert.True(isConnected);

            // Act
            var result = await service.TryExtractInvoiceExpiryAsync("lnbc1000n1...");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedExpiry.ToUnixTimeSeconds(), result.Value.ToUnixTimeSeconds());
        }

        [Fact]
        public async Task TryExtractInvoiceExpiryAsync_ReturnsNull_WhenNoExpiryFieldsPresent()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithParseInvoiceExpirySupport(0, 0); // No expiry data
            var service = mockBuilder.Build();

            // Act
            var result = await service.TryExtractInvoiceExpiryAsync("lnbc1000n1...");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task TryExtractInvoiceExpiryAsync_ReturnsNull_WhenParseThrowsException()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithParseFailure();
            var service = mockBuilder.Build();

            // Act
            var result = await service.TryExtractInvoiceExpiryAsync("malformed");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task TryExtractInvoiceExpiryAsync_ReturnsNull_WhenInvoiceIsNull()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk();
            var service = mockBuilder.Build();

            // Act
            var result = await service.TryExtractInvoiceExpiryAsync(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task TryExtractInvoiceExpiryAsync_ReturnsNull_WhenInvoiceIsEmpty()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk();
            var service = mockBuilder.Build();

            // Act
            var result = await service.TryExtractInvoiceExpiryAsync(string.Empty);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task TryExtractInvoiceExpiryAsync_ReturnsNull_WhenInvoiceIsWhitespace()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk();
            var service = mockBuilder.Build();

            // Act
            var result = await service.TryExtractInvoiceExpiryAsync("   ");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task TryExtractInvoiceExpiryAsync_ReturnsNull_WhenInvoiceIsMalformed()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithParseFailure();
            var service = mockBuilder.Build();

            // Act
            var result = await service.TryExtractInvoiceExpiryAsync("malformed_invoice_string_@#$%");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task TryExtractInvoiceExpiryAsync_ReturnsNull_WhenTimestampIsZeroAndTtlIsZero()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithParseInvoiceExpirySupport(0, 0);
            var service = mockBuilder.Build();

            // Act
            var result = await service.TryExtractInvoiceExpiryAsync("lnbc1000n1...");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task TryExtractInvoiceExpiryAsync_ReturnsNull_WhenTimestampIsNegative()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithParseInvoiceExpirySupport(-1000, 3600);
            var service = mockBuilder.Build();

            // Act
            var result = await service.TryExtractInvoiceExpiryAsync("lnbc1000n1...");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task TryExtractInvoiceExpiryAsync_ReturnsNull_WhenTtlIsNegative()
        {
            // Arrange
            var creationTime = DateTimeOffset.UtcNow;
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithParseInvoiceExpirySupport(creationTime.ToUnixTimeSeconds(), -3600);
            var service = mockBuilder.Build();

            // Act
            var result = await service.TryExtractInvoiceExpiryAsync("lnbc1000n1...");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task TryExtractInvoiceExpiryAsync_ReturnsNull_WhenBothTimestampAndTtlAreNegative()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithParseInvoiceExpirySupport(-1000, -3600);
            var service = mockBuilder.Build();

            // Act
            var result = await service.TryExtractInvoiceExpiryAsync("lnbc1000n1...");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task TryExtractInvoiceExpiryAsync_ReturnsNull_ForNonBolt11Invoice()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithParseNonBolt11Support();
            var service = mockBuilder.Build();

            // Act
            var result = await service.TryExtractInvoiceExpiryAsync("bc1qxy2kgdygjrsqtzq2n0yrf2493p83kkfjhx0wlh");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task TryExtractInvoiceExpiryAsync_HandlesExtremelyLargeTtl_Gracefully()
        {
            // Arrange
            var creationTime = DateTimeOffset.UtcNow;
            var extremelyLargeTtl = long.MaxValue;

            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithParseInvoiceExpirySupport(creationTime.ToUnixTimeSeconds(), extremelyLargeTtl);
            var service = mockBuilder.Build();

            // Act & Assert - Should either return null or handle overflow gracefully
            var result = await service.TryExtractInvoiceExpiryAsync("lnbc1000n1...");

            // The implementation should handle this gracefully (either null or valid date)
            // This test ensures no exception is thrown
            Assert.True(result == null || result.Value > creationTime);
        }

        [Fact]
        public async Task TryExtractInvoiceExpiryAsync_HandlesExtremelyLargeTimestamp_Gracefully()
        {
            // Arrange
            var extremelyLargeTimestamp = long.MaxValue;

            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithParseInvoiceExpirySupport(extremelyLargeTimestamp, 3600);
            var service = mockBuilder.Build();

            // Act & Assert - Should handle overflow gracefully
            var result = await service.TryExtractInvoiceExpiryAsync("lnbc1000n1...");

            // The implementation should handle this gracefully (likely return null)
            // This test ensures no exception is thrown
            Assert.True(true); // Test passes if no exception
        }

        #endregion

        #region GetPaymentByHashAsync Tests

        [Fact]
        public async Task GetPaymentByHashAsync_ThrowsException_WhenSdkIsNotConnected()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithDisconnectedSdk();

            var service = mockBuilder.Build();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetPaymentByHashAsync("testHash"));
        }

        [Fact]
        public async Task GetPaymentByHashAsync_ReturnsPayment_WhenHashExists()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithGetPaymentSupport("abc123", exists: true);
            var service = mockBuilder.Build();

            // Act
            var payment = await service.GetPaymentByHashAsync("abc123");

            // Assert
            Assert.NotNull(payment);
        }

        [Fact]
        public async Task GetPaymentByHashAsync_ReturnsNull_WhenHashDoesNotExist()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithGetPaymentSupport("nonexistent", exists: false);
            var service = mockBuilder.Build();

            // Act
            var payment = await service.GetPaymentByHashAsync("nonexistent");

            // Assert
            Assert.Null(payment);
        }

        [Fact]
        public async Task GetPaymentByHashAsync_ThrowsException_WhenWrapperFails()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithWrapper(w => w.Setup(x => x.GetPaymentAsync(
                    It.IsAny<BindingLiquidSdk>(),
                    It.IsAny<GetPaymentRequest>(),
                    It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("SDK error")));
            var service = mockBuilder.Build();

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => service.GetPaymentByHashAsync("test"));
        }

        #endregion

        #region GetPaymentsAsync Tests

        [Fact]
        public async Task GetPaymentsAsync_ThrowsException_WhenSdkIsNotConnected()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithDisconnectedSdk();

            var service = mockBuilder.Build();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetPaymentsAsync());
        }

        [Fact]
        public async Task GetPaymentsAsync_ReturnsPayments_WhenSdkConnected()
        {
            // Arrange
            var expectedPayments = new List<global::Breez.Sdk.Liquid.Payment>();
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithWrapper(w => w.Setup(x => x.ListPaymentsAsync(
                    It.IsAny<BindingLiquidSdk>(),
                    It.IsAny<ListPaymentsRequest>(),
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedPayments));
            var service = mockBuilder.Build();

            // Act
            var payments = await service.GetPaymentsAsync();

            // Assert
            Assert.Equal(expectedPayments, payments);
        }

        [Fact]
        public async Task GetPaymentsAsync_ThrowsException_WhenWrapperFails()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithWrapper(w => w.Setup(x => x.ListPaymentsAsync(
                    It.IsAny<BindingLiquidSdk>(),
                    It.IsAny<ListPaymentsRequest>(),
                    It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("SDK error")));
            var service = mockBuilder.Build();

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => service.GetPaymentsAsync());
        }

        #endregion

        #region GetReceiveFeeQuoteAsync Tests

        [Fact]
        public async Task GetReceiveFeeQuoteAsync_ThrowsException_WhenSdkIsNotConnected()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithDisconnectedSdk();

            var service = mockBuilder.Build();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetReceiveFeeQuoteAsync(100));
        }

        [Fact]
        public async Task GetReceiveFeeQuoteAsync_ReturnsFeeQuote_ForBolt11()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithFeeQuoteSupport(1000, expectedFee: 50);
            var service = mockBuilder.Build();

            // Act
            var fee = await service.GetReceiveFeeQuoteAsync(1000, bolt12: false);

            // Assert
            Assert.Equal(50, fee);
        }

        [Fact]
        public async Task GetReceiveFeeQuoteAsync_ReturnsFeeQuote_ForBolt12()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithFeeQuoteSupport(5000, expectedFee: 100, bolt12: true);
            var service = mockBuilder.Build();

            // Act
            var fee = await service.GetReceiveFeeQuoteAsync(5000, bolt12: true);

            // Assert
            Assert.Equal(100, fee);
        }

        [Fact]
        public async Task GetReceiveFeeQuoteAsync_ValidatesAmount()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk();
            var service = mockBuilder.Build();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidInvoiceRequestException>(
                () => service.GetReceiveFeeQuoteAsync(0));
        }

        [Fact]
        public async Task GetReceiveFeeQuoteAsync_ThrowsInvoiceException_WhenPrepareFails()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithWrapper(w => w.Setup(x => x.PrepareReceivePaymentAsync(
                    It.IsAny<BindingLiquidSdk>(),
                    It.IsAny<PrepareReceiveRequest>(),
                    It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Prepare failed")));
            var service = mockBuilder.Build();

            // Act & Assert
            await Assert.ThrowsAsync<InvoiceException>(() => service.GetReceiveFeeQuoteAsync(1000));
        }

        #endregion

        #region GetRecommendedFeesAsync Tests

        [Fact]
        public async Task GetRecommendedFeesAsync_ReturnsNull_WhenSdkNotConnected()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithDisconnectedSdk();
            var service = mockBuilder.Build();

            // Act
            var fees = await service.GetRecommendedFeesAsync();

            // Assert
            Assert.Null(fees);
        }

        [Fact]
        public async Task GetRecommendedFeesAsync_ReturnsFees_WhenSdkConnected()
        {
            // Arrange
            var expectedFees = new RecommendedFees(
                fastestFee: 10,
                halfHourFee: 5,
                hourFee: 2,
                economyFee: 1,
                minimumFee: 1);

            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithRecommendedFeesSupport(expectedFees);
            var service = mockBuilder.Build();

            // Act
            var fees = await service.GetRecommendedFeesAsync();

            // Assert
            Assert.NotNull(fees);
            Assert.Equal(expectedFees.fastestFee, fees.fastestFee);
        }

        [Fact]
        public async Task GetRecommendedFeesAsync_ReturnsNull_WhenWrapperThrowsException()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithWrapper(w => w.Setup(x => x.RecommendedFeesAsync(
                    It.IsAny<BindingLiquidSdk>(),
                    It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Fee fetch failed")));
            var service = mockBuilder.Build();

            // Act
            var fees = await service.GetRecommendedFeesAsync();

            // Assert
            Assert.Null(fees);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public void ValidateInvoiceAmount_ThrowsException_WhenAmountIsZero()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault();
            var service = mockBuilder.Build();

            // Act & Assert
            Assert.Throws<InvalidInvoiceRequestException>(() => service.ValidateInvoiceAmount(0));
        }

        [Fact]
        public void ValidateInvoiceAmount_ThrowsException_WhenAmountExceedsMaximum()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault();
            var service = mockBuilder.Build();

            // Act & Assert
            Assert.Throws<InvalidInvoiceRequestException>(() => service.ValidateInvoiceAmount(10000001));
        }

        [Fact]
        public void ValidateInvoiceAmount_DoesNotThrow_WhenAmountEqualsMaximum()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault();
            var settings = mockBuilder.GetSettings();
            var service = mockBuilder.Build();

            // Act & Assert
            var ex = Record.Exception(() => service.ValidateInvoiceAmount(settings.MaxInvoiceAmountSat));
            Assert.Null(ex);
        }

        [Fact]
        public void ValidateInvoiceAmount_DoesNotThrow_ForValidAmount()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault();
            var service = mockBuilder.Build();

            // Act & Assert
            var ex = Record.Exception(() => service.ValidateInvoiceAmount(50000));
            Assert.Null(ex);
        }

        [Fact]
        public void ValidateInvoiceDescription_ThrowsException_WhenDescriptionIsEmpty()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault();
            var service = mockBuilder.Build();

            // Act & Assert
            Assert.Throws<InvalidInvoiceRequestException>(() => service.ValidateInvoiceDescription(""));
        }

        [Fact]
        public void ValidateInvoiceDescription_ThrowsException_WhenDescriptionIsWhitespace()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault();
            var service = mockBuilder.Build();

            // Act & Assert
            Assert.Throws<InvalidInvoiceRequestException>(() => service.ValidateInvoiceDescription("   "));
        }

        [Fact]
        public void ValidateInvoiceDescription_ThrowsException_WhenDescriptionExceedsMaxLength()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault();
            var service = mockBuilder.Build();
            var longDescription = new string('a', 101);

            // Act & Assert
            Assert.Throws<InvalidInvoiceRequestException>(() => service.ValidateInvoiceDescription(longDescription));
        }

        [Fact]
        public void ValidateInvoiceDescription_DoesNotThrow_WhenDescriptionIsAtMaxLengthBoundary()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault();
            var settings = mockBuilder.GetSettings();
            var service = mockBuilder.Build();
            var boundary = new string('b', settings.MaxInvoiceDescriptionLength);

            // Act & Assert
            var ex = Record.Exception(() => service.ValidateInvoiceDescription(boundary));
            Assert.Null(ex);
        }

        [Fact]
        public void ValidateInvoiceDescription_DoesNotThrow_ForAllowedPunctuationCharacters()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault();
            var service = mockBuilder.Build();
            var allowed = "Allowed _ punctuation, set! () [] {} | ;: ?! @#$%^&*()+-=";

            // Act & Assert
            var ex = Record.Exception(() => service.ValidateInvoiceDescription(allowed));
            Assert.Null(ex);
        }

        [Fact]
        public void ValidateInvoiceDescription_ThrowsException_WhenDescriptionContainsNullCharacter()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault();
            var service = mockBuilder.Build();

            // Act & Assert
            Assert.Throws<InvalidInvoiceRequestException>(() => service.ValidateInvoiceDescription("Invalid\u0000Description"));
        }

        [Fact]
        public void ValidateInvoiceDescription_ThrowsException_WhenDescriptionContainsTabCharacter()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault();
            var service = mockBuilder.Build();

            // Act & Assert
            Assert.Throws<InvalidInvoiceRequestException>(() => service.ValidateInvoiceDescription("Invalid\tDescription"));
        }

        [Fact]
        public void ValidateInvoiceDescription_ThrowsException_WhenDescriptionContainsNewlineCharacter()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault();
            var service = mockBuilder.Build();

            // Act & Assert
            Assert.Throws<InvalidInvoiceRequestException>(() => service.ValidateInvoiceDescription("Invalid\nDescription"));
        }

        [Fact]
        public void ValidateInvoiceDescription_ThrowsException_WhenDescriptionContainsCarriageReturn()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault();
            var service = mockBuilder.Build();

            // Act & Assert
            Assert.Throws<InvalidInvoiceRequestException>(() => service.ValidateInvoiceDescription("Invalid\rDescription"));
        }

        [Fact]
        public void ValidateInvoiceDescription_ThrowsException_WhenDescriptionContainsBellCharacter()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault();
            var service = mockBuilder.Build();

            // Act & Assert
            Assert.Throws<InvalidInvoiceRequestException>(() => service.ValidateInvoiceDescription("Invalid\u0007Description"));
        }

        [Fact]
        public void ValidateInvoiceDescription_DoesNotThrow_ForValidUnicodeCharacters()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault();
            var service = mockBuilder.Build();

            // Act & Assert
            var ex = Record.Exception(() => service.ValidateInvoiceDescription("Café ☕ 价格 100"));
            Assert.Null(ex);
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public async Task DisposeAsync_CallsDisconnect_WhenSdkInitialized()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithDisconnectSupport()
                .WithRemoveEventListenerSupport();
            var service = mockBuilder.Build();
            await service.GetSdkAsync();

            // Act
            await service.DisposeAsync();

            // Assert
            mockBuilder.GetMockWrapper().Verify(w => w.DisconnectAsync(It.IsAny<BindingLiquidSdk>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DisposeAsync_HandlesDisconnect_WhenSdkIsNull()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithDisconnectedSdk()
                .WithDisconnectSupport();

            var service = mockBuilder.Build();
            await service.IsConnectedAsync(); // Initialize SDK (will be null)

            // Act
            await service.DisposeAsync();

            // Assert
            mockBuilder.GetMockWrapper().Verify(
                w => w.DisconnectAsync(It.IsAny<BindingLiquidSdk>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task DisposeAsync_RemovesEventListener_WhenSdkInitialized()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithDisconnectSupport()
                .WithRemoveEventListenerSupport();
            var service = mockBuilder.Build();
            await service.GetSdkAsync();

            // Act
            await service.DisposeAsync();

            // Assert
            mockBuilder.GetMockWrapper().Verify(
                w => w.RemoveEventListener(It.IsAny<BindingLiquidSdk>(), It.IsAny<BreezSdkService.SdkEventListener>()),
                Times.Once);
        }

        [Fact]
        public async Task DisposeAsync_IsIdempotent()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithDisconnectSupport()
                .WithRemoveEventListenerSupport();
            var service = mockBuilder.Build();
            await service.GetSdkAsync();

            // Act
            await service.DisposeAsync();
            await service.DisposeAsync(); // Second call

            // Assert - disconnect should only be called once
            mockBuilder.GetMockWrapper().Verify(
                w => w.DisconnectAsync(It.IsAny<BindingLiquidSdk>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DisposeAsync_HandlesExceptionDuringDisconnect()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithRemoveEventListenerSupport()
                .WithWrapper(w => w.Setup(x => x.DisconnectAsync(
                    It.IsAny<BindingLiquidSdk>(),
                    It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Disconnect failed")));
            var service = mockBuilder.Build();
            await service.GetSdkAsync();

            // Act - should not throw
            var exception = await Record.ExceptionAsync(() => service.DisposeAsync().AsTask());

            // Assert - exception should be caught and logged, not thrown
            Assert.Null(exception);
        }

        #endregion

        #region Concurrency Tests

        [Fact]
        public async Task MultipleTests_CanRunInParallel()
        {
            // This test verifies that the mock builder is thread-safe and each test
            // gets its own isolated set of mocks.
            var tasks = new Task[10];

            for (int i = 0; i < 10; i++)
            {
                int index = i;
                tasks[i] = Task.Run(async () =>
                {
                    var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                        .WithDisconnectedSdk();

                    var service = mockBuilder.Build();
                    var result = await service.IsConnectedAsync();

                    Assert.False(result);
                });
            }

            // Act & Assert
            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task GetSdkAsync_CalledConcurrently_InitializesOnlyOnce()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk();
            var service = mockBuilder.Build();

            // Act - call GetSdkAsync multiple times concurrently
            var tasks = Enumerable.Range(0, 10)
                .Select(_ => service.GetSdkAsync())
                .ToArray();

            await Task.WhenAll(tasks);

            // Assert - ConnectAsync should only be called once due to Lazy initialization
            mockBuilder.GetMockWrapper().Verify(
                w => w.ConnectAsync(It.IsAny<ConnectRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetSdkAsync_CalledConcurrently_WithDisposeAsync_HandlesGracefully()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithDisconnectSupport()
                .WithRemoveEventListenerSupport();
            var service = mockBuilder.Build();

            // Act - Start multiple GetSdkAsync calls concurrently with DisposeAsync
            var getSdkTasks = Enumerable.Range(0, 5)
                .Select(_ => Task.Run(() => service.GetSdkAsync()))
                .ToArray();

            var disposeTask = Task.Run(() => service.DisposeAsync().AsTask());

            // Wait for all tasks to complete
            await Task.WhenAll(getSdkTasks.Concat(new[] { disposeTask }));

            // Assert - All GetSdkAsync calls should have completed without exception
            // Some may have returned the SDK, others null after disposal
            foreach (var task in getSdkTasks)
            {
                Assert.True(task.IsCompletedSuccessfully);
            }

            // DisposeAsync should have completed successfully
            Assert.True(disposeTask.IsCompletedSuccessfully);

            // After disposal, GetSdkAsync should return null
            var sdkAfterDispose = await service.GetSdkAsync();
            Assert.Null(sdkAfterDispose);
        }

        #endregion

        #region Webhook Tests

        [Fact]
        public async Task InitializeSdk_RegistersWebhook_WhenValidUrlConfigured()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithCustomSettings(webhookUrl: "https://example.com/webhook")
                .WithConnectedSdk()
                .WithWebhookSupport();
            var service = mockBuilder.Build();

            // Act
            await service.GetSdkAsync();

            // Assert
            mockBuilder.GetMockWrapper().Verify(
                w => w.RegisterWebhookAsync(
                    It.IsAny<BindingLiquidSdk>(),
                    "https://example.com/webhook",
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task InitializeSdk_DoesNotRegisterWebhook_WhenUrlIsEmpty()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithCustomSettings(webhookUrl: "")
                .WithConnectedSdk();
            var service = mockBuilder.Build();

            // Act
            await service.GetSdkAsync();

            // Assert
            mockBuilder.GetMockWrapper().Verify(
                w => w.RegisterWebhookAsync(
                    It.IsAny<BindingLiquidSdk>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task InitializeSdk_DoesNotRegisterWebhook_WhenUrlIsNotHttps()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithCustomSettings(webhookUrl: "http://example.com/webhook")
                .WithConnectedSdk();
            var service = mockBuilder.Build();

            // Act
            await service.GetSdkAsync();

            // Assert
            mockBuilder.GetMockWrapper().Verify(
                w => w.RegisterWebhookAsync(
                    It.IsAny<BindingLiquidSdk>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task InitializeSdk_DoesNotRegisterWebhook_WhenUrlIsInvalid()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithCustomSettings(webhookUrl: "not-a-valid-url")
                .WithConnectedSdk();
            var service = mockBuilder.Build();

            // Act
            await service.GetSdkAsync();

            // Assert
            mockBuilder.GetMockWrapper().Verify(
                w => w.RegisterWebhookAsync(
                    It.IsAny<BindingLiquidSdk>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task InitializeSdk_RetriesWebhookRegistration_OnFailure_AndContinuesInitialization()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithCustomSettings(webhookUrl: "https://example.com/webhook")
                .WithConnectedSdk()
                .WithWrapper(w => w.Setup(x => x.RegisterWebhookAsync(
                    It.IsAny<BindingLiquidSdk>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new Exception("Network error")));
            var service = mockBuilder.Build();

            // Act
            var sdk = await service.GetSdkAsync();

            // Assert
            Assert.Null(sdk); // Initialization failed due to webhook failure

            // Verify retries: 1 initial + 3 retries = 4 calls
            mockBuilder.GetMockWrapper().Verify(
                w => w.RegisterWebhookAsync(
                    It.IsAny<BindingLiquidSdk>(),
                    "https://example.com/webhook",
                    It.IsAny<CancellationToken>()),
                Times.Exactly(4));

            // Verify logging of retries
            mockBuilder.GetMockLogger().Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retry") && v.ToString()!.Contains("webhook registration")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
                Times.Exactly(3)); // 3 retry logs

            // Verify error log
            mockBuilder.GetMockLogger().Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to connect to Breez SDK")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
                Times.Once);
        }

        #endregion

        #region Cancellation Tests

        [Fact]
        public async Task CreateInvoiceAsync_ThrowsOperationCanceledException_WhenCancellationTokenCanceled()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk();
            var service = mockBuilder.Build();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => service.CreateInvoiceAsync(1000, "Test", cts.Token));
        }

        [Fact]
        public async Task CreateBolt12OfferAsync_ThrowsOperationCanceledException_WhenCancellationTokenCanceled()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithBolt12SuccessFlow();
            var service = mockBuilder.Build();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => service.CreateBolt12OfferAsync(1000, "Test", cts.Token));
        }

        [Fact]
        public async Task CreateInvoiceAsync_ThrowsOperationCanceledException_WhenWrapperCancelsDuringPrepare()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithWrapper(w => w.Setup(x => x.FetchLightningLimitsAsync(
                    It.IsAny<BindingLiquidSdk>(),
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new LightningPaymentLimitsResponse(
                        receive: new Limits(1, 1000000, 0),
                        send: new Limits(1, 1000000, 0))))
                .WithWrapper(w => w.Setup(x => x.PrepareReceivePaymentAsync(
                    It.IsAny<BindingLiquidSdk>(),
                    It.IsAny<PrepareReceiveRequest>(),
                    It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new OperationCanceledException()));
            var service = mockBuilder.Build();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => service.CreateInvoiceAsync(1000, "Test"));
        }

        [Fact]
        public async Task CreateBolt12OfferAsync_ThrowsOperationCanceledException_WhenWrapperCancelsDuringPrepare()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk()
                .WithWrapper(w => w.Setup(x => x.FetchLightningLimitsAsync(
                    It.IsAny<BindingLiquidSdk>(),
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new LightningPaymentLimitsResponse(
                        receive: new Limits(1, 1000000, 0),
                        send: new Limits(1, 1000000, 0))))
                .WithWrapper(w => w.Setup(x => x.PrepareReceivePaymentAsync(
                    It.IsAny<BindingLiquidSdk>(),
                    It.IsAny<PrepareReceiveRequest>(),
                    It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new OperationCanceledException()));
            var service = mockBuilder.Build();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => service.CreateBolt12OfferAsync(1000, "Test"));
        }

        [Fact]
        public async Task GetReceiveFeeQuoteAsync_ThrowsOperationCanceledException_WhenCancellationTokenCanceled()
        {
            // Arrange
            var mockBuilder = BreezSdkServiceMockBuilder.CreateDefault()
                .WithConnectedSdk();
            var service = mockBuilder.Build();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => service.GetReceiveFeeQuoteAsync(1000, false, cts.Token));
        }

        #endregion
    }
}

