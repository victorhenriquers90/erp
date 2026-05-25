using ProjetoVarejo.Application.Configuracao;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Domain.Enums;

namespace ProjetoVarejo.Desktop.Forms;

[ModuloRequerido(ModuloSistema.Auditoria)]
public class FrmAuditoria : Form
{
    private readonly AuditLogService _svc;
    private DateTimePicker dtDe = null!, dtAte = null!;
    private TextBox txtEntidade = null!;
    private StyledGrid grid = null!;

    public FrmAuditoria(AuditLogService svc)
    {
        _svc = svc;
        InitUi();
        Shown += async (s, e) => await CarregarAsync();
    }

    private void InitUi()
    {
        Text = "Auditoria";
        Size = new Size(1200, 660);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Tema.CorFundo;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderPagina("Auditoria", "Trilha de alterações nas entidades sensíveis");

        var filtros = new Card { Dock = DockStyle.Top, Height = 80, Padding = new Padding(16) };
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };
        pnl.Controls.Add(Inputs.Rotulo("DE", 0, 0));
        dtDe = new DateTimePicker { Left = 0, Top = 18, Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-7), Font = Tema.FontCorpo };
        pnl.Controls.Add(dtDe);
        pnl.Controls.Add(Inputs.Rotulo("ATÉ", 145, 0));
        dtAte = new DateTimePicker { Left = 145, Top = 18, Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(1), Font = Tema.FontCorpo };
        pnl.Controls.Add(dtAte);
        pnl.Controls.Add(Inputs.Rotulo("ENTIDADE", 290, 0));
        txtEntidade = new TextBox { Left = 290, Top = 18, Width = 220, Height = 28, Font = Tema.FontCorpo, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "ex: Venda, NotaFiscal..." };
        pnl.Controls.Add(txtEntidade);
        var btn = Botoes.Primario("Filtrar", 110, 32);
        btn.Top = 18; btn.Left = 525;
        btn.Click += async (s, e) => await CarregarAsync();
        pnl.Controls.Add(btn);
        filtros.Controls.Add(pnl);

        var cardGrid = new Card { Dock = DockStyle.Fill, Padding = new Padding(0) };
        grid = new StyledGrid();
        grid.Columns.Add("Data", "Data");
        grid.Columns.Add("Usuario", "Usuário");
        grid.Columns.Add("Entidade", "Entidade");
        grid.Columns.Add("Registro", "Id");
        grid.Columns.Add("Tipo", "Tipo");
        grid.Columns.Add("Antes", "Antes");
        grid.Columns.Add("Depois", "Depois");
        grid.Columns["Antes"]!.FillWeight = 250;
        grid.Columns["Depois"]!.FillWeight = 250;
        cardGrid.Controls.Add(grid);

        Controls.Add(cardGrid);
        Controls.Add(filtros);
        Controls.Add(header);
    }

    private async Task CarregarAsync()
    {
        var lista = await _svc.ListarAsync(dtDe.Value.Date, dtAte.Value.Date.AddDays(1),
            string.IsNullOrWhiteSpace(txtEntidade.Text) ? null : txtEntidade.Text.Trim());
        grid.Rows.Clear();
        foreach (var a in lista)
        {
            int idx = grid.Rows.Add(a.Data.ToString("dd/MM HH:mm:ss"),
                a.Usuario?.Nome ?? "(sistema)",
                a.Entidade, a.RegistroId, a.Tipo.ToString(),
                a.ValoresAntes, a.ValoresDepois);
            var tipoCell = grid.Rows[idx].Cells["Tipo"];
            tipoCell.Style.Font = Tema.FontCorpoBold;
            tipoCell.Style.ForeColor = a.Tipo switch
            {
                TipoAuditoria.Insert => Tema.CorSucesso,
                TipoAuditoria.Update => Tema.CorInfo,
                TipoAuditoria.Delete => Tema.CorErro,
                _ => Tema.CorTextoMedio
            };
        }
    }
}
