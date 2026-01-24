namespace Breez.Sdk.Liquid.Extensions.Core.Domain;

/// <summary>
/// Type of Lightning invoice.
/// </summary>
public enum InvoiceType
{
    /// <summary>
    /// Standard BOLT11 Lightning invoice.
    /// </summary>
    Bolt11,

    /// <summary>
    /// BOLT12 offer (reusable invoice).
    /// </summary>
    Bolt12Offer
}
