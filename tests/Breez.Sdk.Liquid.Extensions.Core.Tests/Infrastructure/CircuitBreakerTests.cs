using Breez.Sdk.Liquid.Extensions.Core.Configuration;
using Breez.Sdk.Liquid.Extensions.Core.Infrastructure;
using FluentAssertions;
using Polly;
using Polly.CircuitBreaker;

namespace Breez.Sdk.Liquid.Extensions.Core.Tests.Infrastructure;

/// <summary>
/// Unit tests for circuit breaker functionality in ResiliencePolicies.
/// These tests verify the circuit breaker pattern implementation using Polly v8.
/// </summary>
/// <remarks>
/// TDD RED PHASE: These tests reference CreateCircuitBreakerPolicy methods that don't exist yet.
/// They will fail initially, then pass once circuit breaker support is added to ResiliencePolicies.
///
/// Circuit Breaker States:
/// - CLOSED: Normal operation, requests pass through
/// - OPEN: Circuit broken, requests fail immediately
/// - HALF-OPEN: Testing recovery, single request allowed
///
/// Test Coverage:
/// 1. Circuit opens after failure threshold
/// 2. Circuit rejects calls when open
/// 3. Circuit transitions to half-open after break duration
/// 4. Circuit closes on successful half-open call
/// 5. Circuit reopens on failed half-open call
/// 6. Custom configuration options are respected
/// </remarks>
public class CircuitBreakerTests
{
    private readonly CircuitBreakerOptions _defaultOptions;
    private readonly CircuitBreakerOptions _customOptions;

    public CircuitBreakerTests()
    {
        _defaultOptions = new CircuitBreakerOptions();

        _customOptions = new CircuitBreakerOptions
        {
            FailureThreshold = 3,
            SamplingDurationSeconds = 30,
            BreakDurationSeconds = 15
        };
    }

    #region Circuit Opening Tests

    [Fact]
    public async Task CircuitBreaker_OpensAfterFailureThreshold()
    {
        // Arrange
        var policy = ResiliencePolicies.CreateCircuitBreakerPolicy(_defaultOptions);
        var callCount = 0;

        // Act - Simulate failures to reach threshold (default: 5)
        for (int i = 0; i < _defaultOptions.FailureThreshold; i++)
        {
            try
            {
                await policy.ExecuteAsync(async token =>
                {
                    callCount++;
                    await Task.CompletedTask;
                    throw new InvalidOperationException($"Simulated failure {i + 1}");
                });
            }
            catch (InvalidOperationException)
            {
                // Expected failure
            }
        }

        // Assert - Next call should fail immediately due to open circuit
        var circuitBroken = false;
        try
        {
            await policy.ExecuteAsync(async token =>
            {
                callCount++;
                await Task.CompletedTask;
                return "success";
            });
        }
        catch (BrokenCircuitException)
        {
            circuitBroken = true;
        }

        circuitBroken.Should().BeTrue();
        callCount.Should().Be(_defaultOptions.FailureThreshold,
            "circuit should open after threshold, preventing additional calls");
    }

    [Fact]
    public async Task CircuitBreaker_WithCustomThreshold_OpensAfterCustomFailureCount()
    {
        // Arrange
        var policy = ResiliencePolicies.CreateCircuitBreakerPolicy(_customOptions);
        var callCount = 0;

        // Act - Simulate failures to reach custom threshold (3)
        for (int i = 0; i < _customOptions.FailureThreshold; i++)
        {
            try
            {
                await policy.ExecuteAsync(async token =>
                {
                    callCount++;
                    await Task.CompletedTask;
                    throw new InvalidOperationException($"Simulated failure {i + 1}");
                });
            }
            catch (InvalidOperationException)
            {
                // Expected failure
            }
        }

        // Assert - Next call should fail immediately
        var circuitBroken = false;
        try
        {
            await policy.ExecuteAsync(async token =>
            {
                callCount++;
                await Task.CompletedTask;
                return "success";
            });
        }
        catch (BrokenCircuitException)
        {
            circuitBroken = true;
        }

        circuitBroken.Should().BeTrue();
        callCount.Should().Be(_customOptions.FailureThreshold);
    }

    [Fact]
    public async Task CircuitBreaker_DoesNotOpenBeforeThreshold()
    {
        // Arrange
        var policy = ResiliencePolicies.CreateCircuitBreakerPolicy(_defaultOptions);
        var failureCount = _defaultOptions.FailureThreshold - 1;

        // Act - Simulate failures just below threshold
        for (int i = 0; i < failureCount; i++)
        {
            try
            {
                await policy.ExecuteAsync(async token =>
                {
                    await Task.CompletedTask;
                    throw new InvalidOperationException($"Simulated failure {i + 1}");
                });
            }
            catch (InvalidOperationException)
            {
                // Expected failure
            }
        }

        // Assert - Next call should still attempt execution (not immediately rejected)
        var successCount = 0;
        var result = await policy.ExecuteAsync(async token =>
        {
            successCount++;
            await Task.CompletedTask;
            return "success";
        });

        result.Should().Be("success");
        successCount.Should().Be(1, "circuit should still be closed, allowing execution");
    }

    #endregion

    #region Circuit Rejection Tests

    [Fact]
    public async Task CircuitBreaker_WhenOpen_RejectsCallsImmediately()
    {
        // Arrange - Open the circuit by hitting failure threshold
        var policy = ResiliencePolicies.CreateCircuitBreakerPolicy(_defaultOptions);
        await OpenCircuit(policy, _defaultOptions.FailureThreshold);

        // Act - Try multiple calls while circuit is open
        var rejectedCalls = 0;
        for (int i = 0; i < 5; i++)
        {
            try
            {
                await policy.ExecuteAsync(async token =>
                {
                    await Task.CompletedTask;
                    return "should not execute";
                });
            }
            catch (BrokenCircuitException)
            {
                rejectedCalls++;
            }
        }

        // Assert
        rejectedCalls.Should().Be(5, "all calls should be rejected when circuit is open");
    }

    [Fact]
    public async Task CircuitBreaker_WhenOpen_ThrowsBrokenCircuitException()
    {
        // Arrange - Open the circuit
        var policy = ResiliencePolicies.CreateCircuitBreakerPolicy(_defaultOptions);
        await OpenCircuit(policy, _defaultOptions.FailureThreshold);

        // Act & Assert
        BrokenCircuitException? caughtException = null;
        try
        {
            await policy.ExecuteAsync(async token =>
            {
                await Task.CompletedTask;
                return "test";
            });
        }
        catch (BrokenCircuitException ex)
        {
            caughtException = ex;
        }

        caughtException.Should().NotBeNull();
        caughtException!.Message.Should().Contain("circuit is now open");
    }

    #endregion

    #region Half-Open State Tests

    [Fact]
    public async Task CircuitBreaker_TransitionsToHalfOpenAfterBreakDuration()
    {
        // Arrange - Open the circuit
        var policy = ResiliencePolicies.CreateCircuitBreakerPolicy(_customOptions);
        await OpenCircuit(policy, _customOptions.FailureThreshold);

        // Verify circuit is open
        var circuitIsOpen = false;
        try
        {
            await policy.ExecuteAsync(async token =>
            {
                await Task.CompletedTask;
                return "test";
            });
        }
        catch (BrokenCircuitException)
        {
            circuitIsOpen = true;
        }
        circuitIsOpen.Should().BeTrue();

        // Act - Wait for break duration to elapse (add buffer for timing)
        await Task.Delay(TimeSpan.FromSeconds(_customOptions.BreakDurationSeconds + 1));

        // Assert - Circuit should allow a test call (half-open state)
        var executionAttempted = false;
        var result = await policy.ExecuteAsync(async token =>
        {
            executionAttempted = true;
            await Task.CompletedTask;
            return "success";
        });

        result.Should().Be("success");
        executionAttempted.Should().BeTrue("circuit should transition to half-open and allow execution");
    }

    [Fact]
    public async Task CircuitBreaker_ClosesOnSuccessfulHalfOpenCall()
    {
        // Arrange - Open the circuit and wait for half-open transition
        var policy = ResiliencePolicies.CreateCircuitBreakerPolicy(_customOptions);
        await OpenCircuit(policy, _customOptions.FailureThreshold);
        await Task.Delay(TimeSpan.FromSeconds(_customOptions.BreakDurationSeconds + 1));

        // Act - Make successful call in half-open state
        var result = await policy.ExecuteAsync(async token =>
        {
            await Task.CompletedTask;
            return "success";
        });
        result.Should().Be("success");

        // Assert - Circuit should be closed, allowing multiple successful calls
        var successCount = 0;
        for (int i = 0; i < 5; i++)
        {
            var callResult = await policy.ExecuteAsync(async token =>
            {
                successCount++;
                await Task.CompletedTask;
                return "success";
            });

            callResult.Should().Be("success");
        }

        successCount.Should().Be(5, "circuit should be closed and allow all calls through");
    }

    [Fact]
    public async Task CircuitBreaker_RemainsOpenOnFailedHalfOpenCall()
    {
        // Arrange - Open the circuit and wait for half-open transition
        var policy = ResiliencePolicies.CreateCircuitBreakerPolicy(_customOptions);
        await OpenCircuit(policy, _customOptions.FailureThreshold);
        await Task.Delay(TimeSpan.FromSeconds(_customOptions.BreakDurationSeconds + 1));

        // Act - Make failed call in half-open state
        Exception? halfOpenException = null;
        try
        {
            await policy.ExecuteAsync(async token =>
            {
                await Task.CompletedTask;
                throw new InvalidOperationException("Still failing");
            });
        }
        catch (InvalidOperationException ex)
        {
            halfOpenException = ex;
        }
        halfOpenException.Should().NotBeNull();

        // Assert - Circuit should reopen, immediately rejecting calls
        var circuitReopened = false;
        try
        {
            await policy.ExecuteAsync(async token =>
            {
                await Task.CompletedTask;
                return "test";
            });
        }
        catch (BrokenCircuitException)
        {
            circuitReopened = true;
        }

        circuitReopened.Should().BeTrue();
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void CreateCircuitBreakerPolicy_WithDefaultOptions_CreatesValidPolicy()
    {
        // Arrange & Act
        var policy = ResiliencePolicies.CreateCircuitBreakerPolicy(_defaultOptions);

        // Assert
        policy.Should().NotBeNull();
    }

    [Fact]
    public void CreateCircuitBreakerPolicy_WithCustomOptions_RespectsConfiguration()
    {
        // Arrange
        var customOptions = new CircuitBreakerOptions
        {
            FailureThreshold = 2,
            SamplingDurationSeconds = 20,
            BreakDurationSeconds = 10
        };

        // Act
        var policy = ResiliencePolicies.CreateCircuitBreakerPolicy(customOptions);

        // Assert
        policy.Should().NotBeNull();
        // Configuration is verified through behavior in other tests
    }

    [Fact]
    public void CreateCircuitBreakerPolicy_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => ResiliencePolicies.CreateCircuitBreakerPolicy(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public async Task CreateCircuitBreakerPolicy_SamplingDuration_CountsFailuresInWindow()
    {
        // Arrange - Use short sampling duration
        var options = new CircuitBreakerOptions
        {
            FailureThreshold = 3,
            SamplingDurationSeconds = 2, // 2-second window
            BreakDurationSeconds = 5
        };
        var policy = ResiliencePolicies.CreateCircuitBreakerPolicy(options);

        // Act - Generate 2 failures
        for (int i = 0; i < 2; i++)
        {
            try
            {
                await policy.ExecuteAsync(async token =>
                {
                    await Task.CompletedTask;
                    throw new InvalidOperationException("Failure");
                });
            }
            catch (InvalidOperationException)
            {
                // Expected failure
            }
        }

        // Wait for sampling window to pass
        await Task.Delay(TimeSpan.FromSeconds(options.SamplingDurationSeconds + 1));

        // Generate 2 more failures (should not trigger circuit break because previous failures aged out)
        for (int i = 0; i < 2; i++)
        {
            try
            {
                await policy.ExecuteAsync(async token =>
                {
                    await Task.CompletedTask;
                    throw new InvalidOperationException("Failure");
                });
            }
            catch (BrokenCircuitException)
            {
                Assert.Fail("Circuit should not be broken yet");
            }
            catch (InvalidOperationException)
            {
                // Expected failure
            }
        }

        // Assert - Circuit should still be closed (failures in different windows)
        var executionCount = 0;
        var result = await policy.ExecuteAsync(async token =>
        {
            executionCount++;
            await Task.CompletedTask;
            return "success";
        });

        result.Should().Be("success");
        executionCount.Should().Be(1, "circuit should remain closed due to sampling window");
    }

    #endregion

    #region Typed Policy Tests

    [Fact]
    public async Task CreateCircuitBreakerPolicy_TypedVersion_OpensAfterFailures()
    {
        // Arrange
        var policy = ResiliencePolicies.CreateCircuitBreakerPolicy<string>(_defaultOptions);

        // Act - Trigger failures to open circuit
        for (int i = 0; i < _defaultOptions.FailureThreshold; i++)
        {
            try
            {
                await policy.ExecuteAsync<string>(async token =>
                {
                    await Task.CompletedTask;
                    throw new InvalidOperationException("Failure");
                });
            }
            catch (InvalidOperationException)
            {
                // Expected failure
            }
        }

        // Assert - Circuit should be open
        var circuitBroken = false;
        try
        {
            await policy.ExecuteAsync<string>(async token =>
            {
                await Task.CompletedTask;
                return "test";
            });
        }
        catch (BrokenCircuitException)
        {
            circuitBroken = true;
        }

        circuitBroken.Should().BeTrue();
    }

    [Fact]
    public async Task CreateCircuitBreakerPolicy_TypedVersion_ReturnsCorrectType()
    {
        // Arrange
        var policy = ResiliencePolicies.CreateCircuitBreakerPolicy<int>(_defaultOptions);

        // Act
        var result = await policy.ExecuteAsync<int>(async token =>
        {
            await Task.CompletedTask;
            return 42;
        });

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void CreateCircuitBreakerPolicy_TypedVersion_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => ResiliencePolicies.CreateCircuitBreakerPolicy<string>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper method to open a circuit by executing the specified number of failures.
    /// </summary>
    private static async Task OpenCircuit(ResiliencePipeline policy, int failureCount)
    {
        for (int i = 0; i < failureCount; i++)
        {
            try
            {
                await policy.ExecuteAsync(async token =>
                {
                    await Task.CompletedTask;
                    throw new InvalidOperationException($"Simulated failure {i + 1}");
                });
            }
            catch (InvalidOperationException)
            {
                // Expected failure to trigger circuit breaker
            }
        }
    }

    #endregion
}
