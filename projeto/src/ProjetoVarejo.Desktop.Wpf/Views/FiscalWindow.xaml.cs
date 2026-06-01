using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class FiscalWindow : UserControl
{
    private readonly AppDbContext _db;
    private readonly CultureInfo _ptBr = new("pt-BR");

    public FiscalWindow(AppDbContext db)
    {
        _db = db;
        InitializeComponent();
        DtDe.SelectedDate = DateTime.Today.AddDays(-30);
        DtAte.SelectedDate = DateTime.Today;
        Loaded += async (_, _) => await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        var de = DtDe.SelectedDate ?? DateTime.Today.AddDays(-30);
        var ate = (DtAte.SelectedDate ?? DateTime.Today).AddDays(1);

        var notas = await _db.NotasFiscais
            .AsNoTracking()
            .Include(n => n.Venda)
            .Where(n => n.CriadoEm >= de && n.CriadoEm < ate)
            .OrderByDescending(n => n.CriadoEm)
            .Take(400)
            .ToListAsync();

        var empresa = await _db.EmpresaConfigs.AsNoTracking().OrderBy(e => e.Id).FirstOrDefaultAsync();

        LblAutorizadas.Text = notas.Count(n => n.Status == StatusNotaFiscal.Autorizada).ToString("N0", _ptBr);
        LblRejeitadas.Text = notas.Count(n => n.Status == StatusNotaFiscal.Rejeitada).ToString("N0", _ptBr);
        LblCanceladas.Text = notas.Count(n => n.Status == StatusNotaFiscal.Cancelada).ToString("N0", _ptBr);
        LblContingencia.Text = notas.Count(n => n.EmitidaEmContingencia).ToString("N0", _ptBr);
        LblAmbiente.Text = empresa == null ? "-" : (empresa.AmbienteHomologacao ? "Homologacao" : "Producao");

        DgNotas.ItemsSource = notas.Select(n => new
        {
            Numero = n.Numero.ToString("N0", _ptBr),
            Serie = n.Serie.ToString("N0", _ptBr),
            Venda = n.Venda?.Numero ?? n.VendaId.ToString("D6"),
            Status = StatusTexto(n.Status),
            AutorizadaEm = n.AutorizadaEm?.ToString("dd/MM/yyyy HH:mm") ?? "-",
            Chave = string.IsNullOrWhiteSpace(n.ChaveAcesso) ? "-" : n.ChaveAcesso,
            Mensagem = string.IsNullOrWhiteSpace(n.MensagemSefaz) ? "-" : n.MensagemSefaz
        }).ToList();

        DgConformidade.ItemsSource = MontarConformidade(empresa, notas);
    }

    private static string StatusTexto(StatusNotaFiscal status) => status switch
    {
        StatusNotaFiscal.NaoEmitida => "Nao emitida",
        StatusNotaFiscal.EmDigitacao => "Em digitacao",
        StatusNotaFiscal.Autorizada => "Autorizada",
        StatusNotaFiscal.Rejeitada => "Rejeitada",
        StatusNotaFiscal.Cancelada => "Cancelada",
        StatusNotaFiscal.Contingencia => "Contingencia",
        _ => status.ToString()
    };

    private static List<object> MontarConformidade(Domain.Entities.EmpresaConfig? empresa, List<Domain.Entities.NotaFiscal> notas)
    {
        var lista = new List<object>();
        if (empresa == null)
        {
            lista.Add(new { Item = "Cadastro da empresa", Situacao = "Pendente", Detalhe = "Empresa nÃ£o configurada para operaÃ§Ã£o fiscal." });
            return lista.Cast<object>().ToList();
        }

        lista.Add(new
        {
            Item = "Certificado digital",
            Situacao = string.IsNullOrWhiteSpace(empresa.CertificadoCaminho) ? "Pendente" : "OK",
            Detalhe = string.IsNullOrWhiteSpace(empresa.CertificadoCaminho) ? "Informe o caminho do certificado A1." : empresa.CertificadoCaminho
        });

        lista.Add(new
        {
            Item = "CSC / Token",
            Situacao = string.IsNullOrWhiteSpace(empresa.CscId) || string.IsNullOrWhiteSpace(empresa.CscToken) ? "Pendente" : "OK",
            Detalhe = string.IsNullOrWhiteSpace(empresa.CscId) || string.IsNullOrWhiteSpace(empresa.CscToken)
                ? "Preencha CSC ID e CSC Token para NFC-e."
                : "CSC configurado."
        });

        lista.Add(new
        {
            Item = "SequÃªncia de numeraÃ§Ã£o NFC-e",
            Situacao = empresa.ProximoNumeroNfce > 0 ? "OK" : "Ajustar",
            Detalhe = $"PrÃ³ximo nÃºmero configurado: {empresa.ProximoNumeroNfce:N0}"
        });

        var rejeitadas = notas.Count(n => n.Status == StatusNotaFiscal.Rejeitada);
        lista.Add(new
        {
            Item = "Taxa de rejeiÃ§Ã£o no perÃ­odo",
            Situacao = rejeitadas == 0 ? "OK" : "AtenÃ§Ã£o",
            Detalhe = rejeitadas == 0 ? "Sem rejeiÃ§Ãµes no perÃ­odo selecionado." : $"{rejeitadas:N0} nota(s) rejeitada(s)."
        });

        return lista.Cast<object>().ToList();
    }

    private async void Atualizar_Click(object sender, RoutedEventArgs e)
    {
        await CarregarAsync();
    }
}
