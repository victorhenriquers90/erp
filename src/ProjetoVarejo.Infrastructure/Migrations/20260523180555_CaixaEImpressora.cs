using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoVarejo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CaixaEImpressora : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ImpressoraBaud",
                table: "EmpresaConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ImpressoraColunas",
                table: "EmpresaConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ImpressoraDestino",
                table: "EmpresaConfigs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ImpressoraPorta",
                table: "EmpresaConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ImpressoraTipo",
                table: "EmpresaConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ImprimirAutomatico",
                table: "EmpresaConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "MovimentosCaixa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaixaSessaoId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    FormaPagamento = table.Column<int>(type: "int", nullable: true),
                    VendaId = table.Column<int>(type: "int", nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimentosCaixa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimentosCaixa_CaixasSessao_CaixaSessaoId",
                        column: x => x.CaixaSessaoId,
                        principalTable: "CaixasSessao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovimentosCaixa_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentosCaixa_Vendas_VendaId",
                        column: x => x.VendaId,
                        principalTable: "Vendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosCaixa_CaixaSessaoId_Tipo",
                table: "MovimentosCaixa",
                columns: new[] { "CaixaSessaoId", "Tipo" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosCaixa_UsuarioId",
                table: "MovimentosCaixa",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosCaixa_VendaId",
                table: "MovimentosCaixa",
                column: "VendaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimentosCaixa");

            migrationBuilder.DropColumn(
                name: "ImpressoraBaud",
                table: "EmpresaConfigs");

            migrationBuilder.DropColumn(
                name: "ImpressoraColunas",
                table: "EmpresaConfigs");

            migrationBuilder.DropColumn(
                name: "ImpressoraDestino",
                table: "EmpresaConfigs");

            migrationBuilder.DropColumn(
                name: "ImpressoraPorta",
                table: "EmpresaConfigs");

            migrationBuilder.DropColumn(
                name: "ImpressoraTipo",
                table: "EmpresaConfigs");

            migrationBuilder.DropColumn(
                name: "ImprimirAutomatico",
                table: "EmpresaConfigs");
        }
    }
}
