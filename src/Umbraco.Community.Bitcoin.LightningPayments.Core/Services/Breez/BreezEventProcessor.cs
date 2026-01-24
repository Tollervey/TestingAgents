using Breez.Sdk.Liquid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Features.Realtime.Services;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Payment;
using System.Diagnostics;
using System.Threading.Channels;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Breez
{
    internal class BreezEventProcessor : IBreezEventProcessor, IHostedService, IDisposable
    {
        private static readonly ActivitySource _activity = new("BreezEventProcessor");
        private readonly ILogger<BreezEventProcessor> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Channel<SdkEvent.PaymentSucceeded> _queue;
        private readonly Channel<SdkEvent> _eventQueue = Channel.CreateUnbounded<SdkEvent>();
        private Task? _consumerTask;
        private Task? _eventConsumerTask;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public BreezEventProcessor(ILogger<BreezEventProcessor> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            var options = new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait };
            _queue = Channel.CreateBounded<SdkEvent.PaymentSucceeded>(options);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _consumerTask = Task.Run(async () => await ConsumeQueueAsync(_cts.Token), cancellationToken);
            _eventConsumerTask = Task.Run(async () => await ConsumeGeneralEventsAsync(_cts.Token), cancellationToken);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();
            try
            {
                await Task.WhenAll(_consumerTask ?? Task.CompletedTask, _eventConsumerTask ?? Task.CompletedTask);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during task shutdown.");
            }
        }

        public async Task EnqueueEvent(SdkEvent.PaymentSucceeded e)
        {
            await _queue.Writer.WriteAsync(e);
        }

        public async Task Enqueue(SdkEvent e)
        {
            await _eventQueue.Writer.WriteAsync(e);
        }

        private async Task ConsumeGeneralEventsAsync(CancellationToken ct)
        {
            await foreach (var e in _eventQueue.Reader.ReadAllAsync(ct))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var sseHub = scope.ServiceProvider.GetRequiredService<SseHub>();

                    // Build a sanitized payload that excludes any raw invoice, preimage, or other sensitive strings.
                    var payload = new Dictionary<string, object?> { { "type", e.GetType().Name } };

                    var paymentHash = TryExtractPaymentHash(e);
                    if (!string.IsNullOrWhiteSpace(paymentHash))
                    {
                        // only include a truncated hash to avoid leaking full preimage/invoice
                        payload["paymentHash"] = TruncateHash(paymentHash);
                    }

                    // Broadcast to all connected sessions (use a special key "*" to mean broadcast-all)
                    sseHub.Broadcast("*", "breez-event", payload);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to broadcast Breez SDK event.");
                }
            }
        }

        private async Task ConsumeQueueAsync(CancellationToken ct)
        {
            await foreach (var e in _queue.Reader.ReadAllAsync(ct))
            {
                using var activity = _activity.StartActivity("OnPaymentSucceeded");
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentStateService>();
                    var sseHub = scope.ServiceProvider.GetRequiredService<SseHub>();

                    string? paymentHash = null;
                    try
                    {
                        paymentHash = TryExtractPaymentHash(e);
                        activity?.SetTag("paymentHash", paymentHash);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to extract paymentHash using reflection from SDK event.");
                        activity?.SetStatus(ActivityStatusCode.Error, "Failed to extract paymentHash");
                    }

                    if (string.IsNullOrEmpty(paymentHash))
                    {
                        _logger.LogWarning("Unable to extract paymentHash from PaymentSucceeded event.");
                        continue;
                    }

                    // Confirm and broadcast. If duplicate events arrive, calling ConfirmPaymentAsync for an already-paid
                    // hash is idempotent and returns AlreadyConfirmed, so a separate deduper is unnecessary.
                    var result = await paymentService.ConfirmPaymentAsync(paymentHash);
                    _logger.LogInformation("PaymentSucceeded processed for hash: {PaymentHash} => {Result}", paymentHash, result);

                    var state = await paymentService.GetByPaymentHashAsync(paymentHash);
                    if (state != null && !string.IsNullOrWhiteSpace(state.UserSessionId))
                    {
                        // Broadcast a minimal payload for the session; avoid exposing full payment data.
                        sseHub.Broadcast(state.UserSessionId, "payment-succeeded", new { paymentHash = TruncateHash(state.PaymentHash), contentId = state.ContentId, kind = state.Kind.ToString(), status = state.Status.ToString(), amountSat = state.AmountSat });
                    }

                    activity?.SetStatus(ActivityStatusCode.Ok);
                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    _logger.LogError(ex, "Failed to confirm/broadcast payment from SDK event.");
                }
            }
        }

        private static string? TryExtractPaymentHash(SdkEvent e)
        {
            try
            {
                dynamic d = e;
                return (string?)d.details?.details?.paymentHash;
            }
            catch
            {
                return null;
            }
        }

        private static string? TruncateHash(string? hash)
        {
            return hash?.Length > 16 ? hash[..16] + "..." : hash;
        }

        public void Dispose()
        {
            _cts.Cancel();
            _queue.Writer.Complete();
            _eventQueue.Writer.Complete();
            _cts.Dispose();
        }
    }
}




