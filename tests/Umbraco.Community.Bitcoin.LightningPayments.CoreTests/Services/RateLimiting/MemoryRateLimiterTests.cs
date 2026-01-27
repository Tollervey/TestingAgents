using FluentAssertions;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.RateLimiting;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Services.RateLimiting;

public class MemoryRateLimiterTests
{
    [Fact]
    public void TryConsume_WithinLimit_ReturnsTrue()
    {
        var limiter = new MemoryRateLimiter();
        var key = $"test-{Guid.NewGuid():N}";

        var result = limiter.TryConsume(key, 5, TimeSpan.FromSeconds(30), out var retryAfter);

        result.Should().BeTrue();
        retryAfter.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void TryConsume_ExceedingLimit_ReturnsFalse()
    {
        var limiter = new MemoryRateLimiter();
        var key = $"test-{Guid.NewGuid():N}";
        var limit = 3;

        for (int i = 0; i < limit; i++)
        {
            limiter.TryConsume(key, limit, TimeSpan.FromSeconds(30), out _).Should().BeTrue();
        }

        var result = limiter.TryConsume(key, limit, TimeSpan.FromSeconds(30), out var retryAfter);

        result.Should().BeFalse();
        retryAfter.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void TryConsume_RetryAfterIsPositive_WhenRateLimited()
    {
        var limiter = new MemoryRateLimiter();
        var key = $"test-{Guid.NewGuid():N}";

        limiter.TryConsume(key, 1, TimeSpan.FromSeconds(30), out _);
        limiter.TryConsume(key, 1, TimeSpan.FromSeconds(30), out var retryAfter);

        retryAfter.TotalSeconds.Should().BeGreaterThan(0);
        retryAfter.TotalSeconds.Should().BeLessThanOrEqualTo(30d);
    }

    [Fact]
    public async Task TryConsume_ResetsAfterWindow()
    {
        var limiter = new MemoryRateLimiter();
        var key = $"test-{Guid.NewGuid():N}";
        var window = TimeSpan.FromMilliseconds(100);

        limiter.TryConsume(key, 1, window, out _);
        limiter.TryConsume(key, 1, window, out _).Should().BeFalse();

        await Task.Delay(150);

        var result = limiter.TryConsume(key, 1, window, out _);
        result.Should().BeTrue();
    }

    [Fact]
    public void TryConsume_DifferentKeys_AreIndependent()
    {
        var limiter = new MemoryRateLimiter();
        var key1 = $"bucket-a-{Guid.NewGuid():N}";
        var key2 = $"bucket-b-{Guid.NewGuid():N}";

        limiter.TryConsume(key1, 1, TimeSpan.FromSeconds(30), out _);
        limiter.TryConsume(key1, 1, TimeSpan.FromSeconds(30), out _).Should().BeFalse();

        var result = limiter.TryConsume(key2, 1, TimeSpan.FromSeconds(30), out _);
        result.Should().BeTrue();
    }

    [Fact]
    public void TryConsume_ThreadSafe_UnderConcurrentAccess()
    {
        var limiter = new MemoryRateLimiter();
        var key = $"concurrent-{Guid.NewGuid():N}";
        var limit = 100;
        var totalAttempts = 200;
        var successCount = 0;

        Parallel.For(0, totalAttempts, _ =>
        {
            TimeSpan retryAfter;
            if (limiter.TryConsume(key, limit, TimeSpan.FromSeconds(60), out retryAfter))
            {
                Interlocked.Increment(ref successCount);
            }
        });

        successCount.Should().Be(limit);
    }
}
