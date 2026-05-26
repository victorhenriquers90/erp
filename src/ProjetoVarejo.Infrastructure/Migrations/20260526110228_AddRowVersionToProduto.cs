using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoVarejo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersionToProduto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Adiciona RowVersion para controle otimista de concorrência em Produto
            // Permite detectar conflitos quando múltiplos usuários atualizam estoque simultaneamente
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Produtos",
                type: "rowversion",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Produtos");
        }
    }
}
