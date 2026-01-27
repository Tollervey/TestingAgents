using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Migrations;

/// <summary>
/// Migration to add Bolt12 offers, payment notifications, refund transactions, and exchange rates.
/// </summary>
public partial class AddBolt12NotificationsRefunds : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Bolt12Offers table
        migrationBuilder.CreateTable(
            name: "Bolt12Offers",
            columns: table => new
            {
                OfferId = table.Column<Guid>(type: "TEXT", nullable: false),
                OfferString = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                AmountSat = table.Column<ulong>(type: "INTEGER", nullable: true),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                DeactivatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                ContentId = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Bolt12Offers", x => x.OfferId);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Bolt12Offers_IsActive",
            table: "Bolt12Offers",
            column: "IsActive");

        migrationBuilder.CreateIndex(
            name: "IX_Bolt12Offers_CreatedAt",
            table: "Bolt12Offers",
            column: "CreatedAt");

        // RefundTransactions table
        migrationBuilder.CreateTable(
            name: "RefundTransactions",
            columns: table => new
            {
                RefundId = table.Column<Guid>(type: "TEXT", nullable: false),
                OriginalPaymentHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                AmountSat = table.Column<ulong>(type: "INTEGER", nullable: false),
                DestinationInvoice = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                InitiatedByUserId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                InitiatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                RefundPaymentHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefundTransactions", x => x.RefundId);
                table.ForeignKey(
                    name: "FK_RefundTransactions_PaymentStates_OriginalPaymentHash",
                    column: x => x.OriginalPaymentHash,
                    principalTable: "PaymentStates",
                    principalColumn: "PaymentHash",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RefundTransactions_OriginalPaymentHash",
            table: "RefundTransactions",
            column: "OriginalPaymentHash");

        migrationBuilder.CreateIndex(
            name: "IX_RefundTransactions_Status",
            table: "RefundTransactions",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_RefundTransactions_InitiatedAt",
            table: "RefundTransactions",
            column: "InitiatedAt");

        // PaymentNotifications table
        migrationBuilder.CreateTable(
            name: "PaymentNotifications",
            columns: table => new
            {
                NotificationId = table.Column<Guid>(type: "TEXT", nullable: false),
                PaymentHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                Type = table.Column<int>(type: "INTEGER", nullable: false),
                Event = table.Column<int>(type: "INTEGER", nullable: false),
                Destination = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                AttemptCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                MaxAttempts = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 5),
                LastError = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                NextRetryAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                SentAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                FailedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                Payload = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                ResponseStatusCode = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentNotifications", x => x.NotificationId);
                table.ForeignKey(
                    name: "FK_PaymentNotifications_PaymentStates_PaymentHash",
                    column: x => x.PaymentHash,
                    principalTable: "PaymentStates",
                    principalColumn: "PaymentHash",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PaymentNotifications_PaymentHash",
            table: "PaymentNotifications",
            column: "PaymentHash");

        migrationBuilder.CreateIndex(
            name: "IX_PaymentNotifications_Status",
            table: "PaymentNotifications",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_PaymentNotifications_NextRetryAt",
            table: "PaymentNotifications",
            column: "NextRetryAt");

        migrationBuilder.CreateIndex(
            name: "IX_PaymentNotifications_Status_NextRetryAt",
            table: "PaymentNotifications",
            columns: new[] { "Status", "NextRetryAt" });

        // ExchangeRates table
        migrationBuilder.CreateTable(
            name: "ExchangeRates",
            columns: table => new
            {
                Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                RatePerBtc = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                FetchedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                Source = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "CoinGecko")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExchangeRates", x => x.Currency);
            });

        // Add Bolt12OfferId to PaymentStates
        migrationBuilder.AddColumn<Guid>(
            name: "Bolt12OfferId",
            table: "PaymentStates",
            type: "TEXT",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_PaymentStates_Bolt12OfferId",
            table: "PaymentStates",
            column: "Bolt12OfferId");

        migrationBuilder.AddForeignKey(
            name: "FK_PaymentStates_Bolt12Offers_Bolt12OfferId",
            table: "PaymentStates",
            column: "Bolt12OfferId",
            principalTable: "Bolt12Offers",
            principalColumn: "OfferId",
            onDelete: ReferentialAction.SetNull);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_PaymentStates_Bolt12Offers_Bolt12OfferId",
            table: "PaymentStates");

        migrationBuilder.DropIndex(
            name: "IX_PaymentStates_Bolt12OfferId",
            table: "PaymentStates");

        migrationBuilder.DropColumn(
            name: "Bolt12OfferId",
            table: "PaymentStates");

        migrationBuilder.DropTable(
            name: "ExchangeRates");

        migrationBuilder.DropTable(
            name: "PaymentNotifications");

        migrationBuilder.DropTable(
            name: "RefundTransactions");

        migrationBuilder.DropTable(
            name: "Bolt12Offers");
    }
}
