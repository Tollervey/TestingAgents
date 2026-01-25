using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Breez.Sdk.Liquid.Extensions.Sqlite.Data;

/// <summary>
/// Entity Framework Core configuration for the PaymentState entity.
/// </summary>
internal class PaymentStateConfiguration : IEntityTypeConfiguration<PaymentState>
{
    /// <summary>
    /// Configures the PaymentState entity for SQLite database.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<PaymentState> builder)
    {
        builder.ToTable("PaymentStates");

        // Primary key
        builder.HasKey(p => p.Id);

        // PaymentHash: unique index, required, max length 64
        builder.Property(p => p.PaymentHash)
            .IsRequired()
            .HasMaxLength(64);
        builder.HasIndex(p => p.PaymentHash)
            .IsUnique();

        // Status: convert enum to string, max length 20, index for queries
        builder.Property(p => p.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();
        builder.HasIndex(p => p.Status);

        // AmountSat: stored as INTEGER (long) in SQLite, converted to/from ulong
        builder.Property(p => p.AmountSat)
            .IsRequired()
            .HasConversion(
                v => (long)v,
                v => (ulong)v);

        // Description: optional, max length 500
        builder.Property(p => p.Description)
            .HasMaxLength(500);

        // Invoice: optional, max length 2000 (BOLT11 can be long)
        builder.Property(p => p.Invoice)
            .HasMaxLength(2000);

        // Kind: convert enum to string, max length 20
        builder.Property(p => p.Kind)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        // CreatedAt: indexed for date range queries
        builder.Property(p => p.CreatedAt)
            .IsRequired();
        builder.HasIndex(p => p.CreatedAt);

        // ConfirmedAt: optional timestamp
        builder.Property(p => p.ConfirmedAt);

        // ExpiresAt: optional timestamp
        builder.Property(p => p.ExpiresAt);

        // Preimage: optional, max length 64 (hex-encoded)
        builder.Property(p => p.Preimage)
            .HasMaxLength(64);

        // FeeSat: optional, stored as INTEGER (long) in SQLite
        builder.Property(p => p.FeeSat)
            .HasConversion(
                v => v.HasValue ? (long?)v.Value : null,
                v => v.HasValue ? (ulong?)v.Value : null);

        // Metadata: serialize Dictionary<string,string> to JSON TEXT
        builder.Property(p => p.Metadata)
            .IsRequired()
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null)
                     ?? new Dictionary<string, string>());

        // CorrelationId: optional, max length 64
        builder.Property(p => p.CorrelationId)
            .HasMaxLength(64);
    }
}
