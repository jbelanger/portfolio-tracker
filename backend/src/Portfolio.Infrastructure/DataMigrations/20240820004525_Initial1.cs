using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Portfolio.Infrastructure.DataMigrations
{
    /// <inheritdoc />
    public partial class Initial1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Wallets_UserPortfolios_UserPortfolioId",
                table: "Wallets");

            migrationBuilder.DropIndex(
                name: "IX_Wallets_UserPortfolioId",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "UserPortfolioId",
                table: "Wallets");

            migrationBuilder.AddColumn<long>(
                name: "PortfolioId",
                table: "Wallets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_PortfolioId",
                table: "Wallets",
                column: "PortfolioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Wallets_UserPortfolios_PortfolioId",
                table: "Wallets",
                column: "PortfolioId",
                principalTable: "UserPortfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Wallets_UserPortfolios_PortfolioId",
                table: "Wallets");

            migrationBuilder.DropIndex(
                name: "IX_Wallets_PortfolioId",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "PortfolioId",
                table: "Wallets");

            migrationBuilder.AddColumn<long>(
                name: "UserPortfolioId",
                table: "Wallets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_UserPortfolioId",
                table: "Wallets",
                column: "UserPortfolioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Wallets_UserPortfolios_UserPortfolioId",
                table: "Wallets",
                column: "UserPortfolioId",
                principalTable: "UserPortfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
