using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projeto.Migrations
{
    /// <inheritdoc />
    public partial class Initial2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Licitacoes_Leiloes_LeilaoId",
                table: "Licitacoes");

            migrationBuilder.AlterColumn<int>(
                name: "LeilaoId",
                table: "Licitacoes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UtilizadorId",
                table: "Licitacoes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Licitacoes_UtilizadorId",
                table: "Licitacoes",
                column: "UtilizadorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Licitacoes_Leiloes_LeilaoId",
                table: "Licitacoes",
                column: "LeilaoId",
                principalTable: "Leiloes",
                principalColumn: "LeilaoId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Licitacoes_Utilizador_UtilizadorId",
                table: "Licitacoes",
                column: "UtilizadorId",
                principalTable: "Utilizador",
                principalColumn: "UtilizadorId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Licitacoes_Leiloes_LeilaoId",
                table: "Licitacoes");

            migrationBuilder.DropForeignKey(
                name: "FK_Licitacoes_Utilizador_UtilizadorId",
                table: "Licitacoes");

            migrationBuilder.DropIndex(
                name: "IX_Licitacoes_UtilizadorId",
                table: "Licitacoes");

            migrationBuilder.DropColumn(
                name: "UtilizadorId",
                table: "Licitacoes");

            migrationBuilder.AlterColumn<int>(
                name: "LeilaoId",
                table: "Licitacoes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Licitacoes_Leiloes_LeilaoId",
                table: "Licitacoes",
                column: "LeilaoId",
                principalTable: "Leiloes",
                principalColumn: "LeilaoId");
        }
    }
}
