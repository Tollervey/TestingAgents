using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Features.Paywall.Notifications;

/// <summary>
/// Notification handler for paywall message-related content events.
/// When an editor ticks the HasPaywall property and publishes, ensures the matching Member Group exists.
/// </summary>
public class PaywallMessageNotificationHandler(
    IMemberGroupService memberGroupService,
    IPublicAccessService publicAccessService,
    IContentService contentService,
    ILogger<PaywallMessageNotificationHandler> logger) : INotificationAsyncHandler<ContentPublishedNotification>
{
    private readonly IMemberGroupService _memberGroupService = memberGroupService;
    private readonly IPublicAccessService _publicAccessService = publicAccessService;
    private readonly IContentService _contentService = contentService;
    private readonly ILogger<PaywallMessageNotificationHandler> _logger = logger;

    public async Task HandleAsync(ContentPublishedNotification notification, CancellationToken cancellationToken)
    {
        foreach (var content in notification.PublishedEntities)
        {
            if (content.ContentType.Alias != "contentPage")
                continue;

            var hasPaywall = content.GetValue<bool>("hasPaywall");
            if (!hasPaywall)
                continue;

            var groupName = $"Paywall - {content.Name}";
            var groupAlias = $"paywall-{content.Key}";

            // Ensure the member group exists
            var group = await _memberGroupService.GetByNameAsync(groupName);
            if (group == null)
            {
                var newGroup = new MemberGroup { Name = groupName };
                var createResult = await _memberGroupService.CreateAsync(newGroup);
                if (!createResult.Success)
                {
                    _logger.LogError("Failed to create member group '{GroupName}'", groupName);
                    continue;
                }
                _logger.LogInformation("Created member group '{GroupName}' for paywall content '{ContentName}'",
                    groupName, content.Name);
            }

            // Configure public access for this page
            var existing = _publicAccessService.GetEntryForContent(content);
            if (existing != null)
            {
                _logger.LogDebug("Public access already configured for content '{ContentName}' (ID: {ContentId})",
                    content.Name, content.Id);
                continue;
            }

            // Get login and error pages from any existing public access entry
            IContent? loginPage = null;
            IContent? errorPage = null;

            var allEntries = _publicAccessService.GetAll();
            var existingEntry = allEntries.FirstOrDefault();
            if (existingEntry != null)
            {
                // Reuse the login/error pages from existing public access configuration
                loginPage = _contentService.GetById(existingEntry.LoginNodeId);
                errorPage = _contentService.GetById(existingEntry.NoAccessNodeId);

                if (loginPage != null && errorPage != null)
                {
                    _logger.LogDebug("Reusing login page (ID: {LoginId}) and error page (ID: {ErrorId}) from existing public access entry",
                        loginPage.Id, errorPage.Id);
                }
            }

            // Only create public access if we have proper login/error pages
            // Creating it without proper pages would cause a circular redirect
            if (loginPage == null || errorPage == null)
            {
                _logger.LogInformation(
                    "Skipping public access configuration for '{ContentName}' (ID: {ContentId}). " +
                    "Member group '{GroupName}' has been created. " +
                    "To enable protection, set up Public Access manually in Umbraco with proper login/error pages, " +
                    "or publish another page with hasPaywall=true after configuring Public Access elsewhere.",
                    content.Name, content.Id, groupName);
                continue;
            }

            // Create public access entry with the member group
            var entry = new PublicAccessEntry(content, loginPage, errorPage, new[]
            {
                new PublicAccessRule
                {
                    RuleValue = groupName,
                    RuleType = global::Umbraco.Cms.Core.Constants.Conventions.PublicAccess.MemberRoleRuleType
                }
            });

            var result = _publicAccessService.Save(entry);
            if (result.Success)
            {
                _logger.LogInformation(
                    "Configured public access for paywall content '{ContentName}' (ID: {ContentId}) with member group '{GroupAlias}'",
                    content.Name, content.Id, groupAlias);
            }
            else
            {
                _logger.LogError(
                    "Failed to configure public access for paywall content '{ContentName}' (ID: {ContentId})",
                    content.Name, content.Id);
            }
        }
    }
}
