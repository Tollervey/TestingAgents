using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Bolt12;

/// <summary>
/// Service for managing BOLT12 offer lifecycle (create, list, deactivate, payment summary).
/// </summary>
public interface IBolt12OfferService
{
    /// <summary>
    /// Creates a new BOLT12 offer via the Breez SDK and persists it.
    /// </summary>
    /// <param name="description">Human-readable offer description.</param>
    /// <param name="amountSat">Fixed amount in sats, or null for variable amount offers.</param>
    /// <param name="contentId">Optional content ID to associate with the offer.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The persisted <see cref="Bolt12Offer"/>.</returns>
    Task<Bolt12Offer> CreateOfferAsync(string description, ulong? amountSat, int? contentId, CancellationToken ct = default);

    /// <summary>
    /// Gets a single offer by its ID.
    /// </summary>
    /// <param name="offerId">The offer's unique identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The offer, or null if not found.</returns>
    Task<Bolt12Offer?> GetOfferByIdAsync(Guid offerId, CancellationToken ct = default);

    /// <summary>
    /// Lists offers, optionally filtering to active-only.
    /// </summary>
    /// <param name="activeOnly">When true, returns only active offers.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of matching offers.</returns>
    Task<List<Bolt12Offer>> GetOffersAsync(bool activeOnly = true, CancellationToken ct = default);

    /// <summary>
    /// Deactivates an offer by setting IsActive to false.
    /// </summary>
    /// <param name="offerId">The offer ID to deactivate.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeactivateOfferAsync(Guid offerId, CancellationToken ct = default);

    /// <summary>
    /// Gets payment summary for an offer (total count, total received, confirmed count).
    /// </summary>
    /// <param name="offerId">The offer ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tuple of (paymentCount, totalReceivedSat, confirmedCount).</returns>
    Task<(int paymentCount, long totalReceivedSat, int confirmedCount)> GetOfferPaymentSummaryAsync(Guid offerId, CancellationToken ct = default);
}
