using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.DataMigrations
{
    /// <inheritdoc />
    public partial class InitialMigration66 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CoinInfos",
                columns: table => new
                {
                    CoinId = table.Column<string>(type: "TEXT", nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Image = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    MarketCap = table.Column<decimal>(type: "TEXT", nullable: true),
                    MarketCapRank = table.Column<int>(type: "INTEGER", nullable: true),
                    FullyDilutedValuation = table.Column<decimal>(type: "TEXT", nullable: true),
                    TotalVolume = table.Column<decimal>(type: "TEXT", nullable: true),
                    High24h = table.Column<decimal>(type: "TEXT", nullable: true),
                    Low24h = table.Column<decimal>(type: "TEXT", nullable: true),
                    PriceChange24h = table.Column<decimal>(type: "TEXT", nullable: true),
                    PriceChangePercentage24h = table.Column<decimal>(type: "TEXT", nullable: true),
                    MarketCapChange24h = table.Column<decimal>(type: "TEXT", nullable: true),
                    MarketCapChangePercentage24h = table.Column<decimal>(type: "TEXT", nullable: true),
                    CirculatingSupply = table.Column<decimal>(type: "TEXT", nullable: true),
                    TotalSupply = table.Column<decimal>(type: "TEXT", nullable: true),
                    MaxSupply = table.Column<decimal>(type: "TEXT", nullable: true),
                    Ath = table.Column<decimal>(type: "TEXT", nullable: true),
                    AthChangePercentage = table.Column<decimal>(type: "TEXT", nullable: true),
                    AthDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Atl = table.Column<decimal>(type: "TEXT", nullable: true),
                    AtlChangePercentage = table.Column<decimal>(type: "TEXT", nullable: true),
                    AtlDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoinInfos", x => x.CoinId);
                });

            migrationBuilder.CreateTable(
                name: "HttpRequestLogEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequestDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RequestUri = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HttpRequestLogEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoinInfos_Symbol",
                table: "CoinInfos",
                column: "Symbol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoinInfos");

            migrationBuilder.DropTable(
                name: "HttpRequestLogEntries");
        }
    }
}
