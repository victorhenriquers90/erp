using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoVarejo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCamposNfe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProximoNumeroNfe",
                table: "EmpresaConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SerieNfe",
                table: "EmpresaConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CodigoMunicipioIbge",
                table: "Clientes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProximoNumeroNfe",
                table: "EmpresaConfigs");

            migrationBuilder.DropColumn(
                name: "SerieNfe",
                table: "EmpresaConfigs");

            migrationBuilder.DropColumn(
                name: "CodigoMunicipioIbge",
                table: "Clientes");
        }
    }
}
