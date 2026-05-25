using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoVarejo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CpfCnpj = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PessoaJuridica = table.Column<bool>(type: "bit", nullable: false),
                    InscricaoEstadual = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Telefone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cep = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Logradouro = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Numero = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Complemento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bairro = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cidade = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Uf = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LimiteCredito = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmpresaConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RazaoSocial = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NomeFantasia = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cnpj = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InscricaoEstadual = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InscricaoMunicipal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cep = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Logradouro = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Complemento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bairro = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cidade = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Uf = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CodigoMunicipioIbge = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Telefone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegimeTributario = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CertificadoCaminho = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CertificadoSenha = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CscId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CscToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AmbienteHomologacao = table.Column<bool>(type: "bit", nullable: false),
                    ProximoNumeroNfce = table.Column<int>(type: "int", nullable: false),
                    SerieNfce = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmpresaConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fornecedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RazaoSocial = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NomeFantasia = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Cnpj = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InscricaoEstadual = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Contato = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cep = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Logradouro = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Numero = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Complemento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bairro = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cidade = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Uf = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fornecedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotasFiscais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Serie = table.Column<int>(type: "int", nullable: false),
                    Modelo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChaveAcesso = table.Column<string>(type: "nvarchar(44)", maxLength: 44, nullable: true),
                    Protocolo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AutorizadaEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    XmlEnviado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    XmlRetorno = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MensagemSefaz = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JustificativaCancelamento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CanceladaEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VendaId = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotasFiscais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Login = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SenhaHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Perfil = table.Column<int>(type: "int", nullable: false),
                    UltimoAcesso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CodigoBarras = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CategoriaId = table.Column<int>(type: "int", nullable: true),
                    Unidade = table.Column<int>(type: "int", nullable: false),
                    PrecoCusto = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PrecoVenda = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Estoque = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    EstoqueMinimo = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ControlaEstoque = table.Column<bool>(type: "bit", nullable: false),
                    PermiteVendaFracionada = table.Column<bool>(type: "bit", nullable: false),
                    Ncm = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Cest = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Cfop = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    Origem = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    CstIcms = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    AliquotaIcms = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CstPisCofins = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Produtos_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CaixasSessao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioAberturaId = table.Column<int>(type: "int", nullable: false),
                    AbertaEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechadaEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioFechamentoId = table.Column<int>(type: "int", nullable: true),
                    ValorAbertura = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ValorFechamentoInformado = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ValorFechamentoCalculado = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Diferenca = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaixasSessao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaixasSessao_Usuarios_UsuarioAberturaId",
                        column: x => x.UsuarioAberturaId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaixasSessao_Usuarios_UsuarioFechamentoId",
                        column: x => x.UsuarioFechamentoId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vendas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DataVenda = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Desconto = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Acrescimo = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ValorPago = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Troco = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FinalizadaEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CanceladaEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NotaFiscalId = table.Column<int>(type: "int", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vendas_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Vendas_NotasFiscais_NotaFiscalId",
                        column: x => x.NotaFiscalId,
                        principalTable: "NotasFiscais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Vendas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContasFinanceiras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DocumentoNumero = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataEmissao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataVencimento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataPagamento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Valor = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ValorPago = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Juros = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Multa = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Desconto = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FormaPagamento = table.Column<int>(type: "int", nullable: true),
                    ClienteId = table.Column<int>(type: "int", nullable: true),
                    FornecedorId = table.Column<int>(type: "int", nullable: true),
                    VendaId = table.Column<int>(type: "int", nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContasFinanceiras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContasFinanceiras_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContasFinanceiras_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContasFinanceiras_Vendas_VendaId",
                        column: x => x.VendaId,
                        principalTable: "Vendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ItensVenda",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VendaId = table.Column<int>(type: "int", nullable: false),
                    ProdutoId = table.Column<int>(type: "int", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Desconto = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensVenda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensVenda_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItensVenda_Vendas_VendaId",
                        column: x => x.VendaId,
                        principalTable: "Vendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovimentosEstoque",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProdutoId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SaldoAnterior = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SaldoAtual = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CustoUnitario = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Documento = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VendaId = table.Column<int>(type: "int", nullable: true),
                    FornecedorId = table.Column<int>(type: "int", nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimentosEstoque", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimentosEstoque_Fornecedores_FornecedorId",
                        column: x => x.FornecedorId,
                        principalTable: "Fornecedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MovimentosEstoque_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentosEstoque_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimentosEstoque_Vendas_VendaId",
                        column: x => x.VendaId,
                        principalTable: "Vendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PagamentosVenda",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VendaId = table.Column<int>(type: "int", nullable: false),
                    FormaPagamento = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Parcelas = table.Column<int>(type: "int", nullable: false),
                    Autorizacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagamentosVenda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagamentosVenda_Vendas_VendaId",
                        column: x => x.VendaId,
                        principalTable: "Vendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaixasSessao_UsuarioAberturaId",
                table: "CaixasSessao",
                column: "UsuarioAberturaId");

            migrationBuilder.CreateIndex(
                name: "IX_CaixasSessao_UsuarioFechamentoId",
                table: "CaixasSessao",
                column: "UsuarioFechamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Nome",
                table: "Categorias",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_CpfCnpj",
                table: "Clientes",
                column: "CpfCnpj");

            migrationBuilder.CreateIndex(
                name: "IX_ContasFinanceiras_ClienteId",
                table: "ContasFinanceiras",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasFinanceiras_FornecedorId",
                table: "ContasFinanceiras",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasFinanceiras_VendaId",
                table: "ContasFinanceiras",
                column: "VendaId");

            migrationBuilder.CreateIndex(
                name: "IX_Fornecedores_Cnpj",
                table: "Fornecedores",
                column: "Cnpj",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItensVenda_ProdutoId",
                table: "ItensVenda",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensVenda_VendaId",
                table: "ItensVenda",
                column: "VendaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEstoque_FornecedorId",
                table: "MovimentosEstoque",
                column: "FornecedorId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEstoque_ProdutoId",
                table: "MovimentosEstoque",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEstoque_UsuarioId",
                table: "MovimentosEstoque",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosEstoque_VendaId",
                table: "MovimentosEstoque",
                column: "VendaId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_ChaveAcesso",
                table: "NotasFiscais",
                column: "ChaveAcesso");

            migrationBuilder.CreateIndex(
                name: "IX_PagamentosVenda_VendaId",
                table: "PagamentosVenda",
                column: "VendaId");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_CategoriaId",
                table: "Produtos",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_Codigo",
                table: "Produtos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_CodigoBarras",
                table: "Produtos",
                column: "CodigoBarras");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Login",
                table: "Usuarios",
                column: "Login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_ClienteId",
                table: "Vendas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_NotaFiscalId",
                table: "Vendas",
                column: "NotaFiscalId",
                unique: true,
                filter: "[NotaFiscalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_Numero",
                table: "Vendas",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_UsuarioId",
                table: "Vendas",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaixasSessao");

            migrationBuilder.DropTable(
                name: "ContasFinanceiras");

            migrationBuilder.DropTable(
                name: "EmpresaConfigs");

            migrationBuilder.DropTable(
                name: "ItensVenda");

            migrationBuilder.DropTable(
                name: "MovimentosEstoque");

            migrationBuilder.DropTable(
                name: "PagamentosVenda");

            migrationBuilder.DropTable(
                name: "Fornecedores");

            migrationBuilder.DropTable(
                name: "Produtos");

            migrationBuilder.DropTable(
                name: "Vendas");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "NotasFiscais");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
