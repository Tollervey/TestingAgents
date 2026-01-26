using Breez.Sdk.Liquid.Extensions.Core.Domain.Events;

namespace Breez.Sdk.Liquid.Extensions.Core.Abstractions;

/// <summary>
/// Defines a channel for buffering and distributing payment events.
/// </summary>
/// <remarks>
/// The payment event channel provides an asynchronous, buffered communication
/// mechanism for payment events. It supports:
/// - Bounded capacity with configurable backpressure (BoundedChannelFullMode.Wait)
/// - Single-writer, multiple-reader pattern
/// - Graceful completion and cleanup
/// - Cancellation support for both read and write operations
/// - FIFO ordering guarantees
/// </remarks>
public interface IPaymentEventChannel : IAsyncDisposable
{
    /// <summary>
    /// Writes a payment event to the channel asynchronously.
    /// </summary>
    /// <param name="paymentEvent">The payment event to write.</param>
    /// <param name="cancellationToken">Token to cancel the write operation.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the event is written.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to write after the channel has been completed via <see cref="Complete"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when attempting to write after the channel has been disposed.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// If the channel is full (capacity reached), this method will wait asynchronously
    /// until space becomes available (backpressure mode: BoundedChannelFullMode.Wait).
    /// </remarks>
    ValueTask WriteAsync(PaymentEvent paymentEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads all available payment events from the channel asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the read operation.</param>
    /// <returns>
    /// An <see cref="IAsyncEnumerable{T}"/> that yields payment events as they become available.
    /// The enumeration completes when the channel is completed and all buffered events have been read.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when attempting to read after the channel has been disposed.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled via the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method supports multiple concurrent readers. Each event is delivered to exactly one reader
    /// in a round-robin fashion (Channel's default behavior for multiple readers).
    /// </para>
    /// <para>
    /// If no events are available, the reader will wait asynchronously until an event is written
    /// or the channel is completed via <see cref="Complete"/>.
    /// </para>
    /// <para>
    /// Events are delivered in FIFO order (first-in, first-out).
    /// </para>
    /// </remarks>
    IAsyncEnumerable<PaymentEvent> ReadAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Signals that no more events will be written to the channel.
    /// </summary>
    /// <remarks>
    /// <para>
    /// After calling this method, any subsequent calls to <see cref="WriteAsync"/> will throw
    /// an <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// Readers can continue to consume any events that were buffered before completion.
    /// Once all buffered events are read, <see cref="ReadAllAsync"/> will complete.
    /// </para>
    /// <para>
    /// This method is idempotent - calling it multiple times has no effect after the first call.
    /// </para>
    /// </remarks>
    void Complete();

    /// <summary>
    /// Gets the maximum capacity of the channel.
    /// </summary>
    /// <value>
    /// The maximum number of events that can be buffered in the channel.
    /// This value is set during construction and cannot be changed.
    /// </value>
    int Capacity { get; }

    /// <summary>
    /// Publishes a payment event to the channel asynchronously.
    /// </summary>
    /// <param name="paymentEvent">The payment event to publish.</param>
    /// <param name="cancellationToken">Token to cancel the publish operation.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the event is published.</returns>
    /// <remarks>
    /// This is an alias for <see cref="WriteAsync"/> that provides a more semantic name
    /// for event-based patterns.
    /// </remarks>
    ValueTask PublishAsync(PaymentEvent paymentEvent, CancellationToken cancellationToken = default);
}
