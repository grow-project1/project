using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projeto.Migrations
{
    /// <inheritdoc />
    public partial class inittt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leiloes_ItemId",
                table: "Leiloes");

            migrationBuilder.AddColumn<int>(
                name: "UtilizadorId",
                table: "Leiloes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Leiloes",
                keyColumn: "LeilaoId",
                keyValue: 1,
                column: "UtilizadorId",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Leiloes",
                keyColumn: "LeilaoId",
                keyValue: 2,
                column: "UtilizadorId",
                value: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Leiloes_ItemId",
                table: "Leiloes",
                column: "ItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leiloes_ItemId",
                table: "Leiloes");

            migrationBuilder.DropColumn(
                name: "UtilizadorId",
                table: "Leiloes");

            migrationBuilder.CreateIndex(
                name: "IX_Leiloes_ItemId",
                table: "Leiloes",
                column: "ItemId",
                unique: true);
        }
    }
}
