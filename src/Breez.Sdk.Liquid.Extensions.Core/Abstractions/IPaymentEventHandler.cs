using Breez.Sdk.Liquid.Extensions.Core.Domain.Events;

namespace Breez.Sdk.Liquid.Extensions.Core.Abstractions;

/// <summary>
/// Interface for handling payment events.
/// Implement this interface to receive notifications about payment lifecycle changes.
/// </summary>
/// <remarks>
/// This interface provides a non-generic contract for handling any type of payment event.
/// Use this when you need to handle multiple event types in a single handler, or when
/// the event type is determined at runtime.
/// </remarks>
/// <example>
/// <code>
/// public class PaymentLogger : IPaymentEventHandler
/// {
///     public async Task HandleAsync(PaymentEvent @event, CancellationToken cancellationToken)
///     {
///         _logger.LogInformation("Payment event: {EventType} for {PaymentHash}",
///             @event.GetType().Name, @event.PaymentHash);
///         await Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public interface IPaymentEventHandler
{
    /// <summary>
    /// Handles a payment event asynchronously.
    /// </summary>
    /// <param name="event">The payment event to handle. Must not be null.</param>
    /// <param name="cancellationToken">
    /// A cancellation token to observe while waiting for the task to complete.
    /// </param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="event"/> is null.
    /// </exception>
    /// <remarks>
    /// Implementations should be idempotent where possible, as the same event may be
    /// delivered multiple times due to retry logic or system failures.
    /// Implementations should not throw exceptions for business logic failures;
    /// instead, they should log errors and handle them gracefully.
    /// </remarks>
    Task HandleAsync(PaymentEvent @event, CancellationToken cancellationToken = default);
}

/// <summary>
/// Typed interface for handling specific payment events.
/// </summary>
/// <typeparam name="TEvent">
/// The type of payment event to handle. Must derive from <see cref="PaymentEvent"/>.
/// </typeparam>
/// <remarks>
/// This interface provides a strongly-typed contract for handling a specific type of payment event.
/// Use this when you want to handle a single event type with compile-time type safety.
/// The event dispatcher will automatically route events to the appropriate handler based on type.
/// </remarks>
/// <example>
/// <code>
/// public class PaymentConfirmedHandler : IPaymentEventHandler&lt;PaymentConfirmed&gt;
/// {
///     public async Task HandleAsync(PaymentConfirmed @event, CancellationToken cancellationToken)
///     {
///         // Handle payment confirmation logic
///         await _notificationService.SendConfirmationAsync(@event.PaymentHash, cancellationToken);
///     }
/// }
/// </code>
/// </example>
public interface IPaymentEventHandler<in TEvent> where TEvent : PaymentEvent
{
    /// <summary>
    /// Handles a specific type of payment event asynchronously.
    /// </summary>
    /// <param name="event">The payment event to handle. Must not be null.</param>
    /// <param name="cancellationToken">
    /// A cancellation token to observe while waiting for the task to complete.
    /// </param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="event"/> is null.
    /// </exception>
    /// <remarks>
    /// Implementations should be idempotent where possible, as the same event may be
    /// delivered multiple times due to retry logic or system failures.
    /// Implementations should not throw exceptions for business logic failures;
    /// instead, they should log errors and handle them gracefully.
    /// Exception handling behavior can be controlled through the event dispatcher configuration.
    /// </remarks>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
