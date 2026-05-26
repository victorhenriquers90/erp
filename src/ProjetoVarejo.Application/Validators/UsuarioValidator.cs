using FluentValidation;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Validators;

public class UsuarioValidator : AbstractValidator<Usuario>
{
    public UsuarioValidator()
    {
        RuleFor(u => u.Login)
            .NotEmpty().WithMessage("Login é obrigatório")
            .Length(3, 50).WithMessage("Login deve ter entre 3 e 50 caracteres")
            .Matches(@"^[a-zA-Z0-9_-]+$").WithMessage("Login deve conter apenas letras, números, hífen e underscore")
            .Must(login => login == login.ToLower()).WithMessage("Login deve ser em minúsculas");

        RuleFor(u => u.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .Length(3, 100).WithMessage("Nome deve ter entre 3 e 100 caracteres")
            .Matches(@"^[a-zA-Z\s]+$").WithMessage("Nome deve conter apenas letras e espaços");

        RuleFor(u => u.Perfil)
            .IsInEnum().WithMessage("Perfil inválido");

        RuleFor(u => u.Ativo)
            .NotNull().WithMessage("Status de ativo/inativo é obrigatório");

        RuleFor(u => u.SenhaHash)
            .NotEmpty().WithMessage("Hash de senha é obrigatório para usuários cadastrados")
            .When(u => u.Id > 0);
    }
}

public class CriarUsuarioValidator : AbstractValidator<(string login, string nome, PerfilUsuario perfil, string senha)>
{
    public CriarUsuarioValidator()
    {
        RuleFor(x => x.login)
            .NotEmpty().WithMessage("Login é obrigatório")
            .Length(3, 50).WithMessage("Login deve ter entre 3 e 50 caracteres")
            .Matches(@"^[a-zA-Z0-9_-]+$").WithMessage("Login deve conter apenas letras, números, hífen e underscore");

        RuleFor(x => x.nome)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .Length(3, 100).WithMessage("Nome deve ter entre 3 e 100 caracteres")
            .Matches(@"^[a-zA-Z\s]+$").WithMessage("Nome deve conter apenas letras e espaços");

        RuleFor(x => x.senha)
            .NotEmpty().WithMessage("Senha é obrigatória")
            .Length(6, 50).WithMessage("Senha deve ter entre 6 e 50 caracteres")
            .Matches(@"[A-Z]").WithMessage("Senha deve conter pelo menos uma letra maiúscula")
            .Matches(@"[a-z]").WithMessage("Senha deve conter pelo menos uma letra minúscula")
            .Matches(@"[0-9]").WithMessage("Senha deve conter pelo menos um número");

        RuleFor(x => x.perfil)
            .IsInEnum().WithMessage("Perfil inválido");
    }
}
