using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projeto.Migrations
{
    /// <inheritdoc />
    public partial class init4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Leiloes",
                keyColumn: "LeilaoId",
                keyValue: 1,
                columns: new[] { "DataFim", "DataInicio" },
                values: new object[] { new DateTime(2025, 1, 28, 23, 10, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 28, 12, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Leiloes",
                keyColumn: "LeilaoId",
                keyValue: 2,
                column: "DataFim",
                value: new DateTime(2025, 1, 28, 22, 45, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Leiloes",
                keyColumn: "LeilaoId",
                keyValue: 1,
                columns: new[] { "DataFim", "DataInicio" },
                values: new object[] { new DateTime(2025, 1, 28, 22, 41, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 28, 23, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Leiloes",
                keyColumn: "LeilaoId",
                keyValue: 2,
                column: "DataFim",
                value: new DateTime(2025, 1, 28, 13, 15, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
