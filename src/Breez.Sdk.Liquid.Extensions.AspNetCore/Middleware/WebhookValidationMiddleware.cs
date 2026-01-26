using Breez.Sdk.Liquid.Extensions.Core.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Breez.Sdk.Liquid.Extensions.AspNetCore.Middleware;

/// <summary>
/// ASP.NET Core middleware that validates webhook requests using HMAC-SHA256 signatures.
/// </summary>
/// <remarks>
/// <para>
/// This middleware validates incoming webhook requests by:
/// - Checking the X-Breez-Signature header for HMAC-SHA256 signature
/// - Checking the X-Breez-Timestamp header for replay protection
/// - Verifying the signature matches the expected value
/// - Ensuring the timestamp is within the allowed window
/// </para>
/// <para>
/// Invalid requests are rejected with 401 Unauthorized.
/// Valid requests are passed to the next middleware with the body reset for re-reading.
/// </para>
/// </remarks>
public class WebhookValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly BreezSdkOptions _options;
    private readonly ILogger<WebhookValidationMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookValidationMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="options">The BreezSDK options containing the webhook secret.</param>
    /// <param name="logger">The logger instance.</param>
    public WebhookValidationMiddleware(
        RequestDelegate next,
        IOptions<BreezSdkOptions> options,
        ILogger<WebhookValidationMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invokes the middleware to validate the webhook request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Check if webhook secret is configured
        if (string.IsNullOrEmpty(_options.WebhookSecret))
        {
            _logger.LogWarning("Webhook secret is not configured, rejecting request");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Extract headers
        var signature = context.Request.Headers["X-Breez-Signature"].FirstOrDefault();
        var timestamp = context.Request.Headers["X-Breez-Timestamp"].FirstOrDefault();

        // Validate required headers are present
        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("Missing X-Breez-Signature header");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (string.IsNullOrEmpty(timestamp))
        {
            _logger.LogWarning("Missing X-Breez-Timestamp header");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Enable buffering so we can read the body multiple times
        context.Request.EnableBuffering();

        // Read the body
        string payload;
        using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
        {
            payload = await reader.ReadToEndAsync();
        }

        // Reset the body position for subsequent reads
        context.Request.Body.Position = 0;

        // Validate the signature
        if (!WebhookValidator.ValidateSignature(payload, signature, timestamp, _options.WebhookSecret))
        {
            _logger.LogWarning(
                "Invalid webhook signature or timestamp. Signature: {SignaturePrefix}..., Timestamp: {Timestamp}",
                signature.Length > 10 ? signature[..10] : signature,
                timestamp);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        _logger.LogDebug(
            "Webhook signature validated successfully. Timestamp: {Timestamp}",
            timestamp);

        await _next(context);
    }
}
