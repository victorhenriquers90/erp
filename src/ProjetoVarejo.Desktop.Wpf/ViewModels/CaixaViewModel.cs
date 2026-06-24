using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Application.Contracts.Services.DTOs;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

public partial class CaixaViewModel : BaseViewModel
{
    private readonly ICaixaService _service;

    [ObservableProperty] private CaixaSessao? _caixaAberto;
    [ObservableProperty] private bool _temCaixaAberto;
    [ObservableProperty] private decimal _valorAbertura;
    [ObservableProperty] private decimal _valorMovimento;
    [ObservableProperty] private string _motivoMovimento = string.Empty;
    [ObservableProperty] private decimal _valorFechamentoInformado;
    [ObservableProperty] private string _observacaoFechamento = string.Empty;

    [ObservableProperty] private decimal _totalDinheiro;
    [ObservableProperty] private decimal _totalDebito;
    [ObservableProperty] private decimal _totalCredito;
    [ObservableProperty] private decimal _totalPix;
    [ObservableProperty] private decimal _totalOutros;
    [ObservableProperty] private decimal _totalSuprimentos;
    [ObservableProperty] private decimal _totalSangrias;
    [ObservableProperty] private decimal _saldoEsperado;
    [ObservableProperty] private decimal _diferenca;

    [ObservableProperty] private string _status = "Caixa nao verificado";
    [ObservableProperty] private string _mensagem = string.Empty;
    [ObservableProperty] private string _erro = string.Empty;

    public CaixaViewModel(ICaixaService service) => _service = service;

    [RelayCommand]
    private async Task CarregarAsync()
    {
        SetBusy(true, "Verificando caixa...");
        LimparMensagens();
        try
        {
            CaixaAberto = await _service.ObterCaixaAbertoAsync();
            if (CaixaAberto == null)
            {
                TemCaixaAberto = false;
                Status = "Nenhum caixa aberto";
                LimparResumo();
                return;
            }

            TemCaixaAberto = true;
            Status = $"Caixa aberto desde {CaixaAberto.AbertaEm:dd/MM/yyyy HH:mm}";
            await CarregarResumoAsync(CaixaAberto.Id);
        }
        catch (Exception ex)
        {
            Erro = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task AbrirAsync()
    {
        SetBusy(true, "Abrindo caixa...");
        LimparMensagens();
        try
        {
            var result = await _service.AbrirAsync(ValorAbertura);
            if (!result.Sucesso)
            {
                Erro = result.Erro ?? "Nao foi possivel abrir o caixa.";
                return;
            }

            Mensagem = "Caixa aberto com sucesso.";
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            Erro = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task SangriaAsync()
    {
        SetBusy(true, "Registrando sangria...");
        LimparMensagens();
        try
        {
            var result = await _service.SangriaAsync(ValorMovimento, MotivoMovimento);
            if (!result.Sucesso)
            {
                Erro = result.Erro ?? "Nao foi possivel registrar sangria.";
                return;
            }

            Mensagem = "Sangria registrada.";
            ValorMovimento = 0;
            MotivoMovimento = string.Empty;
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            Erro = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task SuprimentoAsync()
    {
        SetBusy(true, "Registrando suprimento...");
        LimparMensagens();
        try
        {
            var result = await _service.SuprimentoAsync(ValorMovimento, MotivoMovimento);
            if (!result.Sucesso)
            {
                Erro = result.Erro ?? "Nao foi possivel registrar suprimento.";
                return;
            }

            Mensagem = "Suprimento registrado.";
            ValorMovimento = 0;
            MotivoMovimento = string.Empty;
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            Erro = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task FecharAsync()
    {
        SetBusy(true, "Fechando caixa...");
        LimparMensagens();
        try
        {
            if (CaixaAberto == null)
            {
                Erro = "Nao existe caixa aberto para fechar.";
                return;
            }

            var result = await _service.FecharAsync(CaixaAberto.Id, ValorFechamentoInformado, ObservacaoFechamento);
            if (!result.Sucesso || result.Valor == null)
            {
                Erro = result.Erro ?? "Nao foi possivel fechar o caixa.";
                return;
            }

            Diferenca = result.Valor.Diferenca;
            Mensagem = $"Caixa fechado. Diferenca: {Diferenca:C2}";
            ValorFechamentoInformado = 0;
            ObservacaoFechamento = string.Empty;
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            Erro = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task CarregarResumoAsync(int caixaSessaoId)
    {
        var resumo = await _service.ResumoAsync(caixaSessaoId);

        TotalDinheiro = ObterPorForma(resumo, FormaPagamentoTipo.Dinheiro);
        TotalDebito = ObterPorForma(resumo, FormaPagamentoTipo.Debito);
        TotalCredito = ObterPorForma(resumo, FormaPagamentoTipo.Credito);
        TotalPix = ObterPorForma(resumo, FormaPagamentoTipo.Pix);
        TotalOutros = ObterPorForma(resumo, FormaPagamentoTipo.Outros);

        TotalSuprimentos = resumo.TotalSuprimentos;
        TotalSangrias = resumo.TotalSangrias;
        SaldoEsperado = resumo.SaldoDinheiroEsperado;
    }

    private static decimal ObterPorForma(ResumoCaixa resumo, FormaPagamentoTipo forma) =>
        resumo.VendasPorForma.TryGetValue(forma, out var valor) ? valor : 0m;

    private void LimparResumo()
    {
        TotalDinheiro = 0;
        TotalDebito = 0;
        TotalCredito = 0;
        TotalPix = 0;
        TotalOutros = 0;
        TotalSuprimentos = 0;
        TotalSangrias = 0;
        SaldoEsperado = 0;
    }

    private void LimparMensagens()
    {
        Erro = string.Empty;
        Mensagem = string.Empty;
    }
}
