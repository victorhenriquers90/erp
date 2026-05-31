using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Tests.Builders;

/// <summary>
/// Fluent builder for creating test Usuario entities.
/// Provides default values and convenience methods for common scenarios.
/// </summary>
public class UsuarioBuilder
{
    private int _id = 1;
    private string _login = "testuser";
    private string _nome = "Test User";
    private PerfilUsuario _perfil = PerfilUsuario.Caixa;
    private bool _ativo = true;
    private string _senhaHash = string.Empty;

    public UsuarioBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public UsuarioBuilder WithLogin(string login)
    {
        _login = login;
        return this;
    }

    public UsuarioBuilder WithNome(string nome)
    {
        _nome = nome;
        return this;
    }

    public UsuarioBuilder WithPerfil(PerfilUsuario perfil)
    {
        _perfil = perfil;
        return this;
    }

    public UsuarioBuilder WithAtivo(bool ativo)
    {
        _ativo = ativo;
        return this;
    }

    public UsuarioBuilder WithSenhaHash(string senhaHash)
    {
        _senhaHash = senhaHash;
        return this;
    }

    /// <summary>
    /// Set password hash using SenhaHasher for secure test data.
    /// </summary>
    public UsuarioBuilder WithSenha(string senha)
    {
        _senhaHash = SenhaHasher.Hash(senha);
        return this;
    }

    public Usuario Build()
    {
        var usuario = new Usuario
        {
            Id = _id,
            Login = _login,
            Nome = _nome,
            Perfil = _perfil,
            Ativo = _ativo,
            SenhaHash = _senhaHash
        };

        // Set default password hash if not explicitly set
        if (string.IsNullOrEmpty(_senhaHash))
        {
            usuario.SenhaHash = SenhaHasher.Hash("senha123");
        }

        return usuario;
    }

    /// <summary>
    /// Create admin user with default values.
    /// </summary>
    public static Usuario CreateAdmin(int id = 1)
    {
        return new UsuarioBuilder()
            .WithId(id)
            .WithLogin($"admin{id}")
            .WithNome($"Administrator {id}")
            .WithPerfil(PerfilUsuario.Administrador)
            .WithSenha("admin123")
            .Build();
    }

    /// <summary>
    /// Create manager user with default values.
    /// </summary>
    public static Usuario CreateGerente(int id = 2)
    {
        return new UsuarioBuilder()
            .WithId(id)
            .WithLogin($"gerente{id}")
            .WithNome($"Manager {id}")
            .WithPerfil(PerfilUsuario.Gerente)
            .WithSenha("gerente123")
            .Build();
    }

    /// <summary>
    /// Create cashier user with default values.
    /// </summary>
    public static Usuario CreateCaixa(int id = 3)
    {
        return new UsuarioBuilder()
            .WithId(id)
            .WithLogin($"caixa{id}")
            .WithNome($"Cashier {id}")
            .WithPerfil(PerfilUsuario.Caixa)
            .WithSenha("caixa123")
            .Build();
    }

    /// <summary>
    /// Create warehouse user with default values.
    /// </summary>
    public static Usuario CreateEstoquista(int id = 4)
    {
        return new UsuarioBuilder()
            .WithId(id)
            .WithLogin($"estoquista{id}")
            .WithNome($"Warehouse {id}")
            .WithPerfil(PerfilUsuario.Estoquista)
            .WithSenha("estoque123")
            .Build();
    }
}
