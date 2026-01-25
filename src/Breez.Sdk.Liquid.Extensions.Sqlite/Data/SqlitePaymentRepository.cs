using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Breez.Sdk.Liquid.Extensions.Sqlite.Data;

/// <summary>
/// SQLite implementation of the payment repository interface.
/// </summary>
public class SqlitePaymentRepository : IPaymentRepository
{
    private readonly SqlitePaymentDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlitePaymentRepository"/> class.
    /// </summary>
    /// <param name="context">The SQLite database context.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    public SqlitePaymentRepository(SqlitePaymentDbContext context)
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
            .FirstOrDefaultAsync(p => p.PaymentHash == paymentHash, cancellationToken)
            .ConfigureAwait(false);
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
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PaymentState> AddAsync(PaymentState payment, CancellationToken cancellationToken = default)
    {
        if (payment == null)
        {
            throw new ArgumentNullException(nameof(payment));
        }

        // Check if payment with same hash already exists
        var exists = await ExistsAsync(payment.PaymentHash, cancellationToken).ConfigureAwait(false);
        if (exists)
        {
            throw new InvalidOperationException($"Payment with hash '{payment.PaymentHash}' already exists.");
        }

        await _context.PaymentStates.AddAsync(payment, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return payment;
    }

    /// <inheritdoc />
    public async Task<PaymentState> UpdateAsync(PaymentState payment, CancellationToken cancellationToken = default)
    {
        if (payment == null)
        {
            throw new ArgumentNullException(nameof(payment));
        }

        // Verify payment exists
        var exists = await _context.PaymentStates
            .AnyAsync(p => p.Id == payment.Id, cancellationToken)
            .ConfigureAwait(false);

        if (!exists)
        {
            throw new InvalidOperationException($"Payment with ID '{payment.Id}' does not exist.");
        }

        _context.PaymentStates.Update(payment);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return payment;
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
            .AnyAsync(p => p.PaymentHash == paymentHash, cancellationToken)
            .ConfigureAwait(false);
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
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaymentState>> GetByDateRangeAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        if (from > to)
        {
            throw new ArgumentException("'from' date must be less than or equal to 'to' date.");
        }

        return await _context.PaymentStates
            .AsNoTracking()
            .Where(p => p.CreatedAt >= from && p.CreatedAt <= to)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
