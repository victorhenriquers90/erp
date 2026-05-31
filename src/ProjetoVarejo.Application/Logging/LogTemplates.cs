namespace ProjetoVarejo.Application.Logging;

/// <summary>
/// Centralized log message templates for consistent logging across the application.
/// </summary>
public static class LogTemplates
{
    // ==================== VENDA OPERATIONS ====================
    public const string VendaIniciada = "Venda {VendaId} iniciada por usuário {Usuario}";
    public const string VendaCriadaComSucesso = "Venda {VendaId} criada com sucesso. Número: {Numero}";
    public const string ItemAdicionado = "Item adicionado à venda {VendaId}: Produto {ProdutoId}, Quantidade {Quantidade}, Valor unitário {ValorUnitario:C}";
    public const string ItemRemovido = "Item {ItemId} removido da venda {VendaId}";
    public const string VendaFinalizada = "Venda {VendaId} finalizada com sucesso. Total: {Total:C}, Valor pago: {ValorPago:C}, Troco: {Troco:C}";
    public const string VendaCancelada = "Venda {VendaId} cancelada. Motivo: {Motivo}";
    public const string DescontoAplicado = "Desconto de {Desconto:C} aplicado à venda {VendaId}";
    public const string TotaisRecalculados = "Totais recalculados para venda {VendaId}. SubTotal: {SubTotal:C}, Total: {Total:C}";

    // ==================== CAIXA OPERATIONS ====================
    public const string CaixaAberto = "Caixa {CaixaId} aberto por usuário {Usuario} com valor de abertura {Valor:C}";
    public const string CaixaFechado = "Caixa {CaixaId} fechado. Valor esperado: {ValorEsperado:C}, Valor informado: {ValorInformado:C}, Diferença: {Diferenca:C}";
    public const string SangriaRegistrada = "Sangria de {Valor:C} registrada no caixa {CaixaId}. Motivo: {Motivo}";
    public const string SuprimentoRegistrado = "Suprimento de {Valor:C} registrado no caixa {CaixaId}. Motivo: {Motivo}";
    public const string VendaRegistradaEmCaixa = "Venda {VendaId} registrada no caixa {CaixaId}. Forma de pagamento: {FormaPagamento}";
    public const string CaixaJaAberto = "Tentativa de abrir caixa quando já existe caixa aberto para o usuário";

    // ==================== ESTOQUE OPERATIONS ====================
    public const string MovimentoEstoque = "Movimento de estoque registrado: Produto {ProdutoId}, Tipo {Tipo}, Quantidade {Quantidade}";
    public const string EstoqueAbaixoMinimo = "ALERTA: Produto {ProdutoId} ({NomeProduto}) abaixo do estoque mínimo. Atual: {EstoqueAtual}, Mínimo: {EstoqueMinimo}";
    public const string EstoqueAjustado = "Estoque ajustado para Produto {ProdutoId}. Nova quantidade: {NovaQuantidade}";
    public const string EstoqueInsuficiente = "Estoque insuficiente para Produto {ProdutoId}. Solicitado: {Solicitado}, Disponível: {Disponivel}";

    // ==================== USUARIO OPERATIONS ====================
    public const string LoginSucesso = "Usuário {Usuario} autenticado com sucesso. Perfil: {Perfil}";
    public const string LoginFalha = "Falha ao autenticar usuário {Usuario}. Motivo: {Motivo}";
    public const string LogoutSucesso = "Usuário {Usuario} deslogado com sucesso";
    public const string UsuarioCriado = "Novo usuário criado: {Usuario} com perfil {Perfil}";
    public const string UsuarioInativo = "Tentativa de operação com usuário inativo: {Usuario}";

    // ==================== VALIDACAO ====================
    public const string ValidacaoFalhou = "Validação falhou para entidade {Entidade}. Erros: {Erros}";
    public const string ValidacaoSucesso = "Validação bem-sucedida para entidade {Entidade}";

    // ==================== TRANSACAO ====================
    public const string TransacaoIniciada = "Transação iniciada para operação {Operacao}";
    public const string TransacaoConfirmada = "Transação confirmada para operação {Operacao}";
    public const string TransacaoRevertida = "Transação revertida para operação {Operacao}. Motivo: {Motivo}";
    public const string TransacaoFalhou = "Transação falhou para operação {Operacao}. Exceção: {Excecao}";

    // ==================== NFCE ====================
    public const string NfceEmitida = "NFC-e emitida com sucesso. Número: {Numero}, Série: {Serie}, Venda: {VendaId}, Chave: {ChaveAcesso}";
    public const string NfceCancelada = "NFC-e {Numero} cancelada. Motivo: {Motivo}";
    public const string NfceErro = "Erro ao emitir NFC-e para venda {VendaId}: {Erro}";
    public const string NfceContingencia = "NFC-e emitida em contingência para venda {VendaId}";
    public const string NfceReenviada = "NFC-e {Numero} reenviada do contingenciamento";

    // ==================== PRODUTO ====================
    public const string ProdutoConsultado = "Produto {ProdutoId} ({Codigo}) consultado";
    public const string ProdutosCertados = "Produtos pesquisados. Termo: {Termo}, Resultados encontrados: {Quantidade}";
    public const string ProdutoInativo = "Tentativa de usar produto inativo: {ProdutoId} ({Codigo})";

    // ==================== CATEGORIA ====================
    public const string CategoriaConsultada = "Categoria {CategoriaId} consultada";
    public const string CategoriasCertadas = "Categorias carregadas. Total: {Quantidade}";

    // ==================== BANCO DE DADOS ====================
    public const string QueryExecutada = "Query executada com sucesso. Tipo: {TipoQuery}";
    public const string ErroQuery = "Erro ao executar query: {Erro}";
    public const string ConexaoBancoTestada = "Conexão com banco de dados testada com sucesso";
    public const string ErroConexaoBanco = "Erro ao conectar com banco de dados: {Erro}";

    // ==================== SISTEMA ====================
    public const string AplicacaoIniciada = "Aplicação iniciada. Versão: {Versao}, Ambiente: {Ambiente}";
    public const string AplicacaoFinalizada = "Aplicação finalizada";
    public const string ErroNaoTratado = "Erro não tratado durante {Operacao}: {Mensagem}";
    public const string ErroValidacao = "Erro de validação: {Mensagem}";
    public const string ErroServidor = "Erro do servidor: {Mensagem}";
    public const string ConfiguracaoCarregada = "Configuração carregada com sucesso. App: {App}, Env: {Env}";

    // ==================== BACKUP ====================
    public const string BackupIniciado = "Backup iniciado. Caminho: {Caminho}";
    public const string BackupConcluido = "Backup concluído com sucesso. Arquivo: {Arquivo}";
    public const string ErroBackup = "Erro durante backup: {Erro}";

    // ==================== AUDIT ====================
    public const string OperacaoAuditada = "Operação auditada: {Operacao} realizada por {Usuario} em entidade {Entidade}";
}
