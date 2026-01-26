using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Breez.Sdk.Liquid.Extensions.PostgreSql.Data;

/// <summary>
/// PostgreSQL implementation of the payment repository using Entity Framework Core.
/// </summary>
internal class PostgreSqlPaymentRepository : IPaymentRepository
{
    private readonly PostgreSqlPaymentDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlPaymentRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public PostgreSqlPaymentRepository(PostgreSqlPaymentDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<PaymentState?> GetByHashAsync(string paymentHash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(paymentHash))
        {
            throw new ArgumentNullException(nameof(paymentHash));
        }

        return await _context.PaymentStates
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PaymentHash == paymentHash, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaymentState>> GetByStatusAsync(
        PaymentStatus status,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be at least 1.");
        }

        return await _context.PaymentStates
            .AsNoTracking()
            .Where(p => p.Status == status)
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PaymentState> AddAsync(PaymentState payment, CancellationToken cancellationToken = default)
    {
        if (payment == null)
        {
            throw new ArgumentNullException(nameof(payment));
        }

        // Check for duplicate payment hash
        var exists = await ExistsAsync(payment.PaymentHash, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Payment with hash '{payment.PaymentHash}' already exists.");
        }

        await _context.PaymentStates.AddAsync(payment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return payment;
    }

    /// <inheritdoc />
    public async Task<PaymentState> UpdateAsync(PaymentState payment, CancellationToken cancellationToken = default)
    {
        if (payment == null)
        {
            throw new ArgumentNullException(nameof(payment));
        }

        // Check if payment exists
        var existingPayment = await _context.PaymentStates
            .FirstOrDefaultAsync(p => p.Id == payment.Id, cancellationToken);

        if (existingPayment == null)
        {
            throw new InvalidOperationException($"Payment with ID '{payment.Id}' does not exist.");
        }

        // Update the tracked entity
        _context.Entry(existingPayment).CurrentValues.SetValues(payment);

        // Manually update complex properties that SetValues doesn't handle
        existingPayment.Status = payment.Status;
        existingPayment.ConfirmedAt = payment.ConfirmedAt;
        existingPayment.Preimage = payment.Preimage;
        existingPayment.FeeSat = payment.FeeSat;

        await _context.SaveChangesAsync(cancellationToken);

        return existingPayment;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string paymentHash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(paymentHash))
        {
            throw new ArgumentNullException(nameof(paymentHash));
        }

        return await _context.PaymentStates
            .AsNoTracking()
            .AnyAsync(p => p.PaymentHash == paymentHash, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaymentState>> GetAllAsync(
        int offset = 0,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset must be at least 0.");
        }

        if (limit < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be at least 1.");
        }

        return await _context.PaymentStates
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaymentState>> GetByDateRangeAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        if (from > to)
        {
            throw new ArgumentException("From date must be less than or equal to To date.", nameof(from));
        }

        return await _context.PaymentStates
            .AsNoTracking()
            .Where(p => p.CreatedAt >= from && p.CreatedAt <= to)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
