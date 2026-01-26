using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Breez.Sdk.Liquid.Extensions.AspNetCore.Endpoints;
using Breez.Sdk.Liquid.Extensions.AspNetCore.Extensions;
using Breez.Sdk.Liquid.Extensions.AspNetCore.Middleware;
using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Breez.Sdk.Liquid.Extensions.AspNetCore.Tests.Integration;

/// <summary>
/// Integration tests for WebApplicationBuilder extensions.
/// Tests the full flow of service registration, middleware, and endpoints.
/// </summary>
public class WebApplicationBuilderExtensionsTests : IAsyncDisposable
{
    private const string TestWebhookSecret = "test-webhook-secret-for-integration";
    private const string TestApiKey = "test-api-key";
    private WebApplication? _app;

    [Fact]
    public async Task AddBreezSdk_RegistersBreezSdkService()
    {
        // Arrange
        _app = await CreateTestApplicationAsync();

        // Act
        var sdkService = _app.Services.GetService<IBreezSdkService>();

        // Assert
        sdkService.Should().NotBeNull();
    }

    [Fact]
    public async Task MapBreezSdkEndpoints_WithoutSignature_Returns401()
    {
        // Arrange
        _app = await CreateTestApplicationAsync();
        var client = _app.GetTestClient();
        var payload = CreatePaymentReceivedPayload("test-hash-456", 100000UL);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/breez/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        // No signature headers

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MapBreezSdkEndpoints_WithInvalidSignature_Returns401()
    {
        // Arrange
        _app = await CreateTestApplicationAsync();
        var client = _app.GetTestClient();
        var payload = CreatePaymentReceivedPayload("test-hash-789", 75000UL);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/breez/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Breez-Signature", "sha256=invalid-signature");
        request.Headers.Add("X-Breez-Timestamp", timestamp);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddBreezSdkOffline_RegistersOfflineService()
    {
        // Arrange
        _app = await CreateOfflineTestApplicationAsync();

        // Act
        var sdkService = _app.Services.GetService<IBreezSdkService>();

        // Assert
        sdkService.Should().NotBeNull();
    }

    [Fact]
    public async Task WebhookMiddleware_WithStaleTimestamp_Returns401()
    {
        // Arrange
        _app = await CreateTestApplicationAsync();
        var client = _app.GetTestClient();
        var payload = CreatePaymentReceivedPayload("stale-hash-ghi", 20000UL);

        // Create signature with a timestamp from 10 minutes ago (beyond 5 minute window)
        var staleTimestamp = (DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds()).ToString();
        var dataToSign = $"{staleTimestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(TestWebhookSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign));
        var signature = $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/breez/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Breez-Signature", signature);
        request.Headers.Add("X-Breez-Timestamp", staleTimestamp);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WebhookMiddleware_WithValidSignature_PassesToEndpoint()
    {
        // Arrange
        _app = await CreateTestApplicationAsync();
        var client = _app.GetTestClient();
        var payload = CreatePaymentReceivedPayload("valid-hash-xyz", 30000UL);
        var (signature, timestamp) = CreateValidSignature(payload);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/breez/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Breez-Signature", signature);
        request.Headers.Add("X-Breez-Timestamp", timestamp);

        // Act
        var response = await client.SendAsync(request);

        // Assert - if it passes middleware, endpoint will process it
        // Response should be OK (200) if the endpoint processes successfully
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<WebApplication> CreateTestApplicationAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        var config = new Dictionary<string, string?>
        {
            ["BreezSdk:ApiKey"] = TestApiKey,
            ["BreezSdk:WebhookSecret"] = TestWebhookSecret,
            ["BreezSdk:Network"] = "Testnet",
            ["BreezSdk:OfflineMode"] = "true"
        };
        builder.Configuration.AddInMemoryCollection(config);
        builder.Services.AddRouting();

        builder.AddBreezSdkOffline(options =>
        {
            options.ApiKey = TestApiKey;
            options.WebhookSecret = TestWebhookSecret;
        });

        var app = builder.Build();

        // Use middleware for webhook path validation
        app.UseWhen(
            context => context.Request.Path.StartsWithSegments("/api/breez/webhook"),
            appBuilder => appBuilder.UseMiddleware<WebhookValidationMiddleware>());

        app.MapBreezSdkEndpoints();

        await app.StartAsync();

        return app;
    }

    private static async Task<WebApplication> CreateOfflineTestApplicationAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        var config = new Dictionary<string, string?>
        {
            ["BreezSdk:OfflineMode"] = "true"
        };
        builder.Configuration.AddInMemoryCollection(config);
        builder.Services.AddRouting();

        builder.AddBreezSdkOffline();

        var app = builder.Build();

        await app.StartAsync();

        return app;
    }

    private static string CreatePaymentReceivedPayload(string paymentHash, ulong amountSat)
    {
        return JsonSerializer.Serialize(new
        {
            eventType = "payment_received",
            paymentHash = paymentHash,
            amountSat = amountSat,
            timestamp = DateTimeOffset.UtcNow
        });
    }

    private static (string signature, string timestamp) CreateValidSignature(string payload)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var dataToSign = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(TestWebhookSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign));
        var signature = $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";
        return (signature, timestamp);
    }

    public async ValueTask DisposeAsync()
    {
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
