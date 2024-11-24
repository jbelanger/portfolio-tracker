using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.DataMigrations
{
    /// <inheritdoc />
    public partial class InitialMigration7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CryptoCurrencyProcessedTransaction");

            migrationBuilder.DropColumn(
                name: "Fees",
                table: "CryptoCurrencyHoldings");

            migrationBuilder.AddColumn<string>(
                name: "DefaultCurrency",
                table: "UserPortfolios",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "CryptoCurrencyRawTransactions",
                type: "TEXT",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "CryptoCurrencyHoldings",
                type: "TEXT",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ErrorType",
                table: "CryptoCurrencyHoldings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PurchaseRecord",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    PricePerUnit = table.Column<decimal>(type: "TEXT", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CryptoCurrencyHoldingId = table.Column<long>(type: "INTEGER", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseRecord_CryptoCurrencyHoldings_CryptoCurrencyHoldingId",
                        column: x => x.CryptoCurrencyHoldingId,
                        principalTable: "CryptoCurrencyHoldings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaxableEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AverageCost = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    ValueAtDisposal = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    DisposedAsset = table.Column<string>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    UserPortfolioId = table.Column<long>(type: "INTEGER", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxableEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxableEvents_UserPortfolios_UserPortfolioId",
                        column: x => x.UserPortfolioId,
                        principalTable: "UserPortfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRecord_CryptoCurrencyHoldingId",
                table: "PurchaseRecord",
                column: "CryptoCurrencyHoldingId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxableEvents_UserPortfolioId",
                table: "TaxableEvents",
                column: "UserPortfolioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseRecord");

            migrationBuilder.DropTable(
                name: "TaxableEvents");

            migrationBuilder.DropColumn(
                name: "DefaultCurrency",
                table: "UserPortfolios");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "CryptoCurrencyRawTransactions");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "CryptoCurrencyHoldings");

            migrationBuilder.DropColumn(
                name: "ErrorType",
                table: "CryptoCurrencyHoldings");

            migrationBuilder.AddColumn<decimal>(
                name: "Fees",
                table: "CryptoCurrencyHoldings",
                type: "decimal(18,8)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "CryptoCurrencyProcessedTransaction",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Asset = table.Column<string>(type: "TEXT", nullable: false),
                    AveragePriceAtTime = table.Column<decimal>(type: "TEXT", nullable: true),
                    BalanceAfterTransaction = table.Column<decimal>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    DateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UserPortfolioId = table.Column<long>(type: "INTEGER", nullable: true),
                    WalletName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CryptoCurrencyProcessedTransaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CryptoCurrencyProcessedTransaction_UserPortfolios_UserPortfolioId",
                        column: x => x.UserPortfolioId,
                        principalTable: "UserPortfolios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CryptoCurrencyProcessedTransaction_UserPortfolioId",
                table: "CryptoCurrencyProcessedTransaction",
                column: "UserPortfolioId");
        }
    }
}
