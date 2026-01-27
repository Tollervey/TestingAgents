using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Payment;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Services.Payment;

public class LatePaymentHandlerTests
{
    private readonly Mock<ILogger<LatePaymentHandler>> _loggerMock = new();

    [Fact]
    public async Task HandleLatePaymentAsync_LogsWarning()
    {
        var handler = new LatePaymentHandler(_loggerMock.Object);

        await handler.HandleLatePaymentAsync("test-hash-123");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Late payment received")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleLatePaymentAsync_DoesNotThrow()
    {
        var handler = new LatePaymentHandler(_loggerMock.Object);

        var act = () => handler.HandleLatePaymentAsync("test-hash");

        await act.Should().NotThrowAsync();
    }
}

public class ConfirmPaymentLatePaymentTests : IDisposable
{
    private readonly PaymentDbContext _context;

    public ConfirmPaymentLatePaymentTests()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: $"LatePaymentTest_{Guid.NewGuid()}")
            .Options;
        _context = new PaymentDbContext(options);
    }

    [Fact]
    public async Task ConfirmPaymentAsync_ExpiredPayment_ReturnsConfirmedLatePayment()
    {
        var paymentHash = $"hash-{Guid.NewGuid():N}";
        _context.PaymentStates.Add(new PaymentState
        {
            PaymentHash = paymentHash,
            ContentId = 1,
            UserSessionId = "session-1",
            Status = PaymentStatus.Expired,
            AmountSat = 1000,
            Kind = PaymentKind.Paywall
        });
        await _context.SaveChangesAsync();

        var service = new PersistentPaymentStateService(_context);
        var result = await service.ConfirmPaymentAsync(paymentHash);

        result.Should().Be(PaymentConfirmationResult.ConfirmedLatePayment);

        var updated = await _context.PaymentStates.FirstAsync(p => p.PaymentHash == paymentHash);
        updated.Status.Should().Be(PaymentStatus.Paid);
    }

    [Fact]
    public async Task ConfirmPaymentAsync_PendingPayment_ReturnsConfirmed()
    {
        var paymentHash = $"hash-{Guid.NewGuid():N}";
        _context.PaymentStates.Add(new PaymentState
        {
            PaymentHash = paymentHash,
            ContentId = 1,
            UserSessionId = "session-1",
            Status = PaymentStatus.Pending,
            AmountSat = 1000,
            Kind = PaymentKind.Paywall
        });
        await _context.SaveChangesAsync();

        var service = new PersistentPaymentStateService(_context);
        var result = await service.ConfirmPaymentAsync(paymentHash);

        result.Should().Be(PaymentConfirmationResult.Confirmed);
    }

    [Fact]
    public async Task ConfirmPaymentAsync_FailedPayment_ReturnsNotFound()
    {
        var paymentHash = $"hash-{Guid.NewGuid():N}";
        _context.PaymentStates.Add(new PaymentState
        {
            PaymentHash = paymentHash,
            ContentId = 1,
            UserSessionId = "session-1",
            Status = PaymentStatus.Failed,
            AmountSat = 1000,
            Kind = PaymentKind.Paywall
        });
        await _context.SaveChangesAsync();

        var service = new PersistentPaymentStateService(_context);
        var result = await service.ConfirmPaymentAsync(paymentHash);

        result.Should().Be(PaymentConfirmationResult.NotFound);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
