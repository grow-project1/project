using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace projeto.Migrations
{
    /// <inheritdoc />
    public partial class init5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Usado",
                table: "DescontoResgatado");
        }

       
    }
}
