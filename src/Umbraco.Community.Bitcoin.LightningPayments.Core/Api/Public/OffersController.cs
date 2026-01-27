using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Public.Dto;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Public;

/// <summary>
/// Public API controller for BOLT12 offer information.
/// Implements /offers/{offerString}/info from public-api.yaml.
/// </summary>
[ApiController]
[RequireHttps]
[AllowAnonymous]
[Route("api/public/lightning/offers")]
[Produces("application/json")]
public class OffersController : ControllerBase
{
    private readonly PaymentDbContext _context;

    public OffersController(PaymentDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Get public information about a BOLT12 offer by its offer string.
    /// </summary>
    [HttpGet("{offerString}/info")]
    [ProducesResponseType<OfferInfo>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OfferInfo>> GetOfferInfo(string offerString, CancellationToken ct = default)
    {
        var offer = await _context.Bolt12Offers
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OfferString == offerString, ct);

        if (offer is null)
        {
            return NotFound();
        }

        return Ok(new OfferInfo
        {
            OfferString = offer.OfferString,
            Description = offer.Description,
            AmountSat = offer.AmountSat,
            IsActive = offer.IsActive
            // AmountFiat left null; populated by exchange rate service when available (US6)
        });
    }
}
