using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.RateLimiting;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Services.RateLimiting
{
    /// <summary>
    /// Unit tests for MemoryRateLimiter.
    /// </summary>
    public class MemoryRateLimiterTests
    {
        #region Constructor and Setup Tests

        [Fact]
        public void Constructor_InitializesBuckets()
        {
            // Arrange & Act
            var limiter = new MemoryRateLimiter();

            // Assert
            Assert.NotNull(limiter);
            // Since _buckets is private, we can't directly assert, but instantiation should succeed
        }

        #endregion

        #region TryConsume Tests

        [Fact]
        public void TryConsume_FirstCall_ReturnsTrue()
        {
            // Arrange
            var limiter = new MemoryRateLimiter();
            var bucketKey = "test-key";
            var limit = 5;
            var window = TimeSpan.FromSeconds(10);

            // Act
            var result = limiter.TryConsume(bucketKey, limit, window, out var retryAfter);

            // Assert
            Assert.True(result);
            Assert.Equal(TimeSpan.Zero, retryAfter);
        }

        [Fact]
        public void TryConsume_WithinLimit_ReturnsTrue()
        {
            // Arrange
            var limiter = new MemoryRateLimiter();
            var bucketKey = "test-key";
            var limit = 3;
            var window = TimeSpan.FromSeconds(10);

            // Act
            for (int i = 0; i < limit; i++)
            {
                var result = limiter.TryConsume(bucketKey, limit, window, out var retryAfter);
                Assert.True(result);
                Assert.Equal(TimeSpan.Zero, retryAfter);
            }
        }

        [Fact]
        public void TryConsume_ExceedsLimit_ReturnsFalse_WithRetryAfter()
        {
            // Arrange
            var limiter = new MemoryRateLimiter();
            var bucketKey = "test-key";
            var limit = 2;
            var window = TimeSpan.FromSeconds(10);

            // Act - Consume up to limit
            for (int i = 0; i < limit; i++)
            {
                limiter.TryConsume(bucketKey, limit, window, out _);
            }

            // Now exceed
            var result = limiter.TryConsume(bucketKey, limit, window, out var retryAfter);

            // Assert
            Assert.False(result);
            Assert.True(retryAfter > TimeSpan.Zero);
        }

        [Fact]
        public void TryConsume_AfterWindowExpires_ResetsAndAllows()
        {
            // Arrange
            var limiter = new MemoryRateLimiter();
            var bucketKey = "test-key";
            var limit = 1;
            var window = TimeSpan.FromMilliseconds(100); // Short window for test

            // Act - Consume once
            limiter.TryConsume(bucketKey, limit, window, out _);

            // Wait for window to expire
            Thread.Sleep(150);

            // Try again
            var result = limiter.TryConsume(bucketKey, limit, window, out var retryAfter);

            // Assert
            Assert.True(result);
            Assert.Equal(TimeSpan.Zero, retryAfter);
        }

        [Fact]
        public void TryConsume_DifferentBuckets_AreIndependent()
        {
            // Arrange
            var limiter = new MemoryRateLimiter();
            var bucketKey1 = "key1";
            var bucketKey2 = "key2";
            var limit = 1;
            var window = TimeSpan.FromSeconds(10);

            // Act - Consume on first bucket
            limiter.TryConsume(bucketKey1, limit, window, out _);

            // Consume on second bucket
            var result = limiter.TryConsume(bucketKey2, limit, window, out var retryAfter);

            // Assert
            Assert.True(result);
            Assert.Equal(TimeSpan.Zero, retryAfter);
        }

        [Fact]
        public void TryConsume_SameBucket_ExceedsLimit_AfterMultipleCalls()
        {
            // Arrange
            var limiter = new MemoryRateLimiter();
            var bucketKey = "test-key";
            var limit = 3;
            var window = TimeSpan.FromSeconds(10);

            // Act - Consume up to limit
            for (int i = 0; i < limit; i++)
            {
                limiter.TryConsume(bucketKey, limit, window, out _);
            }

            // Exceed
            var result = limiter.TryConsume(bucketKey, limit, window, out var retryAfter);

            // Assert
            Assert.False(result);
            Assert.True(retryAfter > TimeSpan.Zero);
        }

        #endregion
    }
}