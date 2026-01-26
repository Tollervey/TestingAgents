namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Shared;

/// <summary>
/// Represents a monetary amount in a fiat currency.
/// </summary>
public record FiatAmount
{
    /// <summary>
    /// The numeric amount in the specified currency.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// ISO 4217 currency code (e.g., USD, EUR, GBP).
    /// </summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>
    /// Locale-formatted string representation of the amount (e.g., "$42.50").
    /// </summary>
    public string FormattedAmount { get; init; } = string.Empty;
}
