using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.DataMigrations
{
    /// <inheritdoc />
    public partial class InitialMigration8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ErrorType",
                table: "CryptoCurrencyRawTransactions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "AverageBoughtPrice",
                table: "CryptoCurrencyHoldings",
                type: "decimal(18,8)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Fees",
                table: "CryptoCurrencyHoldings",
                type: "decimal(18,8)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SentAmount",
                table: "CryptoCurrencyHoldings",
                type: "decimal(18,8)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SentCurrency",
                table: "CryptoCurrencyHoldings",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorType",
                table: "CryptoCurrencyRawTransactions");

            migrationBuilder.DropColumn(
                name: "Fees",
                table: "CryptoCurrencyHoldings");

            migrationBuilder.DropColumn(
                name: "SentAmount",
                table: "CryptoCurrencyHoldings");

            migrationBuilder.DropColumn(
                name: "SentCurrency",
                table: "CryptoCurrencyHoldings");

            migrationBuilder.AlterColumn<decimal>(
                name: "AverageBoughtPrice",
                table: "CryptoCurrencyHoldings",
                type: "decimal(18,8)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)");
        }
    }
}
