namespace Breez.Sdk.Liquid.Extensions.Core.Configuration;

/// <summary>
/// Utility class for redacting sensitive information in logs and diagnostics.
/// </summary>
public static class SecretsRedactor
{
    private const string RedactedPlaceholder = "[REDACTED]";
    private const string EmptyPlaceholder = "";
    private const int ApiKeyVisibleChars = 4;
    private const int MnemonicVisibleWords = 2;

    /// <summary>
    /// Redacts an API key, showing only first 4 characters.
    /// </summary>
    /// <param name="apiKey">The API key to redact. May be null or empty.</param>
    /// <returns>
    /// A redacted string showing only the first 4 characters followed by asterisks.
    /// Returns empty string for null, empty, or whitespace inputs.
    /// For keys shorter than 4 characters, returns "****" to completely hide the value.
    /// For keys 4 characters or longer, shows first 4 chars + enough asterisks to match or exceed original length.
    /// </returns>
    /// <example>
    /// <code>
    /// RedactApiKey("test-api-key-12345678") // Returns: "test****************" (length >= 21)
    /// RedactApiKey("abc1") // Returns: "abc1****"
    /// RedactApiKey("ab") // Returns: "****"
    /// RedactApiKey(null) // Returns: ""
    /// RedactApiKey("") // Returns: ""
    /// RedactApiKey("   ") // Returns: ""
    /// </code>
    /// </example>
    public static string RedactApiKey(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return EmptyPlaceholder;
        }

        // For keys shorter than 4 chars, completely redact to avoid revealing length
        if (apiKey.Length < ApiKeyVisibleChars)
        {
            return "****";
        }

        // Show first 4 chars + enough asterisks to match or exceed original length
        var visiblePart = apiKey[..ApiKeyVisibleChars];
        var asteriskCount = Math.Max(4, apiKey.Length - ApiKeyVisibleChars);
        var asterisks = new string('*', asteriskCount);

        return $"{visiblePart}{asterisks}";
    }

    /// <summary>
    /// Redacts a BIP39 mnemonic, showing only first 2 words.
    /// </summary>
    /// <param name="mnemonic">The BIP39 mnemonic to redact. May be null or empty.</param>
    /// <returns>
    /// A redacted string showing only the first 2 words followed by "[...REDACTED]".
    /// Returns empty string for null, empty, or whitespace inputs.
    /// If fewer than 2 words are present, shows all available words.
    /// </returns>
    /// <example>
    /// <code>
    /// RedactMnemonic("abandon abandon ability able about above absent absorb abstract absurd abuse access")
    ///     // Returns: "abandon abandon [...REDACTED]"
    /// RedactMnemonic("abandon") // Returns: "abandon [...REDACTED]"
    /// RedactMnemonic(null) // Returns: ""
    /// RedactMnemonic("") // Returns: ""
    /// RedactMnemonic("   ") // Returns: ""
    /// </code>
    /// </example>
    public static string RedactMnemonic(string? mnemonic)
    {
        if (string.IsNullOrWhiteSpace(mnemonic))
        {
            return EmptyPlaceholder;
        }

        var words = mnemonic.Trim().Split(new[] { ' ', '\t', '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (words.Length == 0)
        {
            return EmptyPlaceholder;
        }

        var visibleWords = words.Take(MnemonicVisibleWords);
        return $"{string.Join(" ", visibleWords)} [...REDACTED]";
    }

    /// <summary>
    /// Completely redacts a webhook secret.
    /// </summary>
    /// <param name="secret">The webhook secret to redact. May be null or empty.</param>
    /// <returns>
    /// "[REDACTED]" for any non-empty value, or empty string for null, empty, or whitespace inputs.
    /// </returns>
    /// <example>
    /// <code>
    /// RedactWebhookSecret("my-secret-key-123") // Returns: "[REDACTED]"
    /// RedactWebhookSecret("x") // Returns: "[REDACTED]"
    /// RedactWebhookSecret(null) // Returns: ""
    /// RedactWebhookSecret("") // Returns: ""
    /// RedactWebhookSecret("   ") // Returns: ""
    /// </code>
    /// </example>
    public static string RedactWebhookSecret(string? secret)
    {
        return string.IsNullOrWhiteSpace(secret)
            ? EmptyPlaceholder
            : RedactedPlaceholder;
    }

    /// <summary>
    /// Creates a copy of BreezSdkOptions with all sensitive fields redacted.
    /// Does not modify the original instance.
    /// </summary>
    /// <param name="options">The BreezSdkOptions instance to redact. Must not be null.</param>
    /// <returns>
    /// A new BreezSdkOptions instance with sensitive fields redacted:
    /// <list type="bullet">
    /// <item><description>ApiKey - redacted using <see cref="RedactApiKey"/></description></item>
    /// <item><description>Mnemonic - redacted using <see cref="RedactMnemonic"/></description></item>
    /// <item><description>WebhookSecret - redacted using <see cref="RedactWebhookSecret"/></description></item>
    /// </list>
    /// All other properties are preserved unchanged.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    /// <example>
    /// <code>
    /// var options = new BreezSdkOptions
    /// {
    ///     ApiKey = "test-api-key-12345",
    ///     Mnemonic = "abandon abandon ability able about above absent absorb abstract absurd abuse access",
    ///     WebhookSecret = "webhook-secret-123",
    ///     Network = BreezNetwork.Testnet,
    ///     WorkingDirectory = "/data"
    /// };
    ///
    /// var redacted = SecretsRedactor.RedactOptions(options);
    /// // redacted.ApiKey => "test****"
    /// // redacted.Mnemonic => "abandon abandon [...REDACTED]"
    /// // redacted.WebhookSecret => "[REDACTED]"
    /// // redacted.Network => BreezNetwork.Testnet (unchanged)
    /// // redacted.WorkingDirectory => "/data" (unchanged)
    /// </code>
    /// </example>
    public static BreezSdkOptions RedactOptions(BreezSdkOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new BreezSdkOptions
        {
            ApiKey = RedactApiKey(options.ApiKey),
            Mnemonic = RedactMnemonic(options.Mnemonic),
            Network = options.Network,
            WorkingDirectory = options.WorkingDirectory,
            WebhookUrl = options.WebhookUrl,
            WebhookSecret = RedactWebhookSecret(options.WebhookSecret),
            MaxInvoiceAmountSat = options.MaxInvoiceAmountSat,
            MaxInvoiceDescriptionLength = options.MaxInvoiceDescriptionLength,
            ConnectionTimeoutSeconds = options.ConnectionTimeoutSeconds,
            OfflineMode = options.OfflineMode,
            CircuitBreaker = options.CircuitBreaker,
            OfflineSimulateDelayMs = options.OfflineSimulateDelayMs,
            OfflineSimulateFailureRate = options.OfflineSimulateFailureRate,
            OfflineMockBalanceSat = options.OfflineMockBalanceSat
        };
    }
}
