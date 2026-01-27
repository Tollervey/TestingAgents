namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Payment;

/// <summary>
/// Handles cleanup when paywalled content is unpublished while payments are pending.
/// </summary>
public interface IContentUnpublishedPaymentHandler
{
    /// <summary>
    /// Marks all pending payments for the given content as failed and notifies admin.
    /// </summary>
    Task HandleContentUnpublishedAsync(int contentId, CancellationToken cancellationToken = default);
}
