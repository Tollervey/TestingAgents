using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Breez.Sdk.Liquid.Extensions.SqlServer.Data;

/// <summary>
/// Entity Framework Core configuration for <see cref="PaymentState"/> entity.
/// Defines table schema, indexes, constraints, and SQL Server-specific mappings.
/// </summary>
public class PaymentStateConfiguration : IEntityTypeConfiguration<PaymentState>
{
    /// <summary>
    /// Configures the <see cref="PaymentState"/> entity mapping.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<PaymentState> builder)
    {
        // Table name
        builder.ToTable("PaymentStates");

        // Primary key
        builder.HasKey(p => p.Id);

        // PaymentHash: Required, MaxLength(64), Unique Index
        builder.Property(p => p.PaymentHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(p => p.PaymentHash)
            .IsUnique()
            .HasDatabaseName("IX_PaymentStates_PaymentHash");

        // Status: Convert enum to string, MaxLength(20), Indexed
        builder.Property(p => p.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("IX_PaymentStates_Status");

        // Kind: Convert enum to string, MaxLength(20)
        builder.Property(p => p.Kind)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        // AmountSat: Use decimal(20,0) for SQL Server (ulong compatibility)
        // decimal(20,0) can store values up to 10^20 - 1, which covers ulong.MaxValue (18,446,744,073,709,551,615)
        builder.Property(p => p.AmountSat)
            .IsRequired()
            .HasColumnType("decimal(20,0)")
            .HasConversion(
                v => (decimal)v,
                v => (ulong)v);

        // Description: MaxLength(500), Optional
        builder.Property(p => p.Description)
            .HasMaxLength(500);

        // Invoice: MaxLength(2000), Optional
        builder.Property(p => p.Invoice)
            .HasMaxLength(2000);

        // CreatedAt: Required, Indexed
        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("IX_PaymentStates_CreatedAt");

        // ConfirmedAt: Optional
        builder.Property(p => p.ConfirmedAt);

        // ExpiresAt: Optional
        builder.Property(p => p.ExpiresAt);

        // Preimage: MaxLength(64), Optional
        builder.Property(p => p.Preimage)
            .HasMaxLength(64);

        // FeeSat: Optional, use decimal(20,0) for consistency
        builder.Property(p => p.FeeSat)
            .HasColumnType("decimal(20,0)")
            .HasConversion(
                v => v.HasValue ? (decimal?)v.Value : null,
                v => v.HasValue ? (ulong?)v.Value : null);

        // CorrelationId: MaxLength(64), Optional
        builder.Property(p => p.CorrelationId)
            .HasMaxLength(64);

        // Metadata: JSON serialization to nvarchar(max)
        builder.Property(p => p.Metadata)
            .IsRequired()
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null)
                     ?? new Dictionary<string, string>());
    }
}
