using System.Security.Cryptography;
using System.Text;

namespace Breez.Sdk.Liquid.Extensions.AspNetCore.Middleware;

/// <summary>
/// Validates webhook requests using HMAC-SHA256 signatures.
/// </summary>
/// <remarks>
/// Implements the webhook security protocol:
/// - Signature is computed over "{timestamp}.{payload}" using the shared secret
/// - Timestamps must be within 5 minutes to prevent replay attacks
/// - Uses constant-time comparison to prevent timing attacks
/// </remarks>
public static class WebhookValidator
{
    /// <summary>
    /// The maximum allowed age for a webhook timestamp in minutes.
    /// Webhooks older than this are rejected to prevent replay attacks.
    /// </summary>
    public const int MaxTimestampAgeMinutes = 5;

    /// <summary>
    /// Validates the webhook signature and timestamp.
    /// </summary>
    /// <param name="payload">The raw request body.</param>
    /// <param name="signature">The X-Breez-Signature header value (format: sha256=xxx).</param>
    /// <param name="timestamp">The X-Breez-Timestamp header value (Unix timestamp).</param>
    /// <param name="secret">The shared webhook secret.</param>
    /// <returns>True if the signature is valid and timestamp is fresh; otherwise false.</returns>
    public static bool ValidateSignature(
        string payload,
        string? signature,
        string? timestamp,
        string? secret)
    {
        if (string.IsNullOrEmpty(signature) ||
            string.IsNullOrEmpty(timestamp) ||
            string.IsNullOrEmpty(secret))
        {
            return false;
        }

        // Validate timestamp format and freshness
        if (!long.TryParse(timestamp, out var unixTimestamp))
        {
            return false;
        }

        var eventTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
        var now = DateTimeOffset.UtcNow;
        var timeDifference = Math.Abs((now - eventTime).TotalMinutes);

        if (timeDifference > MaxTimestampAgeMinutes)
        {
            return false;
        }

        // Compute expected signature
        var signaturePayload = $"{timestamp}.{payload}";
        var expectedSignature = ComputeSignature(signaturePayload, secret);

        // Normalize the provided signature (remove sha256= prefix if present)
        var normalizedSignature = signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)
            ? signature[7..]
            : signature;

        // Constant-time comparison to prevent timing attacks
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature.ToLowerInvariant()),
            Encoding.UTF8.GetBytes(normalizedSignature.ToLowerInvariant()));
    }

    /// <summary>
    /// Computes the HMAC-SHA256 signature for a payload.
    /// </summary>
    /// <param name="payload">The payload to sign.</param>
    /// <param name="secret">The secret key.</param>
    /// <returns>The hex-encoded signature.</returns>
    public static string ComputeSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
