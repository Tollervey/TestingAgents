namespace Breez.Sdk.Liquid.Extensions.Core.Domain;

/// <summary>
/// Categorizes payments by business purpose.
/// </summary>
public enum PaymentKind
{
    /// <summary>
    /// Generic payment without specific category.
    /// </summary>
    Custom,

    /// <summary>
    /// Payment for content access (paywall).
    /// </summary>
    Paywall,

    /// <summary>
    /// Voluntary payment (tip jar).
    /// </summary>
    TipJar,

    /// <summary>
    /// E-commerce purchase.
    /// </summary>
    Purchase,

    /// <summary>
    /// Subscription payment.
    /// </summary>
    Subscription
}
