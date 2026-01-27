namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Features.Dashboard;

/// <summary>
/// Lightning wallet receive and send limits.
/// </summary>
public record WalletLimits
{
    /// <summary>
    /// Receive limits for the wallet.
    /// </summary>
    public LimitRange Receive { get; init; } = new();

    /// <summary>
    /// Send limits for the wallet.
    /// </summary>
    public LimitRange Send { get; init; } = new();
}

/// <summary>
/// Represents a range of satoshi amounts.
/// </summary>
public record LimitRange
{
    /// <summary>
    /// Minimum amount in satoshis.
    /// </summary>
    public long MinSat { get; init; }

    /// <summary>
    /// Maximum amount in satoshis.
    /// </summary>
    public long MaxSat { get; init; }
}
