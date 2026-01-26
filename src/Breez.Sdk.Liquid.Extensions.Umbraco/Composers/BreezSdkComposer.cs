using Breez.Sdk.Liquid.Extensions.Umbraco.Components;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Breez.Sdk.Liquid.Extensions.Umbraco.Composers;

/// <summary>
/// Umbraco Composer for registering BreezSDK services.
/// </summary>
/// <remarks>
/// <para>
/// This composer automatically registers BreezSDK services when the Umbraco application starts.
/// It follows the Umbraco composition pattern to integrate with the Umbraco DI container.
/// </para>
/// <para>
/// Configuration is read from the "BreezSdk" section of appsettings.json.
/// For offline/development mode, set BreezSdk:OfflineMode to true.
/// </para>
/// </remarks>
/// <example>
/// appsettings.json configuration:
/// <code>
/// {
///   "BreezSdk": {
///     "ApiKey": "your-api-key",
///     "Mnemonic": "your-mnemonic-words",
///     "Network": "Testnet",
///     "OfflineMode": false
///   }
/// }
/// </code>
/// </example>
public class BreezSdkComposer : IComposer
{
    /// <summary>
    /// Composes BreezSDK services into the Umbraco DI container.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    public void Compose(IUmbracoBuilder builder)
    {
        // Register BreezSDK services using the extension method
        builder.AddBreezSdk();

        // Register the component for lifecycle management
        builder.Components().Append<BreezSdkComponent>();
    }
}
