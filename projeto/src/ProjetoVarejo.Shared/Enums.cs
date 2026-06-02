namespace ProjetoVarejo.Shared;

public enum TipoMovimentoEstoque
{
    Entrada = 1,
    Saida = 2,
    AjusteEntrada = 3,
    AjusteSaida = 4,
    Devolucao = 5
}

public enum StatusVenda
{
    EmAberto = 0,
    Finalizada = 1,
    Cancelada = 2
}

public enum FormaPagamentoTipo
{
    Dinheiro = 1,
    Debito = 2,
    Credito = 3,
    Pix = 4,
    Boleto = 5,
    Crediario = 6,
    ValeRefeicao = 7,
    Outros = 99
}

public enum StatusConta
{
    EmAberto = 0,
    Paga = 1,
    Atrasada = 2,
    Cancelada = 3
}

public enum TipoConta
{
    Pagar = 1,
    Receber = 2
}

public enum StatusNotaFiscal
{
    NaoEmitida = 0,
    EmDigitacao = 1,
    Autorizada = 2,
    Rejeitada = 3,
    Cancelada = 4,
    Contingencia = 5
}

public enum PerfilUsuario
{
    Administrador = 1,
    Gerente = 2,
    Caixa = 3,
    Estoquista = 4
}

public enum Permissao
{
    // Vendas
    AbrirPdv = 100,
    AplicarDesconto = 101,
    CancelarVenda = 102,
    EmitirNfce = 103,
    CancelarNfce = 104,
    InutilizarNfce = 105,
    // Caixa
    AbrirCaixa = 200,
    FecharCaixa = 201,
    Sangria = 202,
    Suprimento = 203,
    // Cadastros
    GerenciarProdutos = 300,
    GerenciarClientes = 301,
    GerenciarFornecedores = 302,
    GerenciarUsuarios = 303,
    // Estoque
    LancarEntradaEstoque = 400,
    LancarSaidaEstoque = 401,
    ImportarXmlNfe = 402,
    // Financeiro
    GerenciarContas = 500,
    QuitarContas = 501,
    // Sistema
    AcessarRelatorios = 600,
    GerenciarConfiguracoes = 601,
    ExecutarBackup = 602
}

public enum UnidadeMedida
{
    UN = 1,
    KG = 2,
    G = 3,
    L = 4,
    ML = 5,
    M = 6,
    CM = 7,
    CX = 8,
    PCT = 9,
    DZ = 10
}
