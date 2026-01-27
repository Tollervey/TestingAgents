using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Breez;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Exceptions;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Refund;

/// <summary>
/// Manages refund lifecycle: preparation with validation, initiation via Breez SDK, and querying.
/// </summary>
public class RefundService : IRefundService
{
    private readonly PaymentDbContext _context;
    private readonly IBreezSdkService _breezSdkService;
    private readonly ILogger<RefundService> _logger;

    public RefundService(
        PaymentDbContext context,
        IBreezSdkService breezSdkService,
        ILogger<RefundService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _breezSdkService = breezSdkService ?? throw new ArgumentNullException(nameof(breezSdkService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<PrepareRefundResult> PrepareRefundAsync(string originalPaymentHash, string destinationInvoice, CancellationToken ct = default)
    {
        // 1. Find the original payment
        var originalPayment = await _context.PaymentStates
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PaymentHash == originalPaymentHash, ct);

        if (originalPayment is null)
        {
            throw new PaymentNotFoundException(originalPaymentHash);
        }

        // 2. Check payment status is Paid
        if (originalPayment.Status != PaymentStatus.Paid)
        {
            return new PrepareRefundResult
            {
                OriginalAmountSat = originalPayment.AmountSat,
                RefundAmountSat = 0,
                FeeSat = 0,
                WalletBalanceSat = 0,
                CanProceed = false,
                ValidationError = $"Only paid payments can be refunded. Current status: {originalPayment.Status}"
            };
        }

        // 3. Parse the destination invoice
        global::Breez.Sdk.Liquid.LnInvoice parsedInvoice;
        try
        {
            parsedInvoice = await _breezSdkService.ParseInvoiceAsync(destinationInvoice, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse destination invoice for refund of payment {PaymentHash}", originalPaymentHash);
            throw new InvalidInvoiceException(destinationInvoice, "Failed to parse invoice");
        }

        // 4. Get existing refunds for this payment to calculate total already refunded
        var existingRefunds = await _context.RefundTransactions
            .AsNoTracking()
            .Where(r => r.OriginalPaymentHash == originalPaymentHash && r.Status == RefundStatus.Succeeded)
            .ToListAsync(ct);

        var totalAlreadyRefunded = existingRefunds.Aggregate(0UL, (sum, r) => sum + r.AmountSat);

        // 5. Calculate refund amount from the invoice (amountMsat / 1000 if present, else original payment amount)
        ulong refundAmount;
        if (parsedInvoice.amountMsat.HasValue && parsedInvoice.amountMsat.Value > 0)
        {
            refundAmount = parsedInvoice.amountMsat.Value / 1000;
        }
        else
        {
            // If invoice has no amount, use the remaining refundable amount
            refundAmount = originalPayment.AmountSat - totalAlreadyRefunded;
        }

        // 6. Validate refund amount + already refunded doesn't exceed original
        if (refundAmount + totalAlreadyRefunded > originalPayment.AmountSat)
        {
            throw new RefundExceedsOriginalException(refundAmount, originalPayment.AmountSat, totalAlreadyRefunded);
        }

        // 7. Get wallet balance
        var (balanceSat, _, _) = await _breezSdkService.GetWalletBalanceAsync(ct);

        // 8. Try to prepare send payment to get fee estimate
        global::Breez.Sdk.Liquid.PrepareSendResponse prepareResponse;
        try
        {
            prepareResponse = await _breezSdkService.PrepareSendPaymentAsync(destinationInvoice, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to prepare send payment for refund of payment {PaymentHash}", originalPaymentHash);
            return new PrepareRefundResult
            {
                OriginalAmountSat = originalPayment.AmountSat,
                RefundAmountSat = refundAmount,
                FeeSat = 0,
                WalletBalanceSat = balanceSat,
                CanProceed = false,
                ValidationError = "Failed to estimate fees for refund payment"
            };
        }

        var feeSat = GetRecordPropertyValue<ulong>(prepareResponse, "feesSat");

        // 9. Check if wallet balance >= refundAmount + fee
        var hasSufficientBalance = balanceSat >= (refundAmount + feeSat);

        var canProceed = hasSufficientBalance;
        string? validationError = null;

        if (!hasSufficientBalance)
        {
            validationError = $"Insufficient balance. Required: {refundAmount + feeSat} sats (amount: {refundAmount}, fee: {feeSat}), Available: {balanceSat} sats";
        }

        _logger.LogInformation(
            "Prepared refund for payment {PaymentHash}: Amount={RefundAmount} sats, Fee={Fee} sats, Balance={Balance} sats, CanProceed={CanProceed}",
            originalPaymentHash, refundAmount, feeSat, balanceSat, canProceed);

        return new PrepareRefundResult
        {
            OriginalAmountSat = originalPayment.AmountSat,
            RefundAmountSat = refundAmount,
            FeeSat = feeSat,
            WalletBalanceSat = balanceSat,
            CanProceed = canProceed,
            ValidationError = validationError
        };
    }

    /// <inheritdoc />
    public async Task<RefundTransaction> InitiateRefundAsync(string originalPaymentHash, string destinationInvoice, string initiatedByUserId, string? reason = null, CancellationToken ct = default)
    {
        // 1. First call PrepareRefundAsync to validate (reuse logic)
        var prepareResult = await PrepareRefundAsync(originalPaymentHash, destinationInvoice, ct);

        if (!prepareResult.CanProceed)
        {
            throw new InvalidOperationException($"Cannot initiate refund: {prepareResult.ValidationError}");
        }

        // 2. Create RefundTransaction entity with status Pending
        var refund = new RefundTransaction
        {
            RefundId = Guid.NewGuid(),
            OriginalPaymentHash = originalPaymentHash,
            AmountSat = prepareResult.RefundAmountSat,
            DestinationInvoice = destinationInvoice,
            Status = RefundStatus.Pending,
            Reason = reason,
            InitiatedByUserId = initiatedByUserId,
            InitiatedAt = DateTimeOffset.UtcNow
        };

        // 3. Save to DB
        _context.RefundTransactions.Add(refund);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Created refund {RefundId} for payment {PaymentHash} with amount {Amount} sats",
            refund.RefundId, originalPaymentHash, prepareResult.RefundAmountSat);

        // 4. Try to send payment via SDK
        try
        {
            var prepareResponse = await _breezSdkService.PrepareSendPaymentAsync(destinationInvoice, ct);
            var sendResponse = await _breezSdkService.SendPaymentAsync(prepareResponse, ct);

            // 5. If successful: update status to Succeeded
            refund.Status = RefundStatus.Succeeded;
            refund.CompletedAt = DateTimeOffset.UtcNow;
            refund.RefundPaymentHash = GetRecordPropertyValue<string>(GetRecordPropertyValue<object>(sendResponse, "payment"), "txId");

            _logger.LogInformation(
                "Refund {RefundId} succeeded with payment hash {PaymentHash}",
                refund.RefundId, refund.RefundPaymentHash);
        }
        catch (Exception ex)
        {
            // 6. If failed: update status to Failed
            refund.Status = RefundStatus.Failed;
            refund.ErrorMessage = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
            refund.CompletedAt = DateTimeOffset.UtcNow;

            _logger.LogError(ex, "Refund {RefundId} failed", refund.RefundId);
        }

        // 7. Save changes and return the refund
        await _context.SaveChangesAsync(ct);

        return refund;
    }

    /// <summary>
    /// Helper to get property value from SDK record types that may use lowercase naming.
    /// </summary>
    private static T GetRecordPropertyValue<T>(object record, string propertyName)
    {
        var property = record.GetType().GetProperty(propertyName)
            ?? record.GetType().GetProperty(char.ToUpperInvariant(propertyName[0]) + propertyName.Substring(1));
        return property != null ? (T)property.GetValue(record)! : default!;
    }

    /// <inheritdoc />
    public async Task<RefundTransaction?> GetRefundByIdAsync(Guid refundId, CancellationToken ct = default)
    {
        return await _context.RefundTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RefundId == refundId, ct);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<RefundTransaction> Items, int Total)> GetRefundsAsync(RefundStatus? status = null, int skip = 0, int take = 20, CancellationToken ct = default)
    {
        var query = _context.RefundTransactions.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(r => r.InitiatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }
}
