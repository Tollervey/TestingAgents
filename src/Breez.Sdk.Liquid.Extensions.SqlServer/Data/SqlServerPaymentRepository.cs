using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Breez.Sdk.Liquid.Extensions.SqlServer.Data;

/// <summary>
/// SQL Server implementation of <see cref="IPaymentRepository"/>.
/// Provides asynchronous data access for payment state persistence using Entity Framework Core.
/// </summary>
public class SqlServerPaymentRepository : IPaymentRepository
{
    private readonly SqlServerPaymentDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerPaymentRepository"/> class.
    /// </summary>
    /// <param name="context">The database context for SQL Server operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    public SqlServerPaymentRepository(SqlServerPaymentDbContext context)
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

        // Check if payment with same hash already exists
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
        var exists = await _context.PaymentStates
            .AnyAsync(p => p.Id == payment.Id, cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException($"Payment with ID '{payment.Id}' does not exist.");
        }

        _context.PaymentStates.Update(payment);
        await _context.SaveChangesAsync(cancellationToken);

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
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset must be non-negative.");
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
            throw new ArgumentException("'from' date must be less than or equal to 'to' date.", nameof(from));
        }

        return await _context.PaymentStates
            .AsNoTracking()
            .Where(p => p.CreatedAt >= from && p.CreatedAt <= to)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
