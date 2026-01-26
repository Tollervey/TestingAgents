using Microsoft.Extensions.Options;

namespace Breez.Sdk.Liquid.Extensions.Core.Configuration;

/// <summary>
/// Validates <see cref="BreezSdkOptions"/> configuration.
/// </summary>
/// <remarks>
/// This validator ensures that all required configuration values are properly set based on the operating mode.
/// In live mode (OfflineMode = false), sensitive credentials like ApiKey and Mnemonic are required.
/// In offline mode, these credentials are optional but WorkingDirectory is always required.
/// </remarks>
public class BreezSdkOptionsValidator : IValidateOptions<BreezSdkOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, BreezSdkOptions options)
    {
        var failures = new List<string>();

        // CircuitBreaker must not be null
        if (options.CircuitBreaker == null)
        {
            failures.Add("CircuitBreaker configuration is required and cannot be null.");
            // Return early since we can't validate CircuitBreaker properties if it's null
            return ValidateOptionsResult.Fail(failures);
        }

        // WorkingDirectory is always required regardless of mode
        if (string.IsNullOrWhiteSpace(options.WorkingDirectory))
        {
            failures.Add("WorkingDirectory is required and cannot be empty.");
        }

        // In live mode (not offline), ApiKey and Mnemonic are required
        if (!options.OfflineMode)
        {
            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                failures.Add("ApiKey is required when not in offline mode.");
            }

            if (string.IsNullOrWhiteSpace(options.Mnemonic))
            {
                failures.Add("Mnemonic is required when not in offline mode.");
            }
            else
            {
                // Validate mnemonic has at least 12 words (BIP39 standard)
                var wordCount = options.Mnemonic.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                if (wordCount < 12)
                {
                    failures.Add("Mnemonic must contain at least 12 words.");
                }
            }
        }

        // Validate Network enum
        if (!Enum.IsDefined(typeof(BreezNetwork), options.Network))
        {
            failures.Add("Network must be a valid BreezNetwork value (Mainnet, Testnet, or Regtest).");
        }

        // Validate WebhookUrl if provided
        if (!string.IsNullOrWhiteSpace(options.WebhookUrl))
        {
            if (!Uri.TryCreate(options.WebhookUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                failures.Add("WebhookUrl must be a valid HTTP or HTTPS URL.");
            }
        }

        // Validate MaxInvoiceAmountSat
        if (options.MaxInvoiceAmountSat == 0)
        {
            failures.Add("MaxInvoiceAmountSat must be greater than 0.");
        }

        // Validate ConnectionTimeoutSeconds
        if (options.ConnectionTimeoutSeconds <= 0)
        {
            failures.Add("ConnectionTimeoutSeconds must be greater than 0.");
        }

        // Validate CircuitBreaker properties
        if (options.CircuitBreaker.FailureThreshold <= 0)
        {
            failures.Add("CircuitBreaker.FailureThreshold must be greater than 0.");
        }

        if (options.CircuitBreaker.SamplingDurationSeconds <= 0)
        {
            failures.Add("CircuitBreaker.SamplingDurationSeconds must be greater than 0.");
        }

        if (options.CircuitBreaker.BreakDurationSeconds <= 0)
        {
            failures.Add("CircuitBreaker.BreakDurationSeconds must be greater than 0.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
