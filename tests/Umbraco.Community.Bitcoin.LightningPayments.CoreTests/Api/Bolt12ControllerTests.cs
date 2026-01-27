using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Management;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Management.Dto;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Bolt12;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Exceptions;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Api;

/// <summary>
/// Unit tests for Bolt12Controller.
/// Tests follow TDD RED-GREEN-REFACTOR pattern.
/// These tests are written FIRST and will FAIL until the implementation is created.
/// </summary>
public class Bolt12ControllerTests
{
    private readonly Mock<IBolt12OfferService> _bolt12OfferServiceMock;
    private readonly Bolt12Controller _sut;

    public Bolt12ControllerTests()
    {
        _bolt12OfferServiceMock = new Mock<IBolt12OfferService>();
        _sut = new Bolt12Controller(_bolt12OfferServiceMock.Object);
    }

    #region ListOffers Tests

    [Fact]
    public async Task ListOffers_WithActiveOnly_ReturnsOkWithActiveOffers()
    {
        // Arrange
        var offers = new List<Bolt12Offer>
        {
            CreateTestOffer("Active offer 1", 1000, isActive: true),
            CreateTestOffer("Active offer 2", 2000, isActive: true)
        };

        _bolt12OfferServiceMock
            .Setup(s => s.GetOffersAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(offers);

        // Act
        var result = await _sut.ListOffers(activeOnly: true, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as List<Bolt12OfferResponse>;
        response.Should().NotBeNull();
        response.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListOffers_IncludingInactive_ReturnsOkWithAllOffers()
    {
        // Arrange
        var offers = new List<Bolt12Offer>
        {
            CreateTestOffer("Active", 1000, isActive: true),
            CreateTestOffer("Inactive", 2000, isActive: false)
        };

        _bolt12OfferServiceMock
            .Setup(s => s.GetOffersAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(offers);

        // Act
        var result = await _sut.ListOffers(activeOnly: false, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as List<Bolt12OfferResponse>;
        response.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListOffers_WithNoOffers_ReturnsOkWithEmptyList()
    {
        // Arrange
        _bolt12OfferServiceMock
            .Setup(s => s.GetOffersAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Bolt12Offer>());

        // Act
        var result = await _sut.ListOffers(activeOnly: true, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as List<Bolt12OfferResponse>;
        response.Should().NotBeNull();
        response.Should().BeEmpty();
    }

    #endregion

    #region CreateOffer Tests

    [Fact]
    public async Task CreateOffer_WithValidRequest_ReturnsCreatedWithOffer()
    {
        // Arrange
        var request = new CreateOfferRequest
        {
            Description = "Monthly subscription",
            AmountSat = 50_000,
            ContentId = 42
        };

        var createdOffer = new Bolt12Offer
        {
            OfferId = Guid.NewGuid(),
            OfferString = "lno1qgsqvjlwvejwxzrfq0test",
            Description = request.Description,
            AmountSat = request.AmountSat,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            ContentId = request.ContentId
        };

        _bolt12OfferServiceMock
            .Setup(s => s.CreateOfferAsync(
                request.Description,
                request.AmountSat,
                request.ContentId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdOffer);

        // Act
        var result = await _sut.CreateOffer(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult!.StatusCode.Should().Be(201);

        var response = createdResult.Value as Bolt12OfferResponse;
        response.Should().NotBeNull();
        response!.OfferId.Should().Be(createdOffer.OfferId);
        response.OfferString.Should().Be(createdOffer.OfferString);
        response.Description.Should().Be(request.Description);
        response.AmountSat.Should().Be(request.AmountSat);
        response.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateOffer_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        var request = new CreateOfferRequest { Description = "Test" };

        _bolt12OfferServiceMock
            .Setup(s => s.CreateOfferAsync(
                It.IsAny<string>(),
                It.IsAny<ulong?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BreezSdkConnectionException("SDK not connected"));

        // Act
        var act = async () => await _sut.CreateOffer(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BreezSdkConnectionException>();
    }

    #endregion

    #region GetOffer Tests

    [Fact]
    public async Task GetOffer_WithExistingOffer_ReturnsOkWithOfferDetails()
    {
        // Arrange
        var offerId = Guid.NewGuid();
        var offer = CreateTestOffer("Test offer", 5000, isActive: true);
        offer.OfferId = offerId;

        _bolt12OfferServiceMock
            .Setup(s => s.GetOfferByIdAsync(offerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(offer);

        _bolt12OfferServiceMock
            .Setup(s => s.GetOfferPaymentSummaryAsync(offerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((3, 15_000L, 2));

        // Act
        var result = await _sut.GetOffer(offerId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as Bolt12OfferDetailsResponse;
        response.Should().NotBeNull();
        response!.OfferId.Should().Be(offerId);
        response.PaymentCount.Should().Be(3);
        response.TotalReceivedSat.Should().Be(15_000);
        response.ConfirmedCount.Should().Be(2);
    }

    [Fact]
    public async Task GetOffer_WithNonExistentOffer_ReturnsNotFound()
    {
        // Arrange
        var offerId = Guid.NewGuid();

        _bolt12OfferServiceMock
            .Setup(s => s.GetOfferByIdAsync(offerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bolt12Offer?)null);

        // Act
        var result = await _sut.GetOffer(offerId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region DeactivateOffer Tests

    [Fact]
    public async Task DeactivateOffer_WithExistingOffer_ReturnsNoContent()
    {
        // Arrange
        var offerId = Guid.NewGuid();

        _bolt12OfferServiceMock
            .Setup(s => s.DeactivateOfferAsync(offerId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.DeactivateOffer(offerId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _bolt12OfferServiceMock.Verify(
            s => s.DeactivateOfferAsync(offerId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeactivateOffer_WithNonExistentOffer_ReturnsNotFound()
    {
        // Arrange
        var offerId = Guid.NewGuid();

        _bolt12OfferServiceMock
            .Setup(s => s.DeactivateOfferAsync(offerId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PaymentNotFoundException($"Offer {offerId} not found"));

        // Act
        var result = await _sut.DeactivateOffer(offerId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region ListOfferPayments Tests

    [Fact]
    public async Task ListOfferPayments_WithExistingOffer_ReturnsOkWithPayments()
    {
        // Arrange
        var offerId = Guid.NewGuid();
        var offer = CreateTestOffer("Test", 1000, isActive: true);
        offer.OfferId = offerId;

        _bolt12OfferServiceMock
            .Setup(s => s.GetOfferByIdAsync(offerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(offer);

        // Act
        var result = await _sut.ListOfferPayments(offerId, skip: 0, take: 20, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ListOfferPayments_WithNonExistentOffer_ReturnsNotFound()
    {
        // Arrange
        var offerId = Guid.NewGuid();

        _bolt12OfferServiceMock
            .Setup(s => s.GetOfferByIdAsync(offerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bolt12Offer?)null);

        // Act
        var result = await _sut.ListOfferPayments(offerId, skip: 0, take: 20, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullBolt12OfferService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new Bolt12Controller(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("bolt12OfferService");
    }

    #endregion

    #region Helpers

    private static Bolt12Offer CreateTestOffer(string description, ulong? amountSat, bool isActive)
    {
        return new Bolt12Offer
        {
            OfferId = Guid.NewGuid(),
            OfferString = $"lno1qgsqvjl_{Guid.NewGuid():N}",
            Description = description,
            AmountSat = amountSat,
            IsActive = isActive,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            DeactivatedAt = isActive ? null : DateTimeOffset.UtcNow.AddHours(-1)
        };
    }

    #endregion
}
