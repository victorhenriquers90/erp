using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoVarejo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmpresaUrlApiApelido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Apelido",
                table: "EmpresaConfigs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UrlApi",
                table: "EmpresaConfigs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Apelido",
                table: "EmpresaConfigs");

            migrationBuilder.DropColumn(
                name: "UrlApi",
                table: "EmpresaConfigs");
        }
    }
}
