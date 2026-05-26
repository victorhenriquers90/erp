using FluentValidation;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Validators;

public class VendaValidator : AbstractValidator<Venda>
{
    public VendaValidator()
    {
        RuleFor(v => v.UsuarioId)
            .GreaterThan(0).WithMessage("Usuário é obrigatório");

        RuleFor(v => v.Status)
            .IsInEnum().WithMessage("Status de venda inválido");

        RuleFor(v => v.Total)
            .GreaterThanOrEqualTo(0).WithMessage("Total da venda não pode ser negativo");

        RuleFor(v => v.SubTotal)
            .GreaterThanOrEqualTo(0).WithMessage("Subtotal não pode ser negativo");

        RuleFor(v => v.Desconto)
            .GreaterThanOrEqualTo(0).WithMessage("Desconto não pode ser negativo")
            .LessThanOrEqualTo(v => v.SubTotal).WithMessage("Desconto não pode ser maior que o subtotal");

        RuleFor(v => v.Acrescimo)
            .GreaterThanOrEqualTo(0).WithMessage("Acréscimo não pode ser negativo");

        RuleFor(v => v.Itens)
            .NotEmpty().WithMessage("Venda deve conter pelo menos um item")
            .When(v => v.Status == StatusVenda.Finalizada);

        RuleForEach(v => v.Itens)
            .SetValidator(new ItemVendaValidator())
            .When(v => v.Itens != null && v.Itens.Any());
    }
}

public class FinalizarVendaValidator : AbstractValidator<(Venda venda, List<PagamentoVenda> pagamentos)>
{
    public FinalizarVendaValidator()
    {
        RuleFor(x => x.venda)
            .NotNull().WithMessage("Venda não encontrada");

        RuleFor(x => x.venda.Itens)
            .NotEmpty().WithMessage("Venda deve conter itens")
            .When(x => x.venda != null);

        RuleFor(x => x.pagamentos)
            .NotEmpty().WithMessage("Deve haver pelo menos uma forma de pagamento");

        RuleFor(x => x.pagamentos.Sum(p => p.Valor))
            .GreaterThanOrEqualTo(0)
            .WithMessage("Valor pago deve ser maior ou igual a zero")
            .When(x => x.pagamentos != null);

        // Validação do total pago vs valor da venda é feita no serviço
        // pois requer acesso ao valor total que pode ser calculado dinamicamente

        RuleForEach(x => x.pagamentos)
            .SetValidator(new PagamentoVendaValidator())
            .When(x => x.pagamentos != null);
    }
}
