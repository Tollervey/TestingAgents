using Breez.Sdk.Liquid.Extensions.Core.Domain;

namespace Breez.Sdk.Liquid.Extensions.Core.Abstractions;

/// <summary>
/// Repository interface for payment state persistence.
/// Provides asynchronous methods for CRUD operations and querying payment records.
/// </summary>
public interface IPaymentRepository
{
    /// <summary>
    /// Gets a payment by its hash.
    /// </summary>
    /// <param name="paymentHash">The payment hash (hex-encoded) that uniquely identifies the payment.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The payment state, or null if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="paymentHash"/> is null or whitespace.</exception>
    Task<PaymentState?> GetByHashAsync(string paymentHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payments by status with pagination.
    /// </summary>
    /// <param name="status">The payment status to filter by.</param>
    /// <param name="limit">Maximum number of records to return. Default is 100.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A read-only list of payment states matching the specified status.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="limit"/> is less than 1.</exception>
    Task<IReadOnlyList<PaymentState>> GetByStatusAsync(
        PaymentStatus status,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new payment record to the repository.
    /// </summary>
    /// <param name="payment">The payment state to add.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The added payment with any generated values (e.g., database-generated IDs).</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="payment"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a payment with the same hash already exists.</exception>
    Task<PaymentState> AddAsync(PaymentState payment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing payment record.
    /// </summary>
    /// <param name="payment">The payment state to update.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The updated payment state.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="payment"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the payment does not exist in the repository.</exception>
    Task<PaymentState> UpdateAsync(PaymentState payment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a payment with the given hash exists in the repository.
    /// </summary>
    /// <param name="paymentHash">The payment hash (hex-encoded) to check.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if a payment with the specified hash exists; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="paymentHash"/> is null or whitespace.</exception>
    Task<bool> ExistsAsync(string paymentHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated payment history ordered by creation date (newest first).
    /// </summary>
    /// <param name="offset">Number of records to skip. Default is 0.</param>
    /// <param name="limit">Maximum number of records to return. Default is 50.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A read-only list of payment states in descending order by creation date.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="offset"/> is less than 0 or <paramref name="limit"/> is less than 1.</exception>
    Task<IReadOnlyList<PaymentState>> GetAllAsync(
        int offset = 0,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payments within a date range ordered by creation date (newest first).
    /// </summary>
    /// <param name="from">Start date (inclusive).</param>
    /// <param name="to">End date (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A read-only list of payment states created within the specified date range.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="from"/> is greater than <paramref name="to"/>.</exception>
    Task<IReadOnlyList<PaymentState>> GetByDateRangeAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);
}
