using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Infrastructure.Tef;

namespace ProjetoVarejo.Desktop.Forms;

public class FrmTef : Form
{
    private readonly ITefService _tef;
    private readonly decimal _valor;
    private readonly TefBandeira _bandeira;
    private readonly int _parcelas;
    public TefTransacao? Resultado { get; private set; }

    private Label _lblStatus = null!;
    private Label _lblIcone = null!;
    private ProgressBar _prog = null!;
    private Button _btnFechar = null!;

    public FrmTef(ITefService tef, decimal valor, TefBandeira bandeira, int parcelas = 1)
    {
        _tef = tef; _valor = valor; _bandeira = bandeira; _parcelas = parcelas;
        InitUi();
        Shown += async (s, e) => await ExecutarAsync();
    }

    private void InitUi()
    {
        Text = "Processando pagamento";
        Size = new Size(520, 380);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = Tema.Branco;
        MaximizeBox = false; MinimizeBox = false;
        ControlBox = false;

        var topo = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Tema.CorPrimaria };
        var lblTitulo = new Label
        {
            Text = $"{_bandeira.ToString().ToUpper()}   |   R$ {_valor:N2}" + (_parcelas > 1 ? $"  ({_parcelas}x)" : ""),
            Dock = DockStyle.Fill,
            Font = new Font(Tema.FontFamily, 16, FontStyle.Bold),
            ForeColor = Tema.Branco,
            TextAlign = ContentAlignment.MiddleCenter
        };
        topo.Controls.Add(lblTitulo);

        _lblIcone = new Label
        {
            Text = "",
            Font = new Font("Segoe MDL2 Assets", 64),
            ForeColor = Tema.CorAlerta,
            Dock = DockStyle.Top,
            Height = 120,
            TextAlign = ContentAlignment.MiddleCenter
        };

        _lblStatus = new Label
        {
            Text = "Aguarde, processando na maquininha...",
            Dock = DockStyle.Top,
            Height = 40,
            Font = new Font(Tema.FontFamily, 12, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Tema.CorTextoEscuro
        };

        _prog = new ProgressBar { Dock = DockStyle.Top, Height = 8, Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 30 };

        _btnFechar = Botoes.Primario("Fechar", 200, 44);
        _btnFechar.Dock = DockStyle.Bottom;
        _btnFechar.Visible = false;
        _btnFechar.Click += (s, e) => Close();

        Controls.Add(_btnFechar);
        Controls.Add(_lblStatus);
        Controls.Add(_lblIcone);
        Controls.Add(_prog);
        Controls.Add(topo);
    }

    private async Task ExecutarAsync()
    {
        try
        {
            var tx = await _tef.IniciarAsync(_bandeira, _valor, _parcelas);
            await Task.Delay(200);
            tx = await _tef.ConfirmarAsync(tx.Id);
            Resultado = tx;

            _prog.Visible = false;
            if (tx.Status == TefStatus.Aprovado)
            {
                _lblIcone.Text = ""; _lblIcone.ForeColor = Tema.CorSucesso;
                _lblStatus.Text = $"APROVADO   NSU {tx.Nsu}";
                _lblStatus.ForeColor = Tema.CorSucesso;
            }
            else
            {
                _lblIcone.Text = ""; _lblIcone.ForeColor = Tema.CorErro;
                _lblStatus.Text = "NEGADO — " + tx.Mensagem;
                _lblStatus.ForeColor = Tema.CorErro;
            }
            _btnFechar.Visible = true;
            ControlBox = true;
            DialogResult = tx.Status == TefStatus.Aprovado ? DialogResult.OK : DialogResult.Cancel;
        }
        catch (Exception ex)
        {
            _prog.Visible = false;
            _lblIcone.Text = ""; _lblIcone.ForeColor = Tema.CorErro;
            _lblStatus.Text = "Erro: " + ex.Message;
            _lblStatus.ForeColor = Tema.CorErro;
            _btnFechar.Visible = true;
            ControlBox = true;
        }
    }
}
