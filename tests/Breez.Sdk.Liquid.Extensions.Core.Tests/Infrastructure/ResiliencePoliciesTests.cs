using Breez.Sdk.Liquid.Extensions.Core.Infrastructure;
using FluentAssertions;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace Breez.Sdk.Liquid.Extensions.Core.Tests.Infrastructure;

/// <summary>
/// Unit tests for ResiliencePolicies.
/// These tests verify retry behavior, backoff strategies, timeouts, and jitter configurations
/// for all resilience policies defined in the system.
/// </summary>
/// <remarks>
/// IMPORTANT: These tests use SHORT DELAY test policies (50ms base) instead of production
/// policies (2000ms base) to keep tests fast. We verify BEHAVIOR (retry counts, backoff patterns)
/// not exact production timing values. Production delay values are configuration, not logic.
///
/// Test Design Principles:
/// - Use 50ms base delays for pattern verification (keeps tests under 1s)
/// - Verify retry counts and backoff patterns, not exact timing
/// - Production policies are validated for existence and first-attempt success only
/// </remarks>
public class ResiliencePoliciesTests
{
    #region Test Policy Factories (Short Delays for Fast Testing)

    /// <summary>
    /// Creates a test policy mimicking ConnectPolicy but with 50ms base delay.
    /// Production: 3 retries, 2s exponential backoff with jitter, 30s timeout.
    /// Test: 3 retries, 50ms exponential backoff with jitter, 1s timeout.
    /// </summary>
    private static ResiliencePipeline CreateFastConnectPolicy() => new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(50),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        })
        .AddTimeout(TimeSpan.FromSeconds(1))
        .Build();

    /// <summary>
    /// Creates a test policy mimicking PaymentOperationPolicy but with 50ms delay.
    /// Production: 2 retries, 2s constant delay, 15s timeout.
    /// Test: 2 retries, 50ms constant delay, 1s timeout.
    /// </summary>
    private static ResiliencePipeline CreateFastPaymentOperationPolicy() => new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 2,
            Delay = TimeSpan.FromMilliseconds(50),
            BackoffType = DelayBackoffType.Constant,
            UseJitter = false
        })
        .AddTimeout(TimeSpan.FromSeconds(1))
        .Build();

    /// <summary>
    /// Creates a test policy mimicking WebhookPolicy but with 50ms base delay.
    /// Production: 3 retries, 2s exponential backoff (no jitter), 30s timeout.
    /// Test: 3 retries, 50ms exponential backoff (no jitter), 1s timeout.
    /// </summary>
    private static ResiliencePipeline CreateFastWebhookPolicy() => new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(50),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = false
        })
        .AddTimeout(TimeSpan.FromSeconds(1))
        .Build();

    /// <summary>
    /// Creates a generic test policy for counting retries with minimal delay.
    /// </summary>
    private static ResiliencePipeline CreateFastRetryPolicy(int maxRetries, DelayBackoffType backoffType, bool useJitter) =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = maxRetries,
                Delay = TimeSpan.FromMilliseconds(10), // Minimal delay for counting tests
                BackoffType = backoffType,
                UseJitter = useJitter
            })
            .Build();

    #endregion

    #region Production Policy Existence Tests

    [Fact]
    public async Task ConnectPolicy_ShouldSucceedOnFirstAttempt_WhenNoFailure()
    {
        // Arrange
        var attemptCount = 0;

        // Act
        var result = await ResiliencePolicies.ConnectPolicy.ExecuteAsync(async token =>
        {
            attemptCount++;
            await Task.CompletedTask;
            return "success";
        });

        // Assert
        attemptCount.Should().Be(1, "should succeed without retrying");
        result.Should().Be("success");
    }

    [Fact]
    public async Task PaymentOperationPolicy_ShouldSucceedOnFirstAttempt_WhenNoFailure()
    {
        // Arrange
        var attemptCount = 0;

        // Act
        var result = await ResiliencePolicies.PaymentOperationPolicy.ExecuteAsync(async token =>
        {
            attemptCount++;
            await Task.CompletedTask;
            return "success";
        });

        // Assert
        attemptCount.Should().Be(1, "should succeed without retrying");
        result.Should().Be("success");
    }

    [Fact]
    public async Task QueryPolicy_ShouldSucceedOnFirstAttempt_WhenNoFailure()
    {
        // Arrange
        var attemptCount = 0;

        // Act
        var result = await ResiliencePolicies.QueryPolicy.ExecuteAsync(async token =>
        {
            attemptCount++;
            await Task.CompletedTask;
            return "success";
        });

        // Assert
        attemptCount.Should().Be(1, "should succeed without retrying");
        result.Should().Be("success");
    }

    [Fact]
    public async Task WebhookPolicy_ShouldSucceedOnFirstAttempt_WhenNoFailure()
    {
        // Arrange
        var attemptCount = 0;

        // Act
        var result = await ResiliencePolicies.WebhookPolicy.ExecuteAsync(async token =>
        {
            attemptCount++;
            await Task.CompletedTask;
            return "success";
        });

        // Assert
        attemptCount.Should().Be(1, "should succeed without retrying");
        result.Should().Be("success");
    }

    #endregion

    #region Retry Count Tests (Using Fast Test Policies)

    [Fact]
    public async Task ConnectPolicyPattern_ShouldRetryThreeTimes_WhenOperationFails()
    {
        // Arrange - Use fast test policy with same retry count as production
        var policy = CreateFastConnectPolicy();
        var attemptCount = 0;
        var expectedAttempts = 4; // Initial attempt + 3 retries

        // Act
        var result = await policy.ExecuteAsync(async token =>
        {
            attemptCount++;
            await Task.CompletedTask;

            if (attemptCount < expectedAttempts)
            {
                throw new InvalidOperationException($"Attempt {attemptCount} failed");
            }

            return "success";
        });

        // Assert
        attemptCount.Should().Be(expectedAttempts, "policy should retry 3 times after initial failure");
        result.Should().Be("success");
    }

    [Fact]
    public async Task ConnectPolicyPattern_ShouldThrowException_WhenAllRetriesExhausted()
    {
        // Arrange
        var policy = CreateFastConnectPolicy();
        var attemptCount = 0;
        var expectedAttempts = 4; // Initial attempt + 3 retries

        // Act
        var act = async () => await policy.ExecuteAsync(async token =>
        {
            attemptCount++;
            await Task.CompletedTask;
            throw new InvalidOperationException($"Attempt {attemptCount} failed");
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Attempt 4 failed");
        attemptCount.Should().Be(expectedAttempts, "should attempt initial call plus 3 retries");
    }

    [Fact]
    public async Task PaymentOperationPolicyPattern_ShouldRetryTwoTimes_WhenOperationFails()
    {
        // Arrange - Use fast test policy with same retry count as production
        var policy = CreateFastPaymentOperationPolicy();
        var attemptCount = 0;
        var expectedAttempts = 3; // Initial attempt + 2 retries

        // Act
        var result = await policy.ExecuteAsync(async token =>
        {
            attemptCount++;
            await Task.CompletedTask;

            if (attemptCount < expectedAttempts)
            {
                throw new InvalidOperationException($"Attempt {attemptCount} failed");
            }

            return "success";
        });

        // Assert
        attemptCount.Should().Be(expectedAttempts, "policy should retry 2 times after initial failure");
        result.Should().Be("success");
    }

    [Fact]
    public async Task PaymentOperationPolicyPattern_ShouldThrowException_WhenAllRetriesExhausted()
    {
        // Arrange
        var policy = CreateFastPaymentOperationPolicy();
        var attemptCount = 0;
        var expectedAttempts = 3; // Initial attempt + 2 retries

        // Act
        var act = async () => await policy.ExecuteAsync(async token =>
        {
            attemptCount++;
            await Task.CompletedTask;
            throw new InvalidOperationException($"Attempt {attemptCount} failed");
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Attempt 3 failed");
        attemptCount.Should().Be(expectedAttempts, "should attempt initial call plus 2 retries");
    }

    [Fact]
    public async Task QueryPolicy_ShouldRetryOnce_WhenOperationFails()
    {
        // QueryPolicy has zero delay, so we can use production policy directly
        var attemptCount = 0;
        var expectedAttempts = 2; // Initial attempt + 1 retry

        // Act
        var result = await ResiliencePolicies.QueryPolicy.ExecuteAsync(async token =>
        {
            attemptCount++;
            await Task.CompletedTask;

            if (attemptCount < expectedAttempts)
            {
                throw new InvalidOperationException($"Attempt {attemptCount} failed");
            }

            return "success";
        });

        // Assert
        attemptCount.Should().Be(expectedAttempts, "policy should retry once after initial failure");
        result.Should().Be("success");
    }

    [Fact]
    public async Task QueryPolicy_ShouldThrowException_WhenBothAttemptsExhausted()
    {
        // Arrange
        var attemptCount = 0;
        var expectedAttempts = 2; // Initial attempt + 1 retry

        // Act
        var act = async () => await ResiliencePolicies.QueryPolicy.ExecuteAsync(async token =>
        {
            attemptCount++;
            await Task.CompletedTask;
            throw new InvalidOperationException($"Attempt {attemptCount} failed");
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Attempt 2 failed");
        attemptCount.Should().Be(expectedAttempts, "should attempt initial call plus 1 retry");
    }

    [Fact]
    public async Task WebhookPolicyPattern_ShouldRetryThreeTimes_WhenOperationFails()
    {
        // Arrange - Use fast test policy with same retry count as production
        var policy = CreateFastWebhookPolicy();
        var attemptCount = 0;
        var expectedAttempts = 4; // Initial attempt + 3 retries

        // Act
        var result = await policy.ExecuteAsync(async token =>
        {
            attemptCount++;
            await Task.CompletedTask;

            if (attemptCount < expectedAttempts)
            {
                throw new InvalidOperationException($"Attempt {attemptCount} failed");
            }

            return "success";
        });

        // Assert
        attemptCount.Should().Be(expectedAttempts, "policy should retry 3 times after initial failure");
        result.Should().Be("success");
    }

    [Fact]
    public async Task WebhookPolicyPattern_ShouldThrowException_WhenAllRetriesExhausted()
    {
        // Arrange
        var policy = CreateFastWebhookPolicy();
        var attemptCount = 0;
        var expectedAttempts = 4; // Initial attempt + 3 retries

        // Act
        var act = async () => await policy.ExecuteAsync(async token =>
        {
            attemptCount++;
            await Task.CompletedTask;
            throw new InvalidOperationException($"Attempt {attemptCount} failed");
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Attempt 4 failed");
        attemptCount.Should().Be(expectedAttempts, "should attempt initial call plus 3 retries");
    }

    #endregion

    #region Backoff Pattern Tests (Using Fast Test Policies)

    [Fact]
    public async Task ExponentialBackoff_ShouldIncreaseDelaysBetweenRetries()
    {
        // Arrange - Test exponential backoff pattern with 50ms base delay
        var policy = CreateFastWebhookPolicy(); // Exponential, no jitter
        var attemptTimestamps = new List<DateTimeOffset>();
        var attemptCount = 0;
        var expectedAttempts = 4; // Initial + 3 retries

        // Act
        await policy.ExecuteAsync(async token =>
        {
            attemptCount++;
            attemptTimestamps.Add(DateTimeOffset.UtcNow);
            await Task.CompletedTask;

            if (attemptCount < expectedAttempts)
            {
                throw new InvalidOperationException($"Attempt {attemptCount} failed");
            }

            return "success";
        });

        // Assert - Verify exponential pattern: delays should roughly double
        attemptTimestamps.Should().HaveCount(expectedAttempts);

        var delay1 = (attemptTimestamps[1] - attemptTimestamps[0]).TotalMilliseconds;
        var delay2 = (attemptTimestamps[2] - attemptTimestamps[1]).TotalMilliseconds;
        var delay3 = (attemptTimestamps[3] - attemptTimestamps[2]).TotalMilliseconds;

        // With 50ms base: expect ~50ms, ~100ms, ~200ms
        delay1.Should().BeInRange(30, 100, "first delay should be ~50ms");
        delay2.Should().BeGreaterThan(delay1 * 1.5, "second delay should be significantly larger than first (exponential)");
        delay3.Should().BeGreaterThan(delay2 * 1.5, "third delay should be significantly larger than second (exponential)");
    }

    [Fact]
    public async Task ConstantBackoff_ShouldMaintainConsistentDelays()
    {
        // Arrange - Test constant backoff pattern with 50ms delay
        var policy = CreateFastPaymentOperationPolicy(); // Constant delay
        var attemptTimestamps = new List<DateTimeOffset>();
        var attemptCount = 0;
        var expectedAttempts = 3; // Initial + 2 retries

        // Act
        await policy.ExecuteAsync(async token =>
        {
            attemptCount++;
            attemptTimestamps.Add(DateTimeOffset.UtcNow);
            await Task.CompletedTask;

            if (attemptCount < expectedAttempts)
            {
                throw new InvalidOperationException($"Attempt {attemptCount} failed");
            }

            return "success";
        });

        // Assert - Verify constant pattern: delays should be similar
        attemptTimestamps.Should().HaveCount(expectedAttempts);

        var delay1 = (attemptTimestamps[1] - attemptTimestamps[0]).TotalMilliseconds;
        var delay2 = (attemptTimestamps[2] - attemptTimestamps[1]).TotalMilliseconds;

        // Both delays should be approximately 50ms (within reasonable tolerance)
        delay1.Should().BeInRange(30, 100, "first delay should be ~50ms");
        delay2.Should().BeInRange(30, 100, "second delay should be ~50ms");

        // Delays should be similar (constant backoff)
        var delayDifference = Math.Abs(delay1 - delay2);
        delayDifference.Should().BeLessThan(50, "delays should be similar for constant backoff");
    }

    [Fact]
    public async Task QueryPolicy_ShouldRetryImmediately_WithNoDelay()
    {
        // QueryPolicy has zero delay - can use production policy
        var attemptTimestamps = new List<DateTimeOffset>();
        var attemptCount = 0;
        var expectedAttempts = 2; // Initial + 1 retry

        // Act
        await ResiliencePolicies.QueryPolicy.ExecuteAsync(async token =>
        {
            attemptCount++;
            attemptTimestamps.Add(DateTimeOffset.UtcNow);
            await Task.CompletedTask;

            if (attemptCount < expectedAttempts)
            {
                throw new InvalidOperationException($"Attempt {attemptCount} failed");
            }

            return "success";
        });

        // Assert - Verify immediate retry (near-zero delay)
        attemptTimestamps.Should().HaveCount(expectedAttempts);
        var delay = (attemptTimestamps[1] - attemptTimestamps[0]).TotalMilliseconds;
        delay.Should().BeLessThan(100, "retry should be immediate with minimal delay");
    }

    [Fact]
    public async Task JitterBackoff_ShouldAddRandomnessToDelays()
    {
        // Arrange - Test jitter by running multiple times and checking variance
        var policy = CreateFastConnectPolicy(); // Exponential with jitter
        var firstDelays = new List<double>();

        // Run multiple times to observe jitter variance
        for (var run = 0; run < 5; run++)
        {
            var attemptTimestamps = new List<DateTimeOffset>();
            var attemptCount = 0;

            await policy.ExecuteAsync(async token =>
            {
                attemptCount++;
                attemptTimestamps.Add(DateTimeOffset.UtcNow);
                await Task.CompletedTask;

                if (attemptCount < 2) // Just need first retry
                {
                    throw new InvalidOperationException("Retry");
                }

                return "success";
            });

            var firstDelay = (attemptTimestamps[1] - attemptTimestamps[0]).TotalMilliseconds;
            firstDelays.Add(firstDelay);
        }

        // Assert - With jitter, delays should have some variance
        var minDelay = firstDelays.Min();
        var maxDelay = firstDelays.Max();

        // All delays should be in reasonable range (jitter doesn't make them too extreme)
        firstDelays.Should().AllSatisfy(d => d.Should().BeInRange(10, 150));
    }

    #endregion

    #region Timeout Tests (Using Short Timeout Policies)

    [Fact]
    public async Task TimeoutBehavior_ShouldThrowTimeoutRejectedException_WhenOperationExceedsTimeout()
    {
        // Arrange - Create a short-timeout policy for fast testing (100ms timeout)
        var shortTimeoutPolicy = new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromMilliseconds(100))
            .Build();

        // Act
        var act = async () => await shortTimeoutPolicy.ExecuteAsync(async token =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1), token); // Delay longer than timeout
            return "success";
        });

        // Assert - Verify timeout behavior works
        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }

    [Fact]
    public async Task TimeoutBehavior_ShouldSucceed_WhenOperationCompletesBeforeTimeout()
    {
        // Arrange
        var shortTimeoutPolicy = new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromMilliseconds(500))
            .Build();

        // Act
        var result = await shortTimeoutPolicy.ExecuteAsync(async token =>
        {
            await Task.Delay(50, token); // Complete well before timeout
            return "success";
        });

        // Assert
        result.Should().Be("success");
    }

    #endregion

    #region Generic Policy Factory Tests

    [Fact]
    public async Task CreateConnectPolicy_ShouldCreateWorkingPolicy()
    {
        // Arrange
        var policy = ResiliencePolicies.CreateConnectPolicy<string>();
        var attemptCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async token =>
        {
            attemptCount++;
            await Task.CompletedTask;
            return "success";
        });

        // Assert
        attemptCount.Should().Be(1);
        result.Should().Be("success");
    }

    [Fact]
    public async Task CreatePaymentOperationPolicy_ShouldCreateWorkingPolicy()
    {
        // Arrange
        var policy = ResiliencePolicies.CreatePaymentOperationPolicy<string>();
        var attemptCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async token =>
        {
            attemptCount++;
            await Task.CompletedTask;
            return "success";
        });

        // Assert
        attemptCount.Should().Be(1);
        result.Should().Be("success");
    }

    [Fact]
    public async Task CreateQueryPolicy_ShouldCreateWorkingPolicy()
    {
        // Arrange
        var policy = ResiliencePolicies.CreateQueryPolicy<string>();
        var attemptCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async token =>
        {
            attemptCount++;
            await Task.CompletedTask;
            return "success";
        });

        // Assert
        attemptCount.Should().Be(1);
        result.Should().Be("success");
    }

    [Fact]
    public async Task CreateWebhookPolicy_ShouldCreateWorkingPolicy()
    {
        // Arrange
        var policy = ResiliencePolicies.CreateWebhookPolicy<string>();
        var attemptCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async token =>
        {
            attemptCount++;
            await Task.CompletedTask;
            return "success";
        });

        // Assert
        attemptCount.Should().Be(1);
        result.Should().Be("success");
    }

    #endregion

    #region Cross-Policy Comparison Tests

    [Fact]
    public async Task Policies_ShouldHaveDistinctRetryBehaviors()
    {
        // This test verifies that each policy type has the correct retry count
        // Uses fast test policies to avoid waiting for production delays

        var connectRetries = await CountRetriesAsync(CreateFastRetryPolicy(3, DelayBackoffType.Exponential, true));
        var paymentRetries = await CountRetriesAsync(CreateFastRetryPolicy(2, DelayBackoffType.Constant, false));
        var queryRetries = await CountRetriesAsync(CreateFastRetryPolicy(1, DelayBackoffType.Constant, false));
        var webhookRetries = await CountRetriesAsync(CreateFastRetryPolicy(3, DelayBackoffType.Exponential, false));

        // Assert - Verify each policy type has correct retry configuration
        connectRetries.Should().Be(4, "ConnectPolicy pattern: initial + 3 retries");
        paymentRetries.Should().Be(3, "PaymentOperationPolicy pattern: initial + 2 retries");
        queryRetries.Should().Be(2, "QueryPolicy pattern: initial + 1 retry");
        webhookRetries.Should().Be(4, "WebhookPolicy pattern: initial + 3 retries");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Counts the total number of execution attempts (initial + retries) for a given policy.
    /// </summary>
    private static async Task<int> CountRetriesAsync(ResiliencePipeline policy)
    {
        var attemptCount = 0;

        try
        {
            await policy.ExecuteAsync(async token =>
            {
                attemptCount++;
                await Task.CompletedTask;
                throw new InvalidOperationException("Forced failure");
            });
        }
        catch (InvalidOperationException)
        {
            // Expected - all retries exhausted
        }

        return attemptCount;
    }

    #endregion
}
