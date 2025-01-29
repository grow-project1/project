using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projeto.Migrations
{
    /// <inheritdoc />
    public partial class initt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leiloes_ItemId",
                table: "Leiloes");

            migrationBuilder.DropColumn(
                name: "FotoUrl",
                table: "Itens");

            migrationBuilder.CreateIndex(
                name: "IX_Leiloes_ItemId",
                table: "Leiloes",
                column: "ItemId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leiloes_ItemId",
                table: "Leiloes");

            migrationBuilder.AddColumn<string>(
                name: "FotoUrl",
                table: "Itens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Itens",
                keyColumn: "ItemId",
                keyValue: 1,
                column: "FotoUrl",
                value: "/images/relogio.jpg");

            migrationBuilder.UpdateData(
                table: "Itens",
                keyColumn: "ItemId",
                keyValue: 2,
                column: "FotoUrl",
                value: "/images/bicicleta.jpg");

            migrationBuilder.CreateIndex(
                name: "IX_Leiloes_ItemId",
                table: "Leiloes",
                column: "ItemId");
        }
    }
}
