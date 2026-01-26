using System.Security.Cryptography;
using System.Text;
using Breez.Sdk.Liquid.Extensions.AspNetCore.Middleware;
using Breez.Sdk.Liquid.Extensions.Core.Configuration;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Breez.Sdk.Liquid.Extensions.AspNetCore.Tests.Middleware;

/// <summary>
/// Unit tests for WebhookValidationMiddleware.
/// Tests follow TDD RED-GREEN-REFACTOR pattern.
/// These tests are written FIRST and will FAIL until the implementation is created.
/// </summary>
public class WebhookValidationMiddlewareTests
{
    private const string TestSecret = "test-webhook-secret-12345";
    private readonly Mock<IOptions<BreezSdkOptions>> _mockOptions;
    private readonly Mock<ILogger<WebhookValidationMiddleware>> _mockLogger;

    public WebhookValidationMiddlewareTests()
    {
        _mockOptions = new Mock<IOptions<BreezSdkOptions>>();
        _mockOptions.Setup(x => x.Value).Returns(new BreezSdkOptions
        {
            WebhookSecret = TestSecret
        });

        _mockLogger = new Mock<ILogger<WebhookValidationMiddleware>>();
    }

    [Fact]
    public async Task InvokeAsync_WithValidSignatureAndTimestamp_CallsNext()
    {
        // Arrange
        var payload = """{"eventType":"payment_received","paymentHash":"abc123"}""";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = ComputeSignature(timestamp, payload, TestSecret);

        var context = CreateHttpContext(payload);
        context.Request.Headers["X-Breez-Signature"] = signature;
        context.Request.Headers["X-Breez-Timestamp"] = timestamp;

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new WebhookValidationMiddleware(next, _mockOptions.Object, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().NotBe(401);
    }

    [Fact]
    public async Task InvokeAsync_WithMissingSignature_Returns401()
    {
        // Arrange
        var payload = """{"eventType":"payment_received","paymentHash":"abc123"}""";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        var context = CreateHttpContext(payload);
        context.Request.Headers["X-Breez-Timestamp"] = timestamp;
        // No X-Breez-Signature header

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new WebhookValidationMiddleware(next, _mockOptions.Object, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_WithMissingTimestamp_Returns401()
    {
        // Arrange
        var payload = """{"eventType":"payment_received","paymentHash":"abc123"}""";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = ComputeSignature(timestamp, payload, TestSecret);

        var context = CreateHttpContext(payload);
        context.Request.Headers["X-Breez-Signature"] = signature;
        // No X-Breez-Timestamp header

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new WebhookValidationMiddleware(next, _mockOptions.Object, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidSignature_Returns401()
    {
        // Arrange
        var payload = """{"eventType":"payment_received","paymentHash":"abc123"}""";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var invalidSignature = "sha256=invalid-signature-value";

        var context = CreateHttpContext(payload);
        context.Request.Headers["X-Breez-Signature"] = invalidSignature;
        context.Request.Headers["X-Breez-Timestamp"] = timestamp;

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new WebhookValidationMiddleware(next, _mockOptions.Object, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_WithWrongSecret_Returns401()
    {
        // Arrange
        var payload = """{"eventType":"payment_received","paymentHash":"abc123"}""";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signatureWithWrongSecret = ComputeSignature(timestamp, payload, "wrong-secret");

        var context = CreateHttpContext(payload);
        context.Request.Headers["X-Breez-Signature"] = signatureWithWrongSecret;
        context.Request.Headers["X-Breez-Timestamp"] = timestamp;

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new WebhookValidationMiddleware(next, _mockOptions.Object, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_WithStaleTimestamp_Returns401()
    {
        // Arrange
        var payload = """{"eventType":"payment_received","paymentHash":"abc123"}""";
        // Timestamp from 10 minutes ago (stale - outside 5 minute window)
        var staleTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds().ToString();
        var signature = ComputeSignature(staleTimestamp, payload, TestSecret);

        var context = CreateHttpContext(payload);
        context.Request.Headers["X-Breez-Signature"] = signature;
        context.Request.Headers["X-Breez-Timestamp"] = staleTimestamp;

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new WebhookValidationMiddleware(next, _mockOptions.Object, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_WithFutureTimestamp_Returns401()
    {
        // Arrange
        var payload = """{"eventType":"payment_received","paymentHash":"abc123"}""";
        // Timestamp from 10 minutes in the future (invalid)
        var futureTimestamp = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds().ToString();
        var signature = ComputeSignature(futureTimestamp, payload, TestSecret);

        var context = CreateHttpContext(payload);
        context.Request.Headers["X-Breez-Signature"] = signature;
        context.Request.Headers["X-Breez-Timestamp"] = futureTimestamp;

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new WebhookValidationMiddleware(next, _mockOptions.Object, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_WithTimestampWithinWindow_CallsNext()
    {
        // Arrange
        var payload = """{"eventType":"payment_received","paymentHash":"abc123"}""";
        // Timestamp from 3 minutes ago (within 5 minute window)
        var timestamp = DateTimeOffset.UtcNow.AddMinutes(-3).ToUnixTimeSeconds().ToString();
        var signature = ComputeSignature(timestamp, payload, TestSecret);

        var context = CreateHttpContext(payload);
        context.Request.Headers["X-Breez-Signature"] = signature;
        context.Request.Headers["X-Breez-Timestamp"] = timestamp;

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new WebhookValidationMiddleware(next, _mockOptions.Object, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidTimestampFormat_Returns401()
    {
        // Arrange
        var payload = """{"eventType":"payment_received","paymentHash":"abc123"}""";
        var invalidTimestamp = "not-a-number";
        var signature = ComputeSignature(invalidTimestamp, payload, TestSecret);

        var context = CreateHttpContext(payload);
        context.Request.Headers["X-Breez-Signature"] = signature;
        context.Request.Headers["X-Breez-Timestamp"] = invalidTimestamp;

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new WebhookValidationMiddleware(next, _mockOptions.Object, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_WithNoWebhookSecretConfigured_Returns401()
    {
        // Arrange
        var optionsWithNoSecret = new Mock<IOptions<BreezSdkOptions>>();
        optionsWithNoSecret.Setup(x => x.Value).Returns(new BreezSdkOptions
        {
            WebhookSecret = null
        });

        var payload = """{"eventType":"payment_received","paymentHash":"abc123"}""";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = ComputeSignature(timestamp, payload, TestSecret);

        var context = CreateHttpContext(payload);
        context.Request.Headers["X-Breez-Signature"] = signature;
        context.Request.Headers["X-Breez-Timestamp"] = timestamp;

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new WebhookValidationMiddleware(next, optionsWithNoSecret.Object, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_AllowsBodyToBeReadAgainByNext()
    {
        // Arrange
        var payload = """{"eventType":"payment_received","paymentHash":"abc123"}""";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = ComputeSignature(timestamp, payload, TestSecret);

        var context = CreateHttpContext(payload);
        context.Request.Headers["X-Breez-Signature"] = signature;
        context.Request.Headers["X-Breez-Timestamp"] = timestamp;

        string? bodyReadByNext = null;
        RequestDelegate next = async ctx =>
        {
            ctx.Request.Body.Position = 0;
            using var reader = new StreamReader(ctx.Request.Body);
            bodyReadByNext = await reader.ReadToEndAsync();
        };

        var middleware = new WebhookValidationMiddleware(next, _mockOptions.Object, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        bodyReadByNext.Should().Be(payload);
    }

    private static DefaultHttpContext CreateHttpContext(string body)
    {
        var context = new DefaultHttpContext();
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        context.Request.Body = new MemoryStream(bodyBytes);
        context.Request.ContentLength = bodyBytes.Length;
        context.Request.ContentType = "application/json";
        context.Request.Method = "POST";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static string ComputeSignature(string timestamp, string payload, string secret)
    {
        var signaturePayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signaturePayload));
        return $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
