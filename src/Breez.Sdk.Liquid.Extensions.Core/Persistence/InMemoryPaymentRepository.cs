using System.Collections.Concurrent;
using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Domain;

namespace Breez.Sdk.Liquid.Extensions.Core.Persistence;

/// <summary>
/// In-memory implementation of <see cref="IPaymentRepository"/> for development and testing.
/// </summary>
/// <remarks>
/// This implementation uses a <see cref="ConcurrentDictionary{TKey,TValue}"/> to store payment states
/// in memory. It is thread-safe and suitable for single-process scenarios, but does not persist data
/// across application restarts.
/// For production use, implement a persistent storage backend (e.g., SQL, NoSQL).
/// </remarks>
public class InMemoryPaymentRepository : IPaymentRepository
{
    private readonly ConcurrentDictionary<string, PaymentState> _payments = new();

    /// <inheritdoc />
    public Task<PaymentState?> GetByHashAsync(string paymentHash, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentHash);

        _payments.TryGetValue(paymentHash, out var payment);
        return Task.FromResult(payment);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PaymentState>> GetByStatusAsync(
        PaymentStatus status,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);

        var result = _payments.Values
            .Where(p => p.Status == status)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<PaymentState>>(result);
    }

    /// <inheritdoc />
    public Task<PaymentState> AddAsync(PaymentState payment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payment);

        if (!_payments.TryAdd(payment.PaymentHash, payment))
        {
            throw new InvalidOperationException(
                $"A payment with hash '{payment.PaymentHash}' already exists in the repository.");
        }

        return Task.FromResult(payment);
    }

    /// <inheritdoc />
    public Task<PaymentState> UpdateAsync(PaymentState payment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payment);

        if (!_payments.ContainsKey(payment.PaymentHash))
        {
            throw new InvalidOperationException(
                $"Payment with hash '{payment.PaymentHash}' does not exist in the repository.");
        }

        _payments[payment.PaymentHash] = payment;
        return Task.FromResult(payment);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string paymentHash, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentHash);

        return Task.FromResult(_payments.ContainsKey(paymentHash));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PaymentState>> GetAllAsync(
        int offset = 0,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(offset, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);

        var result = _payments.Values
            .OrderByDescending(p => p.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<PaymentState>>(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PaymentState>> GetByDateRangeAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        if (from > to)
        {
            throw new ArgumentException(
                $"The 'from' date ({from:O}) must be less than or equal to the 'to' date ({to:O}).",
                nameof(from));
        }

        var result = _payments.Values
            .Where(p => p.CreatedAt >= from && p.CreatedAt <= to)
            .OrderByDescending(p => p.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<PaymentState>>(result);
    }
}
