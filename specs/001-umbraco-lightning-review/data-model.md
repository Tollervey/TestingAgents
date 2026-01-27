# Data Model: Umbraco Lightning Payments Improvements

**Feature Branch**: `001-umbraco-lightning-review`
**Created**: 2026-01-26
**Status**: Design

## Overview

This document defines the entity models, relationships, and database schema for the Umbraco Lightning Payments improvements. It extends the existing `PaymentDbContext` with new entities for Bolt12 offers, refunds, notifications, and exchange rates.

---

## Existing Entities (Reference)

### PaymentState

**Location**: `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Data/Models/PaymentState.cs`

```csharp
public class PaymentState
{
    public string PaymentHash { get; set; }      // PK
    public int ContentId { get; set; }           // 0 for tips
    public string UserSessionId { get; set; }
    public PaymentStatus Status { get; set; }    // Pending, Paid, Failed, Expired
    public ulong AmountSat { get; set; }
    public PaymentKind Kind { get; set; }        // Paywall, Tip
}
```

### IdempotencyMapping

**Location**: `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Data/Models/IdempotencyMapping.cs`

```csharp
public class IdempotencyMapping
{
    public string IdempotencyKey { get; set; }   // PK
    public string PaymentHash { get; set; }
    public string Invoice { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```

---

## New Entities

### 1. Bolt12Offer

**Purpose**: Track reusable Bolt12 offers for recurring payment use cases (FR-009 through FR-011)

**Location**: `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Data/Models/Bolt12Offer.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;

/// <summary>
/// Represents a reusable BOLT12 offer for accepting multiple payments.
/// </summary>
public class Bolt12Offer
{
    /// <summary>
    /// Unique identifier for the offer.
    /// </summary>
    [Key]
    public Guid OfferId { get; set; }

    /// <summary>
    /// The BOLT12 offer string (starts with "lno1").
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string OfferString { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of the offer.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Fixed amount in satoshis (null for variable amount offers).
    /// </summary>
    public ulong? AmountSat { get; set; }

    /// <summary>
    /// Whether the offer is currently accepting payments.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the offer was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the offer was deactivated (null if still active).
    /// </summary>
    public DateTimeOffset? DeactivatedAt { get; set; }

    /// <summary>
    /// Optional: Content ID this offer is associated with.
    /// </summary>
    public int? ContentId { get; set; }

    /// <summary>
    /// Payments received against this offer.
    /// </summary>
    public virtual ICollection<PaymentState> Payments { get; set; } = new List<PaymentState>();
}
```

**Validation Rules**:
- `OfferString` must start with "lno1" (BOLT12 format)
- `Description` max 200 characters (consistent with invoice descriptions)
- `AmountSat` nullable for variable-amount offers

**State Transitions**:
```
Created (IsActive=true)
    ↓
Deactivated (IsActive=false, DeactivatedAt set)
```

### 2. RefundTransaction

**Purpose**: Track refund operations linked to original payments (FR-016 through FR-018)

**Location**: `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Data/Models/RefundTransaction.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;

/// <summary>
/// Represents a refund transaction linked to an original payment.
/// </summary>
public class RefundTransaction
{
    /// <summary>
    /// Unique identifier for the refund.
    /// </summary>
    [Key]
    public Guid RefundId { get; set; }

    /// <summary>
    /// Payment hash of the original payment being refunded.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string OriginalPaymentHash { get; set; } = string.Empty;

    /// <summary>
    /// Amount being refunded in satoshis.
    /// </summary>
    public ulong AmountSat { get; set; }

    /// <summary>
    /// BOLT11 invoice provided by the customer for the refund.
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string DestinationInvoice { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the refund.
    /// </summary>
    public RefundStatus Status { get; set; } = RefundStatus.Pending;

    /// <summary>
    /// Error message if the refund failed.
    /// </summary>
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Reason for the refund (provided by admin).
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// Umbraco user ID who initiated the refund.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string InitiatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// When the refund was initiated.
    /// </summary>
    public DateTimeOffset InitiatedAt { get; set; }

    /// <summary>
    /// When the refund completed (success or failure).
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Payment hash of the outbound refund payment (from SDK).
    /// </summary>
    [MaxLength(64)]
    public string? RefundPaymentHash { get; set; }

    /// <summary>
    /// Navigation to the original payment.
    /// </summary>
    [ForeignKey(nameof(OriginalPaymentHash))]
    public virtual PaymentState? OriginalPayment { get; set; }
}

/// <summary>
/// Status of a refund transaction.
/// </summary>
public enum RefundStatus
{
    /// <summary>
    /// Refund has been initiated but not yet processed.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Refund payment was sent successfully.
    /// </summary>
    Succeeded = 1,

    /// <summary>
    /// Refund failed (see ErrorMessage for details).
    /// </summary>
    Failed = 2
}
```

**Validation Rules**:
- `AmountSat` must be > 0 and <= original payment amount
- `DestinationInvoice` must be valid BOLT11 format
- `InitiatedByUserId` must be an Administrator

**State Transitions**:
```
Pending
    ↓ (Payment sent)
Succeeded (CompletedAt set, RefundPaymentHash set)

Pending
    ↓ (Payment failed)
Failed (CompletedAt set, ErrorMessage set)
```

### 3. PaymentNotification

**Purpose**: Track notification delivery for payment events (FR-012 through FR-015)

**Location**: `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Data/Models/PaymentNotification.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;

/// <summary>
/// Represents a notification (email or webhook) for a payment event.
/// </summary>
public class PaymentNotification
{
    /// <summary>
    /// Unique identifier for the notification.
    /// </summary>
    [Key]
    public Guid NotificationId { get; set; }

    /// <summary>
    /// Payment hash this notification relates to.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string PaymentHash { get; set; } = string.Empty;

    /// <summary>
    /// Type of notification (Email or Webhook).
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// Event that triggered the notification.
    /// </summary>
    public NotificationEvent Event { get; set; }

    /// <summary>
    /// Destination (email address or webhook URL).
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Destination { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the notification.
    /// </summary>
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    /// <summary>
    /// Number of delivery attempts made.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Maximum number of attempts before marking as permanently failed.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Last error message if delivery failed.
    /// </summary>
    [MaxLength(1000)]
    public string? LastError { get; set; }

    /// <summary>
    /// When the notification was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the next retry should be attempted.
    /// </summary>
    public DateTimeOffset? NextRetryAt { get; set; }

    /// <summary>
    /// When the notification was successfully sent.
    /// </summary>
    public DateTimeOffset? SentAt { get; set; }

    /// <summary>
    /// When the notification permanently failed.
    /// </summary>
    public DateTimeOffset? FailedAt { get; set; }

    /// <summary>
    /// JSON payload sent (for debugging/retry).
    /// </summary>
    [MaxLength(4000)]
    public string? Payload { get; set; }

    /// <summary>
    /// HTTP response status code (for webhooks).
    /// </summary>
    public int? ResponseStatusCode { get; set; }

    /// <summary>
    /// Navigation to the related payment.
    /// </summary>
    [ForeignKey(nameof(PaymentHash))]
    public virtual PaymentState? Payment { get; set; }
}

/// <summary>
/// Type of notification delivery.
/// </summary>
public enum NotificationType
{
    Email = 0,
    Webhook = 1
}

/// <summary>
/// Event that triggered the notification.
/// </summary>
public enum NotificationEvent
{
    PaymentConfirmed = 0,
    PaymentFailed = 1,
    PaymentExpired = 2,
    RefundInitiated = 3,
    RefundCompleted = 4
}

/// <summary>
/// Status of notification delivery.
/// </summary>
public enum NotificationStatus
{
    /// <summary>
    /// Notification created, waiting to be sent.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Notification successfully delivered.
    /// </summary>
    Sent = 1,

    /// <summary>
    /// Delivery failed, will retry.
    /// </summary>
    Retrying = 2,

    /// <summary>
    /// Permanently failed after max attempts.
    /// </summary>
    Failed = 3
}
```

**Validation Rules**:
- `Destination` must be valid email format for Email type
- `Destination` must be valid HTTPS URL for Webhook type
- `MaxAttempts` default 5 (per FR-014)

**State Transitions**:
```
Pending
    ↓ (Success)
Sent (SentAt set)

Pending
    ↓ (Failure, attempts < max)
Retrying (NextRetryAt set, AttemptCount++)

Retrying
    ↓ (Success)
Sent (SentAt set)

Retrying
    ↓ (Failure, attempts >= max)
Failed (FailedAt set)
```

### 4. ExchangeRate

**Purpose**: Cache exchange rates for multi-currency display (FR-019 through FR-022)

**Location**: `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Data/Models/ExchangeRate.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;

/// <summary>
/// Cached exchange rate for a fiat currency.
/// </summary>
public class ExchangeRate
{
    /// <summary>
    /// ISO 4217 currency code (USD, EUR, GBP, etc.).
    /// </summary>
    [Key]
    [MaxLength(3)]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Exchange rate per 1 BTC (e.g., 45000.00 for USD).
    /// </summary>
    public decimal RatePerBtc { get; set; }

    /// <summary>
    /// Exchange rate per 1 satoshi (RatePerBtc / 100_000_000).
    /// </summary>
    [NotMapped]
    public decimal RatePerSat => RatePerBtc / 100_000_000m;

    /// <summary>
    /// When this rate was fetched from the source.
    /// </summary>
    public DateTimeOffset FetchedAt { get; set; }

    /// <summary>
    /// Source of the exchange rate (e.g., "CoinGecko").
    /// </summary>
    [MaxLength(50)]
    public string Source { get; set; } = "CoinGecko";

    /// <summary>
    /// Whether this rate is considered stale (>5 minutes old).
    /// </summary>
    [NotMapped]
    public bool IsStale => DateTimeOffset.UtcNow - FetchedAt > TimeSpan.FromMinutes(5);
}
```

**Supported Currencies**:
- USD (US Dollar)
- EUR (Euro)
- GBP (British Pound)
- Additional currencies configurable

---

## Extended Entities

### PaymentState (Updated)

Add foreign key to Bolt12Offer for tracking payments against offers:

```csharp
public class PaymentState
{
    // Existing properties...

    /// <summary>
    /// Optional: Bolt12 offer this payment was received against.
    /// </summary>
    public Guid? Bolt12OfferId { get; set; }

    /// <summary>
    /// Navigation to the associated offer.
    /// </summary>
    [ForeignKey(nameof(Bolt12OfferId))]
    public virtual Bolt12Offer? Bolt12Offer { get; set; }

    /// <summary>
    /// Refunds issued against this payment.
    /// </summary>
    public virtual ICollection<RefundTransaction> Refunds { get; set; } = new List<RefundTransaction>();

    /// <summary>
    /// Notifications sent for this payment.
    /// </summary>
    public virtual ICollection<PaymentNotification> Notifications { get; set; } = new List<PaymentNotification>();
}
```

---

## Entity Relationships

```
┌─────────────────┐
│  Bolt12Offer    │
│  (OfferId)      │
└────────┬────────┘
         │ 1:N
         ▼
┌─────────────────┐         ┌─────────────────┐
│  PaymentState   │◄────────│RefundTransaction│
│  (PaymentHash)  │ 1:N     │  (RefundId)     │
└────────┬────────┘         └─────────────────┘
         │ 1:N
         ▼
┌─────────────────┐
│PaymentNotification│
│  (NotificationId) │
└─────────────────┘

┌─────────────────┐
│  ExchangeRate   │  (Standalone, no FK)
│  (Currency)     │
└─────────────────┘
```

---

## Database Schema (EF Core)

### DbContext Extension

**Location**: `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Data/PaymentDbContext.cs`

```csharp
public class PaymentDbContext : DbContext
{
    // Existing
    public DbSet<PaymentState> PaymentStates { get; set; }
    public DbSet<IdempotencyMapping> IdempotencyMappings { get; set; }

    // New
    public DbSet<Bolt12Offer> Bolt12Offers { get; set; }
    public DbSet<RefundTransaction> RefundTransactions { get; set; }
    public DbSet<PaymentNotification> PaymentNotifications { get; set; }
    public DbSet<ExchangeRate> ExchangeRates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
            entity.Property(e => e.RatePerBtc)
                  .HasPrecision(18, 8);
        });

        // PaymentState updates
        modelBuilder.Entity<PaymentState>(entity =>
        {
            entity.HasIndex(e => e.Bolt12OfferId);
        });
    }
}
```

### Migration

**Location**: `src/Umbraco.Community.Bitcoin.LightningPayments.Core/Data/Migrations/[timestamp]_AddBolt12NotificationsRefunds.cs`

```csharp
public partial class AddBolt12NotificationsRefunds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Bolt12Offers table
        migrationBuilder.CreateTable(
            name: "Bolt12Offers",
            columns: table => new
            {
                OfferId = table.Column<Guid>(nullable: false),
                OfferString = table.Column<string>(maxLength: 1000, nullable: false),
                Description = table.Column<string>(maxLength: 200, nullable: false),
                AmountSat = table.Column<ulong>(nullable: true),
                IsActive = table.Column<bool>(nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                DeactivatedAt = table.Column<DateTimeOffset>(nullable: true),
                ContentId = table.Column<int>(nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_Bolt12Offers", x => x.OfferId));

        migrationBuilder.CreateIndex("IX_Bolt12Offers_IsActive", "Bolt12Offers", "IsActive");
        migrationBuilder.CreateIndex("IX_Bolt12Offers_CreatedAt", "Bolt12Offers", "CreatedAt");

        // RefundTransactions table
        migrationBuilder.CreateTable(
            name: "RefundTransactions",
            columns: table => new
            {
                RefundId = table.Column<Guid>(nullable: false),
                OriginalPaymentHash = table.Column<string>(maxLength: 64, nullable: false),
                AmountSat = table.Column<ulong>(nullable: false),
                DestinationInvoice = table.Column<string>(maxLength: 2000, nullable: false),
                Status = table.Column<int>(nullable: false),
                ErrorMessage = table.Column<string>(maxLength: 500, nullable: true),
                Reason = table.Column<string>(maxLength: 500, nullable: true),
                InitiatedByUserId = table.Column<string>(maxLength: 50, nullable: false),
                InitiatedAt = table.Column<DateTimeOffset>(nullable: false),
                CompletedAt = table.Column<DateTimeOffset>(nullable: true),
                RefundPaymentHash = table.Column<string>(maxLength: 64, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefundTransactions", x => x.RefundId);
                table.ForeignKey("FK_RefundTransactions_PaymentStates",
                    x => x.OriginalPaymentHash,
                    "PaymentStates", "PaymentHash",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_RefundTransactions_OriginalPaymentHash", "RefundTransactions", "OriginalPaymentHash");
        migrationBuilder.CreateIndex("IX_RefundTransactions_Status", "RefundTransactions", "Status");

        // PaymentNotifications table
        migrationBuilder.CreateTable(
            name: "PaymentNotifications",
            columns: table => new
            {
                NotificationId = table.Column<Guid>(nullable: false),
                PaymentHash = table.Column<string>(maxLength: 64, nullable: false),
                Type = table.Column<int>(nullable: false),
                Event = table.Column<int>(nullable: false),
                Destination = table.Column<string>(maxLength: 500, nullable: false),
                Status = table.Column<int>(nullable: false),
                AttemptCount = table.Column<int>(nullable: false, defaultValue: 0),
                MaxAttempts = table.Column<int>(nullable: false, defaultValue: 5),
                LastError = table.Column<string>(maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                NextRetryAt = table.Column<DateTimeOffset>(nullable: true),
                SentAt = table.Column<DateTimeOffset>(nullable: true),
                FailedAt = table.Column<DateTimeOffset>(nullable: true),
                Payload = table.Column<string>(maxLength: 4000, nullable: true),
                ResponseStatusCode = table.Column<int>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentNotifications", x => x.NotificationId);
                table.ForeignKey("FK_PaymentNotifications_PaymentStates",
                    x => x.PaymentHash,
                    "PaymentStates", "PaymentHash",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_PaymentNotifications_PaymentHash", "PaymentNotifications", "PaymentHash");
        migrationBuilder.CreateIndex("IX_PaymentNotifications_Status", "PaymentNotifications", "Status");
        migrationBuilder.CreateIndex("IX_PaymentNotifications_Status_NextRetryAt", "PaymentNotifications", new[] { "Status", "NextRetryAt" });

        // ExchangeRates table
        migrationBuilder.CreateTable(
            name: "ExchangeRates",
            columns: table => new
            {
                Currency = table.Column<string>(maxLength: 3, nullable: false),
                RatePerBtc = table.Column<decimal>(precision: 18, scale: 8, nullable: false),
                FetchedAt = table.Column<DateTimeOffset>(nullable: false),
                Source = table.Column<string>(maxLength: 50, nullable: false, defaultValue: "CoinGecko")
            },
            constraints: table => table.PrimaryKey("PK_ExchangeRates", x => x.Currency));

        // Add Bolt12OfferId to PaymentStates
        migrationBuilder.AddColumn<Guid>(
            name: "Bolt12OfferId",
            table: "PaymentStates",
            nullable: true);

        migrationBuilder.CreateIndex("IX_PaymentStates_Bolt12OfferId", "PaymentStates", "Bolt12OfferId");

        migrationBuilder.AddForeignKey(
            name: "FK_PaymentStates_Bolt12Offers",
            table: "PaymentStates",
            column: "Bolt12OfferId",
            principalTable: "Bolt12Offers",
            principalColumn: "OfferId",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_PaymentStates_Bolt12Offers", "PaymentStates");
        migrationBuilder.DropIndex("IX_PaymentStates_Bolt12OfferId", "PaymentStates");
        migrationBuilder.DropColumn("Bolt12OfferId", "PaymentStates");
        migrationBuilder.DropTable("ExchangeRates");
        migrationBuilder.DropTable("PaymentNotifications");
        migrationBuilder.DropTable("RefundTransactions");
        migrationBuilder.DropTable("Bolt12Offers");
    }
}
```

---

## Indexes Summary

| Table | Index | Columns | Purpose |
|-------|-------|---------|---------|
| Bolt12Offers | IX_IsActive | IsActive | Filter active offers |
| Bolt12Offers | IX_CreatedAt | CreatedAt | Sort by creation date |
| RefundTransactions | IX_OriginalPaymentHash | OriginalPaymentHash | Find refunds by payment |
| RefundTransactions | IX_Status | Status | Filter by status |
| PaymentNotifications | IX_PaymentHash | PaymentHash | Find notifications by payment |
| PaymentNotifications | IX_Status | Status | Filter by status |
| PaymentNotifications | IX_Status_NextRetryAt | Status, NextRetryAt | Retry queue queries |
| PaymentStates | IX_Bolt12OfferId | Bolt12OfferId | Find payments by offer |

---

## Data Access Patterns

### Query: Get pending notifications for retry
```csharp
var pendingNotifications = await _context.PaymentNotifications
    .Where(n => n.Status == NotificationStatus.Retrying &&
                n.NextRetryAt <= DateTimeOffset.UtcNow)
    .OrderBy(n => n.NextRetryAt)
    .Take(100)
    .AsNoTracking()
    .ToListAsync(ct);
```

### Query: Get offer payment summary
```csharp
var summary = await _context.Bolt12Offers
    .Where(o => o.OfferId == offerId)
    .Select(o => new
    {
        Offer = o,
        PaymentCount = o.Payments.Count(),
        TotalAmountSat = o.Payments.Sum(p => (long)p.AmountSat),
        ConfirmedCount = o.Payments.Count(p => p.Status == PaymentStatus.Confirmed)
    })
    .AsNoTracking()
    .FirstOrDefaultAsync(ct);
```

### Query: Get refund history for payment
```csharp
var refunds = await _context.RefundTransactions
    .Where(r => r.OriginalPaymentHash == paymentHash)
    .OrderByDescending(r => r.InitiatedAt)
    .AsNoTracking()
    .ToListAsync(ct);
```

---

*Data model design completed 2026-01-26*
