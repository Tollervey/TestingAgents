using Microsoft.Extensions.Caching.Memory;
using Moq;
using System.Text.Json;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Runtime;
using Umbraco.Cms.Core.Services;
using Xunit;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Services.Runtime
{
    /// <summary>
    /// Unit tests for RuntimeSettingsService.
    /// </summary>
    public class RuntimeSettingsServiceTests
    {
        private const string Key = "LightningPayments:RuntimeFlags";
        private RuntimeFeatureFlags _flags;

        #region Constructor and Setup Tests

        [Fact]
        public void Constructor_InitializesFields()
        {
            // Arrange
            var mockKv = new Mock<IKeyValueService>();
            var mockCache = new Mock<IMemoryCache>();

            // Act
            var service = new RuntimeSettingsService(mockKv.Object, mockCache.Object);

            // Assert
            Assert.NotNull(service);
        }

        #endregion

        #region GetAsync Tests

        [Fact]
        public async Task GetAsync_ReturnsCachedValue_WhenCacheHasValue()
        {
            // Arrange
            var mockKv = new Mock<IKeyValueService>();
            var mockCache = new Mock<IMemoryCache>();
            _flags = new RuntimeFeatureFlags();
            mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
                .Callback(CacheCallback)
                .Returns(true);
            var service = new RuntimeSettingsService(mockKv.Object, mockCache.Object);

            // Act
            var result = await service.GetAsync();

            // Assert
            Assert.Equal(_flags, result);
            mockKv.Verify(k => k.GetValue(It.IsAny<string>()), Times.Never);
        }

        private void CacheCallback(object key, out object value)
        {
            value = _flags;
        }

        [Fact]
        public async Task GetAsync_RetrievesFromKvAndCaches_WhenCacheMissAndValidJson()
        {
            // Arrange
            var mockKv = new Mock<IKeyValueService>();
            var mockCache = new Mock<IMemoryCache>();
            var flags = new RuntimeFeatureFlags { /* set some properties if needed */ };
            var json = JsonSerializer.Serialize(flags);
            mockKv.Setup(k => k.GetValue(Key)).Returns(json);
            mockCache.Setup(c => c.TryGetValue(Key, out It.Ref<object>.IsAny))
                .Returns(false);
            var mockEntry = new Mock<ICacheEntry>();
            mockCache.Setup(c => c.CreateEntry(Key)).Returns(mockEntry.Object);
            var service = new RuntimeSettingsService(mockKv.Object, mockCache.Object);

            // Act
            var result = await service.GetAsync();

            // Assert
            Assert.NotNull(result);
            // Assuming RuntimeFeatureFlags has equality or check properties
            mockKv.Verify(k => k.GetValue(Key), Times.Once);
            mockCache.Verify(c => c.TryGetValue(Key, out It.Ref<object>.IsAny), Times.Once);
        }

        [Fact]
        public async Task GetAsync_ReturnsNewFlags_WhenCacheMissAndJsonIsNull()
        {
            // Arrange
            var mockKv = new Mock<IKeyValueService>();
            var mockCache = new Mock<IMemoryCache>();
            mockKv.Setup(k => k.GetValue(Key)).Returns((string?)null);
            mockCache.Setup(c => c.TryGetValue(Key, out It.Ref<object>.IsAny))
                .Returns(false);
            var mockEntry = new Mock<ICacheEntry>();
            mockCache.Setup(c => c.CreateEntry(Key)).Returns(mockEntry.Object);
            var service = new RuntimeSettingsService(mockKv.Object, mockCache.Object);

            // Act
            var result = await service.GetAsync();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<RuntimeFeatureFlags>(result);
            mockKv.Verify(k => k.GetValue(Key), Times.Once);
            mockCache.Verify(c => c.TryGetValue(Key, out It.Ref<object>.IsAny), Times.Once);
        }

        [Fact]
        public async Task GetAsync_ReturnsNewFlags_WhenCacheMissAndJsonIsEmpty()
        {
            // Arrange
            var mockKv = new Mock<IKeyValueService>();
            var mockCache = new Mock<IMemoryCache>();
            mockKv.Setup(k => k.GetValue(Key)).Returns(string.Empty);
            mockCache.Setup(c => c.TryGetValue(Key, out It.Ref<object>.IsAny))
                .Returns(false);
            var mockEntry = new Mock<ICacheEntry>();
            mockCache.Setup(c => c.CreateEntry(Key)).Returns(mockEntry.Object);
            var service = new RuntimeSettingsService(mockKv.Object, mockCache.Object);

            // Act
            var result = await service.GetAsync();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<RuntimeFeatureFlags>(result);
            mockKv.Verify(k => k.GetValue(Key), Times.Once);
            mockCache.Verify(c => c.TryGetValue(Key, out It.Ref<object>.IsAny), Times.Once);
        }

        [Fact]
        public async Task GetAsync_ReturnsNewFlags_WhenCacheMissAndJsonIsWhitespace()
        {
            // Arrange
            var mockKv = new Mock<IKeyValueService>();
            var mockCache = new Mock<IMemoryCache>();
            mockKv.Setup(k => k.GetValue(Key)).Returns("   ");
            mockCache.Setup(c => c.TryGetValue(Key, out It.Ref<object>.IsAny))
                .Returns(false);
            var mockEntry = new Mock<ICacheEntry>();
            mockCache.Setup(c => c.CreateEntry(Key)).Returns(mockEntry.Object);
            var service = new RuntimeSettingsService(mockKv.Object, mockCache.Object);

            // Act
            var result = await service.GetAsync();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<RuntimeFeatureFlags>(result);
            mockKv.Verify(k => k.GetValue(Key), Times.Once);
            mockCache.Verify(c => c.TryGetValue(Key, out It.Ref<object>.IsAny), Times.Once);
        }

        #endregion

        #region SaveAsync Tests

        [Fact]
        public async Task SaveAsync_SavesToKvAndRemovesFromCache()
        {
            // Arrange
            var mockKv = new Mock<IKeyValueService>();
            var mockCache = new Mock<IMemoryCache>();
            var flags = new RuntimeFeatureFlags();
            var expectedJson = JsonSerializer.Serialize(flags);
            var service = new RuntimeSettingsService(mockKv.Object, mockCache.Object);

            // Act
            await service.SaveAsync(flags);

            // Assert
            mockKv.Verify(k => k.SetValue(Key, expectedJson), Times.Once);
            mockCache.Verify(c => c.Remove(Key), Times.Once);
        }

        #endregion
    }
}