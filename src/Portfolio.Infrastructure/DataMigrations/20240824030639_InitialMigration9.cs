using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.DataMigrations
{
    /// <inheritdoc />
    public partial class InitialMigration9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FeeValueInDefaultCurrency_Amount",
                table: "CryptoCurrencyRawTransactions",
                type: "decimal(18,8)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "FeeValueInDefaultCurrency_CurrencyCode",
                table: "CryptoCurrencyRawTransactions",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ValueInDefaultCurrency_Amount",
                table: "CryptoCurrencyRawTransactions",
                type: "decimal(18,8)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ValueInDefaultCurrency_CurrencyCode",
                table: "CryptoCurrencyRawTransactions",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeeValueInDefaultCurrency_Amount",
                table: "CryptoCurrencyRawTransactions");

            migrationBuilder.DropColumn(
                name: "FeeValueInDefaultCurrency_CurrencyCode",
                table: "CryptoCurrencyRawTransactions");

            migrationBuilder.DropColumn(
                name: "ValueInDefaultCurrency_Amount",
                table: "CryptoCurrencyRawTransactions");

            migrationBuilder.DropColumn(
                name: "ValueInDefaultCurrency_CurrencyCode",
                table: "CryptoCurrencyRawTransactions");
        }
    }
}
