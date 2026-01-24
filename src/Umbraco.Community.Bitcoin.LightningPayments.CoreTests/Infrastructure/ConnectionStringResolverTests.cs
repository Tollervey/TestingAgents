using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Infrastructure;
using Xunit;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Infrastructure
{
    /// <summary>
    /// Unit tests for ConnectionStringResolver.
    /// </summary>
    public class ConnectionStringResolverTests
    {
        #region Resolve Tests

        [Fact]
        public void Resolve_NullConnStr_ThrowsArgumentException()
        {
            // Arrange
            var mockEnv = new Mock<IHostEnvironment>();
            var mockLogger = new Mock<ILogger>();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => ConnectionStringResolver.Resolve(null!, mockEnv.Object, mockLogger.Object));
            Assert.Equal("LightningPayments connection string is missing. (Parameter 'connStr')", exception.Message);
        }

        [Fact]
        public void Resolve_EmptyConnStr_ThrowsArgumentException()
        {
            // Arrange
            var mockEnv = new Mock<IHostEnvironment>();
            var mockLogger = new Mock<ILogger>();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => ConnectionStringResolver.Resolve("", mockEnv.Object, mockLogger.Object));
            Assert.Equal("LightningPayments connection string is missing. (Parameter 'connStr')", exception.Message);
        }

        [Fact]
        public void Resolve_WhitespaceConnStr_ThrowsArgumentException()
        {
            // Arrange
            var mockEnv = new Mock<IHostEnvironment>();
            var mockLogger = new Mock<ILogger>();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => ConnectionStringResolver.Resolve("   ", mockEnv.Object, mockLogger.Object));
            Assert.Equal("LightningPayments connection string is missing. (Parameter 'connStr')", exception.Message);
        }

        [Fact]
        public void Resolve_ReplacesDataDirectory()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            try
            {
                var mockEnv = new Mock<IHostEnvironment>();
                mockEnv.Setup(e => e.ContentRootPath).Returns(tempDir);
                var mockLogger = new Mock<ILogger>();
                var connStr = "Data Source=|DataDirectory|\\db.sqlite";

                // Act
                var result = ConnectionStringResolver.Resolve(connStr, mockEnv.Object, mockLogger.Object);

                // Assert
                var builder = new SqliteConnectionStringBuilder(result);
                var expectedPath = Path.Combine(tempDir, "umbraco", "Data", "db.sqlite");
                Assert.Equal(expectedPath, builder.DataSource);
                Assert.Equal(SqliteCacheMode.Shared, builder.Cache);
                Assert.True(builder.ForeignKeys);
                Assert.True(builder.Pooling);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void Resolve_RelativeDataSource_MakesAbsolute()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            try
            {
                var mockEnv = new Mock<IHostEnvironment>();
                mockEnv.Setup(e => e.ContentRootPath).Returns(tempDir);
                var mockLogger = new Mock<ILogger>();
                var connStr = "Data Source=relative\\db.sqlite";

                // Act
                var result = ConnectionStringResolver.Resolve(connStr, mockEnv.Object, mockLogger.Object);

                // Assert
                var builder = new SqliteConnectionStringBuilder(result);
                var expectedPath = Path.GetFullPath(Path.Combine(tempDir, "relative", "db.sqlite"));
                Assert.Equal(expectedPath, builder.DataSource);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void Resolve_AbsoluteDataSource_LeavesUnchanged()
        {
            // Arrange
            var mockEnv = new Mock<IHostEnvironment>();
            mockEnv.Setup(e => e.ContentRootPath).Returns("C:\\app");
            var mockLogger = new Mock<ILogger>();
            var absolutePath = "C:\\absolute\\db.sqlite";
            var connStr = $"Data Source={absolutePath}";

            // Act
            var result = ConnectionStringResolver.Resolve(connStr, mockEnv.Object, mockLogger.Object);

            // Assert
            var builder = new SqliteConnectionStringBuilder(result);
            Assert.Equal(absolutePath, builder.DataSource);
        }

        [Fact]
        public void Resolve_MemoryDataSource_LeavesUnchanged()
        {
            // Arrange
            var mockEnv = new Mock<IHostEnvironment>();
            mockEnv.Setup(e => e.ContentRootPath).Returns("C:\\app");
            var mockLogger = new Mock<ILogger>();
            var connStr = "Data Source=:memory:";

            // Act
            var result = ConnectionStringResolver.Resolve(connStr, mockEnv.Object, mockLogger.Object);

            // Assert
            var builder = new SqliteConnectionStringBuilder(result);
            Assert.Equal(":memory:", builder.DataSource);
        }

        [Fact]
        public void Resolve_CreatesDirectories()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            try
            {
                var mockEnv = new Mock<IHostEnvironment>();
                mockEnv.Setup(e => e.ContentRootPath).Returns(tempDir);
                var mockLogger = new Mock<ILogger>();
                var connStr = "Data Source=deep\\nested\\db.sqlite";

                // Act
                ConnectionStringResolver.Resolve(connStr, mockEnv.Object, mockLogger.Object);

                // Assert
                var dbDir = Path.Combine(tempDir, "deep", "nested");
                Assert.True(Directory.Exists(dbDir));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void Resolve_SetsDefaults()
        {
            // Arrange
            var mockEnv = new Mock<IHostEnvironment>();
            mockEnv.Setup(e => e.ContentRootPath).Returns("C:\\app");
            var mockLogger = new Mock<ILogger>();
            var connStr = "Data Source=:memory:";

            // Act
            var result = ConnectionStringResolver.Resolve(connStr, mockEnv.Object, mockLogger.Object);

            // Assert
            var builder = new SqliteConnectionStringBuilder(result);
            Assert.Equal(SqliteCacheMode.Shared, builder.Cache);
            Assert.True(builder.ForeignKeys);
            Assert.True(builder.Pooling);
        }

        [Fact]
        public void Resolve_LogsDebugMessage()
        {
            // Arrange
            var mockEnv = new Mock<IHostEnvironment>();
            mockEnv.Setup(e => e.ContentRootPath).Returns("C:\\app");
            var mockLogger = new Mock<ILogger>();
            var connStr = "Data Source=:memory:";

            // Act
            var result = ConnectionStringResolver.Resolve(connStr, mockEnv.Object, mockLogger.Object);

            // Assert
            mockLogger.Verify(l => l.Log(LogLevel.Debug, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void Resolve_NoLogger_DoesNotThrow()
        {
            // Arrange
            var mockEnv = new Mock<IHostEnvironment>();
            mockEnv.Setup(e => e.ContentRootPath).Returns("C:\\app");
            var connStr = "Data Source=:memory:";

            // Act
            var result = ConnectionStringResolver.Resolve(connStr, mockEnv.Object, null);

            // Assert
            Assert.NotNull(result);
        }

        #endregion
    }
}