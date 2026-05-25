using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoVarejo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PermissoesAuditPix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PixChave",
                table: "EmpresaConfigs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PixCidade",
                table: "EmpresaConfigs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PixNomeRecebedor",
                table: "EmpresaConfigs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: true),
                    Entidade = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RegistroId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    ValoresAntes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValoresDepois = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioPermissoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Permissao = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioPermissoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuarioPermissoes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Data",
                table: "AuditLogs",
                column: "Data");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Entidade_RegistroId",
                table: "AuditLogs",
                columns: new[] { "Entidade", "RegistroId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UsuarioId",
                table: "AuditLogs",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioPermissoes_UsuarioId_Permissao",
                table: "UsuarioPermissoes",
                columns: new[] { "UsuarioId", "Permissao" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "UsuarioPermissoes");

            migrationBuilder.DropColumn(
                name: "PixChave",
                table: "EmpresaConfigs");

            migrationBuilder.DropColumn(
                name: "PixCidade",
                table: "EmpresaConfigs");

            migrationBuilder.DropColumn(
                name: "PixNomeRecebedor",
                table: "EmpresaConfigs");
        }
    }
}
