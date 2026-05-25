using Microsoft.EntityFrameworkCore.Migrations;

namespace ProjetoVarejo.Infrastructure.Migrations;

public partial class AddConfiguracaoNegocio : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ConfiguracaoNegocio",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                TipoNegocio = table.Column<int>(type: "int", nullable: false),
                DescricaoNegocio = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: ""),
                ConfiguracaoInicial = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                ModulosAtivos = table.Column<long>(type: "bigint", nullable: false),
                DataAtualizacao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                Versao = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ConfiguracaoNegocio", x => x.Id);
            });

        // Inserir registro inicial padrão
        migrationBuilder.InsertData(
            table: "ConfiguracaoNegocio",
            columns: new[] { "Id", "TipoNegocio", "DescricaoNegocio", "ConfiguracaoInicial", "ModulosAtivos", "DataAtualizacao", "Versao" },
            values: new object[] { 1, 0, "", false, 15L, DateTime.UtcNow, 1 });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ConfiguracaoNegocio");
    }
}
