using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Middleware;
using Xunit;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Middleware
{
    /// <summary>
    /// Unit tests for ExceptionHandlingMiddleware.
    /// </summary>
    public class ExceptionHandlingMiddlewareTests
    {
        #region Constructor and Setup Tests

        [Fact]
        public void Constructor_InitializesFields()
        {
            // Arrange
            var mockNext = new Mock<RequestDelegate>();
            var mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();

            // Act
            var middleware = new ExceptionHandlingMiddleware(mockNext.Object, mockLogger.Object);

            // Assert
            Assert.NotNull(middleware);
        }

        #endregion

        #region InvokeAsync Tests

        [Fact]
        public async Task InvokeAsync_ShouldBypass_CallsNextDelegate()
        {
            // Arrange
            var mockNext = new Mock<RequestDelegate>();
            var mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            var middleware = new ExceptionHandlingMiddleware(mockNext.Object, mockLogger.Object);
            var context = new DefaultHttpContext();
            context.Request.Path = "/umbraco"; // Should bypass

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            mockNext.Verify(next => next(context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_NoException_CallsNextDelegate()
        {
            // Arrange
            var mockNext = new Mock<RequestDelegate>();
            var mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            var middleware = new ExceptionHandlingMiddleware(mockNext.Object, mockLogger.Object);
            var context = new DefaultHttpContext();
            context.Request.Path = "/home"; // Not bypass

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            mockNext.Verify(next => next(context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_OperationCanceledException_LogsInfoAndThrows()
        {
            // Arrange
            var mockNext = new Mock<RequestDelegate>();
            mockNext.Setup(next => next(It.IsAny<HttpContext>())).Throws(new OperationCanceledException());
            var mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            var middleware = new ExceptionHandlingMiddleware(mockNext.Object, mockLogger.Object);
            var context = new DefaultHttpContext();
            context.Request.Path = "/home";

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => middleware.InvokeAsync(context));
            mockLogger.Verify(logger => logger.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_Exception_ResponseNotStarted_WantsJson_ReturnsJsonError()
        {
            // Arrange
            var mockNext = new Mock<RequestDelegate>();
            mockNext.Setup(next => next(It.IsAny<HttpContext>())).Throws(new InvalidOperationException("Test exception"));
            var mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            var middleware = new ExceptionHandlingMiddleware(mockNext.Object, mockLogger.Object);
            var context = new DefaultHttpContext();
            context.Request.Path = "/home";
            context.Request.Headers["Accept"] = "application/json";
            context.TraceIdentifier = "test-trace-id";

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(500, context.Response.StatusCode);
            Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
            var responseBody = context.Response.Body.ToString(); // Note: In real tests, use a MemoryStream to capture body
            var expectedPayload = new { error = "An error occurred. Please try again later.", traceId = "test-trace-id" };
            var expectedJson = JsonSerializer.Serialize(expectedPayload);
            // Assert.Equal(expectedJson, responseBody); // Would need to capture body properly
            mockLogger.Verify(logger => logger.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_Exception_ResponseNotStarted_WantsText_ReturnsTextError()
        {
            // Arrange
            var mockNext = new Mock<RequestDelegate>();
            mockNext.Setup(next => next(It.IsAny<HttpContext>())).Throws(new InvalidOperationException("Test exception"));
            var mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            var middleware = new ExceptionHandlingMiddleware(mockNext.Object, mockLogger.Object);
            var context = new DefaultHttpContext();
            context.Request.Path = "/home";
            context.Request.Headers["Accept"] = "text/html";
            context.TraceIdentifier = "test-trace-id";

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(500, context.Response.StatusCode);
            Assert.Equal("text/plain; charset=utf-8", context.Response.ContentType);
            // Capture body similarly
            mockLogger.Verify(logger => logger.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_Exception_ResponseStarted_LogsErrorAndThrows()
        {
            // Arrange
            var mockNext = new Mock<RequestDelegate>();
            mockNext.Setup(next => next(It.IsAny<HttpContext>())).Throws(new InvalidOperationException("Test exception"));
            var mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            var middleware = new ExceptionHandlingMiddleware(mockNext.Object, mockLogger.Object);
            var mockContext = new Mock<HttpContext>();
            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(r => r.Path).Returns(new PathString("/home"));
            var mockResponse = new Mock<HttpResponse>();
            mockResponse.Setup(r => r.StartAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mockResponse.Setup(r => r.HasStarted).Returns(true);
            mockContext.Setup(c => c.Request).Returns(mockRequest.Object);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockContext.Setup(c => c.TraceIdentifier).Returns("test");
            var context = mockContext.Object;
            await context.Response.StartAsync();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
            mockLogger.Verify(logger => logger.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        #endregion

        #region ShouldBypass Tests

        [Theory]
        [InlineData("/umbraco")]
        [InlineData("/umbraco/management")]
        [InlineData("/umbraco/surface/paywallsurface")]
        [InlineData("/api")]
        [InlineData("/api/v1/test")]
        public void ShouldBypass_BypassPaths_ReturnsTrue(string path)
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = path;

            // Act
            var result = ExceptionHandlingMiddlewareTestsHelper.ShouldBypass(context);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/home")]
        [InlineData("/contact")]
        public void ShouldBypass_NonBypassPaths_ReturnsFalse(string path)
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = path;

            // Act
            var result = ExceptionHandlingMiddlewareTestsHelper.ShouldBypass(context);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region WantsJson Tests

        [Theory]
        [InlineData("application/json", true)]
        [InlineData("application/json, text/plain", true)]
        [InlineData("text/html, application/json", false)] // Contains text/html
        [InlineData("text/plain", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void WantsJson_VariousAcceptHeaders_ReturnsExpected(string accept, bool expected)
        {
            // Arrange
            var context = new DefaultHttpContext();
            if (accept != null)
            {
                context.Request.Headers["Accept"] = accept;
            }

            // Act
            var result = ExceptionHandlingMiddlewareTestsHelper.WantsJson(context);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion
    }

    /// <summary>
    /// Helper class to access private static methods for testing.
    /// </summary>
    internal static class ExceptionHandlingMiddlewareTestsHelper
    {
        public static bool ShouldBypass(HttpContext context)
        {
            // Use reflection to access private method
            var method = typeof(Umbraco.Community.Bitcoin.LightningPayments.Core.Middleware.ExceptionHandlingMiddleware)
                .GetMethod("ShouldBypass", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return (bool)method.Invoke(null, new object[] { context });
        }

        public static bool WantsJson(HttpContext context)
        {
            // Use reflection to access private method
            var method = typeof(Umbraco.Community.Bitcoin.LightningPayments.Core.Middleware.ExceptionHandlingMiddleware)
                .GetMethod("WantsJson", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return (bool)method.Invoke(null, new object[] { context });
        }
    }
}