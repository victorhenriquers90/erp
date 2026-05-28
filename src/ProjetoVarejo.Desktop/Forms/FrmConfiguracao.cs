using ProjetoVarejo.Application.Configuracao;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Enums;

namespace ProjetoVarejo.Desktop.Forms;

public class FrmConfiguracao : Form
{
    private readonly ConfiguracaoNegocioService _configuracao;
    private readonly ImplantacaoService _implantacao;
    private readonly SessaoApp _sessao;
    private TextBox txtDescricao = null!;
    private Button btnConfigurar = null!;
    private Panel pnlTipos = null!;
    private Label lblSelecionado = null!;
    private TipoNegocio? _tipoSelecionado;

    public FrmConfiguracao(ConfiguracaoNegocioService configuracao, ImplantacaoService implantacao, SessaoApp sessao)
    {
        _configuracao = configuracao;
        _implantacao = implantacao;
        _sessao = sessao;
        InitUi();
    }

    private void InitUi()
    {
        Text = "Configuração do Sistema";
        Size = new Size(900, 700);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Tema.CorFundo;
        DoubleBuffered = true;

        // === Container Principal ===
        var container = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorFundo, Padding = new Padding(40) };

        // === Cabeçalho ===
        var header = new Panel { Dock = DockStyle.Top, Height = 120, BackColor = Tema.CorFundo };
        var titulo = new Label
        {
            Text = "⚙️ Configuração Inicial do Sistema",
            Dock = DockStyle.Top,
            Height = 50,
            Font = new Font(Tema.FontFamily, 24, FontStyle.Bold),
            ForeColor = Tema.CorTextoEscuro,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var descricao = new Label
        {
            Text = "Selecione o tipo de negócio para otimizar o sistema com os módulos necessários",
            Dock = DockStyle.Top,
            Height = 60,
            Font = new Font(Tema.FontFamily, 11),
            ForeColor = Tema.CorTextoMedio,
            TextAlign = ContentAlignment.TopLeft
        };
        header.Controls.Add(descricao);
        header.Controls.Add(titulo);

        // === Conteúdo Principal ===
        var conteudo = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorFundo, Padding = new Padding(0, 0, 0, 20) };

        // --- Coluna Esquerda: Grid de Tipos ---
        var colEsq = new Panel { Dock = DockStyle.Left, Width = 450, BackColor = Tema.CorFundo, Padding = new Padding(0, 0, 20, 0) };
        var lblTipos = new Label
        {
            Text = "Tipo de Negócio",
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font(Tema.FontFamily, 12, FontStyle.Bold),
            ForeColor = Tema.CorTextoEscuro,
            BackColor = Color.Transparent
        };

        pnlTipos = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 0)
        };

        // Adicionar botões para cada tipo
        var tipos = Enum.GetValues(typeof(TipoNegocio)).Cast<TipoNegocio>().ToList();
        foreach (var tipo in tipos)
        {
            pnlTipos.Controls.Add(CriarBotaoTipo(tipo));
        }

        colEsq.Controls.Add(pnlTipos);
        colEsq.Controls.Add(lblTipos);

        // --- Coluna Direita: Detalhes ---
        var colDir = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorFundo };
        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(24) };

        var lblDetalhes = new Label
        {
            Text = "Detalhes da Configuração",
            Dock = DockStyle.Top,
            Height = 32,
            Font = new Font(Tema.FontFamily, 14, FontStyle.Bold),
            ForeColor = Tema.CorTextoEscuro,
            BackColor = Color.Transparent
        };

        lblSelecionado = new Label
        {
            Text = "Nenhum tipo selecionado",
            Dock = DockStyle.Top,
            Height = 40,
            Font = new Font(Tema.FontFamily, 20, FontStyle.Bold),
            ForeColor = Tema.CorPrimaria,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };

        var lblDescricao = new Label
        {
            Text = "DESCRIÇÃO DA EMPRESA",
            Dock = DockStyle.Top,
            Height = 24,
            Font = new Font(Tema.FontFamily, 9, FontStyle.Bold),
            ForeColor = Tema.CorTextoMedio,
            Padding = new Padding(0, 20, 0, 8),
            BackColor = Color.Transparent
        };

        txtDescricao = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 48,
            BorderStyle = BorderStyle.None,
            Font = new Font(Tema.FontFamily, 11),
            BackColor = Tema.CorCardAlt,
            ForeColor = Tema.CorTextoEscuro,
            Multiline = true,
            PlaceholderText = "Ex: Padaria Artesanal do João, Açougue Central, Loja 24 horas..."
        };
        txtDescricao.Padding = new Padding(12, 8, 12, 8);
        txtDescricao.Paint += (s, e) =>
        {
            using var pen = new Pen(Tema.CorBorda, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, txtDescricao.Width - 1, txtDescricao.Height - 1);
        };

        var lblModulos = new Label
        {
            Text = "MÓDULOS QUE SERÃO ATIVADOS",
            Dock = DockStyle.Top,
            Height = 24,
            Font = new Font(Tema.FontFamily, 9, FontStyle.Bold),
            ForeColor = Tema.CorTextoMedio,
            Padding = new Padding(0, 20, 0, 8),
            BackColor = Color.Transparent
        };

        var pnlModulos = new Panel { Dock = DockStyle.Top, Height = 140, BackColor = Color.Transparent };
        var flpModulos = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.Transparent
        };

        // Placeholder para módulos
        var lblModulosInfo = new Label
        {
            Text = "Selecione um tipo de negócio para ver os módulos recomendados",
            Dock = DockStyle.Fill,
            Font = new Font(Tema.FontFamily, 10),
            ForeColor = Tema.CorTextoMedio,
            TextAlign = ContentAlignment.TopLeft,
            BackColor = Color.Transparent
        };
        flpModulos.Controls.Add(lblModulosInfo);
        flpModulos.Tag = "modulos";

        pnlModulos.Controls.Add(flpModulos);

        // --- Botão Configurar ---
        var pnlBotao = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.Transparent, Padding = new Padding(0, 12, 0, 0) };
        btnConfigurar = new Button
        {
            Text = "CONFIRMAR CONFIGURAÇÃO",
            Dock = DockStyle.Fill,
            Font = new Font(Tema.FontFamily, 12, FontStyle.Bold),
            BackColor = Tema.CorPrimaria,
            ForeColor = Tema.Branco,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Enabled = false
        };
        btnConfigurar.FlatAppearance.BorderSize = 0;
        btnConfigurar.FlatAppearance.MouseOverBackColor = Tema.CorPrimariaDark;
        btnConfigurar.Click += BtnConfigurar_Click;
        pnlBotao.Controls.Add(btnConfigurar);

        card.Controls.Add(pnlBotao);
        card.Controls.Add(pnlModulos);
        card.Controls.Add(lblModulos);
        card.Controls.Add(txtDescricao);
        card.Controls.Add(lblDescricao);
        card.Controls.Add(lblSelecionado);
        card.Controls.Add(lblDetalhes);

        colDir.Controls.Add(card);

        conteudo.Controls.Add(colDir);
        conteudo.Controls.Add(colEsq);

        container.Controls.Add(conteudo);
        container.Controls.Add(header);

        Controls.Add(container);

        // Borda sutil
        Paint += (s, e) =>
        {
            using var pen = new Pen(Tema.CorBorda, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        };
    }

    private static string DescricaoTipo(TipoNegocio tipo) => tipo switch
    {
        TipoNegocio.Padaria => "🥐 Padaria",
        TipoNegocio.Acougue => "🥩 Açougue",
        TipoNegocio.Loja => "🛍️ Loja Varejo",
        TipoNegocio.Industria => "🏭 Indústria",
        TipoNegocio.Bazar => "🧺 Bazar/Armarinho",
        TipoNegocio.Supermercado => "🛒 Supermercado",
        TipoNegocio.Farmacia => "💊 Farmácia",
        TipoNegocio.Restaurante => "🍽️ Restaurante/Bar",
        _ => tipo.ToString()
    };

    private Control CriarBotaoTipo(TipoNegocio tipo)
    {
        var modulos = ModulosPorTipo.ObterModulosRecomendados(tipo);
        var qtdModulos = ModulosPorTipo.ObterTodosModulos()
            .Count(m => (modulos & m) == m);

        var pnl = new Panel
        {
            Width = 410,
            Height = 70,
            Margin = new Padding(0, 0, 0, 10),
            BackColor = Tema.CorCardAlt,
            Cursor = Cursors.Hand,
            Tag = tipo
        };

        pnl.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
            using var path = Tema.PathArredondado(rect, Tema.RaioBotao);
            using var brush = new SolidBrush(pnl.BackColor);
            g.FillPath(brush, path);
            using var pen = new Pen(Tema.CorBorda, 1);
            g.DrawPath(pen, path);
        };

        var lblTipo = new Label
        {
            Text = DescricaoTipo(tipo),
            Left = 20,
            Top = 8,
            Width = 370,
            Height = 24,
            Font = new Font(Tema.FontFamily, 12, FontStyle.Bold),
            ForeColor = Tema.CorTextoEscuro,
            BackColor = Color.Transparent
        };

        var lblModulos = new Label
        {
            Text = $"{qtdModulos} módulos recomendados",
            Left = 20,
            Top = 36,
            Width = 370,
            Height = 20,
            Font = new Font(Tema.FontFamily, 9),
            ForeColor = Tema.CorTextoMedio,
            BackColor = Color.Transparent
        };

        pnl.Controls.Add(lblModulos);
        pnl.Controls.Add(lblTipo);

        EventHandler click = (s, e) => SelecionarTipo(tipo, pnl);
        pnl.Click += click;
        lblTipo.Click += click;
        lblModulos.Click += click;

        pnl.MouseEnter += (s, e) =>
        {
            pnl.BackColor = Tema.CorPrimariaSoft;
            lblTipo.BackColor = Tema.CorPrimariaSoft;
            lblModulos.BackColor = Tema.CorPrimariaSoft;
        };

        pnl.MouseLeave += (s, e) =>
        {
            pnl.BackColor = (_tipoSelecionado == tipo) ? Tema.CorPrimariaSoft : Tema.CorCardAlt;
            lblTipo.BackColor = pnl.BackColor;
            lblModulos.BackColor = pnl.BackColor;
        };

        return pnl;
    }

    private void SelecionarTipo(TipoNegocio tipo, Panel pnl)
    {
        // Resetar cor dos botões anteriores
        foreach (Control ctrl in pnlTipos.Controls)
        {
            if (ctrl is Panel p && p.Tag is TipoNegocio panelTipo && panelTipo != tipo)
            {
                p.BackColor = Tema.CorCardAlt;
                foreach (Control c in p.Controls)
                    c.BackColor = Tema.CorCardAlt;
            }
        }

        _tipoSelecionado = tipo;
        pnl.BackColor = Tema.CorPrimariaSoft;
        foreach (Control ctrl in pnl.Controls)
            ctrl.BackColor = Tema.CorPrimariaSoft;

        // Atualizar detalhes
        lblSelecionado.Text = DescricaoTipo(tipo);

        var modulos = ModulosPorTipo.ObterModulosRecomendados(tipo);
        AtualizarListaModulos(modulos);

        btnConfigurar.Enabled = true;
        btnConfigurar.BackColor = Tema.CorSucesso;
        btnConfigurar.FlatAppearance.MouseOverBackColor = Color.FromArgb(25, 100, 60);  // Darker green

        txtDescricao.Focus();
    }

    private void AtualizarListaModulos(ModuloSistema modulos)
    {
        var pnlModulos = Controls.Cast<Control>()
            .OfType<Panel>()
            .SelectMany(p => p.Controls.Cast<Control>())
            .OfType<Panel>()
            .SelectMany(p => p.Controls.Cast<Control>())
            .OfType<FlowLayoutPanel>()
            .FirstOrDefault(f => f.Tag?.ToString() == "modulos");

        if (pnlModulos == null) return;

        pnlModulos.Controls.Clear();

        foreach (var modulo in ModulosPorTipo.ObterTodosModulos())
        {
            if ((modulos & modulo) == modulo)
            {
                var lbl = new Label
                {
                    Text = $"✓ {ModulosPorTipo.ObterDescricaoModulo(modulo)}",
                    Height = 24,
                    Font = new Font(Tema.FontFamily, 10),
                    ForeColor = ModulosPorTipo.EObrigatorio(modulo) ? Tema.CorTextoEscuro : Tema.CorSucesso,
                    BackColor = Color.Transparent,
                    AutoSize = true
                };
                pnlModulos.Controls.Add(lbl);
            }
        }
    }

    private async void BtnConfigurar_Click(object? sender, EventArgs e)
    {
        if (_tipoSelecionado == null)
        {
            Toast.Mostrar("Selecione um tipo de negócio", TipoToast.Aviso, owner: this);
            return;
        }

        btnConfigurar.Enabled = false;
        var textoOriginal = btnConfigurar.Text;
        btnConfigurar.Text = "Configurando...";

        try
        {
            var tipo = _tipoSelecionado.Value;

            // Salva no banco (ConfiguracaoNegocio)
            await _configuracao.ConfigurarNegocio(tipo, txtDescricao.Text.Trim());

            // Sincroniza no arquivo implantacao.json para manter os dois configs alinhados
            var implantacaoAtual = await _implantacao.ObterAsync();
            implantacaoAtual.Perfil = tipo;
            implantacaoAtual.ModulosAtivos = ModulosPorTipo.ObterModulosRecomendados(tipo);
            await _implantacao.SalvarAsync(implantacaoAtual);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            Toast.Mostrar($"Erro: {ex.Message}", TipoToast.Erro, owner: this);
            btnConfigurar.Enabled = true;
            btnConfigurar.Text = textoOriginal;
        }
    }
}
