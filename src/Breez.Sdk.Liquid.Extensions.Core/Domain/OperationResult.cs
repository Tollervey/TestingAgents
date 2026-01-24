namespace Breez.Sdk.Liquid.Extensions.Core.Domain;

/// <summary>
/// Represents the result of an SDK operation.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public sealed class OperationResult<T>
{
    private OperationResult(T? value, OperationError? error, bool isSuccess)
    {
        Value = value;
        Error = error;
        IsSuccess = isSuccess;
    }

    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// The success value (if successful).
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// The error details (if failed).
    /// </summary>
    public OperationError? Error { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static OperationResult<T> Success(T value) => new(value, null, true);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static OperationResult<T> Failure(OperationError error) => new(default, error, false);

    /// <summary>
    /// Creates a failed result from an exception.
    /// </summary>
    public static OperationResult<T> Failure(Exception ex, bool isRetryable = false)
        => new(default, OperationError.FromException(ex, isRetryable), false);
}

/// <summary>
/// Describes an operation error.
/// </summary>
public sealed record OperationError
{
    /// <summary>
    /// The error code categorizing the failure.
    /// </summary>
    public required BreezErrorCode Code { get; init; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Whether the operation can be retried.
    /// </summary>
    public bool IsRetryable { get; init; }

    /// <summary>
    /// The underlying exception (if applicable).
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Creates an OperationError from an exception.
    /// </summary>
    /// <param name="ex">The exception to convert.</param>
    /// <param name="isRetryable">Whether the operation can be retried.</param>
    /// <returns>An OperationError instance.</returns>
    public static OperationError FromException(Exception ex, bool isRetryable = false)
        => new()
        {
            Code = MapExceptionToCode(ex),
            Message = ex.Message,
            IsRetryable = isRetryable,
            Exception = ex
        };

    private static BreezErrorCode MapExceptionToCode(Exception ex) => ex switch
    {
        // Exception mapping will be updated after custom exception classes are created (T029-T033)
        // For now, use ServiceUnavailable as fallback
        _ => BreezErrorCode.ServiceUnavailable
    };
}
