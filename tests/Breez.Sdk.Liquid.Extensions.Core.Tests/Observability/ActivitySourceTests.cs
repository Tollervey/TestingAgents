using System.Diagnostics;
using System.Collections.Concurrent;
using Breez.Sdk.Liquid.Extensions.Core.Observability;
using FluentAssertions;

namespace Breez.Sdk.Liquid.Extensions.Core.Tests.Observability;

/// <summary>
/// Unit tests for ActivitySources class that provides OpenTelemetry activity sources and constants.
/// These tests verify proper configuration of distributed tracing infrastructure.
/// </summary>
/// <remarks>
/// TDD RED PHASE: These tests reference ActivitySources which doesn't exist yet.
/// They should fail compilation initially, then pass once the implementation is complete.
///
/// Testing Strategy:
/// - Verify ActivitySource is created with correct name for telemetry collection
/// - Validate operation name constants match OpenTelemetry semantic conventions
/// - Test activity creation and tagging patterns
/// - Verify exception recording and status handling
/// </remarks>
public class ActivitySourceTests : IDisposable
{
    private readonly ActivityListener _listener;
    private readonly List<Activity> _recordedActivities;

    public ActivitySourceTests()
    {
        _recordedActivities = new List<Activity>();

        // Set up listener to capture activities for testing
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Breez.Sdk.Liquid.Extensions",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => _recordedActivities.Add(activity)
        };

        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener.Dispose();
        // ConcurrentBag doesn't have Clear - it will be GC'd with the test instance
    }

    #region ActivitySource Configuration Tests

    [Fact]
    public void ActivitySource_HasCorrectName()
    {
        // Arrange & Act
        var source = ActivitySources.Source;

        // Assert
        source.Should().NotBeNull();
        source.Name.Should().Be("Breez.Sdk.Liquid.Extensions");
    }

    [Fact]
    public void ActivitySource_HasVersion()
    {
        // Arrange & Act
        var source = ActivitySources.Source;

        // Assert
        source.Version.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Operation Name Constants Tests

    [Fact]
    public void OperationNames_Connect_IsCorrect()
    {
        // Act
        var operationName = ActivitySources.Operations.Connect;

        // Assert
        operationName.Should().Be("breez.sdk.connect");
    }

    [Fact]
    public void OperationNames_Disconnect_IsCorrect()
    {
        // Act
        var operationName = ActivitySources.Operations.Disconnect;

        // Assert
        operationName.Should().Be("breez.sdk.disconnect");
    }

    [Fact]
    public void OperationNames_CreateInvoice_IsCorrect()
    {
        // Act
        var operationName = ActivitySources.Operations.CreateInvoice;

        // Assert
        operationName.Should().Be("breez.sdk.create_invoice");
    }

    [Fact]
    public void OperationNames_GetPayment_IsCorrect()
    {
        // Act
        var operationName = ActivitySources.Operations.GetPayment;

        // Assert
        operationName.Should().Be("breez.sdk.get_payment");
    }

    [Fact]
    public void OperationNames_GetBalance_IsCorrect()
    {
        // Act
        var operationName = ActivitySources.Operations.GetBalance;

        // Assert
        operationName.Should().Be("breez.sdk.get_balance");
    }

    [Fact]
    public void OperationNames_SendPayment_IsCorrect()
    {
        // Act
        var operationName = ActivitySources.Operations.SendPayment;

        // Assert
        operationName.Should().Be("breez.sdk.send_payment");
    }

    [Fact]
    public void OperationNames_ListPayments_IsCorrect()
    {
        // Act
        var operationName = ActivitySources.Operations.ListPayments;

        // Assert
        operationName.Should().Be("breez.sdk.list_payments");
    }

    [Fact]
    public void OperationNames_PrepareReceive_IsCorrect()
    {
        // Act
        var operationName = ActivitySources.Operations.PrepareReceive;

        // Assert
        operationName.Should().Be("breez.sdk.prepare_receive");
    }

    [Fact]
    public void OperationNames_PrepareSend_IsCorrect()
    {
        // Act
        var operationName = ActivitySources.Operations.PrepareSend;

        // Assert
        operationName.Should().Be("breez.sdk.prepare_send");
    }

    #endregion

    #region Activity Creation Tests

    [Fact]
    public void StartActivity_WithOperationName_CreatesActivityWithCorrectName()
    {
        // Arrange & Act
        using var activity = ActivitySources.StartActivity(ActivitySources.Operations.Connect);

        // Assert
        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be("breez.sdk.connect");
        _recordedActivities.Should().ContainSingle()
            .Which.OperationName.Should().Be("breez.sdk.connect");
    }

    [Fact]
    public void StartActivity_WithKind_CreatesActivityWithCorrectKind()
    {
        // Arrange & Act
        using var activity = ActivitySources.StartActivity(
            ActivitySources.Operations.CreateInvoice,
            ActivityKind.Server);

        // Assert
        activity.Should().NotBeNull();
        activity!.Kind.Should().Be(ActivityKind.Server);
    }

    [Fact]
    public void StartActivity_WithoutActiveListener_ReturnsNull()
    {
        // Arrange
        _listener.Dispose(); // Remove listener

        // Act
        using var activity = ActivitySources.StartActivity(ActivitySources.Operations.Connect);

        // Assert
        activity.Should().BeNull();
    }

    #endregion

    #region Activity Tagging Tests

    [Fact]
    public void StartActivity_WithNetworkTag_SetsTagCorrectly()
    {
        // Arrange & Act
        using var activity = ActivitySources.StartActivity(ActivitySources.Operations.Connect);
        activity?.SetTag("breez.network", "mainnet");

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem("breez.network").Should().Be("mainnet");
    }

    [Fact]
    public void StartActivity_WithAmountTag_SetsTagCorrectly()
    {
        // Arrange & Act
        using var activity = ActivitySources.StartActivity(ActivitySources.Operations.CreateInvoice);
        activity?.SetTag("breez.amount_sat", 50000);

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem("breez.amount_sat").Should().Be(50000);
    }

    [Fact]
    public void StartActivity_WithPaymentHashTag_SetsTagCorrectly()
    {
        // Arrange
        var paymentHash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

        // Act
        using var activity = ActivitySources.StartActivity(ActivitySources.Operations.GetPayment);
        activity?.SetTag("breez.payment_hash", paymentHash);

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem("breez.payment_hash").Should().Be(paymentHash);
    }

    [Fact]
    public void StartActivity_WithMultipleTags_SetsAllTagsCorrectly()
    {
        // Arrange & Act
        using var activity = ActivitySources.StartActivity(ActivitySources.Operations.SendPayment);
        activity?.SetTag("breez.network", "testnet");
        activity?.SetTag("breez.amount_sat", 25000);
        activity?.SetTag("breez.destination", "lnbc...");

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem("breez.network").Should().Be("testnet");
        activity!.GetTagItem("breez.amount_sat").Should().Be(25000);
        activity!.GetTagItem("breez.destination").Should().Be("lnbc...");
    }

    #endregion

    #region Activity Status Tests

    [Fact]
    public void SetActivityStatus_Success_SetsStatusOk()
    {
        // Arrange
        using var activity = ActivitySources.StartActivity(ActivitySources.Operations.CreateInvoice);

        // Act
        activity?.SetStatus(ActivityStatusCode.Ok);

        // Assert
        activity.Should().NotBeNull();
        activity!.Status.Should().Be(ActivityStatusCode.Ok);
    }

    [Fact]
    public void SetActivityStatus_Error_SetsStatusError()
    {
        // Arrange
        using var activity = ActivitySources.StartActivity(ActivitySources.Operations.SendPayment);

        // Act
        activity?.SetStatus(ActivityStatusCode.Error, "Payment failed");

        // Assert
        activity.Should().NotBeNull();
        activity!.Status.Should().Be(ActivityStatusCode.Error);
        activity!.StatusDescription.Should().Be("Payment failed");
    }

    #endregion

    #region Exception Recording Tests

    [Fact]
    public void Activity_WhenExceptionRecorded_AddsExceptionEvent()
    {
        // Arrange
        using var activity = ActivitySources.StartActivity(ActivitySources.Operations.Connect);
        var exception = new InvalidOperationException("Connection failed");

        // Act - Use standard Activity event recording (OpenTelemetry semantic conventions)
        var tags = new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName },
            { "exception.message", exception.Message }
        };
        activity?.AddEvent(new ActivityEvent("exception", tags: tags));

        // Assert
        activity.Should().NotBeNull();
        activity!.Events.Should().ContainSingle()
            .Which.Name.Should().Be("exception");
    }

    [Fact]
    public void Activity_WhenExceptionRecorded_CanSetStatusToError()
    {
        // Arrange
        using var activity = ActivitySources.StartActivity(ActivitySources.Operations.Connect);
        var exception = new InvalidOperationException("Connection failed");

        // Act
        activity?.SetStatus(ActivityStatusCode.Error, exception.Message);

        // Assert
        activity.Should().NotBeNull();
        activity!.Status.Should().Be(ActivityStatusCode.Error);
        activity!.StatusDescription.Should().Be("Connection failed");
    }

    [Fact]
    public void Activity_WhenExceptionRecorded_IncludesExceptionDetailsAsTags()
    {
        // Arrange
        using var activity = ActivitySources.StartActivity(ActivitySources.Operations.SendPayment);
        var exception = new ArgumentException("Invalid payment amount", "amountSat");

        // Act - Record exception with semantic convention tags
        var tags = new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName },
            { "exception.message", exception.Message }
        };
        activity?.AddEvent(new ActivityEvent("exception", tags: tags));

        // Assert
        activity.Should().NotBeNull();
        var exceptionEvent = activity!.Events.Should().ContainSingle().Subject;
        exceptionEvent.Tags.Should().Contain(tag => tag.Key == "exception.type");
        exceptionEvent.Tags.Should().Contain(tag => tag.Key == "exception.message");
    }

    #endregion

    #region Helper Method Tests

    [Fact]
    public void StartConnectActivity_CreatesActivityWithCorrectName()
    {
        // Act
        using var activity = ActivitySources.StartConnectActivity();

        // Assert
        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be("breez.sdk.connect");
    }

    [Fact]
    public void StartDisconnectActivity_CreatesActivityWithCorrectName()
    {
        // Act
        using var activity = ActivitySources.StartDisconnectActivity();

        // Assert
        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be("breez.sdk.disconnect");
    }

    [Fact]
    public void StartCreateInvoiceActivity_CreatesActivityWithCorrectName()
    {
        // Act
        using var activity = ActivitySources.StartCreateInvoiceActivity();

        // Assert
        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be("breez.sdk.create_invoice");
    }

    [Fact]
    public void StartSendPaymentActivity_CreatesActivityWithCorrectName()
    {
        // Act
        using var activity = ActivitySources.StartSendPaymentActivity();

        // Assert
        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be("breez.sdk.send_payment");
    }

    [Fact]
    public void StartGetBalanceActivity_CreatesActivityWithCorrectName()
    {
        // Act
        using var activity = ActivitySources.StartGetBalanceActivity();

        // Assert
        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be("breez.sdk.get_balance");
    }

    #endregion

    #region Activity Lifecycle Tests

    [Fact]
    public void Activity_WhenDisposed_StopsActivity()
    {
        // Arrange
        Activity? capturedActivity;

        // Act
        using (var activity = ActivitySources.StartActivity(ActivitySources.Operations.Connect))
        {
            capturedActivity = activity;
            capturedActivity.Should().NotBeNull();
        }

        // Assert
        capturedActivity!.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void MultipleActivities_CanBeCreatedConcurrently()
    {
        // Arrange & Act
        using var activity1 = ActivitySources.StartActivity(ActivitySources.Operations.Connect);
        using var activity2 = ActivitySources.StartActivity(ActivitySources.Operations.GetBalance);
        using var activity3 = ActivitySources.StartActivity(ActivitySources.Operations.CreateInvoice);

        // Assert
        activity1.Should().NotBeNull();
        activity2.Should().NotBeNull();
        activity3.Should().NotBeNull();
        _recordedActivities.Should().HaveCount(3);
    }

    [Fact]
    public void NestedActivities_CreateParentChildRelationship()
    {
        // Arrange & Act
        using var parentActivity = ActivitySources.StartActivity(ActivitySources.Operations.SendPayment);
        using var childActivity = ActivitySources.StartActivity(ActivitySources.Operations.PrepareSend);

        // Assert
        parentActivity.Should().NotBeNull();
        childActivity.Should().NotBeNull();
        childActivity!.Parent.Should().Be(parentActivity);
    }

    #endregion
}
