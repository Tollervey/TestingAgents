using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Payment;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Services.Payment
{
    /// <summary>
    /// Unit tests for InMemoryPaymentStateService.
    /// </summary>
    public class InMemoryPaymentStateServiceTests
    {
        private InMemoryPaymentStateService CreateService() => new();

        #region AddPendingPaymentAsync Tests

        [Fact]
        public async Task AddPendingPaymentAsync_WithValidPaywallContent_AddsPayment()
        {
            // Arrange
            var service = CreateService();
            var paymentHash = "hash123";
            var contentId = 1;
            var userSessionId = "session123";

            // Act
            await service.AddPendingPaymentAsync(paymentHash, contentId, userSessionId);

            // Assert
            var state = await service.GetPaymentStateAsync(userSessionId, contentId);
            Assert.NotNull(state);
            Assert.Equal(paymentHash, state.PaymentHash);
            Assert.Equal(contentId, state.ContentId);
            Assert.Equal(userSessionId, state.UserSessionId);
            Assert.Equal(PaymentStatus.Pending, state.Status);
            Assert.Equal(PaymentKind.Paywall, state.Kind);
        }

        [Fact]
        public async Task AddPendingPaymentAsync_WithTipContent_DoesNotMapSession()
        {
            // Arrange
            var service = CreateService();
            var paymentHash = "hash123";
            var contentId = 0; // Tip
            var userSessionId = "session123";

            // Act
            await service.AddPendingPaymentAsync(paymentHash, contentId, userSessionId);

            // Assert
            var state = await service.GetPaymentStateAsync(userSessionId, contentId);
            Assert.Null(state); // Tips not linked to content
        }

        [Fact]
        public async Task AddPendingPaymentAsync_DuplicatePaywallContent_ReplacesExisting()
        {
            // Arrange
            var service = CreateService();
            var contentId = 1;
            var userSessionId = "session123";
            var oldHash = "oldHash";
            var newHash = "newHash";

            // Act
            await service.AddPendingPaymentAsync(oldHash, contentId, userSessionId);
            await service.AddPendingPaymentAsync(newHash, contentId, userSessionId);

            // Assert
            var state = await service.GetPaymentStateAsync(userSessionId, contentId);
            Assert.NotNull(state);
            Assert.Equal(newHash, state.PaymentHash);
        }

        #endregion

        #region SetPaymentMetadataAsync Tests

        [Fact]
        public async Task SetPaymentMetadataAsync_ExistingPayment_UpdatesMetadata()
        {
            // Arrange
            var service = CreateService();
            var paymentHash = "hash123";
            var contentId = 1;
            var userSessionId = "session123";
            await service.AddPendingPaymentAsync(paymentHash, contentId, userSessionId);

            // Act
            await service.SetPaymentMetadataAsync(paymentHash, 1000, PaymentKind.Tip);

            // Assert
            var state = await service.GetByPaymentHashAsync(paymentHash);
            Assert.NotNull(state);
            Assert.Equal(1000UL, state.AmountSat);
            Assert.Equal(PaymentKind.Tip, state.Kind);
        }

        [Fact]
        public async Task SetPaymentMetadataAsync_NonExistingPayment_DoesNothing()
        {
            // Arrange
            var service = CreateService();

            // Act
            await service.SetPaymentMetadataAsync("nonexistent", 1000, PaymentKind.Tip);

            // Assert - No exception, no change
            Assert.True(true);
        }

        #endregion

        #region ConfirmPaymentAsync Tests

        [Fact]
        public async Task ConfirmPaymentAsync_PendingPayment_ConfirmsAndReturnsConfirmed()
        {
            // Arrange
            var service = CreateService();
            var paymentHash = "hash123";
            var contentId = 1;
            var userSessionId = "session123";
            await service.AddPendingPaymentAsync(paymentHash, contentId, userSessionId);

            // Act
            var result = await service.ConfirmPaymentAsync(paymentHash);

            // Assert
            Assert.Equal(PaymentConfirmationResult.Confirmed, result);
            var state = await service.GetByPaymentHashAsync(paymentHash);
            Assert.Equal(PaymentStatus.Paid, state?.Status);
        }

        [Fact]
        public async Task ConfirmPaymentAsync_AlreadyPaid_ReturnsAlreadyConfirmed()
        {
            // Arrange
            var service = CreateService();
            var paymentHash = "hash123";
            var contentId = 1;
            var userSessionId = "session123";
            await service.AddPendingPaymentAsync(paymentHash, contentId, userSessionId);
            await service.ConfirmPaymentAsync(paymentHash);

            // Act
            var result = await service.ConfirmPaymentAsync(paymentHash);

            // Assert
            Assert.Equal(PaymentConfirmationResult.AlreadyConfirmed, result);
        }

        [Fact]
        public async Task ConfirmPaymentAsync_NonExistingPayment_ReturnsNotFound()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.ConfirmPaymentAsync("nonexistent");

            // Assert
            Assert.Equal(PaymentConfirmationResult.NotFound, result);
        }

        [Fact]
        public async Task ConfirmPaymentAsync_FailedPayment_ReturnsNotFound()
        {
            // Arrange
            var service = CreateService();
            var paymentHash = "hash123";
            var contentId = 1;
            var userSessionId = "session123";
            await service.AddPendingPaymentAsync(paymentHash, contentId, userSessionId);
            await service.MarkAsFailedAsync(paymentHash);

            // Act
            var result = await service.ConfirmPaymentAsync(paymentHash);

            // Assert
            Assert.Equal(PaymentConfirmationResult.NotFound, result);
        }

        #endregion

        #region GetPaymentStateAsync Tests

        [Fact]
        public async Task GetPaymentStateAsync_ExistingPaywallPayment_ReturnsState()
        {
            // Arrange
            var service = CreateService();
            var paymentHash = "hash123";
            var contentId = 1;
            var userSessionId = "session123";
            await service.AddPendingPaymentAsync(paymentHash, contentId, userSessionId);

            // Act
            var state = await service.GetPaymentStateAsync(userSessionId, contentId);

            // Assert
            Assert.NotNull(state);
            Assert.Equal(paymentHash, state.PaymentHash);
        }

        [Fact]
        public async Task GetPaymentStateAsync_TipContent_ReturnsNull()
        {
            // Arrange
            var service = CreateService();

            // Act
            var state = await service.GetPaymentStateAsync("session", 0);

            // Assert
            Assert.Null(state);
        }

        [Fact]
        public async Task GetPaymentStateAsync_NonExisting_ReturnsNull()
        {
            // Arrange
            var service = CreateService();

            // Act
            var state = await service.GetPaymentStateAsync("session", 1);

            // Assert
            Assert.Null(state);
        }

        #endregion

        #region GetAllPaymentsAsync Tests

        [Fact]
        public async Task GetAllPaymentsAsync_NoPayments_ReturnsEmpty()
        {
            // Arrange
            var service = CreateService();

            // Act
            var payments = await service.GetAllPaymentsAsync();

            // Assert
            Assert.Empty(payments);
        }

        [Fact]
        public async Task GetAllPaymentsAsync_WithPayments_ReturnsAll()
        {
            // Arrange
            var service = CreateService();
            await service.AddPendingPaymentAsync("hash1", 1, "session1");
            await service.AddPendingPaymentAsync("hash2", 2, "session2");

            // Act
            var payments = await service.GetAllPaymentsAsync();

            // Assert
            Assert.Equal(2, payments.Count());
        }

        #endregion

        #region MarkAsFailedAsync Tests

        [Fact]
        public async Task MarkAsFailedAsync_ExistingPayment_ReturnsTrueAndUpdatesStatus()
        {
            // Arrange
            var service = CreateService();
            var paymentHash = "hash123";
            var contentId = 1;
            var userSessionId = "session123";
            await service.AddPendingPaymentAsync(paymentHash, contentId, userSessionId);

            // Act
            var result = await service.MarkAsFailedAsync(paymentHash);

            // Assert
            Assert.True(result);
            var state = await service.GetByPaymentHashAsync(paymentHash);
            Assert.Equal(PaymentStatus.Failed, state?.Status);
        }

        [Fact]
        public async Task MarkAsFailedAsync_NonExistingPayment_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.MarkAsFailedAsync("nonexistent");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region MarkAsExpiredAsync Tests

        [Fact]
        public async Task MarkAsExpiredAsync_ExistingPayment_ReturnsTrueAndUpdatesStatus()
        {
            // Arrange
            var service = CreateService();
            var paymentHash = "hash123";
            var contentId = 1;
            var userSessionId = "session123";
            await service.AddPendingPaymentAsync(paymentHash, contentId, userSessionId);

            // Act
            var result = await service.MarkAsExpiredAsync(paymentHash);

            // Assert
            Assert.True(result);
            var state = await service.GetByPaymentHashAsync(paymentHash);
            Assert.Equal(PaymentStatus.Expired, state?.Status);
        }

        [Fact]
        public async Task MarkAsExpiredAsync_NonExistingPayment_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.MarkAsExpiredAsync("nonexistent");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region MarkAsRefundPendingAsync Tests

        [Fact]
        public async Task MarkAsRefundPendingAsync_ExistingPayment_ReturnsTrueAndUpdatesStatus()
        {
            // Arrange
            var service = CreateService();
            var paymentHash = "hash123";
            var contentId = 1;
            var userSessionId = "session123";
            await service.AddPendingPaymentAsync(paymentHash, contentId, userSessionId);

            // Act
            var result = await service.MarkAsRefundPendingAsync(paymentHash);

            // Assert
            Assert.True(result);
            var state = await service.GetByPaymentHashAsync(paymentHash);
            Assert.Equal(PaymentStatus.RefundPending, state?.Status);
        }

        [Fact]
        public async Task MarkAsRefundPendingAsync_NonExistingPayment_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.MarkAsRefundPendingAsync("nonexistent");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region MarkAsRefundedAsync Tests

        [Fact]
        public async Task MarkAsRefundedAsync_ExistingPayment_ReturnsTrueAndUpdatesStatus()
        {
            // Arrange
            var service = CreateService();
            var paymentHash = "hash123";
            var contentId = 1;
            var userSessionId = "session123";
            await service.AddPendingPaymentAsync(paymentHash, contentId, userSessionId);

            // Act
            var result = await service.MarkAsRefundedAsync(paymentHash);

            // Assert
            Assert.True(result);
            var state = await service.GetByPaymentHashAsync(paymentHash);
            Assert.Equal(PaymentStatus.Refunded, state?.Status);
        }

        [Fact]
        public async Task MarkAsRefundedAsync_NonExistingPayment_ReturnsFalse()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.MarkAsRefundedAsync("nonexistent");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetByPaymentHashAsync Tests

        [Fact]
        public async Task GetByPaymentHashAsync_ExistingPayment_ReturnsState()
        {
            // Arrange
            var service = CreateService();
            var paymentHash = "hash123";
            var contentId = 1;
            var userSessionId = "session123";
            await service.AddPendingPaymentAsync(paymentHash, contentId, userSessionId);

            // Act
            var state = await service.GetByPaymentHashAsync(paymentHash);

            // Assert
            Assert.NotNull(state);
            Assert.Equal(paymentHash, state.PaymentHash);
        }

        [Fact]
        public async Task GetByPaymentHashAsync_NonExistingPayment_ReturnsNull()
        {
            // Arrange
            var service = CreateService();

            // Act
            var state = await service.GetByPaymentHashAsync("nonexistent");

            // Assert
            Assert.Null(state);
        }

        #endregion

        #region TryGetMappingByKeyAsync Tests

        [Fact]
        public async Task TryGetMappingByKeyAsync_ExistingMapping_ReturnsMapping()
        {
            // Arrange
            var service = CreateService();
            var key = "key123";
            var paymentHash = "hash123";
            var invoice = "invoice123";
            await service.TryCreateMappingAsync(key, paymentHash, invoice);

            // Act
            var mapping = await service.TryGetMappingByKeyAsync(key);

            // Assert
            Assert.NotNull(mapping);
            Assert.Equal(key, mapping.IdempotencyKey);
            Assert.Equal(paymentHash, mapping.PaymentHash);
            Assert.Equal(invoice, mapping.Invoice);
        }

        [Fact]
        public async Task TryGetMappingByKeyAsync_NonExistingMapping_ReturnsNull()
        {
            // Arrange
            var service = CreateService();

            // Act
            var mapping = await service.TryGetMappingByKeyAsync("nonexistent");

            // Assert
            Assert.Null(mapping);
        }

        #endregion

        #region TryCreateMappingAsync Tests

        [Fact]
        public async Task TryCreateMappingAsync_NewMapping_CreatesAndReturnsCreated()
        {
            // Arrange
            var service = CreateService();
            var key = "key123";
            var paymentHash = "hash123";
            var invoice = "invoice123";

            // Act
            var (mapping, created) = await service.TryCreateMappingAsync(key, paymentHash, invoice);

            // Assert
            Assert.True(created);
            Assert.NotNull(mapping);
            Assert.Equal(key, mapping.IdempotencyKey);
            Assert.Equal(paymentHash, mapping.PaymentHash);
            Assert.Equal(invoice, mapping.Invoice);
            Assert.Equal(PaymentStatus.Pending, mapping.Status);
        }

        [Fact]
        public async Task TryCreateMappingAsync_ExistingMapping_ReturnsExistingAndNotCreated()
        {
            // Arrange
            var service = CreateService();
            var key = "key123";
            var paymentHash1 = "hash1";
            var invoice1 = "invoice1";
            var paymentHash2 = "hash2";
            var invoice2 = "invoice2";
            await service.TryCreateMappingAsync(key, paymentHash1, invoice1);

            // Act
            var (mapping, created) = await service.TryCreateMappingAsync(key, paymentHash2, invoice2);

            // Assert
            Assert.False(created);
            Assert.NotNull(mapping);
            Assert.Equal(key, mapping.IdempotencyKey);
            Assert.Equal(paymentHash1, mapping.PaymentHash);
            Assert.Equal(invoice1, mapping.Invoice);
        }

        #endregion

        #region IsServiceHealthyAsync Tests

        [Fact]
        public async Task IsServiceHealthyAsync_ReturnsTrue()
        {
            // Arrange
            var service = CreateService();

            // Act
            var healthy = await service.IsServiceHealthyAsync();

            // Assert
            Assert.True(healthy);
        }

        #endregion
    }
}