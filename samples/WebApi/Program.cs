using Breez.Sdk.Liquid.Extensions.AspNetCore.Endpoints;
using Breez.Sdk.Liquid.Extensions.AspNetCore.Extensions;
using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "BreezSDK Sample API", Version = "v1" });
});

// Add BreezSDK services (offline mode for development)
builder.AddBreezSdkOffline();

// Or use AddBreezSdk() for production
// builder.AddBreezSdk();

// Add health checks
builder.Services.AddHealthChecks()
    .AddBreezSdkHealthCheck();

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map BreezSDK webhook endpoints
app.MapBreezSdkEndpoints();

// Health check endpoints
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

// Invoice endpoints
app.MapPost("/api/invoices", async (
    [FromBody] CreateInvoiceRequest request,
    IBreezSdkService breezService,
    CancellationToken ct) =>
{
    var result = await breezService.CreateInvoiceAsync(
        request.AmountSat,
        request.Description,
        request.ExpirySec,
        ct);

    if (result.IsFailure)
    {
        return Results.BadRequest(new
        {
            error = result.Error!.Code.ToString(),
            message = result.Error.Message
        });
    }

    return Results.Ok(new
    {
        paymentHash = result.Value!.PaymentHash,
        invoice = result.Value.Destination,
        amountSat = result.Value.AmountSat,
        expiresAt = result.Value.ExpiresAt
    });
})
.WithName("CreateInvoice")
.WithTags("Invoices");

app.MapGet("/api/invoices/{paymentHash}", async (
    string paymentHash,
    IBreezSdkService breezService,
    CancellationToken ct) =>
{
    var payment = await breezService.GetPaymentByHashAsync(paymentHash, ct);

    if (payment is null)
    {
        return Results.NotFound(new { error = "Payment not found" });
    }

    return Results.Ok(new
    {
        paymentHash = payment.PaymentHash,
        status = payment.Status.ToString(),
        amountSat = payment.AmountSat,
        createdAt = payment.CreatedAt,
        confirmedAt = payment.ConfirmedAt
    });
})
.WithName("GetPayment")
.WithTags("Invoices");

app.MapGet("/api/invoices", async (
    [FromQuery] int offset = 0,
    [FromQuery] int limit = 50,
    IBreezSdkService? breezService = null,
    CancellationToken ct = default) =>
{
    if (breezService is null)
    {
        return Results.Problem("Service unavailable");
    }

    var payments = await breezService.GetPaymentHistoryAsync(offset, limit, ct);

    return Results.Ok(new
    {
        offset,
        limit,
        count = payments.Count,
        payments = payments.Select(p => new
        {
            paymentHash = p.PaymentHash,
            status = p.Status.ToString(),
            amountSat = p.AmountSat,
            createdAt = p.CreatedAt
        })
    });
})
.WithName("ListPayments")
.WithTags("Invoices");

app.MapGet("/api/balance", async (
    IBreezSdkService breezService,
    CancellationToken ct) =>
{
    var result = await breezService.GetBalanceAsync(ct);

    if (result.IsFailure)
    {
        return Results.BadRequest(new
        {
            error = result.Error!.Code.ToString(),
            message = result.Error.Message
        });
    }

    return Results.Ok(new { balanceSat = result.Value });
})
.WithName("GetBalance")
.WithTags("Wallet");

app.Run();

/// <summary>
/// Request model for creating an invoice.
/// </summary>
public record CreateInvoiceRequest(
    ulong AmountSat,
    string? Description = null,
    uint? ExpirySec = null);
