using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Enums;

namespace ProjetoVarejo.Desktop.Forms;

public class FrmImplantacao : Form
{
    private readonly ImplantacaoService _svc;
    private ImplantacaoConfig _config = new();

    private ComboBox cboPerfil = null!;
    private Label lblDescricao = null!;
    private Label lblResumo = null!;
    private ListView lstModulos = null!;

    public FrmImplantacao(ImplantacaoService svc)
    {
        _svc = svc;
        InitUi();
        Shown += async (s, e) => await CarregarAsync();
    }

    private void InitUi()
    {
        Text = "Perfil de Implantacao";
        Size = new Size(980, 700);
        MinimumSize = new Size(880, 620);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Tema.CorFundo;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderPagina(
            "Perfil de Implantacao",
            "Defina onde o sistema sera usado e quais modulos ficam disponiveis");

        var topo = new Card { Dock = DockStyle.Top, Height = 126, Padding = new Padding(20) };
        var lblPerfil = Inputs.Rotulo("Segmento de uso", 0, 0, 360);
        cboPerfil = new ComboBox
        {
            Left = 0,
            Top = 24,
            Width = 360,
            Height = Tema.AlturaInput,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = Tema.FontCorpo,
            DisplayMember = "Nome",
            ValueMember = "Id"
        };
        cboPerfil.SelectedIndexChanged += (s, e) => AtualizarDescricaoPerfil();

        lblDescricao = new Label
        {
            Left = 390,
            Top = 24,
            Width = 500,
            Height = 44,
            Font = Tema.FontCorpo,
            ForeColor = Tema.CorTextoMedio,
            BackColor = Color.Transparent
        };

        var btnAplicar = Botoes.Info("Aplicar recomendado", 190, 38);
        btnAplicar.Left = 0;
        btnAplicar.Top = 74;
        btnAplicar.Click += (s, e) => AplicarPerfilRecomendado();

        var btnTodos = Botoes.Ghost("Marcar todos", 140, 38);
        btnTodos.Left = 202;
        btnTodos.Top = 74;
        btnTodos.Click += (s, e) => MarcarTodos();

        topo.Controls.Add(btnTodos);
        topo.Controls.Add(btnAplicar);
        topo.Controls.Add(lblDescricao);
        topo.Controls.Add(cboPerfil);
        topo.Controls.Add(lblPerfil);

        var cardModulos = new Card { Dock = DockStyle.Fill, Padding = new Padding(20) };
        lblResumo = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Font = Tema.FontSubtitulo,
            ForeColor = Tema.CorTextoEscuro,
            BackColor = Color.Transparent
        };

        lstModulos = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            CheckBoxes = true,
            FullRowSelect = true,
            GridLines = false,
            BorderStyle = BorderStyle.None,
            Font = Tema.FontCorpo,
            BackColor = Tema.CorCard,
            ForeColor = Tema.CorTextoEscuro
        };
        lstModulos.Columns.Add("Modulo", 210);
        lstModulos.Columns.Add("Descricao", 560);
        lstModulos.Columns.Add("Tipo", 120);
        lstModulos.ItemCheck += LstModulos_ItemCheck;
        lstModulos.ItemChecked += (s, e) => AtualizarResumo();

        cardModulos.Controls.Add(lstModulos);
        cardModulos.Controls.Add(lblResumo);

        var rodape = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = Tema.CorFundo, Padding = new Padding(0, 15, 0, 0) };
        var btnSalvar = Botoes.Primario("Salvar perfil", 170, 40);
        btnSalvar.Dock = DockStyle.Right;
        btnSalvar.Click += async (s, e) => await SalvarAsync();
        var btnCancelar = Botoes.Ghost("Cancelar", 130, 40);
        btnCancelar.Dock = DockStyle.Right;
        btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        var spacer = new Panel { Dock = DockStyle.Right, Width = 10 };
        rodape.Controls.Add(btnSalvar);
        rodape.Controls.Add(spacer);
        rodape.Controls.Add(btnCancelar);

        Controls.Add(cardModulos);
        Controls.Add(rodape);
        Controls.Add(topo);
        Controls.Add(header);
    }

    private async Task CarregarAsync()
    {
        _config = await _svc.ObterAsync();

        cboPerfil.Items.Clear();
        foreach (var perfil in _svc.Perfis)
            cboPerfil.Items.Add(perfil);

        SelecionarPerfil(_config.Perfil);
        CarregarModulos();
        AtualizarDescricaoPerfil();
        AtualizarResumo();
    }

    private void CarregarModulos()
    {
        lstModulos.Items.Clear();
        foreach (var modulo in _svc.ModulosDisponiveis)
        {
            var item = new ListViewItem(modulo.Nome) { Tag = modulo };
            item.SubItems.Add(modulo.Descricao);
            item.SubItems.Add(modulo.Essencial ? "Essencial" : "Opcional");
            item.Checked = modulo.Essencial || (_config.ModulosAtivos & modulo.Id) == modulo.Id;
            if (modulo.Essencial)
                item.ForeColor = Tema.CorTextoMedio;

            lstModulos.Items.Add(item);
        }
    }

    private void AtualizarDescricaoPerfil()
    {
        if (cboPerfil.SelectedItem is not PerfilSistemaInfo perfil) return;
        lblDescricao.Text = perfil.Descricao;
    }

    private void AplicarPerfilRecomendado()
    {
        if (cboPerfil.SelectedItem is not PerfilSistemaInfo perfil) return;

        var recomendados = _svc.ModulosRecomendados(perfil.Id);
        foreach (ListViewItem item in lstModulos.Items)
        {
            if (item.Tag is not ModuloSistemaInfo modulo) continue;
            item.Checked = modulo.Essencial || (recomendados & modulo.Id) == modulo.Id;
        }

        AtualizarResumo();
    }

    private void MarcarTodos()
    {
        foreach (ListViewItem item in lstModulos.Items)
            item.Checked = true;

        AtualizarResumo();
    }

    private void LstModulos_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (lstModulos.Items[e.Index].Tag is ModuloSistemaInfo { Essencial: true } && e.NewValue == CheckState.Unchecked)
            e.NewValue = CheckState.Checked;
    }

    private async Task SalvarAsync()
    {
        if (cboPerfil.SelectedItem is not PerfilSistemaInfo perfil)
        {
            Toast.Mostrar("Selecione um perfil.", TipoToast.Aviso, owner: this);
            return;
        }

        var modulos = lstModulos.CheckedItems
            .Cast<ListViewItem>()
            .Select(i => ((ModuloSistemaInfo)i.Tag!).Id)
            .Aggregate(ModuloSistema.PDV, (acc, m) => acc | m);

        _config.Perfil = perfil.Id;
        _config.ModulosAtivos = modulos;
        await _svc.SalvarAsync(_config);

        Toast.Mostrar("Perfil de implantacao salvo.", TipoToast.Sucesso, owner: this);
        DialogResult = DialogResult.OK;
        Close();
    }

    private void SelecionarPerfil(TipoNegocio perfil)
    {
        for (var i = 0; i < cboPerfil.Items.Count; i++)
        {
            if (cboPerfil.Items[i] is PerfilSistemaInfo item && item.Id == perfil)
            {
                cboPerfil.SelectedIndex = i;
                return;
            }
        }

        if (cboPerfil.Items.Count > 0)
            cboPerfil.SelectedIndex = 0;
    }

    private void AtualizarResumo()
    {
        if (lblResumo == null || lstModulos == null) return;
        lblResumo.Text = $"{lstModulos.CheckedItems.Count} de {lstModulos.Items.Count} modulos ativos";
    }
}
