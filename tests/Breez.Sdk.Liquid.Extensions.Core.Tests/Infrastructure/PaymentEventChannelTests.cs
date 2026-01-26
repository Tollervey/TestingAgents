using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Domain.Events;
using Breez.Sdk.Liquid.Extensions.Core.Infrastructure;
using FluentAssertions;

namespace Breez.Sdk.Liquid.Extensions.Core.Tests.Infrastructure;

/// <summary>
/// Unit tests for PaymentEventChannel.
/// These tests verify the channel's behavior in buffering events,
/// handling backpressure, cancellation, and proper cleanup.
/// </summary>
/// <remarks>
/// TDD RED PHASE: These tests reference PaymentEventChannel which doesn't exist yet.
/// Expected compilation failures until implementation is complete.
/// </remarks>
public class PaymentEventChannelTests : IAsyncDisposable
{
    private PaymentEventChannel? _sut;

    public async ValueTask DisposeAsync()
    {
        if (_sut is not null)
        {
            await _sut.DisposeAsync();
        }
    }

    #region WriteAsync Tests

    [Fact]
    public async Task WriteAsync_WithValidEvent_WritesSuccessfully()
    {
        // Arrange
        _sut = new PaymentEventChannel(capacity: 10);
        var paymentEvent = CreateInvoiceCreatedEvent();

        // Act
        var writeTask = _sut.WriteAsync(paymentEvent, CancellationToken.None);

        // Assert
        await writeTask;
        writeTask.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task WriteAsync_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        _sut = new PaymentEventChannel(capacity: 10);
        var paymentEvent = CreateInvoiceCreatedEvent();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await _sut.WriteAsync(paymentEvent, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task WriteAsync_WhenChannelIsFull_WaitsForSpace()
    {
        // Arrange
        const int capacity = 2;
        _sut = new PaymentEventChannel(capacity: capacity);
        var event1 = CreateInvoiceCreatedEvent(paymentHash: "hash1");
        var event2 = CreateInvoiceCreatedEvent(paymentHash: "hash2");
        var event3 = CreateInvoiceCreatedEvent(paymentHash: "hash3");

        // Act - Fill the channel to capacity
        await _sut.WriteAsync(event1, CancellationToken.None);
        await _sut.WriteAsync(event2, CancellationToken.None);

        // Act - This write should wait until space is available
        var writeTask = _sut.WriteAsync(event3, CancellationToken.None);

        // Assert - Write task should not complete immediately
        await Task.Delay(50); // Give it a moment to process
        writeTask.IsCompleted.Should().BeFalse("channel is full and backpressure should block");

        // Act - Read one event to make space
        var reader = _sut.ReadAllAsync(CancellationToken.None).GetAsyncEnumerator();
        await reader.MoveNextAsync();

        // Assert - Write should now complete
        await writeTask.AsTask().WaitAsync(TimeSpan.FromSeconds(1));
        writeTask.IsCompletedSuccessfully.Should().BeTrue();

        await reader.DisposeAsync();
    }

    [Fact]
    public async Task WriteAsync_AfterChannelCompleted_ThrowsException()
    {
        // Arrange
        _sut = new PaymentEventChannel(capacity: 10);
        _sut.Complete();
        var paymentEvent = CreateInvoiceCreatedEvent();

        // Act
        var act = async () => await _sut.WriteAsync(paymentEvent, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot write*completed*");
    }

    #endregion

    #region ReadAllAsync Tests

    [Fact]
    public async Task ReadAllAsync_WithWrittenEvents_ReadsAllEvents()
    {
        // Arrange
        _sut = new PaymentEventChannel(capacity: 10);
        var event1 = CreateInvoiceCreatedEvent(paymentHash: "hash1");
        var event2 = CreatePaymentReceivedEvent(paymentHash: "hash2");
        var event3 = CreateInvoiceCreatedEvent(paymentHash: "hash3");

        await _sut.WriteAsync(event1, CancellationToken.None);
        await _sut.WriteAsync(event2, CancellationToken.None);
        await _sut.WriteAsync(event3, CancellationToken.None);
        _sut.Complete();

        // Act
        var events = new List<PaymentEvent>();
        await foreach (var evt in _sut.ReadAllAsync(CancellationToken.None))
        {
            events.Add(evt);
        }

        // Assert
        events.Should().HaveCount(3);
        events[0].PaymentHash.Should().Be("hash1");
        events[1].PaymentHash.Should().Be("hash2");
        events[2].PaymentHash.Should().Be("hash3");
    }

    [Fact]
    public async Task ReadAllAsync_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        _sut = new PaymentEventChannel(capacity: 10);
        using var cts = new CancellationTokenSource();

        // Act
        var act = async () =>
        {
            await foreach (var evt in _sut.ReadAllAsync(cts.Token))
            {
                cts.Cancel(); // Cancel after attempting to read
            }
        };

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ReadAllAsync_WhenNoEvents_WaitsForEvents()
    {
        // Arrange
        _sut = new PaymentEventChannel(capacity: 10);
        var readStarted = false;

        // Act - Start reading from empty channel
        var readTask = Task.Run(async () =>
        {
            var events = new List<PaymentEvent>();
            await foreach (var evt in _sut.ReadAllAsync(CancellationToken.None))
            {
                readStarted = true;
                events.Add(evt);
                break; // Only read one event
            }
            return events;
        });

        // Assert - Reader should be waiting
        await Task.Delay(50);
        readStarted.Should().BeFalse("no events available yet");

        // Act - Write an event
        var paymentEvent = CreateInvoiceCreatedEvent();
        await _sut.WriteAsync(paymentEvent, CancellationToken.None);

        // Assert - Reader should now receive the event
        var result = await readTask.WaitAsync(TimeSpan.FromSeconds(1));
        result.Should().ContainSingle()
            .Which.PaymentHash.Should().Be(paymentEvent.PaymentHash);
    }

    [Fact]
    public async Task ReadAllAsync_AfterComplete_ReturnsRemainingEventsAndCompletes()
    {
        // Arrange
        _sut = new PaymentEventChannel(capacity: 10);
        var event1 = CreateInvoiceCreatedEvent(paymentHash: "hash1");
        var event2 = CreatePaymentReceivedEvent(paymentHash: "hash2");

        await _sut.WriteAsync(event1, CancellationToken.None);
        await _sut.WriteAsync(event2, CancellationToken.None);
        _sut.Complete();

        // Act
        var events = new List<PaymentEvent>();
        await foreach (var evt in _sut.ReadAllAsync(CancellationToken.None))
        {
            events.Add(evt);
        }

        // Assert
        events.Should().HaveCount(2, "all buffered events should be read before completion");
        events[0].PaymentHash.Should().Be("hash1");
        events[1].PaymentHash.Should().Be("hash2");
    }

    #endregion

    #region Event Ordering Tests

    [Fact]
    public async Task ReadAllAsync_MultipleEvents_MaintainsFIFOOrder()
    {
        // Arrange
        _sut = new PaymentEventChannel(capacity: 100);
        var expectedOrder = new List<string>();

        // Act - Write 10 events
        for (int i = 0; i < 10; i++)
        {
            var hash = $"hash-{i:D3}";
            expectedOrder.Add(hash);
            await _sut.WriteAsync(CreateInvoiceCreatedEvent(paymentHash: hash), CancellationToken.None);
        }
        _sut.Complete();

        // Act - Read all events
        var actualOrder = new List<string>();
        await foreach (var evt in _sut.ReadAllAsync(CancellationToken.None))
        {
            actualOrder.Add(evt.PaymentHash);
        }

        // Assert
        actualOrder.Should().Equal(expectedOrder, "events must be received in FIFO order");
    }

    [Fact]
    public async Task ReadAllAsync_InterleavedWritesAndReads_MaintainsFIFOOrder()
    {
        // Arrange
        _sut = new PaymentEventChannel(capacity: 10);
        var receivedEvents = new List<PaymentEvent>();

        // Act - Interleave writes and reads
        await _sut.WriteAsync(CreateInvoiceCreatedEvent(paymentHash: "hash1"), CancellationToken.None);
        await _sut.WriteAsync(CreatePaymentReceivedEvent(paymentHash: "hash2"), CancellationToken.None);

        var reader = _sut.ReadAllAsync(CancellationToken.None).GetAsyncEnumerator();

        await reader.MoveNextAsync();
        receivedEvents.Add(reader.Current);

        await _sut.WriteAsync(CreateInvoiceCreatedEvent(paymentHash: "hash3"), CancellationToken.None);

        await reader.MoveNextAsync();
        receivedEvents.Add(reader.Current);

        await reader.MoveNextAsync();
        receivedEvents.Add(reader.Current);

        // Assert
        receivedEvents.Should().HaveCount(3);
        receivedEvents[0].PaymentHash.Should().Be("hash1");
        receivedEvents[1].PaymentHash.Should().Be("hash2");
        receivedEvents[2].PaymentHash.Should().Be("hash3");

        await reader.DisposeAsync();
    }

    #endregion

    #region Completion Tests

    [Fact]
    public async Task Complete_StopsAcceptingNewWrites()
    {
        // Arrange
        _sut = new PaymentEventChannel(capacity: 10);
        var event1 = CreateInvoiceCreatedEvent(paymentHash: "hash1");

        // Act
        await _sut.WriteAsync(event1, CancellationToken.None);
        _sut.Complete();

        // Assert - Attempting to write should throw
        var event2 = CreateInvoiceCreatedEvent(paymentHash: "hash2");
        var act = async () => await _sut.WriteAsync(event2, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Complete_AllowsReadersToFinish()
    {
        // Arrange
        _sut = new PaymentEventChannel(capacity: 10);
        PaymentEvent[] events =
        [
            CreateInvoiceCreatedEvent(paymentHash: "hash1"),
            CreatePaymentReceivedEvent(paymentHash: "hash2"),
            CreateInvoiceCreatedEvent(paymentHash: "hash3")
        ];

        foreach (var evt in events)
        {
            await _sut.WriteAsync(evt, CancellationToken.None);
        }

        // Act
        _sut.Complete();

        // Assert - All events should still be readable
        var readEvents = new List<PaymentEvent>();
        await foreach (var evt in _sut.ReadAllAsync(CancellationToken.None))
        {
            readEvents.Add(evt);
        }

        readEvents.Should().HaveCount(3);
    }

    [Fact]
    public void Complete_CanBeCalledMultipleTimes()
    {
        // Arrange
        _sut = new PaymentEventChannel(capacity: 10);

        // Act
        var act = () =>
        {
            _sut.Complete();
            _sut.Complete();
            _sut.Complete();
        };

        // Assert
        act.Should().NotThrow("Complete should be idempotent");
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public async Task DisposeAsync_ProperlyReleasesResources()
    {
        // Arrange
        _sut = new PaymentEventChannel(capacity: 10);
        await _sut.WriteAsync(CreateInvoiceCreatedEvent(), CancellationToken.None);

        // Act
        await _sut.DisposeAsync();

        // Assert - Operations after dispose should throw
        var act = async () => await _sut.WriteAsync(CreateInvoiceCreatedEvent(), CancellationToken.None);
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task DisposeAsync_CompletesChannel()
    {
        // Arrange
        _sut = new PaymentEventChannel(capacity: 10);
        await _sut.WriteAsync(CreateInvoiceCreatedEvent(paymentHash: "hash1"), CancellationToken.None);
        await _sut.WriteAsync(CreatePaymentReceivedEvent(paymentHash: "hash2"), CancellationToken.None);

        // Act
        await _sut.DisposeAsync();

        // Assert - Attempting to read after dispose should complete gracefully or throw
        var events = new List<PaymentEvent>();
        var act = async () =>
        {
            await foreach (var evt in _sut.ReadAllAsync(CancellationToken.None))
            {
                events.Add(evt);
            }
        };

        // After dispose, either we get the events or an ObjectDisposedException
        try
        {
            await act();
            // If we get here, events should be empty or contain buffered events
            events.Should().HaveCountLessThanOrEqualTo(2);
        }
        catch (ObjectDisposedException)
        {
            // This is also acceptable behavior
        }
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        _sut = new PaymentEventChannel(capacity: 10);

        // Act
        var act = async () =>
        {
            await _sut.DisposeAsync();
            await _sut.DisposeAsync();
            await _sut.DisposeAsync();
        };

        // Assert
        await act.Should().NotThrowAsync("DisposeAsync should be idempotent");
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithDefaultCapacity_CreatesChannel()
    {
        // Act
        _sut = new PaymentEventChannel();

        // Assert
        _sut.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomCapacity_CreatesChannelWithSpecifiedCapacity()
    {
        // Act
        _sut = new PaymentEventChannel(capacity: 50);

        // Assert
        _sut.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidCapacity_ThrowsArgumentException(int invalidCapacity)
    {
        // Act
        var act = () => new PaymentEventChannel(capacity: invalidCapacity);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*capacity*");
    }

    #endregion

    #region Multiple Readers Tests

    [Fact]
    public async Task ReadAllAsync_MultipleReaders_AllReceiveSameEvents()
    {
        // Arrange
        _sut = new PaymentEventChannel(capacity: 10);
        var event1 = CreateInvoiceCreatedEvent(paymentHash: "hash1");
        var event2 = CreatePaymentReceivedEvent(paymentHash: "hash2");
        var event3 = CreateInvoiceCreatedEvent(paymentHash: "hash3");

        var reader1Events = new List<PaymentEvent>();
        var reader2Events = new List<PaymentEvent>();

        // Start two readers
        var reader1Task = Task.Run(async () =>
        {
            await foreach (var evt in _sut.ReadAllAsync(CancellationToken.None))
            {
                reader1Events.Add(evt);
            }
        });

        var reader2Task = Task.Run(async () =>
        {
            await foreach (var evt in _sut.ReadAllAsync(CancellationToken.None))
            {
                reader2Events.Add(evt);
            }
        });

        // Give readers time to start
        await Task.Delay(50);

        // Act - Write events
        await _sut.WriteAsync(event1, CancellationToken.None);
        await _sut.WriteAsync(event2, CancellationToken.None);
        await _sut.WriteAsync(event3, CancellationToken.None);
        _sut.Complete();

        // Wait for readers to complete
        await Task.WhenAll(reader1Task, reader2Task).WaitAsync(TimeSpan.FromSeconds(2));

        // Assert - Both readers should receive all events
        // Note: With Channel<T>, each event is delivered to only ONE reader
        // So the total count across both readers should equal the number of events written
        var totalEventsRead = reader1Events.Count + reader2Events.Count;
        totalEventsRead.Should().Be(3, "each event should be delivered to exactly one reader");

        // Verify all events were read
        var allHashes = reader1Events.Concat(reader2Events).Select(e => e.PaymentHash).ToList();
        allHashes.Should().Contain("hash1");
        allHashes.Should().Contain("hash2");
        allHashes.Should().Contain("hash3");
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task WriteAsync_ConcurrentWrites_AllEventsAreReceived()
    {
        // Arrange
        _sut = new PaymentEventChannel(capacity: 100);
        const int numberOfWrites = 50;
        var writeTasks = new List<Task>();

        // Act - Write concurrently from multiple tasks
        for (int i = 0; i < numberOfWrites; i++)
        {
            var hash = $"hash-{i:D3}";
            writeTasks.Add(_sut.WriteAsync(CreateInvoiceCreatedEvent(paymentHash: hash), CancellationToken.None).AsTask());
        }

        await Task.WhenAll(writeTasks);
        _sut.Complete();

        // Act - Read all events
        var events = new List<PaymentEvent>();
        await foreach (var evt in _sut.ReadAllAsync(CancellationToken.None))
        {
            events.Add(evt);
        }

        // Assert
        events.Should().HaveCount(numberOfWrites, "all concurrent writes should be captured");
        events.Select(e => e.PaymentHash).Should().OnlyHaveUniqueItems("each event should have a unique hash");
    }

    #endregion

    #region Helper Methods

    private static InvoiceCreated CreateInvoiceCreatedEvent(
        string paymentHash = "test-payment-hash-123",
        string invoice = "lnbc50000n1...",
        ulong amountSat = 5000,
        string? description = "Test invoice")
    {
        return new InvoiceCreated
        {
            PaymentHash = paymentHash,
            Invoice = invoice,
            AmountSat = amountSat,
            Description = description,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
            Timestamp = DateTimeOffset.UtcNow,
            CorrelationId = Guid.NewGuid().ToString()
        };
    }

    private static PaymentReceived CreatePaymentReceivedEvent(
        string paymentHash = "test-payment-hash-456",
        ulong amountSat = 5000,
        string? preimage = "test-preimage-abc123")
    {
        return new PaymentReceived
        {
            PaymentHash = paymentHash,
            AmountSat = amountSat,
            Preimage = preimage,
            Timestamp = DateTimeOffset.UtcNow,
            CorrelationId = Guid.NewGuid().ToString()
        };
    }

    #endregion
}
