using System.Diagnostics.Metrics;
using FluentAssertions;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Observability;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Services.Observability;

public class LightningPaymentsMetricsTests : IDisposable
{
    private readonly MeterListener _listener;
    private readonly List<(string Name, object? Value, KeyValuePair<string, object?>[] Tags)> _measurements = new();

    public LightningPaymentsMetricsTests()
    {
        _listener = new MeterListener();
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == LightningPaymentsMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, state) =>
        {
            _measurements.Add((instrument.Name, value, tags.ToArray()));
        });
        _listener.SetMeasurementEventCallback<double>((instrument, value, tags, state) =>
        {
            _measurements.Add((instrument.Name, value, tags.ToArray()));
        });
        _listener.Start();
    }

    [Fact]
    public void RecordNotificationSent_Records_Counter()
    {
        var type = $"email-{Guid.NewGuid():N}";
        LightningPaymentsMetrics.RecordNotificationSent(type, "success");

        var match = _measurements.Where(m =>
            m.Name == "lightning.notification.sent" &&
            m.Tags.Any(t => t.Key == "type" && t.Value?.ToString() == type)).ToList();
        match.Should().HaveCount(1);
    }

    [Fact]
    public void RecordRefundInitiated_Records_Counter()
    {
        LightningPaymentsMetrics.RecordRefundInitiated();
        _measurements.Should().Contain(m => m.Name == "lightning.refund.initiated");
    }

    [Fact]
    public void RecordRefundCompleted_Records_WithStatus()
    {
        var status = $"success-{Guid.NewGuid():N}";
        LightningPaymentsMetrics.RecordRefundCompleted(status);

        var match = _measurements.Where(m =>
            m.Name == "lightning.refund.completed" &&
            m.Tags.Any(t => t.Key == "status" && t.Value?.ToString() == status)).ToList();
        match.Should().HaveCount(1);
    }

    [Fact]
    public void RecordExchangeRateFetched_Records_WithCurrencyAndSource()
    {
        var currency = $"usd-{Guid.NewGuid():N}";
        LightningPaymentsMetrics.RecordExchangeRateFetched(currency, "coingecko");

        var match = _measurements.Where(m =>
            m.Name == "lightning.exchange_rate.fetched" &&
            m.Tags.Any(t => t.Key == "currency" && t.Value?.ToString() == currency)).ToList();
        match.Should().HaveCount(1);
    }

    [Fact]
    public void RecordDashboardAccessed_Records_Counter()
    {
        LightningPaymentsMetrics.RecordDashboardAccessed();
        _measurements.Should().Contain(m => m.Name == "lightning.dashboard.accessed");
    }

    [Fact]
    public void RecordApiRequestDuration_Records_Histogram()
    {
        var controller = $"test-{Guid.NewGuid():N}";
        LightningPaymentsMetrics.RecordApiRequestDuration(controller, "get", 123.45);

        var match = _measurements.Where(m =>
            m.Name == "lightning.api.request.duration" &&
            m.Tags.Any(t => t.Key == "controller" && t.Value?.ToString() == controller)).ToList();
        match.Should().HaveCount(1);
    }

    [Fact]
    public void MeterName_Is_Correct()
    {
        LightningPaymentsMetrics.MeterName.Should().Be("Umbraco.Community.LightningPayments");
    }

    public void Dispose()
    {
        _listener.Dispose();
    }
}
