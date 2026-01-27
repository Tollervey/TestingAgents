using System.Text.Json.Serialization;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Public.Dto;

/// <summary>
/// Request to create an invoice for paywalled content.
/// </summary>
public class CreatePaywallInvoiceRequest
{
    /// <summary>
    /// Umbraco content node ID.
    /// </summary>
    [JsonPropertyName("contentId")]
    public int ContentId { get; set; }

    /// <summary>
    /// User session identifier.
    /// </summary>
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Access duration tier. Valid values: "1h", "8h", "24h", "7d"
    /// </summary>
    [JsonPropertyName("tier")]
    public string Tier { get; set; } = string.Empty;

    /// <summary>
    /// Optional key to prevent duplicate invoices.
    /// </summary>
    [JsonPropertyName("idempotencyKey")]
    public string? IdempotencyKey { get; set; }
}

/// <summary>
/// Response containing invoice details for payment.
/// </summary>
public class InvoiceResponse
{
    /// <summary>
    /// Unique payment identifier (hex string).
    /// </summary>
    [JsonPropertyName("paymentHash")]
    public string PaymentHash { get; set; } = string.Empty;

    /// <summary>
    /// BOLT11 invoice string.
    /// </summary>
    [JsonPropertyName("invoice")]
    public string Invoice { get; set; } = string.Empty;

    /// <summary>
    /// Amount in satoshis.
    /// </summary>
    [JsonPropertyName("amountSat")]
    public long AmountSat { get; set; }

    /// <summary>
    /// Optional fiat equivalent amount.
    /// </summary>
    [JsonPropertyName("amountFiat")]
    public FiatAmount? AmountFiat { get; set; }

    /// <summary>
    /// Invoice expiration timestamp (ISO 8601).
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Optional Base64 QR code image data URL.
    /// </summary>
    [JsonPropertyName("qrCodeDataUrl")]
    public string? QrCodeDataUrl { get; set; }
}

/// <summary>
/// Status of paywall access for a user session.
/// </summary>
public class PaywallStatus
{
    /// <summary>
    /// Content ID this status is for.
    /// </summary>
    [JsonPropertyName("contentId")]
    public int ContentId { get; set; }

    /// <summary>
    /// Whether the user has paid for access.
    /// </summary>
    [JsonPropertyName("hasPaid")]
    public bool HasPaid { get; set; }

    /// <summary>
    /// When payment was confirmed (null if not paid).
    /// </summary>
    [JsonPropertyName("paidAt")]
    public DateTime? PaidAt { get; set; }

    /// <summary>
    /// When access expires (null if not paid or permanent).
    /// </summary>
    [JsonPropertyName("accessExpiresAt")]
    public DateTime? AccessExpiresAt { get; set; }

    /// <summary>
    /// The tier purchased (null if not paid).
    /// </summary>
    [JsonPropertyName("tier")]
    public string? Tier { get; set; }

    /// <summary>
    /// Checks if access is still valid based on expiration.
    /// </summary>
    [JsonIgnore]
    public bool IsAccessValid =>
        HasPaid && (AccessExpiresAt == null || AccessExpiresAt > DateTime.UtcNow);
}

/// <summary>
/// Fiat currency amount with optional formatting.
/// </summary>
public class FiatAmount
{
    /// <summary>
    /// ISO 4217 currency code (e.g., "USD", "EUR", "GBP").
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Amount in fiat currency.
    /// </summary>
    [JsonPropertyName("amount")]
    public double Amount { get; set; }

    /// <summary>
    /// Locale-formatted amount string (e.g., "$0.45").
    /// </summary>
    [JsonPropertyName("formatted")]
    public string? Formatted { get; set; }

    /// <summary>
    /// Whether the exchange rate is stale (>5 minutes old).
    /// </summary>
    [JsonPropertyName("isStale")]
    public bool IsStale { get; set; }
}

/// <summary>
/// Valid tier duration values.
/// </summary>
public static class PaywallTiers
{
    public const string OneHour = "1h";
    public const string EightHours = "8h";
    public const string TwentyFourHours = "24h";
    public const string SevenDays = "7d";

    public static readonly HashSet<string> ValidTiers = new()
    {
        OneHour,
        EightHours,
        TwentyFourHours,
        SevenDays
    };

    /// <summary>
    /// Checks if a tier value is valid.
    /// </summary>
    public static bool IsValid(string tier) => ValidTiers.Contains(tier);

    /// <summary>
    /// Gets the duration for a tier.
    /// </summary>
    public static TimeSpan? GetDuration(string tier)
    {
        return tier switch
        {
            OneHour => TimeSpan.FromHours(1),
            EightHours => TimeSpan.FromHours(8),
            TwentyFourHours => TimeSpan.FromHours(24),
            SevenDays => TimeSpan.FromDays(7),
            _ => null
        };
    }
}
