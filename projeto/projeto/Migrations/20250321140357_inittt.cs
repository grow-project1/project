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
            migrationBuilder.CreateIndex(
                name: "IX_Leiloes_VencedorId",
                table: "Leiloes",
                column: "VencedorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Leiloes_Utilizador_VencedorId",
                table: "Leiloes",
                column: "VencedorId",
                principalTable: "Utilizador",
                principalColumn: "UtilizadorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leiloes_Utilizador_VencedorId",
                table: "Leiloes");

            migrationBuilder.DropIndex(
                name: "IX_Leiloes_VencedorId",
                table: "Leiloes");
        }
    }
}
