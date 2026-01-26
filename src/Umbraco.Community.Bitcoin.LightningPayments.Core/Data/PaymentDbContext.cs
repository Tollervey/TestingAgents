using Microsoft.EntityFrameworkCore;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Data
{
    /// <summary>
    /// EF Core database context for persisting payment state.
    /// </summary>
    public class PaymentDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PaymentDbContext"/> with the given options.
        /// </summary>
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

        /// <summary>
        /// Payment states tracked by the system, keyed by <see cref="PaymentState.PaymentHash"/>.
        /// </summary>
        public DbSet<PaymentState> PaymentStates { get; set; }

        /// <summary>
        /// Idempotency key mappings.
        /// </summary>
        public DbSet<IdempotencyMapping> IdempotencyMappings { get; set; }

        /// <summary>
        /// BOLT12 offers for recurring payments.
        /// </summary>
        public DbSet<Bolt12Offer> Bolt12Offers { get; set; }

        /// <summary>
        /// Refund transactions linked to original payments.
        /// </summary>
        public DbSet<RefundTransaction> RefundTransactions { get; set; }

        /// <summary>
        /// Payment notifications (email/webhook) tracking.
        /// </summary>
        public DbSet<PaymentNotification> PaymentNotifications { get; set; }

        /// <summary>
        /// Cached exchange rates for fiat currencies.
        /// </summary>
        public DbSet<ExchangeRate> ExchangeRates { get; set; }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // PaymentState configuration
            modelBuilder.Entity<PaymentState>(entity =>
            {
                entity.HasKey(p => p.PaymentHash);
                entity.Property(p => p.AmountSat).HasDefaultValue(0UL);
                entity.Property(p => p.Kind).HasDefaultValue(PaymentKind.Paywall);
                entity.HasIndex(p => p.Bolt12OfferId);
            });

            // IdempotencyMapping configuration
            modelBuilder.Entity<IdempotencyMapping>(entity =>
            {
                entity.HasKey(i => i.IdempotencyKey);
                entity.Property(i => i.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Bolt12Offer configuration
            modelBuilder.Entity<Bolt12Offer>(entity =>
            {
                entity.HasKey(e => e.OfferId);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasMany(e => e.Payments)
                      .WithOne(p => p.Bolt12Offer)
                      .HasForeignKey(p => p.Bolt12OfferId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // RefundTransaction configuration
            modelBuilder.Entity<RefundTransaction>(entity =>
            {
                entity.HasKey(e => e.RefundId);
                entity.HasIndex(e => e.OriginalPaymentHash);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.InitiatedAt);
                entity.HasOne(e => e.OriginalPayment)
                      .WithMany(p => p.Refunds)
                      .HasForeignKey(e => e.OriginalPaymentHash)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // PaymentNotification configuration
            modelBuilder.Entity<PaymentNotification>(entity =>
            {
                entity.HasKey(e => e.NotificationId);
                entity.HasIndex(e => e.PaymentHash);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.NextRetryAt);
                entity.HasIndex(e => new { e.Status, e.NextRetryAt }); // Composite for retry queries
                entity.HasOne(e => e.Payment)
                      .WithMany(p => p.Notifications)
                      .HasForeignKey(e => e.PaymentHash)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ExchangeRate configuration
            modelBuilder.Entity<ExchangeRate>(entity =>
            {
                entity.HasKey(e => e.Currency);
                entity.Property(e => e.RatePerBtc).HasPrecision(18, 8);
            });
        }
    }
}


