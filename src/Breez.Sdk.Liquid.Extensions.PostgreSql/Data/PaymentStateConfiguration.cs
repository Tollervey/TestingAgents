using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Breez.Sdk.Liquid.Extensions.PostgreSql.Data;

/// <summary>
/// Entity Framework Core configuration for the PaymentState entity with PostgreSQL-specific settings.
/// </summary>
internal class PaymentStateConfiguration : IEntityTypeConfiguration<PaymentState>
{
    /// <summary>
    /// Configures the PaymentState entity for PostgreSQL with snake_case naming conventions.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<PaymentState> builder)
    {
        // Table configuration
        builder.ToTable("payment_states");

        // Primary key
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever(); // Guid is generated in the domain entity

        // Payment hash - unique identifier for Lightning payments
        builder.Property(p => p.PaymentHash)
            .HasColumnName("payment_hash")
            .IsRequired()
            .HasMaxLength(64);

        // Status - stored as string for readability
        builder.Property(p => p.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        // Amount - use numeric(20,0) for ulong compatibility
        builder.Property(p => p.AmountSat)
            .HasColumnName("amount_sat")
            .IsRequired()
            .HasColumnType("numeric(20,0)");

        // Description
        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        // Invoice - BOLT11 invoice string
        builder.Property(p => p.Invoice)
            .HasColumnName("invoice")
            .HasMaxLength(2000);

        // Kind - payment type stored as string
        builder.Property(p => p.Kind)
            .HasColumnName("kind")
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        // Timestamps
        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.ConfirmedAt)
            .HasColumnName("confirmed_at");

        builder.Property(p => p.ExpiresAt)
            .HasColumnName("expires_at");

        // Preimage - cryptographic secret
        builder.Property(p => p.Preimage)
            .HasColumnName("preimage")
            .HasMaxLength(64);

        // Fee - use numeric(20,0) for ulong compatibility
        builder.Property(p => p.FeeSat)
            .HasColumnName("fee_sat")
            .HasColumnType("numeric(20,0)");

        // Metadata - PostgreSQL JSONB for better performance
        builder.Property(p => p.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>()
            );

        // Correlation ID for distributed tracing
        builder.Property(p => p.CorrelationId)
            .HasColumnName("correlation_id")
            .HasMaxLength(64);

        // Indexes
        builder.HasIndex(p => p.PaymentHash)
            .IsUnique()
            .HasDatabaseName("ix_payment_states_payment_hash");

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("ix_payment_states_status");

        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("ix_payment_states_created_at");
    }
}
