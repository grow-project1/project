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
            migrationBuilder.DropColumn(
                name: "Vencedor",
                table: "Leiloes");

            migrationBuilder.AddColumn<bool>(
                name: "Pago",
                table: "Leiloes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "VencedorId",
                table: "Leiloes",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Pago",
                table: "Leiloes");

            migrationBuilder.DropColumn(
                name: "VencedorId",
                table: "Leiloes");

            migrationBuilder.AddColumn<string>(
                name: "Vencedor",
                table: "Leiloes",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
