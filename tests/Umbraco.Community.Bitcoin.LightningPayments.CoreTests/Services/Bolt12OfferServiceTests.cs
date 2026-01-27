using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Bolt12;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Breez;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Exceptions;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Services;

/// <summary>
/// Unit tests for Bolt12OfferService.
/// Tests verify Bolt12 offer lifecycle management per management-api.yaml contract.
///
/// IMPORTANT: These tests are written FIRST (TDD Red phase) and will FAIL until
/// IBolt12OfferService and Bolt12OfferService are implemented.
/// </summary>
public class Bolt12OfferServiceTests : IDisposable
{
    private readonly PaymentDbContext _context;
    private readonly Mock<IBreezSdkService> _breezSdkServiceMock;
    private readonly Mock<ILogger<Bolt12OfferService>> _loggerMock;
    private readonly IBolt12OfferService _sut;

    public Bolt12OfferServiceTests()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase($"Bolt12OfferServiceTest_{Guid.NewGuid()}")
            .Options;
        _context = new PaymentDbContext(options);
        _context.Database.EnsureCreated();

        _breezSdkServiceMock = new Mock<IBreezSdkService>();
        _loggerMock = new Mock<ILogger<Bolt12OfferService>>();

        _sut = new Bolt12OfferService(
            _context,
            _breezSdkServiceMock.Object,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region CreateOfferAsync Tests

    [Fact]
    public async Task CreateOfferAsync_WithValidDescription_CreatesOfferInDatabaseAndReturnsIt()
    {
        // Arrange
        var description = "Monthly subscription";
        var expectedOfferString = "lno1qgsqvjlwvejwxzrfq0test";

        _breezSdkServiceMock
            .Setup(s => s.CreateBolt12OfferAsync(It.IsAny<ulong>(), description, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOfferString);

        // Act
        var result = await _sut.CreateOfferAsync(description, amountSat: null, contentId: null);

        // Assert
        result.Should().NotBeNull();
        result.OfferString.Should().Be(expectedOfferString);
        result.Description.Should().Be(description);
        result.IsActive.Should().BeTrue();
        result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        result.DeactivatedAt.Should().BeNull();
        result.AmountSat.Should().BeNull();
        result.ContentId.Should().BeNull();

        // Verify persisted to database
        var dbOffer = await _context.Bolt12Offers.FindAsync(result.OfferId);
        dbOffer.Should().NotBeNull();
        dbOffer!.OfferString.Should().Be(expectedOfferString);
    }

    [Fact]
    public async Task CreateOfferAsync_WithAmountSat_PassesAmountToSdk()
    {
        // Arrange
        ulong amountSat = 50_000;
        var description = "Fixed price offer";

        _breezSdkServiceMock
            .Setup(s => s.CreateBolt12OfferAsync(amountSat, description, It.IsAny<CancellationToken>()))
            .ReturnsAsync("lno1qgsqvjl_fixed_amount");

        // Act
        var result = await _sut.CreateOfferAsync(description, amountSat, contentId: null);

        // Assert
        result.AmountSat.Should().Be(amountSat);
        _breezSdkServiceMock.Verify(
            s => s.CreateBolt12OfferAsync(amountSat, description, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateOfferAsync_WithNullAmount_CreatesVariableAmountOffer()
    {
        // Arrange
        _breezSdkServiceMock
            .Setup(s => s.CreateBolt12OfferAsync(0, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("lno1qgsqvjl_variable");

        // Act
        var result = await _sut.CreateOfferAsync("Donation jar", amountSat: null, contentId: null);

        // Assert
        result.AmountSat.Should().BeNull();
        _breezSdkServiceMock.Verify(
            s => s.CreateBolt12OfferAsync(0, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateOfferAsync_WithContentId_AssociatesOfferWithContent()
    {
        // Arrange
        int contentId = 42;
        _breezSdkServiceMock
            .Setup(s => s.CreateBolt12OfferAsync(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("lno1qgsqvjl_content");

        // Act
        var result = await _sut.CreateOfferAsync("Content subscription", amountSat: 10_000, contentId: contentId);

        // Assert
        result.ContentId.Should().Be(contentId);
    }

    [Fact]
    public async Task CreateOfferAsync_WhenSdkThrows_PropagatesException()
    {
        // Arrange
        _breezSdkServiceMock
            .Setup(s => s.CreateBolt12OfferAsync(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BreezSdkConnectionException("SDK not connected"));

        // Act
        var act = async () => await _sut.CreateOfferAsync("Test", amountSat: null, contentId: null);

        // Assert
        await act.Should().ThrowAsync<BreezSdkConnectionException>()
            .WithMessage("SDK not connected");
    }

    #endregion

    #region GetOfferByIdAsync Tests

    [Fact]
    public async Task GetOfferByIdAsync_WithExistingOffer_ReturnsOffer()
    {
        // Arrange
        var offer = await SeedOffer("Test offer", 1000, isActive: true);

        // Act
        var result = await _sut.GetOfferByIdAsync(offer.OfferId);

        // Assert
        result.Should().NotBeNull();
        result!.OfferId.Should().Be(offer.OfferId);
        result.Description.Should().Be("Test offer");
        result.AmountSat.Should().Be(1000UL);
    }

    [Fact]
    public async Task GetOfferByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _sut.GetOfferByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetOffersAsync Tests

    [Fact]
    public async Task GetOffersAsync_ActiveOnly_ReturnsOnlyActiveOffers()
    {
        // Arrange
        await SeedOffer("Active offer 1", 1000, isActive: true);
        await SeedOffer("Active offer 2", 2000, isActive: true);
        await SeedOffer("Inactive offer", 3000, isActive: false);

        // Act
        var result = await _sut.GetOffersAsync(activeOnly: true);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(o => o.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task GetOffersAsync_IncludeInactive_ReturnsAllOffers()
    {
        // Arrange
        await SeedOffer("Active offer", 1000, isActive: true);
        await SeedOffer("Inactive offer", 2000, isActive: false);

        // Act
        var result = await _sut.GetOffersAsync(activeOnly: false);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOffersAsync_WithNoOffers_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetOffersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region DeactivateOfferAsync Tests

    [Fact]
    public async Task DeactivateOfferAsync_WithActiveOffer_SetsIsActiveFalseAndDeactivatedAt()
    {
        // Arrange
        var offer = await SeedOffer("Active offer", 1000, isActive: true);

        // Act
        await _sut.DeactivateOfferAsync(offer.OfferId);

        // Assert
        var dbOffer = await _context.Bolt12Offers.FindAsync(offer.OfferId);
        dbOffer.Should().NotBeNull();
        dbOffer!.IsActive.Should().BeFalse();
        dbOffer.DeactivatedAt.Should().NotBeNull();
        dbOffer.DeactivatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeactivateOfferAsync_WithNonExistentOffer_ThrowsPaymentNotFoundException()
    {
        // Act
        var act = async () => await _sut.DeactivateOfferAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<PaymentNotFoundException>();
    }

    [Fact]
    public async Task DeactivateOfferAsync_WithAlreadyInactiveOffer_DoesNotThrow()
    {
        // Arrange
        var offer = await SeedOffer("Inactive offer", 1000, isActive: false);

        // Act
        var act = async () => await _sut.DeactivateOfferAsync(offer.OfferId);

        // Assert - idempotent operation should not throw
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region GetOfferPaymentSummaryAsync Tests

    [Fact]
    public async Task GetOfferPaymentSummaryAsync_WithPayments_ReturnsCorrectCounts()
    {
        // Arrange
        var offer = await SeedOffer("Offer with payments", 1000, isActive: true);
        await SeedPaymentForOffer(offer.OfferId, "hash1", PaymentStatus.Paid, 1000);
        await SeedPaymentForOffer(offer.OfferId, "hash2", PaymentStatus.Paid, 2000);
        await SeedPaymentForOffer(offer.OfferId, "hash3", PaymentStatus.Pending, 500);
        await SeedPaymentForOffer(offer.OfferId, "hash4", PaymentStatus.Failed, 750);

        // Act
        var (paymentCount, totalReceivedSat, confirmedCount) =
            await _sut.GetOfferPaymentSummaryAsync(offer.OfferId);

        // Assert
        paymentCount.Should().Be(4);
        totalReceivedSat.Should().Be(4250); // 1000 + 2000 + 500 + 750
        confirmedCount.Should().Be(2); // Only Paid status
    }

    [Fact]
    public async Task GetOfferPaymentSummaryAsync_WithNoPayments_ReturnsZeroCounts()
    {
        // Arrange
        var offer = await SeedOffer("Empty offer", 1000, isActive: true);

        // Act
        var (paymentCount, totalReceivedSat, confirmedCount) =
            await _sut.GetOfferPaymentSummaryAsync(offer.OfferId);

        // Assert
        paymentCount.Should().Be(0);
        totalReceivedSat.Should().Be(0);
        confirmedCount.Should().Be(0);
    }

    [Fact]
    public async Task GetOfferPaymentSummaryAsync_WithNonExistentOffer_ThrowsPaymentNotFoundException()
    {
        // Act
        var act = async () => await _sut.GetOfferPaymentSummaryAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<PaymentNotFoundException>();
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new Bolt12OfferService(
            null!,
            _breezSdkServiceMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Constructor_WithNullBreezSdkService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new Bolt12OfferService(
            _context,
            null!,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("breezSdkService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new Bolt12OfferService(
            _context,
            _breezSdkServiceMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Helpers

    private async Task<Bolt12Offer> SeedOffer(string description, ulong? amountSat, bool isActive)
    {
        var offer = new Bolt12Offer
        {
            OfferId = Guid.NewGuid(),
            OfferString = $"lno1qgsqvjl_{Guid.NewGuid():N}",
            Description = description,
            AmountSat = amountSat,
            IsActive = isActive,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            DeactivatedAt = isActive ? null : DateTimeOffset.UtcNow.AddHours(-1)
        };

        _context.Bolt12Offers.Add(offer);
        await _context.SaveChangesAsync();
        return offer;
    }

    private async Task SeedPaymentForOffer(Guid offerId, string paymentHash, PaymentStatus status, ulong amountSat)
    {
        var payment = new PaymentState
        {
            PaymentHash = paymentHash,
            Status = status,
            AmountSat = amountSat,
            ContentId = 0,
            UserSessionId = $"session-{Guid.NewGuid()}",
            Kind = PaymentKind.Paywall,
            Bolt12OfferId = offerId
        };

        _context.PaymentStates.Add(payment);
        await _context.SaveChangesAsync();
    }

    #endregion
}
