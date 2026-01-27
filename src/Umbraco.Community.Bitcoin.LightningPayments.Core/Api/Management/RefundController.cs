using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Base;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Management.Dto;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Exceptions;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Refund;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Management;

/// <summary>
/// Management API controller for refund operations (prepare, initiate, query).
/// </summary>
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Lightning Payments Refunds")]
[Authorize(Policy = Umbraco.Cms.Web.Common.Authorization.AuthorizationPolicies.SectionAccessSettings)]
public class RefundController : OurUmbracoBitcoinLightningPaymentsApiControllerBase
{
    private readonly IRefundService _refundService;

    public RefundController(IRefundService refundService)
    {
        _refundService = refundService ?? throw new ArgumentNullException(nameof(refundService));
    }

    /// <summary>
    /// List refunds with optional status filter and pagination.
    /// </summary>
    [HttpGet("refunds")]
    [ProducesResponseType<RefundListResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<RefundListResponse>> ListRefunds(
        [FromQuery] RefundStatus? status = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var (items, total) = await _refundService.GetRefundsAsync(status, skip, take, ct);

        return Ok(new RefundListResponse
        {
            Items = items.Select(MapToResponse).ToList(),
            Total = total
        });
    }

    /// <summary>
    /// Initiate a refund for a confirmed payment.
    /// </summary>
    [HttpPost("refunds")]
    [ProducesResponseType<RefundTransactionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RefundTransactionResponse>> InitiateRefund(
        [FromBody] InitiateRefundRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var userId = User.Identity?.Name ?? "unknown";
            var refund = await _refundService.InitiateRefundAsync(
                request.OriginalPaymentHash,
                request.DestinationInvoice,
                userId,
                request.Reason,
                ct);

            var response = MapToResponse(refund);
            return CreatedAtAction(nameof(GetRefund), new { refundId = refund.RefundId }, response);
        }
        catch (PaymentNotFoundException)
        {
            return NotFound();
        }
        catch (RefundExceedsOriginalException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Refund Exceeds Original Amount",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InsufficientBalanceException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Insufficient Balance",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InvalidInvoiceException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Invoice",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Get details of a specific refund transaction.
    /// </summary>
    [HttpGet("refunds/{refundId:guid}")]
    [ProducesResponseType<RefundTransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RefundTransactionResponse>> GetRefund(
        Guid refundId,
        CancellationToken ct = default)
    {
        var refund = await _refundService.GetRefundByIdAsync(refundId, ct);
        if (refund is null)
        {
            return NotFound();
        }

        return Ok(MapToResponse(refund));
    }

    /// <summary>
    /// Prepare a refund (validate payment, check balance, estimate fees).
    /// </summary>
    [HttpPost("refunds/prepare")]
    [ProducesResponseType<PrepareRefundResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrepareRefundResponse>> PrepareRefund(
        [FromBody] PrepareRefundRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _refundService.PrepareRefundAsync(
                request.OriginalPaymentHash,
                request.DestinationInvoice,
                ct);

            return Ok(new PrepareRefundResponse
            {
                OriginalAmountSat = (long)result.OriginalAmountSat,
                RefundAmountSat = (long)result.RefundAmountSat,
                FeeSat = (long)result.FeeSat,
                WalletBalanceSat = (long)result.WalletBalanceSat,
                CanProceed = result.CanProceed,
                ValidationError = result.ValidationError
            });
        }
        catch (PaymentNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidInvoiceException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Invoice",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    private static RefundTransactionResponse MapToResponse(RefundTransaction refund) => new()
    {
        RefundId = refund.RefundId,
        OriginalPaymentHash = refund.OriginalPaymentHash,
        AmountSat = (long)refund.AmountSat,
        DestinationInvoice = refund.DestinationInvoice,
        Status = refund.Status.ToString().ToLowerInvariant(),
        ErrorMessage = refund.ErrorMessage,
        Reason = refund.Reason,
        InitiatedByUserId = refund.InitiatedByUserId,
        InitiatedAt = refund.InitiatedAt,
        CompletedAt = refund.CompletedAt,
        RefundPaymentHash = refund.RefundPaymentHash
    };
}
