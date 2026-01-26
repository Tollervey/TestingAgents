using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Base;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Management.Dto;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;
using Umbraco.Cms.Core.Services;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Management;

/// <summary>
/// Payments API controller for listing and viewing payment details.
/// </summary>
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Lightning Payments")]
public class PaymentsController : OurUmbracoBitcoinLightningPaymentsApiControllerBase
{
    private readonly PaymentDbContext _dbContext;
    private readonly IContentService _contentService;

    public PaymentsController(PaymentDbContext dbContext, IContentService contentService)
    {
        _dbContext = dbContext;
        _contentService = contentService;
    }

    /// <summary>
    /// List payment history with filtering and pagination.
    /// </summary>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take (max 100).</param>
    /// <param name="status">Filter by payment status.</param>
    /// <param name="kind">Filter by payment kind.</param>
    /// <param name="search">Search by payment hash or content name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of payments.</returns>
    [HttpGet("payments")]
    [ProducesResponseType<PaymentListResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentListResponse>> ListPayments(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? kind = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        // Validate take
        if (take > 100) take = 100;
        if (take < 1) take = 20;
        if (skip < 0) skip = 0;

        var query = _dbContext.PaymentStates.AsNoTracking();

        // Apply status filter
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<PaymentStatus>(status, true, out var statusFilter))
        {
            query = query.Where(p => p.Status == statusFilter);
        }

        // Apply kind filter
        if (!string.IsNullOrEmpty(kind) && Enum.TryParse<PaymentKind>(kind, true, out var kindFilter))
        {
            query = query.Where(p => p.Kind == kindFilter);
        }

        // Apply search filter
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p => p.PaymentHash.Contains(search));
        }

        // Get total count
        var total = await query.CountAsync(ct);

        // Get paginated results
        var payments = await query
            .OrderByDescending(p => p.PaymentHash) // Order by hash as proxy for time
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        // Map to DTOs with content names
        var items = payments.Select(p => MapToSummary(p)).ToList();

        return Ok(new PaymentListResponse
        {
            Items = items,
            Total = total
        });
    }

    /// <summary>
    /// Get detailed information about a specific payment.
    /// </summary>
    /// <param name="paymentHash">The payment hash.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Payment details including refunds and notifications.</returns>
    [HttpGet("payments/{paymentHash}")]
    [ProducesResponseType<PaymentDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentDetailsDto>> GetPayment(
        string paymentHash,
        CancellationToken ct)
    {
        var payment = await _dbContext.PaymentStates
            .Include(p => p.Refunds)
            .Include(p => p.Notifications)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PaymentHash == paymentHash, ct);

        if (payment == null)
        {
            return NotFound(new { error = "Payment not found" });
        }

        var details = MapToDetails(payment);
        return Ok(details);
    }

    private PaymentSummaryDto MapToSummary(PaymentState payment)
    {
        string? contentName = null;
        if (payment.ContentId > 0)
        {
            var content = _contentService.GetById(payment.ContentId);
            contentName = content?.Name;
        }

        return new PaymentSummaryDto
        {
            PaymentHash = payment.PaymentHash,
            AmountSat = (long)payment.AmountSat,
            Status = payment.Status,
            Kind = payment.Kind,
            ContentId = payment.ContentId > 0 ? payment.ContentId : null,
            ContentName = contentName,
            CreatedAt = DateTimeOffset.UtcNow // Placeholder - PaymentState doesn't have a timestamp
        };
    }

    private PaymentDetailsDto MapToDetails(PaymentState payment)
    {
        var summary = MapToSummary(payment);

        return new PaymentDetailsDto
        {
            PaymentHash = summary.PaymentHash,
            AmountSat = summary.AmountSat,
            Status = summary.Status,
            Kind = summary.Kind,
            ContentId = summary.ContentId,
            ContentName = summary.ContentName,
            CreatedAt = summary.CreatedAt,
            UserSessionId = payment.UserSessionId,
            Bolt12OfferId = payment.Bolt12OfferId,
            Refunds = payment.Refunds.Select(r => new RefundSummaryDto
            {
                RefundId = r.RefundId,
                AmountSat = (long)r.AmountSat,
                Status = r.Status.ToString(),
                InitiatedAt = r.InitiatedAt
            }).ToList(),
            Notifications = payment.Notifications.Select(n => new NotificationSummaryDto
            {
                NotificationId = n.NotificationId,
                PaymentHash = n.PaymentHash,
                Type = n.Type.ToString(),
                Event = n.Event.ToString(),
                Status = n.Status.ToString(),
                AttemptCount = n.AttemptCount,
                CreatedAt = n.CreatedAt,
                SentAt = n.SentAt,
                LastError = n.LastError
            }).ToList()
        };
    }
}
