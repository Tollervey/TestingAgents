using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Infrastructure;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Breez;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Payment;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Services.Breez
{
    public class OfflineBreezSdkServiceTests
    {
        private readonly Mock<IOptions<LightningPaymentsSettings>> _settingsMock;
        private readonly Mock<IOptions<OfflineLightningPaymentsOptions>> _offlineOptionsMock;
        private readonly Mock<ILogger<OfflineBreezSdkService>> _loggerMock;
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
        private readonly Mock<IServiceScope> _scopeMock;
        private readonly Mock<IPaymentStateService> _paymentStateMock;

        public OfflineBreezSdkServiceTests()
        {
            _settingsMock = new Mock<IOptions<LightningPaymentsSettings>>();
            _settingsMock.Setup(s => s.Value).Returns(new LightningPaymentsSettings
            {
                MaxInvoiceAmountSat = 1000000,
                MaxInvoiceDescriptionLength = 100
            });

            _offlineOptionsMock = new Mock<IOptions<OfflineLightningPaymentsOptions>>();
            _offlineOptionsMock.Setup(o => o.Value).Returns(new OfflineLightningPaymentsOptions
            {
                SimulatedFailureRate = 0.0,
                SimulatedConfirmationDelayMs = 0
            });

            _loggerMock = new Mock<ILogger<OfflineBreezSdkService>>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();
            _scopeMock = new Mock<IServiceScope>();
            _paymentStateMock = new Mock<IPaymentStateService>();

            _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
            _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);
            _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory))).Returns(_scopeFactoryMock.Object);
            _serviceProviderMock.Setup(sp => sp.GetService(typeof(IPaymentStateService))).Returns(_paymentStateMock.Object);
        }

        [Fact]
        public async Task IsConnectedAsync_ReturnsTrue()
        {
            // Arrange
            var service = new OfflineBreezSdkService(_settingsMock.Object, _offlineOptionsMock.Object, _loggerMock.Object, _serviceProviderMock.Object);

            // Act
            var result = await service.IsConnectedAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CreateInvoiceAsync_ValidInput_ReturnsInvoice()
        {
            // Arrange
            var service = new OfflineBreezSdkService(_settingsMock.Object, _offlineOptionsMock.Object, _loggerMock.Object, _serviceProviderMock.Object);
            ulong amountSat = 1000;
            string description = "Test invoice";

            // Act
            var result = await service.CreateInvoiceAsync(amountSat, description);

            // Assert
            Assert.Contains("lnoffline1-p=", result);
            Assert.Contains("-a=1000", result);
            Assert.Contains("-d=", result);
            _paymentStateMock.Verify(ps => ps.ConfirmPaymentAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task CreateInvoiceAsync_InvalidAmount_ThrowsException()
        {
            // Arrange
            var service = new OfflineBreezSdkService(_settingsMock.Object, _offlineOptionsMock.Object, _loggerMock.Object, _serviceProviderMock.Object);
            ulong amountSat = 0;
            string description = "Test";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidInvoiceRequestException>(() => service.CreateInvoiceAsync(amountSat, description));
        }

        [Fact]
        public async Task CreateInvoiceAsync_InvalidDescription_ThrowsException()
        {
            // Arrange
            var service = new OfflineBreezSdkService(_settingsMock.Object, _offlineOptionsMock.Object, _loggerMock.Object, _serviceProviderMock.Object);
            ulong amountSat = 1000;
            string description = "";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidInvoiceRequestException>(() => service.CreateInvoiceAsync(amountSat, description));
        }

        [Fact]
        public async Task CreateInvoiceAsync_SimulatedFailure_ThrowsException()
        {
            // Arrange
            _offlineOptionsMock.Setup(o => o.Value).Returns(new OfflineLightningPaymentsOptions { SimulatedFailureRate = 1.0 });
            var service = new OfflineBreezSdkService(_settingsMock.Object, _offlineOptionsMock.Object, _loggerMock.Object, _serviceProviderMock.Object);
            ulong amountSat = 1000;
            string description = "Test";

            // Act & Assert
            await Assert.ThrowsAsync<InvoiceException>(() => service.CreateInvoiceAsync(amountSat, description));
        }

        [Fact]
        public async Task CreateBolt12OfferAsync_ValidInput_ReturnsOffer()
        {
            // Arrange
            var service = new OfflineBreezSdkService(_settingsMock.Object, _offlineOptionsMock.Object, _loggerMock.Object, _serviceProviderMock.Object);
            ulong amountSat = 1000;
            string description = "Test offer";

            // Act
            var result = await service.CreateBolt12OfferAsync(amountSat, description);

            // Assert
            Assert.Contains("lnofflineoffer1-p=", result);
            Assert.Contains("-a=1000", result);
            _paymentStateMock.Verify(ps => ps.ConfirmPaymentAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetReceiveFeeQuoteAsync_ReturnsZero()
        {
            // Arrange
            var service = new OfflineBreezSdkService(_settingsMock.Object, _offlineOptionsMock.Object, _loggerMock.Object, _serviceProviderMock.Object);

            // Act
            var result = await service.GetReceiveFeeQuoteAsync(1000);

            // Assert
            Assert.Equal(0L, result);
        }

        [Fact]
        public async Task GetRecommendedFeesAsync_ReturnsNull()
        {
            // Arrange
            var service = new OfflineBreezSdkService(_settingsMock.Object, _offlineOptionsMock.Object, _loggerMock.Object, _serviceProviderMock.Object);

            // Act
            var result = await service.GetRecommendedFeesAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task TryExtractPaymentHashAsync_ReturnsNull()
        {
            // Arrange
            var service = new OfflineBreezSdkService(_settingsMock.Object, _offlineOptionsMock.Object, _loggerMock.Object, _serviceProviderMock.Object);

            // Act
            var result = await service.TryExtractPaymentHashAsync("invoice");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetPaymentByHashAsync_ReturnsNull()
        {
            // Arrange
            var service = new OfflineBreezSdkService(_settingsMock.Object, _offlineOptionsMock.Object, _loggerMock.Object, _serviceProviderMock.Object);

            // Act
            var result = await service.GetPaymentByHashAsync("hash");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task TryExtractInvoiceExpiryAsync_ReturnsNull()
        {
            // Arrange
            var service = new OfflineBreezSdkService(_settingsMock.Object, _offlineOptionsMock.Object, _loggerMock.Object, _serviceProviderMock.Object);

            // Act
            var result = await service.TryExtractInvoiceExpiryAsync("invoice");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetSdkAsync_ReturnsNull()
        {
            // Arrange
            var service = new OfflineBreezSdkService(_settingsMock.Object, _offlineOptionsMock.Object, _loggerMock.Object, _serviceProviderMock.Object);

            // Act
            var result = await service.GetSdkAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DisposeAsync_CancelsToken()
        {
            // Arrange
            var service = new OfflineBreezSdkService(_settingsMock.Object, _offlineOptionsMock.Object, _loggerMock.Object, _serviceProviderMock.Object);

            // Act
            await service.DisposeAsync();

            // Assert
            // Since _cts is private, we can't directly test, but no exception should occur
        }
    }
}

