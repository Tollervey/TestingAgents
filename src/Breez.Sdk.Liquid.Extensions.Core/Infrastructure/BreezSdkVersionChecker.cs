using Microsoft.Extensions.Logging;

namespace Breez.Sdk.Liquid.Extensions.Core.Infrastructure;

/// <summary>
/// Utility for checking BreezSDK version compatibility.
/// </summary>
/// <remarks>
/// This utility ensures that the loaded Breez.Sdk.Liquid assembly version is compatible
/// with this library. It checks both minimum version requirements and warns about
/// untested versions that may have breaking changes.
/// </remarks>
public static class BreezSdkVersionChecker
{
    /// <summary>
    /// Minimum supported Breez.Sdk.Liquid version.
    /// </summary>
    /// <remarks>
    /// Versions below this are not supported and will cause <see cref="IsVersionCompatible"/>
    /// to return false and log an error.
    /// </remarks>
    public static readonly Version MinSupportedVersion = new(0, 11, 0);

    /// <summary>
    /// Maximum tested Breez.Sdk.Liquid version.
    /// </summary>
    /// <remarks>
    /// Versions above this will trigger a warning but are still allowed. The library
    /// may work with newer versions, but functionality is not guaranteed.
    /// </remarks>
    public static readonly Version MaxTestedVersion = new(0, 11, 9);

    /// <summary>
    /// Checks if the current SDK version is compatible with this library.
    /// </summary>
    /// <param name="sdkVersion">The SDK version to check.</param>
    /// <param name="logger">Optional logger for warnings and errors. If provided, version
    /// mismatches will be logged with appropriate severity levels.</param>
    /// <returns>
    /// <c>true</c> if the SDK version is within supported range (>= <see cref="MinSupportedVersion"/>);
    /// <c>false</c> if the version is below the minimum supported version.
    /// </returns>
    /// <remarks>
    /// Version compatibility rules:
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// Versions below <see cref="MinSupportedVersion"/> are rejected (returns false, logs error).
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Versions between <see cref="MinSupportedVersion"/> and <see cref="MaxTestedVersion"/>
    /// are fully supported (returns true, no warnings).
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Versions above <see cref="MaxTestedVersion"/> are allowed but untested
    /// (returns true, logs warning).
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var sdkVersion = BreezSdkVersionChecker.GetSdkVersion();
    /// if (sdkVersion == null)
    /// {
    ///     logger.LogError("Breez.Sdk.Liquid assembly not loaded");
    ///     return;
    /// }
    ///
    /// if (!BreezSdkVersionChecker.IsVersionCompatible(sdkVersion, logger))
    /// {
    ///     throw new InvalidOperationException(
    ///         $"Incompatible BreezSDK version: {sdkVersion}");
    /// }
    /// </code>
    /// </example>
    public static bool IsVersionCompatible(Version sdkVersion, ILogger? logger = null)
    {
        if (sdkVersion < MinSupportedVersion)
        {
            logger?.LogError(
                "BreezSDK version {Version} is below minimum supported version {MinVersion}",
                sdkVersion, MinSupportedVersion);
            return false;
        }

        if (sdkVersion > MaxTestedVersion)
        {
            logger?.LogWarning(
                "BreezSDK version {Version} is newer than tested version {MaxVersion}. " +
                "Some features may not work as expected.",
                sdkVersion, MaxTestedVersion);
        }

        return true;
    }

    /// <summary>
    /// Gets the version of the Breez.Sdk.Liquid assembly currently loaded in the AppDomain.
    /// </summary>
    /// <returns>
    /// The version of the loaded Breez.Sdk.Liquid assembly, or <c>null</c> if the assembly
    /// is not loaded or the version cannot be determined.
    /// </returns>
    /// <remarks>
    /// This method searches for the assembly by name ("Breez.Sdk.Liquid") in the current
    /// AppDomain. If multiple versions are loaded (rare), it returns the first match found.
    /// </remarks>
    /// <example>
    /// <code>
    /// var version = BreezSdkVersionChecker.GetSdkVersion();
    /// if (version != null)
    /// {
    ///     Console.WriteLine($"Breez.Sdk.Liquid version: {version}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine("Breez.Sdk.Liquid assembly not loaded");
    /// }
    /// </code>
    /// </example>
    public static Version? GetSdkVersion()
    {
        // Try to get version from loaded assembly
        try
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Breez.Sdk.Liquid");
            return assembly?.GetName().Version;
        }
        catch
        {
            return null;
        }
    }
}
