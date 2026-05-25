using ProjetoVarejo.Application.Configuracao;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Enums;

namespace ProjetoVarejo.Desktop.Forms;

/// <summary>
/// Formulário administrativo para gerenciar módulos e tipo de negócio.
/// Permite ativar/desativar módulos e visualizar/alterar o tipo de negócio configurado.
/// </summary>
[ModuloRequerido(ModuloSistema.Backup)]  // Apenas administradores com acesso a Backup
public class FrmGerenciadorModulos : Form
{
    private readonly ConfiguracaoNegocioService _svc;
    private ProjetoVarejo.Domain.Configuracao.ConfiguracaoNegocio _config = null!;
    private CheckedListBox chkModulos = null!;
    private Label lblTipo = null!;
    private Label lblDataAtualizado = null!;
    private Button btnSalvar = null!;
    private Button btnReset = null!;
    private TextBox txtDescricao = null!;

    public FrmGerenciadorModulos(ConfiguracaoNegocioService svc)
    {
        _svc = svc;
        InitUi();
        Shown += async (s, e) => await CarregarAsync();
    }

    private void InitUi()
    {
        Text = "Gerenciador de Módulos";
        Size = new Size(700, 750);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = Tema.CorFundo;
        MaximizeBox = false;
        MinimizeBox = false;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderPagina("Gerenciador de Módulos", "Configure os módulos ativos do sistema");

        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(20) };
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };

        // ===== Informações =====
        int y = 0;
        pnl.Controls.Add(Inputs.Rotulo("TIPO DE NEGÓCIO", 0, y));
        lblTipo = new Label { Left = 0, Top = y + 20, Width = 600, Height = 28, Font = Tema.FontCorpoBold, ForeColor = Tema.CorTextoEscuro, BackColor = Color.Transparent };
        pnl.Controls.Add(lblTipo);
        y += 60;

        pnl.Controls.Add(Inputs.Rotulo("DESCRIÇÃO", 0, y));
        txtDescricao = new TextBox { Left = 0, Top = y + 20, Width = 600, Height = 60, Multiline = true, Font = Tema.FontCorpo, BorderStyle = BorderStyle.FixedSingle };
        pnl.Controls.Add(txtDescricao);
        y += 90;

        pnl.Controls.Add(Inputs.Rotulo("ÚLTIMA ATUALIZAÇÃO", 0, y));
        lblDataAtualizado = new Label { Left = 0, Top = y + 20, Width = 600, Height = 28, Font = Tema.FontCorpo, ForeColor = Tema.CorTextoMedio, BackColor = Color.Transparent };
        pnl.Controls.Add(lblDataAtualizado);
        y += 60;

        // ===== Módulos =====
        pnl.Controls.Add(Inputs.Rotulo("MÓDULOS ATIVOS", 0, y));
        chkModulos = new CheckedListBox
        {
            Left = 0,
            Top = y + 20,
            Width = 600,
            Height = 280,
            Font = Tema.FontCorpo,
            BackColor = Tema.CorCardAlt,
            BorderStyle = BorderStyle.FixedSingle,
            CheckOnClick = true
        };
        pnl.Controls.Add(chkModulos);
        y += 320;

        // ===== Botões =====
        var pnlBotoes = new Panel { Left = 0, Top = y, Width = 600, Height = 50, BackColor = Tema.CorCard };

        btnSalvar = Botoes.Sucesso("Salvar Configuração", 160, 44);
        btnSalvar.Left = 0;
        btnSalvar.Top = 0;
        btnSalvar.Click += async (s, e) => await SalvarAsync();
        pnlBotoes.Controls.Add(btnSalvar);

        btnReset = Botoes.Aviso("Restaurar Padrão", 160, 44);
        btnReset.Left = 180;
        btnReset.Top = 0;
        btnReset.Click += async (s, e) => await RestaurarPadraoAsync();
        pnlBotoes.Controls.Add(btnReset);

        var btnFechar = Botoes.Ghost("Fechar", 80, 44);
        btnFechar.Left = 360;
        btnFechar.Top = 0;
        btnFechar.Click += (s, e) => DialogResult = DialogResult.OK;
        pnlBotoes.Controls.Add(btnFechar);

        pnl.Controls.Add(pnlBotoes);

        card.Controls.Add(pnl);
        Controls.Add(card);
        Controls.Add(header);
    }

    private async Task CarregarAsync()
    {
        _config = await _svc.ObterConfiguracao();

        // Carregar informações
        var tipo = _config.TipoNegocio;
        lblTipo.Text = tipo == (TipoNegocio)0
            ? "⚠️ NÃO CONFIGURADO"
            : $"🔷 {_config.ObterDescricaoTipo()} ({tipo})";

        txtDescricao.Text = _config.DescricaoNegocio ?? "";
        lblDataAtualizado.Text = $"Atualizado em {_config.DataAtualizacao:dd/MM/yyyy HH:mm:ss}";

        // Carregar módulos
        PopularModulos();
    }

    private void PopularModulos()
    {
        chkModulos.Items.Clear();

        foreach (var modulo in GetAllModulos())
        {
            var idx = chkModulos.Items.Add(modulo.Item1);
            var estaAtivo = (_config.ModulosAtivos & modulo.Item2) == modulo.Item2;
            chkModulos.SetItemChecked(idx, estaAtivo);
        }
    }

    private async Task SalvarAsync()
    {
        // Reconstruir flags a partir das caixas marcadas
        var novosModulos = ModuloSistema.PDV;  // Start com um valor
        foreach (int idx in chkModulos.CheckedIndices)
        {
            var modulos = GetAllModulos();
            novosModulos |= modulos[idx].Item2;
        }

        _config.ModulosAtivos = novosModulos;
        _config.DescricaoNegocio = txtDescricao.Text;

        await _svc.SalvarConfiguracao(_config);

        Toast.Mostrar("Configuração salva com sucesso.", TipoToast.Sucesso, owner: this);
        await CarregarAsync();
    }

    private async Task RestaurarPadraoAsync()
    {
        var result = MessageBox.Show(
            "Deseja restaurar os módulos recomendados para este tipo de negócio?\n\n" +
            "Os módulos obrigatórios sempre serão ativados.",
            "Restaurar Padrão",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            if (_config.TipoNegocio == (TipoNegocio)0)
            {
                Toast.Mostrar("Configure o tipo de negócio primeiro.", TipoToast.Aviso, owner: this);
                return;
            }

            _config.ModulosAtivos = ModulosPorTipo.ObterModulosRecomendados(_config.TipoNegocio);
            await _svc.SalvarConfiguracao(_config);

            Toast.Mostrar("Módulos restaurados para o padrão.", TipoToast.Sucesso, owner: this);
            await CarregarAsync();
        }
    }

    private static List<(string Label, ModuloSistema Modulo)> GetAllModulos()
    {
        return new()
        {
            ("✓ PDV - Ponto de Venda", ModuloSistema.PDV),
            ("✓ Estoque - Gestão de Inventário", ModuloSistema.Estoque),
            ("✓ Cadastros - Produtos, Clientes, Fornecedores", ModuloSistema.Cadastros),
            ("✓ Financeiro - Contas a Pagar/Receber", ModuloSistema.Financeiro),
            ("Fiscal - NFC-e e Integração SEFAZ", ModuloSistema.Fiscal),
            ("Produção - Controle de Produção", ModuloSistema.Producao),
            ("Pesagem - Integração com Balança", ModuloSistema.Pesagem),
            ("Pré-venda - Promoções", ModuloSistema.Prevenda),
            ("Comissões - Vendedores", ModuloSistema.Comissoes),
            ("✓ Relatórios - Analytics e Indicadores", ModuloSistema.Relatorios),
            ("✓ Auditoria - Rastreamento de Alterações", ModuloSistema.Auditoria),
            ("✓ Backup - Cópia de Segurança", ModuloSistema.Backup),
            ("PIX - Integração com PIX", ModuloSistema.Pix),
            ("TEF - Transferência Eletrônica de Fundos", ModuloSistema.Tef),
            ("Receitas - Gestão de Receitas (Farmácia)", ModuloSistema.Receitas),
            ("Comandas - Controle de Mesas (Restaurante)", ModuloSistema.Comandas)
        };
    }
}
