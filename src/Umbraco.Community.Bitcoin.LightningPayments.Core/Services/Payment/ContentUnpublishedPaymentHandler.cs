using Microsoft.Extensions.Logging;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Payment;

/// <summary>
/// Marks pending payments as failed when their associated content is unpublished.
/// </summary>
public class ContentUnpublishedPaymentHandler : IContentUnpublishedPaymentHandler
{
    private readonly IPaymentStateService _paymentStateService;
    private readonly ILogger<ContentUnpublishedPaymentHandler> _logger;

    public ContentUnpublishedPaymentHandler(
        IPaymentStateService paymentStateService,
        ILogger<ContentUnpublishedPaymentHandler> logger)
    {
        _paymentStateService = paymentStateService;
        _logger = logger;
    }

    public async Task HandleContentUnpublishedAsync(int contentId, CancellationToken cancellationToken = default)
    {
        var pendingPayments = await _paymentStateService.GetPendingPaymentsByContentIdAsync(contentId);
        var paymentList = pendingPayments.ToList();

        if (paymentList.Count == 0)
        {
            _logger.LogDebug("No pending payments found for unpublished content {ContentId}", contentId);
            return;
        }

        _logger.LogWarning(
            "Content {ContentId} unpublished with {Count} pending payment(s). Marking as failed.",
            contentId, paymentList.Count);

        foreach (var payment in paymentList)
        {
            await _paymentStateService.MarkAsFailedAsync(payment.PaymentHash);
            _logger.LogInformation(
                "Payment {PaymentHash} marked as failed due to content {ContentId} being unpublished.",
                payment.PaymentHash, contentId);
        }
    }
}
