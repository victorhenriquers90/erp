using ProjetoVarejo.Application.Configuracao;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Enums;
using ProjetoVarejo.Infrastructure.Backup;

namespace ProjetoVarejo.Desktop.Forms;

[ModuloRequerido(ModuloSistema.Backup)]
public class FrmBackup : Form
{
    private readonly BackupService _svc;
    private TextBox txtPasta = null!;
    private TextBox txtLog = null!;
    private CheckBox chkAuto = null!;

    public FrmBackup(BackupService svc)
    {
        _svc = svc;
        InitUi();
        CarregarConfig();
    }

    private void InitUi()
    {
        Text = "Backup do Banco";
        Size = new Size(820, 600);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = Tema.CorFundo;
        MaximizeBox = false; MinimizeBox = false;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderPagina("Backup do Banco", "Cópia de segurança do SQL Server (.bak)");

        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(20) };
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };

        int y = 0;
        txtPasta = Inputs.CampoTexto(pnl, "Pasta de destino", 0, y, 600);
        var btnPick = Botoes.Ghost("...", 60, 28);
        btnPick.Left = 610; btnPick.Top = y + 20;
        btnPick.Click += (s, e) =>
        {
            using var fbd = new FolderBrowserDialog { Description = "Selecione a pasta de backup" };
            if (fbd.ShowDialog(this) == DialogResult.OK) txtPasta.Text = fbd.SelectedPath;
        };
        pnl.Controls.Add(btnPick);
        y += 60;

        chkAuto = Inputs.CampoCheck(pnl, "Backup automático ao iniciar (diário)", 0, y, 400);
        y += 40;

        var btnExec = Botoes.Sucesso("Executar backup agora", 250, 44);
        btnExec.Top = y; btnExec.Left = 0;
        btnExec.Click += async (s, e) => await ExecutarAsync();
        pnl.Controls.Add(btnExec);
        var btnAbrir = Botoes.Info("Abrir pasta", 130, 44);
        btnAbrir.Top = y; btnAbrir.Left = 270;
        btnAbrir.Click += (s, e) =>
        {
            try
            {
                var pasta = string.IsNullOrWhiteSpace(txtPasta.Text)
                    ? Path.Combine(AppContext.BaseDirectory, "Backups")
                    : txtPasta.Text;
                Directory.CreateDirectory(pasta);
                System.Diagnostics.Process.Start("explorer.exe", pasta);
            }
            catch { }
        };
        pnl.Controls.Add(btnAbrir);
        y += 60;

        Inputs.Rotulo("LOG", 0, y); pnl.Controls.Add(Inputs.Rotulo("LOG", 0, y));
        txtLog = new TextBox
        {
            Left = 0, Top = y + 20, Width = 720, Height = 240, Multiline = true,
            ReadOnly = true, ScrollBars = ScrollBars.Vertical,
            Font = new Font(Tema.FontFamilyMono, 9),
            BackColor = Color.FromArgb(15, 25, 40),
            ForeColor = Color.FromArgb(120, 220, 150),
            BorderStyle = BorderStyle.None
        };
        pnl.Controls.Add(txtLog);

        card.Controls.Add(pnl);

        Controls.Add(card);
        Controls.Add(header);
    }

    private void CarregarConfig()
    {
        var arquivo = Path.Combine(AppContext.BaseDirectory, "backup.cfg");
        if (File.Exists(arquivo))
        {
            var linhas = File.ReadAllLines(arquivo);
            if (linhas.Length > 0) txtPasta.Text = linhas[0];
            if (linhas.Length > 1) chkAuto.Checked = linhas[1] == "1";
        }
        if (string.IsNullOrWhiteSpace(txtPasta.Text))
            txtPasta.Text = Path.Combine(AppContext.BaseDirectory, "Backups");
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        try
        {
            var arquivo = Path.Combine(AppContext.BaseDirectory, "backup.cfg");
            File.WriteAllLines(arquivo, new[] { txtPasta.Text, chkAuto.Checked ? "1" : "0" });
        }
        catch { }
        base.OnFormClosing(e);
    }

    private async Task ExecutarAsync()
    {
        Log("Iniciando backup...");
        UseWaitCursor = true;
        try
        {
            var res = await _svc.ExecutarAsync(txtPasta.Text);
            if (res.Sucesso) { Log("[OK] " + res.Valor); Toast.Mostrar("Backup concluído.", TipoToast.Sucesso, owner: this); }
            else { Log("[ERRO] " + res.Erro); Toast.Mostrar("Falha no backup.", TipoToast.Erro, owner: this); }
        }
        finally { UseWaitCursor = false; }
    }

    private void Log(string msg) => txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\r\n");
}
