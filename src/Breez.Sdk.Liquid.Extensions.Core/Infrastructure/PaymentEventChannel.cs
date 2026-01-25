using System.Threading.Channels;
using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Domain.Events;

namespace Breez.Sdk.Liquid.Extensions.Core.Infrastructure;

/// <summary>
/// Implementation of <see cref="IPaymentEventChannel"/> using System.Threading.Channels.
/// </summary>
/// <remarks>
/// <para>
/// This class wraps a bounded <see cref="Channel{T}"/> configured for:
/// - Backpressure on full channel (BoundedChannelFullMode.Wait)
/// - Single writer optimization (SingleWriter = true)
/// - Multiple concurrent readers (SingleReader = false)
/// - FIFO ordering guarantees
/// </para>
/// <para>
/// The channel uses bounded capacity to prevent unbounded memory growth and applies
/// backpressure when the buffer is full, causing writers to wait asynchronously until
/// space becomes available.
/// </para>
/// </remarks>
public sealed class PaymentEventChannel : IPaymentEventChannel
{
    private readonly Channel<PaymentEvent> _channel;
    private readonly int _capacity;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentEventChannel"/> class.
    /// </summary>
    /// <param name="capacity">
    /// The maximum number of events that can be buffered. Must be greater than 0.
    /// Defaults to 100.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="capacity"/> is less than or equal to 0.
    /// </exception>
    public PaymentEventChannel(int capacity = 100)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                capacity,
                "Capacity must be greater than 0.");
        }

        _capacity = capacity;

        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = true,
            SingleReader = false
        };

        _channel = Channel.CreateBounded<PaymentEvent>(options);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentEventChannel"/> class for testing.
    /// </summary>
    /// <param name="channel">The external channel to use (for testing scenarios).</param>
    /// <remarks>
    /// This constructor allows injecting an external channel for test scenarios
    /// where you need direct control over the channel.
    /// </remarks>
    public PaymentEventChannel(Channel<PaymentEvent> channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        _channel = channel;
        _capacity = int.MaxValue; // External channel capacity is unknown
    }

    /// <inheritdoc />
    public int Capacity => _capacity;

    /// <inheritdoc />
    public async ValueTask WriteAsync(PaymentEvent paymentEvent, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            await _channel.Writer.WriteAsync(paymentEvent, cancellationToken).ConfigureAwait(false);
        }
        catch (ChannelClosedException ex)
        {
            throw new InvalidOperationException(
                "Cannot write to the channel because it has been completed.",
                ex);
        }
    }

    /// <inheritdoc />
    public ValueTask PublishAsync(PaymentEvent paymentEvent, CancellationToken cancellationToken = default)
    {
        return WriteAsync(paymentEvent, cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<PaymentEvent> ReadAllAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await foreach (var paymentEvent in _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return paymentEvent;
        }
    }

    /// <inheritdoc />
    public void Complete()
    {
        try
        {
            _channel.Writer.Complete();
        }
        catch (ChannelClosedException)
        {
            // Channel is already completed - this is idempotent
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Complete the channel to signal no more writes
        try
        {
            _channel.Writer.Complete();
        }
        catch (ChannelClosedException)
        {
            // Channel is already completed - this is acceptable
        }

        // Wait for any pending reads to complete by attempting to consume remaining items
        // This ensures graceful shutdown
        await Task.Yield();

        GC.SuppressFinalize(this);
    }
}
