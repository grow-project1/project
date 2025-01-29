using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projeto.Migrations
{
    /// <inheritdoc />
    public partial class init2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Leiloes",
                keyColumn: "LeilaoId",
                keyValue: 1,
                columns: new[] { "DataFim", "DataInicio" },
                values: new object[] { new DateTime(2025, 1, 28, 12, 10, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 28, 12, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "Leiloes",
                keyColumn: "LeilaoId",
                keyValue: 2,
                columns: new[] { "DataFim", "DataInicio" },
                values: new object[] { new DateTime(2025, 1, 28, 13, 15, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 28, 13, 0, 0, 0, DateTimeKind.Unspecified) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Leiloes",
                keyColumn: "LeilaoId",
                keyValue: 1,
                columns: new[] { "DataFim", "DataInicio" },
                values: new object[] { new DateTime(2025, 1, 28, 22, 35, 39, 4, DateTimeKind.Local).AddTicks(3776), new DateTime(2025, 1, 28, 22, 25, 39, 1, DateTimeKind.Local).AddTicks(6354) });

            migrationBuilder.UpdateData(
                table: "Leiloes",
                keyColumn: "LeilaoId",
                keyValue: 2,
                columns: new[] { "DataFim", "DataInicio" },
                values: new object[] { new DateTime(2025, 1, 28, 22, 40, 39, 4, DateTimeKind.Local).AddTicks(4270), new DateTime(2025, 1, 28, 22, 25, 39, 4, DateTimeKind.Local).AddTicks(4263) });
        }
    }
}
