using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Payment;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Notifications;

/// <summary>
/// Listens for content unpublish events and cleans up pending payments.
/// </summary>
public class ContentUnpublishedNotificationHandler : INotificationAsyncHandler<ContentUnpublishedNotification>
{
    private readonly IContentUnpublishedPaymentHandler _handler;
    private readonly ILogger<ContentUnpublishedNotificationHandler> _logger;

    public ContentUnpublishedNotificationHandler(
        IContentUnpublishedPaymentHandler handler,
        ILogger<ContentUnpublishedNotificationHandler> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task HandleAsync(ContentUnpublishedNotification notification, CancellationToken cancellationToken)
    {
        foreach (var content in notification.UnpublishedEntities)
        {
            try
            {
                await _handler.HandleContentUnpublishedAsync(content.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling content unpublished for ContentId={ContentId}", content.Id);
            }
        }
    }
}
