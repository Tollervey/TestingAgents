using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Infrastructure;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Infrastructure;

public class CorrelationIdMiddlewareTests
{
    private readonly Mock<ILogger<CorrelationIdMiddleware>> _loggerMock = new();

    [Fact]
    public async Task InvokeAsync_WithExistingHeader_UsesProvidedCorrelationId()
    {
        var expectedId = "test-correlation-123";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = expectedId;

        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            _loggerMock.Object);

        await middleware.InvokeAsync(context);

        context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString().Should().Be(expectedId);
        context.Items[CorrelationIdMiddleware.ItemKey].Should().Be(expectedId);
    }

    [Fact]
    public async Task InvokeAsync_WithoutHeader_GeneratesNewGuid()
    {
        var context = new DefaultHttpContext();

        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            _loggerMock.Object);

        await middleware.InvokeAsync(context);

        var id = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        id.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(id, out _).Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_SetsCorrelationIdInHttpContextItems()
    {
        var context = new DefaultHttpContext();

        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            _loggerMock.Object);

        await middleware.InvokeAsync(context);

        context.Items[CorrelationIdMiddleware.ItemKey].Should().NotBeNull();
        context.Items[CorrelationIdMiddleware.ItemKey].Should().BeOfType<string>();
    }

    [Fact]
    public async Task InvokeAsync_ResponseAlwaysHasCorrelationIdHeader()
    {
        var context = new DefaultHttpContext();

        var middleware = new CorrelationIdMiddleware(
            _ => Task.CompletedTask,
            _loggerMock.Object);

        await middleware.InvokeAsync(context);

        context.Response.Headers.Should().ContainKey(CorrelationIdMiddleware.HeaderName);
    }

    [Fact]
    public async Task InvokeAsync_CallsNextMiddleware()
    {
        var nextCalled = false;
        var context = new DefaultHttpContext();

        var middleware = new CorrelationIdMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            _loggerMock.Object);

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }
}
