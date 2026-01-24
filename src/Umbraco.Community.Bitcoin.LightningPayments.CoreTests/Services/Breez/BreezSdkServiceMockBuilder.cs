using Breez.Sdk.Liquid;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Breez;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Services.Breez
{
    /// <summary>
    /// Fluent builder for creating and configuring mocks for BreezSdkService testing.
    /// Centralizes mock setup logic and provides a clean, reusable API for test classes.
    /// 
    /// Thread-safe for parallel test execution - each instance maintains its own mock objects.
    /// </summary>
    public class BreezSdkServiceMockBuilder
    {
        private Mock<IOptions<LightningPaymentsSettings>>? _mockOptions;
        private Mock<IHostEnvironment>? _mockHostEnvironment;
        private Mock<ILoggerFactory>? _mockLoggerFactory;
        private Mock<ILogger<BreezSdkService>>? _mockLogger;
        private Mock<IBreezSdkWrapper>? _mockWrapper;
        private Mock<IServiceProvider>? _mockServiceProvider;
        private LightningPaymentsSettings? _settings;

        #region Helper Methods for Creating Test Objects

        // Helper: attempt to construct a Breez Config instance for tests without depending on SDK internals.
        private static Config CreateTestConfig()
        {
            var t = typeof(Config);
            foreach (var ctor in t.GetConstructors())
            {
                try
                {
                    var pars = ctor.GetParameters();
                    var args = new object?[pars.Length];
                    for (int i = 0; i < pars.Length; i++)
                    {
                        var pt = pars[i].ParameterType;
                        args[i] = pt.IsValueType ? Activator.CreateInstance(pt) : null;
                    }
                    var instance = (Config)ctor.Invoke(args);
                    return instance;
                }
                catch
                {
                    // Try next ctor
                }
            }
            throw new InvalidOperationException("Unable to construct Breez.Sdk.Liquid.Config for test.");
        }

        // Helper: construct BindingLiquidSdk via reflection
        private static BindingLiquidSdk CreateTestBindingLiquidSdk()
        {
            var t = typeof(BindingLiquidSdk);
            foreach (var ctor in t.GetConstructors())
            {
                try
                {
                    var pars = ctor.GetParameters();
                    var args = new object?[pars.Length];
                    for (int i = 0; i < pars.Length; i++)
                    {
                        var pt = pars[i].ParameterType;
                        args[i] = pt.IsValueType ? Activator.CreateInstance(pt) : null;
                    }
                    return (BindingLiquidSdk)ctor.Invoke(args);
                }
                catch
                {
                    // Try next
                }
            }
            throw new InvalidOperationException("Unable to construct Breez.Sdk.Liquid.BindingLiquidSdk for test.");
        }

        // Helper: construct PrepareReceiveResponse via reflection
        private static PrepareReceiveResponse CreateTestPrepareReceiveResponse(ulong feesSat = 0)
        {
            var t = typeof(PrepareReceiveResponse);
            foreach (var ctor in t.GetConstructors())
            {
                try
                {
                    var pars = ctor.GetParameters();
                    var args = new object?[pars.Length];
                    for (int i = 0; i < pars.Length; i++)
                    {
                        var pt = pars[i].ParameterType;
                        // Try to set feesSat if parameter name matches
                        if (pars[i].Name == "feesSat")
                        {
                            args[i] = feesSat;
                        }
                        else
                        {
                            args[i] = pt.IsValueType ? Activator.CreateInstance(pt) : null;
                        }
                    }
                    return (PrepareReceiveResponse)ctor.Invoke(args);
                }
                catch
                {
                    // Try next
                }
            }
            throw new InvalidOperationException("Unable to construct Breez.Sdk.Liquid.PrepareReceiveResponse for test.");
        }

        // Helper: create a mock LnInvoice with payment hash
        private static InputType.Bolt11 CreateMockBolt11WithHash(string paymentHash)
        {
            var invoiceType = typeof(LnInvoice);
            object? invoice = null;

            foreach (var ctor in invoiceType.GetConstructors())
            {
                try
                {
                    var pars = ctor.GetParameters();
                    var args = new object?[pars.Length];
                    for (int i = 0; i < pars.Length; i++)
                    {
                        var pt = pars[i].ParameterType;
                        if (pars[i].Name == "paymentHash")
                        {
                            args[i] = paymentHash;
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
                    invoice = ctor.Invoke(args);
                    break;
                }
                catch
                {
                    // Try next
                }
            }

            if (invoice == null)
            {
                throw new InvalidOperationException("Unable to construct LnInvoice for test.");
            }

            return new InputType.Bolt11((LnInvoice)invoice);
        }

        // Helper: create a mock LnInvoice with expiry fields
        private static InputType.Bolt11 CreateMockBolt11WithExpiry(long timestamp, long ttl, long expiryTime = 0)
        {
            var invoiceType = typeof(LnInvoice);
            object? invoice = null;

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

                        if (string.Equals(paramName, "timestamp", StringComparison.OrdinalIgnoreCase))
                        {
                            if (pt == typeof(ulong))
                                args[i] = (ulong)timestamp;
                            else
                                args[i] = Convert.ChangeType(timestamp, pt);
                        }
                        else if (string.Equals(paramName, "expiry", StringComparison.OrdinalIgnoreCase))
                        {
                            // Always set expiry to the relative ttl value
                            if (pt == typeof(ulong))
                                args[i] = (ulong)ttl;
                            else
                                args[i] = Convert.ChangeType(ttl, pt);
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
                    invoice = ctor.Invoke(args);
                    break;
                }
                catch
                {
                    // Try next
                }
            }

            if (invoice == null)
            {
                throw new InvalidOperationException("Unable to construct LnInvoice for test.");
            }

            var timestampProp = invoiceType.GetProperty("timestamp");
            if (timestampProp != null && timestampProp.CanWrite)
            {
                if (timestampProp.PropertyType == typeof(ulong))
                    timestampProp.SetValue(invoice, (ulong)timestamp);
                else
                    timestampProp.SetValue(invoice, timestamp);
            }
            var expiryProp = invoiceType.GetProperty("expiry");
            if (expiryProp != null && expiryProp.CanWrite)
            {
                // Always set expiry to the relative ttl value
                if (expiryProp.PropertyType == typeof(ulong))
                    expiryProp.SetValue(invoice, (ulong)ttl);
                else
                    expiryProp.SetValue(invoice, ttl);
            }

            return new InputType.Bolt11((LnInvoice)invoice);
        }

        // Helper: create a mock Payment object
        private static global::Breez.Sdk.Liquid.Payment CreateMockPayment()
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
                        if (pt == typeof(string))
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
                    // Try next
                }
            }
            throw new InvalidOperationException("Unable to construct Payment for test.");
        }

        #endregion

        #region Builder Factory Methods

        /// <summary>
        /// Initializes a new instance of the BreezSdkServiceMockBuilder with default configurations.
        /// </summary>
        public static BreezSdkServiceMockBuilder Create()
        {
            return new BreezSdkServiceMockBuilder();
        }

        /// <summary>
        /// Creates a builder with default settings and mocks already initialized.
        /// </summary>
        public static BreezSdkServiceMockBuilder CreateDefault()
        {
            var builder = new BreezSdkServiceMockBuilder();
            builder.WithDefaultSettings();
            builder.WithDefaultMocks();
            return builder;
        }

        #endregion

        #region Settings Configuration

        /// <summary>
        /// Configures the settings used by the IOptions mock.
        /// </summary>
        public BreezSdkServiceMockBuilder WithSettings(LightningPaymentsSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (_mockOptions != null)
            {
                _mockOptions.Setup(o => o.Value).Returns(_settings);
            }
            return this;
        }

        /// <summary>
        /// Configures the settings with default test values.
        /// </summary>
        public BreezSdkServiceMockBuilder WithDefaultSettings()
        {
            _settings = new LightningPaymentsSettings
            {
                BreezApiKey = "test-api-key",
                Mnemonic = "test mnemonic",
                Network = LightningPaymentsSettings.LightningNetwork.Testnet,
                MaxInvoiceAmountSat = 1000000,
                MaxInvoiceDescriptionLength = 100,
                WorkingDirectory = null,
                WebhookUrl = null
            };
            return this;
        }

        /// <summary>
        /// Creates a copy of the current settings with the specified customizations applied.
        /// </summary>
        public BreezSdkServiceMockBuilder WithCustomSettings(
            string? breezApiKey = null,
            string? mnemonic = null,
            LightningPaymentsSettings.LightningNetwork? network = null,
            ulong? maxInvoiceAmountSat = null,
            int? maxInvoiceDescriptionLength = null,
            string? workingDirectory = null,
            string? webhookUrl = null)
        {
            WithDefaultSettings();

            var currentSettings = _settings;
            _settings = new LightningPaymentsSettings
            {
                BreezApiKey = breezApiKey ?? currentSettings!.BreezApiKey,
                Mnemonic = mnemonic ?? currentSettings!.Mnemonic,
                Network = network ?? currentSettings!.Network,
                MaxInvoiceAmountSat = maxInvoiceAmountSat ?? currentSettings!.MaxInvoiceAmountSat,
                MaxInvoiceDescriptionLength = maxInvoiceDescriptionLength ?? currentSettings!.MaxInvoiceDescriptionLength,
                WorkingDirectory = workingDirectory ?? currentSettings!.WorkingDirectory,
                WebhookUrl = webhookUrl
            };

            if (_mockOptions != null)
            {
                _mockOptions.Setup(o => o.Value).Returns(_settings);
            }

            return this;
        }

        #endregion

        #region Mock Initialization

        /// <summary>
        /// Initializes all mock objects with their default configuration.
        /// </summary>
        public BreezSdkServiceMockBuilder WithDefaultMocks()
        {
            _mockOptions = new Mock<IOptions<LightningPaymentsSettings>>();
            if (_settings != null)
            {
                _mockOptions.Setup(o => o.Value).Returns(_settings);
            }

            _mockHostEnvironment = new Mock<IHostEnvironment>();
            _mockHostEnvironment.Setup(e => e.ContentRootPath).Returns("C:\\Test\\ContentRoot");

            _mockLoggerFactory = new Mock<ILoggerFactory>();
            _mockLogger = new Mock<ILogger<BreezSdkService>>();

            _mockWrapper = new Mock<IBreezSdkWrapper>();
            _mockServiceProvider = new Mock<IServiceProvider>();

            return this;
        }

        #endregion

        #region Mock Configuration Methods

        /// <summary>
        /// Configures the IOptions mock with custom setup logic.
        /// </summary>
        public BreezSdkServiceMockBuilder WithOptions(Action<Mock<IOptions<LightningPaymentsSettings>>> configure)
        {
            _mockOptions ??= new Mock<IOptions<LightningPaymentsSettings>>();
            configure(_mockOptions);
            return this;
        }

        /// <summary>
        /// Configures the IHostEnvironment mock with custom setup logic.
        /// </summary>
        public BreezSdkServiceMockBuilder WithHostEnvironment(Action<Mock<IHostEnvironment>> configure)
        {
            _mockHostEnvironment ??= new Mock<IHostEnvironment>();
            configure(_mockHostEnvironment);
            return this;
        }

        /// <summary>
        /// Configures the ILoggerFactory mock with custom setup logic.
        /// </summary>
        public BreezSdkServiceMockBuilder WithLoggerFactory(Action<Mock<ILoggerFactory>> configure)
        {
            _mockLoggerFactory ??= new Mock<ILoggerFactory>();
            configure(_mockLoggerFactory);
            return this;
        }

        /// <summary>
        /// Configures the ILogger mock with custom setup logic.
        /// </summary>
        public BreezSdkServiceMockBuilder WithLogger(Action<Mock<ILogger<BreezSdkService>>> configure)
        {
            _mockLogger ??= new Mock<ILogger<BreezSdkService>>();
            configure(_mockLogger);
            return this;
        }

        /// <summary>
        /// Configures the IBreezSdkWrapper mock with custom setup logic.
        /// </summary>
        public BreezSdkServiceMockBuilder WithWrapper(Action<Mock<IBreezSdkWrapper>> configure)
        {
            _mockWrapper ??= new Mock<IBreezSdkWrapper>();
            configure(_mockWrapper);
            return this;
        }

        /// <summary>
        /// Configures the IServiceProvider mock with custom setup logic.
        /// </summary>
        public BreezSdkServiceMockBuilder WithServiceProvider(Action<Mock<IServiceProvider>> configure)
        {
            _mockServiceProvider ??= new Mock<IServiceProvider>();
            configure(_mockServiceProvider);
            return this;
        }

        #endregion

        #region SDK Connection Configuration

        /// <summary>
        /// Configures the wrapper to simulate a disconnected/failed SDK initialization.
        /// </summary>
        public BreezSdkServiceMockBuilder WithDisconnectedSdk()
        {
            WithWrapper(w => w
                .Setup(x => x.ConnectAsync(It.IsAny<ConnectRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((BindingLiquidSdk?)null));
            return this;
        }

        /// <summary>
        /// Configures the wrapper to simulate a null DefaultConfig response.
        /// </summary>
        public BreezSdkServiceMockBuilder WithNullDefaultConfig()
        {
            WithWrapper(w => w
                .Setup(x => x.DefaultConfig(It.IsAny<LiquidNetwork>(), It.IsAny<string>()))
                .Returns((Config)null!));
            return this;
        }

        /// <summary>
        /// Configures the wrapper to accept SetLogger calls.
        /// </summary>
        public BreezSdkServiceMockBuilder WithSetLoggerSupport()
        {
            WithWrapper(w => w.Setup(x => x.SetLogger(It.IsAny<Logger>())));
            return this;
        }

        /// <summary>
        /// Configures the wrapper to support disconnect operations.
        /// </summary>
        public BreezSdkServiceMockBuilder WithDisconnectSupport()
        {
            WithWrapper(w => w
                .Setup(x => x.DisconnectAsync(It.IsAny<BindingLiquidSdk>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask));
            return this;
        }

        /// <summary>
        /// Configures the wrapper to simulate a successful SDK connection.
        /// </summary>
        public BreezSdkServiceMockBuilder WithConnectedSdk()
        {
            WithSetLoggerSupport();
            var cfg = CreateTestConfig();
            WithWrapper(w => w
                .Setup(x => x.DefaultConfig(It.IsAny<LiquidNetwork>(), It.IsAny<string>()))
                .Returns(cfg));
            var sdkInstance = CreateTestBindingLiquidSdk();
            WithWrapper(w => w
                .Setup(x => x.ConnectAsync(It.IsAny<ConnectRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sdkInstance));
            WithWrapper(w => w
                .Setup(x => x.AddEventListener(It.IsAny<BindingLiquidSdk>(), It.IsAny<BreezSdkService.SdkEventListener>())));
            return this;
        }

        #endregion

        #region Invoice Creation Configuration

        /// <summary>
        /// Configures the wrapper to simulate a successful BOLT11 invoice creation flow.
        /// </summary>
        public BreezSdkServiceMockBuilder WithInvoiceSuccessFlow(string destination = "lnbc123")
        {
            if (_settings == null) throw new InvalidOperationException("Settings must be initialized before configuring invoice flow.");
            WithWrapper(w =>
            {
                var limits = new LightningPaymentLimitsResponse(
                    receive: new Limits(1, _settings.MaxInvoiceAmountSat, 0),
                    send: new Limits(1, _settings.MaxInvoiceAmountSat, 0));
                w.Setup(x => x.FetchLightningLimitsAsync(It.IsAny<BindingLiquidSdk>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(limits);
                var prepareResponse = CreateTestPrepareReceiveResponse();

                // Use It.Is with a Func that checks the enum value
                w.Setup(x => x.PrepareReceivePaymentAsync(
                    It.IsAny<BindingLiquidSdk>(),
                    It.Is<PrepareReceiveRequest>(r => r.paymentMethod == PaymentMethod.Bolt11Invoice),
                    It.IsAny<CancellationToken>()))
                 .ReturnsAsync(prepareResponse);

                w.Setup(x => x.ReceivePaymentAsync(It.IsAny<BindingLiquidSdk>(), It.IsAny<ReceivePaymentRequest>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new ReceivePaymentResponse(destination, null, null));
            });
            return this;
        }

        /// <summary>
        /// Configures the wrapper to simulate a successful BOLT12 offer creation flow.
        /// </summary>
        public BreezSdkServiceMockBuilder WithBolt12SuccessFlow(string destination = "lno1offer123")
        {
            if (_settings == null) throw new InvalidOperationException("Settings must be initialized before configuring BOLT12 flow.");
            WithWrapper(w =>
            {
                var limits = new LightningPaymentLimitsResponse(
                    receive: new Limits(1, _settings.MaxInvoiceAmountSat, 0),
                    send: new Limits(1, _settings.MaxInvoiceAmountSat, 0));
                w.Setup(x => x.FetchLightningLimitsAsync(It.IsAny<BindingLiquidSdk>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(limits);
                var prepareResponse = CreateTestPrepareReceiveResponse();

                // Use It.Is with a Func that checks the enum value
                w.Setup(x => x.PrepareReceivePaymentAsync(
                    It.IsAny<BindingLiquidSdk>(),
                    It.Is<PrepareReceiveRequest>(r => r.paymentMethod == PaymentMethod.Bolt12Offer),
                    It.IsAny<CancellationToken>()))
                 .ReturnsAsync(prepareResponse);

                w.Setup(x => x.ReceivePaymentAsync(It.IsAny<BindingLiquidSdk>(), It.IsAny<ReceivePaymentRequest>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new ReceivePaymentResponse(destination, null, null));
            });
            return this;
        }

        /// <summary>
        /// Configures the wrapper to simulate a failure during BOLT12 offer creation.
        /// </summary>
        public BreezSdkServiceMockBuilder WithBolt12FailureFlow()
        {
            if (_settings == null) throw new InvalidOperationException("Settings must be initialized before configuring BOLT12 flow.");
            WithWrapper(w =>
            {
                var limits = new LightningPaymentLimitsResponse(
                    receive: new Limits(1, _settings.MaxInvoiceAmountSat, 0),
                    send: new Limits(1, _settings.MaxInvoiceAmountSat, 0));
                w.Setup(x => x.FetchLightningLimitsAsync(It.IsAny<BindingLiquidSdk>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(limits);

                // Use It.Is with a Func that checks the enum value
                w.Setup(x => x.PrepareReceivePaymentAsync(
                    It.IsAny<BindingLiquidSdk>(),
                    It.Is<PrepareReceiveRequest>(r => r.paymentMethod == PaymentMethod.Bolt12Offer),
                    It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new System.Net.Http.HttpRequestException("BOLT12 prepare failed"));
            });
            return this;
        }

        /// <summary>
        /// Configures the wrapper to simulate a failure during invoice creation.
        /// </summary>
        public BreezSdkServiceMockBuilder WithInvoiceFailureFlow()
        {
            if (_settings == null) throw new InvalidOperationException("Settings must be initialized before configuring invoice flow.");
            WithWrapper(w =>
            {
                var limits = new LightningPaymentLimitsResponse(
                    receive: new Limits(1, _settings.MaxInvoiceAmountSat, 0),
                    send: new Limits(1, _settings.MaxInvoiceAmountSat, 0));
                w.Setup(x => x.FetchLightningLimitsAsync(It.IsAny<BindingLiquidSdk>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(limits);
                w.Setup(x => x.PrepareReceivePaymentAsync(It.IsAny<BindingLiquidSdk>(), It.IsAny<PrepareReceiveRequest>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new System.Net.Http.HttpRequestException("prepare failed"));
            });
            return this;
        }

        #endregion

        #region Event Listener Configuration

        /// <summary>
        /// Adds support for removing event listeners.
        /// </summary>
        public BreezSdkServiceMockBuilder WithRemoveEventListenerSupport()
        {
            WithWrapper(w => w.Setup(x => x.RemoveEventListener(It.IsAny<BindingLiquidSdk>(), It.IsAny<BreezSdkService.SdkEventListener>())));
            return this;
        }

        #endregion

        #region Parse and Extract Configuration

        /// <summary>
        /// Configures the wrapper to support parsing invoices with payment hash extraction.
        /// </summary>
        public BreezSdkServiceMockBuilder WithParseInvoiceSupport(string paymentHash)
        {
            WithWrapper(w => w.Setup(x => x.ParseAsync(
                It.IsAny<BindingLiquidSdk>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateMockBolt11WithHash(paymentHash)));
            return this;
        }

        /// <summary>
        /// Configures the wrapper to return non-BOLT11 input type when parsing.
        /// </summary>
        public BreezSdkServiceMockBuilder WithParseNonBolt11Support()
        {
            WithWrapper(w => w.Setup(x => x.ParseAsync(
                It.IsAny<BindingLiquidSdk>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new InputType.LnUrlPay(
                    new LnUrlPayRequestData(
                        callback: "",
                        maxSendable: 0,
                        minSendable: 0,
                        metadataStr: "",
                        commentAllowed: 0,
                        domain: "",
                        allowsNostr: false,
                        nostrPubkey: null,
                        lnAddress: null),
                    bip353Address: null)));
            return this;
        }

        /// <summary>
        /// Configures the wrapper to throw an exception when parsing.
        /// </summary>
        public BreezSdkServiceMockBuilder WithParseFailure()
        {
            WithWrapper(w => w.Setup(x => x.ParseAsync(
                It.IsAny<BindingLiquidSdk>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Parse failed")));
            return this;
        }

        /// <summary>
        /// Configures the wrapper to support parsing invoices with expiry information.
        /// </summary>
        public BreezSdkServiceMockBuilder WithParseInvoiceExpirySupport(long timestamp, long ttl = 0, string expiryFieldName = "timestamp")
        {
            long expiryTime = timestamp + ttl;  // Always calculate absolute expiry time
            WithWrapper(w => w.Setup(x => x.ParseAsync(
                It.IsAny<BindingLiquidSdk>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateMockBolt11WithExpiry(timestamp, ttl, expiryTime)));
            return this;
        }

        #endregion

        #region Payment Retrieval Configuration

        /// <summary>
        /// Configures the wrapper to support payment retrieval by hash.
        /// </summary>
        public BreezSdkServiceMockBuilder WithGetPaymentSupport(string paymentHash, bool exists = true)
        {
            WithWrapper(w => w.Setup(x => x.GetPaymentAsync(
                It.IsAny<BindingLiquidSdk>(),
                It.IsAny<GetPaymentRequest>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(exists ? CreateMockPayment() : null));
            return this;
        }

        #endregion

        #region Fee Quote Configuration

        /// <summary>
        /// Configures the wrapper to support fee quote requests.
        /// </summary>
        public BreezSdkServiceMockBuilder WithFeeQuoteSupport(ulong amount, ulong expectedFee = 0, bool bolt12 = false)
        {
            WithWrapper(w => w.Setup(x => x.PrepareReceivePaymentAsync(
                It.IsAny<BindingLiquidSdk>(),
                It.Is<PrepareReceiveRequest>(r => MatchesFeeQuoteRequest(r, amount, bolt12)),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateTestPrepareReceiveResponse(expectedFee)));
            return this;
        }

        // Helper method to match fee quote requests outside expression tree
        private static bool MatchesFeeQuoteRequest(PrepareReceiveRequest request, ulong amount, bool bolt12)
        {
            // Check payment method enum value
            var expectedMethod = bolt12 ? PaymentMethod.Bolt12Offer : PaymentMethod.Bolt11Invoice;
            if (request.paymentMethod != expectedMethod)
                return false;

            // Check amount - the property on PrepareReceiveRequest is called "amount", not "payerAmountSat"
            var amountProp = request.GetType().GetProperty("amount");
            if (amountProp == null)
                return false;

            var receiveAmount = amountProp.GetValue(request);
            if (receiveAmount == null)
            {
                return false;
            }

            // Try to get the amount from ReceiveAmount.Bitcoin
            if (receiveAmount is ReceiveAmount.Bitcoin bitcoinAmount)
            {
                // Use reflection to get the payerAmountSat since we don't know the exact property name
                var amountType = receiveAmount.GetType();

                // Try common property names
                foreach (var propName in new[] { "payerAmountSat", "amountSat", "amount", "AmountSat", "Amount" })
                {
                    var prop = amountType.GetProperty(propName);
                    if (prop != null)
                    {
                        var value = prop.GetValue(receiveAmount);
                        if (value != null)
                        {
                            try
                            {
                                var amountValue = Convert.ToUInt64(value);
                                return amountValue == amount;
                            }
                            catch
                            {
                                continue;
                            }
                        }
                    }
                }
            }

            return false;
        }

        #endregion

        #region Recommended Fees Configuration

        /// <summary>
        /// Configures the wrapper to support recommended fees requests.
        /// </summary>
        public BreezSdkServiceMockBuilder WithRecommendedFeesSupport(RecommendedFees fees)
        {
            WithWrapper(w => w.Setup(x => x.RecommendedFeesAsync(
                It.IsAny<BindingLiquidSdk>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(fees));
            return this;
        }

        #endregion

        #region Webhook Configuration

        /// <summary>
        /// Configures the wrapper to support webhook registration.
        /// </summary>
        public BreezSdkServiceMockBuilder WithWebhookSupport()
        {
            WithWrapper(w => w.Setup(x => x.RegisterWebhookAsync(
                It.IsAny<BindingLiquidSdk>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask));
            return this;
        }

        #endregion

        #region Accessor Methods

        /// <summary>
        /// Gets the configured IOptions mock.
        /// </summary>
        public Mock<IOptions<LightningPaymentsSettings>> GetMockOptions()
        {
            return _mockOptions ?? throw new InvalidOperationException("Options mock not initialized. Call WithDefaultMocks() first.");
        }

        /// <summary>
        /// Gets the configured IHostEnvironment mock.
        /// </summary>
        public Mock<IHostEnvironment> GetMockHostEnvironment()
        {
            return _mockHostEnvironment ?? throw new InvalidOperationException("HostEnvironment mock not initialized. Call WithDefaultMocks() first.");
        }

        /// <summary>
        /// Gets the configured ILoggerFactory mock.
        /// </summary>
        public Mock<ILoggerFactory> GetMockLoggerFactory()
        {
            return _mockLoggerFactory ?? throw new InvalidOperationException("LoggerFactory mock not initialized. Call WithDefaultMocks() first.");
        }

        /// <summary>
        /// Gets the configured ILogger mock.
        /// </summary>
        public Mock<ILogger<BreezSdkService>> GetMockLogger()
        {
            return _mockLogger ?? throw new InvalidOperationException("Logger mock not initialized. Call WithDefaultMocks() first.");
        }

        /// <summary>
        /// Gets the configured IBreezSdkWrapper mock.
        /// </summary>
        public Mock<IBreezSdkWrapper> GetMockWrapper()
        {
            return _mockWrapper ?? throw new InvalidOperationException("Wrapper mock not initialized. Call WithDefaultMocks() first.");
        }

        /// <summary>
        /// Gets the configured IServiceProvider mock.
        /// </summary>
        public Mock<IServiceProvider> GetMockServiceProvider()
        {
            return _mockServiceProvider ?? throw new InvalidOperationException("ServiceProvider mock not initialized. Call WithDefaultMocks() first.");
        }

        /// <summary>
        /// Gets the configured settings.
        /// </summary>
        public LightningPaymentsSettings GetSettings()
        {
            return _settings ?? throw new InvalidOperationException("Settings not initialized. Call WithDefaultSettings() or WithSettings() first.");
        }

        #endregion

        #region Build Method

        /// <summary>
        /// Builds and returns a new BreezSdkService instance with the configured mocks.
        /// </summary>
        public BreezSdkService Build()
        {
            return new BreezSdkService(
                GetMockOptions().Object,
                GetMockHostEnvironment().Object,
                GetMockLoggerFactory().Object,
                GetMockLogger().Object,
                GetMockWrapper().Object,
                GetMockServiceProvider().Object
            );
        }

        /// <summary>
        /// Provides a tuple of all mock objects for advanced testing scenarios.
        /// </summary>
        public (Mock<IOptions<LightningPaymentsSettings>> Options,
                 Mock<IHostEnvironment> HostEnvironment,
                 Mock<ILoggerFactory> LoggerFactory,
                 Mock<ILogger<BreezSdkService>> Logger,
                 Mock<IBreezSdkWrapper> Wrapper,
                 Mock<IServiceProvider> ServiceProvider) GetAllMocks()
        {
            return (
                GetMockOptions(),
                GetMockHostEnvironment(),
                GetMockLoggerFactory(),
                GetMockLogger(),
                GetMockWrapper(),
                GetMockServiceProvider()
            );
        }

        #endregion
    }
}
