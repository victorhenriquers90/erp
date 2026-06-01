using FluentValidation;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Application.Validators;

public class UsuarioValidator : AbstractValidator<Usuario>
{
    public UsuarioValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres")
            .MaximumLength(150).WithMessage("Nome não pode ter mais de 150 caracteres");

        RuleFor(x => x.Login)
            .NotEmpty().WithMessage("Login é obrigatório")
            .MinimumLength(3).WithMessage("Login deve ter no mínimo 3 caracteres")
            .MaximumLength(50).WithMessage("Login não pode ter mais de 50 caracteres")
            .Matches(@"^[a-zA-Z0-9_.-]+$").WithMessage("Login deve conter apenas letras, números, ponto, hífen e underscore");

        RuleFor(x => x.SenhaHash)
            .NotEmpty().WithMessage("Senha é obrigatória")
            .MinimumLength(6).WithMessage("Senha deve ter no mínimo 6 caracteres");
    }
}
