using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Base;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Public.Dto;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Features.Paywall.Middleware;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Features.PropertyEditor;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Breez;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Invoice;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Payment;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.RateLimiting;
using LegacyPaywallConfig = Umbraco.Community.Bitcoin.LightningPayments.Core.Features.Paywall.Models.PaywallConfig;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Public;

/// <summary>
/// Public API controller for paywall invoice creation and status checking.
/// Implements endpoints from public-api.yaml specification.
/// </summary>
[ApiController]
[RequireHttps]
[AllowAnonymous]
[Route("api/public/lightning/paywall")]
[Produces("application/json")]
public class PaywallController : ControllerBase
{
    private readonly IBreezSdkService _breezSdkService;
    private readonly IPaymentStateService _paymentStateService;
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly ILogger<PaywallController> _logger;
    private readonly IRateLimiter _rateLimiter;
    private readonly IInvoiceHelper _invoiceHelper;

    public PaywallController(
        IBreezSdkService breezSdkService,
        IPaymentStateService paymentStateService,
        IUmbracoContextFactory umbracoContextFactory,
        ILogger<PaywallController> logger,
        IRateLimiter rateLimiter,
        IInvoiceHelper invoiceHelper)
    {
        _breezSdkService = breezSdkService;
        _paymentStateService = paymentStateService;
        _umbracoContextFactory = umbracoContextFactory;
        _logger = logger;
        _rateLimiter = rateLimiter;
        _invoiceHelper = invoiceHelper;
    }

    /// <summary>
    /// Creates a Lightning invoice for accessing paywalled content.
    /// Supports tiered pricing with different durations.
    /// </summary>
    /// <param name="request">Invoice creation request with content ID, session, and tier</param>
    /// <returns>Invoice response with payment details</returns>
    [HttpPost("invoice")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreatePaywallInvoice([FromBody] CreatePaywallInvoiceRequest request)
    {
        // Rate limiting
        var rateResult = CheckRateLimit("paywall-invoice");
        if (!rateResult.Allowed)
        {
            Response.Headers["Retry-After"] = Math.Ceiling(rateResult.RetryAfter.TotalSeconds).ToString();
            return Problem(
                title: "Too Many Requests",
                detail: "Rate limit exceeded. Please try again later.",
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        // Validate request
        if (request.ContentId <= 0)
        {
            return Problem(
                title: "Invalid Request",
                detail: "Content ID must be a positive integer.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            return Problem(
                title: "Invalid Request",
                detail: "Session ID is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!PaywallTiers.IsValid(request.Tier))
        {
            return Problem(
                title: "Invalid Request",
                detail: $"Invalid tier. Valid values are: {string.Join(", ", PaywallTiers.ValidTiers)}",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            // Get content and paywall configuration
            var (content, paywallConfig) = GetContentAndPaywallConfig(request.ContentId);
            if (content == null)
            {
                return Problem(
                    title: "Not Found",
                    detail: "Content not found.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            if (paywallConfig == null || !paywallConfig.Enabled)
            {
                return Problem(
                    title: "Not Found",
                    detail: "Content is not paywalled.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            // Get price for requested tier
            var tierPrice = paywallConfig.GetTierPrice(request.Tier);
            if (tierPrice == null || tierPrice == 0)
            {
                return Problem(
                    title: "Invalid Request",
                    detail: $"Tier '{request.Tier}' is not configured for this content.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            _logger.LogInformation(
                "Creating paywall invoice for ContentId={ContentId}, Tier={Tier}, Amount={Amount} sats",
                request.ContentId, request.Tier, tierPrice);

            // Create invoice
            var description = $"Access to content ID {request.ContentId} ({request.Tier})";
            var (invoice, paymentHash) = await _invoiceHelper.CreateInvoiceAndHashAsync(tierPrice.Value, description);

            // Record pending payment
            await _paymentStateService.AddPendingPaymentAsync(
                paymentHash,
                request.ContentId,
                request.SessionId);

            // Get expiry
            var expiry = await _invoiceHelper.TryGetInvoiceExpiryAsync(invoice);
            var expiresAt = expiry.HasValue
                ? expiry.Value.UtcDateTime
                : DateTime.UtcNow.AddMinutes(15); // Default 15 min expiry

            return Ok(new InvoiceResponse
            {
                PaymentHash = paymentHash,
                Invoice = invoice,
                AmountSat = (long)tierPrice.Value,
                ExpiresAt = expiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating paywall invoice for ContentId={ContentId}", request.ContentId);
            return Problem(
                title: "Server Error",
                detail: "An error occurred while creating the invoice.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Checks if the user has paid for access to specific content.
    /// </summary>
    /// <param name="sessionId">User session identifier</param>
    /// <param name="contentId">Content node ID</param>
    /// <returns>Payment status for the content</returns>
    [HttpGet("status/{sessionId}")]
    [ProducesResponseType(typeof(PaywallStatus), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaywallStatus(
        [FromRoute] string sessionId,
        [FromQuery] int contentId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Problem(
                title: "Invalid Request",
                detail: "Session ID is required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (contentId <= 0)
        {
            return Problem(
                title: "Invalid Request",
                detail: "Content ID must be a positive integer.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var paymentState = await _paymentStateService.GetPaymentStateAsync(sessionId, contentId);

        if (paymentState == null || paymentState.Status != Data.Models.PaymentStatus.Paid)
        {
            return Ok(new PaywallStatus
            {
                ContentId = contentId,
                HasPaid = false
            });
        }

        // TODO: Implement tier and expiration tracking
        // For now, assume permanent access once paid
        return Ok(new PaywallStatus
        {
            ContentId = contentId,
            HasPaid = true,
            PaidAt = DateTime.UtcNow, // Would come from payment record
            AccessExpiresAt = null, // Would be calculated from tier
            Tier = null // Would come from payment metadata
        });
    }

    private (Umbraco.Cms.Core.Models.PublishedContent.IPublishedContent? Content, PaywallConfig? Config) GetContentAndPaywallConfig(int contentId)
    {
        using var contextRef = _umbracoContextFactory.EnsureUmbracoContext();
        var content = contextRef.UmbracoContext.Content?.GetById(contentId);

        if (content == null)
        {
            return (null, null);
        }

        // Try to get paywall config from the new tiered property editor first
        var paywallValue = content.Value<PaywallConfig>("breezPaywall");
        if (paywallValue != null)
        {
            return (content, paywallValue);
        }

        // Fall back to legacy paywall config from old model
        var legacyConfig = content.Value<LegacyPaywallConfig>("breezPaywall");
        if (legacyConfig != null)
        {
            // Convert legacy config to new tiered format
            return (content, new PaywallConfig
            {
                Enabled = legacyConfig.Enabled,
                TierPrices = legacyConfig.Fee > 0
                    ? new Dictionary<string, ulong> { ["24h"] = legacyConfig.Fee }
                    : new Dictionary<string, ulong>(),
                Description = legacyConfig.Message ?? legacyConfig.CustomMessage
            });
        }

        return (content, null);
    }

    private (bool Allowed, TimeSpan RetryAfter) CheckRateLimit(string endpointKey)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var session = Request.Cookies[PaywallMiddleware.PaywallCookieName] ?? "anon";
        var key = $"{endpointKey}:{ip}:{session}";
        var allowed = _rateLimiter.TryConsume(key, 5, TimeSpan.FromSeconds(30), out var retry);
        return (allowed, retry);
    }
}
