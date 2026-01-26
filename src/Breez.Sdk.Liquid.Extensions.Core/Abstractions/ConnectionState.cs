namespace Breez.Sdk.Liquid.Extensions.Core.Abstractions;

/// <summary>
/// Represents the connection state of the BreezSDK wrapper.
/// </summary>
public enum ConnectionState
{
    /// <summary>
    /// Not connected to the SDK.
    /// </summary>
    Disconnected,

    /// <summary>
    /// Connection attempt in progress.
    /// </summary>
    Connecting,

    /// <summary>
    /// Successfully connected to the SDK.
    /// </summary>
    Connected,

    /// <summary>
    /// Automatic reconnection in progress.
    /// </summary>
    Reconnecting,

    /// <summary>
    /// Failed to connect after all retry attempts exhausted.
    /// </summary>
    Failed
}
