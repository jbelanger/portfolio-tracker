using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.DataMigrations
{
    /// <inheritdoc />
    public partial class InitialMigration57 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PriceHistoryRecords_CurrencyPair_CloseDate",
                table: "PriceHistoryRecords");

            migrationBuilder.DropColumn(
                name: "SentAmount",
                table: "AssetHoldings");

            migrationBuilder.DropColumn(
                name: "SentCurrency",
                table: "AssetHoldings");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistoryRecords_CurrencyPair_CloseDate",
                table: "PriceHistoryRecords",
                columns: new[] { "CurrencyPair", "CloseDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PriceHistoryRecords_CurrencyPair_CloseDate",
                table: "PriceHistoryRecords");

            migrationBuilder.AddColumn<decimal>(
                name: "SentAmount",
                table: "AssetHoldings",
                type: "decimal(18,8)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SentCurrency",
                table: "AssetHoldings",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistoryRecords_CurrencyPair_CloseDate",
                table: "PriceHistoryRecords",
                columns: new[] { "CurrencyPair", "CloseDate" });
        }
    }
}
