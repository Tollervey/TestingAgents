using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Payment;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Services.Payment;

public class ContentUnpublishedPaymentHandlerTests : IDisposable
{
    private readonly PaymentDbContext _context;
    private readonly PersistentPaymentStateService _stateService;
    private readonly ContentUnpublishedPaymentHandler _handler;
    private readonly Mock<ILogger<ContentUnpublishedPaymentHandler>> _loggerMock = new();

    public ContentUnpublishedPaymentHandlerTests()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase($"ContentUnpublishedTest_{Guid.NewGuid()}")
            .Options;
        _context = new PaymentDbContext(options);
        _stateService = new PersistentPaymentStateService(_context);
        _handler = new ContentUnpublishedPaymentHandler(_stateService, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleContentUnpublished_MarksPendingPaymentsAsFailed()
    {
        var contentId = 42;
        _context.PaymentStates.Add(new PaymentState
        {
            PaymentHash = $"hash-{Guid.NewGuid():N}",
            ContentId = contentId,
            UserSessionId = "s1",
            Status = PaymentStatus.Pending,
            AmountSat = 1000,
            Kind = PaymentKind.Paywall
        });
        _context.PaymentStates.Add(new PaymentState
        {
            PaymentHash = $"hash-{Guid.NewGuid():N}",
            ContentId = contentId,
            UserSessionId = "s2",
            Status = PaymentStatus.Pending,
            AmountSat = 2000,
            Kind = PaymentKind.Paywall
        });
        await _context.SaveChangesAsync();

        await _handler.HandleContentUnpublishedAsync(contentId);

        var payments = await _context.PaymentStates
            .Where(p => p.ContentId == contentId)
            .ToListAsync();
        payments.Should().AllSatisfy(p => p.Status.Should().Be(PaymentStatus.Failed));
    }

    [Fact]
    public async Task HandleContentUnpublished_DoesNotAffectNonPendingPayments()
    {
        var contentId = 43;
        _context.PaymentStates.Add(new PaymentState
        {
            PaymentHash = $"hash-{Guid.NewGuid():N}",
            ContentId = contentId,
            UserSessionId = "s1",
            Status = PaymentStatus.Paid,
            AmountSat = 1000,
            Kind = PaymentKind.Paywall
        });
        await _context.SaveChangesAsync();

        await _handler.HandleContentUnpublishedAsync(contentId);

        var payment = await _context.PaymentStates.FirstAsync(p => p.ContentId == contentId);
        payment.Status.Should().Be(PaymentStatus.Paid);
    }

    [Fact]
    public async Task HandleContentUnpublished_NoError_WhenNoPendingPayments()
    {
        var act = () => _handler.HandleContentUnpublishedAsync(999);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetPendingPaymentsByContentId_ReturnsOnlyPending()
    {
        var contentId = 44;
        _context.PaymentStates.Add(new PaymentState
        {
            PaymentHash = $"hash-{Guid.NewGuid():N}",
            ContentId = contentId,
            UserSessionId = "s1",
            Status = PaymentStatus.Pending,
            AmountSat = 1000,
            Kind = PaymentKind.Paywall
        });
        _context.PaymentStates.Add(new PaymentState
        {
            PaymentHash = $"hash-{Guid.NewGuid():N}",
            ContentId = contentId,
            UserSessionId = "s2",
            Status = PaymentStatus.Paid,
            AmountSat = 2000,
            Kind = PaymentKind.Paywall
        });
        await _context.SaveChangesAsync();

        var result = (await _stateService.GetPendingPaymentsByContentIdAsync(contentId)).ToList();

        result.Should().HaveCount(1);
        result[0].Status.Should().Be(PaymentStatus.Pending);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
