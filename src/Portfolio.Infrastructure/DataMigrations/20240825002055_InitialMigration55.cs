using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.DataMigrations
{
    /// <inheritdoc />
    public partial class InitialMigration55 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PriceHistoryRecords_CloseDate",
                table: "PriceHistoryRecords");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistoryRecords_CurrencyPair_CloseDate",
                table: "PriceHistoryRecords",
                columns: new[] { "CurrencyPair", "CloseDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PriceHistoryRecords_CurrencyPair_CloseDate",
                table: "PriceHistoryRecords");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistoryRecords_CloseDate",
                table: "PriceHistoryRecords",
                column: "CloseDate");
        }
    }
}
