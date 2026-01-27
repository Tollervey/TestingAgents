namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Features.Dashboard;

/// <summary>
/// Current wallet balance information.
/// </summary>
public record WalletBalance
{
    /// <summary>
    /// Current confirmed balance in satoshis.
    /// </summary>
    public long BalanceSat { get; init; }

    /// <summary>
    /// Amount pending to be received in satoshis.
    /// </summary>
    public long PendingReceiveSat { get; init; }

    /// <summary>
    /// Amount pending to be sent in satoshis.
    /// </summary>
    public long PendingSendSat { get; init; }
}
