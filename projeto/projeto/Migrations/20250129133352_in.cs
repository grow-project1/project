using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace projeto.Migrations
{
    /// <inheritdoc />
    public partial class @in : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Desconto",
                keyColumn: "DescontoId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Desconto",
                keyColumn: "DescontoId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Leiloes",
                keyColumn: "LeilaoId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Leiloes",
                keyColumn: "LeilaoId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Itens",
                keyColumn: "ItemId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Itens",
                keyColumn: "ItemId",
                keyValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Desconto",
                columns: new[] { "DescontoId", "DataFim", "DataObtencao", "Descricao", "IsLoja", "PontosNecessarios", "UtilizadorId", "Valor" },
                values: new object[,]
                {
                    { 1, null, null, "10% desconto", true, 10, null, 10.0 },
                    { 2, null, null, "20% desconto", true, 20, null, 20.0 }
                });

            migrationBuilder.InsertData(
                table: "Itens",
                columns: new[] { "ItemId", "Categoria", "Descricao", "FotoUrl", "PrecoInicial", "Sustentavel", "Titulo" },
                values: new object[,]
                {
                    { 1, 2, "Relógio luxuoso em ouro 18k.", "", 500.0, false, "Relógio de Ouro" },
                    { 2, 4, "Bicicleta clássica para colecionadores.", "", 200.0, true, "Bicicleta Vintage" }
                });

            migrationBuilder.InsertData(
                table: "Leiloes",
                columns: new[] { "LeilaoId", "DataFim", "DataInicio", "ItemId", "UtilizadorId", "ValorIncrementoMinimo", "Vencedor" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 1, 28, 23, 10, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 28, 12, 0, 0, 0, DateTimeKind.Unspecified), 1, 0, 5.0, null },
                    { 2, new DateTime(2025, 1, 28, 22, 45, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 28, 13, 0, 0, 0, DateTimeKind.Unspecified), 2, 0, 10.0, null }
                });
        }
    }
}
