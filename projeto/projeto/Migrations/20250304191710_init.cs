using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace growTests.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Itens",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrecoInicial = table.Column<double>(type: "float", nullable: false),
                    Categoria = table.Column<int>(type: "int", nullable: false),
                    Sustentavel = table.Column<bool>(type: "bit", nullable: false),
                    FotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Itens", x => x.ItemId);
                });

            migrationBuilder.CreateTable(
                name: "LoginModel",
                columns: table => new
                {
                    LoginModelId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginModel", x => x.LoginModelId);
                });

            migrationBuilder.CreateTable(
                name: "Utilizador",
                columns: table => new
                {
                    UtilizadorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Morada = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CodigoPostal = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Telemovel = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Pais = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstadoConta = table.Column<int>(type: "int", nullable: false),
                    Pontos = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilizador", x => x.UtilizadorId);
                });

            migrationBuilder.CreateTable(
                name: "VerificationModel",
                columns: table => new
                {
                    VerificationModelId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerificationCode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationModel", x => x.VerificationModelId);
                });

            migrationBuilder.CreateTable(
                name: "Leiloes",
                columns: table => new
                {
                    LeilaoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataFim = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValorIncrementoMinimo = table.Column<double>(type: "float", nullable: false),
                    Vencedor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UtilizadorId = table.Column<int>(type: "int", nullable: false),
                    ValorAtualLance = table.Column<double>(type: "float", nullable: false),
                    EstadoLeilao = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leiloes", x => x.LeilaoId);
                    table.ForeignKey(
                        name: "FK_Leiloes_Itens_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Itens",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Desconto",
                columns: table => new
                {
                    DescontoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Valor = table.Column<double>(type: "float", nullable: false),
                    PontosNecessarios = table.Column<int>(type: "int", nullable: false),
                    DataObtencao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataFim = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UtilizadorId = table.Column<int>(type: "int", nullable: true),
                    IsLoja = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Desconto", x => x.DescontoId);
                    table.ForeignKey(
                        name: "FK_Desconto_Utilizador_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalTable: "Utilizador",
                        principalColumn: "UtilizadorId");
                });

            migrationBuilder.CreateTable(
                name: "LogUtilizadores",
                columns: table => new
                {
                    LogUtilizadorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LogDataLogin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UtilizadorId = table.Column<int>(type: "int", nullable: false),
                    LogUtilizadorEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsLoginSuccess = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogUtilizadores", x => x.LogUtilizadorId);
                    table.ForeignKey(
                        name: "FK_LogUtilizadores_Utilizador_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalTable: "Utilizador",
                        principalColumn: "UtilizadorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Licitacoes",
                columns: table => new
                {
                    LicitacaoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataLicitacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValorLicitacao = table.Column<double>(type: "float", nullable: false),
                    LeilaoId = table.Column<int>(type: "int", nullable: false),
                    UtilizadorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Licitacoes", x => x.LicitacaoId);
                    table.ForeignKey(
                        name: "FK_Licitacoes_Leiloes_LeilaoId",
                        column: x => x.LeilaoId,
                        principalTable: "Leiloes",
                        principalColumn: "LeilaoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Licitacoes_Utilizador_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalTable: "Utilizador",
                        principalColumn: "UtilizadorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DescontoResgatado",
                columns: table => new
                {
                    DescontoResgatadoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DescontoId = table.Column<int>(type: "int", nullable: false),
                    UtilizadorId = table.Column<int>(type: "int", nullable: false),
                    DataResgate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataValidade = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DescontoResgatado", x => x.DescontoResgatadoId);
                    table.ForeignKey(
                        name: "FK_DescontoResgatado_Desconto_DescontoId",
                        column: x => x.DescontoId,
                        principalTable: "Desconto",
                        principalColumn: "DescontoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DescontoResgatado_Utilizador_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalTable: "Utilizador",
                        principalColumn: "UtilizadorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Desconto",
                columns: new[] { "DescontoId", "DataFim", "DataObtencao", "Descricao", "IsLoja", "PontosNecessarios", "UtilizadorId", "Valor" },
                values: new object[,]
                {
                    { 1, null, null, "Desconto de 10% na Loja", true, 10, null, 10.0 },
                    { 2, null, null, "Desconto de 25% desconto", true, 20, null, 25.0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Desconto_UtilizadorId",
                table: "Desconto",
                column: "UtilizadorId");

            migrationBuilder.CreateIndex(
                name: "IX_DescontoResgatado_DescontoId",
                table: "DescontoResgatado",
                column: "DescontoId");

            migrationBuilder.CreateIndex(
                name: "IX_DescontoResgatado_UtilizadorId",
                table: "DescontoResgatado",
                column: "UtilizadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Leiloes_ItemId",
                table: "Leiloes",
                column: "ItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Licitacoes_LeilaoId",
                table: "Licitacoes",
                column: "LeilaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Licitacoes_UtilizadorId",
                table: "Licitacoes",
                column: "UtilizadorId");

            migrationBuilder.CreateIndex(
                name: "IX_LogUtilizadores_UtilizadorId",
                table: "LogUtilizadores",
                column: "UtilizadorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DescontoResgatado");

            migrationBuilder.DropTable(
                name: "Licitacoes");

            migrationBuilder.DropTable(
                name: "LoginModel");

            migrationBuilder.DropTable(
                name: "LogUtilizadores");

            migrationBuilder.DropTable(
                name: "VerificationModel");

            migrationBuilder.DropTable(
                name: "Desconto");

            migrationBuilder.DropTable(
                name: "Leiloes");

            migrationBuilder.DropTable(
                name: "Utilizador");

            migrationBuilder.DropTable(
                name: "Itens");
        }
    }
}
