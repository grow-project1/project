using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projeto.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Desconto",
                keyColumn: "DescontoId",
                keyValue: 1,
                column: "Descricao",
                value: "10% discount");

            migrationBuilder.UpdateData(
                table: "Desconto",
                keyColumn: "DescontoId",
                keyValue: 2,
                column: "Descricao",
                value: "25% discount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Desconto",
                keyColumn: "DescontoId",
                keyValue: 1,
                column: "Descricao",
                value: "Desconto de 10% na Loja");

            migrationBuilder.UpdateData(
                table: "Desconto",
                keyColumn: "DescontoId",
                keyValue: 2,
                column: "Descricao",
                value: "Desconto de 25% desconto");
        }
    }
}
