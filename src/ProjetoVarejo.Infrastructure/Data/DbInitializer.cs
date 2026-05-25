using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Infrastructure.Data;

public static class DbInitializer
{
    public static void Inicializar(AppDbContext db)
    {
        db.Database.Migrate();

        if (!db.Usuarios.Any())
        {
            db.Usuarios.Add(new Usuario
            {
                Login = "admin",
                Nome = "Administrador",
                SenhaHash = SenhaHasher.Hash("admin"),
                Perfil = PerfilUsuario.Administrador
            });
        }

        if (!db.EmpresaConfigs.Any())
        {
            db.EmpresaConfigs.Add(new EmpresaConfig
            {
                RazaoSocial = "EMPRESA EXEMPLO LTDA",
                NomeFantasia = "Loja Exemplo",
                Cnpj = "00000000000000",
                InscricaoEstadual = "ISENTO",
                Cep = "01001000",
                Logradouro = "Praça da Sé",
                Numero = "1",
                Bairro = "Sé",
                Cidade = "São Paulo",
                Uf = "SP",
                CodigoMunicipioIbge = "3550308",
                Telefone = "11999999999",
                Email = "contato@exemplo.com.br",
                AmbienteHomologacao = true
            });
        }

        if (!db.Categorias.Any())
        {
            db.Categorias.AddRange(
                new Categoria { Nome = "Geral" },
                new Categoria { Nome = "Bebidas" },
                new Categoria { Nome = "Alimentos" },
                new Categoria { Nome = "Limpeza" },
                new Categoria { Nome = "Higiene" }
            );
        }

        db.SaveChanges();
    }
}
