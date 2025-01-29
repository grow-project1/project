using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projeto.Migrations
{
    /// <inheritdoc />
    public partial class ins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leiloes_ItemId",
                table: "Leiloes");

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

            migrationBuilder.CreateIndex(
                name: "IX_Leiloes_ItemId",
                table: "Leiloes",
                column: "ItemId");
        }
    }
}
