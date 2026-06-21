namespace ProjetoVarejo.Domain.Entities;

/// <summary>
/// Representa uma unidade física / estabelecimento da empresa.
/// Segue o conceito de Filial/Plant do SAP e TOTVS:
///   - O segmento de negócio é propriedade da Empresa (ConfiguracaoNegocio)
///   - A Filial define ONDE o usuário ou terminal opera
///   - Usuários são vinculados a uma Filial; null = acesso irrestrito (Admin)
/// </summary>
public class Filial : EntidadeBase
{
    /// <summary>Código curto para exibição, ex: "001", "MAT", "CTR"</summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Nome descritivo, ex: "Matriz", "Filial Centro"</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>CNPJ do estabelecimento (pode ser diferente da empresa matriz)</summary>
    public string? Cnpj { get; set; }

    /// <summary>Endereço resumido para exibição</summary>
    public string? Endereco { get; set; }

    public string? Telefone { get; set; }

    /// <summary>
    /// Indica que este é o estabelecimento principal (Matriz).
    /// Apenas uma filial pode ser Matriz.
    /// </summary>
    public bool IsMatriz { get; set; }

    // Navegação
    public ICollection<Usuario> Usuarios { get; set; } = [];
}
