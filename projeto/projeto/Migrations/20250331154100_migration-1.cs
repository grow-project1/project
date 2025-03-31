using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace projeto.Migrations
{
    /// <inheritdoc />
    public partial class migration1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Used",
                table: "DescontoResgatado",
                newName: "Usado");

            migrationBuilder.UpdateData(
                table: "Desconto",
                keyColumn: "DescontoId",
                keyValue: 1,
                columns: new[] { "Descricao", "PontosNecessarios", "Valor" },
                values: new object[] { "2% discount", 50, 2.0 });

            migrationBuilder.UpdateData(
                table: "Desconto",
                keyColumn: "DescontoId",
                keyValue: 2,
                columns: new[] { "Descricao", "PontosNecessarios", "Valor" },
                values: new object[] { "3% discount", 100, 3.0 });

            migrationBuilder.InsertData(
                table: "Desconto",
                columns: new[] { "DescontoId", "DataFim", "DataObtencao", "Descricao", "IsLoja", "PontosNecessarios", "UtilizadorId", "Valor" },
                values: new object[,]
                {
                    { 3, null, null, "5% discount", true, 150, null, 5.0 },
                    { 4, null, null, "6% discount", true, 200, null, 6.0 },
                    { 5, null, null, "7% discount", true, 300, null, 7.0 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            

            migrationBuilder.RenameColumn(
                name: "Usado",
                table: "DescontoResgatado",
                newName: "Used");

            migrationBuilder.UpdateData(
                table: "Desconto",
                keyColumn: "DescontoId",
                keyValue: 1,
                columns: new[] { "Descricao", "PontosNecessarios", "Valor" },
                values: new object[] { "10% discount", 10, 10.0 });

            migrationBuilder.UpdateData(
                table: "Desconto",
                keyColumn: "DescontoId",
                keyValue: 2,
                columns: new[] { "Descricao", "PontosNecessarios", "Valor" },
                values: new object[] { "25% discount", 20, 25.0 });
        }
    }
}
