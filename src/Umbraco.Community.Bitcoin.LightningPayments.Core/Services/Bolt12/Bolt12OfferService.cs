using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Breez;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Exceptions;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Bolt12;

/// <summary>
/// Manages BOLT12 offer lifecycle: creation via Breez SDK, persistence, listing, and deactivation.
/// </summary>
public class Bolt12OfferService : IBolt12OfferService
{
    private readonly PaymentDbContext _context;
    private readonly IBreezSdkService _breezSdkService;
    private readonly ILogger<Bolt12OfferService> _logger;

    public Bolt12OfferService(
        PaymentDbContext context,
        IBreezSdkService breezSdkService,
        ILogger<Bolt12OfferService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _breezSdkService = breezSdkService ?? throw new ArgumentNullException(nameof(breezSdkService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Bolt12Offer> CreateOfferAsync(string description, ulong? amountSat, int? contentId, CancellationToken ct = default)
    {
        var sdkAmount = amountSat ?? 0;
        var offerString = await _breezSdkService.CreateBolt12OfferAsync(sdkAmount, description, ct);

        var offer = new Bolt12Offer
        {
            OfferId = Guid.NewGuid(),
            OfferString = offerString,
            Description = description,
            AmountSat = amountSat,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            ContentId = contentId
        };

        _context.Bolt12Offers.Add(offer);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Created BOLT12 offer {OfferId} with description '{Description}'", offer.OfferId, description);

        return offer;
    }

    /// <inheritdoc />
    public async Task<Bolt12Offer?> GetOfferByIdAsync(Guid offerId, CancellationToken ct = default)
    {
        return await _context.Bolt12Offers
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OfferId == offerId, ct);
    }

    /// <inheritdoc />
    public async Task<List<Bolt12Offer>> GetOffersAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        var query = _context.Bolt12Offers.AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(o => o.IsActive);
        }

        return await query.OrderByDescending(o => o.CreatedAt).ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task DeactivateOfferAsync(Guid offerId, CancellationToken ct = default)
    {
        var offer = await _context.Bolt12Offers.FindAsync(new object[] { offerId }, ct);

        if (offer is null)
        {
            throw new PaymentNotFoundException($"Offer {offerId} not found");
        }

        if (!offer.IsActive)
        {
            return; // Idempotent
        }

        offer.IsActive = false;
        offer.DeactivatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Deactivated BOLT12 offer {OfferId}", offerId);
    }

    /// <inheritdoc />
    public async Task<(int paymentCount, long totalReceivedSat, int confirmedCount)> GetOfferPaymentSummaryAsync(Guid offerId, CancellationToken ct = default)
    {
        var offerExists = await _context.Bolt12Offers.AnyAsync(o => o.OfferId == offerId, ct);
        if (!offerExists)
        {
            throw new PaymentNotFoundException($"Offer {offerId} not found");
        }

        var payments = await _context.PaymentStates
            .AsNoTracking()
            .Where(p => p.Bolt12OfferId == offerId)
            .ToListAsync(ct);

        var paymentCount = payments.Count;
        var totalReceivedSat = payments.Sum(p => (long)p.AmountSat);
        var confirmedCount = payments.Count(p => p.Status == PaymentStatus.Paid);

        return (paymentCount, totalReceivedSat, confirmedCount);
    }
}
