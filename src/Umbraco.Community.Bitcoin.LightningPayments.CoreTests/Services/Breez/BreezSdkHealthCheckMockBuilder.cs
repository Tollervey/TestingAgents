using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Breez;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Services.Breez
{
    /// <summary>
    /// Builder for creating mock dependencies for BreezSdkHealthCheck tests.
    /// </summary>
    public class BreezSdkHealthCheckMockBuilder
    {
        private Mock<IBreezSdkService>? _mockService;
        private Mock<IHostEnvironment>? _mockHostEnvironment;
        private Mock<IOptions<LightningPaymentsSettings>>? _mockOptions;
        private LightningPaymentsSettings? _settings;

        /// <summary>
        /// Creates a new builder instance.
        /// </summary>
        public static BreezSdkHealthCheckMockBuilder Create()
        {
            return new BreezSdkHealthCheckMockBuilder();
        }

        /// <summary>
        /// Creates a builder with default mocks configured.
        /// </summary>
        public static BreezSdkHealthCheckMockBuilder CreateDefault()
        {
            return Create()
                .WithDefaultMocks()
                .WithDefaultSettings();
        }

        /// <summary>
        /// Configures default mock implementations.
        /// </summary>
        public BreezSdkHealthCheckMockBuilder WithDefaultMocks()
        {
            _mockService = new Mock<IBreezSdkService>();
            _mockHostEnvironment = new Mock<IHostEnvironment>();
            _mockOptions = new Mock<IOptions<LightningPaymentsSettings>>();
            return this;
        }

        /// <summary>
        /// Configures default settings.
        /// </summary>
        public BreezSdkHealthCheckMockBuilder WithDefaultSettings()
        {
            _settings = new LightningPaymentsSettings
            {
                BreezApiKey = "test-api-key",
                Mnemonic = "test mnemonic",
                ConnectionString = "Data Source=:memory:",
                WorkingDirectory = null // Will use default
            };

            GetMockOptions().Setup(o => o.Value).Returns(_settings);
            return this;
        }

        /// <summary>
        /// Configures custom settings.
        /// </summary>
        public BreezSdkHealthCheckMockBuilder WithSettings(LightningPaymentsSettings settings)
        {
            _settings = settings;
            GetMockOptions().Setup(o => o.Value).Returns(_settings);
            return this;
        }

        /// <summary>
        /// Configures custom settings with specific values.
        /// </summary>
        public BreezSdkHealthCheckMockBuilder WithCustomSettings(string? workingDirectory = null)
        {
            _settings = new LightningPaymentsSettings
            {
                BreezApiKey = "test-api-key",
                Mnemonic = "test mnemonic",
                ConnectionString = "Data Source=:memory:",
                WorkingDirectory = workingDirectory
            };

            GetMockOptions().Setup(o => o.Value).Returns(_settings);
            return this;
        }

        /// <summary>
        /// Configures the service mock to return connected state.
        /// </summary>
        public BreezSdkHealthCheckMockBuilder WithConnectedService()
        {
            GetMockService().Setup(s => s.IsConnectedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            return this;
        }

        /// <summary>
        /// Configures the service mock to return disconnected state.
        /// </summary>
        public BreezSdkHealthCheckMockBuilder WithDisconnectedService()
        {
            GetMockService().Setup(s => s.IsConnectedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            return this;
        }

        /// <summary>
        /// Configures the service mock to throw an exception.
        /// </summary>
        public BreezSdkHealthCheckMockBuilder WithServiceException(Exception exception)
        {
            GetMockService().Setup(s => s.IsConnectedAsync(It.IsAny<CancellationToken>())).ThrowsAsync(exception);
            return this;
        }

        /// <summary>
        /// Configures the host environment with a specific content root path.
        /// </summary>
        public BreezSdkHealthCheckMockBuilder WithContentRootPath(string contentRootPath)
        {
            GetMockHostEnvironment().Setup(e => e.ContentRootPath).Returns(contentRootPath);
            return this;
        }

        /// <summary>
        /// Configures a custom action on the service mock.
        /// </summary>
        public BreezSdkHealthCheckMockBuilder WithService(Action<Mock<IBreezSdkService>> configure)
        {
            configure(GetMockService());
            return this;
        }

        /// <summary>
        /// Configures a custom action on the host environment mock.
        /// </summary>
        public BreezSdkHealthCheckMockBuilder WithHostEnvironment(Action<Mock<IHostEnvironment>> configure)
        {
            configure(GetMockHostEnvironment());
            return this;
        }

        /// <summary>
        /// Configures a custom action on the options mock.
        /// </summary>
        public BreezSdkHealthCheckMockBuilder WithOptions(Action<Mock<IOptions<LightningPaymentsSettings>>> configure)
        {
            configure(GetMockOptions());
            return this;
        }

        /// <summary>
        /// Gets or creates the service mock.
        /// </summary>
        public Mock<IBreezSdkService> GetMockService()
        {
            _mockService ??= new Mock<IBreezSdkService>();
            return _mockService;
        }

        /// <summary>
        /// Gets or creates the host environment mock.
        /// </summary>
        public Mock<IHostEnvironment> GetMockHostEnvironment()
        {
            _mockHostEnvironment ??= new Mock<IHostEnvironment>();
            return _mockHostEnvironment;
        }

        /// <summary>
        /// Gets or creates the options mock.
        /// </summary>
        public Mock<IOptions<LightningPaymentsSettings>> GetMockOptions()
        {
            _mockOptions ??= new Mock<IOptions<LightningPaymentsSettings>>();
            return _mockOptions;
        }

        /// <summary>
        /// Gets the current settings.
        /// </summary>
        public LightningPaymentsSettings GetSettings()
        {
            if (_settings == null)
            {
                WithDefaultSettings();
            }
            return _settings!;
        }

        /// <summary>
        /// Builds the BreezSdkHealthCheck instance.
        /// </summary>
        public BreezSdkHealthCheck Build()
        {
            return new BreezSdkHealthCheck(
                GetMockService().Object,
                GetMockHostEnvironment().Object,
                GetMockOptions().Object
            );
        }

        /// <summary>
        /// Gets all mocks as a tuple.
        /// </summary>
        public (Mock<IBreezSdkService> Service,
                Mock<IHostEnvironment> HostEnvironment,
                Mock<IOptions<LightningPaymentsSettings>> Options) GetAllMocks()
        {
            return (GetMockService(), GetMockHostEnvironment(), GetMockOptions());
        }
    }
}

