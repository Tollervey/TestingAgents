using System.Text.Json;
using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Breez.Sdk.Liquid.Extensions.Core.Domain.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Breez.Sdk.Liquid.Extensions.AspNetCore.Endpoints;

/// <summary>
/// Extension methods for mapping BreezSDK webhook endpoints.
/// </summary>
public static class WebhookEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Maps BreezSDK webhook endpoints to the endpoint route builder.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="prefix">The route prefix. Defaults to "/api/breez".</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapBreezSdkEndpoints(
        this IEndpointRouteBuilder endpoints,
        string prefix = "/api/breez")
    {
        endpoints.MapPost($"{prefix}/webhook", HandleWebhookAsync)
            .WithName("BreezSdkWebhook")
            .WithTags("BreezSDK")
            .Produces<WebhookResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<IResult> HandleWebhookAsync(
        HttpContext context,
        IPaymentEventChannel eventChannel,
        ILogger<WebhookEndpointHandler> logger,
        CancellationToken cancellationToken)
    {
        WebhookPayload? payload;

        try
        {
            // Try to reset body position if the stream is seekable (e.g., if middleware enabled buffering)
            if (context.Request.Body.CanSeek)
            {
                context.Request.Body.Position = 0;
            }

            payload = await JsonSerializer.DeserializeAsync<WebhookPayload>(
                context.Request.Body,
                JsonOptions,
                cancellationToken);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize webhook payload");
            return Results.Problem(
                title: "Invalid JSON",
                detail: "The request body is not valid JSON",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (payload is null)
        {
            return Results.Problem(
                title: "Invalid payload",
                detail: "The request body is empty",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Validate required fields
        if (string.IsNullOrEmpty(payload.EventType))
        {
            return Results.Problem(
                title: "Missing required field",
                detail: "The eventType field is required",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrEmpty(payload.PaymentHash))
        {
            return Results.Problem(
                title: "Missing required field",
                detail: "The paymentHash field is required",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Map to domain event
        PaymentEvent? domainEvent;
        try
        {
            domainEvent = MapToEvent(payload);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning("Unknown event type: {EventType}", payload.EventType);
            return Results.Problem(
                title: "Unknown event type",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Publish to event channel
        try
        {
            await eventChannel.PublishAsync(domainEvent, cancellationToken);
            logger.LogInformation(
                "Webhook processed: {EventType} for payment {PaymentHash}",
                payload.EventType,
                payload.PaymentHash[..8]);

            return Results.Ok(new WebhookResponse { Success = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish event to channel");
            return Results.Problem(
                title: "Event processing failed",
                detail: "Failed to process the webhook event",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static PaymentEvent MapToEvent(WebhookPayload payload)
    {
        return payload.EventType.ToLowerInvariant() switch
        {
            "invoice_created" => new InvoiceCreated
            {
                PaymentHash = payload.PaymentHash,
                Invoice = payload.Invoice ?? string.Empty,
                AmountSat = payload.AmountSat ?? 0,
                Description = payload.Description,
                ExpiresAt = payload.ExpiresAt ?? DateTimeOffset.UtcNow.AddHours(1),
                Timestamp = payload.Timestamp,
                CorrelationId = payload.CorrelationId
            },
            "payment_received" => new PaymentReceived
            {
                PaymentHash = payload.PaymentHash,
                AmountSat = payload.AmountSat ?? 0,
                Timestamp = payload.Timestamp,
                CorrelationId = payload.CorrelationId
            },
            "payment_confirmed" => new PaymentConfirmed
            {
                PaymentHash = payload.PaymentHash,
                AmountSat = payload.AmountSat ?? 0,
                Preimage = payload.Preimage ?? string.Empty,
                FeeSat = payload.FeeSat,
                Timestamp = payload.Timestamp,
                CorrelationId = payload.CorrelationId
            },
            "payment_failed" => new PaymentFailed
            {
                PaymentHash = payload.PaymentHash,
                ErrorCode = MapErrorCode(payload.ErrorCode),
                Reason = payload.ErrorMessage ?? "Unknown error",
                IsRetryable = IsRetryable(payload.ErrorCode),
                Timestamp = payload.Timestamp,
                CorrelationId = payload.CorrelationId
            },
            "invoice_expired" => new InvoiceExpired
            {
                PaymentHash = payload.PaymentHash,
                ExpiredAt = payload.ExpiredAt ?? payload.Timestamp,
                Timestamp = payload.Timestamp,
                CorrelationId = payload.CorrelationId
            },
            _ => throw new ArgumentException($"Unknown event type: {payload.EventType}")
        };
    }

    private static BreezErrorCode MapErrorCode(int? errorCode)
    {
        if (errorCode is null)
        {
            return BreezErrorCode.PaymentFailed;
        }

        return Enum.IsDefined(typeof(BreezErrorCode), errorCode.Value)
            ? (BreezErrorCode)errorCode.Value
            : BreezErrorCode.PaymentFailed;
    }

    private static bool IsRetryable(int? errorCode)
    {
        if (errorCode is null)
        {
            return false;
        }

        // Transient errors (5xxx) are retryable
        return errorCode >= 5000 && errorCode < 6000;
    }
}

/// <summary>
/// Marker class for endpoint logging.
/// </summary>
internal class WebhookEndpointHandler { }

/// <summary>
/// DTO for incoming webhook payloads.
/// </summary>
public record WebhookPayload
{
    /// <summary>
    /// Type of payment event.
    /// </summary>
    public string EventType { get; init; } = default!;

    /// <summary>
    /// Payment hash (64 hex characters).
    /// </summary>
    public string PaymentHash { get; init; } = default!;

    /// <summary>
    /// Amount in satoshis.
    /// </summary>
    public ulong? AmountSat { get; init; }

    /// <summary>
    /// ISO 8601 UTC timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Payment preimage (for confirmed payments).
    /// </summary>
    public string? Preimage { get; init; }

    /// <summary>
    /// Fee paid in satoshis.
    /// </summary>
    public ulong? FeeSat { get; init; }

    /// <summary>
    /// BreezErrorCode (for failed payments).
    /// </summary>
    public int? ErrorCode { get; init; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// The encoded invoice string.
    /// </summary>
    public string? Invoice { get; init; }

    /// <summary>
    /// Human-readable description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Invoice expiration time.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Time when the invoice expired.
    /// </summary>
    public DateTimeOffset? ExpiredAt { get; init; }

    /// <summary>
    /// Custom metadata.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// DTO for webhook response.
/// </summary>
public record WebhookResponse
{
    /// <summary>
    /// Whether the webhook was processed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Optional status message.
    /// </summary>
    public string? Message { get; init; }
}
