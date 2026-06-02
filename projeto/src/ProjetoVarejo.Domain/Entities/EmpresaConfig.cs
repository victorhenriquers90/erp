namespace ProjetoVarejo.Domain.Entities;

public class EmpresaConfig : EntidadeBase
{
    public string RazaoSocial { get; set; } = string.Empty;
    public string NomeFantasia { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string InscricaoEstadual { get; set; } = string.Empty;
    public string? InscricaoMunicipal { get; set; }
    public string Cep { get; set; } = string.Empty;
    public string Logradouro { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? Complemento { get; set; }
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Uf { get; set; } = "SP";
    public string CodigoMunicipioIbge { get; set; } = "3550308";
    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RegimeTributario { get; set; } = "1";

    public string CertificadoCaminho { get; set; } = string.Empty;
    public string CertificadoSenha { get; set; } = string.Empty;
    public string CscId { get; set; } = string.Empty;
    public string CscToken { get; set; } = string.Empty;
    public bool AmbienteHomologacao { get; set; } = true;
    public int ProximoNumeroNfce { get; set; } = 1;
    public int SerieNfce { get; set; } = 1;
    public int ProximoNumeroNfe { get; set; } = 1;
    public int SerieNfe { get; set; } = 1;

    public int ImpressoraTipo { get; set; } = 1;
    public string ImpressoraDestino { get; set; } = "";
    public int ImpressoraPorta { get; set; } = 9100;
    public int ImpressoraBaud { get; set; } = 9600;
    public int ImpressoraColunas { get; set; } = 48;
    public bool ImprimirAutomatico { get; set; } = true;

    public string PixChave { get; set; } = string.Empty;
    public string PixNomeRecebedor { get; set; } = string.Empty;
    public string PixCidade { get; set; } = string.Empty;
}
