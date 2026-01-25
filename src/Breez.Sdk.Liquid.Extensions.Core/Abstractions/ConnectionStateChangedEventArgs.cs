namespace Breez.Sdk.Liquid.Extensions.Core.Abstractions;

/// <summary>
/// Event arguments for connection state changes.
/// </summary>
public class ConnectionStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// The previous connection state.
    /// </summary>
    public ConnectionState OldState { get; init; }

    /// <summary>
    /// The new connection state.
    /// </summary>
    public ConnectionState NewState { get; init; }

    /// <summary>
    /// Exception that caused the state change, if any (e.g., when transitioning to Failed).
    /// </summary>
    public Exception? Exception { get; init; }
}
