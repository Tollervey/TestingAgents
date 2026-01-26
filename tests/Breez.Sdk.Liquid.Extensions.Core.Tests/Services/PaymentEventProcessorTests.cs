using System.Threading.Channels;
using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Breez.Sdk.Liquid.Extensions.Core.Domain.Events;
using Breez.Sdk.Liquid.Extensions.Core.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Breez.Sdk.Liquid.Extensions.Core.Tests.Services;

/// <summary>
/// Unit tests for PaymentEventProcessor background service.
/// These tests verify event dispatching, error handling, and graceful shutdown behavior.
/// </summary>
/// <remarks>
/// TDD RED PHASE: These tests reference PaymentEventProcessor which doesn't exist yet.
/// They should fail compilation initially, then pass once the implementation is complete.
///
/// PaymentEventProcessor is responsible for:
/// - Consuming events from a PaymentEventChannel
/// - Dispatching events to all registered IPaymentEventHandler instances
/// - Handling errors from handlers without stopping event processing
/// - Graceful shutdown when cancellation is requested
/// </remarks>
public class PaymentEventProcessorTests : IDisposable
{
    private readonly Channel<PaymentEvent> _channel;
    private readonly PaymentEventChannel _eventChannel;
    private readonly Mock<ILogger<PaymentEventProcessor>> _loggerMock;
    private readonly List<Mock<IPaymentEventHandler>> _handlerMocks;
    private PaymentEventProcessor _sut;

    public PaymentEventProcessorTests()
    {
        _channel = Channel.CreateUnbounded<PaymentEvent>();
        _eventChannel = new PaymentEventChannel(_channel);
        _loggerMock = new Mock<ILogger<PaymentEventProcessor>>();
        _handlerMocks = new List<Mock<IPaymentEventHandler>>();

        // System under test - will be created per test with specific handlers
        _sut = null!;
    }

    #region StartAsync and StopAsync Tests

    [Fact]
    public async Task StartAsync_StartsBackgroundProcessing_WithoutBlocking()
    {
        // Arrange
        _sut = new PaymentEventProcessor(
            _eventChannel,
            Array.Empty<IPaymentEventHandler>(),
            _loggerMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        var startTask = _sut.StartAsync(cts.Token);

        // Assert - StartAsync should complete immediately (non-blocking)
        await startTask.WaitAsync(TimeSpan.FromMilliseconds(100));
        startTask.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task StopAsync_StopsProcessingGracefully_WhenCancellationRequested()
    {
        // Arrange
        _sut = new PaymentEventProcessor(
            _eventChannel,
            Array.Empty<IPaymentEventHandler>(),
            _loggerMock.Object);

        using var cts = new CancellationTokenSource();
        await _sut.StartAsync(cts.Token);

        // Act
        await _sut.StopAsync(CancellationToken.None);

        // Assert - Should complete without throwing
        // Background processing should have stopped
        _channel.Writer.Complete();

        // Give background task time to complete
        await Task.Delay(100);
    }

    [Fact]
    public async Task StopAsync_CompletesPendingEvents_BeforeShutdown()
    {
        // Arrange
        var handlerMock = new Mock<IPaymentEventHandler>();
        var eventHandled = new TaskCompletionSource<bool>();

        handlerMock
            .Setup(h => h.HandleAsync(It.IsAny<PaymentEvent>(), It.IsAny<CancellationToken>()))
            .Callback(() => eventHandled.TrySetResult(true))
            .Returns(Task.CompletedTask);

        _sut = new PaymentEventProcessor(
            _eventChannel,
            new[] { handlerMock.Object },
            _loggerMock.Object);

        using var cts = new CancellationTokenSource();
        await _sut.StartAsync(cts.Token);

        var paymentEvent = new PaymentReceived
        {
            PaymentHash = "test-hash-123",
            AmountSat = 5000
        };

        // Act
        await _eventChannel.PublishAsync(paymentEvent);

        // Wait for event to be handled
        await eventHandled.Task.WaitAsync(TimeSpan.FromSeconds(1));

        await _sut.StopAsync(CancellationToken.None);

        // Assert
        handlerMock.Verify(
            h => h.HandleAsync(It.Is<PaymentReceived>(e => e.PaymentHash == "test-hash-123"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Event Handling - Single Handler Tests

    [Fact]
    public async Task ProcessEvents_WithSingleHandler_DispatchesEventSuccessfully()
    {
        // Arrange
        var handlerMock = new Mock<IPaymentEventHandler>();
        var eventHandled = new TaskCompletionSource<bool>();

        handlerMock
            .Setup(h => h.HandleAsync(It.IsAny<PaymentEvent>(), It.IsAny<CancellationToken>()))
            .Callback(() => eventHandled.TrySetResult(true))
            .Returns(Task.CompletedTask);

        _sut = new PaymentEventProcessor(
            _eventChannel,
            new[] { handlerMock.Object },
            _loggerMock.Object);

        using var cts = new CancellationTokenSource();
        await _sut.StartAsync(cts.Token);

        var paymentEvent = new PaymentReceived
        {
            PaymentHash = "abc123",
            AmountSat = 10000,
            Timestamp = DateTimeOffset.UtcNow,
            CorrelationId = "corr-001"
        };

        // Act
        await _eventChannel.PublishAsync(paymentEvent);

        // Assert
        await eventHandled.Task.WaitAsync(TimeSpan.FromSeconds(1));

        handlerMock.Verify(
            h => h.HandleAsync(
                It.Is<PaymentReceived>(e =>
                    e.PaymentHash == "abc123" &&
                    e.AmountSat == 10000 &&
                    e.CorrelationId == "corr-001"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessEvents_WithMultipleEventsSequentially_HandlesAllEvents()
    {
        // Arrange
        var handlerMock = new Mock<IPaymentEventHandler>();
        var eventsHandled = new List<string>();
        var allEventsHandled = new TaskCompletionSource<bool>();

        handlerMock
            .Setup(h => h.HandleAsync(It.IsAny<PaymentEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PaymentEvent, CancellationToken>((e, ct) =>
            {
                eventsHandled.Add(e.PaymentHash);
                if (eventsHandled.Count == 3)
                {
                    allEventsHandled.TrySetResult(true);
                }
            })
            .Returns(Task.CompletedTask);

        _sut = new PaymentEventProcessor(
            _eventChannel,
            new[] { handlerMock.Object },
            _loggerMock.Object);

        using var cts = new CancellationTokenSource();
        await _sut.StartAsync(cts.Token);

        var event1 = new PaymentReceived { PaymentHash = "hash-1", AmountSat = 1000 };
        var event2 = new PaymentReceived { PaymentHash = "hash-2", AmountSat = 2000 };
        var event3 = new PaymentReceived { PaymentHash = "hash-3", AmountSat = 3000 };

        // Act
        await _eventChannel.PublishAsync(event1);
        await _eventChannel.PublishAsync(event2);
        await _eventChannel.PublishAsync(event3);

        // Assert
        await allEventsHandled.Task.WaitAsync(TimeSpan.FromSeconds(2));

        eventsHandled.Should().HaveCount(3);
        eventsHandled.Should().ContainInOrder("hash-1", "hash-2", "hash-3");
    }

    #endregion

    #region Event Handling - Multiple Handlers Tests

    [Fact]
    public async Task ProcessEvents_WithMultipleHandlers_DispatchesToAllHandlers()
    {
        // Arrange
        var handler1Mock = new Mock<IPaymentEventHandler>();
        var handler2Mock = new Mock<IPaymentEventHandler>();
        var handler3Mock = new Mock<IPaymentEventHandler>();

        var allHandlersComplete = new TaskCompletionSource<bool>();
        var handlerCount = 0;
        var lockObj = new object();

        void IncrementAndCheck()
        {
            lock (lockObj)
            {
                handlerCount++;
                if (handlerCount == 3)
                {
                    allHandlersComplete.TrySetResult(true);
                }
            }
        }

        handler1Mock
            .Setup(h => h.HandleAsync(It.IsAny<PaymentEvent>(), It.IsAny<CancellationToken>()))
            .Callback(IncrementAndCheck)
            .Returns(Task.CompletedTask);

        handler2Mock
            .Setup(h => h.HandleAsync(It.IsAny<PaymentEvent>(), It.IsAny<CancellationToken>()))
            .Callback(IncrementAndCheck)
            .Returns(Task.CompletedTask);

        handler3Mock
            .Setup(h => h.HandleAsync(It.IsAny<PaymentEvent>(), It.IsAny<CancellationToken>()))
            .Callback(IncrementAndCheck)
            .Returns(Task.CompletedTask);

        _sut = new PaymentEventProcessor(
            _eventChannel,
            new[] { handler1Mock.Object, handler2Mock.Object, handler3Mock.Object },
            _loggerMock.Object);

        using var cts = new CancellationTokenSource();
        await _sut.StartAsync(cts.Token);

        var paymentEvent = new PaymentReceived
        {
            PaymentHash = "multi-handler-test",
            AmountSat = 5000
        };

        // Act
        await _eventChannel.PublishAsync(paymentEvent);

        // Assert
        await allHandlersComplete.Task.WaitAsync(TimeSpan.FromSeconds(2));

        handler1Mock.Verify(
            h => h.HandleAsync(It.Is<PaymentReceived>(e => e.PaymentHash == "multi-handler-test"), It.IsAny<CancellationToken>()),
            Times.Once);

        handler2Mock.Verify(
            h => h.HandleAsync(It.Is<PaymentReceived>(e => e.PaymentHash == "multi-handler-test"), It.IsAny<CancellationToken>()),
            Times.Once);

        handler3Mock.Verify(
            h => h.HandleAsync(It.Is<PaymentReceived>(e => e.PaymentHash == "multi-handler-test"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessEvents_WithDifferentEventTypes_DispatchesCorrectly()
    {
        // Arrange
        var handlerMock = new Mock<IPaymentEventHandler>();
        var eventsHandled = new List<Type>();
        var allEventsHandled = new TaskCompletionSource<bool>();

        handlerMock
            .Setup(h => h.HandleAsync(It.IsAny<PaymentEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PaymentEvent, CancellationToken>((e, ct) =>
            {
                eventsHandled.Add(e.GetType());
                if (eventsHandled.Count == 2)
                {
                    allEventsHandled.TrySetResult(true);
                }
            })
            .Returns(Task.CompletedTask);

        _sut = new PaymentEventProcessor(
            _eventChannel,
            new[] { handlerMock.Object },
            _loggerMock.Object);

        using var cts = new CancellationTokenSource();
        await _sut.StartAsync(cts.Token);

        var receivedEvent = new PaymentReceived { PaymentHash = "hash-1", AmountSat = 1000 };
        var failedEvent = new PaymentFailed
        {
            PaymentHash = "hash-2",
            ErrorCode = BreezErrorCode.PaymentFailed,
            Reason = "Insufficient funds",
            IsRetryable = true
        };

        // Act
        await _eventChannel.PublishAsync(receivedEvent);
        await _eventChannel.PublishAsync(failedEvent);

        // Assert
        await allEventsHandled.Task.WaitAsync(TimeSpan.FromSeconds(2));

        eventsHandled.Should().HaveCount(2);
        eventsHandled.Should().Contain(typeof(PaymentReceived));
        eventsHandled.Should().Contain(typeof(PaymentFailed));
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ProcessEvents_WhenHandlerThrows_ContinuesProcessing()
    {
        // Arrange
        var faultyHandlerMock = new Mock<IPaymentEventHandler>();
        var healthyHandlerMock = new Mock<IPaymentEventHandler>();

        var healthyHandlerCalled = new TaskCompletionSource<bool>();

        faultyHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<PaymentEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Handler error"));

        healthyHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<PaymentEvent>(), It.IsAny<CancellationToken>()))
            .Callback(() => healthyHandlerCalled.TrySetResult(true))
            .Returns(Task.CompletedTask);

        _sut = new PaymentEventProcessor(
            _eventChannel,
            new[] { faultyHandlerMock.Object, healthyHandlerMock.Object },
            _loggerMock.Object);

        using var cts = new CancellationTokenSource();
        await _sut.StartAsync(cts.Token);

        var paymentEvent = new PaymentReceived
        {
            PaymentHash = "error-test",
            AmountSat = 5000
        };

        // Act
        await _eventChannel.PublishAsync(paymentEvent);

        // Assert - Healthy handler should still be called despite faulty handler throwing
        await healthyHandlerCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));

        healthyHandlerMock.Verify(
            h => h.HandleAsync(It.IsAny<PaymentEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify error was logged
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handler error") || v.ToString()!.Contains("error-test")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            Times.Once);
    }

    [Fact]
    public async Task ProcessEvents_WhenAllHandlersThrow_ContinuesProcessingNextEvent()
    {
        // Arrange
        var handlerMock = new Mock<IPaymentEventHandler>();
        var firstEventHandled = new TaskCompletionSource<bool>();
        var secondEventHandled = new TaskCompletionSource<bool>();
        var callCount = 0;

        handlerMock
            .Setup(h => h.HandleAsync(It.IsAny<PaymentEvent>(), It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    firstEventHandled.TrySetResult(true);
                }
                else if (callCount == 2)
                {
                    secondEventHandled.TrySetResult(true);
                }
            })
            .ThrowsAsync(new InvalidOperationException("Persistent handler error"));

        _sut = new PaymentEventProcessor(
            _eventChannel,
            new[] { handlerMock.Object },
            _loggerMock.Object);

        using var cts = new CancellationTokenSource();
        await _sut.StartAsync(cts.Token);

        var event1 = new PaymentReceived { PaymentHash = "error-1", AmountSat = 1000 };
        var event2 = new PaymentReceived { PaymentHash = "error-2", AmountSat = 2000 };

        // Act
        await _eventChannel.PublishAsync(event1);
        await firstEventHandled.Task.WaitAsync(TimeSpan.FromSeconds(1));

        await _eventChannel.PublishAsync(event2);
        await secondEventHandled.Task.WaitAsync(TimeSpan.FromSeconds(1));

        // Assert - Both events should be attempted despite first one throwing
        handlerMock.Verify(
            h => h.HandleAsync(It.IsAny<PaymentEvent>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    #endregion

    #region Empty Handlers Tests

    [Fact]
    public async Task ProcessEvents_WithNoHandlers_DoesNotThrow()
    {
        // Arrange
        _sut = new PaymentEventProcessor(
            _eventChannel,
            Array.Empty<IPaymentEventHandler>(),
            _loggerMock.Object);

        using var cts = new CancellationTokenSource();
        await _sut.StartAsync(cts.Token);

        var paymentEvent = new PaymentReceived
        {
            PaymentHash = "no-handlers",
            AmountSat = 5000
        };

        // Act - should not throw even with no handlers
        await _eventChannel.PublishAsync(paymentEvent);

        // Give processing time to complete
        await Task.Delay(100);

        // Assert - No errors should be logged
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            Times.Never);
    }

    [Fact]
    public async Task ProcessEvents_WithNullHandlerCollection_DoesNotThrow()
    {
        // Arrange - Testing defensive programming
        _sut = new PaymentEventProcessor(
            _eventChannel,
            null!,
            _loggerMock.Object);

        using var cts = new CancellationTokenSource();

        // Act & Assert - Should handle null handlers gracefully
        var startAction = async () => await _sut.StartAsync(cts.Token);
        await startAction.Should().NotThrowAsync();
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ProcessEvents_WhenCancelled_StopsProcessing()
    {
        // Arrange
        var handlerMock = new Mock<IPaymentEventHandler>();
        var eventsHandled = new List<string>();

        handlerMock
            .Setup(h => h.HandleAsync(It.IsAny<PaymentEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PaymentEvent, CancellationToken>((e, ct) =>
            {
                eventsHandled.Add(e.PaymentHash);
            })
            .Returns(Task.CompletedTask);

        _sut = new PaymentEventProcessor(
            _eventChannel,
            new[] { handlerMock.Object },
            _loggerMock.Object);

        using var cts = new CancellationTokenSource();
        await _sut.StartAsync(cts.Token);

        var event1 = new PaymentReceived { PaymentHash = "before-cancel", AmountSat = 1000 };

        // Act
        await _eventChannel.PublishAsync(event1);
        await Task.Delay(100); // Allow first event to process

        await _sut.StopAsync(CancellationToken.None);

        // Try to publish after cancellation
        var event2 = new PaymentReceived { PaymentHash = "after-cancel", AmountSat = 2000 };
        await _eventChannel.PublishAsync(event2);
        await Task.Delay(200);

        // Assert - Should have processed first event but not second
        eventsHandled.Should().Contain("before-cancel");
        eventsHandled.Should().NotContain("after-cancel");
    }

    [Fact]
    public async Task ProcessEvents_WithCancellationToken_PassesToHandlers()
    {
        // Arrange
        var handlerMock = new Mock<IPaymentEventHandler>();
        CancellationToken capturedToken = default;
        var eventHandled = new TaskCompletionSource<bool>();

        handlerMock
            .Setup(h => h.HandleAsync(It.IsAny<PaymentEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PaymentEvent, CancellationToken>((e, ct) =>
            {
                capturedToken = ct;
                eventHandled.TrySetResult(true);
            })
            .Returns(Task.CompletedTask);

        _sut = new PaymentEventProcessor(
            _eventChannel,
            new[] { handlerMock.Object },
            _loggerMock.Object);

        using var cts = new CancellationTokenSource();
        await _sut.StartAsync(cts.Token);

        var paymentEvent = new PaymentReceived
        {
            PaymentHash = "token-test",
            AmountSat = 5000
        };

        // Act
        await _eventChannel.PublishAsync(paymentEvent);

        // Assert
        await eventHandled.Task.WaitAsync(TimeSpan.FromSeconds(1));

        // Cancellation token should have been passed to handler
        capturedToken.Should().NotBe(default(CancellationToken));
    }

    #endregion

    public void Dispose()
    {
        _sut?.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
        try
        {
            _channel.Writer.Complete();
        }
        catch (ChannelClosedException)
        {
            // Channel already completed - this is expected
        }
    }
}
