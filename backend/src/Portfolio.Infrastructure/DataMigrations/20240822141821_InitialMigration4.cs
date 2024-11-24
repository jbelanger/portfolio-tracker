using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.DataMigrations
{
    /// <inheritdoc />
    public partial class InitialMigration4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SentAmountCurrency",
                table: "CryptoCurrencyRawTransactions",
                newName: "SentCurrency");

            migrationBuilder.RenameColumn(
                name: "ReceivedAmountCurrency",
                table: "CryptoCurrencyRawTransactions",
                newName: "ReceivedCurrency");

            migrationBuilder.RenameColumn(
                name: "FeeAmountCurrency",
                table: "CryptoCurrencyRawTransactions",
                newName: "FeeCurrency");

            migrationBuilder.AlterColumn<decimal>(
                name: "FeeAmount",
                table: "CryptoCurrencyRawTransactions",
                type: "decimal(18,8)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FeeCurrency",
                table: "CryptoCurrencyRawTransactions",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 3,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SentCurrency",
                table: "CryptoCurrencyRawTransactions",
                newName: "SentAmountCurrency");

            migrationBuilder.RenameColumn(
                name: "ReceivedCurrency",
                table: "CryptoCurrencyRawTransactions",
                newName: "ReceivedAmountCurrency");

            migrationBuilder.RenameColumn(
                name: "FeeCurrency",
                table: "CryptoCurrencyRawTransactions",
                newName: "FeeAmountCurrency");

            migrationBuilder.AlterColumn<decimal>(
                name: "FeeAmount",
                table: "CryptoCurrencyRawTransactions",
                type: "decimal(18,8)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)");

            migrationBuilder.AlterColumn<string>(
                name: "FeeAmountCurrency",
                table: "CryptoCurrencyRawTransactions",
                type: "TEXT",
                maxLength: 3,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 3);
        }
    }
}
