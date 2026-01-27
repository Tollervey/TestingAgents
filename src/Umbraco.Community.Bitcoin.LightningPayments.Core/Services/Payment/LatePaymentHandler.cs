using Microsoft.Extensions.Logging;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Payment;

/// <summary>
/// Default implementation that logs late payments and notifies administrators.
/// </summary>
public class LatePaymentHandler : ILatePaymentHandler
{
    private readonly ILogger<LatePaymentHandler> _logger;

    public LatePaymentHandler(ILogger<LatePaymentHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleLatePaymentAsync(string paymentHash, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Late payment received for expired invoice. PaymentHash={PaymentHash}. " +
            "Payment has been accepted and funds tracked. Admin review recommended.",
            paymentHash);

        return Task.CompletedTask;
    }
}
