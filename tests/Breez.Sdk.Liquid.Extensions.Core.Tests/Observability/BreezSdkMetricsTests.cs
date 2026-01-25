using System.Diagnostics.Metrics;
using Breez.Sdk.Liquid.Extensions.Core.Observability;
using FluentAssertions;

namespace Breez.Sdk.Liquid.Extensions.Core.Tests.Observability;

/// <summary>
/// Unit tests for BreezSdkMetrics.
/// These tests verify OpenTelemetry metrics collection for SDK operations including
/// counters, histograms, and observable gauges for connection state.
/// </summary>
/// <remarks>
/// IMPORTANT: This is TDD - these tests are written BEFORE implementation.
/// They will FAIL initially because BreezSdkMetrics does not exist yet.
///
/// Test Design Principles:
/// - Use MeterListener to capture and verify metrics
/// - Test thread safety with concurrent metric updates
/// - Verify appropriate tags (network, status, operation_type)
/// - Validate histogram value ranges
/// - Test observable gauge subscription/disposal
/// </remarks>
public class BreezSdkMetricsTests : IDisposable
{
    private readonly MeterListener _listener;
    private readonly Dictionary<string, List<Measurement<long>>> _counterMeasurements;
    private readonly Dictionary<string, List<Measurement<double>>> _histogramMeasurements;
    private readonly Dictionary<string, List<Measurement<int>>> _gaugeMeasurements;

    public BreezSdkMetricsTests()
    {
        _counterMeasurements = new Dictionary<string, List<Measurement<long>>>();
        _histogramMeasurements = new Dictionary<string, List<Measurement<double>>>();
        _gaugeMeasurements = new Dictionary<string, List<Measurement<int>>>();

        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Breez.Sdk.Liquid.Extensions")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        _listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            lock (_counterMeasurements)
            {
                if (!_counterMeasurements.ContainsKey(instrument.Name))
                {
                    _counterMeasurements[instrument.Name] = new List<Measurement<long>>();
                }
                _counterMeasurements[instrument.Name].Add(new Measurement<long>(measurement, tags));
            }
        });

        _listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            lock (_histogramMeasurements)
            {
                if (!_histogramMeasurements.ContainsKey(instrument.Name))
                {
                    _histogramMeasurements[instrument.Name] = new List<Measurement<double>>();
                }
                _histogramMeasurements[instrument.Name].Add(new Measurement<double>(measurement, tags));
            }
        });

        _listener.SetMeasurementEventCallback<int>((instrument, measurement, tags, state) =>
        {
            lock (_gaugeMeasurements)
            {
                if (!_gaugeMeasurements.ContainsKey(instrument.Name))
                {
                    _gaugeMeasurements[instrument.Name] = new List<Measurement<int>>();
                }
                _gaugeMeasurements[instrument.Name].Add(new Measurement<int>(measurement, tags));
            }
        });

        _listener.Start();
    }

    public void Dispose()
    {
        _listener?.Dispose();
    }

    #region Meter Creation Tests

    [Fact]
    public void Meter_ShouldHaveCorrectName()
    {
        // Arrange & Act
        var meterName = BreezSdkMetrics.MeterName;

        // Assert
        meterName.Should().Be("Breez.Sdk.Liquid.Extensions");
    }

    [Fact]
    public void Meter_ShouldBeAccessible()
    {
        // Arrange & Act
        var meter = BreezSdkMetrics.Meter;

        // Assert
        meter.Should().NotBeNull();
        meter.Name.Should().Be("Breez.Sdk.Liquid.Extensions");
    }

    #endregion

    #region Counter Tests - Invoice Created

    [Fact]
    public void InvoiceCreated_ShouldIncrementCounter_WithCorrectTags()
    {
        // Arrange
        var network = "testnet";
        var status = "success";

        // Act
        BreezSdkMetrics.RecordInvoiceCreated(network, status);
        _listener.RecordObservableInstruments();

        // Assert
        _counterMeasurements.Should().ContainKey("breez.invoice.created");
        var measurements = _counterMeasurements["breez.invoice.created"];
        measurements.Should().HaveCount(1);

        var measurement = measurements.First();
        measurement.Value.Should().Be(1);

        var tags = measurement.Tags.ToArray();
        var hasNetwork = tags.Any(t => t.Key == "network" && t.Value?.ToString() == network);
        var hasStatus = tags.Any(t => t.Key == "status" && t.Value?.ToString() == status);
        hasNetwork.Should().BeTrue();
        hasStatus.Should().BeTrue();
    }

    [Fact]
    public void InvoiceCreated_ShouldAccumulateMultipleIncrements()
    {
        // Arrange
        var network = "mainnet";
        var status = "success";

        // Act
        BreezSdkMetrics.RecordInvoiceCreated(network, status);
        BreezSdkMetrics.RecordInvoiceCreated(network, status);
        BreezSdkMetrics.RecordInvoiceCreated(network, status);
        _listener.RecordObservableInstruments();

        // Assert
        _counterMeasurements.Should().ContainKey("breez.invoice.created");
        var measurements = _counterMeasurements["breez.invoice.created"];
        measurements.Should().HaveCount(3);
        measurements.Sum(m => m.Value).Should().Be(3);
    }

    [Fact]
    public void InvoiceCreated_ShouldTrackDifferentNetworksSeparately()
    {
        // Arrange & Act
        BreezSdkMetrics.RecordInvoiceCreated("mainnet", "success");
        BreezSdkMetrics.RecordInvoiceCreated("testnet", "success");
        _listener.RecordObservableInstruments();

        // Assert
        var measurements = _counterMeasurements["breez.invoice.created"];
        measurements.Should().HaveCount(2);

        var hasMainnet = measurements.Any(m =>
            m.Tags.ToArray().Any(t => t.Key == "network" && t.Value?.ToString() == "mainnet"));
        var hasTestnet = measurements.Any(m =>
            m.Tags.ToArray().Any(t => t.Key == "network" && t.Value?.ToString() == "testnet"));
        hasMainnet.Should().BeTrue();
        hasTestnet.Should().BeTrue();
    }

    [Fact]
    public void InvoiceCreated_ShouldTrackDifferentStatusesSeparately()
    {
        // Arrange & Act
        BreezSdkMetrics.RecordInvoiceCreated("mainnet", "success");
        BreezSdkMetrics.RecordInvoiceCreated("mainnet", "failure");
        _listener.RecordObservableInstruments();

        // Assert
        var measurements = _counterMeasurements["breez.invoice.created"];
        measurements.Should().HaveCount(2);

        var hasSuccess = measurements.Any(m =>
            m.Tags.ToArray().Any(t => t.Key == "status" && t.Value?.ToString() == "success"));
        var hasFailure = measurements.Any(m =>
            m.Tags.ToArray().Any(t => t.Key == "status" && t.Value?.ToString() == "failure"));
        hasSuccess.Should().BeTrue();
        hasFailure.Should().BeTrue();
    }

    #endregion

    #region Counter Tests - Payment Received

    [Fact]
    public void PaymentReceived_ShouldIncrementCounter_WithCorrectTags()
    {
        // Arrange
        var network = "testnet";
        var status = "success";

        // Act
        BreezSdkMetrics.RecordPaymentReceived(network, status);
        _listener.RecordObservableInstruments();

        // Assert
        _counterMeasurements.Should().ContainKey("breez.payment.received");
        var measurements = _counterMeasurements["breez.payment.received"];
        measurements.Should().HaveCount(1);

        var measurement = measurements.First();
        measurement.Value.Should().Be(1);

        var tags = measurement.Tags.ToArray();
        var hasNetwork = tags.Any(t => t.Key == "network" && t.Value?.ToString() == network);
        var hasStatus = tags.Any(t => t.Key == "status" && t.Value?.ToString() == status);
        hasNetwork.Should().BeTrue();
        hasStatus.Should().BeTrue();
    }

    [Fact]
    public void PaymentReceived_ShouldAccumulateMultipleIncrements()
    {
        // Arrange
        var network = "mainnet";
        var status = "success";

        // Act
        for (var i = 0; i < 5; i++)
        {
            BreezSdkMetrics.RecordPaymentReceived(network, status);
        }
        _listener.RecordObservableInstruments();

        // Assert
        _counterMeasurements.Should().ContainKey("breez.payment.received");
        var measurements = _counterMeasurements["breez.payment.received"];
        measurements.Should().HaveCount(5);
        measurements.Sum(m => m.Value).Should().Be(5);
    }

    #endregion

    #region Counter Tests - Payment Failed

    [Fact]
    public void PaymentFailed_ShouldIncrementCounter_WithCorrectTags()
    {
        // Arrange
        var network = "testnet";
        var errorType = "insufficient_funds";

        // Act
        BreezSdkMetrics.RecordPaymentFailed(network, errorType);
        _listener.RecordObservableInstruments();

        // Assert
        _counterMeasurements.Should().ContainKey("breez.payment.failed");
        var measurements = _counterMeasurements["breez.payment.failed"];
        measurements.Should().HaveCount(1);

        var measurement = measurements.First();
        measurement.Value.Should().Be(1);

        var tags = measurement.Tags.ToArray();
        var hasNetwork = tags.Any(t => t.Key == "network" && t.Value?.ToString() == network);
        var hasErrorType = tags.Any(t => t.Key == "error_type" && t.Value?.ToString() == errorType);
        hasNetwork.Should().BeTrue();
        hasErrorType.Should().BeTrue();
    }

    [Fact]
    public void PaymentFailed_ShouldTrackDifferentErrorTypesSeparately()
    {
        // Arrange & Act
        BreezSdkMetrics.RecordPaymentFailed("mainnet", "insufficient_funds");
        BreezSdkMetrics.RecordPaymentFailed("mainnet", "timeout");
        BreezSdkMetrics.RecordPaymentFailed("mainnet", "network_error");
        _listener.RecordObservableInstruments();

        // Assert
        var measurements = _counterMeasurements["breez.payment.failed"];
        measurements.Should().HaveCount(3);

        var errorTypes = measurements.Select(m =>
            m.Tags.ToArray().First(t => t.Key == "error_type").Value?.ToString()).ToList();
        errorTypes.Should().Contain("insufficient_funds");
        errorTypes.Should().Contain("timeout");
        errorTypes.Should().Contain("network_error");
    }

    #endregion

    #region Histogram Tests - Operation Duration

    [Fact]
    public void OperationDuration_ShouldRecordDuration_WithCorrectTags()
    {
        // Arrange
        var operationType = "prepare_receive";
        var network = "testnet";
        var durationMs = 125.5;

        // Act
        BreezSdkMetrics.RecordOperationDuration(operationType, network, durationMs);
        _listener.RecordObservableInstruments();

        // Assert
        _histogramMeasurements.Should().ContainKey("breez.operation.duration");
        var measurements = _histogramMeasurements["breez.operation.duration"];
        measurements.Should().HaveCount(1);

        var measurement = measurements.First();
        measurement.Value.Should().Be(durationMs);

        var tags = measurement.Tags.ToArray();
        var hasOperationType = tags.Any(t => t.Key == "operation_type" && t.Value?.ToString() == operationType);
        var hasNetwork = tags.Any(t => t.Key == "network" && t.Value?.ToString() == network);
        hasOperationType.Should().BeTrue();
        hasNetwork.Should().BeTrue();
    }

    [Fact]
    public void OperationDuration_ShouldRecordMultipleMeasurements()
    {
        // Arrange
        var operationType = "send_payment";
        var network = "mainnet";

        // Act
        BreezSdkMetrics.RecordOperationDuration(operationType, network, 50.0);
        BreezSdkMetrics.RecordOperationDuration(operationType, network, 150.5);
        BreezSdkMetrics.RecordOperationDuration(operationType, network, 225.75);
        _listener.RecordObservableInstruments();

        // Assert
        var measurements = _histogramMeasurements["breez.operation.duration"];
        measurements.Should().HaveCount(3);

        var durations = measurements.Select(m => m.Value).ToList();
        durations.Should().Contain(50.0);
        durations.Should().Contain(150.5);
        durations.Should().Contain(225.75);
    }

    [Fact]
    public void OperationDuration_ShouldTrackDifferentOperationTypesSeparately()
    {
        // Arrange & Act
        BreezSdkMetrics.RecordOperationDuration("connect", "mainnet", 100.0);
        BreezSdkMetrics.RecordOperationDuration("prepare_receive", "mainnet", 50.0);
        BreezSdkMetrics.RecordOperationDuration("send_payment", "mainnet", 200.0);
        _listener.RecordObservableInstruments();

        // Assert
        var measurements = _histogramMeasurements["breez.operation.duration"];
        measurements.Should().HaveCount(3);

        var operationTypes = measurements.Select(m =>
            m.Tags.ToArray().First(t => t.Key == "operation_type").Value?.ToString()).ToList();
        operationTypes.Should().Contain("connect");
        operationTypes.Should().Contain("prepare_receive");
        operationTypes.Should().Contain("send_payment");
    }

    [Fact]
    public void OperationDuration_ShouldAcceptZeroDuration()
    {
        // Arrange
        var operationType = "get_info";
        var network = "mainnet";

        // Act
        BreezSdkMetrics.RecordOperationDuration(operationType, network, 0.0);
        _listener.RecordObservableInstruments();

        // Assert
        var measurements = _histogramMeasurements["breez.operation.duration"];
        measurements.Should().HaveCount(1);
        measurements.First().Value.Should().Be(0.0);
    }

    [Fact]
    public void OperationDuration_ShouldAcceptLargeDurations()
    {
        // Arrange
        var operationType = "sync";
        var network = "mainnet";
        var largeDuration = 30000.0; // 30 seconds

        // Act
        BreezSdkMetrics.RecordOperationDuration(operationType, network, largeDuration);
        _listener.RecordObservableInstruments();

        // Assert
        var measurements = _histogramMeasurements["breez.operation.duration"];
        measurements.Should().HaveCount(1);
        measurements.First().Value.Should().Be(largeDuration);
    }

    #endregion

    #region Observable Gauge Tests - Connection State

    [Fact]
    public void ConnectionState_ShouldReport1_WhenConnected()
    {
        // Arrange - Use unique network name to avoid interference from other tests
        var uniqueNetwork = $"connected-test-{Guid.NewGuid():N}";
        BreezSdkMetrics.SetConnectionState(true, uniqueNetwork);

        // Act
        _listener.RecordObservableInstruments();

        // Assert
        _gaugeMeasurements.Should().ContainKey("breez.sdk.connected");
        var measurements = _gaugeMeasurements["breez.sdk.connected"];

        // Find the measurement for our unique network
        var measurement = measurements
            .FirstOrDefault(m => m.Tags.ToArray().Any(t => t.Key == "network" && t.Value?.ToString() == uniqueNetwork));

        measurement.Should().NotBeNull();
        measurement.Value.Should().Be(1);
    }

    [Fact]
    public void ConnectionState_ShouldReport0_WhenDisconnected()
    {
        // Arrange - Use unique network name to avoid interference from other tests
        var uniqueNetwork = $"disconnected-test-{Guid.NewGuid():N}";
        BreezSdkMetrics.SetConnectionState(false, uniqueNetwork);

        // Act
        _listener.RecordObservableInstruments();

        // Assert
        _gaugeMeasurements.Should().ContainKey("breez.sdk.connected");
        var measurements = _gaugeMeasurements["breez.sdk.connected"];

        // Find the measurement for our unique network
        var measurement = measurements
            .FirstOrDefault(m => m.Tags.ToArray().Any(t => t.Key == "network" && t.Value?.ToString() == uniqueNetwork));

        measurement.Should().NotBeNull();
        measurement.Value.Should().Be(0);
    }

    [Fact]
    public void ConnectionState_ShouldUpdateValue_WhenStateChanges()
    {
        // Arrange - Use unique network name to avoid interference from other tests
        var uniqueNetwork = $"update-test-{Guid.NewGuid():N}";
        BreezSdkMetrics.SetConnectionState(true, uniqueNetwork);
        _listener.RecordObservableInstruments();
        _gaugeMeasurements.Clear();

        // Act - Change to disconnected
        BreezSdkMetrics.SetConnectionState(false, uniqueNetwork);
        _listener.RecordObservableInstruments();

        // Assert
        var measurements = _gaugeMeasurements["breez.sdk.connected"];

        // Find the measurement for our unique network
        var measurement = measurements
            .FirstOrDefault(m => m.Tags.ToArray().Any(t => t.Key == "network" && t.Value?.ToString() == uniqueNetwork));

        measurement.Should().NotBeNull();
        measurement.Value.Should().Be(0);
    }

    [Fact]
    public void ConnectionState_ShouldTrackMultipleNetworks()
    {
        // Arrange - Use unique network names to avoid interference from other tests
        var uniqueNetworkA = $"multi-a-{Guid.NewGuid():N}";
        var uniqueNetworkB = $"multi-b-{Guid.NewGuid():N}";
        BreezSdkMetrics.SetConnectionState(true, uniqueNetworkA);
        BreezSdkMetrics.SetConnectionState(false, uniqueNetworkB);

        // Act
        _listener.RecordObservableInstruments();

        // Assert
        var measurements = _gaugeMeasurements["breez.sdk.connected"];

        var networkAMeasurement = measurements
            .First(m => m.Tags.ToArray().Any(t => t.Key == "network" && t.Value?.ToString() == uniqueNetworkA));
        var networkBMeasurement = measurements
            .First(m => m.Tags.ToArray().Any(t => t.Key == "network" && t.Value?.ToString() == uniqueNetworkB));

        networkAMeasurement.Value.Should().Be(1);
        networkBMeasurement.Value.Should().Be(0);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task Metrics_ShouldBeThreadSafe_WhenRecordingConcurrently()
    {
        // Arrange
        var tasks = new List<Task>();
        var iterationsPerThread = 100;
        var threadCount = 10;

        // Act - Record metrics from multiple threads simultaneously
        for (var i = 0; i < threadCount; i++)
        {
            var threadIndex = i;
            tasks.Add(Task.Run(() =>
            {
                for (var j = 0; j < iterationsPerThread; j++)
                {
                    BreezSdkMetrics.RecordInvoiceCreated($"network-{threadIndex}", "success");
                    BreezSdkMetrics.RecordPaymentReceived($"network-{threadIndex}", "success");
                    BreezSdkMetrics.RecordPaymentFailed($"network-{threadIndex}", "error");
                    BreezSdkMetrics.RecordOperationDuration("test_op", $"network-{threadIndex}", 50.0);
                }
            }));
        }

        await Task.WhenAll(tasks);
        _listener.RecordObservableInstruments();

        // Assert - All measurements should be recorded without data corruption
        var invoiceCount = _counterMeasurements.ContainsKey("breez.invoice.created")
            ? _counterMeasurements["breez.invoice.created"].Count
            : 0;
        var paymentReceivedCount = _counterMeasurements.ContainsKey("breez.payment.received")
            ? _counterMeasurements["breez.payment.received"].Count
            : 0;
        var paymentFailedCount = _counterMeasurements.ContainsKey("breez.payment.failed")
            ? _counterMeasurements["breez.payment.failed"].Count
            : 0;
        var operationDurationCount = _histogramMeasurements.ContainsKey("breez.operation.duration")
            ? _histogramMeasurements["breez.operation.duration"].Count
            : 0;

        var expectedCount = threadCount * iterationsPerThread;
        invoiceCount.Should().Be(expectedCount);
        paymentReceivedCount.Should().Be(expectedCount);
        paymentFailedCount.Should().Be(expectedCount);
        operationDurationCount.Should().Be(expectedCount);
    }

    [Fact]
    public async Task ConnectionState_ShouldBeThreadSafe_WhenUpdatingConcurrently()
    {
        // Arrange
        var tasks = new List<Task>();
        var iterationsPerThread = 50;
        var threadCount = 5;

        // Act - Update connection state from multiple threads
        for (var i = 0; i < threadCount; i++)
        {
            var threadIndex = i;
            tasks.Add(Task.Run(() =>
            {
                for (var j = 0; j < iterationsPerThread; j++)
                {
                    var isConnected = j % 2 == 0;
                    BreezSdkMetrics.SetConnectionState(isConnected, $"network-{threadIndex}");
                }
            }));
        }

        await Task.WhenAll(tasks);
        _listener.RecordObservableInstruments();

        // Assert - Should not throw exceptions and should record final states
        if (_gaugeMeasurements.ContainsKey("breez.sdk.connected"))
        {
            var measurements = _gaugeMeasurements["breez.sdk.connected"];
            measurements.Should().HaveCountGreaterThan(0);

            // All values should be either 0 or 1
            foreach (var m in measurements)
            {
                var isValidValue = m.Value == 0 || m.Value == 1;
                isValidValue.Should().BeTrue();
            }
        }
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void RecordInvoiceCreated_ShouldHandleNullNetwork_Gracefully()
    {
        // Arrange
        string? network = null;

        // Act
        var act = () => BreezSdkMetrics.RecordInvoiceCreated(network!, "success");

        // Assert - Should either accept null or throw ArgumentNullException
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordOperationDuration_ShouldHandleNegativeDuration_Appropriately()
    {
        // Arrange
        var operationType = "test";
        var network = "mainnet";
        var negativeDuration = -50.0;

        // Act
        var act = () => BreezSdkMetrics.RecordOperationDuration(operationType, network, negativeDuration);

        // Assert - Should either clamp to 0 or throw ArgumentOutOfRangeException
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordOperationDuration_ShouldHandleEmptyOperationType()
    {
        // Arrange
        var operationType = string.Empty;
        var network = "mainnet";

        // Act
        var act = () => BreezSdkMetrics.RecordOperationDuration(operationType, network, 100.0);

        // Assert - Should handle empty strings gracefully
        act.Should().NotThrow();
    }

    #endregion
}
