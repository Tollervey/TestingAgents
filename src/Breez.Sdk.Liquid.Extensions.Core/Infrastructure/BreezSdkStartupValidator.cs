using Breez.Sdk.Liquid.Extensions.Core.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Breez.Sdk.Liquid.Extensions.Core.Infrastructure;

/// <summary>
/// Hosted service that validates BreezSDK configuration at application startup.
/// Ensures fail-fast behavior for configuration errors before the application starts processing requests.
/// </summary>
/// <remarks>
/// <para>
/// This validator is registered as an <see cref="IHostedService"/> and runs during application startup.
/// It performs the following validations:
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// Triggers validation of <see cref="BreezSdkOptions"/> by accessing the configured values.
/// If validation fails, an <see cref="OptionsValidationException"/> is thrown.
/// </description>
/// </item>
/// <item>
/// <description>
/// Optionally verifies that the BreezSDK version is compatible with this library
/// using <see cref="BreezSdkVersionChecker"/>.
/// </description>
/// </item>
/// <item>
/// <description>
/// Logs the validated configuration with secrets redacted using <see cref="SecretsRedactor"/>.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>Fail-Fast Philosophy:</strong> By validating configuration at startup, this service ensures
/// that configuration errors are caught early rather than during runtime operations. This prevents
/// cascading failures and provides clear diagnostics.
/// </para>
/// </remarks>
/// <example>
/// Register in DI container (typically done automatically when using AddBreezSdk):
/// <code>
/// services.AddHostedService&lt;BreezSdkStartupValidator&gt;();
/// </code>
/// </example>
public class BreezSdkStartupValidator : IHostedService
{
    private readonly IOptions<BreezSdkOptions> _options;
    private readonly ILogger<BreezSdkStartupValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreezSdkStartupValidator"/> class.
    /// </summary>
    /// <param name="options">
    /// The configured BreezSDK options. Accessing <see cref="IOptions{TOptions}.Value"/>
    /// will trigger validation.
    /// </param>
    /// <param name="logger">Logger for recording validation results and version information.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> or <paramref name="logger"/> is null.
    /// </exception>
    public BreezSdkStartupValidator(
        IOptions<BreezSdkOptions> options,
        ILogger<BreezSdkStartupValidator> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates BreezSDK configuration when the application starts.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A completed task.</returns>
    /// <exception cref="OptionsValidationException">
    /// Thrown when BreezSDK configuration validation fails (e.g., missing API key, invalid network).
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when BreezSDK version is incompatible with this library.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method performs the following steps:
    /// </para>
    /// <list type="number">
    /// <item>
    /// <description>
    /// Accesses <see cref="IOptions{TOptions}.Value"/> to trigger validation.
    /// If validation fails, an <see cref="OptionsValidationException"/> is thrown automatically.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Verifies BreezSDK version compatibility using <see cref="BreezSdkVersionChecker"/>.
    /// Logs warnings for untested versions and throws for incompatible versions.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Logs the validated configuration with secrets redacted for security.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// <strong>Note:</strong> This method runs synchronously during application startup.
    /// Any exceptions thrown will prevent the application from starting.
    /// </para>
    /// </remarks>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating BreezSDK configuration...");

        // Accessing _options.Value triggers validation
        // If validation fails, OptionsValidationException is thrown
        var options = _options.Value;

        // Verify SDK version compatibility
        ValidateSdkVersion();

        // Log successful configuration (with redacted secrets)
        LogValidatedConfiguration(options);

        _logger.LogInformation("BreezSDK configuration validation successful");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs cleanup when the application stops. No cleanup is needed for this validator.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Validates that the BreezSDK version is compatible with this library.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the SDK version cannot be determined or is incompatible.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// Attempts to get the SDK version using <see cref="BreezSdkVersionChecker.GetSdkVersion"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Checks compatibility using <see cref="BreezSdkVersionChecker.IsVersionCompatible"/>.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Logs version information and throws if incompatible.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// <strong>Version Compatibility:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// Versions below <see cref="BreezSdkVersionChecker.MinSupportedVersion"/>: Rejected (throws exception)
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Versions between min and <see cref="BreezSdkVersionChecker.MaxTestedVersion"/>: Fully supported
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Versions above max tested: Allowed but logged as warning
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    private void ValidateSdkVersion()
    {
        var sdkVersion = BreezSdkVersionChecker.GetSdkVersion();

        if (sdkVersion == null)
        {
            _logger.LogWarning(
                "Unable to determine BreezSDK version. Ensure Breez.Sdk.Liquid is properly referenced.");
            return;
        }

        _logger.LogInformation("Detected BreezSDK version: {Version}", sdkVersion);

        if (!BreezSdkVersionChecker.IsVersionCompatible(sdkVersion, _logger))
        {
            throw new InvalidOperationException(
                $"BreezSDK version {sdkVersion} is incompatible. " +
                $"Minimum supported version: {BreezSdkVersionChecker.MinSupportedVersion}. " +
                $"Please upgrade the Breez.Sdk.Liquid NuGet package.");
        }

        _logger.LogInformation(
            "BreezSDK version {Version} is compatible (min: {MinVersion}, tested: {MaxVersion})",
            sdkVersion,
            BreezSdkVersionChecker.MinSupportedVersion,
            BreezSdkVersionChecker.MaxTestedVersion);
    }

    /// <summary>
    /// Logs the validated configuration with secrets redacted for security.
    /// </summary>
    /// <param name="options">The validated options to log.</param>
    /// <remarks>
    /// <para>
    /// This method uses <see cref="SecretsRedactor.RedactOptions"/> to create a safe copy
    /// of the configuration with sensitive data redacted:
    /// </para>
    /// <list type="bullet">
    /// <item><description>API Key: First 4 characters + "****"</description></item>
    /// <item><description>Mnemonic: First 2 words + "[...REDACTED]"</description></item>
    /// <item><description>Webhook Secret: Completely redacted as "[REDACTED]"</description></item>
    /// </list>
    /// <para>
    /// This provides visibility into configuration values without exposing secrets in logs.
    /// </para>
    /// </remarks>
    private void LogValidatedConfiguration(BreezSdkOptions options)
    {
        var redacted = SecretsRedactor.RedactOptions(options);

        _logger.LogInformation(
            "BreezSDK configuration: Network={Network}, OfflineMode={OfflineMode}, " +
            "WorkingDirectory={WorkingDirectory}, ApiKey={ApiKey}, " +
            "ConnectionTimeout={ConnectionTimeout}s, MaxInvoiceAmount={MaxInvoiceAmount} sat",
            redacted.Network,
            redacted.OfflineMode,
            redacted.WorkingDirectory,
            redacted.ApiKey,
            redacted.ConnectionTimeoutSeconds,
            redacted.MaxInvoiceAmountSat);

        if (!string.IsNullOrEmpty(redacted.WebhookUrl))
        {
            _logger.LogInformation(
                "Webhook configured: URL={WebhookUrl}, Secret={WebhookSecret}",
                redacted.WebhookUrl,
                redacted.WebhookSecret);
        }

        if (redacted.OfflineMode)
        {
            _logger.LogInformation(
                "Offline mode enabled: MockBalance={MockBalance} sat, " +
                "SimulateDelay={SimulateDelay}ms, FailureRate={FailureRate}",
                redacted.OfflineMockBalanceSat,
                redacted.OfflineSimulateDelayMs,
                redacted.OfflineSimulateFailureRate);
        }
    }
}
