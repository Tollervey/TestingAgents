using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Infrastructure;

/// <summary>
/// Middleware that extracts or generates a correlation ID for each request,
/// adds it to the response headers and logging scope.
/// </summary>
public class CorrelationIdMiddleware
{
    /// <summary>
    /// The HTTP header name for the correlation ID.
    /// </summary>
    public const string HeaderName = "X-Correlation-ID";

    /// <summary>
    /// The HttpContext.Items key for the correlation ID.
    /// </summary>
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString("D");

        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        Activity.Current?.SetTag("correlation.id", correlationId);

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }
}
