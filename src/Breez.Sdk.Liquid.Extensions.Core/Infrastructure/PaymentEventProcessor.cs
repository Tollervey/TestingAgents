using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Domain.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Breez.Sdk.Liquid.Extensions.Core.Infrastructure;

/// <summary>
/// Background service that processes payment events from a channel and dispatches them to registered handlers.
/// </summary>
/// <remarks>
/// <para>
/// PaymentEventProcessor implements <see cref="IHostedService"/> to run as a background service
/// that continuously consumes events from the payment event channel.
/// </para>
/// <para>
/// Key features:
/// - Dispatches each event to all registered <see cref="IPaymentEventHandler"/> instances
/// - Continues processing even if individual handlers throw exceptions
/// - Logs handler errors without stopping the event processing loop
/// - Supports graceful shutdown via cancellation token
/// </para>
/// </remarks>
public sealed class PaymentEventProcessor : IHostedService, IDisposable
{
    private readonly IPaymentEventChannel _eventChannel;
    private readonly IEnumerable<IPaymentEventHandler>? _handlers;
    private readonly ILogger<PaymentEventProcessor> _logger;
    private CancellationTokenSource? _stoppingTokenSource;
    private Task? _processingTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentEventProcessor"/> class.
    /// </summary>
    /// <param name="eventChannel">The channel to consume payment events from.</param>
    /// <param name="handlers">The collection of event handlers to dispatch events to.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public PaymentEventProcessor(
        IPaymentEventChannel eventChannel,
        IEnumerable<IPaymentEventHandler>? handlers,
        ILogger<PaymentEventProcessor> logger)
    {
        _eventChannel = eventChannel ?? throw new ArgumentNullException(nameof(eventChannel));
        _handlers = handlers;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PaymentEventProcessor is starting");

        _stoppingTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Start background processing task (fire and forget - tracked via _processingTask)
        _processingTask = ProcessEventsAsync(_stoppingTokenSource.Token);

        // Return immediately - the task runs in the background
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PaymentEventProcessor is stopping");

        if (_stoppingTokenSource == null)
        {
            return;
        }

        // Signal the processing loop to stop
        await _stoppingTokenSource.CancelAsync();

        if (_processingTask != null)
        {
            try
            {
                // Wait for the processing task to complete, but respect the shutdown timeout
                await Task.WhenAny(_processingTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
        }

        _logger.LogInformation("PaymentEventProcessor has stopped");
    }

    /// <summary>
    /// Processes events from the channel until cancellation is requested.
    /// </summary>
    private async Task ProcessEventsAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Starting event processing loop");

        try
        {
            await foreach (var paymentEvent in _eventChannel.ReadAllAsync(stoppingToken))
            {
                await DispatchEventToHandlersAsync(paymentEvent, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Event processing loop cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in event processing loop");
        }

        _logger.LogDebug("Event processing loop exited");
    }

    /// <summary>
    /// Dispatches a payment event to all registered handlers.
    /// </summary>
    /// <param name="paymentEvent">The event to dispatch.</param>
    /// <param name="cancellationToken">Token to cancel handler execution.</param>
    private async Task DispatchEventToHandlersAsync(PaymentEvent paymentEvent, CancellationToken cancellationToken)
    {
        if (_handlers == null)
        {
            _logger.LogDebug(
                "No handlers registered, skipping event {PaymentHash}",
                paymentEvent.PaymentHash);
            return;
        }

        // Create log scope with correlation ID from event (or generate new one)
        var correlationId = paymentEvent.CorrelationId ?? Guid.NewGuid().ToString();

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["PaymentHash"] = paymentEvent.PaymentHash,
            ["EventType"] = paymentEvent.GetType().Name
        }))
        {
            _logger.LogDebug(
                "Dispatching event {EventType} with hash {PaymentHash} to handlers",
                paymentEvent.GetType().Name,
                paymentEvent.PaymentHash);

            foreach (var handler in _handlers)
            {
                try
                {
                    await handler.HandleAsync(paymentEvent, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Don't log cancellation as an error - it's expected behavior
                    throw;
                }
                catch (Exception ex)
                {
                    // Log error but continue processing with other handlers
                    _logger.LogError(
                        ex,
                        "Handler {HandlerType} failed to process event {PaymentHash}: {ErrorMessage}",
                        handler.GetType().Name,
                        paymentEvent.PaymentHash,
                        ex.Message);
                }
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _stoppingTokenSource?.Cancel();
        _stoppingTokenSource?.Dispose();
    }
}
