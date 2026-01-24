using Moq;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Payment;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Health;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Services.Health
{
    /// <summary>
    /// Builder for creating mock dependencies for LightningPaymentsHealthCheck tests.
    /// </summary>
    public class LightningPaymentsHealthCheckMockBuilder
    {
        private Mock<IPaymentStateService>? _mockPaymentStateService;

        /// <summary>
        /// Creates a new builder instance.
        /// </summary>
        public static LightningPaymentsHealthCheckMockBuilder Create()
        {
            return new LightningPaymentsHealthCheckMockBuilder();
        }

        /// <summary>
        /// Creates a builder with default mocks configured.
        /// </summary>
        public static LightningPaymentsHealthCheckMockBuilder CreateDefault()
        {
            return Create()
                .WithDefaultMocks();
        }

        /// <summary>
        /// Configures default mock implementations.
        /// </summary>
        public LightningPaymentsHealthCheckMockBuilder WithDefaultMocks()
        {
            _mockPaymentStateService = new Mock<IPaymentStateService>();
            return this;
        }

        /// <summary>
        /// Configures the payment state service mock to return healthy state.
        /// </summary>
        public LightningPaymentsHealthCheckMockBuilder WithHealthyService()
        {
            GetMockPaymentStateService().Setup(s => s.IsServiceHealthyAsync()).ReturnsAsync(true);
            return this;
        }

        /// <summary>
        /// Configures the payment state service mock to return unhealthy state.
        /// </summary>
        public LightningPaymentsHealthCheckMockBuilder WithUnhealthyService()
        {
            GetMockPaymentStateService().Setup(s => s.IsServiceHealthyAsync()).ReturnsAsync(false);
            return this;
        }

        /// <summary>
        /// Configures the payment state service mock to throw an exception.
        /// </summary>
        public LightningPaymentsHealthCheckMockBuilder WithServiceException(Exception exception)
        {
            GetMockPaymentStateService().Setup(s => s.IsServiceHealthyAsync()).ThrowsAsync(exception);
            return this;
        }

        /// <summary>
        /// Configures a custom action on the payment state service mock.
        /// </summary>
        public LightningPaymentsHealthCheckMockBuilder WithPaymentStateService(Action<Mock<IPaymentStateService>> configure)
        {
            configure(GetMockPaymentStateService());
            return this;
        }

        /// <summary>
        /// Gets or creates the payment state service mock.
        /// </summary>
        public Mock<IPaymentStateService> GetMockPaymentStateService()
        {
            _mockPaymentStateService ??= new Mock<IPaymentStateService>();
            return _mockPaymentStateService;
        }

        /// <summary>
        /// Builds the LightningPaymentsHealthCheck instance.
        /// </summary>
        public LightningPaymentsHealthCheck Build()
        {
            return new LightningPaymentsHealthCheck(
                GetMockPaymentStateService().Object
            );
        }

        /// <summary>
        /// Gets all mocks as a tuple.
        /// </summary>
        public Mock<IPaymentStateService> GetAllMocks()
        {
            return GetMockPaymentStateService();
        }
    }
}