using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.DataMigrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseRecord_CryptoCurrencyHoldings_CryptoCurrencyHoldingId",
                table: "PurchaseRecord");

            migrationBuilder.DropTable(
                name: "CryptoCurrencyHoldings");

            migrationBuilder.DropTable(
                name: "CryptoCurrencyRawTransactions");

            migrationBuilder.DropTable(
                name: "TaxableEvents");

            migrationBuilder.RenameColumn(
                name: "CryptoCurrencyHoldingId",
                table: "PurchaseRecord",
                newName: "AssetHoldingId");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseRecord_CryptoCurrencyHoldingId",
                table: "PurchaseRecord",
                newName: "IX_PurchaseRecord_AssetHoldingId");

            migrationBuilder.CreateTable(
                name: "AssetHoldings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Asset = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    AverageBoughtPrice = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    SentAmount = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    SentCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    ErrorType = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    UserPortfolioId = table.Column<long>(type: "INTEGER", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetHoldings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetHoldings_UserPortfolios_UserPortfolioId",
                        column: x => x.UserPortfolioId,
                        principalTable: "UserPortfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinancialEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CostBasisPerUnit = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    MarketPricePerUnit = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    AssetSymbol = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    BaseCurrency = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    UserPortfolioId = table.Column<long>(type: "INTEGER", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialEvents_UserPortfolios_UserPortfolioId",
                        column: x => x.UserPortfolioId,
                        principalTable: "UserPortfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinancialTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WalletId = table.Column<long>(type: "INTEGER", nullable: false),
                    DateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    ReceivedAmount = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    ReceivedCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    SentAmount = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    SentCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    FeeAmount = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    FeeCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Account = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    TransactionIds = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorType = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    ValueInDefaultCurrency_Amount = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    ValueInDefaultCurrency_CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    FeeValueInDefaultCurrency_Amount = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    FeeValueInDefaultCurrency_CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    CsvLinesJson = table.Column<string>(type: "TEXT", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialTransactions_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceHistoryRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CurrencyPair = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CloseDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosePrice = table.Column<decimal>(type: "decimal(18,8)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceHistoryRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetHoldings_UserPortfolioId",
                table: "AssetHoldings",
                column: "UserPortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialEvents_UserPortfolioId",
                table: "FinancialEvents",
                column: "UserPortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_WalletId",
                table: "FinancialTransactions",
                column: "WalletId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseRecord_AssetHoldings_AssetHoldingId",
                table: "PurchaseRecord",
                column: "AssetHoldingId",
                principalTable: "AssetHoldings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseRecord_AssetHoldings_AssetHoldingId",
                table: "PurchaseRecord");

            migrationBuilder.DropTable(
                name: "AssetHoldings");

            migrationBuilder.DropTable(
                name: "FinancialEvents");

            migrationBuilder.DropTable(
                name: "FinancialTransactions");

            migrationBuilder.DropTable(
                name: "PriceHistoryRecords");

            migrationBuilder.RenameColumn(
                name: "AssetHoldingId",
                table: "PurchaseRecord",
                newName: "CryptoCurrencyHoldingId");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseRecord_AssetHoldingId",
                table: "PurchaseRecord",
                newName: "IX_PurchaseRecord_CryptoCurrencyHoldingId");

            migrationBuilder.CreateTable(
                name: "CryptoCurrencyHoldings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Asset = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    AverageBoughtPrice = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    ErrorType = table.Column<int>(type: "INTEGER", nullable: false),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UserPortfolioId = table.Column<long>(type: "INTEGER", nullable: true),
                    SentAmount = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    SentCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CryptoCurrencyHoldings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CryptoCurrencyHoldings_UserPortfolios_UserPortfolioId",
                        column: x => x.UserPortfolioId,
                        principalTable: "UserPortfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CryptoCurrencyRawTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Account = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    CsvLinesJson = table.Column<string>(type: "TEXT", nullable: false),
                    DateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    ErrorType = table.Column<int>(type: "INTEGER", nullable: false),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    TransactionIds = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    WalletId = table.Column<long>(type: "INTEGER", nullable: false),
                    FeeAmount = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    FeeCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    FeeValueInDefaultCurrency_Amount = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    FeeValueInDefaultCurrency_CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    ReceivedAmount = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    ReceivedCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    SentAmount = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    SentCurrency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    ValueInDefaultCurrency_Amount = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    ValueInDefaultCurrency_CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CryptoCurrencyRawTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CryptoCurrencyRawTransactions_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxableEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AverageCost = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    DateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DisposedAsset = table.Column<string>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    UserPortfolioId = table.Column<long>(type: "INTEGER", nullable: true),
                    ValueAtDisposal = table.Column<decimal>(type: "decimal(18,8)", nullable: false)
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
                name: "IX_CryptoCurrencyHoldings_UserPortfolioId",
                table: "CryptoCurrencyHoldings",
                column: "UserPortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_CryptoCurrencyRawTransactions_WalletId",
                table: "CryptoCurrencyRawTransactions",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxableEvents_UserPortfolioId",
                table: "TaxableEvents",
                column: "UserPortfolioId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseRecord_CryptoCurrencyHoldings_CryptoCurrencyHoldingId",
                table: "PurchaseRecord",
                column: "CryptoCurrencyHoldingId",
                principalTable: "CryptoCurrencyHoldings",
                principalColumn: "Id");
        }
    }
}
