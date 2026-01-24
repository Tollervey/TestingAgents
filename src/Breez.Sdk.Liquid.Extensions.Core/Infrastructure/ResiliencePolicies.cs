using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace Breez.Sdk.Liquid.Extensions.Core.Infrastructure;

/// <summary>
/// Defines standard resilience policies for SDK operations using Polly v8 resilience pipelines.
/// </summary>
/// <remarks>
/// These policies provide retry, timeout, and backoff strategies for different operation types:
/// <list type="bullet">
/// <item><description><see cref="ConnectPolicy"/>: For SDK initialization with exponential backoff and jitter</description></item>
/// <item><description><see cref="PaymentOperationPolicy"/>: For invoice creation and payment operations with linear backoff</description></item>
/// <item><description><see cref="QueryPolicy"/>: For read operations with minimal retry</description></item>
/// <item><description><see cref="WebhookPolicy"/>: For webhook delivery with exponential backoff</description></item>
/// </list>
/// All policies are thread-safe and reusable.
/// </remarks>
public static class ResiliencePolicies
{
    /// <summary>
    /// Gets the connection policy for SDK initialization operations.
    /// </summary>
    /// <remarks>
    /// Configuration:
    /// <list type="bullet">
    /// <item><description>Max Retry Attempts: 3</description></item>
    /// <item><description>Initial Delay: 2 seconds</description></item>
    /// <item><description>Backoff Type: Exponential with jitter</description></item>
    /// <item><description>Timeout: 30 seconds</description></item>
    /// </list>
    /// Use this policy for operations like <c>BreezServices.Connect()</c> that may fail due to network issues
    /// or temporary service unavailability. The exponential backoff with jitter prevents thundering herd problems
    /// when multiple clients reconnect simultaneously.
    /// </remarks>
    public static ResiliencePipeline ConnectPolicy { get; } = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        })
        .AddTimeout(TimeSpan.FromSeconds(30))
        .Build();

    /// <summary>
    /// Gets the payment operation policy for invoice creation and payment operations.
    /// </summary>
    /// <remarks>
    /// Configuration:
    /// <list type="bullet">
    /// <item><description>Max Retry Attempts: 2</description></item>
    /// <item><description>Delay: 2 seconds (linear backoff)</description></item>
    /// <item><description>Backoff Type: Linear (constant delay)</description></item>
    /// <item><description>Timeout: 15 seconds</description></item>
    /// </list>
    /// Use this policy for operations like <c>PrepareReceivePayment</c>, <c>PrepareSendPayment</c>,
    /// and <c>ReceivePayment</c> that may experience transient failures. The linear backoff provides
    /// predictable retry timing for payment operations.
    /// </remarks>
    public static ResiliencePipeline PaymentOperationPolicy { get; } = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 2,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Constant,
            UseJitter = false
        })
        .AddTimeout(TimeSpan.FromSeconds(15))
        .Build();

    /// <summary>
    /// Gets the query policy for read-only operations.
    /// </summary>
    /// <remarks>
    /// Configuration:
    /// <list type="bullet">
    /// <item><description>Max Retry Attempts: 1</description></item>
    /// <item><description>Delay: None (immediate retry)</description></item>
    /// <item><description>Backoff Type: None</description></item>
    /// <item><description>Timeout: 10 seconds</description></item>
    /// </list>
    /// Use this policy for operations like <c>ListPayments</c>, <c>GetInfo</c>, and other read operations
    /// that are expected to be fast and have minimal transient failures. The single retry attempt provides
    /// basic resilience without adding significant latency.
    /// </remarks>
    public static ResiliencePipeline QueryPolicy { get; } = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 1,
            Delay = TimeSpan.Zero,
            BackoffType = DelayBackoffType.Constant,
            UseJitter = false
        })
        .AddTimeout(TimeSpan.FromSeconds(10))
        .Build();

    /// <summary>
    /// Gets the webhook policy for webhook delivery operations.
    /// </summary>
    /// <remarks>
    /// Configuration:
    /// <list type="bullet">
    /// <item><description>Max Retry Attempts: 3</description></item>
    /// <item><description>Initial Delay: 2 seconds</description></item>
    /// <item><description>Backoff Type: Exponential</description></item>
    /// <item><description>Timeout: 30 seconds</description></item>
    /// </list>
    /// Use this policy for webhook delivery to external endpoints that may be temporarily unavailable
    /// or experiencing high load. The exponential backoff gives the receiving endpoint time to recover
    /// before subsequent retry attempts.
    /// </remarks>
    public static ResiliencePipeline WebhookPolicy { get; } = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = false
        })
        .AddTimeout(TimeSpan.FromSeconds(30))
        .Build();

    /// <summary>
    /// Creates a typed connection policy for SDK initialization operations that return a specific result type.
    /// </summary>
    /// <typeparam name="T">The result type returned by the operation.</typeparam>
    /// <returns>A typed resilience pipeline with the same configuration as <see cref="ConnectPolicy"/>.</returns>
    /// <remarks>
    /// This is a generic version of <see cref="ConnectPolicy"/> that can be used with operations
    /// returning specific types. The configuration is identical to the non-generic version.
    /// </remarks>
    public static ResiliencePipeline<T> CreateConnectPolicy<T>()
    {
        return new ResiliencePipelineBuilder<T>()
            .AddRetry(new RetryStrategyOptions<T>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true
            })
            .AddTimeout(TimeSpan.FromSeconds(30))
            .Build();
    }

    /// <summary>
    /// Creates a typed payment operation policy for operations that return a specific result type.
    /// </summary>
    /// <typeparam name="T">The result type returned by the operation.</typeparam>
    /// <returns>A typed resilience pipeline with the same configuration as <see cref="PaymentOperationPolicy"/>.</returns>
    /// <remarks>
    /// This is a generic version of <see cref="PaymentOperationPolicy"/> that can be used with operations
    /// returning specific types. The configuration is identical to the non-generic version.
    /// </remarks>
    public static ResiliencePipeline<T> CreatePaymentOperationPolicy<T>()
    {
        return new ResiliencePipelineBuilder<T>()
            .AddRetry(new RetryStrategyOptions<T>
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Constant,
                UseJitter = false
            })
            .AddTimeout(TimeSpan.FromSeconds(15))
            .Build();
    }

    /// <summary>
    /// Creates a typed query policy for read-only operations that return a specific result type.
    /// </summary>
    /// <typeparam name="T">The result type returned by the operation.</typeparam>
    /// <returns>A typed resilience pipeline with the same configuration as <see cref="QueryPolicy"/>.</returns>
    /// <remarks>
    /// This is a generic version of <see cref="QueryPolicy"/> that can be used with operations
    /// returning specific types. The configuration is identical to the non-generic version.
    /// </remarks>
    public static ResiliencePipeline<T> CreateQueryPolicy<T>()
    {
        return new ResiliencePipelineBuilder<T>()
            .AddRetry(new RetryStrategyOptions<T>
            {
                MaxRetryAttempts = 1,
                Delay = TimeSpan.Zero,
                BackoffType = DelayBackoffType.Constant,
                UseJitter = false
            })
            .AddTimeout(TimeSpan.FromSeconds(10))
            .Build();
    }

    /// <summary>
    /// Creates a typed webhook policy for webhook delivery operations that return a specific result type.
    /// </summary>
    /// <typeparam name="T">The result type returned by the operation.</typeparam>
    /// <returns>A typed resilience pipeline with the same configuration as <see cref="WebhookPolicy"/>.</returns>
    /// <remarks>
    /// This is a generic version of <see cref="WebhookPolicy"/> that can be used with operations
    /// returning specific types. The configuration is identical to the non-generic version.
    /// </remarks>
    public static ResiliencePipeline<T> CreateWebhookPolicy<T>()
    {
        return new ResiliencePipelineBuilder<T>()
            .AddRetry(new RetryStrategyOptions<T>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = false
            })
            .AddTimeout(TimeSpan.FromSeconds(30))
            .Build();
    }
}
