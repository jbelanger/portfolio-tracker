using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.DataMigrations
{
    /// <inheritdoc />
    public partial class InitialMigration3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CryptoCurrencyProcessedTransactions_UserPortfolios_UserPortfolioId",
                table: "CryptoCurrencyProcessedTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_CryptoCurrencyRawTransactions_Wallets_WalletId",
                table: "CryptoCurrencyRawTransactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CryptoCurrencyProcessedTransactions",
                table: "CryptoCurrencyProcessedTransactions");

            migrationBuilder.RenameTable(
                name: "CryptoCurrencyProcessedTransactions",
                newName: "CryptoCurrencyProcessedTransaction");

            migrationBuilder.RenameIndex(
                name: "IX_CryptoCurrencyProcessedTransactions_UserPortfolioId",
                table: "CryptoCurrencyProcessedTransaction",
                newName: "IX_CryptoCurrencyProcessedTransaction_UserPortfolioId");

            migrationBuilder.AlterColumn<long>(
                name: "WalletId",
                table: "CryptoCurrencyRawTransactions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "BalanceAfterTransaction",
                table: "CryptoCurrencyProcessedTransaction",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AveragePriceAtTime",
                table: "CryptoCurrencyProcessedTransaction",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "CryptoCurrencyProcessedTransaction",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CryptoCurrencyProcessedTransaction",
                table: "CryptoCurrencyProcessedTransaction",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CryptoCurrencyProcessedTransaction_UserPortfolios_UserPortfolioId",
                table: "CryptoCurrencyProcessedTransaction",
                column: "UserPortfolioId",
                principalTable: "UserPortfolios",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CryptoCurrencyRawTransactions_Wallets_WalletId",
                table: "CryptoCurrencyRawTransactions",
                column: "WalletId",
                principalTable: "Wallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CryptoCurrencyProcessedTransaction_UserPortfolios_UserPortfolioId",
                table: "CryptoCurrencyProcessedTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_CryptoCurrencyRawTransactions_Wallets_WalletId",
                table: "CryptoCurrencyRawTransactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CryptoCurrencyProcessedTransaction",
                table: "CryptoCurrencyProcessedTransaction");

            migrationBuilder.RenameTable(
                name: "CryptoCurrencyProcessedTransaction",
                newName: "CryptoCurrencyProcessedTransactions");

            migrationBuilder.RenameIndex(
                name: "IX_CryptoCurrencyProcessedTransaction_UserPortfolioId",
                table: "CryptoCurrencyProcessedTransactions",
                newName: "IX_CryptoCurrencyProcessedTransactions_UserPortfolioId");

            migrationBuilder.AlterColumn<long>(
                name: "WalletId",
                table: "CryptoCurrencyRawTransactions",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<decimal>(
                name: "BalanceAfterTransaction",
                table: "CryptoCurrencyProcessedTransactions",
                type: "decimal(18,8)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AveragePriceAtTime",
                table: "CryptoCurrencyProcessedTransactions",
                type: "decimal(18,8)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "CryptoCurrencyProcessedTransactions",
                type: "decimal(18,8)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CryptoCurrencyProcessedTransactions",
                table: "CryptoCurrencyProcessedTransactions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CryptoCurrencyProcessedTransactions_UserPortfolios_UserPortfolioId",
                table: "CryptoCurrencyProcessedTransactions",
                column: "UserPortfolioId",
                principalTable: "UserPortfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CryptoCurrencyRawTransactions_Wallets_WalletId",
                table: "CryptoCurrencyRawTransactions",
                column: "WalletId",
                principalTable: "Wallets",
                principalColumn: "Id");
        }
    }
}
